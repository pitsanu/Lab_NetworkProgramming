Code samples in this package:

udp_finder:
----------
This sample advertises a server using UDP multicast. The 'find' command will send a multicast message, and machines running the 'advertise' command will be found
Expected output:
>udp_finder.exe advertise
Advertising service; Waiting for find requests
Request for service 666 response port 4669 sent from 4670 192.30.199.183
 
>udp_finder.exe find
Found service 666 on 192.30.199.184

To build: csc udp_finder.cs /r:system.xml.dll /r:system.dll



udp_client_server:
-----------------
This sample sends, receives, and forwards udp (unicast) packets

Use:

udp_client_server.exe server port-number
- will listen on port port-number
udp_client_server.exe client ip-address port-number
- will send to a specific ip-address and port-number
udp_client_server.exe forward listen-port forward-ip forward-port
- will listen to UDP packets on listen-port and forward them to forward-ip:forward-port
This option can be used to test a protocol; you can easilly modify the sample to forward only some packets, or delay some packets by modifying the methods DropPacket() and GetForwardDelay()

For example, one way to use this sample would be to run: 
start udp_client_server.exe server 3001
sleep 1
start udp_client_server.exe client 127.0.0.1 3002
sleep 1
udp_client_server.exe forward 3002 127.0.0.1 3001

To build: csc udp_client_server.cs /r:system.xml.dll /r:system.dll


UDP_protocol.cs:
---------------
This sample implements a UDP based protocol, and outputs how messages are interpreted. This can be helpful in understanding how any specific UDP protocol would actually run.
udp_protocol.exe receive
- will start receiving; output will be sent to the console
udp_protocol.exe send
- will start sending; output will be sent to the console

E.g. try
>start udp_protocol.exe receive
>start udp_protocol.exe send

You will see output similar to : 

Checking for missing ACKs
Received current ACK on 0 2
Received current ACK on 1 3
Checking if ACK timeout for 1 1000000 0
Received current ACK on 2 4

To build: csc udp_protocol.cs /r:system.xml.dll /r:system.dll


