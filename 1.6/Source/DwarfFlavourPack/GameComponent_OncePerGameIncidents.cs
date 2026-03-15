using System.Collections.Generic;
using Verse;

namespace DwarfFlavourPack;

/// <summary>
/// Tracks which "fire once per game" incidents have already been executed.
/// Automatically instantiated by the game via GameComponent.AllSubclassesNonAbstract()
/// — no XML registration required.
/// </summary>
public class GameComponent_OncePerGameIncidents : GameComponent
{
    private HashSet<string> _firedDefNames = new HashSet<string>();

    // Required constructor signature for GameComponent.
    public GameComponent_OncePerGameIncidents(Game game) { }

    public static GameComponent_OncePerGameIncidents Instance =>
        Current.Game.GetComponent<GameComponent_OncePerGameIncidents>();

    public bool HasFired(string defName) => _firedDefNames.Contains(defName);

    public void MarkFired(string defName) => _firedDefNames.Add(defName);

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref _firedDefNames, "firedOnceIncidents", LookMode.Value);
        _firedDefNames ??= new HashSet<string>();
    }
}
