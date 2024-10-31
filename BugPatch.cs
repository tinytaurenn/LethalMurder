using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalMurder
{
    [HarmonyPatch(typeof(HoarderBugAI))]
    internal class BugPatch
    {

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void UpdatePatch(ref PlayerControllerB ___angryAtPlayer)
        {
            if (Plugin.Instance.modManager == null) return;
            if (___angryAtPlayer == null) return;
            if(Plugin.Instance.modManager.playerList.Count >= (int)___angryAtPlayer.GetClientId())
            if (Plugin.Instance.modManager.playerList[(int)___angryAtPlayer.GetClientId()].role == ModManager.ERoles.Impostor)
            {
                ___angryAtPlayer = null;
            }
            
        }
    }
}
