// FTClient.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace FTClient
{
    class FTClient
    {
        private string ftServerAddress;
        private ushort ftServerPort;
        bool connected;
        Socket clientSocket;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;

        public FTClient(string ftServerAddress, ushort ftServerPort)
        {
            // TODO: FTClient.FTClient()

            // save server address/port
            this.ftServerAddress = ftServerAddress;
            this.ftServerPort = ftServerPort;

            // initialize to not connected to server
            connected = false;
            clientSocket = null;
            stream = null;
            reader = null;
            writer = null;

        }

        public void Connect()
        {
            // TODO: FTClient.Connect()

            if (!connected)
            {
                Console.WriteLine("FTClient Connecting");
                // create a client socket and connect to the FT Server's IP address and port
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(IPAddress.Parse(ftServerAddress), ftServerPort);

                // establish the network stream, reader and writer
                stream = new NetworkStream(clientSocket);
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);

                // now connected
                connected = true;

                Console.WriteLine("FTClient now Connected");
            }
        }

        public void Disconnect()
        {
            // TODO: FTClient.Disconnect()

            if (connected)
            {
                Console.WriteLine("FTClient Disconnecting");


                SendExit();
                // close the reader, writer and stream
                reader.Close();
                writer.Close();
                stream.Close();

                // close the socket
                clientSocket.Disconnect(false);
                clientSocket.Close();

                // no longer connected
                connected = false;
                clientSocket = null;
                stream = null;
                writer = null;
                reader = null;

            }
        }

        public void GetDirectory(string directory)
        {
            // TODO: FTClient.GetDirectory()

            if (connected)
            {
                // send the get command with directory to the server
                SendGet(directory);

                // receive and save each file in the directory
                while (ReceiveFile(directory))
                {
                    Console.WriteLine("FTClient recieved one file");
                }
                Console.WriteLine("FTClient no more files");
            }
        }

        #region implementation

        private void SendGet(string directory)
        {
            // TODO: FTClient.SendGet()

            if (connected)
            {
                // send the get command to the server
                writer.WriteLine($"get\n" + directory + "\n");
                writer.Flush();
                Console.WriteLine("FTClient sent get for: " + directory);
            }
        }

        public void SendExit()
        {
            // TODO: FTClient.SendExit()
            // send the exit command to the server
            writer.WriteLine("exit\n");
            writer.Flush();
            Console.WriteLine("FTClient Sent Exit to Server");

        }

        public void SendInvalidMessage()
        {
            // TODO: FTClient.SendInvalidMessage()

            if (connected)
            {
                // send an invalid message to the server
                writer.WriteLine("invalid\n");
            }
        }

        private bool ReceiveFile(string directoryName)
        {
            // TODO: FTClient.ReceiveFile()
            // receive a single file from the server and save it locally in the specified directory
            // expect file name from server
            string cmd = reader.ReadLine();

            // when the server sends "done", then there are no more files!
            if (cmd == "done")
            {
                Console.WriteLine("FTClient received done from server");
                return false;
            }
            else if (cmd == "error")
            {
                // TODO: handle error messages from the server
                Console.WriteLine("FTClient received error from server");
                return false;
            }
            else
            {
                // received a file name
                string fileName = cmd;

                // receive file length from server
                // TODO: error check that this is actually an integer
                int fileLength = int.Parse(reader.ReadLine());

                Console.WriteLine("FTClient received file name: " + fileName + " with length " + fileLength.ToString());

                // read the file content based on the specified length
                char[] buffer = new char[fileLength];
                reader.Read(buffer, 0, fileLength);
                string fileContent = new string(buffer);

                // ensure the directory exists and save the file locally
                Directory.CreateDirectory(directoryName);
                string filePath = Path.Combine(directoryName, fileName);
                File.WriteAllText(filePath, fileContent);

                Console.WriteLine("FTClient saved file to " + filePath);
                return true;
            }
        }



        #endregion
    }
}
