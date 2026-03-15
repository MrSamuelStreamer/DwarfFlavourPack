using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

public class DwarfFlavourPackMod : Mod
{
    public static Settings settings;

    public DwarfFlavourPackMod(ModContentPack content) : base(content)
    {

        // initialize settings
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.DwarfFlavourPack.main");	
        harmony.PatchAll();
        
        LongEventHandler.ExecuteWhenFinished(ApplySettingsToDefs);
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
        ApplySettingsToDefs();
    }

    public override string SettingsCategory()
    {
        return "DwarfFlavourPack_SettingsCategory".Translate();
    }
    
    private static void ApplySettingsToDefs()
    {
        StatDef statDef = StatDef.Named("MechFormingSpeed");
        if (statDef != null)
        {
            statDef.defaultBaseValue = 1f/settings.MechFormingSpeedBaseValue;
        }
    }
}
