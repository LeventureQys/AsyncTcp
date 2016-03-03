using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AsyncTcpClient;

namespace AsyncTcpClientTest
{
    class Program
    {
        private static SocketClient client = null;
        private static Socket _socket;

        static void Main(string[] args)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.104"), 4399);
            SocketSetting setting = new SocketSetting(25, 20, 30, 100, 10, 4, localEndPoint);
            client = new SocketClient(setting);
            client.StartConnect();
            client.ConnectedEvent += client_ConnectedEvent;
            client.ReceiveEvent += client_ReceiveEvent;
            Console.ReadLine();
        }

        static void client_ReceiveEvent(Socket socket,byte[] bytes)
        {
           string str= Encoding.UTF8.GetString(bytes);
            Console.WriteLine(str);
           
        }

        static void client_ConnectedEvent(Socket socket)
        {
            _socket = socket;
            byte[] bytes = Encoding.UTF8.GetBytes("虽然从去年开始坊间就盛传佳能会在今年的Photokina之前推出自己的专业级全画幅无反相机，但是在Dpreview最近对佳能经理Tokura的采访中，Tokura却公开否认了该传闻，并表示这种说法是不切实际的。针对佳能未来的无反相机计划等相关问题，佳能经理Tokura表示，就当前的技术水平来看，无反相机在一些诸如自动对焦等方面仍无法媲美DSLRs，因此传闻中所说的佳能将推出专业级无反相机的说法，是有些不切实际的，两种相机之间的鸿沟仍需要一些时间才能解决");
            client.SendData(socket,bytes);

           
        }


    }
}
