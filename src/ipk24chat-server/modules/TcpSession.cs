/******************************************************************************
 *  IPK-2024-2
 *  TcpSession.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    Implementation of TCP user session
 *  Last change:    18.04.23
 *****************************************************************************/

using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Net;
using System.Net.Sockets;

namespace ipk24chat_server.modules
{
    public class TcpSession
    {
        private readonly TcpClient _client;         // TcpClient which is used for sending/receiving messages to/from the user
        private readonly NetworkStream _stream;     // NetworkStream which is used for writing/reading data from the stream
        private readonly IPAddress _ip;             // IP address of the user
        private readonly ushort _port;              // Port of the user

        private bool _openState;                    // Indicates if the user is authorized or not

        private string _username = null!;           // Username of the user
        private string _displayName = null!;        // DisplayName of the user

        private readonly TaskCompletionSource _finishReceiving = new();         // This task is used for stopping the receiver thread
        private readonly TaskCompletionSource _endSession = new();              // This task is used for normal session ending. When client is gone (BYE received), this task must be finished
        private readonly TaskCompletionSource<Exception> _sessionError = new(); // This task is used for error session ending. When some inner error appears, this task must be finished

        private ChannelUser _channelUser = null!;   // An object representing the user on channel
        private Channel _currentChannel = null!;    // Channel the user is currently on

        // TCP user session initializer
        public TcpSession(TcpClient tcpClient)
        {
            _client = tcpClient;
            _stream = tcpClient.GetStream();
            _ip = (_client.Client.RemoteEndPoint as IPEndPoint)!.Address;
            _port = (ushort)(_client.Client.RemoteEndPoint as IPEndPoint)!.Port;
        }

        // Starting the TCP user session
        public void StartSession()
        {
            // Starts the session thread and prints a log message about it
            ThreadPool.QueueUserWorkItem(SessionBehaviour);
            Console.WriteLine($"LOG | New TCP user session with {_ip.ToString()}:{_port} has been started");
        }

