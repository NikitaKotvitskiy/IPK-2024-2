using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Text;
using Xunit;

namespace ipk24chat_server.Tests
{
    public class TcpMessageTests
    {
        [Fact]
        public void Decode_Valid_Auth_TcpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.Username = "xkotvi01";
            fields.DisplayName = "Nikita";
            fields.Secret = "qwerty123";
            var message = new string($"AUTH {fields.Username} AS {fields.DisplayName} USING {fields.Secret}\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act
            var tcpAuth = new AuthMessage();
            tcpAuth.DecodeMessage(data, Message.ProtocolType.Tcp);

            // Assert
            Assert.Equal(Message.ProtocolType.Tcp, tcpAuth.Protocol);
            Assert.Equal(Message.MessageType.Auth, tcpAuth.TypeOfMessage);
            Assert.Equal(fields, tcpAuth.Fields);
        }

        [Fact]
        public void Decode_Invalid_Username_Auth_TcpMessageTest()
        {
            // Arrange
            var message = "AUTH x kotvi 01 AS Nikita USING qwerty123\r\n"; // Invalid username: "x kotvi 01"
            var data = Encoding.ASCII.GetBytes(message);

            // Act && Assert
            var tcpAuth = new AuthMessage();
            Assert.Throws<ProtocolException>(() => tcpAuth.DecodeMessage(data, Message.ProtocolType.Tcp));
        }

        [Fact]
        public void Decode_Invalid_DisplayName_Auth_TcpMessageTest()
        {
            // Arrange
            var message = "AUTH xkotvi01 AS Nik ita USING qwerty123\r\n"; // Invalid display name: "Nik ita"
            var data = Encoding.ASCII.GetBytes(message);

            // Act && Assert
            var tcpAuth = new AuthMessage();
            Assert.Throws<ProtocolException>(() => tcpAuth.DecodeMessage(data, Message.ProtocolType.Tcp));
        }

        [Fact]
        public void Decode_Invalid_Secret_Auth_TcpMessageTest()
        {
            // Arrange
            var message = "AUTH xkotvi01 AS Nikita USING #$%^&\r\n"; // Invalid secret: "#$%^&"
            var data = Encoding.ASCII.GetBytes(message);

            // Act && Assert
            var tcpAuth = new AuthMessage();
            Assert.Throws<ProtocolException>(() => tcpAuth.DecodeMessage(data, Message.ProtocolType.Tcp));
        }

        [Fact]
        public void Encode_Valid_Bye_TcpMessageTest()
        {
            // Arrange
            var message = new string("BYE\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act
            var tcpBye = new ByeMessage();
            tcpBye.EncodeMessage(new Message.MessageFields(), Message.ProtocolType.Tcp);

            // Assert
            Assert.Equal(data, tcpBye.Data);
        }

        [Fact]
        public void Decode_Valid_Bye_TcpMessageTest()
        {
            // Arrange
            var message = new string("BYE\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act
            var tcpBye = new ByeMessage();
            tcpBye.DecodeMessage(data, Message.ProtocolType.Tcp);

            // Assert
            Assert.True(tcpBye.Protocol == Message.ProtocolType.Tcp);
            Assert.True(tcpBye.TypeOfMessage == Message.MessageType.Bye);
        }

        [Fact]
        public void Encode_Valid_Err_TcpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.DisplayName = "Server";
            fields.MessageContent = "Error message";
            var message = new string($"ERR FROM {fields.DisplayName} IS {fields.MessageContent}\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act
            var tcpErr = new ErrMessage();
            tcpErr.EncodeMessage(fields, Message.ProtocolType.Tcp);

            // Assert
            Assert.Equal(data, tcpErr.Data);
        }

        [Fact]
        public void Decode_Valid_Err_TcpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.DisplayName = "User";
            fields.MessageContent = "Error message";
            var message = new string($"ERR FROM {fields.DisplayName} IS {fields.MessageContent}\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act
            var tcpErr = new ErrMessage();
            tcpErr.DecodeMessage(data, Message.ProtocolType.Tcp);

            // Assert
            Assert.True(tcpErr.Protocol == Message.ProtocolType.Tcp);
            Assert.True(tcpErr.TypeOfMessage == Message.MessageType.Err);
            Assert.Equal(fields, tcpErr.Fields);
        }

        [Fact]
        public void Decode_Invalid_MessageContent_Err_TcpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.DisplayName = "User";
            fields.MessageContent = "Error " + (char)0x0A + (char)0x0B + " message"; // Invalid message content
            var message = new string($"ERR FROM {fields.DisplayName} IS {fields.MessageContent}\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act && Assert
            var tcpErr = new ErrMessage();
            Assert.Throws<ProtocolException>(() => tcpErr.DecodeMessage(data, Message.ProtocolType.Tcp));
        }

        [Fact]
        public void Decode_Valid_Join_TcpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.ChannelId = "general";
            fields.DisplayName = "user";
            var message = new string($"JOIN {fields.ChannelId} AS {fields.DisplayName}\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act
            var udpJoin = new JoinMessage();
            udpJoin.DecodeMessage(data, Message.ProtocolType.Tcp);

            // Assert
            Assert.True(udpJoin.Protocol == Message.ProtocolType.Tcp);
            Assert.True(udpJoin.TypeOfMessage == Message.MessageType.Join);
            Assert.Equal(fields, udpJoin.Fields);
        }

        [Fact]
        public void Decode_Invalid_ChannelId_Join_TcpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.ChannelId = "!@#$%s"; // Invalid channel ID
            fields.DisplayName = "user";
            var message = new string($"JOIN {fields.ChannelId} AS {fields.DisplayName}\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act & Assert
            var tcpJoin = new JoinMessage();
            Assert.Throws<ProtocolException>(() => tcpJoin.DecodeMessage(data, Message.ProtocolType.Tcp));
        }

        [Fact]
        public void Encode_Valid_Msg_TcpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.DisplayName = "Server";
            fields.MessageContent = "Server message";
            var message = new string($"MSG FROM {fields.DisplayName} IS {fields.MessageContent}\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act
            var tcpMsg = new MsgMessage();
            tcpMsg.EncodeMessage(fields, Message.ProtocolType.Tcp);

            // Assert
            Assert.Equal(data, tcpMsg.Data);
        }

        [Fact]
        public void Decode_Valid_Msg_TcpMessageTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.DisplayName = "User";
            fields.MessageContent = "User message";
            var message = new string($"MSG FROM {fields.DisplayName} IS {fields.MessageContent}\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act
            var tcpMsg = new MsgMessage();
            tcpMsg.DecodeMessage(data, Message.ProtocolType.Tcp);

            // Assert
            Assert.True(tcpMsg.Protocol == Message.ProtocolType.Tcp);
            Assert.True(tcpMsg.TypeOfMessage == Message.MessageType.Msg);
            Assert.Equal(fields, tcpMsg.Fields);
        }

        [Fact]
        public void Encode_Valid_Reply_TcpMessageTest()
        {
            var fields = new Message.MessageFields();
            fields.Result = false;
            fields.MessageContent = "Access denied";
            var message = new string($"REPLY NOK IS {fields.MessageContent}\r\n");
            var data = Encoding.ASCII.GetBytes(message);

            // Act
            var tcpReply = new ReplyMessage();
            tcpReply.EncodeMessage(fields, Message.ProtocolType.Tcp);

            // Assert
            Assert.Equal(data, tcpReply.Data);
        }
    }
}
