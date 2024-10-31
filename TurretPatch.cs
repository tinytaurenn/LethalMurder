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
    [HarmonyPatch(typeof(Turret))]
    internal class TurretPatch
    {
        [HarmonyPatch("CheckForPlayersInLineOfSight")]
        [HarmonyPostfix]
        static PlayerControllerB CheckLineSightPatch(PlayerControllerB playerOutput)
        {
            if (playerOutput == null)
            {
                return null;
            }
            else
            {
                if (Plugin.Instance.modManager == null) return playerOutput;
                if (Plugin.Instance.modManager.playerList[(int)playerOutput.GetClientId()].role == ModManager.ERoles.Impostor) return null;
                return playerOutput; 
                
                
            }

            
        }
    }
}