        // Implements the behaviour of the client
        private void SessionBehaviour(Object? state)
        {
            try
            {
                // Staring a receiver thread
                ThreadPool.QueueUserWorkItem(MessageReceiver);

                var end = _endSession.Task;
                var error = _sessionError.Task;
                
                // Waits until some end task is finished
                var result = Task.WhenAny(end, error).Result;
                
                // If user was authorized, he must be logged out and left from the current channel
                if (_openState && WelcomeSession.LoggedUsers.ContainsKey(_username))
                {
                    WelcomeSession.LoggedUsers[_username] = false;
                    _currentChannel.LeaveUser(_username).Wait();
                }

                // Trying to send BYE message
                SendByeMessage();
                
                // Stopping the receiver thread
                _finishReceiving.SetResult();

                // If the session was stopped due to error, throw this error
                if (result == error)
                    throw error.Result;
                
            }
            catch (Exception ex)
            {
                // Prints LOG message with error message
                Console.WriteLine($"LOG | Problem with TCP user session {_ip.ToString()}:{_port}: {ex.Message}");
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
            Console.WriteLine($"LOG | TCP user session {_ip.ToString()}:{_port} has been closed");
        }

        // This function implements logic for receiver thread
        private void MessageReceiver(Object? state)
        {
            // It listens to new message all the time
            while (true)
            {
                try
                {
                    var finish = _finishReceiving.Task; // Finish receiving task
                    var buffer = new byte[1024];
                    var receive = _stream.ReadAsync(buffer, 0, buffer.Length); // Receiving task

                    // Receiver will wait for one of specified tasks
                    var complete = Task.WhenAny(finish, receive).Result;

                    // If the complete task is _finishReceiving, receiving will be finished
                    if (complete == finish)
                        return;

                    // Another case means that some message was received from the user

                    // Creating byte array with received message data
                    var count = receive.Result;
                    var data = new byte[count];
                    for (var i = 0; i < count; i++)
                        data[i] = buffer[i];

                    // Translate this array into Message object
                    var message = Message.ConvertDataToMessage(data, Message.ProtocolType.Tcp);
                    
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

        // Processes received message
        private void ProcessMessage(Message? message)
        {
            // Null message means that it was impossible to decode it due to invalid format, so error message must be sent and session must be closed
            if (message == null)
            {
                SendErrorMessage("Invalid message format, session will be closed");
                _endSession.SetResult();
                return;
            }

            // Log received message
            Logging.LogMessage(_ip, _port, true, message);

            // In case of ERROR or BYE message, session must be normally closed
            if (message.TypeOfMessage == Message.MessageType.Err || message.TypeOfMessage == Message.MessageType.Bye)
            {
                _endSession.SetResult();
                return;
            }

            // If the user was not authorized, authorization behaviour is started
            if (!_openState)
            {
                // If the type of message is not AUTH, error message will be sent and the session will be stopped
                if (message.TypeOfMessage != Message.MessageType.Auth)
                {
                    SendErrorMessage("Authentication message was required, session will be terminated!");
                    _endSession.SetResult();
                    return;
                }

                // In other case, username and display name of the user will be stored
                _username = message.Fields.Username!;
                _displayName = message.Fields.DisplayName!;

                // If there is a user with the same username on the server, send negative reply
                if (WelcomeSession.LoggedUsers.TryGetValue(_username, out var value) && value)
                {
                    SendReplyMessage("User with that username is already on the server", false);
                    return;
                }

                // Add the user to the list of authorized ones
                if (!WelcomeSession.LoggedUsers.TryAdd(_username, true))
                    WelcomeSession.LoggedUsers[_username] = true;

                // Connect user to the default channel
                _channelUser = new ChannelUser(_displayName, null, this);
                _currentChannel = Channel.Channels["general"];
                _currentChannel.NewUser(_username, _channelUser).Wait();

                // Send positive reply message
                SendReplyMessage("Authentication is successful. Welcome to the server!", true);
                
                _openState = true;
                return;
            }

            // In open state, behaviour will depends on the type of message
            switch (message)
            {
                // In case of JOIN message...
                case { TypeOfMessage: Message.MessageType.Join }:
                    // Check if user has changed his display name
                    if (message.Fields.DisplayName != _displayName)
                    {
                        _displayName = message.Fields.DisplayName!;
                        _channelUser.UpdateDisplayName(_displayName);
                    }

                    // Check if user does not try to connect to the current channel
                    var channelId = message.Fields.ChannelId!;
                    if (channelId == _currentChannel.ChannelId)
                    {
                        SendReplyMessage("You are already on this channel", false);
                        break;
                    }
                    
                    // Disconnect user from the current channel
                    _currentChannel.LeaveUser(_username).Wait();
                    
                    // Check is channel with specified ID exists
                    Channel.Channels.TryGetValue(channelId, out var newChannel);
                    
                    // If channel does not exist, create one
                    newChannel ??= new Channel(channelId, false);
                    
                    // Connect user to new channel
                    _currentChannel = newChannel;
                    _currentChannel.NewUser(_username, _channelUser).Wait();
                    
                    // Send positive reply message
                    SendReplyMessage($"You has successfully joined \"{channelId}\" channel", true);
                    break;
                
                // In case of MSG message...
                case { TypeOfMessage: Message.MessageType.Msg }:
                    // Check if user has changed his display name
                    if (message.Fields.DisplayName != _displayName)
                    {
                        _displayName = message.Fields.DisplayName!;
                        _currentChannel.UpdateUserName(_username, _displayName).Wait();
                    }
                    
                    // Send the message to all users on the current channel
                    _currentChannel.NewMessage((MsgMessage)message, _username).Wait();
                    break;
                
                // Any other type of message are not expected, so they indicate error
                default:
                    SendErrorMessage("Unexpected type of message, session will be terminated");
                    _endSession.SetResult();
                    return;
            }
        }

        // Sends ERR message to the user
        private void SendErrorMessage(string message)
        {
            var fields = new Message.MessageFields
            {
                DisplayName = "Server",
                MessageContent = message
            };
            var errMessage = new ErrMessage();
            errMessage.EncodeMessage(fields, Message.ProtocolType.Tcp);
            SendMessage(errMessage).Wait();
        }

        // Send BYE message to the user
        private void SendByeMessage()
        {
            var byeMessage = new ByeMessage();
            byeMessage.EncodeMessage(new Message.MessageFields(), Message.ProtocolType.Tcp);
            SendMessage(byeMessage).Wait();
        }

        // Sends reply message to the user
        private void SendReplyMessage(string message, bool result)
        {
            var fields = new Message.MessageFields
            {
                Result = result,
                MessageContent = message
            };
            var replyMessage = new ReplyMessage();
            replyMessage.EncodeMessage(fields, Message.ProtocolType.Tcp);
            SendMessage(replyMessage).Wait();
        }

        // Send message to the user
        public async Task SendMessage(Message message)
        {
            try
            {
                await _stream.WriteAsync(message.Data);
                _stream.Flush();
                Logging.LogMessage(_ip, _port, false, message);
            }
            catch (Exception ex)
            {
                _sessionError.SetResult(ex);
            }
        }
    }
}
