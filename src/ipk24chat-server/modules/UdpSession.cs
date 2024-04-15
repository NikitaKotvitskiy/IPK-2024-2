using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ipk24chat_server.modules
{
    public class UdpSession
    {
        private IPAddress _ip = null!;
        private ushort _port;

        private UdpClient client = null!;

        public object IdLock = new object();
        public ushort GetNewId() => _lastMessageId++;
        private ushort _lastMessageId;
        private readonly HashSet<ushort> UserMessageIds = [];

        private readonly ConcurrentDictionary<ushort, SemaphoreSlim> confirmationSemaphores = new ConcurrentDictionary<ushort, SemaphoreSlim>();

        private bool openState = false;

        private string username = null!;
        private string displayName = null!;

        private TaskCompletionSource finishReceiving = new TaskCompletionSource();
        private TaskCompletionSource endSession = new TaskCompletionSource();
        private TaskCompletionSource<Exception> sessionError = new TaskCompletionSource<Exception>();

        private ChannelUser channelUser = null!;
        private Channel currentChannel = null!;

        public UdpSession(IPEndPoint remoteEndPoint)
        {
            client = new UdpClient(0, AddressFamily.InterNetwork);
            client.Connect(remoteEndPoint);
            _ip = remoteEndPoint.Address;
            _port = (ushort)remoteEndPoint.Port;
        }

        public void StartSession(Message firstMessage)
        {
            ThreadPool.QueueUserWorkItem(SessionBehaviour, firstMessage);
            Console.WriteLine($"LOG | New UDP user session with {_ip.ToString()}:{_port} has been started");
        }

        private void SessionBehaviour(Object? state)
        {
            Message firstMessage = (Message)state!;
            _ = Task.Run(() => ProcessMessage(firstMessage, false));

            try
            {
                ThreadPool.QueueUserWorkItem(MessageReceiver);

                var end = endSession.Task;
                var error = sessionError.Task;
                var result = Task.WhenAny(end, error).Result;

                if (WelcomeSession.loggedUsers.ContainsKey(username))
                    WelcomeSession.loggedUsers[username] = false;
                SendByeMessage();
                finishReceiving.SetResult();

                if (result == error)
                    throw error.Result;

                currentChannel.LeaveUser(username).Wait();
            }
            catch (Exception ex)
            {
                currentChannel.LeaveUser(username).Wait();

                Console.WriteLine($"LOG | Problem with UDP user session {_ip.ToString()}:{_port}: {ex.Message}");
                StopSession();
            }
            finally
            {
                StopSession();
            }
        }

        private void StopSession()
        {
            client.Close();
            Console.WriteLine($"LOG | UDP user session {_ip.ToString()}:{_port} has been closed");
        }

        private void MessageReceiver(Object? state)
        {
            while (true)
            {
                var finish = finishReceiving.Task;
                var receive = client.ReceiveAsync();

                var completed = Task.WhenAny(finish, receive).Result;
                if (completed == finish)
                    return;

                try
                {
                    var data = receive.Result.Buffer;
                    var message = Message.ConvertDataToMessage(data, Message.ProtocolType.UDP);
                    _ = Task.Run(() => ProcessMessage(message));
                }
                catch
                {
                    return;
                }
            }
        }

        private void ProcessMessage(Message? message, bool needToConfirm = true)
        {
            if (message == null)
            {
                sessionError.SetResult(new ProtocolException("Invalid message format, termination will be closed", "", ""));
                return;
            }

            if (needToConfirm)
                Logging.LogMessage(_ip, _port, true, message);

            if (message.TypeOfMessage == Message.MessageType.CONFIRM)
            {
                var refId = message.Fields.MessageRefId;
                if (confirmationSemaphores.TryGetValue((ushort)refId!, out var sem))
                    sem.Release();
                return;
            }

            if (needToConfirm)
                SendConfirmMessage((ushort)message.Fields.MessageId!);

            if (message.TypeOfMessage == Message.MessageType.ERR || message.TypeOfMessage == Message.MessageType.BYE)
            {
                endSession.SetResult();
                return;
            }

            if (message.TypeOfMessage == Message.MessageType.REPLY)
            {
                SendErrorMessage("Invalid type of message, session will be terminated");
                endSession.SetResult();
                return;
            }

            if (!openState)
            {
                username = message.Fields.Username!;
                displayName = message.Fields.DisplayName!;

                if (WelcomeSession.loggedUsers.ContainsKey(username) && WelcomeSession.loggedUsers[username] == true)
                {
                    SendReplyMessage((ushort)message.Fields.MessageId!, "User with that login is already on the server", false);
                    return;
                }
                else
                {
                    if (!WelcomeSession.loggedUsers.ContainsKey(username))
                        WelcomeSession.loggedUsers.TryAdd(username, true);
                    else
                        WelcomeSession.loggedUsers[username] = true;

                    channelUser = new ChannelUser(displayName, this);
                    currentChannel = Channel.Channels["general"];
                    currentChannel.NewUser(username, channelUser).Wait();

                    SendReplyMessage((ushort)message.Fields.MessageId!, "Authentication is successful. Welcome to the server!", true);
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
                        SendReplyMessage((ushort)message.Fields.MessageId!, "You are already on this channel", false);
                        break;
                    }
                    currentChannel.LeaveUser(username).Wait();
                    Channel.Channels.TryGetValue(channelId, out var newChannel);
                    if (newChannel == null)
                        newChannel = new Channel(channelId, false);
                    currentChannel = newChannel;
                    currentChannel.NewUser(username, channelUser).Wait();
                    SendReplyMessage((ushort)message.Fields.MessageId!, $"You has successfully joined \"{channelId}\" channel", true);
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
            lock (IdLock) fields.MessageId = GetNewId();
            fields.DisplayName = "Server";
            fields.MessageContent = message;
            var errMessage = new ErrMessage();
            errMessage.EncodeMessage(fields, Message.ProtocolType.UDP);
            SendMessage(errMessage).Wait();
        }

        private void SendByeMessage()
        {
            var fields = new Message.MessageFields();
            lock (IdLock) fields.MessageId = GetNewId();
            var byeMessage = new ByeMessage();
            byeMessage.EncodeMessage(fields, Message.ProtocolType.UDP);

            try
            {
                SendMessage(byeMessage).Wait();
            }
            catch
            {
                return;
            }
        }

        private void SendConfirmMessage(ushort refId)
        {
            var fields = new Message.MessageFields();
            fields.MessageRefId = refId;
            var confMessage = new ConfirmMessage();
            confMessage.EncodeMessage(fields, Message.ProtocolType.UDP);
            SendMessage(confMessage).Wait();
        }

        private void SendReplyMessage(ushort refId, string message, bool result)
        {
            var fields = new Message.MessageFields();
            lock (IdLock) fields.MessageId = GetNewId();
            fields.MessageRefId = refId;
            fields.Result = result;
            fields.MessageContent = message;
            var replyMessage = new ReplyMessage();
            replyMessage.EncodeMessage(fields, Message.ProtocolType.UDP);
            SendMessage(replyMessage).Wait();
        }

        public async Task SendMessage(Message message)
        {
            if (message.TypeOfMessage == Message.MessageType.CONFIRM || message.TypeOfMessage == Message.MessageType.BYE)
            {
                await client.SendAsync(message.Data);
                Logging.LogMessage(_ip, _port, false, message);
                return;
            }

            var tries = Cla.UdpMaxRetransmissions + 1;
            var idToConfirm = message.Fields.MessageId;
            var sem = new SemaphoreSlim(1);
            confirmationSemaphores.TryAdd((ushort)idToConfirm!, sem);

            while (tries-- > 0)
            {
                await client.SendAsync(message.Data);
                Logging.LogMessage(_ip, _port, false, message);

                var result = sem.Wait(Cla.UdpTimeout);
                if (result)
                    return;
            }

            sessionError.SetResult(new ProtocolException("Confirmation message was not received", "", ""));
        }
    }
}
