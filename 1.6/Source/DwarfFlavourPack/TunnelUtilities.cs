using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DwarfFlavourPack;

public static class TunnelUtilities
{
  
  public static bool HasJobOnTunnel(Pawn pawn, Building_Tunnel portal)
  {
    return portal != null && !portal.leftToLoad.NullOrEmpty() && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && pawn.CanReach((LocalTargetInfo) (Thing) portal, PathEndMode.Touch, pawn.NormalMaxDanger()) && FindThingToLoad(pawn, portal).Thing != null;
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
    if (!neededThings.Any<Thing>())
    {
      tmpAlreadyLoading.Clear();
      return new ThingCount();
    }
    Thing thing = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(p), validator: (Predicate<Thing>) (x => neededThings.Contains(x) && p.CanReserve((LocalTargetInfo) x) && !x.IsForbidden(p) && p.carryTracker.AvailableStackSpace(x.def) > 0), lookInHaulSources: true);
    if (thing == null)
    {
      foreach (Thing neededThing in neededThings)
      {
        if (neededThing is Pawn pawn && (!pawn.IsColonist && !pawn.IsColonyMech || pawn.Downed || pawn.IsSelfShutdown()) && !pawn.inventory.UnloadEverything && p.CanReserveAndReach((LocalTargetInfo) (Thing) pawn, PathEndMode.Touch, Danger.Deadly))
        {
          neededThings.Clear();
          tmpAlreadyLoading.Clear();
          return new ThingCount((Thing) pawn, 1);
        }
      }
    }
    neededThings.Clear();
    if (thing != null)
    {
      TransferableOneWay key = (TransferableOneWay) null;
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
    
}