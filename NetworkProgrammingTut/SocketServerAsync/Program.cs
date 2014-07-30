using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketServerAsync
{
    class Program
    {
        public void StartListening()
        {
            AddressFamily expectAddressFamily = AddressFamily.InterNetwork;

            string hostname = Dns.GetHostName();
            Console.WriteLine("Host name -> {0}", hostname);
            IPAddress[] ipList = Dns.GetHostAddresses(hostname).Where(o => o.AddressFamily == expectAddressFamily).ToArray();

            IPAddress ipAddress = ipList[0];

            int port = 11000;
            IPEndPoint ipe = new IPEndPoint(ipAddress, port);
            Socket listener = new Socket(expectAddressFamily, SocketType.Stream, ProtocolType.Tcp);

            int backlog = 100;
            try
            {
                listener.Bind(ipe);
                listener.Listen(backlog);

                //int count = 1;
                //while (true)
                //{
                    Console.WriteLine("BeginAccept...");
                    listener.BeginAccept(acceptCallback, listener);

                    int task = 1;
                    while (true)
                    {
                        Console.WriteLine("Do some long task {0}", task);
                        task++;
                        Thread.Sleep(2000);
                    }    
                //}

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception : {0}", ex.Message);
            }
        }

        private void acceptCallback(IAsyncResult ar)
        {
            // Add the callback code here.
            Console.WriteLine("Enter acceptCallback ... ");

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

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
            Console.WriteLine("Connection closed");
        }


        static void Main(string[] args)
        {
            Program p = new Program();

            p.StartListening();
        }
    }
}
