using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

namespace ChatServer
{
    class ChatServer
    {
        public static ChatServer server = new ChatServer();

        Dictionary<IPEndPoint, string> connectedClients = new Dictionary<IPEndPoint, string>();

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
            string serverAddr = GetLocalIPAddress();
            Console.WriteLine("Server IP address: " + serverAddr + "\nInitialising chat server...\n\n- - - - - - - - - -\n");

            // List<IPEndPoint> connectedClients = new List<IPEndPoint>();

            IPAddress addr = IPAddress.Any;
            IPEndPoint endPoint = new IPEndPoint(addr, 55555);
            Socket skt = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            skt.Bind(endPoint);

            byte[] incomingData = new byte[1024];
            EndPoint senderEndpnt = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                int numOfBytes = skt.ReceiveFrom(incomingData, ref senderEndpnt);
                string incomingText = System.Text.Encoding.ASCII.GetString(incomingData, 0, numOfBytes);
                string requestString = "";
                if (incomingText.Length >= 64)
                {
                    requestString = incomingText.Substring(0, 64);
                }

                IPEndPoint senderIPEndpnt = (IPEndPoint)senderEndpnt;
                switch (requestString)
                {
                    case connectRequest:
                        if (server.connectedClients.ContainsKey(senderIPEndpnt))
                        {
                            string clientName = "";
                            server.connectedClients.TryGetValue(senderIPEndpnt, out clientName);
                            Console.WriteLine("Re-establishing connection with client:\n   IPv4 address: " + senderIPEndpnt.Address + "\n   Port: " + senderIPEndpnt.Port + "\n   Name: " + clientName + "\n");
                            byte[] confirmData = System.Text.Encoding.ASCII.GetBytes(reconnectConfirm + "_" + clientName);
                            skt.SendTo(confirmData, (IPEndPoint)senderEndpnt);
                        }
                        else
                        {
                            server.connectedClients.Add(senderIPEndpnt, "NO NAME");
                            Console.WriteLine("Establishing connection with new client:\n   IPv4 address: " + senderIPEndpnt.Address + "\n   Port: " + senderIPEndpnt.Port + "\n");
                            byte[] confirmData = System.Text.Encoding.ASCII.GetBytes(connectConfirm);
                            skt.SendTo(confirmData, senderIPEndpnt);
                        }
                        break;

                    case nameSetRequest:
                        string name = incomingText.Substring(65);
                        Console.WriteLine("Client at " + senderIPEndpnt.Address + "." + senderIPEndpnt.Port + " requesting display name set: " + name);
                        if (CheckNameValid(name))
                        {
                            bool nameTaken = false;
                            foreach (KeyValuePair<IPEndPoint, string> kvp in server.connectedClients)
                            {
                                if (kvp.Value == name)
                                {
                                    nameTaken = true;
                                    break;
                                }
                            }

                            if (nameTaken)
                            {
                                byte[] confirmData = System.Text.Encoding.ASCII.GetBytes(nameInUse);
                                skt.SendTo(confirmData, senderIPEndpnt);
                                Console.WriteLine("ERROR: name already in use\nError returned to client\n");
                            }
                            else
                            {
                                byte[] confirmData = System.Text.Encoding.ASCII.GetBytes(nameSetConfirm);
                                skt.SendTo(confirmData, senderIPEndpnt);
                                server.connectedClients[senderIPEndpnt] = name;
                                Console.WriteLine("Request successful, setting display name of client\n");
                            }
                        }
                        else
                        {
                            byte[] confirmData = System.Text.Encoding.ASCII.GetBytes(nameInvalid);
                            skt.SendTo(confirmData, senderIPEndpnt);
                            Console.WriteLine("ERROR: name format invalid\nError returned to client\n");
                        }
                        break;

                    case disconnectRequest:
                        if (server.connectedClients.ContainsKey(senderIPEndpnt))
                        {
                            string clientName = "";
                            server.connectedClients.TryGetValue(senderIPEndpnt, out clientName);
                            Console.WriteLine("Client requested disconnect:\n   IPv4 address: " + senderIPEndpnt.Address + "\n   Port: " + senderIPEndpnt.Port + "\n   Name: " + clientName + "\nTermianting connection...\n");
                            byte[] confirmData = System.Text.Encoding.ASCII.GetBytes(disconnectConfirm);
                            skt.SendTo(confirmData, (IPEndPoint)senderEndpnt);
                            server.connectedClients.Remove(senderIPEndpnt);
                        }
                        break;

                    default:
                        string senderName = "";
                        server.connectedClients.TryGetValue(senderIPEndpnt, out senderName);
                        server.RelayMessage(skt, senderIPEndpnt.Address.ToString(), senderIPEndpnt.Port, senderName, incomingText);
                        break;
                }
            }
        }

        public static string GetLocalIPAddress()
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

        public void RelayMessage(Socket relaySocket, string senderIP, int senderPort, string senderName, string incomingText)
        {
            string outgoingText = "<" + senderName + ">:\n  " + incomingText;
            byte[] outgoingData = System.Text.Encoding.ASCII.GetBytes(outgoingText);

            Console.WriteLine("Message from client \"" + senderName + "\" (" + senderIP + "." + senderPort + "):\n " + incomingText + "\n");

            foreach (KeyValuePair<IPEndPoint, string> client in connectedClients)
            {
                if (client.Key.Address.ToString() != senderIP || client.Key.Port != senderPort)
                {
                    relaySocket.SendTo(outgoingData, client.Key);
                }
            }
        }

        public static bool CheckNameValid(string name)
        {
            bool longEnough = (name.Length >= 3);
            bool containsAlphNum = false;
            char[] alphaNumeric = new char[] {
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
            };

            for (int i = 0; i < name.Length; i++)
            {
                char targetChar = char.Parse(name.Substring(i, 1));
                foreach (char character in alphaNumeric)
                {
                    if (targetChar == character)
                    {
                        containsAlphNum = true;
                        break;
                    }
                }
            }

            return (longEnough && containsAlphNum);
        }
    }
}
