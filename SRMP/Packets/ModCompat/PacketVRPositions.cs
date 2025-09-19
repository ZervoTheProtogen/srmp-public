using Lidgren.Network;
using UnityEngine;

namespace SRMultiplayer.Packets.ModCompat
{
    [Packet(PacketType.VRPositions)]
    public class PacketVRPositions : Packet
    {
        public Vector3 leftPosition;
        public Quaternion leftRotation;
        
        public Vector3 rightPosition;
        public Quaternion rightRotation;
        
        public float headAngle;
        
        //public Vector3 headOffset;

        public byte ID;
        
        public PacketVRPositions(NetIncomingMessage im) { Deserialize(im); }
        public PacketVRPositions() { }
    }
}