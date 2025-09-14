using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.Actors)]
    public class PacketActors : Packet
    {
        public struct ActorData
        {
            public int ID;
            public byte Owner;
            public ushort Ident;
            public Vector3 Position;
            public Quaternion Rotation;
            public byte RegionSet;

            public SlimeModel SlimeModel;
            public ProduceModel ProduceModel;
            public AnimalModel AnimalModel;
            public PlortModel PlortModel;

            public string NicknameModValue;
        }
        public List<ActorData> Actors { get; set; }

        public PacketActors() { }
        public PacketActors(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Actors.Count);
            foreach(var actorData in Actors)
            {
                om.Write(actorData.ID);
                om.Write(actorData.Owner);
                om.Write(actorData.Ident);
                om.Write(actorData.Position);
                om.Write(actorData.Rotation);
                om.Write(actorData.RegionSet);

                om.Write(actorData.SlimeModel != null);
                if (actorData.SlimeModel != null)
                {
                    om.Write(actorData.SlimeModel.emotionAgitation.currVal);
                    om.Write(actorData.SlimeModel.emotionFear.currVal);
                    om.Write(actorData.SlimeModel.emotionHunger.currVal);
                    om.Write(actorData.SlimeModel.isFeral);
                    om.Write(actorData.SlimeModel.isGlitch);
                    om.Write(actorData.SlimeModel.fashions.Count);
                    foreach (var fashion in actorData.SlimeModel.fashions)
                    {
                        om.Write((ushort)fashion);
                    }
                }

                om.Write(actorData.ProduceModel != null);
                if (actorData.ProduceModel != null)
                {
                    om.Write((byte)actorData.ProduceModel.state);
                    om.Write(actorData.ProduceModel.progressTime);
                }

                om.Write(actorData.AnimalModel != null);
                if (actorData.AnimalModel != null)
                {
                    om.Write(actorData.AnimalModel.nextReproduceTime);
                    om.Write(actorData.AnimalModel.transformTime);
                    om.Write(actorData.AnimalModel.fashions.Count);
                    foreach(var fashion in actorData.AnimalModel.fashions)
                    {
                        om.Write((ushort)fashion);
                    }
                }

                om.Write(actorData.PlortModel != null);
                if (actorData.PlortModel != null)
                {
                    om.Write(actorData.PlortModel.destroyTime);
                }
                
                om.Write(actorData.NicknameModValue != null);
                if (actorData.NicknameModValue != null)
                    om.Write(actorData.NicknameModValue);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Actors = new List<ActorData>();
            int actorCount = im.ReadInt32();
            for(int i = 0; i < actorCount; i++)
            {
                var actorData = new ActorData()
                {
                    ID = im.ReadInt32(),
                    Owner = im.ReadByte(),
                    Ident = im.ReadUInt16(),
                    Position = im.ReadVector3(),
                    Rotation = im.ReadQuaternion(),
                    RegionSet = im.ReadByte()
                };

                if(im.ReadBoolean())
                {
                    actorData.SlimeModel = new SlimeModel(0, Identifiable.Id.NONE, MonomiPark.SlimeRancher.Regions.RegionRegistry.RegionSetId.HOME, null);
                    actorData.SlimeModel.emotionAgitation = new SlimeEmotions.EmotionState(SlimeEmotions.Emotion.AGITATION, im.ReadFloat(), 0, 0, 0);
                    actorData.SlimeModel.emotionFear = new SlimeEmotions.EmotionState(SlimeEmotions.Emotion.AGITATION, im.ReadFloat(), 0, 0, 0);
                    actorData.SlimeModel.emotionHunger = new SlimeEmotions.EmotionState(SlimeEmotions.Emotion.AGITATION, im.ReadFloat(), 0, 0, 0);
                    actorData.SlimeModel.isFeral = im.ReadBoolean();
                    actorData.SlimeModel.isGlitch = im.ReadBoolean();
                    actorData.SlimeModel.fashions = new List<Identifiable.Id>();
                    int fashionCount = im.ReadInt32();
                    for(int j = 0; j < fashionCount; j++)
                    {
                        actorData.SlimeModel.fashions.Add((Identifiable.Id)im.ReadUInt16());
                    }
                }
                if (im.ReadBoolean())
                {
                    actorData.ProduceModel = new ProduceModel(0, Identifiable.Id.NONE, MonomiPark.SlimeRancher.Regions.RegionRegistry.RegionSetId.HOME, null);
                    actorData.ProduceModel.state = (ResourceCycle.State)im.ReadByte();
                    actorData.ProduceModel.progressTime = im.ReadDouble();
                }
                if (im.ReadBoolean())
                {
                    actorData.AnimalModel = new AnimalModel(0, Identifiable.Id.NONE, MonomiPark.SlimeRancher.Regions.RegionRegistry.RegionSetId.HOME, null);
                    actorData.AnimalModel.nextReproduceTime = im.ReadDouble();
                    actorData.AnimalModel.transformTime = im.ReadDouble();
                    actorData.AnimalModel.fashions = new List<Identifiable.Id>();
                    int fashionCount = im.ReadInt32();
                    for (int j = 0; j < fashionCount; j++)
                    {
                        actorData.AnimalModel.fashions.Add((Identifiable.Id)im.ReadUInt16());
                    }
                }
                if (im.ReadBoolean())
                {
                    actorData.PlortModel = new PlortModel(0, Identifiable.Id.NONE, MonomiPark.SlimeRancher.Regions.RegionRegistry.RegionSetId.HOME, new GameObject());
                    actorData.PlortModel.destroyTime = im.ReadDouble();
                }
                
                if (im.ReadBoolean())
                {
                    actorData.NicknameModValue = im.ReadString();
                }
                
                Actors.Add(actorData);
            }
        }
    }
}
