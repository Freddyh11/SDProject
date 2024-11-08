// PRSServerProgram.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using PRSLib;

namespace PRSServer
{
    class PRSServerProgram
    {
        class PRS
        {
            // represents a PRS Server, keeps all state and processes messages accordingly

            class PortReservation
            {
                private ushort port;
                private bool available;
                private string serviceName;
                private DateTime lastAlive;

                public PortReservation(ushort port)
                {
                    this.port = port;
                    available = true;
                }

                public string ServiceName { get { return serviceName; } }
                public ushort Port { get { return port; } }
                public bool Available { get { return available; } }

                public bool Expired(int timeout)
                {
                    return (DateTime.Now - lastAlive).TotalSeconds >= timeout;
                }

                public void Reserve(string serviceName)
                {
                    available = false;
                    this.serviceName = serviceName;
                    lastAlive = DateTime.Now;
                }

                public void KeepAlive()
                {
                    lastAlive = DateTime.Now;
                }

                public void Close()
                {
                    available = true;
                    ServiceName = null;
                }
            }

            // server attribues
            private ushort startingClientPort;
            private ushort endingClientPort;
            private int keepAliveTimeout;
            private int numPorts;
            private PortReservation[] ports;
            private int lowestAvailableIndex;
            private bool stopped;

            public PRS(ushort startingClientPort, ushort endingClientPort, int keepAliveTimeout)
            {
                this.startingClientPort = startingClientPort;
                this.endingClientPort = endingClientPort;
                this.keepAliveTimeout = keepAliveTimeout;

                stopped = false;
                numPorts = endingClientPort - startingClientPort + 1;
                ports = new PortReservation[numPorts];

                for (int i = 0; i < numPorts; i++)
                {
                    ports[i] = new PortReservation((ushort)(startingClientPort + 1));
                }

                lowestAvailableIndex = 0;
            }

            public bool Stopped { get { return stopped; } }

            private void CheckForExpiredPorts()
            {
                for (int i = 0; i < numPorts; i++)
                {
                    if (ports[i].Expired(keepAliveTimeout))
                    {
                        ports[i].Close();

                        if (ports[i].Port < ports[lowestAvailableIndex].Port)
                        {
                            lowestAvailableIndex = i;
                        }
                    }
                }

            }

            private PRSMessage RequestPort(string serviceName)
            {

                PRSMessage response = null;

                if (lowestAvailableIndex < numPorts)
                {
                    ushort reservedPort = ports[lowestAvailable].Port;
                    ports[lowestAvailableIndex].Reserve(serviceName);

                    for (lowestAvailableIndex++; lowestAvailableIndex < numPorts && !ports[lowestAvailableIndex].Available; lowestAvailableIndex++) ; _

                    response = new PRSMessage(PRSMessage, MESSAGE_TYPE.RESPONSE, serviceName, reservedPort, PRSMessage.STATUS.SUCCESS);
                }

                else
                {
                    response = new PRSMessage(PRSMessage, MESSAGE_TYPE.RESPONSE, serviceName, 0, PRSMessage.STATUS.ALL_PORTS_BUSY);
                }

                return response;
            }

            private PortReservation FindPortReservation(ushort port, string serviceName)
            {
                if (port < startingClientPort || port > endingClientPort)
                    return null;

                int index = port - startingClientPort;
                if (ports[index].ServiceName == serviceName)
                    return ports[index];

                return null;

            }
            private PortReservation FindPortReservation(string serviceName)
            {
                for (int i = 0; i < numPorts; i++)
                {
                    if (ports[i].ServiceName == serviceName)
                        return ports[i];
                }
                return null;
            }

