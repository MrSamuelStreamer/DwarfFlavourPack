using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack;

public class ThingOwnerProxy : ThingOwner<Thing>
{
  public TunnelCaravan Caravan => owner as TunnelCaravan;

  public ThingOwnerProxy(IThingHolder owner) : base(owner)
  {

  }

  protected override void NotifyAdded(Thing item)
  {
    base.NotifyAdded(item);
    Caravan?.tunnel.Notify_ThingAdded(item);
  }
}

public class TunnelCaravan : Thing, IThingHolder
{
  private ThingOwnerProxy _innerContainer;

  public PlanetTile destination;
  public PlanetTile origin;


  public Building_Tunnel tunnel;

  public SurfaceTile surfaceTile;

  public int travelStartsAtTick = -1;
  public int travelEndsAtTick = -1;

  public bool MapGenerating = false;
  public bool ReadyToSend = false;
  public bool Done = false;

  private List<Pawn> tmpSavedPawns = new();

  public override void ExposeData()
  {
    // CRITICAL: without this, Thing.def / thingIDNumber won't be saved/loaded correctly.
    base.ExposeData();

    if (Scribe.mode == LoadSaveMode.Saving)
    {
      tmpSavedPawns.Clear();
      if (_innerContainer != null)
      {
        foreach (var t in _innerContainer.OfType<Pawn>().ToList())
        {
          _innerContainer.Remove(t);
          tmpSavedPawns.Add(t);
        }
      }
    }

    Scribe_Collections.Look(ref tmpSavedPawns, "tmpSavedPawns", LookMode.Deep);
    Scribe_Deep.Look(ref _innerContainer, "innerContainer", this);
    Scribe_References.Look(ref tunnel, "tunnel");
    Scribe_Deep.Look(ref surfaceTile, "surfaceTile");
    Scribe_Values.Look(ref travelStartsAtTick, "travelStartsAtTick", -1);
    Scribe_Values.Look(ref travelEndsAtTick, "travelEndsAtTick", -1);
    Scribe_Values.Look(ref origin, "origin");
    Scribe_Values.Look(ref destination, "destination");
    Scribe_Values.Look(ref ReadyToSend, "ReadyToSend", false);
    Scribe_Values.Look(ref MapGenerating, "MapGenerating", false);
    Scribe_Values.Look(ref Done, "Done", false);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
      _innerContainer ??= new ThingOwnerProxy(this);
      if (tmpSavedPawns != null)
      {
        foreach (Pawn t in tmpSavedPawns)
        {
          if (t != null)
            _innerContainer.TryAddOrTransfer(t);
        }
        tmpSavedPawns.Clear();
      }
    }
  }

  public void GetChildHolders(List<IThingHolder> outChildren)
  {
    ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
  }

  public ThingOwner GetDirectlyHeldThings()
  {
    _innerContainer ??= new ThingOwnerProxy(this);
    return _innerContainer;
  }

  public Pawn Owner => _innerContainer.OfType<Pawn>().First();

  public string TimeToGo => (travelEndsAtTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();

  public string Progress => "DwarfFlavourPack_CaravanProgress".Translate(TimeToGo, Owner.Named("PAWN"));

  public void SpawnToMap(Map map)
  {
    Building_Tunnel bld = map.listerThings.AllThings.OfType<Building_Tunnel>().FirstOrDefault(t => t.Tile == map.Tile);
    IntVec3 loc = map.Center;
    if (bld != null)
    {
      loc = bld.Position;
    }
    else
    {
      // If no tunnel, look for a suitable spot (e.g. any entry point or just near center)
      if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(x => x.Standable(map) && !x.Fogged(map), map, out IntVec3 result))
      {
        loc = result;
      }
    }
    GetDirectlyHeldThings().TryDropAll(loc, map, ThingPlaceMode.Near);
  }
}