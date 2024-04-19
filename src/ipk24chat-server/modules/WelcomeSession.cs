/******************************************************************************
 *  IPK-2024-2
 *  WelcomeSession.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    Implementation of welcome session (both for TCP and UDP)
 *  Last change:    18.04.23
 *****************************************************************************/

using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ipk24chat_server.modules
{
    public class WelcomeSession
    {
        public static readonly ConcurrentDictionary<string, bool> LoggedUsers = new();  // Dictionary with all authorized users

        private TcpListener _tcpListener = null!;   // Welcome TCP listener
        private UdpClient _udpClient = null!;       // Welcome UDP listener

        public readonly SemaphoreSlim FinishSemaphore = new(0, 1); // Semaphore which is used for stopping server

        // THis method starts the Welcome Session
        public void StartWelcomeSession()
        {
            // Try to create TCP listener and start him
            try
            {
                _tcpListener = new TcpListener(Cla.ListeningIp, Cla.ListeningPort);
                _tcpListener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Creating TCP welcome listener failed: {ex.Message}");
                return;
            }

            // Try to create UDP listener
            try
            {
                _udpClient = new UdpClient(Cla.ListeningPort, AddressFamily.InterNetwork);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Creating UDP welcome client failed: {ex.Message}");
                _tcpListener.Stop();
                return;
            }

            // Start two threads: for TCP listener and UDP listener
            ThreadPool.QueueUserWorkItem(ListenForTcpClients);
            ThreadPool.QueueUserWorkItem(ListenForUdpClients);

            // Waiting for exiting the application
            FinishSemaphore.Wait();
        }

        // TCP listener
        private void ListenForTcpClients(object? state)
        {
            Console.WriteLine("TCP welcome listener is started...");
            
            try
            {
                while (true)
                {
                    // When new TCP user tries to connect the server, create a new TcpClient for him and start a new TCP session
                    var tcpClient = _tcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(CreateNewTcpSession, tcpClient);
                }
            }
            catch (Exception ex)
            {
                // In case of any errors, print error message and stop the server
                Console.WriteLine($"Welcome TCP listener error: {ex.Message}");
                FinishSemaphore.Release();
            }
        }

        // UDP listener
        private void ListenForUdpClients(object? state) 
        {
            Console.WriteLine("UDP welcome listener is started...");
            try
            {
                while (true)
                {
                    // Try to get a message to welcome port
                    IPEndPoint? remoteEndPoint = null;
                    var receivedData = _udpClient.Receive(ref remoteEndPoint);
                    
                    // Try to confirm the first message (and translate it to an Message object at the same time)
                    var firstMessage = ConfirmFirstUdpMessage(receivedData, remoteEndPoint);
                    
                    // If firstMessage is null, it means that it is impossible to decode it (or it is not AUTH message)
                    // In both cases, message will be ignored
                    if (firstMessage == null)
                        continue;
                    
                    // Create new UDP session and start it
                    var newUdpSession = new UdpSession(remoteEndPoint);
                    newUdpSession.StartSession(firstMessage);
                }
            }
            catch (Exception ex)
            {
                // In case of any errors, print error message and stop the server
                Console.WriteLine($"Welcome UDP listener error: {ex.Message}");
                FinishSemaphore.Release();
            }
        }

        // This methods created a new TCP session
        private void CreateNewTcpSession(Object? stateInfo)
        {
            // Get the TclClient and RemoteEndPoint
            var tcpClient = (TcpClient)stateInfo!;
            var remoteEndPoint = (tcpClient.Client.RemoteEndPoint as IPEndPoint)!;

            try
            {
                // Create new TCP session and start it
                var tcpSession = new TcpSession(tcpClient);
                tcpSession.StartSession();
            }
            catch (Exception ex)
            {
                // In case of any problem, log it
                Console.WriteLine($"Problem with TCP user session {remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port}: {ex.Message}");
            }
        }

        // This methods tries do decode the first message from the user, checks if it is an AUTH message, and send CONFIRM message to it
        private Message? ConfirmFirstUdpMessage(byte[] data, IPEndPoint remoteEndPoint)
        {
            // Convert message data to a Message object
            var receivedMessage = Message.ConvertDataToMessage(data, Message.ProtocolType.Udp);

            // If message has an invalid format, return null
            if (receivedMessage == null)
                return null;
            
            // Log the received message
            Logging.LogMessage(remoteEndPoint.Address, (ushort)remoteEndPoint.Port, true, receivedMessage);
            
            // If it is not AUTH message, return null
            if (receivedMessage is not { TypeOfMessage: Message.MessageType.Auth })
                return null;

            // Encode confirmation message
            var fields = new Message.MessageFields
            {
                MessageRefId = receivedMessage.Fields.MessageId
            };
            var confMessage = new ConfirmMessage();
            confMessage.EncodeMessage(fields, Message.ProtocolType.Udp);

            // Try to send a confirmation message and return the message to UDP Welcome Receiver
            try
            {
                _udpClient.Send(confMessage.Data, confMessage.Data.Length, remoteEndPoint);
                Logging.LogMessage(remoteEndPoint.Address, (ushort)remoteEndPoint.Port, false, confMessage);
                return receivedMessage;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to sent confirmation message to {remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port}: {e.Message}");
                return null;
            }
        }
    }
}
