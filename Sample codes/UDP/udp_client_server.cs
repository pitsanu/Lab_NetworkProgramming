namespace Sample
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Net.Sockets;
    using System.Net;


    public class UDP_client_server
    {
        public static void Main(String[] args)
        {
            UDP_helper uh = new UDP_helper();

            if ("client".CompareTo(args[0]) == 0)
            {
                uh.sendToAddr = args[1];
                uh.sendToPort = int.Parse(args[2]);
            }
            else
                if ("server".CompareTo(args[0]) == 0)
                {
                    uh.IsServer = true;
                    uh.listenOnPort = int.Parse(args[1]);

                }
                else
                    if ("forward".CompareTo(args[0]) == 0)
                    {
                        uh.IsForward = true;
                        uh.listenOnPort = int.Parse(args[1]);
                        uh.forwardToAddr = args[2];
                        uh.forwardToPort = int.Parse(args[3]);
                        uh.listenWait = 600;
                    }
                    else
                    {
                        Console.WriteLine("Usage: .exe sends multiple udp messages");
                        Console.WriteLine("Usage: .exe client sendtoaddr sendtoport");
                        Console.WriteLine("Usage: .exe server listenport");
                        Console.WriteLine("Usage: .exe forward listenport forwardtoaddr forwardtoport");
                        return;
                    }
            Console.WriteLine(args[0]);

            uh.Run();
        }
    }
    public class UDP_helper
    {
        // public parameters
        public bool IsServer = false;
        public bool IsForward = false;
        public bool IsMulticast = false;
        public int interfaceIndex = 0; //default
        public int portNum = 3705; //default
        public int sendToPort = 3705; //default
        public int listenOnPort = 3705; //default
        public bool sockReuse = true;
        public bool sockExclusive = false;
        public int listenWait = 60;
        public int messageCount = 200;
        public string sendToAddr = "127.0.0.1";
        public string forwardToAddr = "127.0.0.1";
        public int forwardToPort = 3706;

        void MyWriteLine(string s)
        {
            System.Console.Out.WriteLine(s);
        }

        void MyWrite(string s)
        {
            System.Console.Out.Write(s);
        }

        int sendFromPort;

        void ReportConfig()
        {
            Console.WriteLine("Running " +
                (IsServer ? "server " : "client ") +
                "on " + sendToAddr.ToString() + " : " +
                (IsServer ? listenOnPort.ToString() :
                sendToPort.ToString() + " from " + sendFromPort.ToString())
                );
        }
        // fwd would look like:
        // startfwd(porttolisten, fwdtoaddr, fwdtoport, fwdoptions
        // fwdoptions: drop_probability, delay min, max, variation
        // for any protocol should be called twice

        public void Run()
        {
            ReportConfig();
            try
            {
                if (IsForward)
                {
                    DoForward();
                }
                else
                    if (IsServer)
                    {
                        DoServer();
                    }
                    else
                    {
                        DoClient();
                    }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                if (e is SocketException)
                {
                    SocketException se = (SocketException)e;
                    Console.WriteLine("se.ErrorCode ", se.ErrorCode);
                }
            }


        }
        void InitBuffers()
        {
            // init arrays
            int i = 0;
            for (i = 0; i < sendBufferSize; i++)
            {
                sendBuffer1[i] = 1;
                sendBuffer2[i] = 2;
                receiveBuffer1[i] = 101;
                receiveBuffer2[i] = 102;
                receiveBuffer2[i] = 103;
            }
        }

        void OnReceiveFrom(IAsyncResult result)
        {
            OnReceiveFromHelper(result, false);
        }

        void OnReceiveFromforward(IAsyncResult result)
        {
            OnReceiveFromHelper(result, false);
        }

        void OnReceiveFromV6(IAsyncResult result)
        {
            OnReceiveFromHelper(result, true);
        }

        void setSocketReuse(Socket socket)
        {
            if (sockReuse)
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                MyWriteLine("SocketOptionName.ReuseAddress");
            }

            if (sockExclusive)
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, 1);
                MyWriteLine("SocketOptionName.ExclusiveAddressUse");
            }
        }

        static int count = 0;
        public bool DropPacket()
        {
            return false;
            /* enable packet dropping
            e.g. to drop every 3rd packet
            count = (count + 1) % 3;
            return (count == 0);
             */
        }

        Random r = new Random();

        public int GetForwardDelay()
        {
            return 500 + r.Next(0, 9999);
        }

        public class SendItem : IComparable
        {
            public byte[] buffer;
            public int length;
            public EndPoint forwardEndPoint;
            public Socket socket;
            public DateTime sendIn;
            public int CompareTo(object obj)
            {
                if (obj is SendItem)
                {
                    SendItem temp = (SendItem)obj;
                    return sendIn.CompareTo(temp.sendIn);
                }
                throw new ArgumentException("object is not a SendItem");
            }
        }

        public List<SendItem> Sends = new List<SendItem>();
        public void ScheduleSend(byte[] b, int length, EndPoint forwardEndPoint, Socket socket, int fwdIn)
        {
            SendItem si = new SendItem();
            si.buffer = b; si.length = length; si.forwardEndPoint = forwardEndPoint; si.socket = socket;
            si.sendIn = DateTime.Now.AddMilliseconds(fwdIn);
            // A full implementation would include locking
            Sends.Add(si);
            Sends.Sort();
        }

        public void SendOneItem()
        {
            for (int i = 0; i < Sends.Count; i++)
            {
                SendItem si = Sends[i];

                if (si != null && si.sendIn < DateTime.Now)
                {
                    Console.WriteLine("sending {0} {1} {2}", i, (DateTime.Now - si.sendIn).TotalMilliseconds, si.buffer[0]);

                    // should send
                    //rfd.ForwardSocket.SendTo(b, length,
                    si.socket.SendTo(si.buffer, si.length, SocketFlags.None, si.forwardEndPoint);
                    Sends.Remove(si);
                }
            }

        }

        void ThreadSendProc(Object stateInfo)
        {
            try
            {
                Console.WriteLine("***send***");
                while (true) 
                {
                    SendOneItem();
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(10000);
            }
        }

        void DumpChar(byte b, int c)
        {
            if (c == 1)
            {
                if (b > 31 && b < 128)
                {
                    Console.Write("{0}", (char)(b));
                }
                else
                {
                    Console.Write("\\x{0:x}", (b));
                }
            }
            else if (c < 5)
            {
                for (int j = 0; j < c; j++)
                {
                    DumpChar(b, 1);
                }
            }
            else
            {
                if (b > 31 && b < 128)
                {
                    Console.Write("({0}){1}", c, (char)(b));
                }
                else
                {
                    Console.Write("({0})\\x{1:x}", c, b);
                }
            }
        }
        public void Dumpbuf(byte[] buffer, int offset, int count)
        {
            int lastchar = 257; //impossible value
            int charcount = 1;
            for (int i = offset; i < buffer.Length && i < offset + count; i++)
            {
                if (buffer[i] == lastchar &&
                    !(i + 1 == buffer.Length || i + 1 == offset + count))
                {
                    charcount++;
                }
                else
                {
                    // dump prev
                    if (lastchar != 257)
                    {
                        DumpChar((byte)lastchar, charcount);
                    }
                    charcount = 1;
                    lastchar = buffer[i];
                }
            }
            DumpChar((byte)lastchar, charcount);

        }


        // must differentiate v4 and v6 to get the return addr
        void OnReceiveFromHelper(IAsyncResult result, bool IPv6)
        {
            //Console.Write(">");
            ReceiveFromData rfd = (ReceiveFromData)result.AsyncState;
            Socket receiveSocket = rfd.socket;
            EndPoint remoteEndPoint;
            if (IPv6)
            {
                remoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
            }
            else
            {
                remoteEndPoint = new IPEndPoint(0, 0); // hack to create it..
            }
            int bytesRead = 0;

            try
            {
                bytesRead = receiveSocket.EndReceiveFrom(result, ref remoteEndPoint);
            }
            catch (SocketException e)
            {
                MyWriteLine("SocketException " + e.ErrorCode);
                switch (e.ErrorCode)
                {
                    case 995: break;// thread terminated, expected
                    default:
                        {
                            MyWriteLine("socketexception: " + e.ToString());
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                MyWriteLine("exception: " + e.ToString());
            }

            // extract the EndPoint and cast it to an IPEndPoint. Use the IPEndPoint.Address method to obtain the IP address and the IPEndPoint.Port method to obtain the port number.
            MyWriteLine("[" + receivedBuffers.ToString() + "]" +
                "OnReceiveFrom called data= " +
                rfd.buffer[0].ToString() + " size=" + bytesRead + "/" + rfd.length +
                " host=" +
                ((IPEndPoint)remoteEndPoint).Address.ToString() + ":" +
                ((IPEndPoint)remoteEndPoint).Port.ToString());
            Dumpbuf(rfd.buffer, 0, bytesRead);

            byte[] b = null;
            int length = 0;
            // if fwd then dup buffer
            if (rfd.ForwardSocket != null)
            {
                // b= new byte[rfd.length];
                b = (byte[])rfd.buffer.Clone();
                // pyf get real packet size, use that, send with that size not buffersize
                length = bytesRead;
                EndPoint forwardEndPoint =
                    new IPEndPoint(
                    IPAddress.Parse(rfd.UDP_helper.forwardToAddr),
                    rfd.UDP_helper.forwardToPort); 

                if (!DropPacket())
                {
                    int fwdIn = GetForwardDelay();
                    Console.Write("sched fwd: {0}", fwdIn);
                    ScheduleSend(b, length, forwardEndPoint, rfd.ForwardSocket, fwdIn);
                }
                else
                {
                    Console.WriteLine("drop");
                }


            }
            rfd.BeginReceiveFrom();
            Interlocked.Increment(ref receivedBuffers); 
        }

        class ReceiveFromData
        {
            public AsyncCallback onReceiveFrom;
            public byte[] buffer;
            public int length;
            public EndPoint endPoint;
            public Socket socket;
            public Socket ForwardSocket;
            public UDP_helper UDP_helper;
            public ReceiveFromData(
                ref byte[] buffer, int length, ref EndPoint endPoint,
                ref AsyncCallback onReceiveFrom, ref Socket socket, Socket ForwardSocket,
                UDP_helper UDP_helper
                )
            {
                this.onReceiveFrom = onReceiveFrom;
                this.buffer = buffer;
                this.length = length;
                this.endPoint = endPoint;
                this.socket = socket;
                this.ForwardSocket = ForwardSocket;
                this.UDP_helper = UDP_helper;
            }

            public void BeginReceiveFrom()
            {
                socket.BeginReceiveFrom
                    (buffer, 0, length, SocketFlags.None, ref endPoint, onReceiveFrom, (object)this);
            }
        }

        // prepare send buffers
        const int sendBufferSize = 32 * 1024 - 40;
        byte[] sendBuffer1 = new byte[sendBufferSize];
        byte[] sendBuffer2 = new byte[sendBufferSize];
        byte[] receiveBuffer1 = new byte[sendBufferSize];
        byte[] receiveBuffer2 = new byte[sendBufferSize];
        byte[] receiveBuffer3 = new byte[sendBufferSize];
        int receivedBuffers = 0;

        void DoServer()
        {
            // listen on a socket for N seconds; write received message (int)
            InitBuffers();
            AsyncCallback onReceiveFrom1 = new AsyncCallback(OnReceiveFrom);

            Socket socketA1
   = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint bindEndPointA1 = new IPEndPoint(IPAddress.Any, listenOnPort);
            // bind for listening to unicast.
            socketA1.Bind(bindEndPointA1);

            // start listening
            ReceiveFromData rfd = new ReceiveFromData(ref receiveBuffer1, receiveBuffer1.Length,
                ref bindEndPointA1, ref onReceiveFrom1, ref socketA1, null, this);
            rfd.BeginReceiveFrom();

            WaitReceivedMessages(messageCount + 1);
        }

        void DoClient()
        {
            // listen on a socket for N seconds; write received message (int)
            InitBuffers();

            Socket socketA1
   = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPAddress sendTo = IPAddress.Parse(sendToAddr);

            EndPoint sendEndPointA1 = new IPEndPoint(sendTo, sendToPort);
            {
                AsyncCallback onReceiveFrom1 = new AsyncCallback(OnReceiveFrom);
                listenOnPort = 0;// sendToPort + 1;

                EndPoint bindEndPointA1 = new IPEndPoint(IPAddress.Any, listenOnPort); 
                socketA1.Bind(bindEndPointA1);


                ReceiveFromData rfd = new ReceiveFromData(ref receiveBuffer2, receiveBuffer2.Length,
                    ref bindEndPointA1, ref onReceiveFrom1, ref socketA1, null, this);
                rfd.BeginReceiveFrom();
            }

            for (byte i = 0; i < messageCount; i++)
            {
                sendBuffer1[0] = i;

                socketA1.SendTo(sendBuffer1, Math.Min(sendBufferSize, 1024),
                    SocketFlags.None, sendEndPointA1);
                MyWriteLine("buffer " + i + " sent");
                Thread.Sleep(100);
            }
        }

        void DoForward()
        {
            // listen on a socket for N seconds; write received message (int)
            InitBuffers();
            AsyncCallback onReceiveFrom1 = new AsyncCallback(OnReceiveFrom);
            AsyncCallback onReceiveFromForward1 = new AsyncCallback(OnReceiveFromforward);

            // prepare send thread
            if (!ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadSendProc), null))
            {
                throw new Exception("cannot QueueUserWorkItem ");
            }

            Socket socketA1
   = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Socket socketB1
   = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            EndPoint bindEndPointA1 = new IPEndPoint(IPAddress.Any, listenOnPort);
            // bind for listening to unicast.
            socketA1.Bind(bindEndPointA1);

            // prepare send socket
            {
                EndPoint bindEndPointB1 = new IPEndPoint(IPAddress.Any, 0);
                socketB1.Bind(bindEndPointB1);

                ReceiveFromData rfd2 =
                    new ReceiveFromData(
                    ref receiveBuffer2, receiveBuffer2.Length,
                    ref bindEndPointB1, ref onReceiveFrom1, ref socketB1, null, this);
                rfd2.BeginReceiveFrom();
            }

            // start listening
            ReceiveFromData rfd = new ReceiveFromData(
                ref receiveBuffer1, receiveBuffer1.Length, ref bindEndPointA1,
                ref onReceiveFromForward1, ref socketA1, socketB1, this);
            rfd.BeginReceiveFrom();
            ReceiveFromData rfd3 = new ReceiveFromData(
                ref receiveBuffer3, receiveBuffer3.Length, ref bindEndPointA1,
                ref onReceiveFromForward1, ref socketA1, socketB1, this);
            rfd3.BeginReceiveFrom();


            WaitReceivedMessages(messageCount + 1);
        }



        void WaitReceivedMessages(int messages)
        {
            MyWriteLine("waiting for " + messages.ToString() + " sent messages for " + listenWait.ToString() + " seconds");
            WaitForCounterUsingSleeps(ref receivedBuffers, messages, 100, listenWait * 10);
            MyWriteLine("received: " + receivedBuffers.ToString());
        }

        void WaitReceivedMessage() 
        {
            WaitReceivedMessages(1);
        }

        public IPAddress GetBindIPAddress(AddressFamily DesiredFamily)
        {
            IPAddress[] addresses = Dns.Resolve(Dns.GetHostName()).AddressList;

            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == DesiredFamily)
                {
                    MyWriteLine("DNS resolved localhost to " + address.ToString() + " for addressFamily " + DesiredFamily.ToString());
                    return address;
                }
            }
            throw new Exception("Failed to resolve machine name to desired address family");
        }

        bool WaitForCounterUsingSleeps(ref int counter, int counterValue, int segmentSleepTime, int maxsegments)
        {
            for (int i = 0; i < maxsegments; i++)
            {
                Thread.Sleep(segmentSleepTime);
                if (counter == counterValue)
                    break;
                MyWrite(".");
            }
            return (counter == counterValue);
        }

    }
}
  	


