/******************************************************************************
 *  IPK-2024-2
 *  Program.cs
 *  Authors:        Nikita Kotvitskiy (xkotvi01)
 *  Description:    An entry point with CLA processing, general channel
 *                  creation and server starting
 *  Last change:    15.04.23
 *****************************************************************************/

using ipk24chat_server.inner;
using ipk24chat_server.modules;

namespace ipk24chat_server
{
    public abstract class Program
    {
        static void Main(string[] args)
        {
            // Process CLA. If arguments are invalid or help message was required, print help message
            if (!Cla.ProcessCla(args))
                Console.WriteLine(Cla.HelpMessage);

            // Create instant general channel
            _ = new Channel("general", true);
            
            // Create an object of welcome session
            var welcomeSession = new WelcomeSession();

            // Delegate for right exit from application
            Console.CancelKeyPress += delegate (object? _, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                welcomeSession.FinishSemaphore.Release();
            };

            // Starting welcome session
            welcomeSession.StartWelcomeSession();
        }
    }
}
