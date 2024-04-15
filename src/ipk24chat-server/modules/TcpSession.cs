using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Net;
using System.Net.Sockets;

namespace ipk24chat_server.modules
{
    public class TcpSession
    {
        private TcpClient client = null!;
        private NetworkStream stream = null!;
        private StreamReader reader = null!;
        private IPAddress _ip = null!;
        private ushort _port;

        private bool openState = false;

        private string username = null!;
        private string displayName = null!;

        private TaskCompletionSource finishReceiving = new TaskCompletionSource();
        private TaskCompletionSource endSession = new TaskCompletionSource();
        private TaskCompletionSource<Exception> sessionError = new TaskCompletionSource<Exception>();

        private ChannelUser channelUser = null!;
        private Channel currentChannel = null!;

        public TcpSession(TcpClient tcpClient)
        {
            client = tcpClient;
            stream = tcpClient.GetStream();
            reader = new StreamReader(stream);
            _ip = (client.Client.RemoteEndPoint as IPEndPoint)!.Address;
            _port = (ushort)(client.Client.RemoteEndPoint as IPEndPoint)!.Port;
        }

        public void StartSession()
        {
            ThreadPool.QueueUserWorkItem(SessionBehaviour);
            Console.WriteLine($"LOG | New TCP user session with {_ip.ToString()}:{_port} has been started");
        }

        private void SessionBehaviour(Object? state)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(MessageReceiver);

                var end = endSession.Task;
                var error = sessionError.Task;
                var result = Task.WhenAny(end, error).Result;

                if (openState && WelcomeSession.loggedUsers.ContainsKey(username))
                    WelcomeSession.loggedUsers[username] = false;

                SendByeMessage();
                finishReceiving.SetResult();

                if (result == error)
                    throw error.Result;

                if (openState)
                    currentChannel.LeaveUser(username).Wait();
            }
            catch (Exception ex)
            {
                if (openState)
                    currentChannel.LeaveUser(username).Wait();

                Console.WriteLine($"LOG | Problem with TCP user session {_ip.ToString()}:{_port}: {ex.Message}");
            }
            finally
            {
                StopSession();
            }
        }

        public void StopSession()
        {
            client.Close();
            Console.WriteLine($"LOG | TCP user session {_ip.ToString()}:{_port} has been closed");
        }

        private void MessageReceiver(Object? state)
        {
            while (true)
            {
                var finish = finishReceiving.Task;
                var buffer = new byte[1024];
                var receive = stream.ReadAsync(buffer, 0, buffer.Length);

                var complete = Task.WhenAny(finish, receive).Result;
                if (complete == finish)
                    return;

                try
                {
                    var count = receive.Result;
                    var data = new byte[count];
                    for (var i = 0; i < count; i++)
                        data[i] = buffer[i];
                    var message = Message.ConvertDataToMessage(data, Message.ProtocolType.TCP);
                    _ = Task.Run(() => ProcessMessage(message));
                }
                catch
                {
                    return;
                }
            }
        }

        public void ProcessMessage(Message? message)
        {
            if (message == null)
            {
                sessionError.SetResult(new ProtocolException("Invalid message format, session will be closed", "", ""));
                return;
            }

            Logging.LogMessage(_ip, _port, true, message);

            if (message.TypeOfMessage == Message.MessageType.ERR || message.TypeOfMessage == Message.MessageType.BYE)
            {
                endSession.SetResult();
                return;
            }

            if (!openState)
            {
                if (message.TypeOfMessage != Message.MessageType.AUTH)
                {
                    SendErrorMessage("Authentication message was required, session will be terminated!");
                    endSession.SetResult();
                    return;
                }

                username = message.Fields.Username!;
                displayName = message.Fields.DisplayName!;

                if (WelcomeSession.loggedUsers.ContainsKey(username) && WelcomeSession.loggedUsers[username] == true)
                {
                    SendReplyMessage("User with that username is already on the server", false);
                    return;
                }
                else
                {
                    if (!WelcomeSession.loggedUsers.ContainsKey(username))
                        WelcomeSession.loggedUsers[username] = true;
                    else
                        WelcomeSession.loggedUsers.TryAdd(username, true);

                    channelUser = new ChannelUser(displayName, null, this);
                    currentChannel = Channel.Channels["general"];
                    currentChannel.NewUser(username, channelUser).Wait();

                    SendReplyMessage("Authentication is successful. Welcome to the server!", true);
                    openState = true;
                    return;
                }
            }

            switch (message.TypeOfMessage)
            {
                case Message.MessageType.JOIN:
                    if (message.Fields.DisplayName != displayName)
                    {
                        displayName = message.Fields.DisplayName!;
                        channelUser.UpdateDisplayName(displayName);
                    }

                    var channelId = (string)message.Fields.ChannelId!;
                    if (channelId == currentChannel.ChannelId)
                    {
                        SendReplyMessage("You are already on this channel", false);
                        break;
                    }
                    currentChannel.LeaveUser(username).Wait();
                    Channel.Channels.TryGetValue(channelId, out var newChannel);
                    if (newChannel == null)
                        newChannel = new Channel(channelId, false);
                    currentChannel = newChannel;
                    currentChannel.NewUser(username, channelUser).Wait();
                    SendReplyMessage($"You has successfully joined \"{channelId}\" channel", true);
                    break;
                case Message.MessageType.MSG:
                    if (message.Fields.DisplayName != displayName)
                    {
                        displayName = (string)message.Fields.DisplayName!;
                        currentChannel.UpdateUserName(username, displayName).Wait();
                    }
                    currentChannel.NewMessage((MsgMessage)message, username).Wait();
                    break;
                default:
                    SendErrorMessage("Unexpected type of message, session will be terminated");
                    endSession.SetResult();
                    return;
            }
        }

        private void SendErrorMessage(string message)
        {
            var fields = new Message.MessageFields();
            fields.DisplayName = "Server";
            fields.MessageContent = message;
            var errMessage = new ErrMessage();
            errMessage.EncodeMessage(fields, Message.ProtocolType.TCP);
            SendMessage(errMessage).Wait();
        }

        private void SendByeMessage()
        {
            var byeMessage = new ByeMessage();
            byeMessage.EncodeMessage(new Message.MessageFields(), Message.ProtocolType.TCP);

            try
            {
                SendMessage(byeMessage).Wait();
            }
            catch
            {
                return;
            }
        }

        private void SendReplyMessage(string message, bool result)
        {
            var fields = new Message.MessageFields();
            fields.Result = result;
            fields.MessageContent = message;
            var replyMessage = new ReplyMessage();
            replyMessage.EncodeMessage(fields, Message.ProtocolType.TCP);
            SendMessage(replyMessage).Wait();
        }

        public async Task SendMessage(Message message)
        {
            try
            {
                await stream.WriteAsync(message.Data);
                stream.Flush();
                Logging.LogMessage(_ip, _port, false, message);
            }
            catch (Exception ex)
            {
                sessionError.SetResult(ex);
            }
        }
    }
}
