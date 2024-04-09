namespace ipk24chat_server.messages
{
    public class ByeMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.BYE;
            Data = data;
            Protocol = protocol;

            if (Protocol == ProtocolType.UDP)
            {
                var index = 1;
                SetMessageId(ref index);
            }
        }

        public override void EncodeMessage(MessageFields fields, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.BYE;
            Protocol = protocol;
            Fields = fields;

            if (Protocol == ProtocolType.UDP)
            {
                var mesIdArr = BitConverter.GetBytes((ushort)Fields.MessageId!);
                Array.Reverse(mesIdArr);
                Data = new byte[1 + mesIdArr.Length];
                Data[0] = 0xFF;
                Array.Copy(mesIdArr, 0, Data, 1, mesIdArr.Length);
            }
            else
            {
                var messageString = "BYE";
                EncodeTcpMessageStringToByteArr(messageString);
            }
        }
    }
}
