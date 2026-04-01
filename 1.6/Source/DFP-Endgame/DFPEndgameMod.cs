using HarmonyLib;
using Verse;

namespace DwarfFlavourPack.Endgame;

public class DFPEndgameMod : Mod
{
    public DFPEndgameMod(ModContentPack content)
        : base(content)
    {
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("keyz.rimworld.DwarfFlavourPack.Endgame");
        harmony.PatchAll();
    }
}
