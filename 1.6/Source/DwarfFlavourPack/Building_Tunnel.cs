using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace DwarfFlavourPack;

[StaticConstructorOnStartup]
public class Building_Tunnel : Building, IThingHolder
{
  public ThingOwner<TunnelCaravan> innerContainer;

  private static readonly Texture2D ViewPocketMapTex = ContentFinder<Texture2D>.Get("UI/Commands/ViewCave");
  private static readonly Texture2D CancelEnterTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
  private static readonly Texture2D DefaultEnterTex = ContentFinder<Texture2D>.Get("UI/Commands/EnterCave");

  public SurfaceTile target;

  public List<TransferableOneWay> leftToLoad;

  public bool notifiedCantLoadMore;
  private int lastLoadPossibleTick = -1;

  protected virtual Texture2D EnterTex => DefaultEnterTex;

  public virtual string EnterString => "EnterPortal".Translate((NamedArgument) Label);

  public virtual string CancelEnterString => "CommandCancelEnterPortal".Translate();

  public virtual string EnteringString
  {
    get => "EnteringPortal".Translate((NamedArgument) Label);
  }

  public bool LoadInProgress
  {
    get => leftToLoad != null && leftToLoad.Any();
  }

  private TunnelCaravan caravan;
  public TunnelCaravan Caravan
  {
    get
    {
      innerContainer ??= new ThingOwner<TunnelCaravan>(this);

      // Prefer an already-held instance (e.g. after load)
      if (caravan == null)
        caravan = innerContainer.FirstOrDefault<TunnelCaravan>();

      if (caravan == null)
      {
        caravan = (TunnelCaravan) ThingMaker.MakeThing(DwarfFlavourPackDefOf.DFP_TunnelCaravan);
        caravan.tunnel = this;
        caravan.origin = Tile;

        // IMPORTANT: do NOT spawn it on the map. It must live inside innerContainer.
        innerContainer.TryAddOrTransfer(caravan);
      }
      else
      {
        // Safety: if something ever spawned it anyway, despawn before keeping it in the container.
        if (caravan.Spawned)
          caravan.DeSpawn();
      }

      return caravan;
    }
  }

