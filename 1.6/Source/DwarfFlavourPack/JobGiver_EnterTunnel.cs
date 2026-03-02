using RimWorld;
using Verse;
using Verse.AI;

namespace DwarfFlavourPack;

public class JobGiver_EnterTunnel : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        Building_Tunnel tunnel = pawn.mindState.duty.focus.Thing as Building_Tunnel;
        if (tunnel == null || tunnel.Map != pawn.Map || !pawn.CanReach(tunnel, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
        {
            return null;
        }
        Job job = JobMaker.MakeJob(DwarfFlavourPackDefOf.DFP_EnterTunnel, tunnel);
        job.locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, LocomotionUrgency.Jog);
        return job;
    }
}