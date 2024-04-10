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

            if (!FormatChecking.CheckMessageContent(messageContent))
                throw new ProtocolException("Invalid format of TCP message content field detected", "max 1400 VCHAR symbols and spaces", messageContent);

            var fields = Fields;
            fields.MessageContent = messageContent;
            Fields = fields;
        }

        protected void SetDisplayNameTcp(string displayName)
        {
            if (!FormatChecking.CheckDisplayName(displayName))
                throw new ProtocolException("Invalid format of TCP display name field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", displayName);

            var fields = Fields;
            fields.DisplayName = displayName;
            Fields = fields;
        }

        protected void SetChannelIdTcp(string channelId)
        {
            if (!FormatChecking.CheckChannelId(channelId))
                throw new ProtocolException("Invalid format of TCP channel ID field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", channelId);

            var fields = Fields;
            fields.ChannelId = channelId;
            Fields = fields;
        }

        protected void SetUsernameTcp(string username)
        {
            if (!FormatChecking.CheckUserName(username))
                throw new ProtocolException("Invalid format of TCP username field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", username);

            var fields = Fields;
            fields.Username = username;
            Fields = fields;
        }

        protected void SetSecretTcp(string secret)
        {
            if (!FormatChecking.CheckSecret(secret))
                throw new ProtocolException("Invalid format of TCP secret field detected", "max 128 ALPHA, DIGIT, '-' and '.' symbols", secret);

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
            Array.Copy(Data, index, mesIdSection, 0, 2);
            index += 2;
            var mesId = BitConverter.ToUInt16(mesIdSection);

            var fields = Fields;
            fields.MessageId = mesId;
            Fields = fields;
        }

        protected void SetReferenceMessageId(ref int index)
        {
            var refMesIdSection = new byte[2];
            Array.Copy(Data, index, refMesIdSection, 0, 2);
            index += 2;
            var refMesId = BitConverter.ToUInt16(refMesIdSection);

            var fields = Fields;
            fields.MessageRefId = refMesId;
            Fields = fields;
        }

        protected void SetDisplayNameUdp(ref int index)
        {
            var displayNameString = DecodeUdpString(ref index);

            if (!FormatChecking.CheckDisplayName(displayNameString))
                throw new ProtocolException("Invalid format of UDP display name field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", displayNameString);

            var fields = Fields;
            fields.DisplayName = displayNameString;
            Fields = fields;
        }

        protected void SetMessageContentUdp(ref int index)
        {
            var messageContent = DecodeUdpString(ref index);

            if (!FormatChecking.CheckMessageContent(messageContent))
                throw new ProtocolException("Invalid format of UDP message content field detected", "max 1400 VCHAR symbols and spaces", messageContent);

            var fields = Fields;
            fields.MessageContent = messageContent;
            Fields = fields;
        } 

        protected void SetChannelIdUdp(ref int index)
        {
            var channelIdString = DecodeUdpString(ref index);

            if (!FormatChecking.CheckChannelId(channelIdString))
                throw new ProtocolException("Invalid format of UDP channel ID field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", channelIdString);

            var fields = Fields;
            fields.ChannelId = channelIdString;
            Fields = fields;
        }

        protected void SetUsernameUdp(ref int index)
        {
            var usernameString = DecodeUdpString(ref index);

            if (!FormatChecking.CheckUserName(usernameString))
                throw new ProtocolException("Invalid format of UDP username field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", usernameString);

            var fields = Fields;
            fields.Username = usernameString;
            Fields = fields;
        }

        protected void SetSecretUdp(ref int index)
        {
            var secretString = DecodeUdpString(ref index);

            if (!FormatChecking.CheckSecret(secretString))
                throw new ProtocolException("Invalid format of UDP secret field detected", "max 128 ALPHA, DIGIT, '-' and '.' symbols", secretString);

            var fields = Fields;
            fields.Secret = secretString;
            Fields = fields;
        }

        public static MessageType DefineTypeOfMessage(byte[] data, ProtocolType protocol)
        {
            if (protocol == ProtocolType.UDP)
            {
                var typeByte = (byte)data[0];
                switch (typeByte)
                {
                    case 0x00:
                        return MessageType.CONFIRM;
                    case 0x01:
                        return MessageType.REPLY;
                    case 0x02:
                        return MessageType.AUTH;
                    case 0x03:
                        return MessageType.JOIN;
                    case 0x04:
                        return MessageType.MSG;
                    case 0xFE:
                        return MessageType.ERR;
                    case 0xFF:
                        return MessageType.BYE;
                    default:
                        throw new ProtocolException("Invalid UDP message type detected", "[0x00-0x04 | 0xFE-0xFF]", $"0x{typeByte:X2}");
                }
            }
            else
            {
                var messageString = Encoding.ASCII.GetString(data);
                var typeWord = messageString.Split(' ')[0];
                switch (typeWord)
                {
                    case "ERR":
                        return MessageType.ERR;
                    case "REPLY":
                        return MessageType.REPLY;
                    case "AUTH":
                        return MessageType.AUTH;
                    case "JOIN":
                        return MessageType.JOIN;
                    case "MSG":
                        return MessageType.MSG;
                    case "BYE":
                        return MessageType.BYE;
                    default:
                        throw new ProtocolException("Invalid TCP message type detected", "[ERR|REPLY|AUTH|JOIN|MSG|BYE]", $"{typeWord}");
                }
            }
        }
    }
}
