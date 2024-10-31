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
    [HarmonyPatch(typeof(HauntedMaskItem))]
    internal class HauntedMaskPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(HauntedMaskItem __instance,ref bool ___attaching, ref PlayerControllerB ___previousPlayerHeldBy,ref bool ___maskOn,ref bool ___holdingLastFrame,ref bool ___finishedAttaching,ref float ___lastIntervalCheck)
        {

            if (!__instance.maskIsHaunted || !__instance.IsOwner || (UnityEngine.Object)___previousPlayerHeldBy == (UnityEngine.Object)null || !___maskOn || !___holdingLastFrame || ___finishedAttaching)
                return;

            if (!___attaching)
            {

                if (StartOfRound.Instance.shipIsLeaving || StartOfRound.Instance.inShipPhase && (UnityEngine.Object)StartOfRound.Instance.testRoom == (UnityEngine.Object)null || (double)Time.realtimeSinceStartup <= (double)___lastIntervalCheck)
                    return;

                __instance.BeginAttachment(); 

            }   

        }

    }
}
