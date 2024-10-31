using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;

namespace LethalMurder
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch(HUDManager __instance)
        {
            if ((UnityEngine.Object)GameNetworkManager.Instance == (UnityEngine.Object)null || (UnityEngine.Object)GameNetworkManager.Instance.localPlayerController == (UnityEngine.Object)null || (UnityEngine.Object)GameNetworkManager.Instance.localPlayerController == (UnityEngine.Object)null)
                return;

            if (GameNetworkManager.Instance.localPlayerController.isPlayerDead) return;

            if (Plugin.Instance.modManager == null) return; 
            
            if(LC_API.GameInterfaceAPI.GameState.ShipState == LC_API.Data.ShipState.InOrbit)
            {
                __instance.weightCounter.text = "Otarie <3"; 
            }
            else
            {
                __instance.weightCounter.text = Plugin.Instance.modManager.m_Role.ToString();
            }

            
            
        }
    }
}
