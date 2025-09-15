using HarmonyLib;
using SRMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRMultiplayer.EpicSDK;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(PauseMenu))]
    [HarmonyPatch("Quit")]
    class PauseMenu_Quit
    {
        static void Postfix()
        {
            if (!Globals.IsMultiplayer) return;

            //only handle the client if the client is the one disconnecting
            if (Globals.IsClient)
                EpicApplication.Instance.Lobby.LeaveLobby();
            //if server ahndle the shutdown
            if (Globals.IsServer)
                EpicApplication.Instance.Lobby.DestroyLobby();
        }
    }

    [HarmonyPatch(typeof(PauseMenu))]
    [HarmonyPatch("PauseGame")]
    class PauseMenu_PauseGame
    {
        static void Prefix(PauseMenu __instance)
        {
            var hostMenu = __instance.GetComponentInChildren<NetworkHostUI>(true);
            if (hostMenu != null)
            {
                hostMenu.gameObject.SetActive(true);
                hostMenu.SetOnlineStatus(Globals.IsServer);
            }
        }
    }

    [HarmonyPatch(typeof(PauseMenu))]
    [HarmonyPatch("UnPauseGame")]
    class PauseMenu_UnPauseGame
    {
        static void Prefix(PauseMenu __instance)
        {
            var hostMenu = __instance.GetComponentInChildren<NetworkHostUI>(true);
            if (hostMenu != null)
            {
                hostMenu.gameObject.SetActive(false);
            }
        }
    }
}