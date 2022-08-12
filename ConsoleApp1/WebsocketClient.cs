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
            socket = new SocketIO("ws://192.168.1.100:3000");
            socket.OnConnected += async (sender, e) =>
           {
               Console.WriteLine("connected to socketio server");
           };
            //socket.On("test", () =>
            //{
            //    socket.Emit("hi");

            //});
            socket.ConnectAsync().Wait();
        }
    }
}
