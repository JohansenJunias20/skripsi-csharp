using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;      //required
using System.Net.Sockets;    //required
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class TCPServer
    {
        public struct Config
        {
            public int port;
        }
        int PORT;
        public TcpListener server;
        public TCPServer(Config config)
        {
            PORT = config.port;
            server = new TcpListener(IPAddress.Any, PORT);
            // we set our IP address as server's address, and we also set the port: 9999

            server.Start();  // this will start the server
        }
        public void runServer()
        {
            Task.Run(() =>
            {

                while (true)   //we wait for a connection
                {
                    Console.WriteLine("waiting for client TCP...");
                    TcpClient client = server.AcceptTcpClient();  //if a connection exists, the server will accept it
                    Console.WriteLine("TCP client connected...");
                    while (true)  //while the client is connected, we look for incoming messages
                    {

                        NetworkStream ns = client.GetStream(); //networkstream is used to send/receive messages
                        byte[] msg = new byte[1024];     //the messages arrive as byte array
                        int n = ns.Read(msg, 0, msg.Length);   //the same networkstream reads the message sent by the client
                        if (n == 0) //NOT DETECTED
                        {
                            Console.WriteLine("client disconnected");
                            //disconnected
                            break;
                        }
                        onReceive?.Invoke(Encoding.UTF8.GetString(msg));
                        //convert msg bytes to string
                        Console.WriteLine($"`{Encoding.Default.GetString(msg)}`");
                    }
                    Console.WriteLine("client disconnected");
                }
            }
            );
        }
        public delegate void Notify(string data);
        public event Notify onReceive;

    }
}