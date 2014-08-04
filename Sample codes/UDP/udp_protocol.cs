namespace Sample
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Net.Sockets;
    using System.Net;
    
    public class UDP_protocol
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

        public struct PlayerInfo
        {
            public byte playerID, locationX, locationY;
        }

        public struct PacketInfo
        {
            public int sequenceNumber;
            public long sentTicks;
            public int retryCount;
        }

        const int HeaderData = 0x01020304;
        const int HeaderAck = 0x05060708;
        const int SizeData = (sizeof(int) + sizeof(int) + sizeof(byte) * 3);
        const int SizeAck = (sizeof(int) + sizeof(int) + sizeof(byte));

        // sender
        PlayerInfo[] sentPlayerInfo = new PlayerInfo[256];
        PacketInfo[] sentPacketInfo = new PacketInfo[256];
        Socket senderSocket;
        EndPoint senderEndPoint;
        byte[] bufferAck = new byte[SizeAck];
        public AsyncCallback onReceiveAck = new AsyncCallback(OnReceiveAck);
        bool closingSender = false;

        void OpenSenderSocket(IPAddress ip, int port)
        {
            senderEndPoint = new IPEndPoint(ip, port);
            senderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint bindEndPoint = new IPEndPoint(IPAddress.Any, 0);
            senderSocket.Bind(bindEndPoint); // any free port
            ListenForAcks();
            // start 'ack waiting' thread
            ThreadPool.QueueUserWorkItem(CheckPendingAcks, 0);
            // Note: this sample is simplified code:
            // ignores data structure locking and efficient thread synchronization, and ommits object dispose
        }

        public void StopSenderSocket()
        {
            closingSender = true;
        }

        public void CheckPendingAcks(object o)
        {
            Console.WriteLine("Checking for missing ACKs");
            byte currentPosition = 0;
            for (; !closingSender; )
            {
                Thread.Sleep(10);
                ResendNextPacket(ref currentPosition, 10, DateTime.Now.Ticks - TimeSpan.TicksPerSecond / 10);
            }
        }

        public void ListenForAcks()
        {
            EndPoint endPoint = new IPEndPoint(0, 0);
            senderSocket.BeginReceiveFrom
    (bufferAck, 0, SizeAck, SocketFlags.None, ref endPoint, onReceiveAck, (object)this);
        }

        static void OnReceiveAck(IAsyncResult result)
        {
            UDP_protocol uf = (UDP_protocol)result.AsyncState;
            EndPoint remoteEndPoint = new IPEndPoint(0, 0);
            int bytesRead = 0;
            // full implementation will try..catch EndReceiveFrom
            bytesRead = uf.senderSocket.EndReceiveFrom(result, ref remoteEndPoint);
            uf.ProcessIncomingAck(uf.bufferAck, bytesRead);
            uf.ListenForAcks();
        }

        int sequenceNumber = 1;

        void SendPlayerInfoData(byte playerID, byte locationX, byte locationY)
        {
            PlayerInfo info;
            sequenceNumber++;
            int sequence = sequenceNumber;
            info.playerID = playerID;
            info.locationX = locationX;
            info.locationY = locationY;
            SendPlayerInfo(info, sequence, false);
        }


        void SendPlayerInfo(PlayerInfo info, int sequenceNumber, bool retry)
        {
            int pos = 0;
            byte[] packetData = new byte[SizeData];
            WriteIntToByteArray(packetData, pos, HeaderData); pos+=sizeof(int);
            WriteIntToByteArray(packetData, pos, sequenceNumber); pos+=sizeof(int);
            packetData[pos++] = info.playerID;
            packetData[pos++] = info.locationX;
            packetData[pos] = info.locationY;
            // send
            UpdatePlayerInfo(info, sequenceNumber, retry);
            senderSocket.SendTo(packetData, SizeData, SocketFlags.None, senderEndPoint);
        }

        void UpdatePlayerInfo(PlayerInfo info, int sequenceNumber, bool retry)
        {
            byte playerID = info.playerID;
            sentPlayerInfo[playerID].playerID = info.playerID;
            sentPlayerInfo[playerID].locationX = info.locationX;
            sentPlayerInfo[playerID].locationY = info.locationY;
            sentPacketInfo[playerID].sequenceNumber = sequenceNumber;
            sentPacketInfo[playerID].sentTicks = DateTime.Now.Ticks;
            if (!retry) sentPacketInfo[playerID].retryCount = 0;
        }

        void ResendPlayerInfo(byte playerID)
        {
            SendPlayerInfo(sentPlayerInfo[playerID], sentPacketInfo[playerID].sequenceNumber, true);
            sentPacketInfo[playerID].retryCount++;
            sentPacketInfo[playerID].sentTicks = DateTime.Now.Ticks;
            Console.WriteLine("Resending packet {0} {1} {2}", playerID, sentPacketInfo[playerID].sequenceNumber, sentPacketInfo[playerID].retryCount);
        }

        void ProcessIncomingAck(byte[] packetData, int size)
        {
            int pos = 0;
            byte playerID = 0;
            int sequenceNumber = 0;
            if (size != SizeAck) return; // error packet is ignored
            if (ReadIntFromByteArray(packetData, pos) != HeaderAck) return; // bad header
            pos += sizeof(int);
            sequenceNumber = ReadIntFromByteArray(packetData, pos); 
            pos += sizeof(int);
            playerID = packetData[pos];
            // remove from 'needing ACK' list if ACK for newest info
            if (sentPacketInfo[playerID].sequenceNumber == sequenceNumber)
            {
                Console.WriteLine("Received current ACK on {0} {1}", playerID, sequenceNumber);
                ProcessPacketAck(playerID, sequenceNumber);
            }
            else
            {
                Console.WriteLine("Received outdated ACK on {0} {1} latest {2}", playerID, sequenceNumber, sentPacketInfo[playerID].sequenceNumber);
               
            }
        }

        void ProcessPacketAck(byte playerID, int sequenceNumber)
        {
            sentPacketInfo[playerID].sequenceNumber = 0; // 0 represents 'not pending ack'
        }

        void ResendNextPacket(ref byte currentPosition, byte maxPacketsToSend, long olderThan)
        {
            int packetsSent = 0;
            byte newPosition = (byte)((currentPosition + 1) %256);
            for (; newPosition != currentPosition && packetsSent < maxPacketsToSend; newPosition=(byte)((newPosition + 1)%256))
            {
                if (sentPacketInfo[newPosition].sequenceNumber != 0)
                {
                    Console.WriteLine("Checking if ACK timeout for {0} {1} {2}", newPosition, sentPacketInfo[newPosition].sentTicks - olderThan, sentPacketInfo[newPosition].retryCount);
                }
                if ((sentPacketInfo[newPosition].sequenceNumber != 0) &&
                    sentPacketInfo[newPosition].sentTicks < olderThan)
                {
                    if (sentPacketInfo[newPosition].retryCount > 4)
                    {
                        Console.WriteLine("Too many retries, should fail connection");
                    }
                    else
                    {
                        ResendPlayerInfo(newPosition);
                        packetsSent++;
                    }
                }
            }
            currentPosition = newPosition;
        }

        //receiver
        PlayerInfo[] recvPlayerInfo = new PlayerInfo[256];
        int[] recvSequenceNumber = new int[256];
        Socket receiverSocket;
        EndPoint receiverAckEndPoint = new IPEndPoint(0, 0);
        byte[] bufferData = new byte[SizeData];
        public AsyncCallback onReceiveData = new AsyncCallback(OnReceiveData);

        void OpenReceiverSocket(int port)
        {
            EndPoint receiverEndPoint = new IPEndPoint(IPAddress.Any, port);
            receiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiverSocket.Bind(receiverEndPoint); 
            ListenForData();
        }

        public void ListenForData()
        {
            EndPoint endPoint = new IPEndPoint(0, 0);
            receiverSocket.BeginReceiveFrom
    (bufferData, 0, SizeData, SocketFlags.None, ref endPoint, onReceiveData, (object)this);
        }

        static void OnReceiveData(IAsyncResult result)
        {
            UDP_protocol uf = (UDP_protocol)result.AsyncState;
            EndPoint remoteEndPoint = new IPEndPoint(0, 0);
            int bytesRead = 0;
            // full implementation will try..catch EndReceiveFrom
            // update sender Ack address; this sample assumes only one possible sender
            bytesRead = uf.receiverSocket.EndReceiveFrom(result, ref uf.receiverAckEndPoint);
            uf.ProcessIncomingPlayerInfo(uf.bufferData, bytesRead);
            uf.ListenForData();
        }

        void SendAck(byte playerID, int sequenceNumber)
        {
            // this code would randomly not send some ACKs - testing only
            if (new Random().Next(3) == 0)
            {
                Console.WriteLine("NOT sending ACK {0} {1}", playerID, sequenceNumber);
                return; 
            }
            int pos = 0;
            byte[] packetData = new byte[(sizeof(int) + sizeof(int) + sizeof(byte))];
            WriteIntToByteArray(packetData, pos, HeaderAck); pos += sizeof(int);
            WriteIntToByteArray(packetData, pos, sequenceNumber); pos += sizeof(int);
            packetData[pos++] = playerID;
            // send
            receiverSocket.SendTo(packetData, SizeAck, SocketFlags.None, receiverAckEndPoint);
            Console.WriteLine("Sending Ack {0} {1}", playerID, sequenceNumber);
        }

        void ProcessIncomingPlayerInfo(byte[] packetData, int size)
        {
            PlayerInfo info;
            int sequenceNumber = 0;
            int pos = 0;
            if (size != SizeData) return; // error packet is ignored
            if (ReadIntFromByteArray(packetData, pos) != HeaderData) return; // bad header
            pos += sizeof(int);
            sequenceNumber = ReadIntFromByteArray(packetData, pos);
            pos += sizeof(int);
            info.playerID = packetData[pos++];
            info.locationX = packetData[pos++];
            info.locationY = packetData[pos++];

            // validate dup
            if (sequenceNumber >= recvSequenceNumber[info.playerID])
            {
                SendAck(info.playerID, sequenceNumber);
            }
            if (sequenceNumber > recvSequenceNumber[info.playerID])
            {
                // update data
                recvSequenceNumber[info.playerID] = sequenceNumber;
                recvPlayerInfo[info.playerID].playerID = info.playerID;
                recvPlayerInfo[info.playerID].locationX = info.locationX;
                recvPlayerInfo[info.playerID].locationY = info.locationY;
                // process packet
                Console.WriteLine("Received update: {0} ({1},{2})", info.playerID, info.locationX, info.locationY);
            } // else older packet, don't process or send ACK
        }

        public static void Main(String[] args)
        {
            bool reportError = true;
            if (args.Length == 1)
            {
                if ("send".CompareTo(args[0]) == 0)
                {
                    UDP_protocol uf = new UDP_protocol();
                    uf.OpenSenderSocket(IPAddress.Parse("127.0.0.1"), 667);
                    for (int i = 0; i < 100; i++)
                    {
                        uf.SendPlayerInfoData((byte)(i % 20), (byte)i, (byte)(i + 1));
                        Thread.Sleep(10);
                    }
                    Thread.Sleep(9000);

                    reportError = false;
                }
                else
                    if ("receive".CompareTo(args[0]) == 0)
                    {
                        UDP_protocol uf = new UDP_protocol();
                        uf.OpenReceiverSocket(667);
                        Thread.Sleep(10000);
                        reportError = false;
                    }
            }

            if (reportError)
            {
                Console.WriteLine("Use: UDP_protocol send/receive");
            }
        }
    }
}
