using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack;

public class ThingOwnerProxy : ThingOwner<Thing>
{
    public TunnelCaravan Caravan => owner as TunnelCaravan;
    public ThingOwnerProxy(IThingHolder owner): base(owner)
    {
        
    }
    
    protected override void NotifyAdded(Thing item)
    {
        base.NotifyAdded(item);
        Caravan?.tunnel.Notify_ThingAdded(item);
    }
}

public class TunnelCaravan: Thing, IThingHolder
{
    private ThingOwnerProxy innerContainer;
    
    public PlanetTile destination;
    public PlanetTile origin;

    
    public Building_Tunnel tunnel;
    
    public SurfaceTile surfaceTile;

    public int travelStartsAtTick = -1;
    public int travelEndsAtTick = -1;
    
    public bool MapGenerating = false;
    public bool ReadyToSend = false;
    public bool Done = false;
    
    private List<Thing> tmpThings = new();

    private List<Pawn> tmpSavedPawns = new();

    public override void ExposeData()
    {
        // CRITICAL: without this, Thing.def / thingIDNumber won't be saved/loaded correctly.
        base.ExposeData();

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            if (innerContainer != null)
            {
                tmpThings.Clear();
                tmpThings.AddRange(innerContainer);

                tmpSavedPawns.Clear();
                foreach (var t in tmpThings.OfType<Pawn>())
                {
                    innerContainer.Remove(t);
                    tmpSavedPawns.Add(t);
                }

                tmpThings.Clear();
            }
            else
            {
                tmpSavedPawns.Clear();
            }
        }

        Scribe_Collections.Look(ref tmpSavedPawns, "tmpSavedPawns", LookMode.Deep);
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_References.Look(ref tunnel, "tunnel");
        Scribe_Deep.Look(ref surfaceTile, "surfaceTile");
        Scribe_Values.Look(ref travelStartsAtTick, "travelStartsAtTick");
        Scribe_Values.Look(ref travelEndsAtTick, "travelEndsAtTick");
        Scribe_Values.Look(ref origin, "origin");
        Scribe_Values.Look(ref destination, "destination");
        Scribe_Values.Look(ref ReadyToSend, "ReadyToSend");

        if (Scribe.mode != LoadSaveMode.PostLoadInit && Scribe.mode != LoadSaveMode.Saving)
            return;

        // Ensure container exists before re-adding saved pawns
        innerContainer ??= new ThingOwnerProxy(this);

        foreach (var t in tmpSavedPawns)
            innerContainer.TryAddOrTransfer(t);

        tmpSavedPawns.Clear();
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        innerContainer ??= new ThingOwnerProxy(this);
        return innerContainer;
    }
    
    public Pawn Owner => innerContainer.OfType<Pawn>().First();

    public string TimeToGo => (travelEndsAtTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();
    
    public string Progress => "DwarfFlavourPack_CaravanProgress".Translate(TimeToGo, Owner.Named("PAWN"));

    public void SpawnToMap(Map map)
    {
        Building_Tunnel bld = map.listerThings.AllThings.OfType<Building_Tunnel>().FirstOrDefault();
        IntVec3 loc = map.Center;
        if (bld != null)
        {
            loc = bld.Position;
        }
        GetDirectlyHeldThings().TryDropAll(loc, map, ThingPlaceMode.Near);
    }
}