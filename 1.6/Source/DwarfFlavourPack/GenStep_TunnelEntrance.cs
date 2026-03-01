using RimWorld;
using Verse;

namespace DwarfFlavourPack;

public class GenStep_TunnelEntrance : GenStep_BaseRuins
{
    private LayoutDef layoutDef;
    private static readonly FloatRange BlastMarksPer10K = new(1f, 10f);
    private static readonly FloatRange RubblePilesPer10K = new(6f, 14f);
    private static readonly IntRange RubblePileCountRange = new(3, 7);
    private static readonly IntRange RubblePileDistanceRange = new(2, 10);

    public override int SeedPart => 9642121;

    protected override LayoutDef LayoutDef => layoutDef;

    protected override int RegionSize => 45;

    protected override FloatRange DefaultMapFillPercentRange => new(0.15f, 0.3f);

    protected override FloatRange MergeRange => new(0.1f, 0.35f);

    protected override int MoveRangeLimit => 3;

    protected override int ContractLimit => 3;

    protected override int MinRegionSize => 14;

    protected override Faction Faction => Faction.OfAncientsHostile;

    public override void GenerateRuins(Map map, GenStepParams parms, FloatRange mapFillPercentRange)
    {
        base.GenerateRuins(map, parms, mapFillPercentRange);
        MapGenUtility.SpawnExteriorLumps(map, ThingDefOf.RubblePile, RubblePilesPer10K, RubblePileCountRange, RubblePileDistanceRange);
        MapGenUtility.SpawnScatter(map, ThingDefOf.Filth_BlastMark, BlastMarksPer10K);
    }
}