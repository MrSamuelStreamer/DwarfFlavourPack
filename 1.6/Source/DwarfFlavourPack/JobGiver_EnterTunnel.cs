using RimWorld;
using Verse;
using Verse.AI;

namespace DwarfFlavourPack;

public class JobGiver_EnterTunnel : ThinkNode_JobGiver
{
  protected override Job TryGiveJob(Pawn pawn)
  {
    Building_Tunnel tunnel = pawn.mindState.duty.focus.Thing as Building_Tunnel;
    if (tunnel == null || tunnel.Map != pawn.Map || !pawn.CanReach(tunnel, PathEndMode.Touch, Danger.Deadly))
    {
      return null;
    }

    // If there are still items to load, wait instead of entering.
    // This ensures pawns don't disappear into the tunnel if something (like a forbidden item) 
    // is still in the loadout but temporarily unhaulable.
    // We wait if ANY pawn could potentially load the remaining items (including if they are forbidden).
    if (tunnel.LoadInProgress && TunnelUtilities.AnyPawnCouldLoadAnything(tunnel, includeForbidden: true))
    {
      return null;
    }

    Job job = JobMaker.MakeJob(DwarfFlavourPackDefOf.DFP_EnterTunnel, tunnel);
    job.locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, LocomotionUrgency.Jog);
    return job;
  }
}