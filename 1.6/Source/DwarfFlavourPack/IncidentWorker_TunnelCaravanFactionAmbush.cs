using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack;

public class IncidentWorker_TunnelCaravanFactionAmbush : IncidentWorker_Ambush_EnemyFaction
{
    /// <summary>
    /// Captures the TunnelCaravan's travel state into TunnelEncounterSetup statics before
    /// calling base.TryExecuteWorker. The caravan is destroyed by CaravanEnterMapUtility.Enter
    /// during base execution, so the capture must happen first.
    /// </summary>
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (parms.target is TunnelCaravan tunnelCaravan)
        {
            TunnelEncounterSetup.PendingCaravan           = tunnelCaravan;
            TunnelEncounterSetup.HasActiveEncounter       = true;
            TunnelEncounterSetup.ActiveEncounterTile      = tunnelCaravan.Tile;
            TunnelEncounterSetup.ActiveOrigin             = tunnelCaravan.origin;
            TunnelEncounterSetup.ActiveDestination        = tunnelCaravan.destination;
            TunnelEncounterSetup.ActiveTunnel             = tunnelCaravan.tunnel;
            TunnelEncounterSetup.ActiveTravelStartsAtTick = tunnelCaravan.travelStartsAtTick;
            TunnelEncounterSetup.ActiveTravelEndsAtTick   = tunnelCaravan.travelEndsAtTick;
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

    protected override string GetLetterText(Pawn anyPawn, IncidentParms parms)
    {
        Caravan caravan = parms.target as Caravan;
        string caravanName = caravan != null ? caravan.Name : "yourCaravan".TranslateSimple();
        return def.letterText
            .Formatted(caravanName, parms.faction.def.pawnsPlural, parms.faction.NameColored)
            .Resolve()
            .CapitalizeFirst();
    }
}
