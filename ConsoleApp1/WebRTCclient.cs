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
        public WebsocketClient ws;
        public PeerConnection pc_server;
        public struct Offer { public string sdp; public string type; };
        public string socketid_server = "";
        public DataChannel channelServer =null;
        public WebRTCClient(string roomName)
        {
            //why use task run?
            //sepertinya PeerConnection datachannel menggunakan thread utama
            //sehingga apapun yang menggunakan thread utama akan keblok
            //karena datachannel.onrecieve -> menggunakan main thread
            Task.Run(() =>
            {
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
                ws = new WebsocketClient();
                ws.socket.On("get:master_csharp", (data) =>
                {
                    Console.WriteLine("recieve socket_id csharp");
                    socketid_server = data.GetValue<string>();
                    Console.WriteLine("socketid_server:");
                    Console.WriteLine(socketid_server);
                    startConnect(roomName);
                });
                ws.socket.On("offer", (data) =>
                {
                    Console.WriteLine("offer recieve");
                    var resultstr = data.GetValue(0).ToString();
                    var result = JsonConvert.DeserializeObject<Offer>(resultstr);
                //Console.WriteLine(result);
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
                        ws.socket.EmitAsync("answer", new { sdp = sdp, type = type, socketid = socketid_server });
                    };
                    Console.WriteLine("creating answer..");
                    if (!pc_server.CreateAnswer())
                    {
                        Console.WriteLine("create answer failed");
                    }
                });
                ws.socket.On("icecandidate", (data) =>
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
                   channelServer = data;
                   data.MessageReceived += delegate (byte[] msg)
                   {
                       recieve?.Invoke(msg);
                   };
               };
                ws.socket.EmitAsync("get:master_csharp", roomName);
                pc_server.InitializeAsync(config).Wait();
                Console.WriteLine(pc_server.Initialized);
                Console.WriteLine("peer initialized!");
            });
        }
        public delegate void NotifyRecieve(byte[] data);
        public NotifyRecieve recieve;
        //gameid is socket id yang client dari unreal engine.
        private void startConnect(string roomName)
        {
            Console.WriteLine("joinpeer initiate..");
            pc_server.IceCandidateReadytoSend += delegate (string candidate, int sdpMlineindex, string sdpMid)
            {
                ws.socket.EmitAsync("icecandidate", new
                {
                    socketid = socketid_server,
                    candidate = candidate,
                    sdpmid = sdpMid,
                    sdpindex = sdpMlineindex
                });
            };
            //client dont need negotiation
            ws.socket.EmitAsync("joinpeer", roomName);
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
                Console.WriteLine("message sent to p2p server");
                channelServer.SendMessage(msg);
            }
            else
            {
                Console.WriteLine("data channel is not open yet!!!");
            }
        }
    }
}
