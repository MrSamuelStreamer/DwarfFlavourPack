using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack;

public class TunnelGenData(World world) : WorldComponent(world), IThingHolder
{
    // ReSharper disable once InconsistentNaming
    private ThingOwner<TunnelCaravan> innerContainer;
    
    public WorldPathing Pather
    {
        get
        {
            field ??= new WorldPathing(Find.WorldGrid.Surface);
            return field;
        }
    }

    public struct TunnelLink
    {
        public PlanetTile neighbor;
        public TunnelDef tunnel;
    }
    
    public static TunnelGenData Instance => Find.World.GetComponent<TunnelGenData>();
    public Dictionary<PlanetLayer, List<PlanetTile>> tunnelNodes = new();
    public Dictionary<SurfaceTile, List<TunnelLink>> potentialTunnels = new();

    public void SendCaravan(TunnelCaravan caravan, Building_Tunnel tunnel)
    {
        if(innerContainer == null) innerContainer = new ThingOwner<TunnelCaravan>(this);
        innerContainer.TryTransferToContainer(caravan, tunnel.GetDirectlyHeldThings());
    }
    public void Clear()
    {
        tunnelNodes.Clear();
        potentialTunnels.Clear();
    }
    
    public TunnelDef GetTunnelDef(PlanetTile fromTile, PlanetTile toTile)
    {
        return DwarfFlavourPackDefOf.DFP_Tunnel;
    }
    
    public void OverlayTunnel(PlanetTile fromPlanetTile, PlanetTile toPlanetTile, TunnelDef tunnelDef)
    {
        PlanetLayer layer = fromPlanetTile.Layer;
        
        if (tunnelDef == null)
        {
            Log.ErrorOnce("Attempted to remove tunnel with overlayTunnel; not supported", 90292249);
        }
        else
        {
            tunnelDef = GetTunnelDef(fromPlanetTile, toPlanetTile);
            
            Tile fromTile = layer.Tiles[fromPlanetTile];;
            Tile toTole = layer.Tiles[toPlanetTile];
            
            if (fromTile.Isnt(out SurfaceTile fromSurfaceTile) || toTole.Isnt(out SurfaceTile toSurfaceTile))
                return;
            
            if (!potentialTunnels.ContainsKey(fromSurfaceTile))
                potentialTunnels.Add(fromSurfaceTile, []);
            if (!potentialTunnels.ContainsKey(toSurfaceTile))
                potentialTunnels.Add(toSurfaceTile, []);
            
            if (tunnelDef != null)
            {
                potentialTunnels[fromSurfaceTile].RemoveAll(tile => tile.neighbor == toPlanetTile);
                potentialTunnels[toSurfaceTile].RemoveAll((tile=> tile.neighbor == fromPlanetTile));
            }
            
            TunnelLink toTunnelLink = new TunnelLink
            {
                neighbor = toPlanetTile,
                tunnel = tunnelDef
            };
            potentialTunnels[fromSurfaceTile].Add(toTunnelLink);
            
            TunnelLink fromTunnelLink = new TunnelLink
            {
                neighbor = fromPlanetTile,
                tunnel = tunnelDef
            };
            potentialTunnels[toSurfaceTile].Add(fromTunnelLink);
        }
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;
    public IThingHolder ParentHolder => null;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);

        if (innerContainer == null) innerContainer = new ThingOwner<TunnelCaravan>(this);
    }
    
    
    [DebugAction("Spawning", "Spawn Tunnel Entrance", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
    private static void SpawnTunnelEntrance()
    {
        PlanetTile tile = GenWorld.MouseTile();
        if (!tile.Valid || Find.World.Impassable(tile))
        {
            Messages.Message("Impassable", MessageTypeDefOf.RejectInput, false);
        }
        else
        {
            WorldGenStep_TunnelEntrances.SpawnTunnelEntrance(tile, tile.Layer, Faction.OfAncients);
            
        }
    }
    
    [DebugAction("Spawning", "Regenerate Tunnels", allowedGameStates = AllowedGameStates.PlayingOnWorld)]
    private static void RegenerateTunnels()
    {
        LongEventHandler.QueueLongEvent(() =>
        {
            new WorldGenStep_Tunnels().GenerateFresh(Find.World.info.seedString, Find.World.grid.Surface);
            Find.World.renderer.AllDrawLayers.First(layer => layer is WorldDrawLayer_Tunnels).SetDirty();
        }, "Regenerating Tunnel Network", false, exception => {ModLog.Error(exception.ToString());});

    }
}