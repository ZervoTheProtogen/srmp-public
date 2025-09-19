using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SRMultiplayer.Packets.PacketAccessDoors;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerAnimation)]
    public class PacketPlayerAnimation : Packet
    {
        public enum AnimationType : int
        {
            Layer,
            Speed,
            Parameters

        }

        public byte ID;
        public byte Type;
        public struct AnimationData
        {
            public byte type
            {
                get
                {
                    if (fData.HasValue) return 0;
                    if (bData.HasValue) return 1;
                    if (iData.HasValue) return 2;
                    if (uData.HasValue) return 3;
                    return 8;
                }
            }
            public float? fData;
            public bool? bData;
            public int? iData;
            public ulong? uData;
        }

        public Queue<AnimationData> internalData { get; set; } = new Queue<AnimationData>();

        public void Add<T>(T obj)
        {
            switch (obj)
            {
                case float itm:
                    internalData.Enqueue(new AnimationData() { fData = itm });
                    break;
                case bool itm:
                    internalData.Enqueue(new AnimationData() { bData = itm });
                    break;
                case int itm:
                    internalData.Enqueue(new AnimationData() { iData = itm });
                    break;
                case ulong itm:
                    internalData.Enqueue(new AnimationData() { uData = itm });
                    break;
            }
        }

        /// <summary>
        /// mark construction inheritance incase we need it
        /// </summary>
        public PacketPlayerAnimation() : base() { }
        /// <summary>
        /// mark construction inheritance so the deserialization automatically happens for is
        /// since the base decalres this
        /// </summary>
        public PacketPlayerAnimation(NetIncomingMessage im) : base(im) { }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(internalData.Count);
            foreach (var data in internalData)
            {
                om.Write(data.type);
                switch (data.type)
                {
                    case 0:
                        om.Write(data.fData.Value);
                        break;
                    case 1:
                        om.Write(data.bData.Value);
                        break;
                    case 2:
                        om.Write(data.iData.Value);
                        break;
                    case 3:
                        om.Write(data.uData.Value);
                        break;
                }
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            internalData = new Queue<AnimationData>();
            int Count = im.ReadInt32();
            for (int i = 0; i < Count; i++)
            {
                var data = new AnimationData();
                byte type = im.ReadByte();
                switch (type)
                {
                    case 0:
                        data.fData = im.ReadFloat();
                        break;
                    case 1:
                        data.bData = im.ReadBoolean();
                        break;
                    case 2:
                        data.iData = im.ReadInt32();
                        break;
                    case 3:
                        data.uData = im.ReadUInt64();
                        break;
                }

                internalData.Enqueue(data);
            }
        }
    }

}
