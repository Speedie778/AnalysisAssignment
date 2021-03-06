﻿/*** Fill these lines with your complete information.
 * Note: Incomplete information may result in FAIL.
 * Mameber 1: [Delano Brouwer, first member]:
 * Mameber 2: [Bastiaan Swam, second member]:
 * Std Number 1: [0960339]
 * Std Number 2: [0966726]
 * Class: [INF2C]
 ***/


using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
/* Note: If you are using .net core 2.1, install System.Text.Json (use NuGet). */
using System.Text.Json; 
using System.Threading;

namespace SocketServer
{
    public class ClientInfo
    {
        public string studentnr { get; set; }
        public string classname { get; set; }
        public int clientid { get; set; }
        public string teamname { get; set; }
        public string ip { get; set; }
        public string secret { get; set; }
        public string status { get; set; }
    }

    public class Message
    {
        public const string welcome = "WELCOME";
        public const string stopCommunication = "COMC-STOP";
        public const string statusEnd = "STAT-STOP";
        public const string secret = "SECRET";
    }

    public class SequentialServer
    {
        public Socket listener;
        public IPEndPoint localEndPoint;
        public IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        public readonly int portNumber = 11111;

        public String results = "";
        public LinkedList<ClientInfo> clients = new LinkedList<ClientInfo>();

        private Boolean stopCond = false;
        private int processingTime = 1000;
        private int listeningQueueSize = 5;
        
        public void prepareServer()
        {
            byte[] bytes = new Byte[1024];
            String data = null;
            int numByte = 0;
            string replyMsg = "";
            bool stop;

            try
            {
                Console.WriteLine("[Server] is ready to start ...");
                // Establish the local endpoint
                localEndPoint = new IPEndPoint(ipAddress, portNumber);
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Console.Out.WriteLine("[Server] A socket is established ...");
                // associate a network address to the Server Socket. All clients must know this address
                listener.Bind(localEndPoint);
                // This is a non-blocking listen with max number of pending requests
                listener.Listen(listeningQueueSize);
                while (true)
                {
                    Console.WriteLine("Waiting connection ... ");
                    // Suspend while waiting for incoming connection 

                    Socket connection = listener.Accept();
                    this.sendReply(connection, Message.welcome);

                    stop = false;
                    while (!stop)
                    {
                        numByte = connection.Receive(bytes);
                        data = Encoding.ASCII.GetString(bytes, 0, numByte);
                        replyMsg = processMessage(data);
                        if (replyMsg.Equals(Message.stopCommunication))
                        {
                            stop = true;
                            break;
                        }
                        else
                            this.sendReply(connection, replyMsg);
                    }

                }

            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
        }
        public void handleClient(Socket con)
        {
        }
        public string processMessage(String msg)
        {
            Thread.Sleep(processingTime);
            Console.WriteLine("[Server] received from the client -> {0} ", msg);
            string replyMsg = "";

            try
            {
                switch (msg)
                {
                    case Message.stopCommunication:
                        replyMsg = Message.stopCommunication;
                        break;
                    default:
                        ClientInfo c = JsonSerializer.Deserialize<ClientInfo>(msg.ToString());
                        clients.AddLast(c);
                        if (c.clientid == -1)
                        {
                            stopCond = true;
                            exportResults();
                        }
                        c.secret = c.studentnr + Message.secret;
                        c.status = Message.statusEnd;
                        replyMsg = JsonSerializer.Serialize<ClientInfo>(c);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Server] processMessage {0}", e.Message);
            }

            return replyMsg;
        }
        public void sendReply(Socket connection, string msg)
        {
            byte[] encodedMsg = Encoding.ASCII.GetBytes(msg);
            connection.Send(encodedMsg);
        }
        public void exportResults()
        {
            if (stopCond)
            {
                this.printClients();
            }
        }
        public void printClients()
        {
            string delimiter = " , ";
            Console.Out.WriteLine("[Server] This is the list of clients communicated");
            foreach (ClientInfo c in clients)
            {
                Console.WriteLine(c.classname + delimiter + c.studentnr + delimiter + c.clientid.ToString());
            }
            Console.Out.WriteLine("[Server] Number of handled clients: {0}", clients.Count);

            clients.Clear();
            stopCond = false;

        }
    }


    public class ConcurrentServer : SequentialServer
    {

