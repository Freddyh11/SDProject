// SDConnectedClient.cs
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

namespace SDServer
{
    class SDConnectedClient
    {
        // represents a single connected sd client
        // each client will have its own socket and thread while its connected
        // client is given it's socket from the SDServer when the server accepts the connection
        // this class creates it's own thread
        // the client's thread will process messages on the client's socket until it disconnects
        // NOTE: an sd client can connect/send messages/disconnect many times over it's lifetime

        private Socket clientSocket;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread clientThread;
        private SessionTable sessionTable;      // server's session table
        private ulong sessionId;                // session id for this session, once opened or resumed

        public SDConnectedClient(Socket clientSocket, SessionTable sessionTable)
        {
            this.clientSocket = clientSocket;

            stream = null;
            writer = null;
            reader = null;

            this.sessionTable = sessionTable;

            sessionId = 0;
        }

        public void Start()
        {

            clientThread = new Thread(ThreadProc);
            clientThread.Start(this);

        }

        private static void ThreadProc(Object param)
        {

            (param as SDConnectedClient).Run();

        }

        private void Run()
        {
            // this method is executed on the clientThread

            try
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Client Connected");
                // create network stream, reader and writer over the socket
                stream = new NetworkStream(clientSocket);
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);
                // process client requests
                bool done = false;
                while (!done)
                {
                    // receive a message from the client
                    string msg = reader.ReadLine();
                    if (msg == null)
                    {
                        // no message means the client disconnected
                        // remember that the client will connect and disconnect as desired
                        Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Client Disconnected");
                        done = true;
                    }
                    else
                    {
                        // handle the message
                        Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Received MSG: " + msg);
                        switch (msg)
                        {
                            case "open":
                                HandleOpen();
                                break;

                            case "resume":
                                HandleResume();
                                break;

                            case "close":
                                HandleClose();
                                break;

                            case "get":
                                HandleGet();
                                break;

                            case "post":
                                HandlePost();
                                break;

                            default:
                                {
                                    // Log the invalid message event for debugging
                                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Error on client socket: Invalid command received.");

                                    // Respond with an error message to the client for the invalid command
                                    writer.WriteLine("error\nInvalid command\n");
                                    writer.Flush();
                                }
                                break;
                        }
                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Error on client socket, closing connection: " + se.Message);
            }
            catch (IOException ioe)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "IO Error on client socket, closing connection: " + ioe.Message);
            }

