using ipk24chat_server.inner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ipk24chat_server.messages
{
    public class ConfirmMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            if (protocol == ProtocolType.TCP)
                throw new ProgramException("Trying to decode CONFIRM message in TCP protocol", "messages/ConfirmMessage.cs/DecodeMessage");
            
            TypeOfMessage = MessageType.CONFIRM;
            Protocol = protocol;
            Data = data;

            var index = 1;
            SetReferenceMessageId(ref index);
        }

        public override void EncodeMessage(MessageFields fields, ProtocolType protocol)
        {
            if (protocol == ProtocolType.TCP)
                throw new ProgramException("Trying to encode CONFIRM message in TCP protocol", "messages/ConfirmMessage.cs/EncodeMessage");
            
            TypeOfMessage = MessageType.CONFIRM;
            Protocol = protocol;
            Fields = fields;

            var refMesIdArr = BitConverter.GetBytes((ushort)Fields.MessageRefId!);
            Array.Reverse(refMesIdArr);
            Data = new byte[1 +  refMesIdArr.Length];
            Data[0] = 0x00;
            Array.Copy(refMesIdArr, 0, Data, 1, refMesIdArr.Length);
        }
    }
}
