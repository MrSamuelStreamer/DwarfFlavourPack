using System.Collections.Generic;
using EndGame;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DwarfFlavourPack.Endgame;

[HarmonyPatch(typeof(CompEndGame), nameof(CompEndGame.CompGetGizmosExtra))]
public static class CompEndGame_CompGetGizmosExtra_Patch
{
    [HarmonyPostfix]
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

[HarmonyPatch(typeof(ThingComp), nameof(ThingComp.CompFloatMenuOptions))]
public static class CompEndGame_CompFloatMenuOptions_Patch
{
    [HarmonyPostfix]
    public static IEnumerable<FloatMenuOption> Postfix(
        IEnumerable<FloatMenuOption> results,
        ThingComp __instance,
        Pawn selPawn)
    {
        if (__instance is not CompEndGame endGame)
        {
            foreach (FloatMenuOption opt in results)
                yield return opt;
            yield break;
        }

        foreach (FloatMenuOption opt in results)
            yield return opt;

        if (!endGame.IsActivatingPossible)
        {
            yield return new FloatMenuOption(
                "CommandStartEndGame".Translate() + " (" + "CommandStartEndGame_DisabledReason".Translate() + ")",
                null);
            yield break;
        }

        if (selPawn.CanReach(endGame.parent, PathEndMode.InteractionCell, Danger.Deadly))
        {
            yield return new FloatMenuOption(
                "CommandStartEndGame".Translate(),
                () =>
                {
                    Job job = JobMaker.MakeJob(
                        DFPEndgameDefOf.DFP_ActivateHeartChamber,
                        endGame.parent);
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