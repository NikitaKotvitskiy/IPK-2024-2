using ipk24chat_server.inner;
using ipk24chat_server.messages;
using System.Net;
using System.Text;
using Xunit;

namespace ipk24chat_server.Tests
{
    public class LoggingTests : IDisposable
    {
        private readonly StringWriter consoleOutput = null!;
        private readonly TextWriter originalOutput = null!;

        private readonly IPAddress Ip = IPAddress.Parse("123.123.123.123");
        private readonly ushort Port = 1234;

        public LoggingTests()
        {
            originalOutput = Console.Out;
            consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);
        }

        [Fact]
        public void Auth_In_Udp_LoggingTest()
        {
            // Arrange
            var data = new byte[] { 0x02,                                                           // Message type = AUTH
                                    0x2A, 0x00,                                                     // MessageID = 42
                                    0x78, 0x6B, 0x6F, 0x74, 0x76, 0x69, 0x30, 0x31, 0x00,           // Username = xkotvi01
                                    0x4E, 0x69, 0x6B, 0x69, 0x74, 0x61, 0x00,                       // Display name = Nikita
                                    0x71, 0x77, 0x65, 0x72, 0x74, 0x79, 0x31, 0x32, 0x33, 0x00 };   // Secret = qwerty123
            var message = new AuthMessage();
            message.DecodeMessage(data, Message.ProtocolType.UDP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | AUTH MessageId=42 Username=xkotvi01 DisplayName=Nikita Secret=qwerty123\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Auth_In_Tcp_LogginTest()
        {
            // Arrange
            var messageString = "AUTH xkotvi01 AS Nikita USING qwerty123\r\n";
            var messageData = Encoding.ASCII.GetBytes(messageString);
            var message = new AuthMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.TCP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | AUTH Username=xkotvi01 DisplayName=Nikita Secret=qwerty123\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Bye_In_Udp_LoggingTest()
        {
            // Arrange
            var messageData = new byte[] {  0xFF,           // Message type = BYE
                                            0x2A, 0x00 };   // MessageID = 42
            var message = new ByeMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.UDP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | BYE MessageId=42\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Bye_In_Tcp_LoggingTest()
        {
            // Arrange
            var messageString = "BYE\r\n";
            var messageData = Encoding.ASCII.GetBytes(messageString);
            var message = new ByeMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.TCP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | BYE\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Bye_Out_Udp_LoggingTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageId = 42;
            var message = new ByeMessage();
            message.EncodeMessage(fields, Message.ProtocolType.UDP);
            var expectedLogString = $"SENT {Ip.ToString()}:{Port} | BYE MessageId={fields.MessageId}\n";

            // Act
            Logging.LogMessage(Ip, Port, false, message);

            // Assert
            Assert.Contains(expectedLogString , consoleOutput.ToString());
        }

        [Fact]
        public void Bye_Out_Tcp_LoggingTest()
        {
            // Arrange
            var message = new ByeMessage();
            message.EncodeMessage(new Message.MessageFields(), Message.ProtocolType.TCP);
            var expectedLogString = $"SENT {Ip.ToString()}:{Port} | BYE\n";

            // Act
            Logging.LogMessage(Ip, Port, false, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Confirm_In_Udp_LoggingTest()
        {
            // Arrange
            var messageData = new byte[] {  0x00,           // Message type = CONFIRM
                                            0x2A, 0x00 };   // RefMessageId = 42

            var message = new ConfirmMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.UDP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | CONFIRM RefMessageId=42\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Confirm_Out_Udp_LoggingTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageRefId = 42;
            var message = new ConfirmMessage();
            message.EncodeMessage(fields, Message.ProtocolType.UDP);
            var expectedLogString = $"SENT {Ip.ToString()}:{Port} | CONFIRM RefMessageId=42\n";

            // Act
            Logging.LogMessage(Ip, Port, false, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Err_In_Udp_LoggingTest()
        {
            var messageData = new byte[] {  0xFE,                                               // Message type = ERR
                                            0x2A, 0x00,                                         // MessageID = 42
                                            0x55, 0x73, 0x65, 0x72, 0x00,                       // DisplayName = User
                                            0x4D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00 };   // MessageContent = Message
            var message = new ErrMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.UDP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | ERR MessageId=42 DisplayName=User MessageContent=Message\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Err_In_Tcp_LoggingTest()
        {
            // Arrange
            var messageString = "ERR FROM User IS Message\r\n";
            var messageData = Encoding.ASCII.GetBytes(messageString);
            var message = new ErrMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.TCP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | ERR DisplayName=User MessageContent=Message\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Err_Out_Udp_LoggingTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageId = 42;
            fields.DisplayName = "Server";
            fields.MessageContent = "Message";
            var message = new ErrMessage();
            message.EncodeMessage(fields, Message.ProtocolType.UDP);

            var expectedLogString = $"SENT {Ip.ToString()}:{Port} | ERR MessageId=42 DisplayName=Server MessageContent=Message\n";

            // Act
            Logging.LogMessage(Ip, Port, false, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Err_Out_Tcp_LoggingTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.DisplayName = "Server";
            fields.MessageContent = "Message";
            var message = new ErrMessage();
            message.EncodeMessage(fields, Message.ProtocolType.TCP);
            var expectedLogString = $"SENT {Ip.ToString()}:{Port} | ERR DisplayName=Server MessageContent=Message\n";

            // Act
            Logging.LogMessage(Ip, Port, false, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Join_In_Udp_LoggingTest()
        {
            // Arrange
            var messageData = new byte[] {  0x03,                                               // Message type = JOIN
                                            0x2A, 0x00,                                         // MessageID = 42
                                            0x67, 0x65, 0x6E, 0x65, 0x72, 0x61, 0x6C, 0x00,     // ChannelID = general
                                            0x55, 0x73, 0x65, 0x72, 0x00 };                     // DisplayName = User
            var message = new JoinMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.UDP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | JOIN MessageId=42 ChannelId=general DisplayName=User\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Join_In_Tcp_LoggingTest()
        {
            var messageString = "JOIN general AS User\r\n";
            var messageData = Encoding.ASCII.GetBytes(messageString);
            var message = new JoinMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.TCP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | JOIN ChannelId=general DisplayName=User\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Msg_In_Udp_LoggingTest()
        {
            var messageData = new byte[] {  0x04,                                               // Message type = MSG
                                            0x2A, 0x00,                                         // MessageID = 42
                                            0x55, 0x73, 0x65, 0x72, 0x00,                       // DisplayName = User
                                            0x4D, 0x65, 0x73, 0x73, 0x61, 0x67, 0x65, 0x00 };   // MessageContent = Message
            var message = new MsgMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.UDP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | MSG MessageId=42 DisplayName=User MessageContent=Message\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Msg_In_Tcp_LoggingTest()
        {
            // Arrange
            var messageString = "MSG FROM User IS Message\r\n";
            var messageData = Encoding.ASCII.GetBytes(messageString);
            var message = new MsgMessage();
            message.DecodeMessage(messageData, Message.ProtocolType.TCP);
            var expectedLogString = $"RECV {Ip.ToString()}:{Port} | MSG DisplayName=User MessageContent=Message\n";

            // Act
            Logging.LogMessage(Ip, Port, true, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Msg_Out_Udp_LoggingTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageId = 42;
            fields.DisplayName = "Server";
            fields.MessageContent = "Message";
            var message = new MsgMessage();
            message.EncodeMessage(fields, Message.ProtocolType.UDP);

            var expectedLogString = $"SENT {Ip.ToString()}:{Port} | MSG MessageId=42 DisplayName=Server MessageContent=Message\n";

            // Act
            Logging.LogMessage(Ip, Port, false, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Msg_Out_Tcp_LoggingTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.DisplayName = "Server";
            fields.MessageContent = "Message";
            var message = new MsgMessage();
            message.EncodeMessage(fields, Message.ProtocolType.TCP);
            var expectedLogString = $"SENT {Ip.ToString()}:{Port} | MSG DisplayName=Server MessageContent=Message\n";

            // Act
            Logging.LogMessage(Ip, Port, false, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Reply_Out_Udp_LoggingTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.MessageId = 42;
            fields.MessageRefId = 42;
            fields.Result = true;
            fields.MessageContent = "Success";
            var message = new ReplyMessage();
            message.EncodeMessage(fields, Message.ProtocolType.UDP);
            var expectedLogString = $"SENT {Ip.ToString()}:{Port} | REPLY MessageId=42 RefMessageId=42 Result=True MessageContent=Success\n";

            // Act
            Logging.LogMessage(Ip, Port, false, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        [Fact]
        public void Reply_Out_Tcp_LoggingTest()
        {
            // Arrange
            var fields = new Message.MessageFields();
            fields.Result = false;
            fields.MessageContent = "Denied";
            var message = new ReplyMessage();
            message.EncodeMessage(fields, Message.ProtocolType.TCP);
            var expectedLogString = $"SENT {Ip.ToString()}:{Port} | REPLY Result=False MessageContent=Denied\n";

            // Act
            Logging.LogMessage(Ip, Port, false, message);

            // Assert
            Assert.Contains(expectedLogString, consoleOutput.ToString());
        }

        public void Dispose()
        {
            Console.SetOut(originalOutput);
            consoleOutput.Dispose();
        }
    }
}
