/******************************************************************************
 *  IPK-2024-2
 *  JoinMessage.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    JOIN message decoding
 *  Last change:    10.04.23
 *****************************************************************************/

using System.Text;

namespace ipk24chat_server.messages
{
    public class JoinMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.Join;
            Data = data;
            Protocol = protocol;

            if (Protocol == ProtocolType.Udp)
            {
                var index = 1;
                SetMessageId(ref index);
                SetChannelIdUdp(ref index);
                SetDisplayNameUdp(ref index);
            }
            else
            {
                var messageString = Encoding.ASCII.GetString(Data);

                var channelId = FindField(messageString, JoinStr, AsStr);
                var displayName = FindField(messageString, AsStr, EndStr);

                SetChannelIdTcp(channelId);
                SetDisplayNameTcp(displayName);
            }
        }

        public override void EncodeMessage(MessageFields fields, ProtocolType protocol)
        {
            // Server never generates JOIN message
            throw new NotImplementedException();
        }
    }
}