        byte[] bytes;
        String data;
        int numByte;
        string replyMsg;
        bool stop;
        Thread[] threads = new Thread[250];
        private Boolean stopCond = false;
        private int processingTime = 1000;
        private int listeningQueueSize = 5;
        private Mutex mut = new Mutex();
        private Mutex mut2 = new Mutex();
        

        public void printClients()
        {
            string delimiter = " , ";
            Console.Out.WriteLine("[Server] This is the list of clients communicated");
            foreach (ClientInfo c in clients)
            {
                Console.WriteLine(c.classname + delimiter + c.studentnr + delimiter + c.clientid.ToString());
            }
            Console.Out.WriteLine("[Server] Number of handled clients: {0}", clients.Count);

            clients.Clear();
            stopCond = false;

        }

        public void exportResults()
        {
            if (stopCond) // By redefining the function here, the function looks at the correct stopCond.
            {
                this.printClients();
            }
        }   

        public string processMessage(String msg)
        {
            Thread.Sleep(processingTime);
            Console.WriteLine("[Server] received from the client -> {0} ", msg);
            string replyMsg = "";

            try
            {
                switch (msg)
                {
                    case Message.stopCommunication:
                        replyMsg = Message.stopCommunication;
                        return replyMsg;
                    default:
                        ClientInfo c = JsonSerializer.Deserialize<ClientInfo>(msg.ToString());
                        mut2.WaitOne();
                        clients.AddLast(c);
                        mut2.ReleaseMutex();
                        if (c.clientid == -1)
                        {
                            stopCond = true;
                            exportResults();
                        }
                        c.secret = c.studentnr + Message.secret;
                        c.status = Message.statusEnd;
                        replyMsg = JsonSerializer.Serialize<ClientInfo>(c);
                        return replyMsg;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Server] processMessage {0}", e.Message);
            }

            return replyMsg;
        }

        public void communicate() { 
            bool locStop = false;

            while (!locStop)
            {
                try{
                    mut.WaitOne();
                    Socket connection = listener.Accept();
                    
                    this.sendReply(connection, Message.welcome);
                    
                    numByte = connection.Receive(bytes);
                    
                    data = Encoding.ASCII.GetString(bytes, 0, numByte);
                    mut.ReleaseMutex();
                    replyMsg = processMessage(data);
                    if (replyMsg.Equals(Message.stopCommunication))
                    {
                        locStop = true;
                        break;
                    }
                    else
                        this.sendReply(connection, replyMsg);
                }
                catch{
                    //listener doesn't listen anymore, so catch the error and break from the loop
                    break;
                }
            }

        }
        public void prepareServer()
        {
            bytes = new Byte[1024];
            data = null;
            numByte = 0;
            replyMsg = "";
            stopCond = false;

            try
            {
                
                for(int i = 0; i < 250; i++) {
                    threads[i] = new Thread(communicate);
                    threads[i].IsBackground = true;
                }
                Console.WriteLine("[Server] is ready to start ...");
                // Establish the local endpoint
                localEndPoint = new IPEndPoint(ipAddress, portNumber);
                listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Console.Out.WriteLine("[Server] A socket is established ...");
                // associate a network address to the Server Socket. All clients must know this address
                listener.Bind(localEndPoint);
                // This is a non-blocking listen with max number of pending requests
                listener.Listen(listeningQueueSize);
                Console.WriteLine("Waiting connection ... ");
                while(!stopCond){
                
                    
                    // Suspend while waiting for incoming connection 
                    for(int i = 0; i < 250; i++) {
                        if(!threads[i].IsAlive){
                            threads[i] = new Thread(communicate);
                            threads[i].Start();
                        }
                        
                    }
                
                }
                listener.Close();
                //listener gets closed here, so exportResults() here
                stopCond = true;
                for(int i = 0; i < threads.Length; i++){threads[i].Join(2000);} //Prevents errors in exportResults. Timeout after 2000ms.
                exportResults();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
        }

    }

    public class ServerSimulator
    {
        public static void sequentialRun()
        {
            Console.Out.WriteLine("[Server] A sample server, sequential version ...");
            SequentialServer server = new SequentialServer();
            server.prepareServer();
        }
        public static void concurrentRun()
        {
            Console.Out.WriteLine("[Server] A sample server, concurrent version ...");
            ConcurrentServer cServer = new ConcurrentServer();
            cServer.prepareServer();
        }
    }
    class Program
    {
        // Main Method 
        static void Main(string[] args)
        {
            //ServerSimulator.sequentialRun();
            ServerSimulator.concurrentRun();
        }

    }
}
