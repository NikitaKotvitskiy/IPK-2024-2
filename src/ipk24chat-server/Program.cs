using ipk24chat_server.inner;

namespace ipk24chat_server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (!Cla.ProcessCla(args))
                Console.WriteLine(Cla.HelpMessage);

            return;
        }
    }
}
