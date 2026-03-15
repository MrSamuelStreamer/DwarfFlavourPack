using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace DwarfFlavourPack;

/// <summary>
/// Incident: the caravan stops in a natural chamber where ancient worshippers died
/// before whatever shrine they came to serve. A non-combat discovery event —
/// no enemies, only the scene and whatever loot the dead carried.
///
/// A single "Last Witness" ghost pawn satisfies IncidentWorker_Ambush's non-empty
/// pawn guard (it bails before creating a map if GeneratePawns returns empty).
/// In PostProcess the witness is despawned (not killed) so the player never sees a
/// living non-colonist pawn. Killing would null out mindState, crashing
/// LordToil_ExitMap.UpdateAllDuties; an alive-but-unspawned pawn is safe.
/// </summary>
public class IncidentWorker_TunnelCaravanForgottenOssuary : IncidentWorker_TunnelCaravanSomethingHappened
{
    protected override bool FireOncePerGame => true;

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
        if (map == null)
            return;

        // Suppress the "Caravan battle won" letter — this is a non-combat encounter
        // so there are never any hostile threats and CheckWonBattle fires immediately.
        map.components.Add(new MapComponent_SuppressBattleWon(map));

        // Despawn the ghost witness immediately so the player never sees a living
        // non-colonist pawn. We despawn rather than kill because dead pawns have
        // mindState == null, which causes LordToil_ExitMap.UpdateAllDuties to crash
        // when the lord is created. An alive-but-unspawned pawn is invisible to the
        // player and still has a valid mindState for the duty assignment.
        _witness.DeSpawn();

        IntVec3 shrineCell = FindCellNearCenter(map);

        // Spawn shrine — Reliquary if Ideology is active, otherwise NatureShrine_Large.
        // Pass GenStuff.DefaultStuffFor to silence the madeFromStuff warning on Reliquary.
        ThingDef shrineDef = ModsConfig.IdeologyActive
            ? ThingDefOf.Reliquary
            : ThingDefOf.NatureShrine_Large;
        GenSpawn.Spawn(ThingMaker.MakeThing(shrineDef, GenStuff.DefaultStuffFor(shrineDef)), shrineCell, map, Rot4.South);

        // Spawn 5–8 skeleton corpses in a radial arc in front of the shrine.
        int corpseCount = Rand.RangeInclusive(5, 8);
        for (int i = 0; i < corpseCount; i++)
        {
            // Spread cells in a semicircle south of the shrine (worshippers face it).
            float angle = Mathf.Lerp(-70f, 70f, corpseCount > 1 ? (float)i / (corpseCount - 1) : 0.5f);
            float dist  = Rand.Range(3f, 8f);
            IntVec3 cell = shrineCell + IntVec3.FromVector3(
                new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * dist,
                    0f,
                    -dist));  // negative Z = south / in front

            if (!cell.InBounds(map) || !cell.Standable(map))
                CellFinder.TryFindRandomCellNear(shrineCell, map, 10,
                    c => c.Standable(map) && c != shrineCell, out cell);

            Pawn ancient = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.SpaceRefugee,
                faction:                  null,
                context:                  PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn:     true,
                allowDead:                false,
                allowDowned:              false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence:  false
            ));
            GenSpawn.Spawn(ancient, cell, map, Rot4.Random);
            KillAndDecay(ancient);

            // Give loot to roughly half the corpses.
            if (ancient.Corpse != null && Rand.Bool)
                AddCorpseLoot(ancient.Corpse);
        }

        // Place 1–2 loose shrine offerings on the ground near the centre.
        int offeringCount = Rand.RangeInclusive(1, 2);
        for (int i = 0; i < offeringCount; i++)
        {
            CellFinder.TryFindRandomCellNear(shrineCell, map, 4,
                c => c.Standable(map) && !c.Fogged(map), out IntVec3 lootCell);
            GenSpawn.Spawn(RandomShrineOffering(), lootCell, map, Rot4.Random);
        }
    }

    protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
        => new LordJob_ExitMapBest();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Kills a spawned pawn and advances its corpse to skeleton stage.</summary>
    private static void KillAndDecay(Pawn pawn)
    {
        if (!pawn.Dead)
            pawn.Kill(null);

        Corpse corpse = pawn.Corpse;
        CompRottable rot = corpse?.TryGetComp<CompRottable>();
        if (rot != null)
            rot.RotProgress = rot.PropsRot.TicksToDessicated + 1f;
    }

    private static void AddCorpseLoot(Corpse corpse)
    {
        ThingDef[] options =
        {
            ThingDefOf.Silver,
            ThingDefOf.Gold,
            ThingDefOf.ComponentIndustrial,
            ThingDefOf.Jade,
        };
        ThingDef chosen = options.RandomElement();
        Thing item = ThingMaker.MakeThing(chosen);
        item.stackCount = chosen == ThingDefOf.Silver              ? Rand.RangeInclusive(15, 40)
                        : chosen == ThingDefOf.Gold                ? Rand.RangeInclusive(5,  15)
                        : chosen == ThingDefOf.ComponentIndustrial ? Rand.RangeInclusive(1,   3)
                        : Rand.RangeInclusive(10, 30);  // Jade
        corpse.InnerPawn.inventory.innerContainer.TryAdd(item, canMergeWithExistingStacks: true);
    }

    private static Thing RandomShrineOffering()
    {
        float roll = Rand.Value;
        ThingDef def;
        int count;
        if (roll < 0.5f)
        {
            def   = ThingDefOf.Silver;
            count = Rand.RangeInclusive(20, 60);
        }
        else if (roll < 0.85f)
        {
            def   = ThingDefOf.Gold;
            count = Rand.RangeInclusive(8, 20);
        }
        else
        {
            def   = ThingDefOf.ComponentSpacer;
            count = 1;
        }
        Thing item = ThingMaker.MakeThing(def);
        item.stackCount = count;
        return item;
    }
}
