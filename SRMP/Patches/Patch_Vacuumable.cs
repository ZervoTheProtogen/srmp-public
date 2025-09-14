using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(Vacuumable))]
    [HarmonyPatch("capture")]
    class Vacuumable_capture
    {
        static void Prefix(Vacuumable __instance)
        {
            if (!Globals.IsMultiplayer) return;
            
            var netActor = __instance.GetComponent<NetworkActor>();
            SRMP.Log($"Vacuumable.capture - NetActor is null:{netActor == null}, NetActor is local:{netActor.IsLocal}");
            if (netActor != null && !netActor.IsLocal)
            {
                netActor.TakeOwnership();
            }
        }
    }

    [HarmonyPatch(typeof(Vacuumable))]
    [HarmonyPatch("TryConsume")]
    class Vacuumable_TryConsume
    {
        static bool Prefix(Vacuumable __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netActor = __instance.GetComponent<NetworkActor>();
            return (netActor != null && netActor.IsLocal);
        }
    }
}