            writer.Close();
            reader.Close();
            stream.Close();
            clientSocket.Disconnect(false);
            clientSocket.Close();
        }

        private void HandleOpen()
        {
            // handle an "open" request from the client

            // if no session currently open, then...
            if (sessionId == 0)
            {
                try
                {
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Opening session for client...");
                    // ask the SessionTable to open a new session and save the session ID
                    sessionId = sessionTable.OpenSession();
                    // send accepted message, with the new session's ID, to the client
                    SendAccepted(sessionId);
                }
                catch (SessionException se)
                {
                    SendError(se.Message);
                }
                catch (Exception ex)
                {
                    SendError(ex.Message);
                }
            }
            else
            {
                // error!  the client already has a session open!
                SendError("Session already open!");
            }
        }

        private void HandleResume()
        {
            // handle a "resume" request from the client

            // get the sessionId that the client just asked us to resume
            ulong resumeSessionId = ulong.Parse(reader.ReadLine());
            try
            {
                // if we don't have a session open currently for this client...
                if (sessionId == 0)
                {
                    // try to resume the session in the session table
                    if (sessionTable.ResumeSession(resumeSessionId))
                    {
                        // if success, remember the session that we're now using and send accepted to client
                        this.sessionId = resumeSessionId;
                        SendAccepted(sessionId);
                    }
                    else
                    {
                        // if failed to resume session, send rejectetd to client
                        SendRejected("Unable to resume session");
                    }
                }
                else
                {
                    // error! we already have a session open
                    SendError("Session already open, cannot resume!");
                }
            }
            catch (SessionException se)
            {
                SendError(se.Message);
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void HandleClose()
        {
            // handle a "close" request from the client

            // get the sessionId that the client just asked us to close
            ulong closedSessionID = ulong.Parse(reader.ReadLine());
            try
            {
                // close the session in the session table
                sessionTable.CloseSession(closedSessionID);
                // send closed message back to client
                SendClosed(closedSessionID);
                // record that this client no longer has an open session
                sessionId = 0;

            }
            catch (SessionException se)
            {
                SendError(se.Message);
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void HandleGet()
        {
            // handle a "get" request from the client
            if (sessionId != 0)
            {
                try
                {
                    // Get the document name from the client
                    string documentName = reader.ReadLine();
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "]" + "Getting from: " + documentName);

                    // Retrieve the document content from the session table
                    string documentContent = sessionTable.GetSessionValue(sessionId, documentName);

                    // Send success and document to the client
                    SendSuccess(documentName, documentContent);
                }
                catch (SessionException se)
                {
                    SendError(se.Message);
                }
                catch (Exception ex)
                {
                    SendError(ex.Message);
                }
            }
            else
            {
                SendError("No Session open, Cannot get document");
            }
        }


        private void HandlePost()
        {
            // handle a "post" request from the client
            // if the client has a session open
            if (sessionId != 0)
            {
                try
                {
                    // get the document name, content length and contents from the client
                    string documentName = reader.ReadLine();
                    int documentLength = int.Parse(reader.ReadLine());
                    string documentContent = ReceiveDocument(documentLength);
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "]" + "Posting to: " + documentName);
                    // put the document into the session
                    sessionTable.PutSessionValue(sessionId, documentName, documentContent);
                    // send success to the client
                    SendSuccess();
                }
                catch (SessionException se)
                {
                    SendError(se.Message);
                }
                catch (Exception ex)
                {
                    SendError(ex.Message);
                }
            }
            else
            {
                // error, cannot post without a session
                SendError("No Session open, Cannot post");
            }
        }

        private void SendAccepted(ulong sessionId)
        {
            // send accepted message to SD client, including session id of now open session
            writer.Write("accepted\n" + sessionId.ToString() + "\n");
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Sent accepted to client: " + sessionId.ToString());

        }

        private void SendRejected(string reason)
        {
            // send rejected message to SD client, including reason for rejection
            writer.Write("rejected\n" + reason + "\n");
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Sent rejected to client: " + reason);
        }

        private void SendClosed(ulong sessionId)
        {
            // send closed message to SD client, including session id that was just closed
            writer.Write("Closed\n" + sessionId.ToString() + "\n");
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Sent closed to client: " + sessionId.ToString());
        }

        private void SendSuccess()
        {
            // send sucess message to SD client, with no further info
            // NOTE: in response to a post request
            writer.Write("Success\n");
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Sent Success!");
        }

        private void SendSuccess(string documentName, string documentContent)
        {
            // send success message to SD client, including retrieved document name, length and content
            // NOTE: in response to a get request
            writer.Write("success\n" + documentName + "\n" + documentContent.Length.ToString() + "\n" + documentContent);
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Sent Success: " + documentName + ", " + documentContent.Length.ToString() + ", " + documentContent);
        }

        private void SendError(string errorString)
        {
            // send error message to SD client, including error string
            writer.Write("error\n" + errorString + "\n");
            writer.Flush();
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Sent error to client: " + errorString);

        }

        private string ReceiveDocument(int length)
        {
            // receive a document from the SD client, of expected length
            // NOTE: as part of processing a post request

            // read from the reader until we've received the expected number of characters
            // accumulate the characters into a string and return those when we got enough
            int charsToRead = length;
            string contents = "";
            while (charsToRead > 0)
            {
                char[] buffer = new char[charsToRead];
                int charsActuallyRead = reader.Read(buffer, 0, buffer.Length);
                charsToRead -= charsActuallyRead;
                contents += new string(buffer);

            }
            Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "]" + "Received contents: " + contents);

            return contents;
        }
    }
}
