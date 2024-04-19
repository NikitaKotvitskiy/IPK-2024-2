/******************************************************************************
 *  IPK-2024-2
 *  ByeMessage.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    BYE message encoding/decoding
 *  Last change:    10.04.23
 *****************************************************************************/

namespace ipk24chat_server.messages
{
    public class ByeMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.Bye;
            Data = data;
            Protocol = protocol;

            if (Protocol == ProtocolType.Udp)
            {
                var index = 1;
                SetMessageId(ref index);
            }
        }

        public override void EncodeMessage(MessageFields fields, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.Bye;
            Protocol = protocol;
            Fields = fields;

            if (Protocol == ProtocolType.Udp)
            {
                var mesIdArr = BitConverter.GetBytes((ushort)Fields.MessageId!);
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
