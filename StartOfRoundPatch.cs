using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalMurder
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("ReviveDeadPlayers")]
        [HarmonyPostfix]
        static void ReviveDeadPlayersPatch()
        {
            //here the future award for winning crewmates or impostors

        }

    }
}
