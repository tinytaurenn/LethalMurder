using LethalNetworkAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalMurder
{
    internal class VoteManager : MonoBehaviour
    {
        internal LNetworkMessage<int> voteCallMessage; 


        void Awake()
        {
           voteCallMessage = LNetworkMessage<int>.Create("VoteCall");
        }



        void OnEnable()
        {
            voteCallMessage.OnServerReceived += VoteCallServerReceived;
            voteCallMessage.OnClientReceived += VoteCallClientReceived;

        }

        void OnDisable()
        {
            voteCallMessage.OnServerReceived -= VoteCallServerReceived;
            voteCallMessage.OnClientReceived -= VoteCallClientReceived;
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


    }
}
