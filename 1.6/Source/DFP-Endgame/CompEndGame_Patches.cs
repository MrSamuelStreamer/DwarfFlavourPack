using System.Collections.Generic;
using System.Linq;
using EndGame;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DwarfFlavourPack.Endgame;

[HarmonyPatch(typeof(CompEndGame), nameof(CompEndGame.CompGetGizmosExtra))]
public static class CompEndGame_CompGetGizmosExtra_Patch
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> results)
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

[HarmonyPatch(typeof(CompEndGame), nameof(CompEndGame.CompFloatMenuOptions))]
public static class CompEndGame_CompFloatMenuOptions_Patch
{
    public static IEnumerable<FloatMenuOption> Postfix(
        IEnumerable<FloatMenuOption> results,
        CompEndGame __instance,
        Pawn selPawn)
    {
        foreach (FloatMenuOption opt in results)
            yield return opt;

        if (!__instance.IsActivatingPossible)
        {
            yield return new FloatMenuOption(
                "CommandStartEndGame".Translate() + " (" + "CommandStartEndGame_DisabledReason".Translate() + ")",
                null);
            yield break;
        }

        if (selPawn.CanReach(__instance.parent, PathEndMode.InteractionCell, Danger.Deadly))
        {
            yield return new FloatMenuOption(
                "CommandStartEndGame".Translate(),
                () =>
                {
                    Job job = JobMaker.MakeJob(
                        DFPEndgameDefOf.DFP_ActivateHeartChamber,
                        __instance.parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
        }
        else
        {
            yield return new FloatMenuOption(
                "CommandStartEndGame".Translate() + " (" + "CannotReach".Translate() + ")",
                null);
        }
    }
}
