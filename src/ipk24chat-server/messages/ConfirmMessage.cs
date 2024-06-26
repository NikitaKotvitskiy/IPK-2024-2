﻿/******************************************************************************
 *  IPK-2024-2
 *  ConfirmMessage.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    CONFIRM message encoding/decoding
 *  Last change:    10.04.23
 *****************************************************************************/

using ipk24chat_server.inner;

namespace ipk24chat_server.messages
{
    public class ConfirmMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            if (protocol == ProtocolType.Tcp)
                throw new ProgramException("Trying to decode CONFIRM message in TCP protocol", "messages/ConfirmMessage.cs/DecodeMessage");
            
            TypeOfMessage = MessageType.Confirm;
            Protocol = protocol;
            Data = data;

            var index = 1;
            SetReferenceMessageId(ref index);
        }

        public override void EncodeMessage(MessageFields fields, ProtocolType protocol)
        {
            if (protocol == ProtocolType.Tcp)
                throw new ProgramException("Trying to encode CONFIRM message in TCP protocol", "messages/ConfirmMessage.cs/EncodeMessage");
            
            TypeOfMessage = MessageType.Confirm;
            Protocol = protocol;
            Fields = fields;

            var refMesIdArr = BitConverter.GetBytes((ushort)Fields.MessageRefId!);
            Data = new byte[1 +  refMesIdArr.Length];
            Data[0] = 0x00;
            Array.Copy(refMesIdArr, 0, Data, 1, refMesIdArr.Length);
        }
    }
}
