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
    
    public static JobDef DFP_HaulToTunnel;
    public static JobDef DFP_EnterTunnel;

    
    static DwarfFlavourPackDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DwarfFlavourPackDefOf));
}
