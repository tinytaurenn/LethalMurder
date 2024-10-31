using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameNetcodeStuff;

using UnityEngine;

using System.Collections;
using UnityEngine.UIElements.Collections;
using Unity.Netcode;
using UnityEngine.UIElements.UIR;
using System.Linq;
using LethalNetworkAPI;
using System.Drawing.Drawing2D;
using DunGen;
using LethalNetworkAPI.Utils;
using System.Data;
using LC_API.GameInterfaceAPI.Events.EventArgs.Player;

namespace LethalMurder
{
    internal class ModManager : MonoBehaviour
    {

       
        public enum ERoles
        {
            Crewmate = 0,
            Impostor = 1
        }

        public struct FPlayer
        {
            public ulong playerID;
            public ERoles role;
            public PlayerControllerB playerScript;
            public LC_API.GameInterfaceAPI.Features.Player API_Player; 

            public FPlayer()
            {
                playerID = 0;
                role = ERoles.Crewmate;
                playerScript = null;
                API_Player = null;

            }

            public FPlayer(ulong playerID, ERoles role, PlayerControllerB playerScript, LC_API.GameInterfaceAPI.Features.Player apiPlayer )
            {
                this.playerID = playerID;
                this.role = role;
                this.playerScript = playerScript;
                this.API_Player = apiPlayer;
                
            }
        }

        internal ERoles m_Role = ERoles.Crewmate;
        internal bool isRoleReceived = false;

        internal bool canShipLever = false; 
        internal bool canInstantKill = false;
        internal float killCooldown = 30f; // set to 120 normally
        internal float killCoolDownTime = 0f;

        internal LethalClientMessage<ulong> CloseKillMessage; // client to all clients : nice but nothing works on serv
        internal LNetworkMessage<ulong> TryKillMessage; 
        internal LNetworkMessage<float> KillCoolDownMessage; 

         
        internal LNetworkMessage<int> RoleMessage;
        internal LNetworkMessage<string> ServerInfoMessage;
        internal LNetworkMessage<bool> PlayerDiedMessage; 
        internal LNetworkMessage<bool> CanShipLeverMessage;
        internal LNetworkMessage<Vector3> SpawnEnemyOnPlayerPositionMessage;

        //vote manager
        internal VoteManager voteManager;
        

        //server only infos 

        internal List<FPlayer> playerList = new List<FPlayer>(); 
        internal int impostorCount = 0;
        internal int crewMateCount = 0;
        


        private void Awake()
        {



            CloseKillMessage = new LethalClientMessage<ulong>("closeKillMessage");

            TryKillMessage = LNetworkMessage<ulong>.Create("TryKillMessage");
            KillCoolDownMessage = LNetworkMessage<float>.Create("KillCoolDownMessage");


            RoleMessage = LNetworkMessage<int>.Create("RoleMessageServer"); 
            ServerInfoMessage = LNetworkMessage<string>.Create("ServerInfoMessage"); 
            PlayerDiedMessage = LNetworkMessage<bool>.Create("ImpostorDeathMessage");
            CanShipLeverMessage = LNetworkMessage<bool>.Create("CanShipLeverMessage");
            SpawnEnemyOnPlayerPositionMessage = LNetworkMessage<Vector3>.Create("SpawnEnemyOnPlayerPositionMessage");

            CreatingVoteManager();

        }

        
        void OnEnable()
        {
            UnityEngine.Debug.Log("ModManager OnEnable");

            CloseKillMessage.OnReceivedFromClient += OnCloseKillClientReceived;
            

            RoleMessage.OnClientReceived += OnRoleReceivedForClient; // this one when from serv to all

            ServerInfoMessage.OnServerReceived += InfoMessageServerReception;
            ServerInfoMessage.OnClientReceived += InfoMessageClientReception; 

            TryKillMessage.OnServerReceived += OntryKillServerCalculations; 
            TryKillMessage.OnClientReceived += OnClientKill;

            KillCoolDownMessage.OnClientReceived += RunKillCoolDownOnClient;

            PlayerDiedMessage.OnServerReceived += OnPlayerDeathServer;

            CanShipLeverMessage.OnClientReceived += ShipLeverModif; 

            SpawnEnemyOnPlayerPositionMessage.OnServerReceived += SpawnEnemyServerSide;
           
            

            LC_API.GameInterfaceAPI.GameState.LandOnMoon += LandOnMoonEvent;
            LC_API.GameInterfaceAPI.GameState.ShipStartedLeaving += ShipLeavingEvent; 

            LC_API.GameInterfaceAPI.Events.Handlers.Player.Died += OnPlayerDied;

        }

        

