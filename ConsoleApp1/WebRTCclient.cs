using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static ConsoleApp1.WebRTCServer;

namespace ConsoleApp1
{
    internal class WebRTCClient
    {
        //public WebsocketClient ws;
        public PeerConnection pc_server;
        public struct Offer { public string sdp; public string type; };
        public string socketid_server = "";
        public DataChannel channelServer = null;
        public DataChannel channelServerReliable = null;
        public WebRTCClient()
        {
            //why use task run?
            //sepertinya PeerConnection datachannel menggunakan thread utama
            //sehingga apapun yang menggunakan thread utama akan keblok
            //karena datachannel.onrecieve -> menggunakan main thread
            //Task.Run(() =>
            //{
            pc_server = new PeerConnection();
            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer> {
                    new IceServer{ Urls = { "stun:stun.l.google.com:19302" } },
                    new IceServer{ Urls = { "turn:portofolio.orbitskomputer.com:3478" },
                                    TurnUserName = "guest",TurnPassword="welost123"
                                  }
                }
            };

            //ws = new WebsocketClient();
            Program.ws.socket.On("get:master_csharp", (data) =>
            {
                Console.WriteLine("recieve socket_id csharp");
                socketid_server = data.GetValue<string>();
                Console.WriteLine("socketid_server:");
                Console.WriteLine(socketid_server);
                startConnect();
            });
            Program.ws.socket.On("offer", (data) =>
            {
                Console.WriteLine("offer recieve");
                var resultstr = data.GetValue(0).ToString();
                var result = JsonConvert.DeserializeObject<Offer>(resultstr);
                Console.WriteLine("sdp: " + result.sdp);
                Console.WriteLine("type: " + result.type);
                try
                {

                    if (result.type == "offer")
                        pc_server.SetRemoteDescription(result.type, result.sdp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.InnerException);
                    Console.ReadLine();
                }
                Console.WriteLine("set remote description");
                pc_server.LocalSdpReadytoSend += (string type, string sdp) => //menurut dokumentasinya bisa
                {
                    Console.WriteLine("answer ready to send, sending...");
                    Program.ws.socket.EmitAsync("answer", new { sdp = sdp, type = type, socketid = socketid_server });
                };
                Console.WriteLine("creating answer..");
                if (!pc_server.CreateAnswer())
                {
                    Console.WriteLine("create answer failed");
                }
            });
            Program.ws.socket.On("icecandidate", (data) =>
            {
                Console.WriteLine("recieve ice candidate...");
                var resultstr = data.GetValue(0).ToString();
                var result = JsonConvert.DeserializeObject<IceCandidateResp>(resultstr);
                pc_server.AddIceCandidate(result.sdpmid, result.sdpindex, result.candidate);
                //pc_server.AddIceCandidate("q");
            });
            pc_server.DataChannelAdded += (data) =>
           {
               Console.WriteLine("data channel added...");
               if (data.Label == "channel")
               {
                   //unreliable
                   channelServer = data;
                   data.MessageReceived += delegate (byte[] msg)
                   {
                       #region DEBUG
                       //send(msg); //send back to the server because difference time between computers
                       #endregion
                       recieve?.Invoke(msg);
                   };
               }
               else
               {
                   channelServerReliable = data;
                   data.MessageReceived += delegate (byte[] msg)
                   {
                       recieveReliable?.Invoke(msg);
                   };
               }

           };
            pc_server.InitializeAsync(config).Wait();
            Program.ws.socket.EmitAsync("get:master_csharp", "");
            Console.WriteLine(pc_server.Initialized);
            Console.WriteLine("peer initialized!");
        }
        public delegate void NotifyRecieve(byte[] data);
        public NotifyRecieve recieve;
        public NotifyRecieve recieveReliable;
        //gameid is socket id yang client dari unreal engine.
        private void startConnect()
        {
            Console.WriteLine("joinpeer initiate..");
            pc_server.IceCandidateReadytoSend += delegate (string candidate, int sdpMlineindex, string sdpMid)
            {
                Program.ws.socket.EmitAsync("icecandidate", new
                {
                    socketid = socketid_server,
                    candidate = candidate,
                    sdpmid = sdpMid,
                    sdpindex = sdpMlineindex
                });
            };
            //client dont need negotiation
            Program.ws.socket.EmitAsync("joinpeer", "");
        }
        public void send(byte[] msg)
        {
            if (channelServer == null)
            {
                Console.WriteLine("channel is null");
                return;
            };
            if (channelServer.State == DataChannel.ChannelState.Open)
            {
                //Console.WriteLine("message sent to p2p server");
                channelServer.SendMessage(msg);
            }
            else
            {
                Console.WriteLine("data channel is not open yet!!!");
            }

        }
        public void sendReliable(byte[] msg)
        {
            if (channelServerReliable == null)
            {
                Console.WriteLine("channel is null");
                return;
            };
            if (channelServerReliable.State == DataChannel.ChannelState.Open)
            {
                Console.WriteLine("reliable message sent to p2p server");
                channelServerReliable.SendMessage(msg);
            }
            else
            {
                Console.WriteLine("data channel reliable is not open yet!!!");
            }
        }
    }
}
