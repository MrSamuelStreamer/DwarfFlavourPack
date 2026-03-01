using RimWorld;
using Verse;

namespace DwarfFlavourPack;

[DefOf]
public static class DwarfFlavourPackDefOf
{
    // Remember to annotate any Defs that require a DLC as needed e.g.
    // [MayRequireBiotech]
    // public static GeneDef YourPrefix_YourGeneDefName;
    public static TunnelDef DFP_Tunnel;
    
    static DwarfFlavourPackDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DwarfFlavourPackDefOf));
}
