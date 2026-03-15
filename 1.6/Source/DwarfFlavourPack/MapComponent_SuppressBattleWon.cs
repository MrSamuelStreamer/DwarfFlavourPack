using Verse;

namespace DwarfFlavourPack;

/// <summary>
/// Marker MapComponent. When present on an encounter map, suppresses the
/// "Caravan battle won" letter that <c>CaravansBattlefield.CheckWonBattle</c>
/// fires whenever no hostile threats remain.
///
/// Add this component in <c>PostProcessGeneratedPawnsAfterSpawning</c> for any
/// non-combat tunnel encounter (e.g. Forgotten Ossuary, Cave-In).
/// The suppression patch is in <see cref="HarmonyPatches.CaravansBattlefield_Patch"/>.
/// </summary>
public class MapComponent_SuppressBattleWon : MapComponent
{
    public MapComponent_SuppressBattleWon(Map map) : base(map) { }
}
