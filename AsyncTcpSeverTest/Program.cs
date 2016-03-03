using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AsyncTcpSever;

namespace AsyncTcpSeverTest
{
    class Program
    {
        private static SocketListener socketListener = null;
        static void Main(string[] args)
        {         
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 4399);
            SocketSetting setting = new SocketSetting(25, 20, 20, 100, 10, 4, localEndPoint);
            socketListener = new SocketListener(setting);
            socketListener.ReceiveEvent += socketListener_ReceiveEvent;
            Console.ReadLine();
        }

        static void socketListener_ReceiveEvent(Socket client, byte[] bytes)
        {
            string str = Encoding.UTF8.GetString(bytes);
            Console.WriteLine(str);
            socketListener.SendData(client,Encoding.UTF8.GetBytes("谢谢！"));
        }
    }
}
