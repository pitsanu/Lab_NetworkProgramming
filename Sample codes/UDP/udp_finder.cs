namespace Sample
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Net.Sockets;
    using System.Net;

    public class UDP_finder
    {
        static void WriteIntToByteArray(byte[] array, int position, int value)
        {
            array[position++] = (byte)(value & 0xff); value = value >> 8;
            array[position++] = (byte)(value & 0xff); value = value >> 8;
            array[position++] = (byte)(value & 0xff); value = value >> 8;
            array[position++] = (byte)(value & 0xff); value = value >> 8;
        }
        static int ReadIntFromByteArray(byte[] array, int position)
        {
            int value = 0; position += 3;
            value += array[position--]; value = value << 8;
            value += array[position--]; value = value << 8;
            value += array[position--]; value = value << 8;
            value += array[position--]; 
            return value;
        }

        const int requestIdentifier = 0x12345678;
        const int responseIdentifier = 0x20bcd0ef;

        class FindRequest
        {
            public int serviceID;
            public int responsePort;
            public int SerializeToPacket(byte[] packet)
            {
                WriteIntToByteArray(packet, 0, requestIdentifier);
                WriteIntToByteArray(packet, 4, serviceID);
                WriteIntToByteArray(packet, 8, responsePort);
                return 12;
            }
            public FindRequest() { }
            public FindRequest(byte[] packet)
            {
                if (ReadIntFromByteArray(packet, 0) != requestIdentifier) throw new Exception("bad request identifier " + ReadIntFromByteArray(packet, 0));
                serviceID = ReadIntFromByteArray(packet, 4);
                responsePort = ReadIntFromByteArray(packet, 8);
            }
        }

        class FindResult
        {
            public int serviceID;
            public int SerializeToPacket(byte[] packet)
            {
                WriteIntToByteArray(packet, 0, responseIdentifier);
                WriteIntToByteArray(packet, 4, serviceID);
                return 8;
            }
            public FindResult() { }
            public FindResult(byte[] packet)
            {
                if (ReadIntFromByteArray(packet, 0) != responseIdentifier) throw new Exception("bad response identifier");
                serviceID = ReadIntFromByteArray(packet, 4);
            }
        }

        IPAddress multicastGroup = IPAddress.Parse("239.255.255.19");
        const int multicastPort = 6000;
        int responsePort = 0;
        public byte[] findResultBuffer = new byte[8];
        public byte[] findRequestBuffer = new Byte[12];

        public Socket responseSocket;
        public AsyncCallback onReceiveResponse = new AsyncCallback(OnReceiveResponse);
        public AsyncCallback onReceiveRequest = new AsyncCallback(OnReceiveRequest);
        public int currentServiceID;

        // advertise a service for blockFor miliseconds
        public void FindMe(int blockFor, int serviceID)
        {
            currentServiceID = serviceID;
            // listen to find requests
            responseSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint responseEndPoint = new IPEndPoint(IPAddress.Any, multicastPort);
            responseSocket.Bind(responseEndPoint);
            responseSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastGroup));
            ListenForRequests();
            Console.WriteLine("Advertising service; Waiting for find requests");
            Thread.Sleep(blockFor);
        }

        public void ListenForResponses()
        {
            EndPoint endPoint = new IPEndPoint(0, 0);
            responseSocket.BeginReceiveFrom
    (findResultBuffer, 0, 8, SocketFlags.None, ref endPoint, onReceiveResponse, (object)this);
        }

        public void ListenForRequests()
        {
            EndPoint endPoint = new IPEndPoint(0, 0);
            responseSocket.BeginReceiveFrom
    (findRequestBuffer, 0, 12, SocketFlags.None, ref endPoint, onReceiveRequest, (object)this);
        }


        public void Finder(int waitFor, int serviceID)
        {
            currentServiceID = serviceID;
            // start listening for responses before sending the request
            responseSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint responseEndPoint = new IPEndPoint(IPAddress.Any, 0);
            responseSocket.Bind(responseEndPoint);
            responsePort = ((IPEndPoint)(responseSocket.LocalEndPoint)).Port;
            ListenForResponses();
            // prepare request
            FindRequest fr = new FindRequest();
            fr.serviceID = serviceID; fr.responsePort = responsePort;
            int requestLength = fr.SerializeToPacket(findRequestBuffer);
            //send request
            Socket requestSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint requestEndPointDestination = new IPEndPoint(multicastGroup, multicastPort);
            requestSocket.SendTo(findRequestBuffer, requestLength, SocketFlags.None, requestEndPointDestination);
            requestSocket.Close();

            //wait for responses
            Thread.Sleep(waitFor);
        }

        static void OnReceiveResponse(IAsyncResult result)
        {
            UDP_finder uf = (UDP_finder)result.AsyncState;
            EndPoint remoteEndPoint = new IPEndPoint(0, 0);
            int bytesRead = 0;

            // full implementation will try..catch EndReceiveFrom
            bytesRead = uf.responseSocket.EndReceiveFrom(result, ref remoteEndPoint);
            FindResult response = new FindResult(uf.findResultBuffer);
            uf.ListenForResponses();

            Console.WriteLine("Found service {0} on {1}", response.serviceID, ((IPEndPoint)remoteEndPoint).Address);
        }

        static void OnReceiveRequest(IAsyncResult result)
        {
            UDP_finder uf = (UDP_finder)result.AsyncState;
            EndPoint remoteEndPoint = new IPEndPoint(0, 0);
            int bytesRead = 0;

            // full implementation will try..catch EndReceiveFrom
            bytesRead = uf.responseSocket.EndReceiveFrom(result, ref remoteEndPoint);
            FindRequest request = new FindRequest(uf.findRequestBuffer);

            Console.WriteLine("Request for service {0} response port {1} sent from {2} {3}", 
                request.serviceID, request.responsePort,
                ((IPEndPoint)remoteEndPoint).Port,
                ((IPEndPoint)remoteEndPoint).Address);

            // prepare result
            FindResult fr = new FindResult();
            fr.serviceID = uf.currentServiceID; 
            int requestLength = fr.SerializeToPacket(uf.findResultBuffer);
            //send result
            Socket requestSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint requestEndPointDestination = new IPEndPoint(((IPEndPoint)remoteEndPoint).Address, request.responsePort);
            requestSocket.SendTo(uf.findResultBuffer, requestLength, SocketFlags.None, requestEndPointDestination);
            requestSocket.Close();
            uf.ListenForRequests();
        }

        public static void Main(String[] args)
        {
            bool reportError = true;
            if (args.Length == 1)
            {
                if ("find".CompareTo(args[0]) == 0)
                {
                    UDP_finder uf = new UDP_finder();
                    uf.Finder(10000, 666);
                    reportError = false;
                }
                else
                    if ("advertise".CompareTo(args[0]) == 0)
                    {
                        UDP_finder uf = new UDP_finder();
                        uf.FindMe(10000, 666);
                        reportError = false;
                    }
            }

            if (reportError)
            {
                Console.WriteLine("Use: UDP_finder find/advertise");
            }
        }
    }
}
