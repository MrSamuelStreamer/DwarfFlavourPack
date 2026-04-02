using System.Collections.Generic;
using EndGame;
using HarmonyLib;
using Verse;

namespace DwarfFlavourPack.Endgame;

[HarmonyPatch(typeof(CompEndGame))]
public static class CompEndGame_Patch
{
    [HarmonyPatch(nameof(CompEndGame.CompGetGizmosExtra))]
    [HarmonyPostfix]
    public static IEnumerable<Gizmo> CompGetGizmosExtra_Postfix(IEnumerable<Gizmo> results)
    {
        foreach (Gizmo gizmo in results)
        {
            if (gizmo is Command_Action cmd &&
                cmd.defaultLabel == "CommandStartEndGame".Translate())
                continue;

            yield return gizmo;
        }
    }
}