using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace ChatClient
{
    class ChatClient
    {
        public static ChatClient client = new ChatClient();

        bool connected = false;
        bool nameSet = false;

        bool nameRequestPending = false;
        string nameRequest = "";
        string userName = "NO NAME";

        const string connectRequest = "TNDJ9vqWBZGEk6ptD2bkulKAYkL3UHy0rUjIHqeAM2W9XuMm6cbHbSE1NxQXtOH2";
        const string connectConfirm = "CPTerRhQhKtLrG7qTkdoyaspBjNVruVLns0D6L55KIHp0RGyQQmRzXxVBAhOpOcE";
        const string reconnectConfirm = "tc8O0t7WMPxHe0Q7J3n4F3my3CSVlzT5JrYUw4AzxBR3gsItSoQcLwrZtEsWkwXh";
        const string nameSetRequest = "jTlB433KmquqYwsVEWVOkATV045qnqQnsstD44LYL9DRid5DQGBItlHjIECM5QKv";
        const string nameSetConfirm = "DrNxUT33yRo0IUJ0ogbieg9fxMiTLEFWku2IK8B0PfF0RMGopUFR3sZ0GTdNSTcR";
        const string nameInUse = "tvNL4Ar3DZnHbkzPlFmAgFu6ydVHRTTviv6xZmDl0uuWhDmzP4dOLM8CNVEGV2HD";
        const string nameInvalid = "8EwWbpTnwbfvBmF74EBsi5IPrQHW7BP6xL14nxAc47KD9TAaXybvKRNhnuKtGeJs";
        const string disconnectRequest = "wk38u9hJUNS4rwfuuqdYBB0r3mCokEsS0uKU6AUVxZx8WoS4Q8M9EvjoM8lJm2KV";
        const string disconnectConfirm = "D6YpAx06q4z5DF2EazsWHqwaazDeiy6TiIFoYOsMf3BQn1I6u4dLbKx4XMjG9k8U";

        static void Main(string[] args)
        {
            // OUTBOUND TRAFFIC INFRASTRUCTURE
            Console.WriteLine("Please enter server IP.");
            string stringIP = Console.ReadLine();
            IPAddress destnAddr = IPAddress.Parse(stringIP);
            IPEndPoint destnEndPoint = new IPEndPoint(destnAddr, 55555);
            Console.WriteLine("\nSending text data to " + destnEndPoint.ToString() + "\n");

            Socket skt = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            { EnableBroadcast = true };

            // INBOUND TRAFFIC INFRASTRUCTURE
            Console.WriteLine("Please enter the port for your client to operate through.");
            int localPort = int.Parse(Console.ReadLine());
            IPAddress localAddr = IPAddress.Any; //IPAddress.Any is equivalent to IPAddress.Parse("0.0.0.0")
            IPEndPoint localEndPoint = new IPEndPoint(localAddr, localPort);
            skt.Bind(localEndPoint);
            Console.WriteLine("Opening socket at port " + localPort + " for incoming data...\n");

            Thread receiverThread = new Thread(new ParameterizedThreadStart(ReceiverProc));
            receiverThread.Start(skt);

            // CONNECTION REQUEST DATA
            byte[] connectionData = System.Text.Encoding.ASCII.GetBytes(connectRequest);
            skt.SendTo(connectionData, destnEndPoint);
            Console.WriteLine("Sending connection request...");

            while (true)
            {
                if (client.connected)
                {
                    if (client.nameSet)
                    {
                        string text = Console.ReadLine();
                        if (client.connected)
                        {
                            if (text.Substring(0, 1) != "/")
                            {
                                byte[] outboundData = System.Text.Encoding.ASCII.GetBytes(text);
                                skt.SendTo(outboundData, destnEndPoint);
                                int currentLineCursor = Console.CursorTop;
                                Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);
                                Console.Write("<" + client.userName + "> (Me):" + new string(' ', Console.WindowWidth) + "\n");
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.Write("  " + text);
                                Console.WriteLine("\n");
                            }
                            else
                            {
                                switch (text.Substring(1))
                                {
                                    case "disconnect":
                                    case "dc":
                                        byte[] outboundData = System.Text.Encoding.ASCII.GetBytes(disconnectRequest);
                                        skt.SendTo(outboundData, destnEndPoint);
                                        Console.WriteLine("Disconnecting from chat server at " + destnAddr + "." + destnEndPoint.Port + "...");
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                        else
                        {
                            Environment.Exit(0);
                        }
                    }
                    else if (!client.nameRequestPending)
                    {
                        Console.WriteLine("Please enter the display name you wish to use while chatting. Names must:\n - Be at least 3 characters long\n - Contain at least one alphanumeric (A-Z / 0-9) character");
                        client.nameRequest = nameSetRequest + "|" + Console.ReadLine();
                        byte[] outboundData = System.Text.Encoding.ASCII.GetBytes(client.nameRequest);
                        skt.SendTo(outboundData, destnEndPoint);
                        Console.WriteLine("");
                        client.nameRequestPending = true;
                    }
                }
            }
        }

        static void SenderProc()
        {

        }

        static void ReceiverProc(Object obj)
        {
            byte[] incomingData = new byte[1024];
            Socket skt = (Socket)obj;

            EndPoint senderEndpoint = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                int numOfBytes = skt.ReceiveFrom(incomingData, ref senderEndpoint);

                string incomingText = System.Text.Encoding.ASCII.GetString(incomingData, 0, numOfBytes);
                string requestString = "";
                if (incomingText.Length >= 64)
                {
                    requestString = incomingText.Substring(0, 64);
                }

                switch (requestString)
                {
                    case connectConfirm:
                        Console.WriteLine("Connection established!\n");
                        client.connected = true;
                        break;

                    case reconnectConfirm:
                        client.userName = incomingText.Substring(65);
                        Console.WriteLine("Connection re-established! Welcome back, " + client.userName + "!\n\n- - - - - - - - - -\n");
                        client.connected = true;
                        client.nameSet = true;
                        break;

                    case nameSetConfirm:
                        client.userName = client.nameRequest.Substring(65);
                        client.nameRequest = null;
                        Console.WriteLine("Name set! Welcome, " + client.userName + "!\n\n- - - - - - - - - -\n");
                        client.nameRequestPending = false;
                        client.nameSet = true;
                        break;
                        
                    case nameInUse:
                        Console.WriteLine("ERROR: display name already in use!\n");
                        client.nameRequestPending = false;
                        break;
                        
                    case nameInvalid:
                        Console.WriteLine("ERROR: display name does not meet requirements!\n");
                        client.nameRequestPending = false;
                        break;

                    case disconnectConfirm:
                        client.connected = false;
                        Console.WriteLine("Successfully disconnected from server.\nThis program must be restarted to establish a new connection and send more messages.\n");
                        break;

                    default:
                        if (client.connected && client.nameSet)
                        {
                            Console.WriteLine(incomingText + "\n");
                        }
                        break;
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
            throw new Exception("ERROR: No IPv4 address found in available network adapters");
        }
    }
}
