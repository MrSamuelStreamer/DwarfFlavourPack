using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DwarfFlavourPack.HarmonyPatches;

[HarmonyPatch]
public static class GameOver_Patch
{
  [HarmonyPatch(typeof(GameEnder), nameof(GameEnder.CheckOrUpdateGameOver))]
  [HarmonyPrefix]
  public static bool CheckOrUpdateGameOver_Prefix(GameEnder __instance)
  {
    // If we find any player pawn in a tunnel or tunnel caravan,
    // we prevent GameEnder from continuing its check, which would
    // trigger the "no pawns left" game over if no other pawns were on maps.

    if (AnyPlayerPawnInTunnels())
    {
      // Reset the gameEnding state if it was somehow set,
      // or just prevent it from being set now.
      __instance.gameEnding = false;
      return false; // Skip original method
    }

    return true; // Continue with original check
  }

  private static bool AnyPlayerPawnInTunnels()
  {
    // 1. Check TunnelCaravans (WorldObjects)
    foreach (TunnelCaravan tunnelCaravan in Find.WorldObjects.AllWorldObjects.OfType<TunnelCaravan>())
    {
      if (tunnelCaravan.PawnsListForReading.Any(p => p.Faction == Faction.OfPlayer))
      {
        return true;
      }
    }

    // 2. Check Building_Tunnels on all maps (for pawns currently loading/waiting)
    foreach (Map map in Find.Maps)
    {
      foreach (Building_Tunnel thing in map.listerThings.AllThings.OfType<Building_Tunnel>())
      {
        if (thing.innerContainer.OfType<Pawn>().Any(p => p.Faction == Faction.OfPlayer))
        {
          return true;
        }
      }
    }

    return false;
  }

  [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval), MethodType.Getter)]
  [HarmonyPostfix]
  public static void AnyPawnBlockingMapRemoval_Postfix(MapPawns __instance, ref bool __result)
  {
    if (__result) return;

    Map map = Find.Maps.FirstOrDefault(m => m.mapPawns == __instance);
    if (map == null) return;

    foreach (Building_Tunnel thing in map.listerThings.AllThings.OfType<Building_Tunnel>())
    {
      if (thing.innerContainer.OfType<Pawn>().Any(p => p.Faction == Faction.OfPlayer))
      {
        __result = true;
        return;
      }
    }
  }
}