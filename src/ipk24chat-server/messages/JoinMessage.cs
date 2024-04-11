using System.Text;

namespace ipk24chat_server.messages
{
    public class JoinMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.JOIN;
            Data = data;
            Protocol = protocol;

            if (Protocol == ProtocolType.UDP)
            {
                var index = 1;
                SetMessageId(ref index);
                SetChannelIdUdp(ref index);
                SetDisplayNameUdp(ref index);
            }
            else
            {
                var messageString = Encoding.ASCII.GetString(Data);

                var channelId = FindField(messageString, joinStr, asStr);
                var displayName = FindField(messageString, asStr, endStr);

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
