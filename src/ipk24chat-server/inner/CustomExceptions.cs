namespace ipk24chat_server.inner
{
    public class ProtocolException : Exception
    {
        public string Expected { get; private set; }
        public string Got { get; private set; }
        public bool Crutual { get; private set; }
        public ProtocolException(string message, string expected, string got, bool crutual = false) : base(message) 
        {
            Expected = expected;
            Got = got;
            Crutual = crutual;
        } 
    }

    public class ProgramException : Exception
    {
        public string Place { get; private set; }
        
        public ProgramException(string message, string place) : base(message)
        {
            Place = place;
        }
    }
}
