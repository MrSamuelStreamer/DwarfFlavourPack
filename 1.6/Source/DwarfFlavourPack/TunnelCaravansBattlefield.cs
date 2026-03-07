using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack;

public class TunnelCaravansBattlefield : MapParent
{
    private bool wonBattle;

    public WorldObject_TunnelCaravan Caravan => Find.WorldObjects.WorldObjectAt<WorldObject_TunnelCaravan>(Tile);

    public bool WonBattle => wonBattle;

    public override bool CanReformFoggedEnemies => true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref wonBattle, "wonBattle");
    }

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;
        if (Map.mapPawns.AnyPawnBlockingMapRemoval || TransporterUtility.IncomingTransporterPreventingMapRemoval(Map))
            return false;
        alsoRemoveWorldObject = true;
        return true;
    }

    protected override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (!HasMap)
            return;
        CheckWonBattle();
    }

    public override void PostMapGenerate()
    {
        base.PostMapGenerate();
        GetComponent<TimedDetectionRaids>().StartDetectionCountdown(240000);
    }

    private void CheckWonBattle()
    {
        if (wonBattle || GenHostility.AnyHostileActiveThreatToPlayer(Map))
            return;
        
        Find.LetterStack.ReceiveLetter("LetterLabelCaravansBattlefieldVictory".Translate(), "DFP_LetterCaravansBattlefieldVictory".Translate(), LetterDefOf.PositiveEvent, (WorldObject) this);
        TaleRecorder.RecordTale(TaleDefOf.CaravanAmbushDefeated, Map.mapPawns.FreeColonists.RandomElement());
        wonBattle = true;
    }
}