using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Lidgren.Network;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    public class NetworkRegion : MonoBehaviour
    {
        public Region Region;
        public RanchCellFastForwarder FastForwarder;

        public int ID;
        public byte Owner;
        public bool IsLocal { get { return Owner == Globals.LocalID; } }
        public List<NetworkPlayer> Players = new List<NetworkPlayer>();
        public Action OnBecameOwner;

        public void AddPlayer(NetworkPlayer player)
        {
            if (Players.Count == 0 && FastForwarder != null)
            {
                SRSingleton<GameContext>.Instance.StartCoroutine(FastForwarder.OnFastForward());
            }

            Players.Add(player);
            player.Regions.Add(this);
            var netActors = Region.members.Data.Select(m => m?.GetComponent<NetworkActor>())
                .Where(a => a != null && !a.KnownPlayers.Contains(player)).ToList();
            foreach (var netActor in netActors)
            {
                netActor.KnownPlayers.Add(player);
            }

            var nicknamesEnabled = Globals.NicknamesModInstalled;
            new PacketActors()
            {
                Actors = netActors.Select(a => new PacketActors.ActorData()
                {
                    ID = a.ID,
                    Ident = a.Ident,
                    RegionSet = a.RegionSet,
                    Owner = a.Owner,
                    Position = a.transform.position,
                    Rotation = a.transform.rotation,
                    AnimalModel = a.Reproduce != null ? a.Reproduce.model : null,
                    PlortModel = a.DestroyPlortAfterTime != null ? a.DestroyPlortAfterTime.plortModel : null,
                    SlimeModel = a.SlimeEat != null ? a.SlimeEat.slimeModel : null,
                    ProduceModel = a.ResourceCycle != null ? a.ResourceCycle.model : null,
#if SRML
                    NicknameModValue = nicknamesEnabled ? (string)a.GetComponent(AccessTools.TypeByName("Nicknames.SlimeNickname")).GetField("Name") : null,
#endif            
                }).ToList()
            }.Send(player);
            //SRMP.Log($"{player} loaded into {name} with {netActors.Count} unknown network actors ({Region.members.GetCount()} actors total)");

        }

        public void RemovePlayer(NetworkPlayer player)
        {
            if (Players.Contains(player))
            {
                Players.Remove(player);
                player.Regions.Remove(this);
                //SRMP.Log($"{player} unloaded {name}");

                if (Players.Count == 0 && FastForwarder != null)
                {
                    FastForwarder.model.hibernationTime = new double?(SRSingleton<SceneContext>.Instance.TimeDirector.WorldTime());
                }
            }
        }

        public void TakeOwnership()
        {
            SetOwnership(Globals.LocalID);

            new PacketRegionOwner()
            {
                ID = ID,
                Owner = Globals.LocalID
            }.Send(NetDeliveryMethod.ReliableOrdered);
            
        }

        public void DropOwnership()
        {
            SetOwnership(0);

            new PacketRegionOwner()
            {
                ID = ID,
                Owner = Owner
            }.Send();
        }

        public void SetOwnership(byte id)
        {
            if (Owner == id)
                return;
            
            Owner = id;
            //SRMP.Log($"Owner for {name} now {(Owner == 0 ? "None" : Globals.Players[Owner].ToString())}");

            if(IsLocal)
            {
                OnBecameOwner?.Invoke();
            }
            if(Owner == 0 && Region.root.activeInHierarchy)
            {
                TakeOwnership();
            }
        }
    }
}
