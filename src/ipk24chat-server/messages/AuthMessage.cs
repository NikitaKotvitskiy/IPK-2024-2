using System.Text;

namespace ipk24chat_server.messages
{
    public class AuthMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.AUTH;
            Protocol = protocol;
            Data = data;

            if (Protocol == ProtocolType.UDP)
            {
                var index = 1;
                SetMessageId(ref index);
                SetUsernameUdp(ref index);
                SetDisplayNameUdp(ref index);
                SetSecretUdp(ref index);
            }
            else
            {
                var messageString = Encoding.ASCII.GetString(Data);
                var words = messageString.Split(' ');

                SetUsernameTcp(words[1]);
                SetDisplayNameTcp(words[3]);
                SetSecretTcp(words[5]);
            }
        }

        public override void EncodeMessage(MessageFields fields, ProtocolType protocol)
        {
            // Server never generates AUTH message
            throw new NotImplementedException();
        }
    }
}
