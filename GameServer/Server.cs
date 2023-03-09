using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GameServer
{
    internal class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int,Client> clients = new Dictionary<int,Client>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);//[delegate = a type that represents references to methods with a particular parameter list and return 
        public static Dictionary<int, PacketHandler> packetHandlers;



        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public static void Start(int _maxPlayers, int _port)
        {
            MaxPlayers = _maxPlayers;
            Port = _port;

            Console.WriteLine("Server starting...");
            InitializeServerData();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine("Server Started on port: "+ Port);
        }

        public static void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
            // once a client connects we want to continue listening for connections
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine("Incoming connection from "+ _client.Client.RemoteEndPoint+"...");

            for (int i = 1; i <= MaxPlayers; i++)
            {
                // cheking if slot is empty
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                    return;
                }
            }
            //if the loop executes to complition the server is full!
            Console.WriteLine(_client.Client.RemoteEndPoint + " Faild to connect: Server full!");
        }

        public static void UDPReceiveCallback(IAsyncResult _result)
        {//this method will set our IP endpoint to the endpoint where the data came from
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (_data.Length < 4)
                {
                    return;
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();

                    if (_clientId == 0)// we dont want that...
                    {
                        return;
                    }

                    if (clients[_clientId].udp.endPoint == null)// this means its a new connection (welcome packet)
                    {
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())// we dont want users sending a false client id
                    {
                        //handling data
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine("Error receiving UDP data: " + _ex);
            }
        }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine("Error sending UDP data to [ " + _clientEndPoint + " ] in UDP: " + _ex);
            }
        }

        public static void InitializeServerData()
        {
            //using a aloop to fill the client dict
            for (int i = 1; i <= MaxPlayers; i++)
            {
                // giving each client an ID starting from 1
                clients.Add(i,new Client(i));
            }
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int)ClientPackets.welcomeReceived,ServerHandle.WelcomeReceived},
                {(int)ClientPackets.playerMovement,ServerHandle.PlayerMovement},
            };
            Console.WriteLine("Initialized packets");
        }
    }
}
