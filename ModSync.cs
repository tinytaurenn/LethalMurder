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

            Plugin.Instance.modManager.syncParametersMessage_1.SendClient(Plugin.Instance.modManager.syncParameters_1, clientId); 

            //syncing objects


        }
        [HarmonyPatch("Disconnect")]
        [HarmonyPostfix]
        static void DisconnectPatch()
        {
            UnityEngine.Debug.Log("Disconnecting, stoping routines and etc");
            if (Plugin.Instance.modManager.KillRoutine != null)
            {
                Plugin.Instance.modManager.StopCoroutine(Plugin.Instance.modManager.KillRoutine);
            }
            if(Plugin.Instance.modManager.voteManager.VoteTimeRoutine != null)
            {
                Plugin.Instance.modManager.StopCoroutine(Plugin.Instance.modManager.voteManager.VoteTimeRoutine);
            }

            Plugin.Instance.modManager.voteManager.DestroyAllVoteObjects();
            Plugin.Instance.modManager.voteManager.inVoteMode = false;
            Plugin.Instance.modManager.canInstantKill = false;
            Plugin.Instance.modManager.canShipLever = true;
            Plugin.Instance.modManager.voteManager.canCallVote = false; 

            
            

            //Plugin.CreateModManager(); 
            

        }

        
    }
}
