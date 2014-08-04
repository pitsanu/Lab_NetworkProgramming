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
            
            IPAddress ipAddress = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == addressFamily).FirstOrDefault();
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, listenPort);

            UdpClient listener = new UdpClient(localEndPoint);


            try
            {
                //listener.Client.Bind(localEndPoint);
                //listener.Client.Listen(1000);

                IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, 0);

                while (!done)
                {
                    byte[] data = listener.Receive(ref clientEndpoint);

                    string str = Encoding.ASCII.GetString(data);

                    Console.WriteLine(string.Format("Read:{0} from:{1}", str, clientEndpoint.ToString()));
                   // Console.WriteLine(string.Format("Read:{0}", str));
                }
            }
            catch (SocketException soex)
            {
                Console.WriteLine("SocketException code:{0}, message:{1}", soex.ErrorCode, soex.Message);
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
