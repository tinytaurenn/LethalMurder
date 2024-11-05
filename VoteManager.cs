using LethalNetworkAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace LethalMurder
{
    internal class VoteManager : MonoBehaviour
    {
        internal LNetworkMessage<Vector3> voteCallMessage; 
        internal LNetworkMessage<Vector3> ButtonCreationMessage;

        public static AssetBundle voteButtonBundle;

        internal VotePositions VotePositions; 

        internal bool buttonFound = false;

        internal bool inVoteMode = false; 


        void Awake()
        {
           voteCallMessage = LNetworkMessage<Vector3>.Create("VoteCall");
           ButtonCreationMessage = LNetworkMessage<Vector3>.Create("ButtonCreation");
        }



        void OnEnable()
        {
            voteCallMessage.OnServerReceived += VoteCallServerReceived;
            voteCallMessage.OnClientReceived += VoteCallClientReceived;

            ButtonCreationMessage.OnServerReceived += VoteButtonCreationServerReceived;
            ButtonCreationMessage.OnClientReceived += VoteButtonCreationClientReceived;


        }

        void OnDisable()
        {
            voteCallMessage.OnServerReceived -= VoteCallServerReceived;
            voteCallMessage.OnClientReceived -= VoteCallClientReceived;

            ButtonCreationMessage.OnServerReceived -= VoteButtonCreationServerReceived;
            ButtonCreationMessage.OnClientReceived -= VoteButtonCreationClientReceived;

        }

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
        }

        void VoteCallClientReceived(Vector3 pos)
        {
            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.disableMoveInput = true;
            LC_API.GameInterfaceAPI.Features.Player.LocalPlayer.PlayerController.TeleportPlayer(pos + Vector3.up * 5);
            inVoteMode = true; 
        }


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

            GameObject MyTestButton = voteButtonBundle.LoadAsset<GameObject>("Button");
            GameObject MyVoteRoom = voteButtonBundle.LoadAsset<GameObject>("VoteRoom");
            GameObject MyVotePositions = voteButtonBundle.LoadAsset<GameObject>("VotePositions");
            

            GameObject buttonInstance = UnityEngine.GameObject.Instantiate(MyTestButton, message, Quaternion.identity);
           
            buttonInstance.AddComponent<ButtonBehavior>();

            UnityEngine.GameObject.Instantiate(MyVoteRoom, message + Vector3.up * 100, Quaternion.identity);
            
            GameObject votePositions =  UnityEngine.GameObject.Instantiate(MyVotePositions, message + Vector3.up * 100, Quaternion.identity);
            

            VotePositions = votePositions.AddComponent<VotePositions>();

            //need sync on clients connections 



        }


    }
}
