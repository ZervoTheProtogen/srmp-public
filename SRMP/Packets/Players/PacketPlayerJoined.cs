using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerJoined)]
    public class PacketPlayerJoined : Packet
    {
        public byte ID;
        public string Username;
        public bool VR;

        public PacketPlayerJoined() { }
        public PacketPlayerJoined(NetIncomingMessage im) { Deserialize(im); }
    }
}
