using System;
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

    public override void GenerateFresh(string seed, PlanetLayer layer)
    {
        GenerateTunnelEntrances(layer);
    }
    
    public void GenerateTunnelEntrances(PlanetLayer layer)
    {
        Faction faction = Faction.OfAncients;
        
        float viewAngleFactor = layer.Def.viewAngleSettlementsFactorCurve.Evaluate(Mathf.Clamp01(layer.ViewAngle / 180f));
        float randomInRange = tunnelSitesPer100kTiles.RandomInRange;
        float scaleFactor = Find.World.info.overallPopulation.GetScaleFactor();
        int settlementsToGenerateCount = GenMath.RoundRandom(layer.TilesCount / 100000f * randomInRange * scaleFactor * viewAngleFactor) - Find.WorldObjects.AllSettlementsOnLayer(layer).Count;
        
        for (int index = 0; index < settlementsToGenerateCount; ++index)
        {
            WorldObject worldObject = WorldObjectMaker.MakeWorldObject(DwarfFlavourPackDefOf.DFP_TunnelEntranceSite);
            worldObject.SetFaction(faction);
            worldObject.Tile = TileFinder.RandomSettlementTileFor(layer, faction);
            if (worldObject is INameableWorldObject nameableWorldObject)
                nameableWorldObject.Name = SettlementNameGenerator.GenerateSettlementName(worldObject);
            Find.WorldObjects.Add(worldObject);
        }
    }
}