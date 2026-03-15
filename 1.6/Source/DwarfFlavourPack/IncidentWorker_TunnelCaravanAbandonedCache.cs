using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace DwarfFlavourPack;

/// <summary>
/// Incident: an abandoned supply cache is discovered in the tunnel.
/// No combat — a ghost SpaceRefugee satisfies IncidentWorker_Ambush's non-empty
/// pawn requirement. It is despawned on the next tick (via MapComponent_DespawnGhostNextTick)
/// so the player never sees it wandering. Loot is spawned near the map centre.
/// </summary>
public class IncidentWorker_TunnelCaravanAbandonedCache : IncidentWorker_TunnelCaravanSomethingHappened
{
    private Pawn _witness;

    protected override List<Pawn> GeneratePawns(IncidentParms parms)
    {
        parms.faction = null;
        _witness = MakeGhostPawn();
        return new List<Pawn> { _witness };
    }

    protected override void PostProcessGeneratedPawnsAfterSpawning(List<Pawn> generatedPawns)
    {
        Map map = _witness?.MapHeld;
        if (map == null) return;

        // Suppress the "Caravan battle won" letter — no enemies, so CheckWonBattle
        // fires immediately without this component in place.
        map.components.Add(new MapComponent_SuppressBattleWon(map));

        // Despawn the ghost on the next tick — after DoExecute sends the letter
        // (which needs the pawn spawned as look target), but before the player
        // sees it wandering the map.
        map.components.Add(new MapComponent_DespawnGhostNextTick(map, _witness));

        SpawnCacheItems(map);
    }

    protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
        => new LordJob_ExitMapBest();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SpawnCacheItems(Map map)
    {
        TrySpawnStack(ThingDefOf.Silver,              Rand.Range(50, 200), map);
        TrySpawnStack(ThingDefOf.Steel,               Rand.Range(30, 100), map);
        TrySpawnStack(ThingDefOf.MealSurvivalPack,    Rand.Range(5, 15),   map);
        TrySpawnStack(ThingDefOf.MedicineHerbal,      Rand.Range(3, 10),   map);
        if (Rand.Chance(0.5f))
            TrySpawnStack(ThingDefOf.ComponentIndustrial, Rand.Range(2, 7), map);
        if (Rand.Chance(0.25f))
            TrySpawnStack(ThingDefOf.Gold, Rand.Range(10, 40), map);
        if (Rand.Chance(0.15f))
            TrySpawnStack(ThingDef.Named("MedicineIndustrial"), Rand.Range(2, 6), map);
    }

    private static void TrySpawnStack(ThingDef def, int count, Map map)
    {
        if (def == null) return;
        if (!CellFinder.TryFindRandomCellNear(map.Center, map, 10,
                c => c.Standable(map) && !c.Fogged(map), out IntVec3 cell))
            return; // map too blocked; skip this stack

        Thing thing = ThingMaker.MakeThing(def);
        thing.stackCount = Mathf.Min(count, def.stackLimit);
        GenSpawn.Spawn(thing, cell, map);
    }
}
