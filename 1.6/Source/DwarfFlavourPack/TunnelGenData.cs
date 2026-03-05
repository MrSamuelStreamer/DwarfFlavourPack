using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

public class TunnelGenData(World world) : WorldComponent(world), IThingHolder
{
  // ReSharper disable once InconsistentNaming
  private ThingOwner<TunnelCaravan> caravans;

  public ThingOwner<TunnelCaravan> Caravans
  {
    get
    {
      caravans ??= new ThingOwner<TunnelCaravan>(this);
      return caravans;
    }
  }

  public WorldPathing Pather
  {
    get
    {
      field ??= new WorldPathing(Find.WorldGrid.Surface);
      return field;
    }
  }

  public static IEnumerable<WorldObject> WorldObjectsWithTunnelEntrances()
  {
    foreach (TunnelEntrance tunnelEntrance in Find.WorldObjects.AllWorldObjects.OfType<TunnelEntrance>())
    {
      yield return tunnelEntrance;
    }

    foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome))
    {
      yield return Find.WorldObjects.WorldObjectAt<WorldObject>(map.Tile);
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

  public void SendCaravan(Building_Tunnel tunnel)
  {
    float distance = Find.WorldGrid.ApproxDistanceInTiles(tunnel.Caravan.origin, tunnel.Caravan.destination);
    int ticksToTravel = Mathf.FloorToInt((distance / DwarfFlavourPackMod.settings.TilesPerHour) * GenDate.TicksPerHour);
    tunnel.Caravan.travelEndsAtTick = Find.TickManager.TicksGame + ticksToTravel;
    tunnel.Caravan.travelStartsAtTick = Find.TickManager.TicksGame;

    // Transfer the Thing from the building's ThingOwner into the world component's ThingOwner.
    Caravans.TryAddOrTransfer(tunnel.Caravan);

    tunnel.ClearCaravan();
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

      Tile fromTile = layer.Tiles[fromPlanetTile];
      ;
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
        potentialTunnels[toSurfaceTile].RemoveAll((tile => tile.neighbor == fromPlanetTile));
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
    // Make sure scribing/GC can walk the nested holder graph.
    ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, Caravans);
  }

  public ThingOwner GetDirectlyHeldThings() => Caravans;

  public IThingHolder ParentHolder => null;

  public override void ExposeData()
  {
    base.ExposeData();

    // ThingOwner is the "correct" way to save/load owned Things.
    Scribe_Deep.Look(ref caravans, "caravans", this);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
      caravans ??= new ThingOwner<TunnelCaravan>(this);
  }

  public override void WorldComponentTick()
  {
    base.WorldComponentTick();

    foreach (TunnelCaravan caravan in Caravans.InnerListForReading.Where(c => !c.done && !c.mapGenerating))
    {
      if (Find.TickManager.TicksGame < caravan.travelEndsAtTick) continue;

      WorldObject wo = Find.WorldObjects.WorldObjectAt<WorldObject>(caravan.destination);
      LongEventHandler.QueueLongEvent(() =>
      {
        Map map = Current.Game.FindMap(wo.Tile);
        if (map == null)
        {
          map = GetOrGenerateMapUtility.GetOrGenerateMap(wo.Tile, wo.def, null);
        }

        ModLog.Debug("Generated map for caravan");
        caravan.SpawnToMap(map);
        ModLog.Debug("Spawned to map");
        Current.Game?.CurrentMap = map;
        caravan.done = true;
      }, "DwarfFlavourPack_TunnelMap", false, exception =>
      {
        ModLog.Error(exception.ToString());
        caravan.mapGenerating = false;
      });
    }

    // Remove finished caravans from the ThingOwner (not a List anymore).
    foreach (var done in Caravans.InnerListForReading.Where(c => c.done).ToList())
      Caravans.Remove(done);
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
    }, "Regenerating Tunnel Network", false, exception =>
    {
      ModLog.Error(exception.ToString());
    });
  }
}