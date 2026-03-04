using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack.HarmonyPatches;

[HarmonyPatch(typeof(WorldInterface))]
public static class WorldInterface_Patch
{
    [HarmonyPatch(nameof(WorldInterface.WorldInterfaceOnGUI))]
    [HarmonyPostfix]
    public static void WorldInterfaceOnGUIPostfix()
    {
        if (!WorldRendererUtility.WorldSelected || !Find.WorldCamera.gameObject.activeInHierarchy)
            return;
        if (Event.current.type == EventType.Layout)
            return;
        float leftX = UI.screenWidth - 420f;
        float curBaseY = 200f;
     
        Rect rect = new Rect(leftX, curBaseY, 400, 24);
        GameFont orig = Text.Font;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleRight;
        
        foreach (TunnelCaravan caravan in TunnelGenData.Instance.Caravans.InnerListForReading.Where(c => !c.Done && !c.MapGenerating))
        {
            Widgets.Label(rect, caravan.Progress);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                // TooltipHandler.TipRegion(rect, new TipSignal("Memory usage may not be accurate in optimized builds.", 5670913));
            }

            curBaseY += 28;
        }

        Text.Anchor = TextAnchor.UpperLeft;   
        Text.Font = orig;
    }
}