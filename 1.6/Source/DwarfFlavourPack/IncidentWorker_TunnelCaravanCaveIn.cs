using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace DwarfFlavourPack;

/// <summary>
/// Incident: the tunnel ceiling collapses, scattering biome-matched rock walls across
/// a chokepoint. The caravan cannot reform until all blocking debris is mined clear.
///
/// Non-combat pattern:
///   - Ghost witness (SpaceRefugee) triggers map creation, then is despawned.
///   - MapComponent_SuppressBattleWon prevents the "Caravan battle won" letter.
///   - MapComponent_CaveInBlocker tracks the debris and fires "path cleared" when done.
///   - CaveInReformBlocker_Patch (Harmony) blocks ExitMapAndCreateCaravan while
///     MapComponent_CaveInBlocker.IsCleared is false.
/// </summary>
public class IncidentWorker_TunnelCaravanCaveIn : IncidentWorker_TunnelCaravanSomethingHappened
{
    // FireOncePerGame is deliberately left at the default (false) — cave-ins recur.

    private Pawn _witness;
    private MapComponent_CaveInBlocker _caveInBlocker;

    protected override List<Pawn> GeneratePawns(IncidentParms parms)
    {
        parms.faction = null;
        _witness = MakeGhostPawn();
        return new List<Pawn> { _witness };
    }

    protected override void PostProcessGeneratedPawnsAfterSpawning(List<Pawn> generatedPawns)
    {
        Map map = _witness?.MapHeld;
        if (map == null)
            return;

        // Non-combat checklist: suppress "Caravan battle won" letter.
        map.components.Add(new MapComponent_SuppressBattleWon(map));

        // Non-combat checklist: despawn the ghost witness — alive-but-unspawned keeps
        // mindState valid for LordToil_ExitMap.UpdateAllDuties, but hides the pawn.
        _witness.DeSpawn();

        // Add the blocker component before tracking rocks so TrackThing is ready.
        _caveInBlocker = new MapComponent_CaveInBlocker(map);
        map.components.Add(_caveInBlocker);

        // Spawn 4–7 biome-matched rock walls in a loose cluster near one-third up the map,
        // between the caravan spawn zone (south edge) and the map centre.
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
            _caveInBlocker.TrackThing(rock);
            spawned++;
        }
    }

    protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
        => new LordJob_ExitMapBest();

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
