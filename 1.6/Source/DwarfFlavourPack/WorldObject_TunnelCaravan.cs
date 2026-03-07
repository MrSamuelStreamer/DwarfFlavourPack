using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

public class WorldObject_TunnelCaravan : WorldObject, IIncidentTarget, ILoadReferenceable
{
  public TunnelCaravan caravan;

  public WorldObject_TunnelCaravan()
  {
      storyState = new StoryState(this);
  }

  public override Vector3 DrawPos
  {
    get
    {
      if (caravan == null || caravan.pathTiles.NullOrEmpty())
        return base.DrawPos;

      float progress = (float) (Find.TickManager.TicksGame - caravan.travelStartsAtTick) / (caravan.travelEndsAtTick - caravan.travelStartsAtTick);
      progress = Mathf.Clamp01(progress);

      int pathCount = caravan.pathTiles.Count;
      if (pathCount < 2)
        return Find.WorldGrid.GetTileCenter(Tile);

      float floatIndex = progress * (pathCount - 1);
      int index = Mathf.FloorToInt(floatIndex);
      float t = floatIndex - index;

      if (index >= pathCount - 1)
        return Find.WorldGrid.GetTileCenter(caravan.pathTiles[pathCount - 1]);

      Vector3 startPos = Find.WorldGrid.GetTileCenter(caravan.pathTiles[index]);
      Vector3 endPos = Find.WorldGrid.GetTileCenter(caravan.pathTiles[index + 1]);

      Vector3 pos = Vector3.Slerp(startPos, endPos, t);

      return FinalizePoint(pos);
    }
  }

  private Vector3 FinalizePoint(Vector3 inp)
  {
    return inp + inp.normalized * 0.015f;
  }

  protected override void Tick()
  {
    base.Tick();
    if (caravan == null || caravan.done)
    {
      Destroy();
      return;
    }

    float progress = (float) (Find.TickManager.TicksGame - caravan.travelStartsAtTick) / (caravan.travelEndsAtTick - caravan.travelStartsAtTick);
    progress = Mathf.Clamp01(progress);
    if (!caravan.pathTiles.NullOrEmpty())
    {
      int index = Mathf.FloorToInt(progress * (caravan.pathTiles.Count - 1));
      Tile = caravan.pathTiles[Mathf.Clamp(index, 0, caravan.pathTiles.Count - 1)];
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_References.Look(ref caravan, "caravan");
    Scribe_Deep.Look(ref storyState, "storyState", this);

  }

  public override string GetInspectString()
  {
    return caravan?.Progress ?? base.GetInspectString();
  }

  public StoryState storyState;
  public StoryState StoryState => storyState;
  
  public GameConditionManager GameConditionManager
  {
    get
    {
      Log.ErrorOnce("Attempted to retrieve condition manager directly from caravan", 13291050);
      return null;
    }
  }
  

  public float PlayerWealthForStoryteller
  {
    get
    {
      float num = 0.0f;
      foreach (var t in PlayerPawnsForStoryteller)
      {
        num += WealthWatcher.GetEquipmentApparelAndInventoryWealth(t);
        if (t.Faction == Faction.OfPlayer)
        {
          float marketValue = t.MarketValue;
          if (t.IsSlave)
            marketValue *= 0.75f;
          num += marketValue;
        }
      }
      return num * 0.7f;
    }
  }

  public IEnumerable<Pawn> PlayerPawnsForStoryteller
  {
    get
    {
      return caravan.GetDirectlyHeldThings().OfType<Pawn>().Where(x => x.Faction == Faction.OfPlayer);
    }
  }

  public FloatRange IncidentPointsRandomFactorRange => StorytellerUtility.CaravanPointsRandomFactorRange;

  public int ConstantRandSeed => ID ^ 728249981;
}