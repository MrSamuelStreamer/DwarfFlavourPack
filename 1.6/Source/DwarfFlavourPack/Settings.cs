using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

public class Settings : ModSettings
{
  public float DefaultTilesPerHour = 3f;
  public float ResearchedTilesPerHour = 12f;

  public void DoWindowContents(Rect wrect)
  {
    Listing_Standard options = new();
    options.Begin(wrect);

    DefaultTilesPerHour = options.SliderLabeled("DwarfFlavourPack_Settings_TilesPerHour".Translate(DefaultTilesPerHour), DefaultTilesPerHour, 0f, 100f);
    options.Gap();

    ResearchedTilesPerHour = options.SliderLabeled("DwarfFlavourPack_Settings_ResearchedTilesPerHour".Translate(ResearchedTilesPerHour), ResearchedTilesPerHour, 0f, 100f);
    options.Gap();

    options.End();
  }

  public override void ExposeData()
  {
    Scribe_Values.Look(ref DefaultTilesPerHour, "DefaultTilesPerHour", 3);
    Scribe_Values.Look(ref ResearchedTilesPerHour, "ResearchedTilesPerHour", 12);
  }
}