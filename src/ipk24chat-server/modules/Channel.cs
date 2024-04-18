/******************************************************************************
 *  IPK-2024-2
 *  Channel.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    Implementation of server channels with all its functionality
 *  Last change:    15.04.23
 *****************************************************************************/

using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Collections.Concurrent;

namespace ipk24chat_server.modules
{
    // This class represents a user on the channel
    public class ChannelUser
    {
        public string DisplayName { get; private set; }
        public UdpSession? UserUdpSession { get; private set; }
        public TcpSession? UserTcpSession { get; private set; }

        public ChannelUser(string displayName, UdpSession? userUdpSession = null, TcpSession? userTcpSession = null)
        {
            if (userUdpSession == null && userTcpSession == null || userUdpSession != null && userTcpSession != null)
                throw new ProgramException("Invalud user sessions parameters", "");

            DisplayName = displayName;
            UserUdpSession = userUdpSession;
            UserTcpSession = userTcpSession;
        }

        public void UpdateDisplayName(string displayName) => DisplayName = displayName;
    }

    // This class represents channel
    public class Channel
    {
        public static readonly ConcurrentDictionary<string, Channel> Channels = new ConcurrentDictionary<string, Channel>();
        
        public string ChannelId { get; }    // ChannelID which is used by users to join the channel
        private readonly bool _instant;     // This attribute says if the channel must be present during the whole server lifetime or it will be disposed when nobody uses it
        private readonly ConcurrentDictionary<string, ChannelUser> _channelUsers = new();   // Stores the data of all users on the server in format [username: data]

        public Channel(string channelId, bool instant)
        {
            ChannelId = channelId;
            _instant = instant;

            Channels.TryAdd(channelId, this);
        }

        // Adds new user to the channel and sends a message about new user to all other users
        public async Task NewUser(string username, ChannelUser user)
        {
            _channelUsers.TryAdd(username, user);

            var fields = new Message.MessageFields
            {
                DisplayName = "Server",
                MessageContent = $"{user.DisplayName} has joined the channel"
            };
            var msg = new MsgMessage();
            msg.EncodeMessage(fields, Message.ProtocolType.Tcp);

            await NewMessage(msg, username);
        }

        // Remove the user from the channel and sends a massage about that to all other users
        public async Task LeaveUser(string username)
        {
            // Remove user from the channel
            _channelUsers.TryRemove(username, out var user);

            // If the channel is not instant, check is someone is still on the channel
            // If channel is empty, delete it from the list of channels (and garbage collector then will eliminate it)
            if (!_instant && _channelUsers.Count == 0)
            {
                Channels.TryRemove(ChannelId, out var _);
                return;
            }

            // Send the message about user leaving to all remaining users
            var fields = new Message.MessageFields
            {
                DisplayName = "Server",
                MessageContent = $"{user?.DisplayName} has left the channel"
            };
            var msg = new MsgMessage();
            msg.EncodeMessage(fields, Message.ProtocolType.Tcp);

            await NewMessage(msg, username);
        }

        // Updates DisplayName of the specified user
        public async Task UpdateUserName(string username, string newDisplayName)
        {
            _channelUsers.TryGetValue(username, out var user);

            var fields = new Message.MessageFields
            {
                DisplayName = "Server",
                MessageContent = $"{user?.DisplayName} has changed his display name to {newDisplayName}"
            };
            var msg = new MsgMessage();
            msg.EncodeMessage(fields, Message.ProtocolType.Tcp);

            user?.UpdateDisplayName(newDisplayName);
            await NewMessage(msg, username);
        }

        // Sends message to all users on channel except a user with specified username
        public async Task NewMessage(MsgMessage message, string username)
        {
            foreach (var user in _channelUsers)
                if (user.Key != username)
                {
                    if (user.Value.UserUdpSession != null)
                    {
                        var fields = message.Fields;
                        lock (user.Value.UserUdpSession.IdLock) fields.MessageId = user.Value.UserUdpSession.GetNewId();
                        message.EncodeMessage(fields, Message.ProtocolType.Udp);
                        await user.Value.UserUdpSession.SendMessage(message);
                    }
                    else if (user.Value.UserTcpSession != null)
                    {
                        message.EncodeMessage(message.Fields, Message.ProtocolType.Tcp);
                        await user.Value.UserTcpSession.SendMessage(message);
                    }
                }
        }
    }
}
