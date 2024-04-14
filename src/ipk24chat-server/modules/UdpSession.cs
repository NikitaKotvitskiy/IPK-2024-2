using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Net;
using System.Net.Sockets;

namespace ipk24chat_server.modules
{
    internal class UdpSession
    {
        private IPAddress _ip = null!;
        private ushort _port;

        private UdpClient client = null!;
        private ushort _lastMessageId;
        private ushort GetNewId() => _lastMessageId++;
        private readonly HashSet<ushort> UserMessageIds = [];

        private bool openState = false;

        private string username = null!;
        private string displayName = null!;

        public void StartSession(IPEndPoint remoteEndPoint, Message firstMessage)
        {
            client = new UdpClient(0, AddressFamily.InterNetwork);
            client.Connect(remoteEndPoint);
            _ip = remoteEndPoint.Address;
            _port = (ushort)remoteEndPoint.Port;

            try
            {
                ProcessMessage(firstMessage);
                while (true)
                {
                    var receivedData = ReceiveMessage().Result;
                    var receivedMessage = Message.ConvertDataToMessage(receivedData, Message.ProtocolType.UDP);
                    if (receivedMessage == null)
                        throw new ProtocolException("Invalid message format", "", "");

                    Logging.LogMessage(_ip, _port, true, receivedMessage);
                    if (!ProcessMessage(receivedMessage))
                    {
                        SendByeMessage();
                        break;
                    }
                }
            }
            catch (ProtocolException pex)
            {
                SendErrorMessage(pex.Message);
                SendByeMessage();
                return;
            }
        }

        private void SendErrorMessage(string message)
        {
            var fields = new Message.MessageFields();
            fields.MessageId = GetNewId();
            fields.DisplayName = "Server";
            fields.MessageContent = message;
            var errMessage = new ErrMessage();
            errMessage.EncodeMessage(fields, Message.ProtocolType.UDP);
            SendMessage(errMessage).Wait();
        }

        private void SendByeMessage()
        {
            var fields = new Message.MessageFields();
            fields.MessageId = GetNewId();
            var byeMessage = new ByeMessage();
            byeMessage.EncodeMessage(fields, Message.ProtocolType.UDP);
            SendMessage(byeMessage).Wait();
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
            fields.MessageId = GetNewId();
            fields.MessageRefId = refId;
            fields.Result = result;
            fields.MessageContent = message;
            var replyMessage = new ReplyMessage();
            replyMessage.EncodeMessage(fields, Message.ProtocolType.UDP);
            SendMessage(replyMessage).Wait();
        }

        private bool ProcessMessage(Message message, bool confirm = true)
        {
            if (message.TypeOfMessage != Message.MessageType.CONFIRM && !UserMessageIds.Add((ushort)message.Fields.MessageId!))
                return true;

            if (confirm && message.TypeOfMessage != Message.MessageType.CONFIRM)
            {
                var mesId = (ushort)message.Fields.MessageId!;
                SendConfirmMessage(mesId);
            }

            if (!openState)
            {
                if (message.TypeOfMessage == Message.MessageType.BYE || message.TypeOfMessage == Message.MessageType.ERR)
                    return false;

                if (message.TypeOfMessage != Message.MessageType.AUTH)
                    throw new ProtocolException("Authentication required!", "AUTH message", $"{message.TypeOfMessage}");

                username = message.Fields.Username!;

                if (WelcomeSession.loggedUsers.ContainsKey(username) && WelcomeSession.loggedUsers[username] == true)
                    SendReplyMessage((ushort)message.Fields.MessageId!, "User with that login is already logged in", false);
                else
                {
                    if (!WelcomeSession.loggedUsers.ContainsKey(username))
                        WelcomeSession.loggedUsers.TryAdd(username, true);
                    else
                        WelcomeSession.loggedUsers[username] = true;

                    displayName = message.Fields.DisplayName!;
                    SendReplyMessage((ushort)(message.Fields.MessageId!), "Successful authentication, welcome to the server!", true);
                    
                    // TODO: connect to general channel...

                    openState = true;
                }
                return true;
            }

            if (message.TypeOfMessage == Message.MessageType.BYE || message.TypeOfMessage == Message.MessageType.ERR)
            {
                // TODO: leave channel
                WelcomeSession.loggedUsers[username] = false;
                return false;
            }

            switch (message.TypeOfMessage)
            {
                case Message.MessageType.JOIN:
                    // TODO: join or create a new channel
                    break;
                case Message.MessageType.MSG:
                    // TODO: chack displayName
                    // TODO: send message to the actual channel
                    break;
                case Message.MessageType.AUTH:
                case Message.MessageType.REPLY:
                    SendErrorMessage("Unacceptable type of message!");
                    return false;
                default:
                    break;
            }

            return true;
        }

        private async Task<byte[]> ReceiveMessage()
        {
            var result = await client.ReceiveAsync();
            var data = result.Buffer;
            return data;
        }

        private async Task SendMessage(Message message)
        {
            if (message.TypeOfMessage == Message.MessageType.CONFIRM || message.TypeOfMessage == Message.MessageType.BYE)
            {
                await client.SendAsync(message.Data);
                Logging.LogMessage(_ip, _port, false, message);
                return;
            }

            var tries = Cla.UdpMaxRetransmissions + 1;
            var idToConfirm = message.Fields.MessageId;
            while (tries-- > 0)
            {
                await client.SendAsync(message.Data);
                Logging.LogMessage(_ip, _port, false, message);

                var waitForConfirm = ReceiveMessage();
                var waitForTimeout = Task.Delay(Cla.UdpTimeout);

                var completed = await Task.WhenAny(waitForConfirm, waitForTimeout);
                if (completed == waitForTimeout)
                    continue;
                var recvMessageData = await waitForConfirm;

                var expectedConfirmationMessage = Message.ConvertDataToMessage(recvMessageData, Message.ProtocolType.UDP);
                if (expectedConfirmationMessage == null)
                    throw new ProtocolException("Invalid message format", "Confirmation message", "Invalid message");
                Logging.LogMessage(_ip, _port, true, expectedConfirmationMessage);

                if (expectedConfirmationMessage.TypeOfMessage == Message.MessageType.CONFIRM && expectedConfirmationMessage.Fields.MessageRefId == idToConfirm)
                    return;
            }

            throw new ProtocolException("Confirmation message was not received", "", "");
        }

        public void StopSession()
        {
            client.Close();
        }
    }
}
