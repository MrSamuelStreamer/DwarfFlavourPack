using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace DwarfFlavourPack;

public class IncidentWorker_TunnelCaravanInsectAttack : IncidentWorker_Ambush
{
    /// <summary>
    /// Registers the TunnelCaravan in <see cref="TunnelEncounterSetup"/> before base
    /// triggers map generation so that <see cref="MapComponent_TunnelCaravanState"/>
    /// can capture the travel state inside its constructor (called during map gen).
    /// The caravan is destroyed by CaravanEnterMapUtility.Enter during base execution,
    /// so we must save the reference before that happens.
    /// </summary>
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (parms.target is TunnelCaravan tunnelCaravan)
        {
            // PendingCaravan is consumed by MapComponent_TunnelCaravanState's constructor.
            TunnelEncounterSetup.PendingCaravan = tunnelCaravan;

            // Also store data directly in statics so the reform patch can read it
            // without depending on pawn.MapHeld being non-null or a specific overload.
            TunnelEncounterSetup.HasActiveEncounter      = true;
            TunnelEncounterSetup.ActiveEncounterTile     = tunnelCaravan.Tile;
            TunnelEncounterSetup.ActiveOrigin            = tunnelCaravan.origin;
            TunnelEncounterSetup.ActiveDestination       = tunnelCaravan.destination;
            TunnelEncounterSetup.ActiveTunnel            = tunnelCaravan.tunnel;
            TunnelEncounterSetup.ActiveTravelStartsAtTick = tunnelCaravan.travelStartsAtTick;
            TunnelEncounterSetup.ActiveTravelEndsAtTick  = tunnelCaravan.travelEndsAtTick;
        }

        return base.TryExecuteWorker(parms);
    }

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (parms.target is not TunnelCaravan tunnelCaravan)
            return false;

        if (tunnelCaravan.Spawned)
            return false;

        if (tunnelCaravan.PawnsListForReading.Count < 1)
            return false;

        int ticksGame = Find.TickManager.TicksGame;
        if (ticksGame <= tunnelCaravan.travelStartsAtTick || ticksGame >= tunnelCaravan.travelEndsAtTick)
            return false;

        if (tunnelCaravan.done)
            return false;

        return base.CanFireNowSub(parms);
    }

    protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
    {
        return new LordJob_AssaultColony(parms.faction, canTimeoutOrFlee: false);
    }

    protected override List<Pawn> GeneratePawns(IncidentParms parms)
    {
        parms.faction = Faction.OfInsects;

        List<Pawn> insects = new List<Pawn>();

        float points = parms.points > 0f ? parms.points : 300f;

        PawnKindDef[] insectKinds = new[]
        {
            PawnKindDefOf.Megaspider,
            PawnKindDefOf.Spelopede,
            PawnKindDefOf.Megascarab
        };

        while (points > 0f && insects.Count < 50)
        {
            PawnKindDef insectKind = insectKinds[Rand.Range(0, insectKinds.Length)];
            float cost = insectKind.combatPower;

            if (cost > points && insects.Count > 0)
                break;

            Pawn insect = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                insectKind,
                Faction.OfInsects,
                PawnGenerationContext.NonPlayer,
                -1,
                forceGenerateNewPawn: false,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: true,
                mustBeCapableOfViolence: true
            ));

            insects.Add(insect);
            points -= cost;
        }

        return insects;
    }
}