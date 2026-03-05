using RimWorld;
using Verse;
using Verse.AI;

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
  [MayRequireBiotech]
  public static JobDef DFP_CarryDownedPawnToPortal;

  public static DutyDef DFP_LoadAndEnterTunnel;

  public static WorldObjectDef DFP_TunnelEntranceSite;
  public static SitePartDef DFP_TunnelEntranceSitePart;

  public static ThingDef DFP_TunnelEntrance;
  public static ThingDef DFP_TunnelCaravan;


  static DwarfFlavourPackDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DwarfFlavourPackDefOf));
}