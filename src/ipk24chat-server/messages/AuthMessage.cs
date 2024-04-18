/******************************************************************************
 *  IPK-2024-2
 *  AuthMessage.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    AUTH message decoding
 *  Last change:    10.04.23
 *****************************************************************************/

using System.Text;

namespace ipk24chat_server.messages
{
    public class AuthMessage : Message
    {
        public override void DecodeMessage(byte[] data, ProtocolType protocol)
        {
            TypeOfMessage = MessageType.Auth;
            Protocol = protocol;
            Data = data;

            if (Protocol == ProtocolType.Udp)
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

                var username = FindField(messageString, AuthStr, AsStr);
                var displayName = FindField(messageString, AsStr, UsingStr);
                var secret = FindField(messageString, UsingStr, EndStr);

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
