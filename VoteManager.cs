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
        internal LNetworkMessage<int> voteCallMessage; 
        internal LNetworkMessage<Vector3> ButtonCreationMessage;

        public static AssetBundle voteButtonBundle;

        internal bool buttonFound = false;


        void Awake()
        {
           voteCallMessage = LNetworkMessage<int>.Create("VoteCall");
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
                VoteCallServerReceived(1, LC_API.GameInterfaceAPI.Features.Player.HostPlayer.ClientId); 
            }
            else
            {
                voteCallMessage.SendServer(1);
            }

        }

        void VoteCallServerReceived(int message, ulong clientID)
        {
            voteCallMessage.SendClients(1);
        }

        void VoteCallClientReceived(int message)
        {
            UnityEngine.Debug.Log("message received : " + message);
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
            

            GameObject buttonInstance = UnityEngine.GameObject.Instantiate(MyTestButton, message, Quaternion.identity);
           
            buttonInstance.AddComponent<ButtonBehavior>();

            //need sync on clients connections 

            
            
        }


    }
}
