using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Text;
namespace ConsoleApp1
{
    internal class WebsocketClient
    {
        public SocketIO socket;
        public WebsocketClient()
        {
            Console.WriteLine("connecting to socketio server...");
            socket = new SocketIO("ws://localhost:3000");
            socket.OnConnected += async (sender, e) =>
           {
               Console.WriteLine("connected to socketio server");
               Program.player_proximity.Add(socket.Id, new Program.VolumeProximity() { left = 100, right = 100 });
           };
            socket.OnError += Socket_OnError;
            //socket.On("test", () =>
            //{
            //    socket.Emit("hi");

            //});
            socket.ConnectAsync().Wait();
        }

        private void Socket_OnError(object sender, string e)
        {
            Console.WriteLine("error socketio connection");
            Console.WriteLine(e);
            //throw new NotImplementedException();
        }
    }
}
