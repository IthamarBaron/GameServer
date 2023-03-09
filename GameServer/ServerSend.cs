using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    internal class ServerSend
    {

        private static void SendTCPData(int _toClient, Packet _packet)
        {// this method is in charge of preparing the packet to be sent 

            _packet.WriteLength();//take the length of the byte list and puts it in the start of the packet
            Server.clients[_toClient].tcp.SendData(_packet);
        }

        public static void SendUDPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();//take the length of the byte list and puts it in the start of the packet
            Server.clients[_toClient].udp.SendData(_packet);
        }

        // tcp to all
        private static void SendTCPDataToAll(Packet _packet)
        {// sending data to ALL clients

            _packet.WriteLength();//take the length of the byte list and puts it in the start of the packet
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }

        private static void SendTCPDataToAll(int _exeptClient, Packet _packet)
        {// sending data to ALL clients *EXEPT THE _exeptClient*

            _packet.WriteLength();//take the length of the byte list and puts it in the start of the packet
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                if (i != _exeptClient)
                {
                    Server.clients[i].tcp.SendData(_packet);
                }
            }
        }

        // udp to all
        private static void SendUDPDataToAll(Packet _packet)
        {// sending data to ALL clients

            _packet.WriteLength();//take the length of the byte list and puts it in the start of the packet
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }

        private static void SendUDPDataToAll(int _exeptClient, Packet _packet)
        {// sending data to ALL clients *EXEPT THE _exeptClient*

            _packet.WriteLength();//take the length of the byte list and puts it in the start of the packet
            for (int i = 1; i < Server.MaxPlayers; i++)
            {
                if (i != _exeptClient)
                {
                    Server.clients[i].udp.SendData(_packet);
                }
            }
        }

        #region Packets
        public static void Welcome(int _toClient, string _msg)
        {
            //since our packet class inherits from IDisposable 
            //we need to make sure to dispose it when we are done with it
            using (Packet _packet = new Packet((int)ServerPackets.welcome))//this will auutomaticlly dispose it for us
            {
                _packet.Write(_msg);
                _packet.Write(_toClient);

                SendTCPData(_toClient, _packet);
            }
        }
        public static void SpawnPlayer(int _toClient, Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.username);
                _packet.Write(_player.position);
                _packet.Write(_player.rotation);

                /*sending in tcp cus this is an importent message 
                  that we are only sending once per player so we 
                  cant afford to lose packets*/

                SendTCPData(_toClient, _packet);
            }
        }

        public static void PlayerPosition(Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.position);

                SendUDPDataToAll(_packet);
            }
        }

        public static void PlayerRotation(Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.rotation);

                SendUDPDataToAll(_player.id, _packet);
            }
        }
        #endregion



    }
}
