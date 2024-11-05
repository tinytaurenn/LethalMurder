using HarmonyLib;
using LethalNetworkAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace LethalMurder
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class ModSync : MonoBehaviour
    {
        

       
     
        [HarmonyPatch("Singleton_OnClientConnectedCallback")]
        [HarmonyPostfix]
        static void JoinLobbyPatch(ulong clientId) 
        {
            if (!StartOfRound.Instance.IsHost) return; 
            

            UnityEngine.Debug.Log("AYAYA a player connected : " + clientId.GetPlayerController().playerUsername);

            //syncing objects


        }

        
    }
}
