using ipk24chat_server.messages;
using System.Net;

namespace ipk24chat_server.inner
{
    public abstract class Logging
    {
        private static void WriteErrFields(Message message, ref string logString)
        {
            logString += " ERR";
            if (message.Protocol == Message.ProtocolType.UDP)
                logString += $" MessageId={message.Fields.MessageId}";
            logString += $" DisplayName={message.Fields.DisplayName}";
            logString += $" MessageContent={message.Fields.MessageContent}";
        }
        private static void WriteConfirmFields(Message message, ref string logString)
        {
            logString += " CONFIRM";
            logString += $" RefMessageId={message.Fields.MessageRefId}";
        }
        private static void WriteReplyFields(Message message, ref string logString)
        {
            logString += " REPLY";
            if (message.Protocol == Message.ProtocolType.UDP)
            {
                logString += $" MessageId={message.Fields.MessageId}";
                logString += $" RefMessageId={message.Fields.MessageRefId}";
            }
            logString += $" Result={message.Fields.Result}";
            logString += $" MessageContent={message.Fields.MessageContent}";
        }
        private static void WriteAuthFields(Message message, ref string logString)
        {
            logString += " AUTH";
            if (message.Protocol == Message.ProtocolType.UDP)
                logString += $" MessageId={message.Fields.MessageId}";
            logString += $" Username={message.Fields.Username}";
            logString += $" DisplayName={message.Fields.DisplayName}";
            logString += $" Secret={message.Fields.Secret}";
        }
        private static void WriteJoinFields(Message message, ref string logString)
        {
            logString += " JOIN";
            if (message.Protocol == Message.ProtocolType.UDP)
                logString += $" MessageId={message.Fields.MessageId}";
            logString += $" ChannelId={message.Fields.ChannelId}";
            logString += $" DisplayName={message.Fields.DisplayName}";
        }
        private static void WriteMsgFields(Message message, ref string logString)
        {
            logString += " MSG";
            if (message.Protocol == Message.ProtocolType.UDP)
                logString += $" MessageId={message.Fields.MessageId}";
            logString += $" DisplayName={message.Fields.DisplayName}";
            logString += $" MessageContent={message.Fields.MessageContent}";
        }
        private static void WriteByeFields(Message message, ref string logString)
        {
            logString += " BYE";
            if (message.Protocol == Message.ProtocolType.UDP)
                logString += $" MessageId={message.Fields.MessageId}";
        }

        public static void LogMessage(IPAddress ip, ushort port, bool received, Message message)
        {
            var logString = string.Empty;
            if (received)
                logString += "RECV";
            else
                logString += "SENT";

            logString += $" {ip.ToString()}:{port} |";

            switch (message.TypeOfMessage)
            {
                case Message.MessageType.ERR:
                    WriteErrFields(message, ref logString);
                    break;
                case Message.MessageType.CONFIRM:
                    WriteConfirmFields(message, ref logString);
                    break;
                case Message.MessageType.REPLY:
                    WriteReplyFields(message, ref logString);
                    break;
                case Message.MessageType.AUTH:
                    WriteAuthFields(message, ref logString);
                    break;
                case Message.MessageType.JOIN:
                    WriteJoinFields(message, ref logString);
                    break;
                case Message.MessageType.MSG:
                    WriteMsgFields(message, ref logString);
                    break;
                case Message.MessageType.BYE:
                    WriteByeFields(message, ref logString);
                    break;
            }

            logString += "\n";
            Console.Write(logString);
        }
    }
}
