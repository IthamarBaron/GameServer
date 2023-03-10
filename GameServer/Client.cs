using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Net.Http.Headers;

namespace GameServer
{
    internal class Client
    {
        public static int dataBufferSize = 4096; //4MB

        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;

        public Client(int _clientId)
        {
            id = _clientId;
            tcp = new TCP(id);
            udp = new UDP(id);
        }

        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receiveData;
            private byte[] receiveBuffer;


            public TCP(int _id)
            {
                id = _id;
            }

            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();
                receiveData = new Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                // sending welcome packet
                ServerSend.Welcome(id, "Hello there, Susy Buka!");
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    if (_packet != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine("Error sending data to player [ " + id + " ] in TCP: " + _ex);
                }
            }

            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        //disconect
                        Server.clients[id].Disconnect();
                        return;
                    }
                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    //handle Data
                    receiveData.Reset(HandleData(_data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                }
                catch (Exception _ex)
                {
                    Console.WriteLine("Error receiving TCP data: [ "+_ex+" ].");
                    //disconnect
                    Server.clients[id].Disconnect();
                }
            }

            private bool HandleData(byte[] _data)
            {
                int _packetLength = 0;

                receiveData.SetBytes(_data);

                /*cheking if receivedData contains more then 4 unread bytes,
                 if it does that means we have the start of the packet because the first 4 bytes (int) 
                 of our packets is an int representing the length of the packet*/
                if (receiveData.UnreadLength() >= 4)
                {
                    _packetLength = receiveData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true; // we want to reset receiveData
                    }
                }

                // as long as this loop runs we have a complete packet which we can handle
                while (_packetLength > 0 && _packetLength <= receiveData.UnreadLength())
                {
                    byte[] _packetBytes = receiveData.ReadBytes(_packetLength);

                    //bruh i have no idea
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int _packetId = _packet.ReadInt();
                            //taking the appropriate packet handler using our packet id
                            Server.packetHandlers[_packetId](id,_packet);
                        }
                    });

                    //restarting packet
                    _packetLength = 0;
                    //same <if> from before
                    if (receiveData.UnreadLength() >= 4)
                    {
                        _packetLength = receiveData.ReadInt();
                        if (_packetLength <= 0)
                        {
                            return true; // we want to reset receiveData
                        }
                    }
                }
                if (_packetLength <= 1)
                {
                    //we want to reset receiveData
                    return true;
                }
                //if its bigger we still have parts of the packet
                return false;
            }

            public void Disconnect()
            {
                socket.Close();
                stream = null;
                receiveData = null;
                receiveBuffer = null;
                socket = null;
            }
        }

        public class UDP
        {
            public IPEndPoint endPoint;

            private int id;

            public UDP(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
            }

            public void SendData(Packet _packet)
            {
                Server.SendUDPData(endPoint, _packet);
            }

            public void HandleData(Packet _packetData)
            {
                int _packetLength = _packetData.ReadInt();
                byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });
            }

            public void Disconnect()
            {
                endPoint = null;
            }
        }

        public void SendIntoGame(string _playerName)
        {
            player = new Player(id, _playerName, new Vector3(0, 0, 0));

            //to send info from all other players to new player
            foreach (Client _client in Server.clients.Values)
            {
                if (_client.player != null)
                {
                    if (_client.id != id)
                    {
                        ServerSend.SpawnPlayer(id, _client.player);
                    }
                }
            }
            // sending from new player to all other players as well as himself
            foreach (Client _client in Server.clients.Values)
            {
                if (_client.player != null)
                {
                    ServerSend.SpawnPlayer(_client.id,player);
                }
            }
        }

        private void Disconnect()
        {
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

            player = null;

            tcp.Disconnect();
            udp.Disconnect();
        }
    }
}
