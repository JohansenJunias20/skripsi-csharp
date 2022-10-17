using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static ConsoleApp1.WebRTCClient;
using static ConsoleApp1.WebRTCServer;

namespace ConsoleApp1
{
    internal class Program
    {
        public static WebsocketClient ws;
        public static string socketid_server;
        //udp must be splited into client and server because UDP is connectionless
        public static UDPServer UDPServerUE5; // to recieve UDP packet from UE5
        public static UDPClient UDPClientUE5; // to send UDP packet to UE5
        public static TCPServer TCPServerUE5 = null; // to recieve and send packet from UE5 (because it is bi-directional and open an connection)
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
            ws = new WebsocketClient();
            listenWebsocket();

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

        private static void listenWebsocket()
        {
            ws.socket.On("role", (data) =>
            {
                var role = data.GetValue<string>();
                if (role == "server")
                {
                    typeWebRTC = TypeWebRTC.server;
                    webRTCserver = new WebRTCServer();
                    webRTCserver.recieveMsgP2P += (msg, e) =>
                    {
                        //Console.WriteLine(Encoding.UTF8.GetString(msg));
                        UDPClientUE5.send(msg);
                    };
                    webRTCserver.recieveMsgP2PReliable += (msg, e) =>
                    {
                        Console.WriteLine("server recieve msg reliable form client..");
                        Console.WriteLine(Encoding.UTF8.GetString(msg));
                        //if (TCPServerUE5 != null)
                        TCPServerUE5?.send(msg);
                    };


                }
                else
                {
                    typeWebRTC = TypeWebRTC.client;
                    WebRTCclient = new WebRTCClient();
                    WebRTCclient.recieve += (msg) =>
                    {
                        //Console.WriteLine(Encoding.UTF8.GetString(msg));
                        UDPClientUE5.send(msg);
                    };
                    WebRTCclient.recieveReliable += (msg) =>
                    {
                        Console.WriteLine(Encoding.UTF8.GetString(msg));
                        if (TCPServerUE5 != null)
                            TCPServerUE5?.send(msg);
                    };

                }
            });

            //throw new NotImplementedException();
        }

        private static void Pc_RemoteAudioFrameReady(AudioFrame frame)
        {
            //frame
            //throw new NotImplementedException();
        }

        //static void Main(string[] args)
        //{
        //    Console.ReadLine();
        //}
        #region DEBUG

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

        private static readonly Regex sWhitespace = new Regex(@"\s+");
        public static string ReplaceWhitespace(string input, string replacement)
        {
            return sWhitespace.Replace(input, replacement);
        }
        private static void TCPServerUE5_onReceive(byte[] data)
        {
            var msg = Encoding.UTF8.GetString(data);
            Console.WriteLine(data.Length);
            Console.WriteLine(data);
            //byte[] bdata = Convert.FromBase64String(msg);
            //string decodedString = Encoding.UTF8.GetString(bdata);
            Console.WriteLine($"recieve tcp msg: `{msg}`");
            if (msg == "role")
            {
                Console.WriteLine("it is a role!");
                if (Program.typeWebRTC == Program.TypeWebRTC.server)
                {
                    string obj = "{\"channel\":\"role\",\"to\":\"all\",\"data\":{\"role\":\"server\",\"socketid\":\"" + ws.socket.Id + "\"}}";
                    var bObj = Encoding.UTF8.GetBytes(obj);
                    TCPServerUE5.send(bObj);
                }
                else if (Program.typeWebRTC == Program.TypeWebRTC.client)
                {
                    string obj = "{\"channel\":\"role\",\"to\":\"all\",\"data\":{\"role\":\"client\",\"socketid\":\"" + ws.socket.Id + "\"}}";

                    var bObj = Encoding.UTF8.GetBytes(obj);
                    TCPServerUE5.send(bObj);
                }
                return;
            }
            if (typeWebRTC == TypeWebRTC.server)
            {
                webRTCserver.broadcast_reliable(data, -1);
            }
            else if (typeWebRTC == TypeWebRTC.client)
            {
                WebRTCclient.sendReliable(data);
            }
            return;
            //data = Regex.Replace(data, @"\s+", "");
            ////write data to data.txt
            //File.WriteAllTextAsync("data.txt", data).Wait();
            //Console.WriteLine($"recieve tcp data: '{data}'");
            ////var room = data.Split("|")[1];
            ////Console.WriteLine($"ROOM: '{room}'");
            ////return;
            //if (data.Contains("server"))
            //{
            //    UDPClientUE5.send("testing saja");

            //    Console.WriteLine("initiation webrtc connection as server...");
            //    webRTCserver = new WebRTCServer();
            //    webRTCserver.recieveMsgP2P += delegate (byte[] msg, int id)
            //    {
            //        //Console.WriteLine("recieve message from client p2p..");
            //        //Console.WriteLine(Encoding.Default.GetString(msg));
            //        UDPClientUE5.send(msg);
            //        // caused loop back
            //        //webRTCserver.broadcast?.Invoke(msg, id);
            //    };
            //    typeWebRTC = TypeWebRTC.server;
            //}
            //else if (data.Contains("client"))
            //{
            //    UDPClientUE5.send("testing saja");
            //    typeWebRTC = TypeWebRTC.client;
            //    Console.WriteLine("initiation webrtc connection as client...");
            //    WebRTCclient = new WebRTCClient();
            //    WebRTCclient.recieve += delegate (byte[] msg) // INI JALANKAN DI TASK.RUN
            //    {
            //        Console.WriteLine("recieve from p2p server:");
            //        UDPClientUE5.send(msg);
            //        Console.WriteLine(Encoding.Default.GetString(msg));
            //    };
            //}


        }
        private static void initiateWebRTCserver()
        {

        }
        private static void initiateWebRTCclient()
        {

        }
        private static void UDPServerUE5_onReceive(byte[] data)
        {
            //Console.WriteLine( Encoding.UTF8.GetString(data));
            //Console.WriteLine("RECIEVE FROM UDP UE5..");
            if (typeWebRTC == TypeWebRTC.server)
            {
                //Console.WriteLine("recieve from server game broadcast to every peer...");
                webRTCserver?.broadcast?.Invoke(data, -1); // -1 because the server
            }
            else if (typeWebRTC == TypeWebRTC.client)
            {
                //Console.WriteLine("recieve from client game send to server...");
                WebRTCclient.send(data);
            }

            //throw new NotImplementedException();
            //UDPClientUE5.send(data);
            //Console.WriteLine($"recieve a :{data}, sending it back...");
        }




    }
}

