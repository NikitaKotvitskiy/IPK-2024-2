using System.Collections.Generic;
using System.Net;

namespace ipk24chat_server.inner
{
    public static class Cla
    {
        public static IPAddress ListeningIp { get; private set; } = null!;
        public static ushort ListeningPort { get; private set; }
        public static ushort UdpTimeout { get; private set; }
        public static byte UdpMaxRetransmissions { get; private set; }

        public const string HelpMessage = "The following command line arguments set is acceptable:\n" +
                                            "\t-l [IP] - sets server listening IP address for welcome sockets (0.0.0.0 by default)\n" +
                                            "\t-p [Port] - sets server listening port for welcome sockets (4567 by default)\n" +
                                            "\t-d [ms] - sets UDP confirmation timeout in milliseconds (250 by default)\n" +
                                            "\t-r [count] - sets maximum number of UDP retrasmissions (3 by default)\n" +
                                            "\t-h - writes this help message\n";

        public static bool ProcessCla(string[] args)
        {
            ToDefault();

            var argIndex = 0;
            var currentArg = GetNext();

            while (!String.IsNullOrEmpty(currentArg))
            {
                switch (currentArg)
                {
                    case "-l":
                        currentArg = GetNext();
                        if (!IPAddress.TryParse(currentArg, out var newListeningIp))
                            return false;
                        ListeningIp = newListeningIp;
                        break;
                    case "-p":
                        currentArg = GetNext();
                        if (!ushort.TryParse(currentArg, out var newListeningPort))
                            return false;
                        ListeningPort = newListeningPort;
                        break;
                    case "-d":
                        currentArg = GetNext();
                        if (!ushort.TryParse(currentArg, out var newUdpTimeout))
                            return false;
                        UdpTimeout = newUdpTimeout;
                        break;
                    case "-r":
                        currentArg = GetNext();
                        if (!byte.TryParse(currentArg, out var newUdpMaxRetransmissions))
                            return false;
                        UdpMaxRetransmissions = newUdpMaxRetransmissions;
                        break;
                    case "-h":
                    default:
                        return false;
                }

                currentArg = GetNext();
            }

            return true;

            string? GetNext() => argIndex < args.Length ? args[argIndex++] : null;
        }

        private static void ToDefault()
        {
            ListeningIp = IPAddress.Parse("0.0.0.0");
            ListeningPort = 4567;
            UdpTimeout = 250;
            UdpMaxRetransmissions = 3;
        }
    }
}
