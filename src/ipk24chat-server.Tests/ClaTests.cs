using ipk24chat_server.inner;
using System.Net;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ipk24chat_server.Tests
{
    public class ClaTests
    {
        [Fact]
        public void NoClaTest()
        {
            // Arrange
            var args = new string[0];
            var validIp = IPAddress.Parse("0.0.0.0");

            // Act
            var result = Cla.ProcessCla(args);

            // Assert
            Assert.True(result);
            Assert.Equal(validIp.ToString(), Cla.ListeningIp.ToString());   
            Assert.Equal((ushort)4567, Cla.ListeningPort);
            Assert.Equal((ushort)250, Cla.UdpTimeout);
            Assert.Equal((byte)3, Cla.UdpMaxRetransmissions);
        }

        [Fact]
        public void ValidIpClaTest()
        {
            // Arrange
            var args = new string[] { "-l", "127.0.0.1" };

            // Act
            var result = Cla.ProcessCla(args);

            // Assert
            Assert.True(result);
            Assert.Equal("127.0.0.1", Cla.ListeningIp.ToString());
        }

        [Fact]
        public void ValidPortClaTest()
        {
            // Arrange
            var args = new string[] { "-p", "1234" };

            // Act
            var result = Cla.ProcessCla(args);

            // Assert
            Assert.True(result);
            Assert.Equal((ushort)1234, Cla.ListeningPort);
        }

        [Fact]
        public void ValidUdpTimeoutClaTest()
        {
            // Arrange
            var args = new string[] { "-d", "300" };

            // Act
            var result = Cla.ProcessCla(args);

            // Assert
            Assert.True(result);
            Assert.Equal((ushort)300, Cla.UdpTimeout);
        }

        [Fact]
        public void ValidUdpRetransmissionsClaTest()
        {
            // Arrange
            var args = new string[] { "-r", "5" };

            // Act
            var result = Cla.ProcessCla(args);

            // Assert
            Assert.True(result);
            Assert.Equal((byte)5, Cla.UdpMaxRetransmissions);
        }

        [Fact]
        public void HelpClaTest()
        {
            // Arrange
            var args = new string[] { "-h" };

            // Act
            var result = Cla.ProcessCla(args);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AllValidClaTest()
        {
            // Arrange
            var args = new string[] { "-p", "5678", "-l", "25.25.25.25", "-r", "1", "-d", "500" };

            // Act
            var result = Cla.ProcessCla(args);

            // Assert
            Assert.True(result);
            Assert.Equal("25.25.25.25", Cla.ListeningIp.ToString());
            Assert.Equal((ushort)5678, Cla.ListeningPort);
            Assert.Equal((ushort)500, Cla.UdpTimeout);
            Assert.Equal((byte)1, Cla.UdpMaxRetransmissions);
        }

        [Fact]
        public void OneInvalidClaTest()
        {
            // Arrange
            var args = new string[] { "-p", "5678", "-l", "25.25.25.1000", "-r", "1", "-d", "500" };

            // Act
            var result = Cla.ProcessCla(args);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AllValidButHelpClaTest()
        {
            // Arrange
            var args = new string[] { "-p", "5678", "-l", "25.25.25.25", "-r", "1", "-d", "500", "-h" };

            // Act
            var result = Cla.ProcessCla(args);

            // Assert
            Assert.False(result);
        }
    }
}
