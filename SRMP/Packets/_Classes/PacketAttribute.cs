using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Packets
{
    public class PacketAttribute : Attribute
    {
        public PacketType Type;

        public PacketAttribute(PacketType type)
        {
            Type = type;
        }

    }
}
