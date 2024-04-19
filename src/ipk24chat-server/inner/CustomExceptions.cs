/******************************************************************************
 *  IPK-2024-2
 *  CustomExceptions.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    Definition of custom server exceptions which are used to
 *                  detect problem situations and for debugging
 *  Last change:    08.04.23
 *****************************************************************************/

namespace ipk24chat_server.inner
{
    public class ProtocolException(string message, string expected, string got, bool crutual = false)
        : Exception(message)
    {
        public string Expected { get; private set; } = expected;
        public string Got { get; private set; } = got;
        public bool Crutual { get; private set; } = crutual;
    }

    public class ProgramException(string message, string place) : Exception(message)
    {
        public string Place { get; private set; } = place;
    }
}
