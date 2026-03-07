using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DwarfFlavourPack.HarmonyPatches;

[HarmonyPatch(typeof(Storyteller))]
public static class Storyteller_Patch
{
    [HarmonyPatch(nameof(Storyteller.AllIncidentTargets), MethodType.Getter)]
    [HarmonyPostfix]
    public static void AllIncidentTargets_Postfix(ref List<Thing> __result)
    {
        __result.AddRange(TunnelGenData.Instance.TravellingCaravans);
    }
}