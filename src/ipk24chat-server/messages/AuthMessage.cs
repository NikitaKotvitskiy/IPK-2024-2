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

                var username = FindField(messageString, authStr, asStr);
                var displayName = FindField(messageString, asStr, usingStr);
                var secret = FindField(messageString, usingStr, endStr);

                SetUsernameTcp(username);
                SetDisplayNameTcp(displayName);
                SetSecretTcp(secret);
            }
        }

        public override void EncodeMessage(MessageFields fields, ProtocolType protocol)
        {
            // Server never generates AUTH message
            throw new NotImplementedException();
        }
    }
}
