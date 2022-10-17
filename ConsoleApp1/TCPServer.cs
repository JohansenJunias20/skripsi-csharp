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
        private bool isClientConnected = false;
        public NetworkStream ns;
        public TcpClient client = new TcpClient();
        public void runServer()
        {

            Task.Run(() =>
            {

                while (true)   //we wait for a connection
                {
                    Console.WriteLine("waiting for client TCP...");
                    client = server.AcceptTcpClient();  //if a connection exists, the server will accept it
                    if (isClientConnected)
                    {
                        var stream = client.GetStream();
                        Console.WriteLine("sending TAKEN signal...");
                        byte[] bytes = Encoding.ASCII.GetBytes("taken");
                        stream.Write(bytes, 0, bytes.Length);
                        continue;
                    }
                    isClientConnected = true;
                    var _bytes = Encoding.ASCII.GetBytes("ready");
                    Console.WriteLine("sending ready signal...");
                    ns = client.GetStream();
                    ns.Write(_bytes, 0, _bytes.Length);
                    Task.Run(() =>
                    {
                        Console.WriteLine("TCP client connected...");
                        while (true)  //while the client is connected, we look for incoming messages
                        {

                            var temp = new byte[1];
                            string responseData = "";
                            int numBytes = 0;
                            byte[] by = new byte[1024];
                            do
                            {
                                ns.Read(temp, 0, 1);
                                by[numBytes] = temp[0];
                                //if (temp[0] == 0)
                                //{
                                //    Console.WriteLine("Environment Exit");
                                //    Console.WriteLine("Environment Exit");
                                //    Environment.Exit(0);
                                //    //break;
                                //}
                                numBytes++;
                                //Console.WriteLine(temp[0]);
                            }
                            while (ns.DataAvailable);


                            var finalByte = by.Take(numBytes).ToArray();
                            if (finalByte[0] == 0 && finalByte.Length == 1) continue;
                            if (finalByte.Length == 0) //NOT DETECTED
                            {
                                Console.WriteLine("client disconnected");

                                //disconnected
                                break;
                            }
                            //if (client.ReceiveBufferSize == 0) continue;
                            //byte[] msg = new byte[client.ReceiveBufferSize];     //the messages arrive as byte array
                            //ns.Read(msg, 0, msg.Length);   //the same networkstream reads the message sent by the client
                            //Console.WriteLine(Encoding.UTF8.GetString(msg)); //now , we write the message as string

                            onReceive?.Invoke(finalByte);
                        }
                        Console.WriteLine("client disconnected");
                    });

                }
            }
            );
        }

        public void send(byte[] msg)
        {
            //var ns = client.GetStream();
            if (ns != null)
            {
                ns.Write(msg, 0, msg.Length);
                Console.WriteLine("send to tcp client unreal engine..");
            }
            else
            {
                Console.WriteLine("ns NULL!!!!");
            }
        }
        public delegate void Notify(byte[] data);
        public event Notify onReceive;

    }
}