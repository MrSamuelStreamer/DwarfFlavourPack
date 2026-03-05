using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

public class WorldGenStep_TunnelEntrances : WorldGenStep
{
  public FloatRange tunnelSitesPer100kTiles;

  public override int SeedPart => 235235252;

  public static HashSet<LandmarkDef> PossibleLandmarks
  {
    get
    {
      if (field.NullOrEmpty())
      {
        field =
        [
          LandmarkDefOf.Chasm,
          LandmarkDefOf.Cavern,
          LandmarkDefOf.Hollow,
          LandmarkDefOf.Valley
        ];
      }
      return field;
    }
  }

  public override void GenerateFresh(string seed, PlanetLayer layer)
  {
    GenerateTunnelEntrances(layer);
  }

  public void GenerateTunnelEntrances(PlanetLayer layer)
  {
    Faction faction = Faction.OfAncients;

    float viewAngleFactor = layer.Def.viewAngleSettlementsFactorCurve.Evaluate(Mathf.Clamp01(layer.ViewAngle / 180f));
    float scaleFactor = Find.World.info.overallPopulation.GetScaleFactor();
    int settlementsToGenerateCount = GenMath.RoundRandom(layer.TilesCount / 100000f * tunnelSitesPer100kTiles.RandomInRange * scaleFactor * viewAngleFactor);

    for (int index = 0; index < settlementsToGenerateCount; ++index)
    {
      PlanetTile tile = TileFinder.RandomSettlementTileFor(layer, faction);
      SpawnTunnelEntrance(tile, layer, faction);
    }
  }

  public static TunnelEntrance SpawnTunnelEntrance(PlanetTile tile, PlanetLayer layer, Faction faction)
  {
    TunnelEntrance worldObject = (TunnelEntrance) WorldObjectMaker.MakeWorldObject(DwarfFlavourPackDefOf.DFP_TunnelEntranceSite);
    worldObject.SetFaction(faction);
    worldObject.Tile = tile;
    List<SitePartDefWithParams> sitePartDefsWithParams;

    if (Faction.OfPlayerSilentFail != null)
    {
      SiteMakerHelper.GenerateDefaultParams(StorytellerUtility.DefaultSiteThreatPointsNow(), worldObject.Tile, faction, [DwarfFlavourPackDefOf.DFP_TunnelEntranceSitePart], out sitePartDefsWithParams);
    }
    else
    {
      SiteMakerHelper.GenerateDefaultParams(100, worldObject.Tile, faction, [DwarfFlavourPackDefOf.DFP_TunnelEntranceSitePart], out sitePartDefsWithParams);

    }
    worldObject.AddPart(new SitePart(worldObject, sitePartDefsWithParams[0].def, sitePartDefsWithParams[0].parms));

    worldObject.Name = SettlementNameGenerator.GenerateSettlementName(worldObject, DwarfFlavourPackDefOf.DFP_TunnelEntranceSite.nameMaker);

    if (ModsConfig.OdysseyActive)
      Find.World.landmarks.AddLandmark(PossibleLandmarks.RandomElement(), worldObject.Tile, layer, true);
    Find.WorldObjects.Add(worldObject);

    return worldObject;
  }
}