using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class WebRTCServer
    {
        public WebsocketClient ws;
        public delegate void Notify(byte[] data);
        public Notify recieveMsgP2P;
        struct Answer { public string sdp; public string type; public string socketid; }
        public struct IceCandidateResp
        {
            public string sdpmid;
            public int sdpindex;
            public string candidate;
            public string socketid;
        }
        public Dictionary<string, PeerConnection> peers = new Dictionary<string, PeerConnection>();
        public Dictionary<string, DataChannel> dc_peers = new Dictionary<string, DataChannel>();
        //struct JoinPeerResp
        //{
        //    public string socketid_csharp;
        //    public string socketid;
        //}
        public WebRTCServer(string roomName)
        {
            Console.WriteLine("initializing webrtcserver");
            ws = new WebsocketClient();
            Console.WriteLine("setting socketid_csharp..");
            ws.socket.EmitAsync("set:master_socketid_csharp", roomName).Wait();
            Console.WriteLine("seted socketid_csharp..");
            ws.socket.On("joinpeer", (response) =>
            {
                //var resultstr = response.GetValue(0).ToString();
                //var result = JsonConvert.DeserializeObject<JoinPeerResp>(resultstr);
                //Console.WriteLine("client join.. try to create data channel..");
                var socketid = response.GetValue<string>();
                onPeerJoin(socketid);
            });
            ws.socket.On("icecandidate", (data) =>
            {

                var resultstr = data.GetValue(0).ToString();
                var result = JsonConvert.DeserializeObject<IceCandidateResp>(resultstr);
                Console.WriteLine(result.sdpmid);
                Console.WriteLine(result.sdpindex);
                Console.WriteLine(result.candidate);
                peers[result.socketid].AddIceCandidate(result.sdpmid, result.sdpindex, result.candidate);
            });
            ws.socket.On("answer", (response) =>
            {
                var resultstr = response.GetValue(0).ToString();
                var result = JsonConvert.DeserializeObject<Answer>(resultstr);
                //SdpMessage sdp = new SdpMessage();
                //sdp.Content = result.sdp;
                if (result.type == "answer")
                    peers[result.socketid].SetRemoteDescription(result.type, result.sdp);
            });
        }
        //karena ada 2 socket io client yaitu yang dari c# dan dari unreal engine
        //maka socketid adalah socket id client c#
        //idgame adalah socket id client unreal engine
        //datachannel key-nya didaftarkan berdasarkan idgame bukan socketid
        //karena socketid sudah tidak ada fungsinya lagi bila koneksi p2p sudah terestablished
        async Task onPeerJoin(string socketid)
        {
            var pc = new PeerConnection();
            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer> {
            new IceServer{ Urls = { "stun:stun.l.google.com:19302" } },
            new IceServer{ Urls = { "turn:portofolio.orbitskomputer.com:3478" },
                TurnUserName = "guest",TurnPassword="welost123"  }
        }
            };
            pc.LocalSdpReadytoSend += delegate (string type, string sdp)
            {

                Console.WriteLine("sdp ready to send, creating offer with sdp sent");
                ws.socket.EmitAsync("offer", new { sdp = sdp, type = type, socketid = socketid });

            };
            pc.IceCandidateReadytoSend += delegate (string candidate, int sdpMlineindex, string sdpMid)
            {
                var data = new { socketid = socketid, candidate = candidate, sdpindex = sdpMlineindex, sdpmid = sdpMid };
                Console.WriteLine("ice candiate ready!");
                ws.socket.EmitAsync("icecandidate", data);
            };
            pc.RenegotiationNeeded += delegate ()
            {
                Console.WriteLine("negotiation needed!!");

                pc.CreateOffer();
            };
            await pc.InitializeAsync(config);
            var result = await pc.AddDataChannelAsync("channel", false, false);
            dc_peers.Add(socketid, result);
            result.StateChanged += delegate ()
            {
                Console.WriteLine($"datachannel state changed to: {result.State.ToString()}");
                if (result.State == DataChannel.ChannelState.Open)
                {
                    Console.WriteLine("p2p connection establised");
                    result.MessageReceived += delegate (byte[] msg)
                    {
                        recieveMsgP2P?.Invoke(msg);
                        //broadcast(msg); jangan dibroadcast dulu nanti jadi loop
                    };
                }

               
            };
            this.peers.Add(socketid, pc);
            //var result = await pc.AddDataChannelAsync("tets",false,false);
            pc.InitializeAsync(config).Wait();
            Console.WriteLine(pc.Initialized);
            Console.WriteLine("Peer connection initialized.");
        }
        public void broadcast(string message)
        {
            var bmessage = Encoding.Default.GetBytes(message);
            //foreach peer
            foreach (var dc_peer in dc_peers)
            {
                if (dc_peer.Value.State == DataChannel.ChannelState.Open)
                {

                }
                    dc_peer.Value.SendMessage(bmessage);
            }
        }
        public void broadcast(byte[] message)
        {
            //foreach peer
            foreach (var dc_peer in dc_peers)
            {
                if (dc_peer.Value.State == DataChannel.ChannelState.Open)
                    dc_peer.Value.SendMessage(message);
            }
        }
     
    }
}