        void OnDisable()
        {
            CloseKillMessage.OnReceivedFromClient -= OnCloseKillClientReceived;


            RoleMessage.OnClientReceived -= OnRoleReceivedForClient; // this one when from serv to all

            ServerInfoMessage.OnServerReceived -= InfoMessageServerReception;
            ServerInfoMessage.OnClientReceived -= InfoMessageClientReception;

            TryKillMessage.OnServerReceived -= OntryKillServerCalculations;
            TryKillMessage.OnClientReceived -= OnClientKill;


            CanShipLeverMessage.OnClientReceived -= ShipLeverModif; //enable a client to run the ship 

            SpawnEnemyOnPlayerPositionMessage.OnServerReceived -= SpawnEnemyServerSide; //handler to spawn an ennemy



            LC_API.GameInterfaceAPI.GameState.LandOnMoon -= LandOnMoonEvent;

            LC_API.GameInterfaceAPI.Events.Handlers.Player.Died -= OnPlayerDied;
            PlayerDiedMessage.OnServerReceived -= OnPlayerDeathServer;
        }

        #region Main Plugin Functions 

        void LandOnMoonEvent()
        {
            isRoleReceived = false;
            UnityEngine.Debug.Log("LandOnMoonEvent");
            canShipLever = false; 

            if (LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.IsHost) CreatingRoles();//host only


        }

        private void ShipLeavingEvent()
        {
            StopAllCoroutines(); 
            canInstantKill = false; 
        }

        void UpdatingPlayerScripts()
        {
            playerList.Clear();
            
            foreach (LC_API.GameInterfaceAPI.Features.Player player in LC_API.GameInterfaceAPI.Features.Player.ActiveList)
            {
                playerList.Add(new FPlayer(player.ClientId, ERoles.Crewmate, player.PlayerController, player));
                //unity.debug.log playerlist count and each player id and username
                UnityEngine.Debug.Log("PlayerList count : " + playerList.Count);
                UnityEngine.Debug.Log("Player ID : " + player.ClientId);
                UnityEngine.Debug.Log("Player Username : " + player.Username);

            }

        }


        void CreatingRoles()
        {
            //host only
            UpdatingPlayerScripts();

            int randomImpostor = UnityEngine.Random.Range(0, playerList.Count);

            for (int i = 0; i < playerList.Count; i++)
            {
                if (i == randomImpostor)
                {

                    //playerList[i] = new FPlayer { playerID = playerList[i].playerID, role = ERoles.Impostor, playerScript = playerList[i].playerScript };
                    playerList[i] = new FPlayer(playerList[i].playerID, ERoles.Impostor, playerList[i].playerScript, playerList[i].API_Player);
                    

                    impostorCount++;

                }
                else
                {
                    if (UnityEngine.Random.Range(0f, 1f) > 0.8f)
                    {
                        //LC_API.GameInterfaceAPI.Features.Item.CreateAndGiveItem("Shovel", LC_API.GameInterfaceAPI.Features.Player.Get(playerList[i].playerID), true, true);
                        //GiveItemToPlayer("Shotgun", playerList[i].playerID);
                    }
                    crewMateCount++;


                }


                UnityEngine.Debug.Log("PlayerList count after role: " + playerList.Count);
                UnityEngine.Debug.Log("Player ID after role: " + playerList[i].playerID);
                UnityEngine.Debug.Log("Player Username after role: " + playerList[i].playerScript.playerUsername);

                //


            }
            UnityEngine.Debug.Log("LISTING EVERY PLAYERS ROLES AND ID  ");
            foreach (FPlayer player in playerList)
            {
                UnityEngine.Debug.Log("Player ID: " + player.playerID + " Role: " + player.role);

            }

            UnityEngine.Debug.Log("SENDING ROLES ");

            foreach (FPlayer player in playerList)
            {
                RoleMessage.SendClient((int)player.role, player.playerID);


            }

            
            foreach (FPlayer player in playerList)
            {

                //LC_API.GameInterfaceAPI.Features.Item.CreateAndGiveItem("Shovel", player.API_Player, true, true);
                
                LC_API.GameInterfaceAPI.Features.Item.CreateAndSpawnItem("Shovel", true, player.playerScript.transform.position, Quaternion.identity);
                

                
            }

        }


        private void ShipLeverModif(bool CanShipLever)
        {
            UnityEngine.Debug.Log("Ship Lever Modif to : " + CanShipLever);
            canShipLever = CanShipLever;
        }

        #endregion

        #region Vote Manager 


        void CreatingVoteManager()
        {
            UnityEngine.Debug.Log("Creating VoteManager");
            GameObject voteManagerObject = new GameObject("ModManager");
            UnityEngine.GameObject.DontDestroyOnLoad(voteManagerObject);
            voteManagerObject.hideFlags = (HideFlags)61;
            voteManager = voteManagerObject.AddComponent<VoteManager>(); ;
        }

