using System.Text;

namespace ipk24chat_server.messages
{
    public class MsgMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.MSG;
            Data = data;
            Protocol = protocol;

            if (Protocol == ProtocolType.UDP)
            {
                var index = 1;
                SetMessageId(ref index);
                SetDisplayNameUdp(ref index);
                SetMessageContentUdp(ref index);
            }
            else
            {
                var messageString = Encoding.ASCII.GetString(Data);

                var displayName = FindField(messageString, messageStr, isStr);
                var messageContent = FindField(messageString, isStr, endStr);

                SetDisplayNameTcp(displayName);
                SetMessageContentTcp(messageContent);
            }
        }

        public override void EncodeMessage(MessageFields fields, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.MSG;
            Protocol = protocol;
            Fields = fields;

            if (Protocol == ProtocolType.UDP)
            {
                var mesIdArr = BitConverter.GetBytes((ushort)Fields.MessageId!);
                var displayNameArr = Encoding.ASCII.GetBytes(Fields.DisplayName!);
                var messageContentArr = Encoding.ASCII.GetBytes(Fields.MessageContent!);

                Data = new byte[1 + mesIdArr.Length + displayNameArr.Length + 1 + messageContentArr.Length + 1];
                var index = 0;
                Data[index++] = 0x04;
                Array.Copy(mesIdArr, 0, Data, index, mesIdArr.Length);
                index += mesIdArr.Length;
                Array.Copy(displayNameArr, 0, Data, index, displayNameArr.Length);
                index += displayNameArr.Length;
                Data[index++] = 0x00;
                Array.Copy(messageContentArr, 0, Data, index, messageContentArr.Length);
                index += messageContentArr.Length;
                Data[index] = 0x00;
            }
            else
            {
                var messageString = new string($"MSG FROM {Fields.DisplayName!} IS {Fields.MessageContent}");
                EncodeTcpMessageStringToByteArr(messageString);
            }
        }
    }
}
