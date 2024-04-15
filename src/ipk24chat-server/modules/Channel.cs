using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Collections.Concurrent;

namespace ipk24chat_server.modules
{
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

    public class Channel
    {
        public static ConcurrentDictionary<string, Channel> Channels = new ConcurrentDictionary<string, Channel>();

        public string ChannelId { get; private set; } = null!;
        private bool _instant;
        private readonly ConcurrentDictionary<string, ChannelUser> channelUsers = new ConcurrentDictionary<string, ChannelUser>();

        public Channel(string channelId, bool instant)
        {
            ChannelId = channelId;
            _instant = instant;

            Channels.TryAdd(channelId, this);
        }

        public async Task NewUser(string username, ChannelUser user)
        {
            channelUsers.TryAdd(username, user);

            var fields = new Message.MessageFields();
            fields.DisplayName = "Server";
            fields.MessageContent = $"{user.DisplayName} has joined the channel";
            var msg = new MsgMessage();
            msg.EncodeMessage(fields, Message.ProtocolType.TCP);

            await NewMessage(msg, username);
        }

        public async Task LeaveUser(string username)
        {
            ChannelUser user;
            channelUsers.TryRemove(username, out user!);

            if (!_instant && channelUsers.Count == 0)
            {
                Channels.TryRemove(ChannelId, out var channel);
                return;
            }

            var fields = new Message.MessageFields();
            fields.DisplayName = "Server";
            fields.MessageContent = $"{user.DisplayName} has left the channel";
            var msg = new MsgMessage();
            msg.EncodeMessage(fields, Message.ProtocolType.TCP);

            await NewMessage(msg, username);
        }

        public async Task UpdateUserName(string username, string newDisplayName)
        {
            ChannelUser user;
            channelUsers.TryGetValue(username, out user!);

            var fields = new Message.MessageFields();
            fields.DisplayName = "Server";
            fields.MessageContent = $"{user.DisplayName} has changed his display name to {newDisplayName}";
            var msg = new MsgMessage();
            msg.EncodeMessage(fields, Message.ProtocolType.TCP);

            user.UpdateDisplayName(newDisplayName);
            await NewMessage(msg, username);
        }

        public async Task NewMessage(MsgMessage message, string username)
        {
            foreach (var user in channelUsers)
                if (user.Key != username)
                {
                    if (user.Value.UserUdpSession != null)
                    {
                        var fields = message.Fields;
                        lock (user.Value.UserUdpSession.IdLock) fields.MessageId = user.Value.UserUdpSession.GetNewId();
                        message.EncodeMessage(fields, Message.ProtocolType.UDP);
                        await user.Value.UserUdpSession.SendMessage(message);
                    }
                    else if (user.Value.UserTcpSession != null)
                    {
                        message.EncodeMessage(message.Fields, Message.ProtocolType.TCP);
                        await user.Value.UserTcpSession.SendMessage(message);
                    }
                }
        }
    }
}
