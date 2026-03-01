using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack;

public class TunnelGenData(World world) : WorldComponent(world)
{
    public struct TunnelLink
    {
        public PlanetTile neighbor;
        public TunnelDef tunnel;
    }
    
    public static TunnelGenData Instance => Find.World.GetComponent<TunnelGenData>();
    public Dictionary<PlanetLayer, List<PlanetTile>> tunnelNodes = new Dictionary<PlanetLayer, List<PlanetTile>>();
    public Dictionary<SurfaceTile, List<TunnelLink>> potentialTunnels = new Dictionary<SurfaceTile, List<TunnelLink>>();
    
    
    public void OverlayTunnel(PlanetTile fromTile, PlanetTile toTile, TunnelDef tunnelDef)
    {
        if (tunnelDef == null)
        {
            Log.ErrorOnce("Attempted to remove tunnel with overlayTunnel; not supported", 90292249);
        }
        else
        {
            RoadDef roadDef1 = this.GetRoadDef(fromTile, toTile, false);
            if (roadDef1 == tunnelDef)
                return;
            Tile tile1 = this[fromTile];
            Tile tile2 = this[toTile];
            SurfaceTile surfaceTile;
            ref SurfaceTile local = ref surfaceTile;
            SurfaceTile casted;
            if (tile1.Isnt<SurfaceTile>(out local) || tile2.Isnt<SurfaceTile>(out casted))
                return;
            if (roadDef1 != null)
            {
                if (roadDef1.priority >= tunnelDef.priority)
                    return;
                surfaceTile.potentialRoads.RemoveAll((Predicate<SurfaceTile.RoadLink>) (rl => rl.neighbor == toTile));
                casted.potentialRoads.RemoveAll((Predicate<SurfaceTile.RoadLink>) (rl => rl.neighbor == fromTile));
            }
            if (surfaceTile.potentialRoads == null)
                surfaceTile.potentialRoads = new List<SurfaceTile.RoadLink>();
            if (casted.potentialRoads == null)
                casted.potentialRoads = new List<SurfaceTile.RoadLink>();
            List<SurfaceTile.RoadLink> potentialRoads1 = surfaceTile.potentialRoads;
            SurfaceTile.RoadLink roadLink1 = new SurfaceTile.RoadLink();
            roadLink1.neighbor = toTile;
            roadLink1.road = tunnelDef;
            SurfaceTile.RoadLink roadLink2 = roadLink1;
            potentialRoads1.Add(roadLink2);
            List<SurfaceTile.RoadLink> potentialRoads2 = casted.potentialRoads;
            roadLink1 = new SurfaceTile.RoadLink();
            roadLink1.neighbor = fromTile;
            roadLink1.road = tunnelDef;
            SurfaceTile.RoadLink roadLink3 = roadLink1;
            potentialRoads2.Add(roadLink3);
        }
    }
}