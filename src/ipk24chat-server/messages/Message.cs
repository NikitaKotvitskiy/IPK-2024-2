/******************************************************************************
 *  IPK-2024-2
 *  Message.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    Abstract class with common attributes and methods for
 *                  all types of IPK24-CHAT messages
 *  Last change:    15.04.23
 *****************************************************************************/

using ipk24chat_server.inner;
using System.Text;

namespace ipk24chat_server.messages
{
    public abstract class Message
    {
        public enum ProtocolType { Tcp, Udp }
        public enum MessageType { Err, Confirm, Reply, Auth, Join, Msg, Bye }
        
        // This structure is used for storing all possible data of message
        // If some message does not contain some kind of data, it will be null
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
        public MessageFields Fields { get; protected set; }

        public byte[] Data { get; protected set; } = null!;
        
        // This function is used for encoding messages from the server
        // It takes prepared MessageField structure (with all data in it) and the type of protocol (TCP or UDP)
        public abstract void EncodeMessage(MessageFields fields, ProtocolType protocol);
        
        // This function is used for decoding input messages
        // It takes data array and the type of protocol (TCP or UDP)
        public abstract void DecodeMessage(byte[] data, ProtocolType protocol);

        // Translate ready TCP message string into byte array "Data"
        protected void EncodeTcpMessageStringToByteArr(string message)
        {
            var messageArr = Encoding.ASCII.GetBytes(message);
            Data = new byte[messageArr.Length + 2];
            Array.Copy(messageArr, Data, messageArr.Length);
            Data[^2] = (byte)'\r';
            Data[^1] = (byte)'\n';
        }

        // Takes string with MessageContent value and writes it in MessageFields structure
        protected void SetMessageContentTcp(string messageContent)
        {
            if (!FormatChecking.CheckMessageContent(messageContent))
                throw new ProtocolException("Invalid format of TCP message content field detected", "max 1400 VCHAR symbols and spaces", messageContent);

            var fields = Fields;
            fields.MessageContent = messageContent;
            Fields = fields;
        }

        // Takes string with DisplayName value and writes it in MessageFields structure
        protected void SetDisplayNameTcp(string displayName)
        {
            if (!FormatChecking.CheckDisplayName(displayName))
                throw new ProtocolException("Invalid format of TCP display name field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", displayName);

            var fields = Fields;
            fields.DisplayName = displayName;
            Fields = fields;
        }

        // Takes string with ChannelID value and writes it in MessageFields structure
        protected void SetChannelIdTcp(string channelId)
        {
            if (!FormatChecking.CheckChannelId(channelId))
                throw new ProtocolException("Invalid format of TCP channel ID field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", channelId);

            var fields = Fields;
            fields.ChannelId = channelId;
            Fields = fields;
        }

        // Takes string with Username value and writes it in MessageFields structure
        protected void SetUsernameTcp(string username)
        {
            if (!FormatChecking.CheckUserName(username))
                throw new ProtocolException("Invalid format of TCP username field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", username);

            var fields = Fields;
            fields.Username = username;
            Fields = fields;
        }

        // Takes string with Secret value and writes it in MessageFields structure
        protected void SetSecretTcp(string secret)
        {
            if (!FormatChecking.CheckSecret(secret))
                throw new ProtocolException("Invalid format of TCP secret field detected", "max 128 ALPHA, DIGIT, '-' and '.' symbols", secret);

            var fields = Fields;
            fields.Secret = secret;
            Fields = fields;
        }

        // Takes the start index of string value in byte array with UDP message, finds its end and convert a subarray into a string
        private string DecodeUdpString(ref int index)
        {
            var startIndex = index;
            var count = 0;
            while (Data[index++] != 0x00)
                count++;
            return Encoding.ASCII.GetString(Data, startIndex, count);
        }

        // Takes the index of the first byte of MessageID, decodes it, and writes it in MessageFields structure
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

        // Takes the index of the first byte of RefMessageID, decodes it, and writes it in MessageFields structure
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

        // Takes the index of the first byte of DisplayName, decodes it, and writes it in MessageFields structure
        protected void SetDisplayNameUdp(ref int index)
        {
            var displayNameString = DecodeUdpString(ref index);

            if (!FormatChecking.CheckDisplayName(displayNameString))
                throw new ProtocolException("Invalid format of UDP display name field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", displayNameString);

            var fields = Fields;
            fields.DisplayName = displayNameString;
            Fields = fields;
        }

        // Takes the index of the first byte of MessageContent, decodes it, and writes it in MessageFields structure
        protected void SetMessageContentUdp(ref int index)
        {
            var messageContent = DecodeUdpString(ref index);

            if (!FormatChecking.CheckMessageContent(messageContent))
                throw new ProtocolException("Invalid format of UDP message content field detected", "max 1400 VCHAR symbols and spaces", messageContent);

            var fields = Fields;
            fields.MessageContent = messageContent;
            Fields = fields;
        } 

