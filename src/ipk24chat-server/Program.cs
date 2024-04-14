using ipk24chat_server.inner;
using ipk24chat_server.modules;
using System;

namespace ipk24chat_server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (!Cla.ProcessCla(args))
                Console.WriteLine(Cla.HelpMessage);

            var welcomeSession = new WelcomeSession();

            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) // Delegate for right exit from application
            {
                e.Cancel = true;
                welcomeSession.FinishSemaphore.Release();
            };

            welcomeSession.StartWelcomeSession();

            return;
        }
    }
}
