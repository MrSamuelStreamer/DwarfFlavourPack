using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

public class Settings : ModSettings
{
    //Use Mod.settings.setting to refer to this setting.
    public bool setting = true;
    public float TilesPerHour = 10f;

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);
        
        TilesPerHour = options.SliderLabeled("DwarfFlavourPack_Settings_TilesPerHour".Translate(TilesPerHour),  TilesPerHour, 0f, 100f);
        options.Gap();

        options.End();
    }
    
    public override void ExposeData()
    {
        Scribe_Values.Look(ref TilesPerHour, "TilesPerHour", 10f);
    }
}
