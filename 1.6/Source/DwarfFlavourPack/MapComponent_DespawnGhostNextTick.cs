using Verse;

namespace DwarfFlavourPack;

/// <summary>
/// Despawns a ghost witness pawn on the very next map tick.
///
/// Timeline for non-combat tunnel incidents:
///   PostProcessGeneratedPawnsAfterSpawning  ← add this component here
///   ↓
///   IncidentWorker_Ambush.DoExecute sends the letter (ghost still spawned → look target valid)
///   ↓
///   Next MapComponentTick() → DeSpawn ghost → lord dissolves → ghost never visibly wanders
///
/// The ghost must remain spawned until after DoExecute's SendStandardLetter call, because
/// generatedEnemies[0] is the letter's look target — an unspawned pawn grays out
/// the "Jump to location" button. This component handles the deferred cleanup.
/// </summary>
public class MapComponent_DespawnGhostNextTick : MapComponent
{
    private Pawn _ghost;

    // Parameterless (Map-only) constructor required by RimWorld's reflection-based
    // MapComponent auto-instantiation (Map.FillComponents). _ghost stays null and
    // MapComponentTick does nothing until a real instance is added manually.
    public MapComponent_DespawnGhostNextTick(Map map) : base(map) { }

    public MapComponent_DespawnGhostNextTick(Map map, Pawn ghost) : base(map)
    {
        _ghost = ghost;
    }

    public override void MapComponentTick()
    {
        if (_ghost?.Spawned == true)
            _ghost.DeSpawn();

        map.components.Remove(this);
        _ghost = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref _ghost, "ghost");
    }
}