        // Takes the index of the first byte of ChannelID, decodes it, and writes it in MessageFields structure
        protected void SetChannelIdUdp(ref int index)
        {
            var channelIdString = DecodeUdpString(ref index);

            if (!FormatChecking.CheckChannelId(channelIdString))
                throw new ProtocolException("Invalid format of UDP channel ID field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", channelIdString);

            var fields = Fields;
            fields.ChannelId = channelIdString;
            Fields = fields;
        }

        // Takes the index of the first byte of Username, decodes it, and writes it in MessageFields structure
        protected void SetUsernameUdp(ref int index)
        {
            var usernameString = DecodeUdpString(ref index);

            if (!FormatChecking.CheckUserName(usernameString))
                throw new ProtocolException("Invalid format of UDP username field detected", "max 20 ALPHA, DIGIT, '-' and '.' symbols", usernameString);

            var fields = Fields;
            fields.Username = usernameString;
            Fields = fields;
        }

        // Takes the index of the first byte of Secret, decodes it, and writes it in MessageFields structure
        protected void SetSecretUdp(ref int index)
        {
            var secretString = DecodeUdpString(ref index);

            if (!FormatChecking.CheckSecret(secretString))
                throw new ProtocolException("Invalid format of UDP secret field detected", "max 128 ALPHA, DIGIT, '-' and '.' symbols", secretString);

            var fields = Fields;
            fields.Secret = secretString;
            Fields = fields;
        }

        // Defines the type of message stored in byte array by checking first byte (UDP) or first ASCII word (TCP)
        private static MessageType DefineTypeOfMessage(byte[] data, ProtocolType protocol)
        {
            if (protocol == ProtocolType.Udp)
            {
                var typeByte = data[0];
                return typeByte switch
                {
                    0x00 => MessageType.Confirm,
                    0x01 => MessageType.Reply,
                    0x02 => MessageType.Auth,
                    0x03 => MessageType.Join,
                    0x04 => MessageType.Msg,
                    0xFE => MessageType.Err,
                    0xFF => MessageType.Bye,
                    _ => throw new ProtocolException("Invalid UDP message type detected", "[0x00-0x04 | 0xFE-0xFF]",
                        $"0x{typeByte:X2}")
                };
            }

            var messageString = Encoding.ASCII.GetString(data);
            var typeWord = messageString.Split(' ')[0];
            return typeWord switch
            {
                "ERR" => MessageType.Err,
                "REPLY" => MessageType.Reply,
                "AUTH" => MessageType.Auth,
                "JOIN" => MessageType.Join,
                "MSG" => MessageType.Msg,
                "BYE\r\n" => MessageType.Bye,
                _ => throw new ProtocolException("Invalid TCP message type detected", "[ERR|REPLY|AUTH|JOIN|MSG|BYE]",
                    $"{typeWord}")
            };
        }

        // The following constant string are used for decoding TCP messages:
        protected const string IsStr = " IS ";
        protected const string AsStr = " AS ";
        protected const string UsingStr = " USING ";
        protected const string JoinStr = "JOIN ";
        protected const string AuthStr = "AUTH ";
        protected const string MessageStr = "MSG FROM ";
        protected const string ErrorStr = "ERR FROM ";
        protected const string ReplyStr = "REPLY ";
        protected const string ByeStr = "BYE";
        protected const string EndStr = "\r\n";

        // Takes a string with full TCP message, key word before the field, and key word just after the field
        // Finds the string between two keywords and returns it
        protected string FindField(string message, string beforeField, string afterField)
        {
            var startIndex = message.IndexOf(beforeField, StringComparison.Ordinal) + beforeField.Length;
            var endIndex = message.IndexOf(afterField, StringComparison.Ordinal);
            if (startIndex >= 0 && endIndex >= 0)
                return message.Substring(startIndex, endIndex - startIndex);
            throw new ProtocolException("Invalid TCP message format", $"... {beforeField} [field] {afterField} ...", "no such structure");
        }

        // Takes a byte array and translates it into message due to specified protocol 
        public static Message? ConvertDataToMessage(byte[] data, ProtocolType type)
        {
            if (data.Length == 0)
                return null;

            Message message = new MsgMessage();
            try
            {
                var messType = DefineTypeOfMessage(data, type);
                switch (messType)
                {
                    case MessageType.Err: message = new ErrMessage(); break;
                    case MessageType.Confirm: message = new ConfirmMessage();break;
                    case MessageType.Reply: message = new ReplyMessage(); break;
                    case MessageType.Auth: message = new AuthMessage(); break;
                    case MessageType.Join: message = new JoinMessage(); break;
                    case MessageType.Msg: message = new MsgMessage(); break;
                    case MessageType.Bye: message = new ByeMessage(); break;
                }
                message.DecodeMessage(data, type);
            }
            catch
            {
                return null;
            }

            return message;
        }
    }
}
