using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace UdpClient
{
    class Program
    {
        private const int listenPort = 11000;

        static void StartClient()
        {
            AddressFamily addressFamily = AddressFamily.InterNetwork;
            System.Net.Sockets.UdpClient client = new System.Net.Sockets.UdpClient();        
            IPAddress ipAddress = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == addressFamily).FirstOrDefault();
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, listenPort);

            client.Connect(localEndPoint);
            while (true)
            {
                Console.Write("Input: ");

                string str = Console.ReadLine();
                byte[] data = Encoding.ASCII.GetBytes(str);

                client.Send(data, data.Length);
                
            }
        }

        static int Main(string[] args)
        {
            StartClient();

            return 0;
        }
    }
}
