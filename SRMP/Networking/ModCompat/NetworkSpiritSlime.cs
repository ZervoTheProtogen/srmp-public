using SRMultiplayer.Packets.ModCompat;
using UnityEngine;
using UnityEngine.Serialization;

namespace SRMultiplayer.Networking.ModCompat
{
#if SRML
    public class NetworkSpiritSlime : MonoBehaviour
    {
        [FormerlySerializedAs("DrainOrHealComponent")] public Component HealOrDrainComponent;

        internal void UpdatePacket()
        {
             new PacketSpiritMode(){
                 Mode = (PacketSpiritMode.BehaviorMode)(sbyte)HealOrDrainComponent.GetField("mode")
             }.Send();
        }
    }
#endif
}