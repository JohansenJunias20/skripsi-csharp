using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class UDPServer
    {
        public delegate void Notify(byte[] data);
        public struct Config
        {
            public int port;
        }
        public bool initialized = false;
        int PORT;
        //create constructor
        public UDPServer(Config config)
        {
            PORT = config.port;
        }
        public event Notify onReceive;
        public void runServer()
        {
            Task.Run(() =>
            {
                //run udp server run on this.IP and this.PORT
                byte[] data = new byte[2048];
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, PORT);
                UdpClient newsock = new UdpClient(sender);

                Console.WriteLine("Waiting for a client...");


                //data = newsock.Receive(ref sender);

                //Console.WriteLine("Message received from {0}:", sender.ToString());
                //Console.WriteLine(Encoding.ASCII.GetString(data, 0, data.Length));

                //string welcome = "Welcome to my test server";
                //data = Encoding.ASCII.GetBytes(welcome);
                //newsock.Send(data, data.Length, sender);
                initialized = true;
                while (true)
                {
                    data = newsock.Receive(ref sender);
                    Console.WriteLine("Message received from {0}:", sender.ToString());
                    Console.WriteLine(data.Length);
                    //Console.WriteLine(Encoding.ASCII.GetString(data, 0, data.Length));
                    onReceive?.Invoke(data);
                    //newsock.Send(data, data.Length, sender);
                }
            });
        }
    }
}
