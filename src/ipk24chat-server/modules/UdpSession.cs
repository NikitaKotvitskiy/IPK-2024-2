/******************************************************************************
 *  IPK-2024-2
 *  UdpSession.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    Implementation of UDP user session
 *  Last change:    18.04.23
 *****************************************************************************/

using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ipk24chat_server.modules
{
    public class UdpSession
    {
        private readonly UdpClient _client;     // UdpClient for message sending/receiving to/from the user
        private readonly IPAddress _ip;         // IP address of the user
        private readonly ushort _port;          // Port of the user
        
        public readonly object IdLock = new();          // This lock is used for safe using of MessageIdGenerator from multiple threads
        private ushort _lastMessageId;                  // MessageID counter
        public ushort GetNewId() => _lastMessageId++;   // Generates new MessageID
        
        private readonly HashSet<ushort> _userMessageIds = [];  // This HashSet is used for storing all MessageIDs received from user 

        // This is the main confirmation mechanism for UdpSession
        // The logic is the following:
        // When server wants to send a message to user, it starts new task for it
        // This task sends message and created a semaphore which is stored in this dictionary with MessageID as a key
        // When receiver thread receive a confirmation message, it tries to find corresponding semaphore in this structure and release it
        // Releasing semaphore means that confirmation message was successfully received and sending message is complete
        private readonly ConcurrentDictionary<ushort, SemaphoreSlim> _confirmationSemaphores = new();

        private bool _openState; // Indicates, if user is authorized 

        private string _username = null!;       // Stores the username
        private string _displayName = null!;    // stores the display name

        private readonly TaskCompletionSource _finishReceiving = new();         // This task is used for stopping the receiver thread
        private readonly TaskCompletionSource _endSession = new();              // This task is used for normal session ending. When client is gone (BYE received), this task must be finished
        private readonly TaskCompletionSource<Exception> _sessionError = new(); //This task is used for error session ending. When some inner error appears, this task must be finished

        private ChannelUser _channelUser = null!;   // An object representing the user on channel
        private Channel _currentChannel = null!;    // Channel the user is currently on

        // Initializes the UdpSession
        public UdpSession(IPEndPoint remoteEndPoint)
        {
            _client = new UdpClient(0, AddressFamily.InterNetwork);
            _client.Connect(remoteEndPoint);
            _ip = remoteEndPoint.Address;
            _port = (ushort)remoteEndPoint.Port;
        }

        // Start the session
        // It takes the first that user sent to the server and send it forward to SessionBehaviour thread
        public void StartSession(Message firstMessage)
        {
            // Start the behaviour thread and print log message about new session
            ThreadPool.QueueUserWorkItem(SessionBehaviour, firstMessage);
            Console.WriteLine($"LOG | New UDP user session with {_ip.ToString()}:{_port} has been started");
        }

        // Logic for UdpSession behaviour thread
        private void SessionBehaviour(Object? state)
        {
            Message firstMessage = (Message)state!;
            
            // Start processing of the first message without need of confirmation (because the first messages are
            // confirmed by WelcomeSession)
            _ = Task.Run(() => ProcessMessage(firstMessage, false));

            try
            {
                // Start receiver thread
                ThreadPool.QueueUserWorkItem(MessageReceiver);
                
                var end = _endSession.Task;
                var error = _sessionError.Task;
                
                // Wait for some task is finished
                var result = Task.WhenAny(end, error).Result;
                
                // If the user was authorized, log him out and disconnect from the channel
                if (_openState && WelcomeSession.LoggedUsers.ContainsKey(_username))
                {
                    WelcomeSession.LoggedUsers[_username] = false;
                    _currentChannel.LeaveUser(_username).Wait();
                }

                // Send bye message
                SendByeMessage();
                
                // Stop receiving
                _finishReceiving.SetResult();

                // If session was stopped due to some inner error, throw the exception with this error
                if (result == error)
                    throw error.Result;
            }
            catch (Exception ex)
            {
                // Prints LOG message with error message
                Console.WriteLine($"LOG | Problem with UDP user session {_ip.ToString()}:{_port}: {ex.Message}");
            }
            finally
            {
                // Stops the session
                StopSession();
            }
        }

        // Safely stops the session
        private void StopSession()
        {
            // Closing the TcpClient
            _client.Close();
            
            // Printing log message about session closing
            Console.WriteLine($"LOG | UDP user session {_ip.ToString()}:{_port} has been closed");
        }

        // This method implements logic for receiver thread
        private void MessageReceiver(Object? state)
        {
            // Receiver listens to new messages from user all the time
            while (true)
            {
                var finish = _finishReceiving.Task;                     // Finish receiving task
                var receive = _client.ReceiveAsync();    // Receiving task 

                // Waiting until any of tasks is finished
                var completed = Task.WhenAny(finish, receive).Result;
                
                // Is finish task was complete, stop receiving and return
                if (completed == finish)
                    return;

                // In other case, try to decode message
                try
                {
                    var data = receive.Result.Buffer;
                    var message = Message.ConvertDataToMessage(data, Message.ProtocolType.Udp);
                    
                    // Start processing message thread
                    _ = Task.Run(() => ProcessMessage(message));
                }
                catch
                {
                    // In case of any error, finish receiving
                    
                    // ATTENTION
                    // It is potentially danger code
                    // The logic is the following:
                    // In some rare cases, receiving is continuing when the session is already closed
                    // TcpClient is closed at this moment and attempt to receive something causes error
                    // Using endSessionTask is not safe here, because (in theory) it already must be finished
                    // So here is return; only
                    
                    return;
                }
            }
        }

        // This method implements logic for message processing thread
        // needToConfirm parameter is true by default, because all messages except the first one (which is confirmed
        // by Welcome Session) must be confirmed
        private void ProcessMessage(Message? message, bool needToConfirm = true)
        {
            // If message is null, it means there was an invalid format of user message
            if (message == null)
            {
                SendErrorMessage("Invalid message format, session will be closed");
                _endSession.SetResult();
                return;
            }

            // If confirmation is required, log the received message
            // This action must be done here, before any processing
            if (needToConfirm)
                Logging.LogMessage(_ip, _port, true, message);

            // Is the confirmation message was received, try to release the corresponding semaphore and return
            if (message.TypeOfMessage == Message.MessageType.Confirm)
            {
                var refId = message.Fields.MessageRefId;
                if (_confirmationSemaphores.TryGetValue((ushort)refId!, out var sem))
                    sem.Release();
                return;
            }

            // If confirmation in needed, send confirmation message
            if (needToConfirm)
                SendConfirmMessage((ushort)message.Fields.MessageId!);
            
            // If this message was already received, processing is complete
            if (_userMessageIds.TryGetValue((ushort)message.Fields.MessageId!, out _))
                return;
            
            // Mark the MessageID of received message as confirmed
            _userMessageIds.Add((ushort)message.Fields.MessageId!);

            // If the type of message is ERR or BYE, end session
            if (message.TypeOfMessage == Message.MessageType.Err || message.TypeOfMessage == Message.MessageType.Bye)
            {
                _endSession.SetResult();
                return;
            }

            // REPLY message automatically means error, because user cannot send REPLY messages
            if (message.TypeOfMessage == Message.MessageType.Reply)
            {
                SendErrorMessage("Invalid type of message, session will be terminated");
                _endSession.SetResult();
                return;
            }

            // If user is not authorized, start authorization
            if (!_openState)
            {
                // If the received message is not AUTH message, send error message and finish session
                if (message.TypeOfMessage != Message.MessageType.Auth)
                {
                    SendErrorMessage("Authentication message was required, session will be terminated!");
                    _endSession.SetResult();
                    return;
                }
                
                // Setting username and display name from message
                _username = message.Fields.Username!;
                _displayName = message.Fields.DisplayName!;

                // If there is a user with the same username on the server, send negative REPLY
                if (WelcomeSession.LoggedUsers.ContainsKey(_username) && WelcomeSession.LoggedUsers[_username])
                {
                    SendReplyMessage((ushort)message.Fields.MessageId!, "User with that login is already on the server", false);
                    return;
                }

                // Add user to the list of authorized users
                if (!WelcomeSession.LoggedUsers.TryAdd(_username, true))
                    WelcomeSession.LoggedUsers[_username] = true;

                // Join user to the general channel
                _channelUser = new ChannelUser(_displayName, this);
                _currentChannel = Channel.Channels["general"];
                _currentChannel.NewUser(_username, _channelUser).Wait();

                // Send positive REPLY message
                SendReplyMessage((ushort)message.Fields.MessageId!, "Authentication is successful. Welcome to the server!", true);
                
                // User is now authorized
                _openState = true;
                return;
            }

            // Processing in open state depends on the type of message
            switch (message.TypeOfMessage)
            {
                // In case of JOIN message...
                case Message.MessageType.Join:
                    // Check if user has changed his display name
                    if (message.Fields.DisplayName != _displayName)
                    {
                        _displayName = message.Fields.DisplayName!;
                        _channelUser.UpdateDisplayName(_displayName);
                    }

                    // Get the ID of channel user wants to join
                    var channelId = message.Fields.ChannelId!;
                    
                    // If user already is on this channel, send negative REPLY message
                    if (channelId == _currentChannel.ChannelId)
                    {
                        SendReplyMessage((ushort)message.Fields.MessageId!, "You are already on this channel", false);
                        break;
                    }
                    
                    // Disconnect the user from current channel
                    _currentChannel.LeaveUser(_username).Wait();
                    
                    // Check if channel with required ID already exists
                    Channel.Channels.TryGetValue(channelId, out var newChannel);
                    
                    // If channel does not exist, create new channel
                    newChannel ??= new Channel(channelId, false);
                    
                    // Connect user to the channel
                    _currentChannel = newChannel;
                    _currentChannel.NewUser(_username, _channelUser).Wait();
                    
                    // Send positive REPLY message
                    SendReplyMessage((ushort)message.Fields.MessageId!, $"You has successfully joined \"{channelId}\" channel", true);
                    break;
                
                // In case of MSG message...
                case Message.MessageType.Msg:
                    // Check if user has changed his display name
                    if (message.Fields.DisplayName != _displayName)
                    {
                        _displayName = message.Fields.DisplayName!;
                        _currentChannel.UpdateUserName(_username, _displayName).Wait();
                    }
                    
                    // Send this message to the current channel
                    _currentChannel.NewMessage((MsgMessage)message, _username).Wait();
                    break;
                
                // All other types ar impossible (because they have already been processed)
                default:
                    SendErrorMessage("Unexpected type of message, session will be terminated");
                    _endSession.SetResult();
                    return;
            }
        }

        // Send ERR message to the user
        private void SendErrorMessage(string message)
        {
            var fields = new Message.MessageFields();
            lock (IdLock) fields.MessageId = GetNewId();
            fields.DisplayName = "Server";
            fields.MessageContent = message;
            var errMessage = new ErrMessage();
            errMessage.EncodeMessage(fields, Message.ProtocolType.Udp);
            SendMessage(errMessage).Wait();
        }

        // Send BYE message to the user
        private void SendByeMessage()
        {
            var fields = new Message.MessageFields();
            lock (IdLock) fields.MessageId = GetNewId();
            var byeMessage = new ByeMessage();
            byeMessage.EncodeMessage(fields, Message.ProtocolType.Udp);

            try
            {
                SendMessage(byeMessage).Wait();
            }
            catch
            {
                // ignored
            }
        }

        // Send CONFIRM message to the user
        private void SendConfirmMessage(ushort refId)
        {
            var fields = new Message.MessageFields
            {
                MessageRefId = refId
            };
            var confMessage = new ConfirmMessage();
            confMessage.EncodeMessage(fields, Message.ProtocolType.Udp);
            SendMessage(confMessage).Wait();
        }

        // Send REPLY message to the user
        private void SendReplyMessage(ushort refId, string message, bool result)
        {
            var fields = new Message.MessageFields();
            lock (IdLock) fields.MessageId = GetNewId();
            fields.MessageRefId = refId;
            fields.Result = result;
            fields.MessageContent = message;
            var replyMessage = new ReplyMessage();
            replyMessage.EncodeMessage(fields, Message.ProtocolType.Udp);
            SendMessage(replyMessage).Wait();
        }

        // This method implements message sending
        public async Task SendMessage(Message message)
        {
            // If the message type is CONFIRM or BYE, just send it to the server and return (confirmation for these messages is not necessary)
            if (message.TypeOfMessage is Message.MessageType.Confirm or Message.MessageType.Bye)
            {
                await _client.SendAsync(message.Data);
                Logging.LogMessage(_ip, _port, false, message);
                return;
            }

            // In other cases, message must be confirmed
            // Set the number of tries
            var tries = Cla.UdpMaxRetransmissions + 1;
            
            // Create confirmation semaphore
            var idToConfirm = message.Fields.MessageId;
            var sem = new SemaphoreSlim(1);
            _confirmationSemaphores.TryAdd((ushort)idToConfirm!, sem);

            // While tries left
            while (tries-- > 0)
            {
                // Send message
                await _client.SendAsync(message.Data);
                
                // Log the message
                Logging.LogMessage(_ip, _port, false, message);

                // Wait for confirmation semaphore for timeout time
                var result = await sem.WaitAsync(Cla.UdpTimeout);
                
                // If confirmation was received (confirmation semaphore is released by receiver), then return
                if (result)
                    return;
                
                // In other case, sending is failed, try again...
            }

            // If all tries failed, connection is lost
            _sessionError.SetResult(new ProtocolException("Confirmation message was not received", "", ""));
        }
    }
}
