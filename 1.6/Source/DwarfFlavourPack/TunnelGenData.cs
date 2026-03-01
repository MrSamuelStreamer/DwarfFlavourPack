using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack;

public class TunnelGenData(World world) : WorldComponent(world)
{
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
    public Dictionary<PlanetLayer, List<PlanetTile>> tunnelNodes = new Dictionary<PlanetLayer, List<PlanetTile>>();
    public Dictionary<SurfaceTile, List<TunnelLink>> potentialTunnels = new Dictionary<SurfaceTile, List<TunnelLink>>();

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
                potentialTunnels[fromSurfaceTile].RemoveAll((tile) => tile.neighbor == toPlanetTile);
                potentialTunnels[toSurfaceTile].RemoveAll(((tile)=> tile.neighbor == fromPlanetTile));
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
}