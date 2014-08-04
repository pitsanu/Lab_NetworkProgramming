using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace UdpServer
{
    class Program
    {
        private const int listenPort = 11000;

        static void StartListener()
        {
            AddressFamily addressFamily = AddressFamily.InterNetwork;
            bool done = false;

            UdpClient listener = new UdpClient();
            IPAddress ipAddress = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == addressFamily).FirstOrDefault();
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, listenPort);

     
            try
            {
                while (!done)
                {
                    byte[] data = listener.Receive(ref localEndPoint);
                    
                    string str = Encoding.ASCII.GetString(data);

                    Console.WriteLine(str);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                listener.Close();
            }
            
        }

        static int Main(string[] args)
        {
            StartListener();

            return 0;
        }
    }
}
