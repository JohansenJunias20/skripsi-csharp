using Microsoft.MixedReality.WebRTC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        //udp must be splited into client and server because UDP is connectionless
        public static UDPServer UDPServerUE5; // to recieve UDP packet from UE5
        public static UDPClient UDPClientUE5; // to send UDP packet to UE5
        public static TCPServer TCPServerUE5; // to recieve and send packet from UE5 (because it is bi-directional and open an connection)
        static async Task Main(string[] args)
        {
            #region DEBUG
            //debugWebrtc();
            //return;
            #endregion
            //Console.WriteLine("port UDP server(0/1/2/3/...)");
            //var mod = Convert.ToInt32(Console.ReadLine());
            var mod = 0;

            var portUDPserver = 3001 + (3 * mod);
            Console.WriteLine("port UDP client");
            var portUDPclient = 3002 + (3 * mod);
            Console.WriteLine("port TCP server");
            var portTCPserver = 3003 + (3 * mod);
            //return;
       

            while (true)
            {
                if (TCPchecker.check(3003 + (3 * mod)))
                {
                    portUDPserver = 3001 + (3 * mod);
                    portUDPclient = 3002 + (3 * mod);
                    portTCPserver = 3003 + (3 * mod);
                    break;
                }
                mod++;
            }
            Console.WriteLine(portUDPserver);
            Console.WriteLine(portUDPclient);
            Console.WriteLine(portTCPserver);
            UDPServerUE5 = new UDPServer(new UDPServer.Config { port = portUDPserver });
            UDPServerUE5.runServer();
            UDPClientUE5 = new UDPClient(new UDPClient.Config { port = portUDPclient, IP = "127.0.0.1" });

            TCPServerUE5 = new TCPServer(new TCPServer.Config { port = portTCPserver });
            TCPServerUE5.runServer();
            //TCPServerUE5.
            //Console.WriteLine("readline..");
            //Console.ReadLine();
            UDPServerUE5.onReceive += UDPServerUE5_onReceive;
            TCPServerUE5.onReceive += TCPServerUE5_onReceive;
            //while (true)
            //{
            //    if (UDPServerUE5.initialized)
            //    {
            //        UDPClientUE5.send("testing saja qwlkeqkwo erkoewrmvf erwkoewr fdvmkorsfmf ekoewkr");
            //        Console.WriteLine("sending to UE5");
            //    }
            //    System.Threading.Thread.Sleep(200);
            //}
            Console.ReadLine();
        }
        //static void Main(string[] args)
        //{
        //    Console.ReadLine();
        //}
        #region DEBUG
        //private static void debugWebrtc()
        //{
        //    Console.WriteLine("Server or Client ? (S/C)");
        //    string inpt = Console.ReadLine();
        //    if (inpt.ToUpper() == "S")
        //    {
        //        webRTCserver = new WebRTCServer("test");
        //        typeWebRTC = TypeWebRTC.server;
        //        //webRTCserver.ws.socket.EmitAsync("createroom", new { roomName = "test" }).Wait();
        //    }
        //    else if (inpt.ToUpper() == "C")
        //    {
        //        try
        //        {

        //            WebRTCclient = new WebRTCClient("test");
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message);
        //            Console.WriteLine(ex.ToString());
        //            Console.WriteLine(ex.InnerException.Message);
        //            Console.ReadLine();

        //        }
        //        typeWebRTC = TypeWebRTC.client;
        //    }
        //    else
        //    {
        //        Console.WriteLine("unrecog response");
        //    }
        //    Console.ReadLine();
        //    //throw new NotImplementedException();
        //}
        #endregion
        public enum TypeWebRTC
        {
            server,
            client,
            notset
        }
        public static TypeWebRTC typeWebRTC = TypeWebRTC.notset;
        public static WebRTCServer webRTCserver = null;
        public static WebRTCClient WebRTCclient = null;
        private static void TCPServerUE5_onReceive(string data)
        {
            data = Regex.Replace(data, @"\s+", "");
            //write data to data.txt
            File.WriteAllTextAsync("data.txt", data).Wait();
            Console.WriteLine($"recieve tcp data: '{data}'");
            //var room = data.Split("|")[1];
            //Console.WriteLine($"ROOM: '{room}'");
            //return;
            if (data.Contains("server"))
            {
                UDPClientUE5.send("testing saja");

                Console.WriteLine("initiation webrtc connection as server...");
                webRTCserver = new WebRTCServer();
                webRTCserver.recieveMsgP2P += delegate (byte[] msg, int id)
                {
                    Console.WriteLine("recieve message from client p2p..");
                    Console.WriteLine(Encoding.Default.GetString(msg));
                    UDPClientUE5.send(msg);
                    // caused loop back
                    webRTCserver.broadcast?.Invoke(msg, id);
                };
                typeWebRTC = TypeWebRTC.server;
            }
            else if (data.Contains("client"))
            {
                UDPClientUE5.send("testing saja");
                typeWebRTC = TypeWebRTC.client;
                Console.WriteLine("initiation webrtc connection as client...");
                WebRTCclient = new WebRTCClient();
                WebRTCclient.recieve += delegate (byte[] msg) // INI JALANKAN DI TASK.RUN
                {
                    Console.WriteLine("recieve from p2p server:");
                    UDPClientUE5.send(msg);
                    Console.WriteLine(Encoding.Default.GetString(msg));
                };
            }


        }
        private static void initiateWebRTCserver()
        {

        }
        private static void initiateWebRTCclient()
        {

        }
        private static void UDPServerUE5_onReceive(byte[] data)
        {
            Console.WriteLine("RECIEVE FROM UDP UE5..");
            if (typeWebRTC == TypeWebRTC.server)
            {
                Console.WriteLine("recieve from server game broadcast to every peer...");
                webRTCserver.broadcast?.Invoke(data, -1); // -1 because the server
            }
            else if (typeWebRTC == TypeWebRTC.client)
            {
                //Console.WriteLine("recieve from client game send to server...");
                try
                {
                    WebRTCclient.send(data);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.InnerException.Message);
                    Console.WriteLine(ex.ToString());
                }
            }

            //throw new NotImplementedException();
            //UDPClientUE5.send(data);
            //Console.WriteLine($"recieve a :{data}, sending it back...");
        }




    }
}

