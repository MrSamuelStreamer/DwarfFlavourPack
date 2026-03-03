using HarmonyLib;
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
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "DwarfFlavourPack_SettingsCategory".Translate();
    }
}
