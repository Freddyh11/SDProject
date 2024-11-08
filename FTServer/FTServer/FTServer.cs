// FTServer.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace FTServer
{
    class FTServer
    {
        private ushort listeningPort;
        private int clientBacklog;

        // Declare the listening socket at the class level
        private Socket listeningSocket;

        public FTServer(ushort listeningPort, int clientBacklog)
        {
            this.listeningPort = listeningPort;
            this.clientBacklog = clientBacklog;
        }

        public void Start()
        {
            Console.WriteLine("FTServer started...");

            // Initialize the listening socket
            listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, listeningPort));
            listeningSocket.Listen(clientBacklog);

            Console.WriteLine("FTServer Listening...");
            bool done = false;

            while (!done)
            {
                try
                {
                    Console.WriteLine("FTServer waiting for client connection...");
                    // Accept a client connection
                    Socket clientSocket = listeningSocket.Accept();
                    Console.WriteLine("FTServer Accepted client connection");

                    // Instantiate connected client to process messages
                    FTConnectedClient client = new FTConnectedClient(clientSocket);
                    client.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while accepting and starting client: " + ex.Message);
                    Console.WriteLine("Waiting for 5 seconds and trying again...");
                    Thread.Sleep(5000);
                }
            }

            // Close socket and quit
            listeningSocket.Close();
        }
    }
}
