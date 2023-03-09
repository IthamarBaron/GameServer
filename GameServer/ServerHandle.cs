using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace GameServer
{
    internal class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            // the client sends the packet in the order of : int + String
            // so we need to read them in the same order
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine(Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint + " Connected successfully and is now player: " + _fromClient);
            // making sure client got the currect id
            if (_fromClient != _clientIdCheck)
            {
                // some real bad shit is going on if this is printed
                Console.WriteLine("[ALERT!] Player " + _username + " (ID: " + _fromClient + ") has assumed the wrong client ID (" + _clientIdCheck + ")!" );
            }
            //send player into game
            Server.clients[_fromClient].SendIntoGame(_username);
        }

        public static void PlayerMovement(int _fromClient, Packet _packet)
        {// handeling player movment packets
            bool[] _inputs = new bool[_packet.ReadInt()];
            for (int i = 0; i < _inputs.Length; i++)
            {
                _inputs[i] = _packet.ReadBool();
            }
            Quaternion _rotation = _packet.ReadQuaternion();

            Server.clients[_fromClient].player.SetInput(_inputs, _rotation);
        }
    }
}
