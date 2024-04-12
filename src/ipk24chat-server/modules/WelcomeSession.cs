using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Net;
using System.Net.Sockets;

namespace ipk24chat_server.modules
{
    public class WelcomeSession
    {
        private TcpListener tcpListener = null!;
        private UdpClient udpClient = null!;

        public void StartWelcomeSession()
        {
            tcpListener = new TcpListener(Cla.ListeningIp, Cla.ListeningPort);
            udpClient = new UdpClient(Cla.ListeningPort, AddressFamily.InterNetwork);

            tcpListener.Start();
            ThreadPool.QueueUserWorkItem(ListenForTcpClients);
            ThreadPool.QueueUserWorkItem(ListenForUdpClients);
        }

        private void ListenForTcpClients(object? state)
        {
            while (true)
            {
                var tcpClient = tcpListener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(CreateNewTcpSession, tcpClient);
            }
        }

        private void ListenForUdpClients(object? state) 
        {
            while (true)
            {
                IPEndPoint? remoteEndPoint = null;
                var receivedData = udpClient.Receive(ref remoteEndPoint);
                if (!ConfirmFirstUdpMessage(receivedData, remoteEndPoint))
                    continue;
                ThreadPool.QueueUserWorkItem(CreateNewUdpSession, (remoteEndPoint, receivedData));
            }

        }

        private void CreateNewTcpSession(Object? stateInfo)
        {
            var tcpClient = (TcpClient)stateInfo!;
            var session = new TcpSession();
            session.StartSession(tcpClient);
        }

        private void CreateNewUdpSession(Object? stateInfo)
        {
            (IPEndPoint remoteEndPoint, byte[] data) = ((IPEndPoint, byte[]))stateInfo!;
            var session = new UdpSession();
            session.StartSession(remoteEndPoint, data);
        }


        private void GenerateUdpErrorMessage(string messageContent, IPEndPoint remoteEndPoint)
        {
            var fields = new Message.MessageFields();
            fields.MessageId = 0;
            fields.DisplayName = "Server";
            fields.MessageContent = messageContent;

            var errMessage = new ErrMessage();
            errMessage.EncodeMessage(fields, Message.ProtocolType.UDP);

            udpClient.Send(errMessage.Data, errMessage.Data.Length, remoteEndPoint);
        }

        private Message? ConvertDataIntoMessage(byte[] data, IPEndPoint remoteEndPoint)
        {
            Message? retValue = null;
            try
            {
                var messageType = Message.DefineTypeOfMessage(data, Message.ProtocolType.UDP);
                switch (messageType)
                {
                    case Message.MessageType.ERR:
                        retValue = new ErrMessage();
                        retValue.DecodeMessage(data, Message.ProtocolType.UDP);
                        break;
                    case Message.MessageType.CONFIRM:
                        return null;
                    case Message.MessageType.REPLY:
                        retValue = new ReplyMessage();
                        retValue.DecodeMessage(data, Message.ProtocolType.UDP);
                        break;
                    case Message.MessageType.AUTH:
                        retValue = new AuthMessage();
                        retValue.DecodeMessage(data, Message.ProtocolType.UDP);
                        break;
                    case Message.MessageType.JOIN:
                        retValue = new JoinMessage();
                        retValue.DecodeMessage(data, Message.ProtocolType.UDP);
                        break;
                    case Message.MessageType.MSG:
                        retValue = new MsgMessage();
                        retValue.DecodeMessage(data, Message.ProtocolType.UDP);
                        break;
                    case Message.MessageType.BYE:
                        retValue = new ByeMessage();
                        retValue.DecodeMessage(data, Message.ProtocolType.UDP);
                        break;
                }         
            }
            catch (ProtocolException pex)
            {
                GenerateUdpErrorMessage(pex.Message, remoteEndPoint);
                retValue = null;
            }

            return retValue;
        }
        private bool ConfirmFirstUdpMessage(byte[] data, IPEndPoint remoteEndPoint)
        {
            var recvMessage = ConvertDataIntoMessage(data, remoteEndPoint);
            if (recvMessage == null) return false;

            var fields = new Message.MessageFields();
            fields.MessageRefId = recvMessage.Fields.MessageId;
            var confMessage = new ConfirmMessage();
            confMessage.EncodeMessage(fields, Message.ProtocolType.UDP);

            udpClient.Send(confMessage.Data, confMessage.Data.Length, remoteEndPoint);

            return true;
        }
    }
}
