using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

public class Settings : ModSettings
{
  public float DefaultTilesPerHour = 3f;
  public float ResearchedTilesPerHour = 12f;
  public float MechFormingSpeedBaseValue = 1f;

  public void DoWindowContents(Rect wrect)
  {
    Listing_Standard options = new();
    options.Begin(wrect);

    DefaultTilesPerHour = options.SliderLabeled("DwarfFlavourPack_Settings_TilesPerHour".Translate(DefaultTilesPerHour), DefaultTilesPerHour, 0f, 100f);
    options.Gap();

    ResearchedTilesPerHour = options.SliderLabeled("DwarfFlavourPack_Settings_ResearchedTilesPerHour".Translate(ResearchedTilesPerHour), ResearchedTilesPerHour, 0f, 100f);
    options.Gap();

    MechFormingSpeedBaseValue = options.SliderLabeled("MSSFP_MechGestationTime".Translate(MechFormingSpeedBaseValue.ToString("0.0")), MechFormingSpeedBaseValue, 0.1f, 5f);
    options.Label($"Base gestation cycle time: {48f * MechFormingSpeedBaseValue:0.#}h");
    options.Gap();
    
    options.End();
  }

  public override void ExposeData()
  {
    Scribe_Values.Look(ref DefaultTilesPerHour, "DefaultTilesPerHour", 3);
    Scribe_Values.Look(ref ResearchedTilesPerHour, "ResearchedTilesPerHour", 12);
    Scribe_Values.Look(ref MechFormingSpeedBaseValue, "MechFormingSpeedBaseValue", 1f);
  }
}