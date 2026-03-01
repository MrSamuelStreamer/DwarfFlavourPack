using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack.HarmonyPatches;
    
[StaticConstructorOnStartup]
[HarmonyPatch(typeof(PlaySettings))]
public static class PlaySettings_Patch
{
    public static readonly Texture2D ToggleTex = ContentFinder<Texture2D>.Get(
        "UI/DFP_TunnelIcon"
    );

    [HarmonyPatch(nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    [HarmonyPostfix]
    public static void DoPlaySettingsGlobalControls_Patch(WidgetRow row, bool worldView)
    {
        if(worldView)
            row.ToggleableIcon(ref WorldDrawLayer_Tunnels.TunnelsVisible, ToggleTex, "Show/Hide tunnels");
    }
}