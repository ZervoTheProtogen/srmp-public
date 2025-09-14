using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.Gordos)]
    public class PacketGordos : Packet
    {
        public struct GordoData
        {
            public string ID;
            public GordoModel Model;
            public string NicknameModValue;
        }
        public List<GordoData> Gordos { get; set; }

        public PacketGordos() { }
        public PacketGordos(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Gordos.Count);
            foreach(var gordo in Gordos)
            {
                om.Write(gordo.ID);
                om.Write(gordo.Model.gordoEatenCount);
                om.Write(gordo.Model.fashions.Count);
                foreach(var fashion in gordo.Model.fashions)
                {
                    om.Write((ushort)fashion);
                }

                om.Write(gordo.NicknameModValue != null);
                if (gordo.NicknameModValue != null)
                {
                    om.Write(gordo.NicknameModValue);
                }
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Gordos = new List<GordoData>();
            int gordoCount = im.ReadInt32();
            for(int i = 0; i < gordoCount; i++)
            {
                var gordoData = new GordoData()
                {
                    ID = im.ReadString(),
                    Model = new GordoModel()
                    {
                        gordoEatenCount = im.ReadInt32()
                    }
                };
                int fashionCount = im.ReadInt32();
                for(int j = 0; j < fashionCount; j++)
                {
                    gordoData.Model.fashions.Add((Identifiable.Id)im.ReadUInt16());
                }

                if (im.ReadBoolean())
                {
                    gordoData.NicknameModValue = im.ReadString();
                }
                Gordos.Add(gordoData);
            }
        }
    }
}
