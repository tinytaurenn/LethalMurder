using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration; 

namespace LethalMurder
{
    internal class ModConfig
    {
        

        public readonly ConfigEntry<float> killCooldown;
        public readonly ConfigEntry<float> voteCallCooldown;
        public readonly ConfigEntry<float> voteTime;
        public readonly ConfigEntry<float> timeAfterVote;

        public ModConfig(ConfigFile configFile)
        {
            killCooldown = configFile.Bind(
                "Gameplay",
                "KillCooldown",
                100f,
                "The cooldown between each kill in seconds"
            );
            voteCallCooldown = configFile.Bind(
                "Gameplay",
                "VoteCallCooldown",
                160f,
                "The cooldown between each vote call in seconds"
            );
            voteTime = configFile.Bind(
                "Gameplay",
                "VoteTime",
                60f,
                "The time for voting in seconds"
            );
            timeAfterVote = configFile.Bind(
                "Gameplay",
                "TimeAfterVote",
                7f,
                "The time after voting in seconds"
            );


        }
    }
}
