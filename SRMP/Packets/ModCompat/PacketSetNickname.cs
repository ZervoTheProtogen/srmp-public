using Lidgren.Network;

namespace SRMultiplayer.Packets.ModCompat
{
    [Packet(PacketType.SetNickname)]
    public class PacketSetNickname : Packet
    {
        public string nickname;
        public bool type;
        public int actorId;
        public string gordoId;
        
        public PacketSetNickname() {}
        public PacketSetNickname(NetIncomingMessage im) { Deserialize(im); }
    }
}