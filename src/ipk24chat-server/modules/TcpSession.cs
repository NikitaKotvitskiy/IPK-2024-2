using ipk24chat_server.messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ipk24chat_server.modules
{
    internal class TcpSession
    {
        public void StartSession(TcpClient client)
        {
            throw new NotImplementedException();
        }

        public Message DecodeMessage(byte[] data)
        {
            throw new NotImplementedException();
        }

        public void ProcessMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public byte[] ReceiveMessage()
        {
            throw new NotImplementedException();
        }

        public void SendMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public void StopSession()
        {
            throw new NotImplementedException();
        }
    }
}
