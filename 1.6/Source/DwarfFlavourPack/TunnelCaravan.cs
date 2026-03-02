using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack;

public class TunnelCaravan: Thing, IThingHolder
{
    // ReSharper disable once InconsistentNaming
    private ThingOwner<Thing> innerContainer;

    public Building_Tunnel tunnel;
    
    public SurfaceTile surfaceTile;

    public int travelStartsAtTick = -1;
    public int travelEndsAtTick = -1;

    public override void ExposeData()
    {
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_References.Look(ref tunnel, "tunnel");
        Scribe_Values.Look(ref surfaceTile, "surfaceTile");
        Scribe_Values.Look(ref travelStartsAtTick, "travelStartsAtTick");
        Scribe_Values.Look(ref travelEndsAtTick, "travelEndsAtTick");

        if (innerContainer == null) innerContainer = new ThingOwner<Thing>(this);
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return innerContainer;
    }
}