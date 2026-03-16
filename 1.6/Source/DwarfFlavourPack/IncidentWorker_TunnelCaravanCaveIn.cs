using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

/// <summary>
/// Incident: the tunnel ceiling collapses, scattering biome-matched rock walls across
/// a chokepoint. The caravan cannot reform until all blocking debris is mined clear.
///
///   - MapComponent_CaveInBlocker tracks the debris and fires "path cleared" when done.
///   - CaveInReformBlocker_Patch (Harmony) blocks ExitMapAndCreateCaravan while
///     MapComponent_CaveInBlocker.IsCleared is false.
///   - MapComponent_SuppressBattleWon and map/letter setup handled by base class.
/// </summary>
public class IncidentWorker_TunnelCaravanCaveIn : IncidentWorker_TunnelCaravanNonCombat
{
    // FireOncePerGame is deliberately left at the default (false) — cave-ins recur.

    protected override void PostSetupEncounterMap(Map map)
    {
        var caveInBlocker = new MapComponent_CaveInBlocker(map);
        map.components.Add(caveInBlocker);

        ThingDef rockDef  = GetRockWallDef(map);
        int chokeY        = Mathf.RoundToInt(map.Size.z * 0.35f);
        int debrisCount   = Rand.RangeInclusive(4, 7);
        int spawned       = 0;

        for (int attempt = 0; attempt < debrisCount * 3 && spawned < debrisCount; attempt++)
        {
            IntVec3 cell = new IntVec3(
                Rand.RangeInclusive(map.Size.x / 3, 2 * map.Size.x / 3),
                0,
                chokeY + Rand.RangeInclusive(-2, 2));

            if (!cell.InBounds(map) || !cell.Standable(map))
                continue;

            Thing rock = GenSpawn.Spawn(ThingMaker.MakeThing(rockDef), cell, map);
            caveInBlocker.TrackThing(rock);
            spawned++;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a mineable rock-wall ThingDef matching the tile's biome rock types,
    /// falling back to Sandstone if none is found.
    /// </summary>
    private static ThingDef GetRockWallDef(Map map)
    {
        ThingDef rockType = Find.World.NaturalRockTypesIn(map.Tile).FirstOrDefault();
        return rockType ?? ThingDef.Named("Sandstone");
    }
}
