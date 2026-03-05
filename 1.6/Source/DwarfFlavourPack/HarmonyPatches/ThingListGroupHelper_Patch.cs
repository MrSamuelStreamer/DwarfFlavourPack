using HarmonyLib;
using Verse;

namespace DwarfFlavourPack.HarmonyPatches;

[HarmonyPatch(typeof(ThingListGroupHelper))]
public static class ThingListGroupHelper_Patch
{
  [HarmonyPatch(nameof(ThingListGroupHelper.Includes))]
  [HarmonyPostfix]
  public static void Includes_Patch(ThingRequestGroup group, ThingDef def, ref bool __result)
  {
    if (group != ThingRequestGroup.MapPortal) return;

    if (typeof(Building_Tunnel).IsAssignableFrom(def.thingClass))
    {
      __result = true;
    }
  }
}