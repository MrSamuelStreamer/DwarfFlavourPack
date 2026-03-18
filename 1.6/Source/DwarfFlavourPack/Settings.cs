using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

public class Settings : ModSettings
{
  public float DefaultTilesPerHour = 3f;
  public float ResearchedTilesPerHour = 12f;
  public float MechFormingSpeedBaseValue = 1f;

  // Step index: 0 = Vanilla (no-op), 1 = 4×, 2 = 8×, 3 = 16×, 4 = Unlimited
  public int QuestSiteRadiusStep = 1;

  // Step index: 0 = Vanilla (no-op), 1 = 4×, 2 = 8×, 3 = 16× (no Unlimited — a min cap of ∞ would be meaningless)
  // Constrained to ≤ QuestSiteRadiusStep so min never exceeds max.
  public int QuestSiteMinRadiusStep = 1;

  private static readonly string[] StepLabels =
  {
    "DwarfFlavourPack_Settings_QuestSiteRadius_Vanilla",
    "DwarfFlavourPack_Settings_QuestSiteRadius_4x",
    "DwarfFlavourPack_Settings_QuestSiteRadius_8x",
    "DwarfFlavourPack_Settings_QuestSiteRadius_16x",
    "DwarfFlavourPack_Settings_QuestSiteRadius_Unlimited",
  };

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

    float step = QuestSiteRadiusStep;
    step = options.SliderLabeled(
      "DwarfFlavourPack_Settings_QuestSiteRadius".Translate(StepLabels[(int)step].Translate()),
      step, 0f, 4f);
    QuestSiteRadiusStep = Mathf.RoundToInt(step);
    options.Label("DwarfFlavourPack_Settings_QuestSiteRadius_Desc".Translate());
    options.Gap();

    // Clamp min step if max was reduced below it.
    int maxStepForMin = Mathf.Min(3, QuestSiteRadiusStep);
    if (QuestSiteMinRadiusStep > maxStepForMin)
      QuestSiteMinRadiusStep = maxStepForMin;

    float minStep = QuestSiteMinRadiusStep;
    minStep = options.SliderLabeled(
      "DwarfFlavourPack_Settings_QuestSiteMinRadius".Translate(StepLabels[(int)minStep].Translate()),
      minStep, 0f, (float)maxStepForMin);
    QuestSiteMinRadiusStep = Mathf.RoundToInt(minStep);
    options.Label("DwarfFlavourPack_Settings_QuestSiteMinRadius_Desc".Translate());
    options.Gap();

    options.End();
  }

  public override void ExposeData()
  {
    Scribe_Values.Look(ref DefaultTilesPerHour, "DefaultTilesPerHour", 3);
    Scribe_Values.Look(ref ResearchedTilesPerHour, "ResearchedTilesPerHour", 12);
    Scribe_Values.Look(ref MechFormingSpeedBaseValue, "MechFormingSpeedBaseValue", 1f);
    Scribe_Values.Look(ref QuestSiteRadiusStep, "QuestSiteRadiusStep", 1);
    Scribe_Values.Look(ref QuestSiteMinRadiusStep, "QuestSiteMinRadiusStep", 1);
  }
}