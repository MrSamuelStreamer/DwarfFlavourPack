using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DwarfFlavourPack;

public class FloatMenuOptionProvider_EnterTunnel : FloatMenuOptionProvider
{
  private static List<Pawn> tmpTunnelEnteringPawns = new List<Pawn>();

  protected override bool Drafted => true;

  protected override bool Undrafted => true;

  protected override bool Multiselect => true;

  protected override bool MechanoidCanDo => true;

  protected override FloatMenuOption GetSingleOptionFor(
    Thing clickedThing,
    FloatMenuContext context)
  {
    Building_Tunnel tunnel = clickedThing as Building_Tunnel;
    if (tunnel == null)
      return null;
    string reason;
    if (!tunnel.IsEnterable(out reason))
      return new FloatMenuOption("CannotEnterPortal".Translate((NamedArgument) tunnel.Label) + ": " + reason, null);
    if (!context.IsMultiselect)
    {
      AcceptanceReport acceptanceReport = CanEnterTunnel(context.FirstSelectedPawn, tunnel);
      if (!acceptanceReport.Accepted)
        return new FloatMenuOption("CannotEnterPortal".Translate((NamedArgument) tunnel.Label) + ": " + acceptanceReport.Reason, null);
    }
    tmpTunnelEnteringPawns.Clear();
    foreach (Pawn validSelectedPawn in context.ValidSelectedPawns)
    {
      if (CanEnterTunnel(context.FirstSelectedPawn, tunnel))
        tmpTunnelEnteringPawns.Add(validSelectedPawn);
    }
    return tmpTunnelEnteringPawns.NullOrEmpty() ? null : new FloatMenuOption(tunnel.EnterString, (Action) (() =>
    {
      foreach (Pawn tunnelEnteringPawn in tmpTunnelEnteringPawns)
      {
        Job job = JobMaker.MakeJob(DwarfFlavourPackDefOf.DFP_EnterTunnel, (LocalTargetInfo) (Thing) tunnel);
        job.playerForced = true;
        tunnelEnteringPawn.jobs.TryTakeOrderedJob(job);
      }
    }), MenuOptionPriority.High);
  }

  private static AcceptanceReport CanEnterTunnel(Pawn pawn, Building_Tunnel tunnel)
  {
    if (!pawn.CanReach((LocalTargetInfo) (Thing) tunnel, PathEndMode.ClosestTouch, Danger.Deadly))
      return "NoPath".Translate();
    return !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ? "Incapable".Translate() : true;
  }
}