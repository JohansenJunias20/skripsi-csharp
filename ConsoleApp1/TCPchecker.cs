using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ConsoleApp1
{
    internal class TCPchecker
    {
        //public Action onFailed;
        //public Action onSuccess;
        public TCPchecker()
        {

        }
        public static bool check(int port)
        {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            try
            {
                Console.WriteLine($"checking port: {port}");
                server.Start();
                server.Stop();
                //onSuccess?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                server.Stop();
                //onFailed?.Invoke();
                return false;
            }

        }
    }
}