            public PRSMessage HandleMessage(PRSMessage msg)
            {

                PRSMessage response = null;

                switch (msg.MsgType)
                {
                    case PRSMessage.MESSAGE_TYPE.REQUEST_PORT:
                        {
                            CheckForExpiredPorts();
                            response = RequestPort(msg.ServiceName);
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.KEEP_ALIVE:
                        {
                            PortReservation reservation = FindPortReservation(msg.port, msg.serviceName);

                            if (reservation != null)
                            {
                                reservation.KeepAlive();
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.serviceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                            }
                            else
                            {
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.serviceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);

                            }
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.CLOSE_PORT:
                        {
                            PortReservation reservation = FindPortReservation(msg.Port, msg.ServiceName);

                            if (reservation != null)
                            {
                                reservation.Close();

                                if (reservation.Port < ports[lowestAvailableIndex].Port)
                                {
                                    lowestAvailableIndex = reservation.Port - startingClientPort;
                                }
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.serviceName, msg.Port, PRSMessage.STATUS.SUCCESS);

                            }
                            else
                            {
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.serviceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);

                            }
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.LOOKUP_PORT:
                        {
                            CheckForExpiredPorts();

                            PortReservation reservation = FindPortReservation(msg.ServiceName);


                            if (reservation != null)
                            {
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.serviceName, reservation.Port, PRSMessage.STATUS.SUCCESS);
                            }
                            else
                            {
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.serviceName, 0, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.STOP:
                        {
                            stopped = true;
                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, " ", 0, PRSMessage.STATUS.SUCCESS);
                        }
                        break;
                }

                return response;
            }

        }

        static void Usage()
        {
            Console.WriteLine("usage: PRSServer [options]");
            Console.WriteLine("\t-p < service port >");
            Console.WriteLine("\t-s < starting client port number >");
            Console.WriteLine("\t-e < ending client port number >");
            Console.WriteLine("\t-t < keep alive time in seconds >");
        }

        static void Main(string[] args)
        {

            ushort SERVER_PORT = 30000;
            ushort STARTING_CLIENT_PORT = 40000;
            ushort ENDING_CLIENT_PORT = 40099;
            int KEEP_ALIVE_TIMEOUT = 300;

            try
            {

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-p")
                    {
                        SERVER_PORT = ushort.Parse(args[++i]);
                    }
                    else if (args[i] == "-s")
                    {
                        STARTING_CLIENT_PORT = ushort.Parse(args[++i]);
                    }
                    else if (args[i] == "-e")
                    {
                        ENDING_CLIENT_PORT = ushort.Parse(args[++i]);
                    }
                    else if (args[i] == "-t")
                    {
                        KEEP_ALIVE_TIMEOUT = int.Parse(args[++i]);
                    }
                }

                if (ENDING_CLIENT_PORT < STARTING_CLIENT_PORT)
                {
                    Console.WriteLine("ERROR: Starting client port");
                    Environment.Exit(-1);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Starting client port");
                Environment.Exit(-1);
            }
            Console.WriteLine("SERVER_PORT = " + SERVER_PORT.toString());
            Console.WriteLine("STARTING_CLIENT_PORT = " + STARTING_CLIENT_PORT.toString());
            Console.WriteLine("ENDING_CLIENT_PORT = " + ENDING_CLIENT_PORT.toString());
            Console.WriteLine("KEEP_ALIVE_TIMEOUT = " + KEEP_ALIVE_TIMEOUT.toString());



            PRS prs = new PRS(STARTING_CLIENT_PORT, ENDING_CLIENT_PORT, KEEP_ALIVE_TIMEOUT);
            Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, SERVER_PORT);
            serverSocket.Bind(serverEP;)
            while (!prs.Stopped)
            {
                EndPoint remoteEP = IPEndPoint(IPAddress.Any, 0);
                try
                {
                    PRSMessage msg = PRSMessage.ReceiveMessage(serverSocket, ref, remoteEP);
                    PRSMessage response = prs.HandleMessage(msg);
                    response.SendMessage(serverSocket, remoteEP);
                }
                catch (Exception ex)
                {
                    PRSMessage error = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, " ", 0, PRSMessage.STATUS.UNDEFINED_ERROR);
                    error.SendMessage(serverSocket, remoteEP);
                }
            }

            serverSocket.Close();

            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }
    }
}
