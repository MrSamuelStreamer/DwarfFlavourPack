using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack.HarmonyPatches;

[HarmonyPatch(typeof(InspectPaneFiller))]
public static class InspectPaneFiller_Patch
{
    public static WorldObject worldObject;
    [HarmonyPatch(nameof(InspectPaneFiller.DrawInspectStringFor))]
    [HarmonyPostfix]
    public static void DrawInspectStringFor_Patch(InspectPaneFiller __instance, ISelectable sel, Rect rect)
    {
        if (sel is WorldObject wo)
        {
            worldObject = wo;
        }
    }

    [HarmonyPatch(nameof(InspectPaneFiller.DrawInspectString))]
    [HarmonyPrefix]
    public static void DrawInspectStringFor_Prefix(InspectPaneFiller __instance, ref string str, ref Rect rect)
    {
        if (worldObject != null)
        {
            str += $"/nElevation: {worldObject.Tile.Tile.elevation}";
            str += $"/nHilliness: {worldObject.Tile.Tile.hilliness}";
            str += $"/nTemperature: {worldObject.Tile.Tile.temperature}";
            worldObject = null;
        }
    }
}