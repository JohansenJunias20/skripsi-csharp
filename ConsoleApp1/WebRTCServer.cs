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
        //public WebsocketClient Program.ws;
        public delegate void Notify(byte[] data, int idDataChannel);
        public Notify recieveMsgP2P;
        public Notify recieveMsgP2PReliable;
        public struct Answer { public string sdp; public string type; public string socketid; }
        public struct IceCandidateResp
        {
            public string sdpmid;
            public int sdpindex;
            public string candidate;
            public string socketid;
        }
        public Dictionary<string, PeerConnection> peers = new Dictionary<string, PeerConnection>();
        public Dictionary<string, DataChannel> dc_peers = new Dictionary<string, DataChannel>();
        public Dictionary<string, DataChannel> dc_peers_reliable = new Dictionary<string, DataChannel>();
        //struct JoinPeerResp
        //{
        //    public string socketid_csharp;
        //    public string socketid;
        //}
        public WebRTCServer()
        {
            //Console.WriteLine("initializing webrtcserver");
            //Program.ws = new WebsocketClient();
            //Console.WriteLine("setting socketid_csharp..");
            #region DEBUG
            //Program.ws.socket.EmitAsync("createroom", new { roomName = "test" }).Wait();
            #endregion
            //Program.ws.socket.EmitAsync("set:master_socketid_csharp", "").Wait();
            Console.WriteLine("seted socketid_csharp..");
            Program.ws.socket.On("joinpeer", (response) =>
            {
                //var resultstr = response.GetValue(0).ToString();
                //var result = JsonConvert.DeserializeObject<JoinPeerResp>(resultstr);
                //Console.WriteLine("client join.. try to create data channel..");
                var socketid = response.GetValue<string>();
                onPeerJoin(socketid);
            });
            Program.ws.socket.On("icecandidate", (data) =>
            {

                var resultstr = data.GetValue(0).ToString();
                var result = JsonConvert.DeserializeObject<IceCandidateResp>(resultstr);
                Console.WriteLine(result.sdpmid);
                Console.WriteLine(result.sdpindex);
                Console.WriteLine(result.candidate);
                peers[result.socketid].AddIceCandidate(result.sdpmid, result.sdpindex, result.candidate);
            });
            Program.ws.socket.On("answer", (response) =>
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
        private int peerLength = 0;
        public async Task onPeerJoin(string socketid)
        {
            int id = peerLength;
            peerLength++;
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
                Program.ws.socket.EmitAsync("offer", new { sdp = sdp, type = type, socketid = socketid });

            };
            pc.IceCandidateReadytoSend += delegate (string candidate, int sdpMlineindex, string sdpMid)
            {
                var data = new { socketid = socketid, candidate = candidate, sdpindex = sdpMlineindex, sdpmid = sdpMid };
                Console.WriteLine("ice candiate ready!");
                Program.ws.socket.EmitAsync("icecandidate", data);
            };
            pc.RenegotiationNeeded += delegate ()
            {
                Console.WriteLine("negotiation needed!!");
                Task.Delay(500).Wait();
                pc.CreateOffer();
            };
            await pc.InitializeAsync(config);
            Console.WriteLine("webrtc initialized");
            pc.AddDataChannelAsync("channel", false, false).ContinueWith(async (task) =>
           {
               //var rs = task.Result;
               Console.WriteLine("task unreliable..");
               var result = await task;
               dc_peers.Add(socketid, result);
               int idd = 123;
               result.StateChanged += async delegate ()
               {
                   Console.WriteLine($"datachannel state changed to: {result.State.ToString()}");
                   Console.WriteLine($"id {idd}");
                   if (result.State == DataChannel.ChannelState.Open)
                   {
                       #region DEBUG
                       //Task.Run(async () =>
                       //{
                       //    for (; ; )
                       //    {
                       //        //convert to byte[]
                       //        await Task.Delay(50);
                       //        //get unix timestamp
                       //        Console.WriteLine("sending...");
                       //        result.SendMessage(Encoding.UTF8.GetBytes(((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds().ToString()));
                       //    }
                       //});
                       #endregion
                       broadcast += delegate (byte[] msg, int from)
                        {

                            //Console.WriteLine("delegate broadcast called.. from: " + from);
                            if (id == from && from != -1) return;
                            //Console.WriteLine("delegate broadcast called.. and success ");
                            result.SendMessage(msg);
                        };
                       Console.WriteLine("p2p unreliable connection establised");
                       result.MessageReceived += delegate (byte[] msg)
                       {
                           recieveMsgP2P?.Invoke(msg, id);
                           #region DEBUG
                           //var unixNow = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
                           //var data = Encoding.UTF8.GetString(msg);
                           //var unixMil = Convert.ToInt64(data);
                           //Console.WriteLine($"latency: {unixNow - unixMil}ms");
                           #endregion
                       };


                   }


               };
           });
            pc.AddDataChannelAsync("reliable", true, true).ContinueWith(async (task) =>
            {
                Console.WriteLine("task reliable..");
                var result_reliable = await task;
                Console.WriteLine("adding reliable channel");
                Console.WriteLine("added reliable channel");
                //dc_peers_reliable.Add(socketid, result_reliable);
                result_reliable.StateChanged += delegate ()
               {
                   Console.WriteLine($"datachannel state changed to: {result_reliable.State.ToString()}");
                   if (result_reliable.State == DataChannel.ChannelState.Open)
                   {
                       broadcast_reliable += delegate (byte[] msg, int from)
                       {

                           //Console.WriteLine("delegate broadcast called.. from: " + from);
                           if (id == from && from != -1) return;
                           //Console.WriteLine("delegate broadcast called.. and success ");
                           result_reliable.SendMessage(msg);
                       };
                       Console.WriteLine("p2p reliable connection establised");
                       result_reliable.MessageReceived += delegate (byte[] msg)
                       {
                           recieveMsgP2PReliable?.Invoke(msg,id);
                           //recieveMsgP2PReliable?.Invoke(msg, id);

                       };
                   }


               };
            });

            Console.WriteLine("webrtc initialized");

            this.peers.Add(socketid, pc);
            //var result = await pc.AddDataChannelAsync("tets",false,false);
            //pc.InitializeAsync(config).Wait();
            Console.WriteLine(pc.Initialized);
            Console.WriteLine("Peer connection initialized.");
        }
        public delegate void BroadcastDelegate(byte[] data, int idDataChannel);
        public BroadcastDelegate broadcast_reliable;
        public BroadcastDelegate broadcast;
        //public void broadcast(byte[] message, int from)
        //{
        //    Console.WriteLine("from :" + from);
        //    Console.WriteLine("dc peers:" + dc_peers.Count);
        //    //foreach peer
        //    foreach (var dc_peer in dc_peers)
        //    {
        //        Console.WriteLine("dc peers masuk loop");
        //        Console.WriteLine("id: " + dc_peer.Value.ID);
        //        if (dc_peer.Value.ID == from) continue; //the broadcaster
        //        Console.WriteLine("dc peers masuk loop 1");
        //        if (dc_peer.Value.State == DataChannel.ChannelState.Open)
        //        {
        //            Console.WriteLine("dc peers masuk loop 2");
        //            dc_peer.Value.SendMessage(message);
        //            Console.WriteLine("dc peers masuk loop 2");
        //        }
        //        else
        //        {
        //            Console.WriteLine("Cannot broadcast!, data channel state is closed");
        //        }

        //    }
        //}

    }
}
