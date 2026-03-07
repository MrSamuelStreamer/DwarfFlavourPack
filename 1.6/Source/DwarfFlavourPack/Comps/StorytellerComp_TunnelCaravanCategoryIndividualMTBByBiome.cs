using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack.Comps;

public class StorytellerComp_TunnelCaravanCategoryIndividualMTBByBiome : StorytellerComp
{
    protected StorytellerCompProperties_TunnelCaravanCategoryIndividualMTBByBiome Props
    {
        get => (StorytellerCompProperties_TunnelCaravanCategoryIndividualMTBByBiome) props;
    }

    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
        if (!(target is World))
        {
            List<IncidentDef> allIncidents = DefDatabase<IncidentDef>.AllDefsListForReading;
            for (int i = 0; i < allIncidents.Count; ++i)
            {
                IncidentDef def = allIncidents[i];
                if (def.category == Props.category)
                {
                    BiomeDef biome = Find.WorldGrid[target.Tile].PrimaryBiome;
                    if (def.mtbDaysByBiome != null)
                    {
                        MTBByBiome mtbByBiome = def.mtbDaysByBiome.Find(x => x.biome == biome);
                        if (mtbByBiome != null)
                        {
                            float mtbDays = mtbByBiome.mtbDays;
                            if (Props.applyCaravanVisibility)
                            {
                                if (target is WorldObject_TunnelCaravan caravan)
                                    mtbDays /= CaravanVisibilityCalculator.Visibility(caravan.caravan.GetDirectlyHeldThings().OfType<Pawn>(), !caravan.caravan.paused);
                                else if (target is Map map && map.Parent.def.isTempIncidentMapOwner)
                                {
                                    IEnumerable<Pawn> pawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Concat(map.mapPawns.PrisonersOfColonySpawned);
                                    mtbDays /= CaravanVisibilityCalculator.Visibility(pawns, false);
                                }
                            }
                            if (Rand.MTBEventOccurs(mtbDays, 60000f, 1000f))
                            {
                                IncidentParms parms = GenerateParms(def.category, target);
                                if (def.Worker.CanFireNow(parms))
                                    yield return new FiringIncident(def, this, parms);
                            }
                        }
                    }
                }
            }
        }
    }

    public override string ToString() => $"{base.ToString()} {Props.category}";
}