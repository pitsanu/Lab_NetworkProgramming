using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace SocketClientTut
{
    class Program
    {
        static void Main(string[] args)
        {
            AddressFamily expectAddressFamily = AddressFamily.InterNetwork;

            string hostname = Dns.GetHostName();
            IPAddress[] ipList = Dns.GetHostAddresses(hostname).Where(o => o.AddressFamily == expectAddressFamily).ToArray();
            
            IPAddress ipAddress = ipList[0];
            

            int port = 11000;
            IPEndPoint ipe = new IPEndPoint(ipAddress, port);            
            Socket socket = new Socket(expectAddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Connect(ipe);

                string str = Console.ReadLine();
                byte[] strEncoded = Encoding.Unicode.GetBytes(str);
                int bytesend = socket.Send(strEncoded);

                Console.WriteLine("Byte send {0}", bytesend);

                string data = string.Empty;
                Console.WriteLine("Receiving...");
                while (true)
                {
                    byte[] bytes = new byte[1024];
                    int bytesRec = socket.Receive(bytes);
                    data += Encoding.Unicode.GetString(bytes, 0, bytesRec);

                    if (data.IndexOf("<EOF>") > -1)
                    {
                        Console.WriteLine("Found EOF ...");
                        break;
                    }
                }

                Console.WriteLine("Text return : {0}", data);

                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Console.WriteLine("Socket closed");
            }
            catch (ArgumentNullException ae)
            {
                Console.WriteLine("ArgumentNullException : {0}", ae.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }
    }
}
