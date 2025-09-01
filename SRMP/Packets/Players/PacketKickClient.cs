using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.KickPlayer)]
    public class PacketKickClient : Packet
    {
        public enum Reason : byte
        {
            VersionMismatch,
            Kicked,
            DLCMismatch,
            ModsMismatch,
            Custom,
        }

        public object data;
        public Reason reason;
        
        public PacketKickClient() { }
        public PacketKickClient(NetIncomingMessage im) { Deserialize(im); }

        public override void Deserialize(NetIncomingMessage im)
        {
            reason = (Reason)im.ReadByte();

            switch (reason)
            {
                case Reason.VersionMismatch:
                    data = im.ReadInt32();
                    break;
                case Reason.Custom:
                case Reason.DLCMismatch:
                case Reason.ModsMismatch:
                    data = im.ReadString();
                    break;
                case Reason.Kicked:
                    break;
            }
            
            SRMP.Log($"Kick reason: {reason} - Data: {data}");
        }

        public override void Serialize(NetOutgoingMessage om)
        {
            om.Write((ushort)GetPacketType());
            
            om.Write((byte)reason);

            switch (reason)
            {
                case Reason.VersionMismatch:
                    om.Write((int)data);
                    break;
                case Reason.Custom:
                case Reason.DLCMismatch:
                case Reason.ModsMismatch:
                    om.Write((string)data);
                    break;
                case Reason.Kicked:
                    break;
            }
        }
    }
}
