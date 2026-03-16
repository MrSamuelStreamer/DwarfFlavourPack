using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DwarfFlavourPack;

/// <summary>
/// Tracks the debris Things spawned by a Cave-In encounter. Polls every 60 ticks;
/// when all tracked Things are destroyed or despawned, fires a "path cleared" letter
/// and sets IsCleared = true so the reform-blocking patch stops blocking.
///
/// Add this component in PostProcessGeneratedPawnsAfterSpawning before calling
/// TrackThing. Added at runtime, it participates in MapComponentTick normally.
/// Persisted across save/load via ExposeData.
/// </summary>
public class MapComponent_CaveInBlocker : MapComponent
{
    private HashSet<Thing> _blockingThings = new HashSet<Thing>();
    private bool _cleared;
    // True once TrackThing has been called at least once. Guards against
    // RimWorld's reflection-based auto-instantiation of MapComponents on every
    // map: without this flag, a freshly constructed (never-used) component
    // would immediately fire "path cleared" on its first tick because
    // _blockingThings starts empty.
    private bool _active;

    public MapComponent_CaveInBlocker(Map map) : base(map) { }

    // True when this map has no active cave-in blockage: either the component was
    // never activated (no cave-in on this map — auto-instantiated by RimWorld's
    // reflection but never had TrackThing called), or it was activated and all
    // debris has been cleared. The CaveInReformBlocker_Patch checks this property;
    // without the !_active guard it would block reform on every non-cave-in map.
    public bool IsCleared => !_active || _cleared;

    public void TrackThing(Thing t)
    {
        _blockingThings.Add(t);
        _active = true;
    }

    public override void MapComponentTick()
    {
        if (!_active) return;
        if (_cleared) return;
        if (Find.TickManager.TicksGame % 60 != 0) return;

        _blockingThings.RemoveWhere(t => t == null || t.Destroyed || !t.Spawned);
        if (_blockingThings.Count == 0)
            OnPathCleared();
    }

    private void OnPathCleared()
    {
        _cleared = true;
        Find.LetterStack.ReceiveLetter(
            "DFP_CaveInClearedLabel".Translate(),
            "DFP_CaveInClearedText".Translate(),
            LetterDefOf.NeutralEvent);
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref _blockingThings, "blockingThings", LookMode.Reference);
        Scribe_Values.Look(ref _cleared, "cleared", false);
        Scribe_Values.Look(ref _active, "active", false);
        _blockingThings ??= new HashSet<Thing>();
    }
}
