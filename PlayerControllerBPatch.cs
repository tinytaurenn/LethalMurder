﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LC_API.GameInterfaceAPI.Events.EventArgs.Player;
using LC_API.GameInterfaceAPI.Events.Handlers;
using LC_API.GameInterfaceAPI.Features;
using LethalNetworkAPI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;
using static UnityEngine.SendMouseEvents;


namespace LethalMurder
{
   

    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {

        ManualLogSource Logger;

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void infiniteSprint(ref float ___sprintMeter)
        {
          
            if(___sprintMeter < 1f)
            {

                //Debug.Log("Sprint Meter is less than 1.0f, setting to 1.0f");
                ___sprintMeter += 0.03f * Time.deltaTime;
            }

            
             
        }
        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        static bool KillPlayerPatch(CauseOfDeath causeOfDeath)
        {

            UnityEngine.Debug.Log("cause of death : " + causeOfDeath.ToString());

            if (Plugin.Instance.modManager.m_Role == ModManager.ERoles.Impostor)
            {
                if (causeOfDeath == CauseOfDeath.Gravity && LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.IsInFactory)
                {
                    Plugin.Instance.modManager.TeleportPlayerAtRandomLocation();
                    return false;
                }

                if(!Plugin.Instance.modManager.voteManager.inVoteMode)
                {
                    if (UnityEngine.Random.Range(0f, 1f) > 0.5f) Plugin.Instance.modManager.CustomSpawnEnemy();
                    //spawn random enemy on impostor death 
                }




            }



                return true; 
        }

        [HarmonyPatch("Interact_performed")]
        [HarmonyPostfix]
        static void InteractPerformedPatch(ref ulong ___actualClientId, PlayerControllerB __instance)
        {





            //find swithc light script

            if (Plugin.Instance.modManager.voteManager.inVoteMode)
            {

                InteractPerformedVoteMode(___actualClientId, __instance);

            }
            else
            {
                InteractPerformedPlayMode(___actualClientId, __instance);
            }





        }

        static void InteractPerformedVoteMode(ulong ___actualClientId, PlayerControllerB __instance)
        {
            if (Plugin.Instance.modManager.voteManager.canVote)
            {
                Plugin.Instance.modManager.voteManager.VotePlayer();
            }
            else
            {
                UnityEngine.Debug.Log("you can't vote now"); 
            }
            

        }

        static void InteractPerformedPlayMode(ulong ___actualClientId, PlayerControllerB __instance)
        {
            if (Plugin.Instance.modManager.canInstantKill == true
                && Plugin.Instance.modManager.voteManager.inVoteMode == false)
            {
                //Plugin.Instance.modManager.ServerInfoMessage.SendServer("toz"); //works very well
                Plugin.Instance.modManager.TryKillMessage.SendServer(___actualClientId);

            }
            else
            {
                //UnityEngine.Debug.Log("You can't kill");
            }

            if (Plugin.Instance.modManager.voteManager.buttonFound && Plugin.Instance.modManager.voteManager.canCallVote)
            {
                Plugin.Instance.modManager.voteManager.VoteCall();
            }
        }


        [HarmonyPatch("SetHoverTipAndCurrentInteractTrigger")]
        [HarmonyPostfix]
        static void InteractTriggerPatch(PlayerControllerB __instance)
        {

            if (Plugin.Instance.modManager == null || Plugin.Instance.modManager.voteManager == null) return; 
            
            if (Plugin.Instance.modManager.voteManager.inVoteMode)
            {

                TipHoverVoteMode(__instance);

            }
            else
            {
                TipHoverPlayMode(__instance);
            }

            
        }

        static void TipHoverVoteMode(PlayerControllerB __instance)
        {
            if (!__instance.isGrabbingObjectAnimation && !__instance.inSpecialMenu && !__instance.quickMenuManager.isMenuOpen)
            {

                Ray hitRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
                RaycastHit[] hitInfos;
                hitInfos = Physics.RaycastAll(hitRay, __instance.grabDistance * 10,8);

                foreach (RaycastHit hit in hitInfos)
                {
                    if (hit.collider.gameObject.CompareTag("Player")
                    && hit.collider.GetComponent<PlayerControllerB>() != null
                        && hit.collider.GetComponent<PlayerControllerB>().isPlayerControlled
                        && hit.collider.GetComponent<PlayerControllerB>().GetClientId() != __instance.GetClientId())
                    {
                        //text for killer
                        __instance.cursorIcon.enabled = true;
                        __instance.cursorIcon.sprite = __instance.grabItemIcon;
                        __instance.cursorTip.text = "Vote for " + hit.collider.GetComponent<PlayerControllerB>().playerUsername;
                        break;
                    }//text over button 

                }
            }
        }

        static void TipHoverPlayMode(PlayerControllerB __instance)
        {
            if (!__instance.isGrabbingObjectAnimation && !__instance.inSpecialMenu && !__instance.quickMenuManager.isMenuOpen)
            {

                //int playerMask = 8;
                Ray hitRay = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
                RaycastHit[] hitInfos;
                hitInfos = Physics.RaycastAll(hitRay, __instance.grabDistance);
                bool buttonFound = false;
                foreach (RaycastHit hit in hitInfos)
                {
                    if (hit.collider.gameObject.CompareTag("Player")
                    && hit.collider.GetComponent<PlayerControllerB>() != null
                        && hit.collider.GetComponent<PlayerControllerB>().isPlayerControlled
                        && hit.collider.GetComponent<PlayerControllerB>().GetClientId() != __instance.GetClientId()
                        && Plugin.Instance.modManager.m_Role == ModManager.ERoles.Impostor)
                    {
                        //text for killer
                        if (Plugin.Instance.modManager.canInstantKill)
                        {
                            //ulong targetClientID = hit.collider.GetComponent<PlayerControllerB>().GetClientId();
                            __instance.cursorIcon.enabled = true;
                            __instance.cursorIcon.sprite = __instance.grabItemIcon;
                            __instance.cursorTip.text = "Murder : [E]";

                            //UnityEngine.Debug.Log("show kill player possibility");

                        }
                        else
                        {

                            int killTime = (int)(Plugin.Instance.modManager.killCooldown - Plugin.Instance.modManager.killCoolDownTime);
                            __instance.cursorTip.text = "You can kill in : " + killTime;

                            //UnityEngine.Debug.Log("show kill player possibility");

                        }
                        break;
                    }//text over button 
                    else if (hit.collider != null && hit.collider.TryGetComponent<ButtonBehavior>(out ButtonBehavior buttonBehavior))
                    {

                        if (Plugin.Instance.modManager.voteManager.canCallVote)
                        {
                            __instance.cursorIcon.enabled = true;
                            __instance.cursorIcon.sprite = __instance.grabItemIcon;
                            __instance.cursorTip.text = "Call for a reunion";
                            buttonFound = true;
                            break;
                        }
                        else
                        {
                            int voteCallTime = (int)(Plugin.Instance.modManager.voteManager.voteCoolDown - Plugin.Instance.modManager.voteManager.voteCoolDownTime);
                            __instance.cursorTip.text = "Reunion can be called in : " + voteCallTime;
                            buttonFound = true;
                            break;
                        }

                        
                    }
                }

                if (Plugin.Instance.modManager.voteManager.buttonFound != buttonFound)
                {
                    Plugin.Instance.modManager.voteManager.buttonFound = buttonFound;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("ConnectClientToPlayerObject")]
        public static void InitializeLocalPlayer()
        {
            if (NetworkManager.Singleton.IsHost) return; 

            Plugin.Instance.modManager.syncParametersMessage_1.SendServer(Plugin.Instance.modManager.syncParameters_1);
        }





    }
}
