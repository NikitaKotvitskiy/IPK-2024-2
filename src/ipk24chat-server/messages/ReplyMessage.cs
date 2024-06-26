﻿/******************************************************************************
 *  IPK-2024-2
 *  ReplyMessage.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    REPLY message enoding/decoding
 *  Last change:    10.04.23
 *****************************************************************************/

using System.Text;

namespace ipk24chat_server.messages
{
    public class ReplyMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            // Users never send REPLY messages
            throw new NotImplementedException();
        }

        public override void EncodeMessage(MessageFields fields, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.Reply;
            Protocol = protocol;
            Fields = fields;

            if (protocol == ProtocolType.Udp)
            {
                var mesIdArr = BitConverter.GetBytes((ushort)Fields.MessageId!);
                var resultByte = (byte)(Fields.Result! == true ? 1 : 0);
                var refMesIdArr = BitConverter.GetBytes((ushort)Fields.MessageRefId!);
                var mesContentArr = Encoding.ASCII.GetBytes(Fields.MessageContent!);

                Data = new byte[1 + mesIdArr.Length + 1 + refMesIdArr.Length + mesContentArr.Length + 1];
                var index = 0;
                Data[index++] = 0x01;
                Array.Copy(mesIdArr, 0, Data, index, mesIdArr.Length);
                index += mesIdArr.Length;
                Data[index++] = resultByte;
                Array.Copy(refMesIdArr, 0, Data, index, refMesIdArr.Length);
                index += refMesIdArr.Length;
                Array.Copy(mesContentArr, 0, Data, index, mesContentArr.Length);
                index += mesContentArr.Length;
                Data[index] = 0x00;
            }
            else
            {
                var messageString = new string($"REPLY {(Fields.Result! == true ? "OK" : "NOK")} IS {Fields.MessageContent!}");
                EncodeTcpMessageStringToByteArr(messageString);
            }
        }
    }
}
