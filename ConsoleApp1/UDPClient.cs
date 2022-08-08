using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class UDPClient
    {
        public struct Config
        {
            public int port;
            public string IP;
        }
        int PORT;
        string IP;
        public UdpClient socket;
        //create constructor
        public UDPClient(Config config)
        {
            PORT = config.port;
            IP = config.IP;
            socket = new UdpClient();
            socket.Connect(IP, PORT);
        }
        public void send(string data)
        {
            //convert data to bytes
            byte[] bData = Encoding.ASCII.GetBytes(data);
            socket.Send(bData, bData.Length);
        }
        public void send(byte[] data)
        {
            //convert data to bytes
            socket.Send(data, data.Length);
        }

    }
}