        #endregion


        private void GiveItemToPlayer(string itemString, ulong PlayerID)
        {

            if (PlayerID == LC_API.GameInterfaceAPI.Features.Player.HostPlayer.ClientId)
            {
                try
                {
                    LC_API.GameInterfaceAPI.Features.Item.CreateAndGiveItem(itemString, LC_API.GameInterfaceAPI.Features.Player.HostPlayer, true, true);
                }
                catch (NullReferenceException err)
                {

                    UnityEngine.Debug.Log("tema l'erreur " + err.Message);
                }

            }
            else
            {
                LC_API.GameInterfaceAPI.Features.Item.CreateAndGiveItem(itemString, LC_API.GameInterfaceAPI.Features.Player.Get(PlayerID), true, true);
            }
        }

        private void InfoMessageServerReception(string param, ulong ClientID)
        {
            UnityEngine.Debug.Log("Server Received Info Message " + param + " and client id :  " + ClientID);
        }

        private void InfoMessageClientReception(string message)
        {
            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.QueueTip("INFO MESSAGE", message, 10);
        }

        private void OnPlayerDied(DiedEventArgs ev)
        {

            if(!ev.Player.IsLocalPlayer) return;

            UnityEngine.Debug.Log("Player Died");
            if (m_Role == ERoles.Impostor)
            {
                PlayerDiedMessage.SendServer(true);
            }
            else
            {
                PlayerDiedMessage.SendServer(false);
            }
        }

        private void OnPlayerDeathServer(bool isImpostor, ulong ClientID)
        {
            if(isImpostor)
            {
                UnityEngine.Debug.Log("Impostor Died");
                impostorCount--;

                if(impostorCount <= 0)
                {
                    //mesage to all
                    ServerInfoMessage.SendClients("the impostors are dead, the crewmates win!"); 
                    CanShipLeverMessage.SendClients(true);
                }
            }
            else
            {
                crewMateCount--; 
                if(crewMateCount <= 0)
                {
                    //mesage to all
                    ServerInfoMessage.SendClients("the crewmates are all dead, the impostors win!");
                    CanShipLeverMessage.SendClients(true);
                }
            }
        }
        private void OnRoleReceivedForClient(int Role)
        {
            if (isRoleReceived) return; 

            isRoleReceived = true;
            m_Role = (ERoles)Role;
            UnityEngine.Debug.Log("I AM  :  " + m_Role); 
            if(m_Role == ERoles.Impostor)
            {
                RunKillCooldown(killCooldown);
                //HUDManager.Instance.playerScreenTexture.color = new Color(1f, 0.85f, 0.85f, 1f);

            }
            else
            {
                StopAllCoroutines();
                canInstantKill = false;
                //HUDManager.Instance.playerScreenTexture.color = new Color(1f, 1f, 1f, 1f);

            }

            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.QueueTip("YOUR ROLE IS ",m_Role.ToString(),15,1,true);
            
            
        }

        


        #region Killing Functions 

        private void RunKillCoolDownOnClient(float cooldown)
        {
            if (m_Role == ERoles.Impostor) RunKillCooldown(killCooldown);

        }

        private void OntryKillServerCalculations(ulong param, ulong ClientID)
        {
            UnityEngine.Debug.Log("Server Received Try Kill Message " + param + " and client id :  " + ClientID);
            PlayerControllerB player = ClientID.GetPlayerController();

            if (!player.isPlayerControlled) return;

            RaycastHit[] hitInfos; 
            hitInfos =  Physics.RaycastAll(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward, player.grabDistance,8);
            foreach(RaycastHit hit in hitInfos)
            {
                if(hit.collider.gameObject.CompareTag("Player")
                    &&hit.collider.GetComponent<PlayerControllerB>() != null
                    && hit.collider.GetComponent<PlayerControllerB>().isPlayerControlled
                    && hit.collider.GetComponent<PlayerControllerB>().GetClientId() != ClientID)
                {
                    ulong targetClientID = hit.collider.GetComponent<PlayerControllerB>().GetClientId();
                    UnityEngine.Debug.Log("Player" + targetClientID + "  Hit");
                    TryKillMessage.SendClient(targetClientID, targetClientID); 
                    KillCoolDownMessage.SendClient(killCooldown, ClientID);
                    break;
                }
            }
        }

        private void OnClientKill(ulong param)
        {
            UnityEngine.Debug.Log("Client Received Try Kill Message " + param);
            //((ulong)RoundManager.Instance.playersManager.thisClientPlayerId).GetPlayerController().KillPlayer(Vector3.one * 5.0f);
            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.Kill(Vector3.one * 5.0f);


        }

