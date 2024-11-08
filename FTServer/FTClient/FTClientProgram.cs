// FTClientProgram.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using PRSLib;

namespace FTClient
{
    class FTClientProgram
    {
        private static void Usage()
        {
            /*
                -prs <PRS IP address>:<PRS port>
                -s <file transfer server IP address>
                -d <directory requested>
            */
            Console.WriteLine("Usage: FTClient -d <directory> [-prs <PRS IP>:<PRS port>] [-s <FT Server IP>]");
        }

        static void Main(string[] args)
        {
            // Defaults
            string PRSSERVER_IPADDRESS = "127.0.0.1";
            ushort PSRSERVER_PORT = 30000;
            string FTSERVICE_NAME = "FT Server";
            string FTSERVER_IPADDRESS = "127.0.0.1";
            ushort FTSERVER_PORT = 40000;
            string DIRECTORY_NAME = null;

            // Process command-line arguments
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-prs":
                        if (i + 1 < args.Length)
                        {
                            string[] prsParts = args[++i].Split(':');
                            PRSSERVER_IPADDRESS = prsParts[0];
                            PSRSERVER_PORT = ushort.Parse(prsParts[1]);
                        }
                        break;
                    case "-s":
                        if (i + 1 < args.Length)
                        {
                            FTSERVER_IPADDRESS = args[++i];
                        }
                        break;
                    case "-d":
                        if (i + 1 < args.Length)
                        {
                            DIRECTORY_NAME = args[++i];
                        }
                        break;
                    default:
                        Usage();
                        return;
                }
            }

            // Display configuration
            Console.WriteLine("PRS Address: " + PRSSERVER_IPADDRESS);
            Console.WriteLine("PRS Port: " + PSRSERVER_PORT);
            Console.WriteLine("FT Server Address: " + FTSERVER_IPADDRESS);
            Console.WriteLine("Directory: " + DIRECTORY_NAME);

            if (string.IsNullOrEmpty(DIRECTORY_NAME))
            {
                Console.WriteLine("Error: Directory not specified. Use the -d option to provide a directory.");
                Usage();
                return;
            }

            try
            {
                // Contact PRS server to lookup port for "FT Server"
                PRSClient prs = new PRSClient(PRSSERVER_IPADDRESS, PSRSERVER_PORT, FTSERVICE_NAME);
                FTSERVER_PORT = prs.LookupPort();  // Ensures correct port

                // Create and connect FTClient to the FT Server
                FTClient ft = new FTClient(FTSERVER_IPADDRESS, FTSERVER_PORT);
                ft.Connect();

                // Retrieve directory contents
                ft.GetDirectory(DIRECTORY_NAME);

                // Disconnect from FT Server
                ft.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            // Await user input before closing
            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }
    }
}
