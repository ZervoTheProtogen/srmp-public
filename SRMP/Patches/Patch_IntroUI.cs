using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(IntroUI))]
    [HarmonyPatch("Awake")]
    class IntroUI_Awake
    {
        static void Postfix(IntroUI __instance)
        {
            if (Globals.IsMultiplayer)
            {
                GameObject.Destroy(__instance.gameObject);
            }
        }
    }
}