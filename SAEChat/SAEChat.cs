using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SAEChat
{
    class SAEChat
    {
        static void ReceiverProc(Object obj)
        {
            byte[] data = new byte[1024];
            Socket sock = (Socket)obj;

            string localIP = "";
            try
            {
                localIP = GetLocalIPAddress();
            }
            catch
            {
                //ALTERNAVIE COURSE OF ACTION, IF CODE IN "TRY" FAILS
            }

            EndPoint senderEndpoint = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                int numOfBytes = sock.ReceiveFrom(data, ref senderEndpoint);

                if (localIP != ((IPEndPoint)senderEndpoint).Address.ToString())
                {
                    string text = System.Text.Encoding.ASCII.GetString(data, 0, numOfBytes);
                    Console.WriteLine(text);
                }
            }
        }

        static string GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress addr in host.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    return addr.ToString();
                }
            }
            throw new Exception("ERROR: No network adapters with an IPv4 address in the system!");
        }

        static void Main(string[] args)
        {

            //OUTBOUND TRAFFIC INFRASTRUCTURE
            IPAddress destinationAddr = IPAddress.Broadcast; //IPAddress.Broadcast is equiavalent to IPAddress.Parse("255.255.255.255")
            IPEndPoint destinationEndPoint = new IPEndPoint(destinationAddr, 55555);

            Console.WriteLine("Sending text data to " + destinationEndPoint.ToString());

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.EnableBroadcast = true;
            //*******************************

            //INBOUND TRAFFIC INFRASTRUCTURE
            IPAddress localAddr = IPAddress.Any;//IPAddress.Any is equivalent to IPAddress.Parse("0.0.0.0")
            IPEndPoint localEndPoint = new IPEndPoint(localAddr, 55555); //five fives :D

            sock.Bind(localEndPoint);
            Console.WriteLine("Opening socket, echoing from " + localAddr.ToString() + ":" + localEndPoint.Port);
            //*******************************

            Thread receiverThread = new Thread(new ParameterizedThreadStart(ReceiverProc));
            receiverThread.Start(sock);

            while (true)
            {
                string text = Console.ReadLine();
                byte[] outboundData = System.Text.Encoding.ASCII.GetBytes(text);
                sock.SendTo(outboundData, destinationEndPoint);
            }
        }
    }
}
