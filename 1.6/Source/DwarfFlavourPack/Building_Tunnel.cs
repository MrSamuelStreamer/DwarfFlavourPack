using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace DwarfFlavourPack;

[StaticConstructorOnStartup]
public class Building_Tunnel: Building, IThingHolder
{
  private static readonly Texture2D ViewPocketMapTex = ContentFinder<Texture2D>.Get("UI/Commands/ViewCave");
  private static readonly Texture2D CancelEnterTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
  private static readonly Texture2D DefaultEnterTex = ContentFinder<Texture2D>.Get("UI/Commands/EnterCave");

  public SurfaceTile target;
  
  public List<TransferableOneWay> leftToLoad;
  private ThingOwner<TunnelCaravan> innerContainer;
  
  public bool notifiedCantLoadMore;

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

  public TunnelCaravan Caravan
  {
    get
    {
      if (innerContainer.Count <= 0)
      {
        TunnelCaravan caravan = (TunnelCaravan)ThingMaker.MakeThing(DwarfFlavourPackDefOf.DFP_TunnelCaravan);
        caravan.tunnel = this;
        caravan.origin = Tile;
        innerContainer.TryAdd(caravan);
      }

      return innerContainer[0];
    }
  }
  
  public bool AnyPawnCanLoadAnythingNow
  {
    get
    {
      if (!LoadInProgress || !Spawned)
        return false;
      IReadOnlyList<Pawn> allPawnsSpawned = Map.mapPawns.AllPawnsSpawned;
      for (int index = 0; index < allPawnsSpawned.Count; ++index)
      {
        if (allPawnsSpawned[index].CurJobDef == DwarfFlavourPackDefOf.DFP_HaulToTunnel && ((JobDriver_HaulToTunnel) allPawnsSpawned[index].jobs.curDriver).Tunnel == this || allPawnsSpawned[index].CurJobDef == DwarfFlavourPackDefOf.DFP_EnterTunnel && ((JobDriver_EnterTunnel) allPawnsSpawned[index].jobs.curDriver).Tunnel == this)
          return true;
      }
      for (int index = 0; index < allPawnsSpawned.Count; ++index)
      {
        Thing thing = allPawnsSpawned[index].mindState?.duty?.focus.Thing;
        if (thing != null && thing == this && allPawnsSpawned[index].CanReach((LocalTargetInfo) thing, PathEndMode.Touch, Danger.Deadly))
          return true;
      }
      for (int index = 0; index < allPawnsSpawned.Count; ++index)
      {
        if (allPawnsSpawned[index].IsColonist && TunnelUtilities.HasJobOnTunnel(allPawnsSpawned[index], this))
          return true;
      }
      return false;
    }
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Collections.Look(ref leftToLoad, "leftToLoad", LookMode.Deep);
    if (Scribe.mode != LoadSaveMode.PostLoadInit)
      return;
    leftToLoad?.RemoveAll(x => x.AnyThing == null);
  }

  public override void SpawnSetup(Map map, bool respawningAfterLoad)
  {
    base.SpawnSetup(map, respawningAfterLoad);
    innerContainer = new ThingOwner<TunnelCaravan>();
  }

  protected override void Tick()
  {
    base.Tick();
    if (!this.IsHashIntervalTick(60)) 
      return;
    if(!Spawned)
      return;
    if(!LoadInProgress)
      return;
    if(notifiedCantLoadMore)
      return;
    if(AnyPawnCanLoadAnythingNow)
      return;
    if (leftToLoad[0]?.AnyThing == null)
    {
      if (Caravan.destination != null)
      {
        TunnelGenData.Instance.SendCaravan(Caravan, this);
        notifiedCantLoadMore = false;
        leftToLoad = null;
      }
    }
    notifiedCantLoadMore = true;
    Messages.Message("MessageCantLoadMoreIntoPortal".Translate((NamedArgument) Label, (NamedArgument) Faction.OfPlayer.def.pawnsPlural, (NamedArgument) leftToLoad[0].AnyThing), (Thing) this, MessageTypeDefOf.CautionInput);
  }

  public void GetChildHolders(List<IThingHolder> outChildren)
  {
    ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
  }

  public ThingOwner GetDirectlyHeldThings()
  {
    return innerContainer;
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
        FloatMenuUtility.MakeMenu(
          Find.WorldObjects.AllWorldObjects.OfType<TunnelEntrance>(),
          entrance => entrance.Label,
          entrance =>
          {
            return ()=>
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
    
    if (LoadInProgress)
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