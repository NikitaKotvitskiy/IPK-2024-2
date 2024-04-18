/******************************************************************************
 *  IPK-2024-2
 *  FormatChecking.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    Checking message fields with regular expressions
 *  Last change:    10.04.23
 *****************************************************************************/

using System.Text.RegularExpressions;

namespace ipk24chat_server.messages
{
    public abstract class FormatChecking
    {
        private const string nonZeroPattern = @"^[A-Za-z0-9\-\.]+$";
        private const string printablePattern = @"^[\x21-\x7E]+$";
        private const string printableWithSpacePattern = @"^[\x20-\x7E]+$";

        private static bool CheckType(string str, int maxLength, string pattern) =>
            (str.Length <= maxLength && Regex.IsMatch(str, pattern));

        public static bool CheckUserName(string str) => CheckType(str, 20, nonZeroPattern);
        public static bool CheckChannelId(string str) => CheckType(str, 20, nonZeroPattern);
        public static bool CheckSecret(string str) => CheckType(str, 128, nonZeroPattern);
        public static bool CheckDisplayName(string str) => CheckType(str, 20, printablePattern);
        public static bool CheckMessageContent(string str) => CheckType(str, 1400, printableWithSpacePattern);
    }
}
