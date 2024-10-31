using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.InputSystem.InputRemoting;

namespace LethalMurder
{
    [HarmonyPatch(typeof(StartMatchLever))]
    internal class MatchLeverPatch
    {

        [HarmonyPatch("LeverAnimation")]
        [HarmonyPrefix]
        static bool blockingLevelShipPatch()
        {

            if (LC_API.GameInterfaceAPI.GameState.ShipState == LC_API.Data.ShipState.OnMoon)
            {
                
                if(Plugin.Instance.modManager.canShipLever == false) LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.QueueTip("YOU CANT RUN THE SHIP", "The IMPOSTOR is still alive", 5,0,true);
                return Plugin.Instance.modManager.canShipLever;
            }


            return true; 
        }


    }
}
