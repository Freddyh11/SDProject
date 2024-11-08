// SDServerProgram.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using PRSLib;

namespace SDServer
{
    class SDServerProgram
    {
        private static void Usage()
        {
            Console.WriteLine("Usage: SDServer -prs <PRS IP address>:<PRS port>");
        }

        static void Main(string[] args)
        {
            // defaults
            ushort SDSERVER_PORT = 40000;
            int CLIENT_BACKLOG = 5;
            string PRS_ADDRESS = "127.0.0.1";
            ushort PRS_PORT = 30000;
            string SERVICE_NAME = "SD Server";

            // Diagnostic: Print received arguments
            Console.WriteLine("Received arguments:");
            foreach (var arg in args)
            {
                Console.WriteLine(arg);
            }

            // Process command-line arguments
            if (args.Length == 2 && args[0] == "-prs")
            {
                var prsParts = args[1].Split(':');
                if (prsParts.Length == 2 && ushort.TryParse(prsParts[1], out PRS_PORT))
                {
                    PRS_ADDRESS = prsParts[0];
                }
                else
                {
                    Console.WriteLine("Invalid PRS format. Expected: <IP>:<Port>");
                    Usage();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Invalid arguments.");
                Usage();
                return;
            }

            Console.WriteLine("PRS Address: " + PRS_ADDRESS);
            Console.WriteLine("PRS Port: " + PRS_PORT);

            try
            {
                PRSClient prs = new PRSClient(PRS_ADDRESS, PRS_PORT, SERVICE_NAME);
                SDSERVER_PORT = prs.RequestPort();
                Console.WriteLine("Server listening on port " + SDSERVER_PORT);
                prs.KeepPortAlive();

                SDServer sd = new SDServer(SDSERVER_PORT, CLIENT_BACKLOG);
                sd.Start();

                prs.ClosePort();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }

    }
}