  public bool AnyPawnCanLoadAnythingNow
  {
    get => TunnelUtilities.AnyPawnCanLoadAnythingNow(this);
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref leftToLoad, "leftToLoad", LookMode.Deep);
    Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
    Scribe_Values.Look(ref notifiedCantLoadMore, "notifiedCantLoadMore", false);
    Scribe_Values.Look(ref lastLoadPossibleTick, "lastLoadPossibleTick", -1);
    if (Scribe.mode != LoadSaveMode.PostLoadInit)
      return;
    leftToLoad?.RemoveAll(x => x.AnyThing == null);
  }

  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    WorldGenStep_Tunnels tunnels = new WorldGenStep_Tunnels();
    if (!tunnels.RegenerateNeeded(map.Tile))
      return;

    LongEventHandler.QueueLongEvent(
      () =>
      {
        tunnels.GenerateFresh(Find.World.info.seedString, Find.World.grid.Surface);
        Find.World.renderer.AllDrawLayers.First(layer => layer is WorldDrawLayer_Tunnels).SetDirty();
      },
      "DwarfFlavourPack_TunnelRegen",
      false,
      exception =>
      {
        ModLog.Error(exception.ToString());
      }
    );
  }

  protected override void Tick()
  {
    base.Tick();

    if (ShouldSendCaravanNow())
    {
      TunnelGenData.Instance.SendCaravan(this);
      return;
    }

    if (!Spawned || !LoadInProgress)
    {
      lastLoadPossibleTick = -1;
      return;
    }

    if (AnyPawnCanLoadAnythingNow)
    {
      lastLoadPossibleTick = Find.TickManager.TicksGame;
      notifiedCantLoadMore = false; // Reset so if it gets stuck later, it can warn again
      return;
    }

    if (!this.IsHashIntervalTick(60))
      return;

    // If we've never been able to load, or it's been more than 10 seconds (600 ticks)
    if (lastLoadPossibleTick != -1 && (Find.TickManager.TicksGame - lastLoadPossibleTick) < 600)
      return;

    if (leftToLoad is not { Count: > 0 } || leftToLoad[0]?.AnyThing == null)
      return;

    if (notifiedCantLoadMore)
      return;

    notifiedCantLoadMore = true;
    Messages.Message("MessageCantLoadMoreIntoPortal".Translate((NamedArgument) Label, (NamedArgument) Faction.OfPlayer.def.pawnsPlural, (NamedArgument) leftToLoad[0].AnyThing), (Thing) this, MessageTypeDefOf.CautionInput);
  }

  public bool ShouldSendCaravanNow()
  {
    if (!Caravan.GetDirectlyHeldThings().OfType<Pawn>().Any())
      return false;

    if (Caravan.readyToSend)
      return true;

    if (!LoadInProgress)
    {
      // If no items left to load, check if all pawns from the Lord are inside.
      Lord lord = Map.lordManager.lords.FirstOrDefault(l => l.LordJob is LordJob_LoadAndEnterTunnel lordJob && lordJob.tunnel == this);
      if (lord == null)
      {
        // No lord means no pawns are assigned to enter voluntarily, 
        // OR the process was just starting.
        // If we were loading and now we are not, and there's no lord, we might be ready.
        // But we only want to auto-send if there WAS a loading process.
        return Caravan.GetDirectlyHeldThings().Any;
      }

      // If there is a lord, check if all its owned pawns are in the caravan container.
      foreach (Pawn pawn in lord.ownedPawns)
      {
        if (!Caravan.GetDirectlyHeldThings().Contains(pawn))
          return false;
      }

      return true;
    }

    return false;
  }

  public void GetChildHolders(List<IThingHolder> outChildren)
  {
    // The tunnel holds the caravan Thing (innerContainer),
    // and the caravan holds the actual loaded items/pawns.
    ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, innerContainer);
  }

  public ThingOwner GetDirectlyHeldThings()
  {
    // IMPORTANT:
    // Haul-to-container jobs will deposit into this ThingOwner.
    // That must be the caravan's ThingOwner<Thing>, not ThingOwner<TunnelCaravan>.
    return Caravan.GetDirectlyHeldThings();
  }

  public void Notify_ThingAdded(Thing t) => SubtractFromToLoadList(t, t.stackCount);

  public void AddToTheToLoadList(TransferableOneWay t, int count)
  {
    if (!t.HasAnyThing || count <= 0)
      return;
    if (leftToLoad == null)
      leftToLoad = new List<TransferableOneWay>();
    TransferableOneWay transferableOneWay1 = TransferableUtility.TransferableMatching(t.AnyThing, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
    if (transferableOneWay1 != null)
    {
      for (int index = 0; index < t.things.Count; ++index)
      {
        if (!transferableOneWay1.things.Contains(t.things[index]))
          transferableOneWay1.things.Add(t.things[index]);
      }
      if (!transferableOneWay1.CanAdjustBy(count).Accepted)
        return;
      transferableOneWay1.AdjustBy(count);
    }
    else
    {
      TransferableOneWay transferableOneWay2 = new TransferableOneWay();
      leftToLoad.Add(transferableOneWay2);
      transferableOneWay2.things.AddRange(t.things);
      transferableOneWay2.AdjustTo(count);
    }
  }

  public int SubtractFromToLoadList(Thing t, int count)
  {
    if (leftToLoad == null)
      return 0;
    TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(t, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
    if (transferableOneWay == null || transferableOneWay.CountToTransfer <= 0)
      return 0;
    int loadList = Mathf.Min(count, transferableOneWay.CountToTransfer);
    transferableOneWay.AdjustBy(-loadList);
    transferableOneWay.things.Remove(t);
    if (transferableOneWay.CountToTransfer <= 0)
      leftToLoad.Remove(transferableOneWay);

    if (leftToLoad.Count <= 0)
    {
      // We no longer set readyToSend here, ShouldSendCaravanNow will handle it.
    }

    return loadList;
  }

  public void CancelLoad()
  {
    Lord oldLord = Map.lordManager.lords.FirstOrDefault(l => l.LordJob is LordJob_LoadAndEnterTunnel lordJob && lordJob.tunnel == this);
    if (oldLord != null)
      Map.lordManager.RemoveLord(oldLord);
    leftToLoad.Clear();
    Caravan.GetDirectlyHeldThings().TryDropAll(Position, Map, ThingPlaceMode.Near);
  }

  public void ClearCaravan()
  {
    Lord oldLord = Map.lordManager.lords.FirstOrDefault(l => l.LordJob is LordJob_LoadAndEnterTunnel lordJob && lordJob.tunnel == this);
    if (oldLord != null)
      Map.lordManager.RemoveLord(oldLord);
    // If the caravan was transferred out (to the world component ThingOwner),
    // make sure we don't keep a stale reference in this building's container.
    if (innerContainer != null)
    {
      if (caravan != null)
      {
        innerContainer.Remove(caravan);
      }
      else if (innerContainer.Count > 0)
      {
        // Defensive: the container should only ever hold the one caravan instance.
        // Remove anything left behind so we don't duplicate on next access.
        while (innerContainer.Count > 0)
          innerContainer.Remove(innerContainer[0]);
      }
    }

    caravan = null;
    leftToLoad?.Clear();
    notifiedCantLoadMore = false;
    lastLoadPossibleTick = -1;
  }

  public override string GetInspectString()
  {
    var loaded = Caravan.GetDirectlyHeldThings().ToList();

    if (loaded.Count == 0)
      return base.GetInspectString();

    string result = base.GetInspectString();
    if (!result.NullOrEmpty())
      result += "Contains:\n";

    result += string.Join("\n", loaded.Select(thing => " - " + thing.LabelCap));

    return result;
  }

  public virtual bool IsEnterable(out string reason)
  {
    foreach (ThingComp allComp in AllComps)
    {
      AcceptanceReport acceptanceReport = allComp.CanEnterPortal();
      if (!acceptanceReport.Accepted)
      {
        reason = acceptanceReport.Reason;
        return false;
      }
    }
    reason = "";
    return true;
  }

  public override IEnumerable<Gizmo> GetGizmos()
  {
    foreach (Gizmo gizmo in base.GetGizmos())
      yield return gizmo;
    yield return new Command_Action
    {
      action = delegate
      {
        List<WorldObject> wos = TunnelGenData.WorldObjectsWithTunnelEntrances().Where(wo => wo != null).ToList();

        wos.Remove(Find.WorldObjects.SiteAt(Map.Tile));

        FloatMenuUtility.MakeMenu(
          wos,
          entrance =>
          {
            var distance = Find.WorldGrid.ApproxDistanceInTiles(Tile, entrance.Tile);
            return entrance.LabelCap + " [" + distance.ToStringDecimalIfSmall() + " tiles]";
          },
          entrance =>
          {
            return () =>
            {
              Find.WindowStack.Add(new Dialog_EnterTunnel(this, entrance));
            };
          }
        );
      },
      icon = EnterTex,
      defaultLabel = EnterString + "...",
      defaultDesc = "CommandEnterPortalDesc".Translate(Label),
      Disabled = !IsEnterable(out string text),
      disabledReason = text
    };

    if (LoadInProgress || Caravan.GetDirectlyHeldThings().Any())
    {
      Command_Action gizmo2 = new Command_Action();
      gizmo2.action = CancelLoad;
      gizmo2.icon = CancelEnterTex;
      gizmo2.defaultLabel = CancelEnterString;
      gizmo2.defaultDesc = "CommandCancelEnterPortalDesc".Translate();
      yield return gizmo2;
    }
  }

}