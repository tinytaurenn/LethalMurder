using GameNetcodeStuff;
using LethalNetworkAPI;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace LethalMurder
{
    internal class VoteManager : MonoBehaviour
    {
        //internal struct FendVoteStruct
        //{
        //    internal bool isKilled;
        //    internal int testInt;
        //    internal Vector3 vectorTest; 

        //    public FendVoteStruct()
        //    {
        //        isKilled = false;
        //        testInt = 1;
        //        vectorTest = Vector3.one; 
        //    }
        //}

        internal LNetworkMessage<Vector3> voteCallMessage; 
        internal LNetworkMessage<Vector3> ButtonCreationMessage;
        internal LNetworkMessage<bool> EndVoteMessage; 
        internal LNetworkMessage<int> VotePlayerMessage; 


        public static AssetBundle voteButtonBundle;

        internal VotePositions VotePositions; 

        internal bool buttonFound = false;
        internal bool canCallVote = false; 
        internal float voteCoolDown = 30.0f;
        internal float voteCoolDownTime = 0.0f;

        internal bool inVoteMode = false;
        internal bool canVote = false;
        
        internal TMP_Text VoteTimer;
        internal Coroutine VoteTimeRoutine; 

        internal float VoteTime = 30.0f;
        internal float ShortVoteTime = 7.0f;

        //instantiated objects
        GameObject VoteButtonGO; 
        GameObject VoteTimerGO;
        GameObject VotePositionsGO;
        GameObject VoteRoomGO;
        



        Vector3 VoteTarget; 

        internal struct FplayerVote
        {
            internal ModManager.FPlayer Player;
            internal int voteNumber;

            public FplayerVote()
            {
                Player = new ModManager.FPlayer();
                voteNumber = 0; 
            }

            public FplayerVote(ModManager.FPlayer player, int voteNumber)
            {
                Player = player;
                this.voteNumber = voteNumber;
            }
        }

        List<FplayerVote> playerVotes = new List<FplayerVote>();

        void Awake()
        {
           voteCallMessage = LNetworkMessage<Vector3>.Create("VoteCall");
           ButtonCreationMessage = LNetworkMessage<Vector3>.Create("ButtonCreation");
           EndVoteMessage = LNetworkMessage<bool>.Create("EndVote");
           VotePlayerMessage = LNetworkMessage<int>.Create("VotePlayer");

            

        }



        void OnEnable()
        {
            voteCallMessage.OnServerReceived += VoteCallServerReceived;
            voteCallMessage.OnClientReceived += VoteCallClientReceived;

            ButtonCreationMessage.OnServerReceived += VoteButtonCreationServerReceived;
            ButtonCreationMessage.OnClientReceived += VoteButtonCreationClientReceived;

            EndVoteMessage.OnServerReceived += EndVoteServerReceived;
            EndVoteMessage.OnClientReceived += EndVoteClientReceived;

            VotePlayerMessage.OnServerReceived += VotePlayerServerReceived;
            VotePlayerMessage.OnClientReceived += VotePlayerClientReceived;



        }

        

        void OnDisable()
        {
            voteCallMessage.OnServerReceived -= VoteCallServerReceived;
            voteCallMessage.OnClientReceived -= VoteCallClientReceived;

            ButtonCreationMessage.OnServerReceived -= VoteButtonCreationServerReceived;
            ButtonCreationMessage.OnClientReceived -= VoteButtonCreationClientReceived;

            EndVoteMessage.OnServerReceived -= EndVoteServerReceived;
            EndVoteMessage.OnClientReceived -= EndVoteClientReceived;

            VotePlayerMessage.OnServerReceived -= VotePlayerServerReceived;
            VotePlayerMessage.OnClientReceived -= VotePlayerClientReceived;


        }

        #region voteCoolDown

        IEnumerator VoteCoolDown(float time)
        {
            float i = 0;
            float rate = 1 / time;
            while (i < 1)
            {
                voteCoolDownTime = i * time;
                i += Time.deltaTime * rate;
                yield return 0;
            }

            canCallVote = true;
        }
        #endregion
        #region ButtonCreation

        internal void VoteButtonCreation()
        {

            Vector3 pos = LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.transform.position + LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.transform.forward * 15;

            //pos = StartOfRound.Instance.shipDoorNode.transform.position; 
            pos = StartOfRound.Instance.shipBounds.transform.position
                + (-StartOfRound.Instance.shipBounds.transform.right * 20)
                + (Vector3.down * 10); 

            if (LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.IsHost)
            {
                VoteButtonCreationServerReceived(pos, LC_API.GameInterfaceAPI.Features.Player.HostPlayer.ClientId);
            }
            else
            {
                ButtonCreationMessage.SendServer(pos);
            }


        }
        void VoteButtonCreationServerReceived(Vector3 message, ulong clientID)
        {
            ButtonCreationMessage.SendClients(message);
        }

        void VoteButtonCreationClientReceived(Vector3 message)
        {

            if (voteButtonBundle == null)
            {
                string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                UnityEngine.Debug.Log(Path.Combine(assemblyLocation, "modassets"));

                voteButtonBundle = AssetBundle.LoadFromFile(Path.Combine(assemblyLocation, "modassets"));
            }

            VoteButtonGO = voteButtonBundle.LoadAsset<GameObject>("Button");
            VoteRoomGO = voteButtonBundle.LoadAsset<GameObject>("VoteRoom");
            VotePositionsGO = voteButtonBundle.LoadAsset<GameObject>("VotePositions");
            VoteTimerGO = voteButtonBundle.LoadAsset<GameObject>("Vote_Timer");
            

            VoteButtonGO = UnityEngine.GameObject.Instantiate(VoteButtonGO, message, Quaternion.identity);

            VoteButtonGO.AddComponent<ButtonBehavior>();

            VoteRoomGO =  UnityEngine.GameObject.Instantiate(VoteRoomGO, message + Vector3.up * 100, Quaternion.identity);
            VoteTimerGO =  UnityEngine.GameObject.Instantiate(VoteTimerGO, message + Vector3.up * 100, Quaternion.identity);

            VoteTimer = VoteTimerGO.GetComponentInChildren<TMP_Text>();
            VoteTimer.text = "its working";
            StartCoroutine(VoteCoolDown(voteCoolDown)); 

            
            VotePositionsGO =  UnityEngine.GameObject.Instantiate(VotePositionsGO, message + Vector3.up * 100, Quaternion.identity);
            

            VotePositions = VotePositionsGO.AddComponent<VotePositions>();

            //need sync on clients connections 



        }

        internal void DestroyAllVoteObjects()
        {
            if (VoteButtonGO != null) UnityEngine.GameObject.Destroy(VoteButtonGO);
            if (VoteRoomGO != null) UnityEngine.GameObject.Destroy(VoteRoomGO);
            if (VotePositionsGO != null) UnityEngine.GameObject.Destroy(VotePositionsGO);
            if (VoteTimerGO != null) UnityEngine.GameObject.Destroy(VoteTimerGO);
  
        }

        #endregion

        #region voteCall

        internal void VoteCall()
        {
            
            if(LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.IsHost)
            {
                VoteCallServerReceived(Vector3.zero, LC_API.GameInterfaceAPI.Features.Player.HostPlayer.ClientId); 
            }
            else
            {
                voteCallMessage.SendServer(Vector3.zero);
            }

        }

        void VoteCallServerReceived(Vector3 message, ulong clientID)
        {

            

            UnityEngine.Debug.Log("message received : " + message);
            
            if (VotePositions == null) return;

            VotePositions.ClearChairs(); 
            foreach(ModManager.FPlayer client in Plugin.Instance.modManager.playerList)
            {
                VotePositions.FVotePosition FPos;
                VotePositions.FindEmptyChair(out FPos);
                Vector3 pos = FPos.transform.position;


                voteCallMessage.SendClient(pos, client.playerID);
            }
            playerVotes.Clear(); //clear votes list

            foreach (ModManager.FPlayer client in Plugin.Instance.modManager.playerList)
            {
                if(!client.API_Player.IsDead) playerVotes.Add(new FplayerVote(client, 0));

            }

            //start counting time and receiveing votes
            //create list of playerVotes
            //receive votes from clients
            //each player must point onsomething when they vote

        }

        void VoteCallClientReceived(Vector3 pos)
        {
            UnityEngine.Debug.Log("client received vote call");
            VoteTimer.enabled = true;
            TeleportPlayerToVote(pos);
            canVote = true;
            canCallVote = false;
            if(VoteTimeRoutine != null) StopCoroutine(VoteTimeRoutine);
            VoteTimeRoutine = StartCoroutine(TimerRoutine(VoteTime));

            if (Plugin.Instance.modManager.m_Role == ModManager.ERoles.Impostor)
            {
                if(Plugin.Instance.modManager.KillRoutine != null) StopCoroutine(Plugin.Instance.modManager.KillRoutine);

            }
        }
        #endregion
       
        #region endVote
        void EndVote()
        {
            if (LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.IsHost)
            {
                EndVoteServerReceived(false, LC_API.GameInterfaceAPI.Features.Player.HostPlayer.ClientId);
            }
            
        }

        

        private void EndVoteServerReceived(bool isKilled, ulong ClientID)
        {
            UnityEngine.Debug.Log("Server End vote received");
            //starting server calculations for the vote zone 

            List<FplayerVote> EqualVotePlayerList = new List<FplayerVote>();
            if(IsThereEqualVotes(out EqualVotePlayerList))
            {
                UnityEngine.Debug.Log("equal votes, no one will be killed");

                foreach (FplayerVote player in playerVotes)
                {
                    EndVoteMessage.SendClient(false, player.Player.playerID);
                }
            }
            else
            {
                foreach (FplayerVote player in playerVotes)
                {
                    if (player.Player.playerID == EqualVotePlayerList[0].Player.playerID)
                    {
                        EndVoteMessage.SendClient(true, player.Player.playerID);
                    }
                    else
                    {
                        EndVoteMessage.SendClient(false, player.Player.playerID);
                    }
                }
            }

            



        }

        private void EndVoteClientReceived(bool isKilled)
        {
            VoteTimer.enabled = false;
            if (VoteTimeRoutine != null) StopCoroutine(VoteTimeRoutine);
            StartCoroutine(VoteCoolDown(voteCoolDown));

            if (isKilled)
            {
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.Kill(Vector3.one * 5.0f);
            }
            else
            {
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.disableLookInput = false;
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.timeSinceStartingEmote = 0;
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.performingEmote = true;
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.playerBodyAnimator.SetInteger("emoteNumber", 1);//2 is finger
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.StartPerformingEmoteServerRpc();
                //wait 8 sec and teleport back to position before vote
                 
                Task.Delay((int)ShortVoteTime*1000).ContinueWith(t => { ReleasePlayer(); });
            }
        }

        #endregion

        #region playerVote

        internal void VotePlayer()
        {
            canVote = false;

            if (LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.IsHost)
            {
                VotePlayerServerReceived(1, LC_API.GameInterfaceAPI.Features.Player.HostPlayer.ClientId);
            }
            else
            {
                VotePlayerMessage.SendServer(1);
            }

        }

        private void VotePlayerClientReceived(int voteNumber)
        {
            //client received a vote on himself

            if (voteNumber == 0)
            {
                UnityEngine.Debug.Log("you voted on the wall");
                canVote = true;
            }
            else
            {
                UnityEngine.Debug.Log("you voted on a player");
                canVote = false;


                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.disableLookInput = true;
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.timeSinceStartingEmote = 0;
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.performingEmote = true;
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.playerBodyAnimator.SetInteger("emoteNumber", 2);//2 is finger
                LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.StartPerformingEmoteServerRpc();
            }

        }

        private void VotePlayerServerReceived(int voteNumber, ulong ClientID)
        {
            //does calculations for the vote, add to the list etc

            UnityEngine.Debug.Log("Server received vote Message " + voteNumber + " and client id :  " + ClientID);
            PlayerControllerB player = ClientID.GetPlayerController();

            if (!player.isPlayerControlled) return;

            RaycastHit[] hitInfos;
            hitInfos = Physics.RaycastAll(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward, player.grabDistance * 10, 8);
            foreach (RaycastHit hit in hitInfos)
            {
                if (hit.collider.gameObject.CompareTag("Player")
                    && hit.collider.GetComponent<PlayerControllerB>() != null
                    && hit.collider.GetComponent<PlayerControllerB>().isPlayerControlled
                    && hit.collider.GetComponent<PlayerControllerB>().GetClientId() != ClientID)
                {
                    ulong targetClientID = hit.collider.GetComponent<PlayerControllerB>().GetClientId();
                    UnityEngine.Debug.Log("Player" + targetClientID + "  voted");
                    if (Plugin.Instance.modManager.GetPlayerByID(targetClientID, out ModManager.FPlayer targetPlayer))
                    {
                        if (GetPlayerVoteIndex(targetPlayer, out int playerVoteIndex))
                        {
                            playerVotes[playerVoteIndex] = new FplayerVote(playerVotes[playerVoteIndex].Player, playerVotes[playerVoteIndex].voteNumber + 1);


                            VotePlayerMessage.SendClient(1, ClientID);
                            //debug every vote in playerVotes
                            foreach (FplayerVote vote in playerVotes)
                            {
                                UnityEngine.Debug.Log("Player : " + vote.Player.playerID + "  votes : " + vote.voteNumber);
                            }
                            if (AllVotesDone())
                            {
                                UnityEngine.Debug.Log("vote number is : " + voteNumber + "  playerVotes.Count : " + playerVotes.Count + "ending vote");
                                EndVote();
                            }
                            return;
                        }
                        else
                        {
                            UnityEngine.Debug.Log("can't get player index");

                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log("can't get player by ID");

                    }

                    break;
                }
            }
            VotePlayerMessage.SendClient(0, ClientID);

        }

        #endregion
       

        #region ClientTeleportations
        void ReleasePlayer()
        {

            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.disableMoveInput = false;
            
            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.TeleportPlayer(VotePositions.PositionBeforeVote);

            StopCoroutine(VoteTimeRoutine);
            inVoteMode = false;
            canVote = false;

            if(Plugin.Instance.modManager.m_Role == ModManager.ERoles.Impostor)
            {
                Plugin.Instance.modManager.RunKillCooldown(Plugin.Instance.modManager.killCooldown ); 
            }
        }

        void TeleportPlayerToVote(Vector3 pos)
        {
            if (VotePositions != null) VotePositions.PositionBeforeVote = pos + Vector3.down * 98;
            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.disableMoveInput = true;
            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.TeleportPlayer(pos + Vector3.up * 5);
            inVoteMode = true;
        }
        #endregion

        #region UtilsFunctions

        internal bool GetPlayerVoteIndex(ModManager.FPlayer player, out int playerVoteIndex)
        {
            playerVoteIndex = -1;
            for (int i = 0; i < playerVotes.Count; i++)
            {
                if (playerVotes[i].Player.playerID == player.playerID)
                {
                    playerVoteIndex = i;
                    return true;
                }
            }
            return false;
        }

        
        bool AllVotesDone()
        {
            int voteNumber = 0;
            foreach (FplayerVote vote in playerVotes)
            {
                voteNumber += vote.voteNumber;
            }
            if (voteNumber >= playerVotes.Count)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal ulong FindPlayerIDWithMostVotes(out int maxVotes)
        {
            maxVotes = 0;
            ulong clientID = 0;

            foreach (FplayerVote vote in playerVotes)
            {
                if (vote.voteNumber > maxVotes)
                {
                    maxVotes = vote.voteNumber;
                    clientID = vote.Player.playerID;
                }
            }

            return clientID;
        }

        internal FplayerVote FindPlayerWithMostVotes(out int maxVotes)
        {
            maxVotes = 0;
            FplayerVote playerVote = new FplayerVote();

            foreach (FplayerVote vote in playerVotes)
            {
                if (vote.voteNumber > maxVotes)
                {
                    maxVotes = vote.voteNumber;
                    playerVote = vote;
                }
            }

            return playerVote;
        }

        internal bool IsThereEqualVotes(out List<FplayerVote> EqualVotePlayerList)
        {
            EqualVotePlayerList = new List<FplayerVote>();

            int maxVoteNumber = 0;
            
            int totalVoteNumber = 0;
            EqualVotePlayerList.Add(FindPlayerWithMostVotes(out maxVoteNumber));

            foreach (FplayerVote votePlayer in playerVotes)
            {
                totalVoteNumber += votePlayer.voteNumber;
                if (votePlayer.voteNumber == maxVoteNumber && !EqualVotePlayerList.Contains(votePlayer)) EqualVotePlayerList.Add(votePlayer);
            }
            if(playerVotes.Count/2 >= totalVoteNumber)
            {
                return true;
            }

            if (EqualVotePlayerList.Count > 1)
            {
                UnityEngine.Debug.Log("there is equal votings of " + EqualVotePlayerList.Count + " players");
                return true;
            }
            else
            {
                UnityEngine.Debug.Log("one player must die : " + EqualVotePlayerList[0].Player.API_Player.Username);
                return false;
            }

        }

        #endregion

        #region VoteTimerFunctions



        IEnumerator TimerRoutine(float time)
        {
            float timer = time;

            VoteTimer.text = "Time left : " + timer + " seconds";

            while (timer > 0)
            {
                yield return new WaitForSeconds(1);

                timer--; 
                VoteTimer.text = "Time left : " + timer + " seconds";
            }


            VoteTimer.text = "no Time Left";

            EndVote(); 
            
            yield return 0; 
        }



        #endregion


    }
}
