// FTConnectedClient.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace FTServer
{
    class FTConnectedClient
    {
        private Socket clientSocket;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread clientThread;

        public FTConnectedClient(Socket clientSocket)
        {
            this.clientSocket = clientSocket;
            stream = null;
            reader = null;
            writer = null;
            clientThread = null;
        }

        public void Start()
        {
            clientThread = new Thread(ThreadProc);
            clientThread.Start(this);
        }

        private static void ThreadProc(Object param)
        {
            (param as FTConnectedClient).Run();
        }

        private void Run()
        {
            try
            {
                stream = new NetworkStream(clientSocket);
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);
                
                bool done = false;
                while (!done)
                {
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] waiting for msg from client...");
                    string msg = reader.ReadLine();
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] received msg from client!");

                    if (msg == "get")
                    {
                        string directoryName = reader.ReadLine();
                        Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] get for directory: " + directoryName);
                        
                        DirectoryInfo directory = new DirectoryInfo(directoryName);
                        if (directory.Exists)
                        {
                            foreach (FileInfo fi in directory.GetFiles())
                            {
                                string fileName = fi.Name;
                                
                                if (fi.Extension == ".txt")
                                {
                                    StreamReader fileReader = fi.OpenText();
                                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] found txt file: " + fileName);
                                    string contents = fileReader.ReadToEnd();
                                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] Contents: " + contents);
                                    fileReader.Close();
                                    
                                    SendFileName(fileName, contents.Length);
                                }
                                else
                                {
                                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] found non-txt file: " + fileName);
                                }
                            }
                            SendDone();
                        }
                    }
                    else if (msg == "exit")
                    {
                        Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] Exiting...");
                        done = true;
                    }
                    else
                    {
                        Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] Received invalid message from client: " + msg);
                        SendError("Invalid command received from client");
                        done = true;
                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] Error on client socket, closing connection: " + se.Message);
            }

            writer.Close();
            reader.Close();
            stream.Close();
            clientSocket.Disconnect(false);
            clientThread = null;

            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] Disconnected from Client");
        }

        private void SendFileName(string fileName, int fileLength)
        {
            writer.Write(fileName + "\n" + fileLength.ToString() + "\n");
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] Sent FileName to Client: " + fileName);
        }

        private void SendFileContents(string fileContents)
        {
            writer.Write(fileContents);
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] Sent File Contents: " + fileContents);
        }

        private void SendDone()
        {
            writer.WriteLine("done\n");
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] Sent done to client");
        }

        private void SendError(string errorMessage)
        {
            writer.WriteLine("error\n" + errorMessage + "\n");
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] Sent error to client: " + errorMessage);
        }
    }
}
