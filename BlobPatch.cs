using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalMurder
{
    [HarmonyPatch(typeof(BlobAI))]
    internal class BlobPatch
    {

        [HarmonyPatch("OnCollideWithPlayer")]
        [HarmonyPostfix]
        static void OnCollideWithPlayerPatch(Collider other, BlobAI __instance)
        {
            PlayerControllerB playerControllerB = __instance.MeetsStandardPlayerCollisionConditions(other);
            if ((UnityEngine.Object)playerControllerB == (UnityEngine.Object)null) return;

            if (!playerControllerB.isPlayerDead) Plugin.Instance.modManager.TeleportPlayerAtRandomLocation(playerControllerB);
                


        }
    }
}
