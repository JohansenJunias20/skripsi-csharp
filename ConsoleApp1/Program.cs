using Microsoft.MixedReality.WebRTC;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
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
        //static WaveInEvent waveSource = null;
        //public static BufferedWaveProvider bwp;
        //public static BufferedWaveProvider bwp2;
        //static WaveOutEvent waveOut = null;
        //static WaveOutEvent waveOut2 = null;
        private static void WaveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static async void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            //await Task.Delay(4000);
            //Console.WriteLine("1");
            //throw new NotImplementedException();
            //bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
            //Task.Run(async () =>
            //{
            //    //Task
            //    bwp2.AddSamples(e.Buffer, 0, e.BytesRecorded);
            //});
        }
        static async Task Main(string[] args)
        //static void Main(string[] args)
        {
            // var WF = new WaveFormat(8000, 1);
            // //BWP TETAP HARUS 2, KALAU TIDAK NANTI OUTPUT SUARANYA GANTIAN KARENA INPUT CUMA 1
            // var bwp1 = new BufferedWaveProvider(WF);
            // var bwp2 = new BufferedWaveProvider(WF);
            // WaveInEvent waveSource = new WaveInEvent();
            // waveSource.DataAvailable += (w, e) =>
            //{
            //    bwp1.AddSamples(e.Buffer, 0, e.BytesRecorded);
            //    bwp2.AddSamples(e.Buffer, 0, e.BytesRecorded);
            //};
            // waveSource.DeviceNumber = 1;
            // waveSource.StartRecording();


            // var vsp1 = new VolumeSampleProvider(bwp1.ToSampleProvider());//input1.
            // vsp1.Volume = 0.5f;
            // var vsp2 = new VolumeSampleProvider(bwp2.ToSampleProvider());//input1.
            // vsp2.Volume = 0.9f;
            // MultiplexingWaveProvider waveProvider = new MultiplexingWaveProvider(new IWaveProvider[] { vsp1.ToWaveProvider(), vsp2.ToWaveProvider() }, 2);
            // //waveProvider.ConnectInputToOutput(0, 0);
            // //waveProvider.ConnectInputToOutput(1, 1);
            // WaveOut wave = new WaveOut();
            // wave.Init(waveProvider);
            // wave.Play();
            // Console.Read();
            // return;

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
                Console.WriteLine("recieveing role...");
                if (role == "server")
                {
                    typeWebRTC = TypeWebRTC.server;
                    webRTCserver = new WebRTCServer();
                    webRTCserver.recieveMsgP2P += (msg, e) =>
                    {
                        //Console.WriteLine(Encoding.UTF8.GetString(msg));
                        webRTCserver.broadcast?.Invoke(msg, e);
                        UDPClientUE5.send(msg);
                    };
                    webRTCserver.recieveMsgP2PReliable += (msg, e) =>
                    {
                        Console.WriteLine("server recieve msg reliable form client..");
                        Console.WriteLine(Encoding.UTF8.GetString(msg));
                        //if (TCPServerUE5 != null)
                        TCPServerUE5?.send(msg);
                        webRTCserver.broadcast_reliable?.Invoke(msg, e);
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

        //socket id as key, int as volume (0-100)
        public static TypeWebRTC typeWebRTC = TypeWebRTC.notset;
        public static WebRTCServer webRTCserver = null;
        public static WebRTCClient WebRTCclient = null;

        private static readonly Regex sWhitespace = new Regex(@"\s+");
        public static string ReplaceWhitespace(string input, string replacement)
        {
            return sWhitespace.Replace(input, replacement);
        }
        public struct Player
        {
            public string socketid;
            public string breakoutRoom_RM_socketid; //breakout room RM the socket id
            public bool RM;
        };
        public static Player me = new Player()
        {
            socketid = "",
            RM = false,
            breakoutRoom_RM_socketid = ""
        };
        public static List<Player> Others = new List<Player>();
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
                webRTCserver.broadcast_reliable?.Invoke(data, -1);
            }
            else if (typeWebRTC == TypeWebRTC.client)
            {
                WebRTCclient.sendReliable(data);
            }
            return;

        }
        private static void UDPServerUE5_onReceive(byte[] data)
        {
            //Console.WriteLine( Encoding.UTF8.GetString(data));
            //Console.WriteLine("RECIEVE FROM UDP UE5..");
            if (typeWebRTC == TypeWebRTC.server)
            {
                //because the server send data tick every tick to all players,
                //so we need to convert data tick to JSON to get the information of all breakout room
                //console app need to know all breakout room information to "play or not play" the audio 
                var str = Encoding.UTF8.GetString(data);
                var result = JsonConvert.DeserializeObject<UDPCustomICE<DataTick>>(str);
                if (result.channel == "tick")
                {
                    //this is data tick
                    //Console.WriteLine(result.data.players.Length);
                    //console app need gather information of the breakout rooms from here.
                    //var datatick = (UDPCustomICE)Convert.ChangeType(result["data"], typeof(UDPCustomICE));
                    //datatick.
                        breakoutRooms = result.data.breakoutrooms;
                    //Console.WriteLine()

                    //datatick.players
                }


                //Console.WriteLine("recieve from server game broadcast to every peer...");
                webRTCserver?.broadcast?.Invoke(data, -1); // -1 because the server
            }
            else if (typeWebRTC == TypeWebRTC.client)
            {
                //Console.WriteLine("recieve from client game send to server...");
                WebRTCclient.send(data);
            }

        }
        public struct Transform
        {
            Vector3 location;
        }
        public struct Vector3
        {
            public float x, y, z;
        }
        public struct PlayerUDP
        {
            public string socketid;
            public Vector3 position;
        };
        public struct Object
        {
            public Transform transform;
        };
        public struct BreakoutRoomClient
        {
           public int playerId;
           public string playerNameEOSVoice;
           public string socketio_id;
        }
        public struct BreakoutRoom
        {
            public int RM_PlayerId; //tidak dipakai
            public BreakoutRoomClient[] Clients;
            public string RM_PlayerNameVoiceEOS; //tidak dipakai
            public bool Destroyed;
            public string RM_SocketId;
        }
        public struct DataTick
        {

            public PlayerUDP[] players;
            public Object[] objects;
            public BreakoutRoom[] breakoutrooms;
        }
        public struct UDPCustomICE<T>// ini mengikuti struct Struct_CustomICE pada game Unreal Engine.
        {
            public string channel;
            public T data;
            public string to;
            public string from;
        };
        public static BreakoutRoom[] breakoutRooms = new BreakoutRoom[0];

        //get breakout room index by socketId
        public static int getBreakoutRoomIndex(string socketId)
        {
            for (int i = 0; i < breakoutRooms.Length; i++)
            {
                var room = breakoutRooms[i];
                if (room.Destroyed) continue;
                if (room.RM_SocketId == socketId)
                {
                    return i;
                }

                for (int j = 0; j < breakoutRooms[i].Clients.Length; j++)
                {
                    var cl = breakoutRooms[i].Clients[j];
                    if (cl.socketio_id == socketId)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
        //private struct UDPVoice
        //{
        //    public string socketid;
        //    public bool block;
        //}
        //private struct UDPVoiceResponse
        //{
        //    public UDPVoice[] data;
        //    public string channel;
        //    public string from;
        //    public string to;
        //    public string socketid;
        //}

    }
}

