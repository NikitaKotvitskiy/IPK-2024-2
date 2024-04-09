using ipk24chat_server.inner;
using System.Text;

namespace ipk24chat_server.messages
{
    public abstract class Message
    {
        public enum ProtocolType { TCP, UDP }
        public enum MessageType { ERR, CONFIRM, REPLY, AUTH, JOIN, MSG, BYE }
        public struct MessageFields
        {
            public ushort? MessageId;
            public ushort? MessageRefId;
            public string? Username;
            public string? ChannelId;
            public string? DisplayName;
            public string? Secret;
            public string? MessageContent;
            public bool? Result;
        }

        public ProtocolType Protocol { get; protected set; }
        public MessageType TypeOfMessage { get; protected set; }
        public MessageFields Fields { get; protected set; } = new MessageFields();

        public byte[] Data { get; protected set; } = null!;

        public abstract void EncodeMessage(MessageFields fields, ProtocolType protocol);
        public abstract void DecodeMessage(byte[] data, ProtocolType protocol);

        protected void EncodeTcpMessageStringToByteArr(string message)
        {
            var messageArr = Encoding.ASCII.GetBytes(message);
            Data = new byte[messageArr.Length + 2];
            Array.Copy(messageArr, Data, messageArr.Length);
            Data[Data.Length - 2] = (byte)'\r';
            Data[Data.Length - 1] = (byte)'\n';
        }

        protected void SetMessageContentTcp(string[] words, int startIndex)
        {
            var messageContent = string.Empty;
            for (var i = startIndex; i < words.Length - 1; i++)
                messageContent += words[i] + " ";
            messageContent += words[words.Length - 1];
            // TODO: format checking

            var fields = Fields;
            fields.MessageContent = messageContent;
            Fields = fields;
        }

        protected void SetDisplayNameTcp(string displayName)
        {
            // TODO: format checking
            var fields = Fields;
            fields.DisplayName = displayName;
            Fields = fields;
        }

        protected void SetChannelIdTcp(string channelId)
        {
            // TODO: format checking
            var fields = Fields;
            fields.ChannelId = channelId;
            Fields = fields;
        }

        protected void SetUsernameTcp(string username)
        {
            // TODO: format checking
            var fields = Fields;
            fields.Username = username;
            Fields = fields;
        }

        protected void SetSecretTcp(string secret)
        {
            // TODO: format checking
            var fields = Fields;
            fields.Secret = secret;
            Fields = fields;
        }

        private string DecodeUdpString(ref int index)
        {
            var startIndex = index;
            var count = 0;
            while (Data[index++] != 0x00)
                count++;
            return Encoding.ASCII.GetString(Data, startIndex, count);
        }

        protected void SetMessageId(ref int index)
        {
            var mesIdSection = new byte[2];
            mesIdSection[1] = Data[index++];
            mesIdSection[0] = Data[index++];
            var mesId = BitConverter.ToUInt16(mesIdSection);

            var fields = Fields;
            fields.MessageId = mesId;
            Fields = fields;
        }

        protected void SetReferenceMessageId(ref int index)
        {
            var refMesIdSection = new byte[2];
            refMesIdSection[1] = Data[index++];
            refMesIdSection[0] = Data[index++];
            var refMesId = BitConverter.ToUInt16(refMesIdSection);

            var fields = Fields;
            fields.MessageRefId = refMesId;
            Fields = fields;
        }

        protected void SetDisplayNameUdp(ref int index)
        {
            var displayNameString = DecodeUdpString(ref index);
            // TODO: format checking
            var fields = Fields;
            fields.DisplayName = displayNameString;
            Fields = fields;
        }

        protected void SetMessageContentUdp(ref int index)
        {
            var messageContent = DecodeUdpString(ref index);
            // TODO: format checking
            var fields = Fields;
            fields.MessageContent = messageContent;
            Fields = fields;
        } 

        protected void SetChannelIdUdp(ref int index)
        {
            var channelIdString = DecodeUdpString(ref index);
            // TODO: format checking
            var fields = Fields;
            fields.ChannelId = channelIdString;
            Fields = fields;
        }

        protected void SetUsernameUdp(ref int index)
        {
            var usernameString = DecodeUdpString(ref index);
            // TODO: format checking
            var fields = Fields;
            fields.Username = usernameString;
            Fields = fields;
        }

        protected void SetSecretUdp(ref int index)
        {
            var secretString = DecodeUdpString(ref index);
            // TODO: format checking
            var fields = Fields;
            fields.Secret = secretString;
            Fields = fields;
        }
    }
}
