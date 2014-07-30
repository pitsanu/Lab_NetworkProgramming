using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Security;

namespace SocketTut
{
    class Program
    {
        static void Main(string[] args)
        {
            AddressFamily expectAddressFamily = AddressFamily.InterNetwork;
            Socket tcpServer = new Socket(expectAddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("Created -> tcpServer");

            //Socket udpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //Dns.GetHostEntry(IPAddress.Parse("x.x.x.x"));

            string hostname = Dns.GetHostName();
            Console.WriteLine("Host name -> {0}", hostname);

            //IPHostEntry ipHostInfo = Dns.GetHostEntry(hostname);
            //Console.WriteLine("All return hosts addresses:");

            IPAddress[] ipList = Dns.GetHostAddresses(hostname);
            Console.WriteLine("All return hosts addresses:");
            foreach (var i in ipList)
            {
                Console.WriteLine("\t-> {0}, AddressFamily {1}", i, i.AddressFamily);                
            }

            IPAddress ipAddress = null;// = ipHostInfo.AddressList[0];
            foreach (var i in ipList)
            {
                if (i.AddressFamily == expectAddressFamily)
                {
                    ipAddress = i;
                    break;
                }
                else
                {
                    continue;
                }
            }

            Console.WriteLine("Got address at -> {0}", ipAddress);

            int port = 11000;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            Console.WriteLine("Created endpoint at port -> {0}", localEndPoint);

            /* Bind socket and endpoint */
            try
            {
                tcpServer.Bind(localEndPoint);
                tcpServer.Listen(100);
              
                int count = 1;
                while (true)
                {
                    try
                    {

                        Console.WriteLine("Waiting for a connection {0}...", count);
                        Socket handler = tcpServer.Accept();
                        Console.WriteLine("Connection {0} accepted...", count);


                        String data = null;
                        
                        while (true)
                        {
                            byte[] bytes = new byte[1024];
                            int bytesRec = handler.Receive(bytes);
                            data += Encoding.Unicode.GetString(bytes, 0, bytesRec);

                            if (data.IndexOf("<EOF>") > -1)
                            {
                                Console.WriteLine("Found EOF ...");
                                break;
                            }
                        }
                        

                        Console.WriteLine("Text received : {0}", data);

                        byte[] msg = Encoding.Unicode.GetBytes(data);
                        handler.Send(msg);
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        Console.WriteLine("Connection {0} closed", count);

                        count++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Connection {0} error {1}", count, ex.Message);

                        count++;
                    }
                }
            }           
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("ArgumentNullException : {0}", ex.Message);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException : {0}", ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine("ObjectDisposedException : {0}", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("InvalidOperationException : {0}", ex.Message);
            }
            catch (SecurityException ex)
            {
                Console.WriteLine("SecurityException : {0}", ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : {0}", ex.Message);
            }     
        }
    }
}
