using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldTime)]
    public class PacketWorldTime : Packet
    {
        public double Time;

        public PacketWorldTime() { }
        public PacketWorldTime(NetIncomingMessage im) { Deserialize(im); }
    }
}
