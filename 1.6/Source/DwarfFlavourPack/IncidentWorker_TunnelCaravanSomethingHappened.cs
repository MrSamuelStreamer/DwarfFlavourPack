using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace DwarfFlavourPack;

/// <summary>
/// Abstract base for all "Something Happened" tunnel-caravan incidents.
/// Captures TunnelCaravan travel state into TunnelEncounterSetup statics before
/// base.TryExecuteWorker destroys the caravan object, and enforces the standard
/// TunnelCaravan-only CanFireNowSub guard.
///
/// Subclasses that should fire at most once per save game override FireOncePerGame
/// to return true. The check and marking are handled here automatically.
///
/// Also provides two shared helpers used by non-SomethingHappened tunnel incidents
/// (InsectAttack, FactionAmbush) that cannot inherit from this class:
///   CaptureEncounterSetup(parms)  — the TunnelEncounterSetup capture block
///   MeetsCaravanGuard(caravan)    — the TunnelCaravan CanFireNowSub checks
/// </summary>
public abstract class IncidentWorker_TunnelCaravanSomethingHappened : IncidentWorker_Ambush
{
    /// <summary>
    /// Return true to prevent this incident from firing more than once per save game.
    /// </summary>
    protected virtual bool FireOncePerGame => false;

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Captures the TunnelCaravan's travel state into TunnelEncounterSetup statics.
    /// Called by TryExecuteWorker here and also exposed for workers that extend a
    /// different base class (e.g. IncidentWorker_TunnelCaravanFactionAmbush).
    /// </summary>
    internal static void CaptureEncounterSetup(IncidentParms parms)
    {
        if (parms.target is not TunnelCaravan tunnelCaravan)
            return;

        TunnelEncounterSetup.PendingCaravan           = tunnelCaravan;
        TunnelEncounterSetup.HasActiveEncounter       = true;
        TunnelEncounterSetup.ActiveEncounterTile      = tunnelCaravan.Tile;
        TunnelEncounterSetup.ActiveOrigin             = tunnelCaravan.origin;
        TunnelEncounterSetup.ActiveDestination        = tunnelCaravan.destination;
        TunnelEncounterSetup.ActiveTunnel             = tunnelCaravan.tunnel;
        TunnelEncounterSetup.ActiveTravelStartsAtTick = tunnelCaravan.travelStartsAtTick;
        TunnelEncounterSetup.ActiveTravelEndsAtTick   = tunnelCaravan.travelEndsAtTick;
    }

    /// <summary>
    /// Shared CanFireNowSub guard for all tunnel-caravan incidents.
    /// Returns false if the target is not an in-transit, non-spawned TunnelCaravan.
    /// Does NOT call base.CanFireNowSub — callers are responsible for that.
    /// </summary>
    internal static bool MeetsCaravanGuard(TunnelCaravan tunnelCaravan)
    {
        if (tunnelCaravan.Spawned) return false;
        if (tunnelCaravan.PawnsListForReading.Count < 1) return false;

        int t = Find.TickManager.TicksGame;
        if (t <= tunnelCaravan.travelStartsAtTick || t >= tunnelCaravan.travelEndsAtTick)
            return false;

        if (tunnelCaravan.done) return false;
        return true;
    }

    /// <summary>
    /// Generates a single harmless SpaceRefugee whose only role is to satisfy
    /// IncidentWorker_Ambush's non-empty pawn requirement and trigger map creation.
    /// Pair with LordJob_ExitMapBest so the pawn immediately flees.
    /// </summary>
    protected static Pawn MakeGhostPawn() => PawnGenerator.GeneratePawn(
        new PawnGenerationRequest(
            PawnKindDefOf.SpaceRefugee,
            faction:                  null,
            context:                  PawnGenerationContext.NonPlayer,
            forceGenerateNewPawn:     true,
            allowDead:                false,
            allowDowned:              false,
            canGeneratePawnRelations: false,
            mustBeCapableOfViolence:  false));

    /// <summary>
    /// Returns a standable, unfogged cell near the map centre.
    /// Falls back to a random nearby cell if map.Center itself is blocked.
    /// </summary>
    protected static IntVec3 FindCellNearCenter(Map map, int radius = 15)
    {
        IntVec3 cell = map.Center;
        if (!cell.Standable(map))
            CellFinder.TryFindRandomCellNear(map.Center, map, radius,
                c => c.Standable(map) && !c.Fogged(map), out cell);
        return cell;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        CaptureEncounterSetup(parms);

        bool result = base.TryExecuteWorker(parms);

        if (result && FireOncePerGame)
            GameComponent_OncePerGameIncidents.Instance?.MarkFired(def.defName);

        return result;
    }

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (parms.target is not TunnelCaravan tunnelCaravan)
            return false;

        if (!MeetsCaravanGuard(tunnelCaravan))
            return false;

        if (FireOncePerGame && (GameComponent_OncePerGameIncidents.Instance?.HasFired(def.defName) ?? false))
            return false;

        return base.CanFireNowSub(parms);
    }
}
