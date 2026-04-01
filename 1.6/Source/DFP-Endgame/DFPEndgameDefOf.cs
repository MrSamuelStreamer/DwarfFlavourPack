using RimWorld;
using Verse;

namespace DwarfFlavourPack.Endgame;

[DefOf]
public static class DFPEndgameDefOf
{
    public static JobDef DFP_ActivateHeartChamber;

    static DFPEndgameDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DFPEndgameDefOf));
}
