using System;

namespace SRMultiplayer.Packets
{
    /// <summary>
    /// Attribute for setting the Packet ID for modded packets. Please use the CustomPacket class on this. <seealso cref="CustomPacket{T}"/> 
    /// </summary>
    public class CustomPacketAttribute : PacketAttribute
    {
        public CustomPacketAttribute(PacketType type, Type packetClass) : base(type)
        {
            Globals.CustomPackets.Add(type, packetClass);
        }
    }
}