using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalMurder
{
    internal class VotePositions : MonoBehaviour
    {

        internal struct FVotePosition
        {
            internal Transform transform;
            internal bool isOccupied;
            internal int voteCount;

            internal FVotePosition(Transform transform)
            {
                this.transform = transform;
                isOccupied = false;
                voteCount = 0;
            }

            internal FVotePosition(Transform transform, bool isOccupied, int voteCount)
            {
                this.transform = transform;
                this.isOccupied = isOccupied;
                this.voteCount = voteCount;
            }
        }

        List<FVotePosition> positionsList = new List<FVotePosition>();

        void Awake()
        {
           
           for (int i = 0; i < transform.childCount; i++)
            {

                positionsList.Add(new FVotePosition(transform.GetChild(i)));
                UnityEngine.Debug.Log("Added transform : " + transform.GetChild(i).name);
            }

            
        }

        internal bool FindEmptyChair(out FVotePosition position)
        {
            List<FVotePosition> shuffledList = positionsList; 
            ModManager.ShuffleList(shuffledList);

            position = shuffledList[0];
            for (int i = 0; i < shuffledList.Count; i++)
            {
                if (!shuffledList[i].isOccupied)
                {
                    position = shuffledList[i];
                    shuffledList[i] = new FVotePosition(shuffledList[i].transform,true, shuffledList[i].voteCount);
                    positionsList = shuffledList;
                    return true; 
                }
            }

            return false;
            
        }

        internal void ClearChairs()
        {
            for (int i = 0; i < transform.childCount; i++)
            {

                positionsList[i] = new FVotePosition(transform.GetChild(i));
                
            }
        }
        


        void OnEnable()
        {
            
        }

        void OnDisable()
        {
            
        }   
    }
}
