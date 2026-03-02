using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace DwarfFlavourPack;

public static class TunnelUtilities
{
  
  public static bool HasJobOnTunnel(Pawn pawn, Building_Tunnel portal)
  {
    return portal != null && !portal.leftToLoad.NullOrEmpty() && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && pawn.CanReach((LocalTargetInfo) (Thing) portal, PathEndMode.Touch, pawn.NormalMaxDanger()) && FindThingToLoad(pawn, portal).Thing != null;
  }
  
  public static Job JobOnTunnel(Pawn p, Building_Tunnel portal)
  {
    Job job = JobMaker.MakeJob(DwarfFlavourPackDefOf.DFP_HaulToTunnel, LocalTargetInfo.Invalid, (LocalTargetInfo) (Thing) portal);
    job.ignoreForbidden = true;
    return job;
  }
  
  private static HashSet<Thing> neededThings = new HashSet<Thing>();
  private static Dictionary<TransferableOneWay, int> tmpAlreadyLoading = new Dictionary<TransferableOneWay, int>();
  
  public static ThingCount FindThingToLoad(Pawn p, Building_Tunnel portal)
  {
    neededThings.Clear();
    List<TransferableOneWay> leftToLoad = portal.leftToLoad;
    tmpAlreadyLoading.Clear();
    if (leftToLoad != null)
    {
      List<Pawn> pawnList = portal.Map.mapPawns.PawnsInFaction(Faction.OfPlayer);
      for (int index = 0; index < pawnList.Count; ++index)
      {
        if (pawnList[index] != p && pawnList[index].CurJobDef == JobDefOf.HaulToPortal)
        {
          JobDriver_HaulToPortal curDriver = (JobDriver_HaulToPortal) pawnList[index].jobs.curDriver;
          if (curDriver.Container == portal)
          {
            TransferableOneWay key = TransferableUtility.TransferableMatchingDesperate(curDriver.ThingToCarry, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
            if (key != null)
            {
              int num = 0;
              if (tmpAlreadyLoading.TryGetValue(key, out num))
                tmpAlreadyLoading[key] = num + curDriver.initialCount;
              else
                tmpAlreadyLoading.Add(key, curDriver.initialCount);
            }
          }
        }
      }
      for (int index1 = 0; index1 < leftToLoad.Count; ++index1)
      {
        TransferableOneWay transferableOneWay = leftToLoad[index1];
        int num;
        if (!tmpAlreadyLoading.TryGetValue(leftToLoad[index1], out num))
          num = 0;
        if (transferableOneWay.CountToTransfer - num > 0)
        {
          for (int index2 = 0; index2 < transferableOneWay.things.Count; ++index2)
            neededThings.Add(transferableOneWay.things[index2]);
        }
      }
    }
    if (!neededThings.Any())
    {
      tmpAlreadyLoading.Clear();
      return new ThingCount();
    }
    Thing thing = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(p), validator: x => neededThings.Contains(x) && p.CanReserve((LocalTargetInfo) x) && !x.IsForbidden(p) && p.carryTracker.AvailableStackSpace(x.def) > 0, lookInHaulSources: true);
    if (thing == null)
    {
      foreach (Thing neededThing in neededThings)
      {
        if (neededThing is Pawn pawn && (!pawn.IsColonist && !pawn.IsColonyMech || pawn.Downed || pawn.IsSelfShutdown()) && !pawn.inventory.UnloadEverything && p.CanReserveAndReach((LocalTargetInfo) (Thing) pawn, PathEndMode.Touch, Danger.Deadly))
        {
          neededThings.Clear();
          tmpAlreadyLoading.Clear();
          return new ThingCount(pawn, 1);
        }
      }
    }
    neededThings.Clear();
    if (thing != null)
    {
      TransferableOneWay key = null;
      for (int index = 0; index < leftToLoad.Count; ++index)
      {
        if (leftToLoad[index].things.Contains(thing))
        {
          key = leftToLoad[index];
          break;
        }
      }
      int num;
      if (!tmpAlreadyLoading.TryGetValue(key, out num))
        num = 0;
      tmpAlreadyLoading.Clear();
      return new ThingCount(thing, Mathf.Min(key.CountToTransfer - num, thing.stackCount));
    }
    tmpAlreadyLoading.Clear();
    return new ThingCount();
  }
    

  public static IEnumerable<Thing> ThingsBeingHauledTo(Building_Tunnel tunnel)
  {
    IReadOnlyList<Pawn> pawns = tunnel.Map.mapPawns.AllPawnsSpawned;
    foreach (var t in pawns)
    {
      if (t.CurJobDef == JobDefOf.HaulToPortal && ((JobDriver_HaulToTunnel) t.jobs.curDriver).Tunnel == tunnel && t.carryTracker.CarriedThing != null)
        yield return t.carryTracker.CarriedThing;
    }
  }

  public static void MakeLordsAsAppropriate(List<Pawn> pawns, Building_Tunnel tunnel)
  {
    Lord lord = null;
    List<Pawn> source = pawns.Where(x =>
    {
      if ((x.IsColonist || x.IsColonyMechPlayerControlled) && !x.Downed)
      {
        if (x.needs == null || (x.needs.energy?.IsSelfShutdown ?? false))
        {
          return x.Spawned;
        }
      }
      return false;
    }).ToList();
    if (source.Any())
    {
      lord = tunnel.Map.lordManager.lords.Find(x => x.LordJob is LordJob_LoadAndEnterTunnel enterTunnel && enterTunnel.tunnel == tunnel) ?? LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_LoadAndEnterTunnel(tunnel), tunnel.Map);
      foreach (Pawn pawn in source)
      {
        if (!lord.ownedPawns.Contains(pawn))
        {
          pawn.GetLord()?.Notify_PawnLost(pawn, PawnLostCondition.ForcedToJoinOtherLord);
          lord.AddPawn(pawn);
          pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
      }
      for (int index = lord.ownedPawns.Count - 1; index >= 0; --index)
      {
        if (!source.Contains(lord.ownedPawns[index]))
          lord.Notify_PawnLost(lord.ownedPawns[index], PawnLostCondition.LordRejected);
      }
    }
    for (int index = tunnel.Map.lordManager.lords.Count - 1; index >= 0; --index)
    {
      if (tunnel.Map.lordManager.lords[index].LordJob is LordJob_LoadAndEnterTunnel lordJob && lordJob.tunnel == tunnel && tunnel.Map.lordManager.lords[index] != lord)
        tunnel.Map.lordManager.RemoveLord(tunnel.Map.lordManager.lords[index]);
    }
  }
}