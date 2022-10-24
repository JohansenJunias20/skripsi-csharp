using Microsoft.MixedReality.WebRTC;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
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
        public DataChannel channelServerVoice = null;
        public Dictionary<string, AudioStruct> audioStructs = new Dictionary<string, AudioStruct>();
        public WaveInEvent waveSource = new WaveInEvent();
        public WebRTCClient()
        {
            var w = new Mp3FileReader("Wew");
            //var 
            waveSource.DataAvailable += WaveSource_DataAvailable;
            waveSource.DeviceNumber = 1;
            waveSource.StartRecording();
            int waveOutDevices = WaveOut.DeviceCount;
            for (int waveOutDevice = 0; waveOutDevice < waveOutDevices; waveOutDevice++)
            {
                WaveOutCapabilities deviceInfo = WaveOut.GetCapabilities(waveOutDevice);
                Console.WriteLine("Device {0}: {1}, {2} channels", waveOutDevice, deviceInfo.ProductName, deviceInfo.Channels);
            }
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
               if (data.Label == "transform")
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
               else if (data.Label == "reliable")
               {
                   channelServerReliable = data;
                   data.MessageReceived += delegate (byte[] msg)
                   {
                       recieveReliable?.Invoke(msg);
                   };
               }
               else if (data.Label == "audio")
               {
                   channelServerVoice = data;
                   data.MessageReceived += delegate (byte[] msg)
                   {
                       var result = JsonConvert.DeserializeObject<DataVoice>(Encoding.UTF8.GetString(msg));
                       if (!audioStructs.ContainsKey(result.socketid))
                       {
                           var WF = new WaveFormat(8000, 1);
                           var bwp = new BufferedWaveProvider(WF);
                           //var t = new MultiplexingWaveProvider(new IWaveProvider[] { bwp }, 2);
                           //t.ConnectInputToOutput()
                           var audiostruct = new AudioStruct()
                           {
                               bwp = bwp,
                               wo = new WaveOutEvent(),
                               vsp = new VolumeSampleProvider(bwp.ToSampleProvider())
                           };
                           audiostruct.vsp.Volume = 0f;
                           audiostruct.wo.Init(audiostruct.vsp);
                           audiostruct.wo.Play();
                           audioStructs.Add(result.socketid, audiostruct);
                           return; //return because this take so long time,
                       }
                       audioStructs[result.socketid].bwp.AddSamples(result.data, 0, result.data.Length);
                       //recieveReliable?.Invoke(msg);
                   };
               }

           };
            pc_server.InitializeAsync(config).Wait();
            Program.ws.socket.EmitAsync("get:master_csharp", "");
            Console.WriteLine(pc_server.Initialized);
            Console.WriteLine("peer initialized!");
        }

        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            //if(channelServerVoice != null)
            //{
            sendVoice(e.Buffer);
            //}
            //throw new NotImplementedException();
        }

        int numFrames = 0;

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
                Console.WriteLine("channel unreliable is null");
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
        public struct DataVoice
        {
            public byte[] data;
            public string socketid;
        };
        public void sendVoice(byte[] buffer)
        {
            if (channelServerVoice == null)
            {
                Console.WriteLine("channel voice is null");
                return;
            };
            if (channelServerVoice.State == DataChannel.ChannelState.Open)
            {
                if (Program.player_proximity[Program.ws.socket.Id] == 0)
                {
                    return; //no need to send because the volume is 0
                }
                channelServerVoice.SendMessage(buffer);
                //var data = new DataVoice()
                //{
                //    data = buffer,
                //    socketid = Program.ws.socket.Id
                //};
                //var str = JsonConvert.SerializeObject(data);
                //Console.WriteLine(str);
                //var buffer_final = Encoding.UTF8.GetBytes(str);
                //Console.WriteLine("reliable message sent to p2p server");
                //channelServerVoice.SendMessage(buffer);
            }
            else
            {
                Console.WriteLine("data channel reliable is not open yet!!!");
            }
        }
        public void sendReliable(byte[] msg)
        {
            if (channelServerReliable == null)
            {
                Console.WriteLine("channel reliable is null");
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
