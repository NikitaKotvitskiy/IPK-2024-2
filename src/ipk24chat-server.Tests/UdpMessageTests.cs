using Xunit;
using ipk24chat_server.messages;
using ipk24chat_server.inner;

namespace ipk24chat_server.Tests
{
    public class UdpMessageTests
    {
        [Fact]
        public void Decode_Valid_Auth_UdpMessageTest()
        {
            // Arrange
            var messageId = 42;
            var username = "xkotvi01";
            var displayName = "Nikita";
            var secret = "qwerty123";
            var data = new byte[] { 0x02,                                                           // Message type
                                    0x2A, 0x00,                                                     // MessageID
                                    0x78, 0x6B, 0x6F, 0x74, 0x76, 0x69, 0x30, 0x31, 0x00,           // Username
                                    0x4E, 0x69, 0x6B, 0x69, 0x74, 0x61, 0x00,                       // Display name
                                    0x71, 0x77, 0x65, 0x72, 0x74, 0x79, 0x31, 0x32, 0x33, 0x00 };   // Secret

            // Act
            var udpAuth = new AuthMessage();
            udpAuth.DecodeMessage(data, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(Message.ProtocolType.UDP, udpAuth.Protocol);
            Assert.Equal(Message.MessageType.AUTH, udpAuth.TypeOfMessage);
            Assert.Equal(data, udpAuth.Data);
            Assert.Equal((ushort?)messageId, udpAuth.Fields.MessageId);
            Assert.Equal(username, udpAuth.Fields.Username);
            Assert.Equal(displayName, udpAuth.Fields.DisplayName);
            Assert.Equal(secret, udpAuth.Fields.Secret);
        }

        [Fact]
        public void Decode_InvalidUsername_Auth_UdpMessageTest()
        {
            // Arrange
            var data = new byte[] { 0x02,                                                               // Message type
                                    0x2A, 0x00,                                                         // MessageID
                                    0x78, 0x20, 0x6B, 0x6F, 0x74, 0x76, 0x69, 0x20, 0x30, 0x31, 0x00,   // Invalid username ("x kotvi 01)
                                    0x4E, 0x69, 0x6B, 0x69, 0x74, 0x61, 0x00,                           // Display name
                                    0x71, 0x77, 0x65, 0x72, 0x74, 0x79, 0x31, 0x32, 0x33, 0x00 };       // Secret

            // Act & Assert
            var udpAuth = new AuthMessage();
            Assert.Throws<ProtocolException>(() => udpAuth.DecodeMessage(data, Message.ProtocolType.UDP));
        }

        [Fact]
        public void Decode_Invalid_DisplayName_Auth_UdpMessageTest()
        {
            // Arrange
            var data = new byte[] { 0x02,                                                           // Message type
                                    0x2A, 0x00,                                                     // MessageID
                                    0x78, 0x6B, 0x6F, 0x74, 0x76, 0x69, 0x30, 0x31, 0x00,           // Username
                                    0x4E, 0x69, 0x6B, 0x20, 0x69, 0x74, 0x61, 0x00,                 // Invalid display name ("Nik ita)
                                    0x71, 0x77, 0x65, 0x72, 0x74, 0x79, 0x31, 0x32, 0x33, 0x00 };   // Secret

            // Act & Assert
            var udpAuth = new AuthMessage();
            Assert.Throws<ProtocolException>(() => udpAuth.DecodeMessage(data, Message.ProtocolType.UDP));
        }

        [Fact]
        public void Decode_Invalid_Secret_Auth_UdpMessageTest()
        {
            // Arrange
            var data = new byte[] { 0x02,                                                   // Message type
                                    0x2A, 0x00,                                             // MessageID
                                    0x78, 0x6B, 0x6F, 0x74, 0x76, 0x69, 0x30, 0x31, 0x00,   // Invalid username
                                    0x4E, 0x69, 0x6B, 0x69, 0x74, 0x61, 0x00,               // Display name
                                    0x40, 0x23, 0x24, 0x25, 0x5E, 0x26, 0x00 };             // Invalid secret ("@#$%^&")

            // Act & Assert
            var udpAuth = new AuthMessage();
            Assert.Throws<ProtocolException>(() => udpAuth.DecodeMessage(data, Message.ProtocolType.UDP));
        }

        [Fact]
        public void Encode_Valid_Bye_UdpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageId = 42;
            var data = new byte[] { 0xFF, 0x2A, 0x00 };

            // Act
            var udpBye = new ByeMessage();
            udpBye.EncodeMessage(fields, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(data, udpBye.Data);
        }

        [Fact]
        public void Decode_Valid_Bye_UdpMessageTest()
        {
            // Arrange
            var data = new byte[] { 0xFF, 0x2A, 0x00 };
            var messageId = 42;

            // Act
            var udpBye = new ByeMessage();
            udpBye.DecodeMessage(data, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(Message.ProtocolType.UDP, udpBye.Protocol);
            Assert.Equal(Message.MessageType.BYE, udpBye.TypeOfMessage);
            Assert.Equal((ushort)messageId, udpBye.Fields.MessageId);
        }

        [Fact]
        public void Encode_Valid_Confirm_UdpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageRefId = 42;
            var data = new byte[] { 0x00, 0x2A, 0x00 };

            // Act
            var udpConfirm = new ConfirmMessage();
            udpConfirm.EncodeMessage(fields, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(data, udpConfirm.Data);
        }

        [Fact]
        public void Decode_Valid_Confirm_UdpMessageTest()
        {
            // Arrange
            var data = new byte[] { 0x00, 0x2A, 0x00 };
            var refMesId = 42;

            // Act
            var udpConfirm = new ConfirmMessage();
            udpConfirm.DecodeMessage(data, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(Message.ProtocolType.UDP, udpConfirm.Protocol);
            Assert.Equal(Message.MessageType.CONFIRM, udpConfirm.TypeOfMessage);
            Assert.Equal((ushort)refMesId, udpConfirm.Fields.MessageRefId);
        }

        [Fact]
        public void Encode_Valid_Err_UdpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageId = 42;
            fields.DisplayName = "Server";
            fields.MessageContent = "Error message";
            var data = new byte[] { 0xFE,                                                                                   // Message type
                                    0x2A, 0x00,                                                                             // Message ID
                                    0x53, 0x65, 0x72, 0x76, 0x65, 0x72, 0x00,                                               // Display name
                                    0x45, 0x72, 0x72, 0x6F, 0x72, 0x20, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00 };   // Message content

            // Act
            var udpErr = new ErrMessage();
            udpErr.EncodeMessage(fields, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(data, udpErr.Data);
        }

        [Fact]
        public void Decode_Valid_Err_UdpMessageTest()
        {
            // Arrange
            var messageId = 42;
            var displayName = "User";
            var messageContent = "Error message";
            var data = new byte[] { 0xFE,                                                                                   // Message type
                                    0x2A, 0x00,                                                                             // Message ID
                                    0x55, 0x73, 0x65, 0x72, 0x00,                                                           // Display name
                                    0x45, 0x72, 0x72, 0x6F, 0x72, 0x20, 0x6D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00 };   // Message content

            // Act
            var udpErr = new ErrMessage();
            udpErr.DecodeMessage(data, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(Message.ProtocolType.UDP, udpErr.Protocol);
            Assert.Equal(Message.MessageType.ERR, udpErr.TypeOfMessage);
            Assert.Equal((ushort)messageId, udpErr.Fields.MessageId);
            Assert.Equal(displayName, udpErr.Fields.DisplayName);
            Assert.Equal(messageContent, udpErr.Fields.MessageContent);
        }

        [Fact]
        public void Decode_Invalid_MessageContent_Err_UdpMessageTest()
        {
            // Arrange
            var data = new byte[] { 0xFE,                                   // Message type
                                    0x2A, 0x00,                             // Message ID
                                    0x55, 0x73, 0x65, 0x72, 0x00,           // Display name
                                    0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x00 };   // Invalid message content (unreadable symbols)

            // Act & Assert
            var udpErr = new ErrMessage();
            Assert.Throws<ProtocolException>(() => udpErr.DecodeMessage(data, Message.ProtocolType.UDP));
        }

        [Fact]
        public void Decode_Valid_Join_UdpMessageTest()
        {
            // Arrange
            var messageId = 42;
            var channelId = "general";
            var displayName = "user";
            var data = new byte[] { 0x03,                                           // Message type
                                    0x2A, 0x00,                                     // Message ID
                                    0x67, 0x65, 0x6E, 0x65, 0x72, 0x61, 0x6C, 0x00, // Channel ID
                                    0x75, 0x73, 0x65, 0x72, 0x00 };                 // Display name

            // Act
            var udpJoin = new JoinMessage();
            udpJoin.DecodeMessage(data, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(Message.ProtocolType.UDP, udpJoin.Protocol);
            Assert.Equal(Message.MessageType.JOIN, udpJoin.TypeOfMessage);
            Assert.Equal((ushort)messageId, udpJoin.Fields.MessageId);
            Assert.Equal(channelId, udpJoin.Fields.ChannelId);
            Assert.Equal(displayName, udpJoin.Fields.DisplayName);
        }

        [Fact]
        public void Decode_Invalid_ChannelId_Join_UdpMessageTest()
        {
            // Arrange
            var data = new byte[] { 0x03,                                           // Message type
                                    0x2A, 0x00,                                     // Message ID
                                    0x2A, 0x65, 0x6E, 0x65, 0x72, 0x61, 0x6C, 0x00, // Invalid channel ID ("*eneral")
                                    0x75, 0x73, 0x65, 0x72, 0x00 };                 // Display name

            // Act & Assert
            var udpJoin = new JoinMessage();
            Assert.Throws<ProtocolException>(() => udpJoin.DecodeMessage(data, Message.ProtocolType.UDP));
        }

        [Fact]
        public void Encode_Valid_Msg_UdpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageId = 42;
            fields.DisplayName = "Server";
            fields.MessageContent = "User connected!";
            var data = new byte[] { 0x04,                                                                                               // Message type
                                    0x2A, 0x00,                                                                                         // Message ID
                                    0x53, 0x65, 0x72, 0x76, 0x65, 0x72, 0x00,                                                           // Display name
                                    0x55, 0x73, 0x65, 0x72, 0x20, 0x63, 0x6F, 0x6E, 0x6E, 0x65, 0x63, 0x74, 0x65, 0x64, 0x21, 0x00 };   // Message content

            // Act
            var udpMsg = new MsgMessage();
            udpMsg.EncodeMessage(fields, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(data, udpMsg.Data);
        }

        [Fact]
        public void Decode_Valid_Msg_UdpMessageTest()
        {
            // Arrange
            var messageId = 42;
            var displayName = "User";
            var messageContent = "Message";
            var data = new byte[] { 0x04,                                               // Message type
                                    0x2A, 0x00,                                         // Message ID
                                    0x55, 0x73, 0x65, 0x72, 0x00,                       // Display name
                                    0x4D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00 };   // Message content

            // Act
            var udpMsg = new MsgMessage();
            udpMsg.DecodeMessage(data, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(Message.ProtocolType.UDP, udpMsg.Protocol);
            Assert.Equal(Message.MessageType.MSG, udpMsg.TypeOfMessage);
            Assert.Equal((ushort)messageId, udpMsg.Fields.MessageId);
            Assert.Equal(displayName, udpMsg.Fields.DisplayName);
            Assert.Equal(messageContent, udpMsg.Fields.MessageContent);
        }

        [Fact]
        public void Encode_Valid_Reply_UdpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageId = 42;
            fields.Result = true;
            fields.MessageRefId = 42;
            fields.MessageContent = "Joined!";
            var data = new byte[] { 0x01,
                                    0x2A, 0x00,
                                    0x01,
                                    0x2A, 0x00,
                                    0x4A, 0x6F, 0x69, 0x6E, 0x65, 0x64, 0x21, 0x00 };

            // Act
            var udpReply = new ReplyMessage();
            udpReply.EncodeMessage(fields, Message.ProtocolType.UDP);

            // Assert
            Assert.Equal(data, udpReply.Data);
        }
    }
}
