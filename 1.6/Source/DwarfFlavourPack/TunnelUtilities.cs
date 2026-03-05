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

  public static bool HasJobOnTunnel(Pawn pawn, Building_Tunnel tunnel)
  {
    return tunnel != null && tunnel.Spawned && !tunnel.leftToLoad.NullOrEmpty() && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && pawn.CanReach((LocalTargetInfo) (Thing) tunnel, PathEndMode.Touch, Danger.Deadly) && FindThingToLoad(pawn, tunnel).Thing != null;
  }

  public static Job JobOnTunnel(Pawn p, Building_Tunnel tunnel)
  {
    ThingCount thingCount = FindThingToLoad(p, tunnel);
    if (thingCount.Thing is Pawn targetPawn && (targetPawn.Downed || targetPawn.IsSelfShutdown()))
    {
      Job job = JobMaker.MakeJob(DwarfFlavourPackDefOf.DFP_CarryDownedPawnToPortal, tunnel, targetPawn);
      job.count = 1;
      return job;
    }
    else
    {
      Job job = JobMaker.MakeJob(DwarfFlavourPackDefOf.DFP_HaulToTunnel, LocalTargetInfo.Invalid, (LocalTargetInfo) (Thing) tunnel);
      job.ignoreForbidden = true;
      return job;
    }
  }

  private static List<TransferableOneWay> tmpHungryTransferables = new List<TransferableOneWay>();
  private static Dictionary<TransferableOneWay, int> tmpAlreadyLoading = new Dictionary<TransferableOneWay, int>();

  public static ThingCount FindThingToLoad(Pawn p, Building_Tunnel tunnel)
  {
    if (tunnel == null || tunnel.Map == null)
    {
      return new ThingCount();
    }
    List<TransferableOneWay> leftToLoad = tunnel.leftToLoad;
    if (leftToLoad.NullOrEmpty())
    {
      return new ThingCount();
    }

    tmpAlreadyLoading.Clear();
    List<Pawn> pawnList = tunnel.Map.mapPawns.PawnsInFaction(Faction.OfPlayer);
    for (int index = 0; index < pawnList.Count; ++index)
    {
      if (pawnList[index] != p && pawnList[index].CurJobDef == DwarfFlavourPackDefOf.DFP_HaulToTunnel)
      {
        JobDriver_HaulToTunnel curDriver = pawnList[index].jobs?.curDriver as JobDriver_HaulToTunnel;
        if (curDriver != null && curDriver.Container == tunnel)
        {
          // Identify which transferable is being satisfied by this other pawn.
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

    tmpHungryTransferables.Clear();
    for (int i = 0; i < leftToLoad.Count; i++)
    {
      int targetedCount = 0;
      tmpAlreadyLoading.TryGetValue(leftToLoad[i], out targetedCount);
      if (leftToLoad[i].CountToTransfer - targetedCount > 0)
      {
        tmpHungryTransferables.Add(leftToLoad[i]);
      }
    }

    if (tmpHungryTransferables.Count == 0)
    {
      tmpAlreadyLoading.Clear();
      return new ThingCount();
    }

    // 1. Find closest item that matches a hungry transferable.
    // Matching by type (TransferableMatchingDesperate) ensures we find items even if they've been
    // split or merged since the loadout was finalized, or if they were recently un-forbidden.
    Thing thing = GenClosest.ClosestThingReachable(p.Position, p.Map,
      ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
      PathEndMode.Touch,
      TraverseParms.For(p),
      validator: x =>
      {
        if (x.IsForbidden(p)) return false;
        if (!p.CanReserve(x)) return false;
        if (p.carryTracker.AvailableStackSpace(x.def) <= 0) return false;

        return TransferableUtility.TransferableMatchingDesperate(x, tmpHungryTransferables, TransferAsOneMode.PodsOrCaravanPacking) != null;
      },
      lookInHaulSources: true);

    if (thing != null)
    {
      TransferableOneWay match = TransferableUtility.TransferableMatchingDesperate(thing, tmpHungryTransferables, TransferAsOneMode.PodsOrCaravanPacking);
      int targetedCount = 0;
      tmpAlreadyLoading.TryGetValue(match, out targetedCount);
      int countToTake = Mathf.Min(match.CountToTransfer - targetedCount, thing.stackCount);

      tmpAlreadyLoading.Clear();
      tmpHungryTransferables.Clear();
      return new ThingCount(thing, countToTake);
    }

    // 2. Search for specifically requested pawns (like downed animals or shutdown mechs)
    for (int i = 0; i < tmpHungryTransferables.Count; i++)
    {
      TransferableOneWay tow = tmpHungryTransferables[i];
      if (tow.ThingDef.category == ThingCategory.Pawn)
      {
        for (int j = 0; j < tow.things.Count; j++)
        {
          if (tow.things[j] is Pawn targetPawn && (!targetPawn.IsColonist && !targetPawn.IsColonyMech || targetPawn.Downed || targetPawn.IsSelfShutdown()) && !targetPawn.inventory.UnloadEverything)
          {
            if (p.CanReserveAndReach(targetPawn, PathEndMode.Touch, Danger.Deadly))
            {
              tmpAlreadyLoading.Clear();
              tmpHungryTransferables.Clear();
              return new ThingCount(targetPawn, 1);
            }
          }
        }
      }
    }

    tmpAlreadyLoading.Clear();
    tmpHungryTransferables.Clear();
    return new ThingCount();
  }


  public static IEnumerable<Thing> ThingsBeingHauledTo(Building_Tunnel tunnel)
  {
    if (tunnel == null || tunnel.Map == null)
      yield break;
    IReadOnlyList<Pawn> pawns = tunnel.Map.mapPawns.AllPawnsSpawned;
    foreach (var t in pawns)
    {
      if (t.CurJobDef == DwarfFlavourPackDefOf.DFP_HaulToTunnel && t.jobs?.curDriver is JobDriver_HaulToTunnel curDriver && curDriver.Tunnel == tunnel && t.carryTracker.CarriedThing != null)
        yield return t.carryTracker.CarriedThing;
    }
  }

  public static bool AnyPawnCanLoadAnythingNow(Building_Tunnel tunnel)
  {
    if (tunnel == null || !tunnel.LoadInProgress || !tunnel.Spawned || tunnel.Map == null)
      return false;
    IReadOnlyList<Pawn> allPawnsSpawned = tunnel.Map.mapPawns.AllPawnsSpawned;
    for (int index = 0; index < allPawnsSpawned.Count; ++index)
    {
      Pawn p = allPawnsSpawned[index];
      if (p.CurJobDef == DwarfFlavourPackDefOf.DFP_HaulToTunnel && p.jobs?.curDriver is JobDriver_HaulToTunnel haulDriver && haulDriver.Tunnel == tunnel || p.CurJobDef == DwarfFlavourPackDefOf.DFP_CarryDownedPawnToPortal && p.jobs?.curDriver is JobDriver_EnterTunnel enterDriver && enterDriver.Tunnel == tunnel)
        return true;
    }
    for (int index = 0; index < allPawnsSpawned.Count; ++index)
    {
      Thing thing = allPawnsSpawned[index].mindState?.duty?.focus.Thing;
      if (thing != null && thing == tunnel && allPawnsSpawned[index].CanReach((LocalTargetInfo) thing, PathEndMode.Touch, Danger.Deadly))
      {
        if (allPawnsSpawned[index].IsColonist && HasJobOnTunnel(allPawnsSpawned[index], tunnel))
          return true;
      }
    }
    return false;
  }

  public static bool AnyPawnCouldLoadAnything(Building_Tunnel tunnel, bool includeForbidden)
  {
    if (tunnel == null || !tunnel.LoadInProgress || !tunnel.Spawned || tunnel.Map == null)
      return false;
    IReadOnlyList<Pawn> allPawnsSpawned = tunnel.Map.mapPawns.AllPawnsSpawned;
    for (int index = 0; index < allPawnsSpawned.Count; ++index)
    {
      Pawn p = allPawnsSpawned[index];
      if (p.CurJobDef == DwarfFlavourPackDefOf.DFP_HaulToTunnel && p.jobs?.curDriver is JobDriver_HaulToTunnel haulDriver && haulDriver.Tunnel == tunnel || p.CurJobDef == DwarfFlavourPackDefOf.DFP_CarryDownedPawnToPortal && p.jobs?.curDriver is JobDriver_EnterTunnel enterDriver && enterDriver.Tunnel == tunnel)
        return true;
    }
    for (int index = 0; index < allPawnsSpawned.Count; ++index)
    {
      Pawn p = allPawnsSpawned[index];
      Thing focusThing = p.mindState?.duty?.focus.Thing;
      if (focusThing != null && focusThing == tunnel && p.CanReach((LocalTargetInfo) focusThing, PathEndMode.Touch, Danger.Deadly))
      {
        if (p.IsColonist && p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
          if (includeForbidden)
          {
            // If we include forbidden, just check if there's *any* reachable thing matching the loadout, ignoring its forbidden status.
            // We reuse the logic from FindThingToLoad but with a custom validator that ignores forbidden.
            if (CanFindAnyThingToLoad(p, tunnel, includeForbidden: true))
              return true;
          }
          else
          {
            if (HasJobOnTunnel(p, tunnel))
              return true;
          }
        }
      }
    }
    return false;
  }

  private static bool CanFindAnyThingToLoad(Pawn p, Building_Tunnel tunnel, bool includeForbidden)
  {
    if (tunnel == null || tunnel.Map == null) return false;
    List<TransferableOneWay> leftToLoad = tunnel.leftToLoad;
    if (leftToLoad.NullOrEmpty()) return false;

    tmpAlreadyLoading.Clear();
    List<Pawn> pawnList = tunnel.Map.mapPawns.PawnsInFaction(Faction.OfPlayer);
    for (int index = 0; index < pawnList.Count; ++index)
    {
      if (pawnList[index] != p && pawnList[index].CurJobDef == DwarfFlavourPackDefOf.DFP_HaulToTunnel)
      {
        JobDriver_HaulToTunnel curDriver = pawnList[index].jobs?.curDriver as JobDriver_HaulToTunnel;
        if (curDriver != null && curDriver.Container == tunnel)
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

    tmpHungryTransferables.Clear();
    for (int i = 0; i < leftToLoad.Count; i++)
    {
      int targetedCount = 0;
      tmpAlreadyLoading.TryGetValue(leftToLoad[i], out targetedCount);
      if (leftToLoad[i].CountToTransfer - targetedCount > 0)
        tmpHungryTransferables.Add(leftToLoad[i]);
    }

    if (tmpHungryTransferables.Count == 0)
    {
      tmpAlreadyLoading.Clear();
      return false;
    }

    // 1. Find closest item that matches a hungry transferable.
    Thing thing = GenClosest.ClosestThingReachable(p.Position, p.Map,
      ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
      PathEndMode.Touch,
      TraverseParms.For(p),
      validator: x =>
      {
        if (!includeForbidden && x.IsForbidden(p)) return false;
        if (!p.CanReserve(x)) return false;
        if (p.carryTracker.AvailableStackSpace(x.def) <= 0) return false;

        return TransferableUtility.TransferableMatchingDesperate(x, tmpHungryTransferables, TransferAsOneMode.PodsOrCaravanPacking) != null;
      },
      lookInHaulSources: true);

    if (thing != null)
    {
      tmpAlreadyLoading.Clear();
      tmpHungryTransferables.Clear();
      return true;
    }

    // 2. Search for specifically requested pawns (like downed animals or shutdown mechs)
    for (int i = 0; i < tmpHungryTransferables.Count; i++)
    {
      TransferableOneWay tow = tmpHungryTransferables[i];
      if (tow.ThingDef.category == ThingCategory.Pawn)
      {
        for (int j = 0; j < tow.things.Count; j++)
        {
          if (tow.things[j] is Pawn targetPawn && (!targetPawn.IsColonist && !targetPawn.IsColonyMech || targetPawn.Downed || targetPawn.IsSelfShutdown()) && !targetPawn.inventory.UnloadEverything)
          {
            if (p.CanReserveAndReach(targetPawn, PathEndMode.Touch, Danger.Deadly))
            {
              tmpAlreadyLoading.Clear();
              tmpHungryTransferables.Clear();
              return true;
            }
          }
        }
      }
    }

    tmpAlreadyLoading.Clear();
    tmpHungryTransferables.Clear();
    return false;
  }

  public static void MakeLordsAsAppropriate(List<Pawn> pawns, Building_Tunnel tunnel)
  {
    Lord lord = null;
    List<Pawn> source = pawns.Where(x =>
    {
      if (!((x.IsColonist || x.IsColonyMechPlayerControlled) && !x.Downed))
        return false;

      return x.needs?.energy?.IsSelfShutdown != true && x.Spawned;
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