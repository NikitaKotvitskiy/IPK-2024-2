/******************************************************************************
 *  IPK-2024-2
 *  Logging.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    Algorithm for building log messages with source/destination
 *                  address and IPK24-CHAT protocol message content
 *  Last change:    11.04.23
 *****************************************************************************/

using ipk24chat_server.messages;
using System.Net;

namespace ipk24chat_server.inner
{
    public abstract class Logging
    {
        // Adds fields from ERR message to the log string
        private static void WriteErrFields(Message message, ref string logString)
        {
            logString += " ERR";
            if (message.Protocol == Message.ProtocolType.Udp)
                logString += $" MessageId={message.Fields.MessageId}";
            logString += $" DisplayName={message.Fields.DisplayName}";
            logString += $" MessageContent={message.Fields.MessageContent}";
        }
        
        // Adds fields from CONFIRM message to the log string
        private static void WriteConfirmFields(Message message, ref string logString)
        {
            logString += " CONFIRM";
            logString += $" RefMessageId={message.Fields.MessageRefId}";
        }
        
        // Adds fields from REPLY message to the log string
        private static void WriteReplyFields(Message message, ref string logString)
        {
            logString += " REPLY";
            if (message.Protocol == Message.ProtocolType.Udp)
            {
                logString += $" MessageId={message.Fields.MessageId}";
                logString += $" RefMessageId={message.Fields.MessageRefId}";
            }
            logString += $" Result={message.Fields.Result}";
            logString += $" MessageContent={message.Fields.MessageContent}";
        }
        
        // Adds fields from AUTH message to the log string
        private static void WriteAuthFields(Message message, ref string logString)
        {
            logString += " AUTH";
            if (message.Protocol == Message.ProtocolType.Udp)
                logString += $" MessageId={message.Fields.MessageId}";
            logString += $" Username={message.Fields.Username}";
            logString += $" DisplayName={message.Fields.DisplayName}";
            logString += $" Secret={message.Fields.Secret}";
        }
        
        // Adds fields from JOIN message to the log string
        private static void WriteJoinFields(Message message, ref string logString)
        {
            logString += " JOIN";
            if (message.Protocol == Message.ProtocolType.Udp)
                logString += $" MessageId={message.Fields.MessageId}";
            logString += $" ChannelId={message.Fields.ChannelId}";
            logString += $" DisplayName={message.Fields.DisplayName}";
        }
        
        // Adds fields from MSG message to the log string
        private static void WriteMsgFields(Message message, ref string logString)
        {
            logString += " MSG";
            if (message.Protocol == Message.ProtocolType.Udp)
                logString += $" MessageId={message.Fields.MessageId}";
            logString += $" DisplayName={message.Fields.DisplayName}";
            logString += $" MessageContent={message.Fields.MessageContent}";
        }
        
        // Adds fields from BYE message to the log string
        private static void WriteByeFields(Message message, ref string logString)
        {
            logString += " BYE";
            if (message.Protocol == Message.ProtocolType.Udp)
                logString += $" MessageId={message.Fields.MessageId}";
        }

        // Gets the data from message and form a log message
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
                case Message.MessageType.Err:
                    WriteErrFields(message, ref logString);
                    break;
                case Message.MessageType.Confirm:
                    WriteConfirmFields(message, ref logString);
                    break;
                case Message.MessageType.Reply:
                    WriteReplyFields(message, ref logString);
                    break;
                case Message.MessageType.Auth:
                    WriteAuthFields(message, ref logString);
                    break;
                case Message.MessageType.Join:
                    WriteJoinFields(message, ref logString);
                    break;
                case Message.MessageType.Msg:
                    WriteMsgFields(message, ref logString);
                    break;
                case Message.MessageType.Bye:
                    WriteByeFields(message, ref logString);
                    break;
            }

            logString += "\n";
            Console.Write(logString);
        }
    }
}