        internal void RunKillCooldown(float time)
        {
            canInstantKill = false;
            StartCoroutine(KillCooldownWithTimer(time));
        }

        //deprecated
        [Obsolete("Use KillCooldownWithTimer instead", true)]
        IEnumerator KillCooldown(float time)
        {
            float timeBeforeWarning = time / 4f;


            yield return new WaitForSeconds(time * 0.75f);

            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.QueueTip("KILL COOLDOWN", "ENDS IN " + time * 0.25f + " SECONDS", 5);

            yield return new WaitForSeconds(time * 0.25f);

            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.QueueTip("YOU CAN INSTANT KILL", "Just Press E on a Player", 10);
            canInstantKill = true;
        }

        IEnumerator KillCooldownWithTimer(float time)
        {

            float i = 0; 
            float rate = 1 / time;
            while(i < 1)
            {
                killCoolDownTime = i * time;
                i += Time.deltaTime * rate;
                yield return 0; 
            }

            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.QueueTip("YOU CAN INSTANT KILL", "Just Press E on a Player", 10);
            canInstantKill = true;
            killCoolDownTime = 0;
        }

        #endregion


        #region teleporting player functions 

        internal void TeleportPlayerAtRandomLocation()
        {
            if (LC_API.GameInterfaceAPI.GameState.ShipState != LC_API.Data.ShipState.OnMoon) return;

            System.Random shipTeleporterSeed = new System.Random();
            Vector3 position2 = RoundManager.Instance.insideAINodes[shipTeleporterSeed.Next(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
            Vector3 inBoxPredictable = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position2, randomSeed: shipTeleporterSeed);

            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.TeleportPlayer(inBoxPredictable);
        }

        internal void TeleportPlayerAtRandomLocation(PlayerControllerB playerController)
        {
            if (LC_API.GameInterfaceAPI.GameState.ShipState != LC_API.Data.ShipState.OnMoon) return;

            System.Random shipTeleporterSeed = new System.Random();
            Vector3 position2 = RoundManager.Instance.insideAINodes[shipTeleporterSeed.Next(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
            Vector3 inBoxPredictable = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position2, randomSeed: shipTeleporterSeed);

            playerController.TeleportPlayer(inBoxPredictable);
        }

        #endregion


        #region Spawning Enemies functions

        internal void CustomSpawnEnemy()
        {
            if (LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.IsHost)
            {
                SpawnEnemyServerSide(LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.Position, 0);
            }
            else
            {
                SpawnEnemyClientSide();
            }

        }


        internal void SpawnEnemyServerSide(Vector3 position, ulong senderID)
        {
            if (LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.IsInFactory)
            {
                if (RoundManager.Instance.currentLevel.Enemies.Count == 0) return; 

                int randomEnemy = UnityEngine.Random.Range(0, RoundManager.Instance.currentLevel.Enemies.Count);
                GameObject enemyObj = RoundManager.Instance.currentLevel.Enemies[0].enemyType.enemyPrefab;
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(enemyObj, position, Quaternion.Euler(Vector3.zero));
                gameObject.gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                RoundManager.Instance.SpawnedEnemies.Add(gameObject.GetComponent<EnemyAI>());
                ++gameObject.GetComponent<EnemyAI>().enemyType.numberSpawned;
            }
            else
            {

                if (RoundManager.Instance.currentLevel.OutsideEnemies.Count == 0) return;

                int randomEnemy = UnityEngine.Random.Range(0, RoundManager.Instance.currentLevel.OutsideEnemies.Count);
                GameObject enemyObj = RoundManager.Instance.currentLevel.OutsideEnemies[0].enemyType.enemyPrefab;
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(enemyObj, position, Quaternion.Euler(Vector3.zero));
                gameObject.gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
                RoundManager.Instance.SpawnedEnemies.Add(gameObject.GetComponent<EnemyAI>());
                ++gameObject.GetComponent<EnemyAI>().enemyType.numberSpawned;
            }
        }

        internal void SpawnEnemyClientSide()
        {
            SpawnEnemyOnPlayerPositionMessage.SendServer(LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.Position);
        }

        #endregion


        #region oldStuff
        //useless
        private void OnCloseKillClientReceived(ulong parameter, ulong clientID)
        {
            UnityEngine.Debug.Log("Client Received Close Kill Message " + parameter + " and client id :  " + clientID);
            //ulong thisIS = (ulong)RoundManager.Instance.playersManager.thisClientPlayerId;
            ulong thisIS = LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.ClientId;

            float distance = Vector3.Distance(thisIS.GetPlayerController().transform.position, clientID.GetPlayerController().transform.position);
            UnityEngine.Debug.Log("Distance between players is " + distance);


        }

        #endregion


    }


}
