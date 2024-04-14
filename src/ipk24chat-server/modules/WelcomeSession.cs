using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;

namespace ipk24chat_server.modules
{
    public class WelcomeSession
    {
        public static ConcurrentDictionary<string, bool> loggedUsers = new ConcurrentDictionary<string, bool>();

        private TcpListener tcpListener = null!;
        private UdpClient udpClient = null!;

        public SemaphoreSlim FinishSemaphore = new SemaphoreSlim(0, 1);

        public void StartWelcomeSession()
        {
            try
            {
                tcpListener = new TcpListener(Cla.ListeningIp, Cla.ListeningPort);
                tcpListener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Creating TCP welcome listener failed: {ex.Message}");
                return;
            }

            try
            {
                udpClient = new UdpClient(Cla.ListeningPort, AddressFamily.InterNetwork);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Creating UDP welcome client failed: {ex.Message}");
                tcpListener.Stop();
                return;
            }

            ThreadPool.QueueUserWorkItem(ListenForTcpClients);
            ThreadPool.QueueUserWorkItem(ListenForUdpClients);

            FinishSemaphore.Wait();
        }

        private void ListenForTcpClients(object? state)
        {
            Console.WriteLine("TCP welcome listener is started...");

            try
            {
                while (true)
                {
                    var tcpClient = tcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(CreateNewTcpSession, tcpClient);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Welcome TCP listener error: {ex.Message}");
                FinishSemaphore.Release();
            }
        }

        private void ListenForUdpClients(object? state) 
        {
            Console.WriteLine("UDP welcome listener is started...");
            try
            {
                while (true)
                {
                    IPEndPoint? remoteEndPoint = null;
                    var receivedData = udpClient.Receive(ref remoteEndPoint);
                    var firstMessage = ConfirmFirstUdpMessage(receivedData, remoteEndPoint);
                    if (firstMessage == null)
                        continue;
                    ThreadPool.QueueUserWorkItem(CreateNewUdpSession, (remoteEndPoint, firstMessage));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Welcome UDP listener error: {ex.Message}");
                FinishSemaphore.Release();
            }

        }

        private void CreateNewTcpSession(Object? stateInfo)
        {
            var tcpClient = (TcpClient)stateInfo!;
            var session = new TcpSession();
            var remoteEndPoint = (tcpClient.Client.RemoteEndPoint as IPEndPoint)!;

            try
            {
                session.StartSession(tcpClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Problem with TCP user session {remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port}: {ex.Message}");
            }
            finally
            {
                session.StopSession();
                Console.WriteLine($"TCP user session {remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port} has been closed");
            }
        }

        private void CreateNewUdpSession(Object? stateInfo)
        {
            (IPEndPoint remoteEndPoint, Message firstMessage) = ((IPEndPoint, Message))stateInfo!;
            var session = new UdpSession();

            try
            {
                session.StartSession(remoteEndPoint, firstMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Problem with UDP user session {remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port}: {ex.Message}");
            }
            finally
            {
                session.StopSession();
                Console.WriteLine($"UDP user session {remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port} has been closed");
            }
        }


        private void GenerateUdpErrorMessage(string messageContent, IPEndPoint remoteEndPoint)
        {
            var fields = new Message.MessageFields();
            fields.MessageId = 0;
            fields.DisplayName = "Server";
            fields.MessageContent = messageContent;

            var errMessage = new ErrMessage();
            errMessage.EncodeMessage(fields, Message.ProtocolType.UDP);

            try
            {
                udpClient.Send(errMessage.Data, errMessage.Data.Length, remoteEndPoint);
            }
            catch (Exception e) 
            {
                Console.WriteLine($"Error! Failed to send error message to {remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port}: {e.Message}");
                return;
            }

            Logging.LogMessage(remoteEndPoint.Address, (ushort)remoteEndPoint.Port, true, errMessage);
        }

        private Message? ConfirmFirstUdpMessage(byte[] data, IPEndPoint remoteEndPoint)
        {
            var recvMessage = Message.ConvertDataToMessage(data, Message.ProtocolType.UDP);
            if (recvMessage == null)
            {
                GenerateUdpErrorMessage("Invalid message format!", remoteEndPoint);
                return null;
            }
            if (recvMessage.TypeOfMessage == Message.MessageType.CONFIRM)
                return null;

            Logging.LogMessage(remoteEndPoint.Address, (ushort)remoteEndPoint.Port, true, recvMessage);

            var fields = new Message.MessageFields();
            fields.MessageRefId = recvMessage.Fields.MessageId;
            var confMessage = new ConfirmMessage();
            confMessage.EncodeMessage(fields, Message.ProtocolType.UDP);

            try
            {
                udpClient.Send(confMessage.Data, confMessage.Data.Length, remoteEndPoint);
                Logging.LogMessage(remoteEndPoint.Address, (ushort)remoteEndPoint.Port, false, confMessage);
                return recvMessage;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to sent confirmation message to {remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port}: {e.Message}");
                return null;
            }
        }
    }
}
