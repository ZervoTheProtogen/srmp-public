using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SRMultiplayer.Networking;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    /// <summary>
    /// Abstract modded packet type. You should handle all code using this.
    /// Make sure you register the PacketType with SRML like you would with an Identifiable.Id.
    /// Example: <code>
    /// // An example of an [EnumHolder] for the PacketType enum.
    /// [CustomPacket(CustomPacketTypes.Example)]
    /// public class PacketExample : CustomPacket&lt;PacketExample&gt;
    /// {
    ///     public PacketExample() { }
    ///     
    ///     public PacketExample(NetIncomingMessage im) { Deserialize(im); }
    ///
    ///     // Client => Server
    ///     public override void HandleServer(PacketExample packet, NetworkPlayer player)
    ///     {
    ///         Debug.Log($"Player {player.ID} sent an example packet! Forwarding to other players.");
    ///         
    ///        // Also works using SendToAll(NetDeliveryMethod.ReliableOrdered).
    ///         packet.SendToAllExcept(player, NetDeliveryMethod.ReliableOrdered);
    ///     }
    ///
    ///     // Server => Client
    ///     public override void HandleClient(PacketExample packet)
    ///     {
    ///        Debug.Log("Server sent an example packet!");
    ///     }
    /// }
    ///</code>
    /// <seealso cref="CustomPacketAttribute"/>
    /// <seealso cref="Extensions.SendToAll(Packet,NetDeliveryMethod,int)"/>
    /// <seealso cref="SRML.Utils.Enum.EnumHolderAttribute"/>
    /// </summary>
    /// <typeparam name="T">Self Type</typeparam>
    public abstract class CustomPacket<T> : Packet where T : CustomPacket<T>
    {
        /// <summary>
        /// Parameterless constructor to be used through inheritance
        /// </summary>
        public CustomPacket()
        {
        }

        /// <summary>
        /// Incoming message based construtor that deserielizes the item for the message
        /// </summary>
        public CustomPacket(NetIncomingMessage im)
        {
            Deserialize(im);
        }

        public virtual void HandleServer(T packet, NetworkPlayer player)
        {
        }

        public virtual void HandleClient(T packet)
        {
        }
    }
}
