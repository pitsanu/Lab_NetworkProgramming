using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SocketServerAsync
{
    class Program
    {
        static void Main(string[] args)
        {
            AddressFamily expectAddressFamily = AddressFamily.InterNetwork;

            string hostname = Dns.GetHostName();
            Console.WriteLine("Host name -> {0}", hostname);
            IPAddress[] ipList = Dns.GetHostAddresses(hostname).Where(o => o.AddressFamily == expectAddressFamily).ToArray();

            IPAddress ipAddress = ipList[0];

            int port = 11000;
            IPEndPoint ipe = new IPEndPoint(ipAddress, port);
            Socket socket = new Socket(expectAddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Bind(ipe);
                socket.Listen(100);


            }
            catch (Exception ex) { }

        }
    }
}
