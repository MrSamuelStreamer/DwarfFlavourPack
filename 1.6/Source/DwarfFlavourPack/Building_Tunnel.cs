using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace DwarfFlavourPack;

[StaticConstructorOnStartup]
public class Building_Tunnel: Building, IThingHolder
{
  private static readonly Texture2D ViewPocketMapTex = ContentFinder<Texture2D>.Get("UI/Commands/ViewCave");
  private static readonly Texture2D CancelEnterTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
  private static readonly Texture2D DefaultEnterTex = ContentFinder<Texture2D>.Get("UI/Commands/EnterCave");

  public SurfaceTile target;
  
  public List<TransferableOneWay> leftToLoad;
  public TunnelCaravan caravan;
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

  public bool AnyPawnCanLoadAnythingNow
  {
    get
    {
      if (!LoadInProgress || !Spawned)
        return false;
      IReadOnlyList<Pawn> allPawnsSpawned = Map.mapPawns.AllPawnsSpawned;
      foreach (var t in allPawnsSpawned)
      {
        if (t.CurJobDef == JobDefOf.HaulToPortal && ((JobDriver_HaulToTunnel) t.jobs.curDriver).Tunnel == this || t.CurJobDef == JobDefOf.EnterPortal && ((JobDriver_EnterTunnel) t.jobs.curDriver).Tunnel == this)
          return true;
      }
      if ((from t in allPawnsSpawned let thing = t.mindState?.duty?.focus.Thing where thing != null && thing == this && t.CanReach((LocalTargetInfo) thing, PathEndMode.Touch, Danger.Deadly) select t).Any())
      {
        return true;
      }

      return allPawnsSpawned.Any(t => t.IsColonist && TunnelUtilities.HasJobOnTunnel(t, this));
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
    caravan = new TunnelCaravan
    {
      tunnel = this
    };
  }

  protected override void Tick()
  {
    base.Tick();
    if (!this.IsHashIntervalTick(60) || !Spawned || !LoadInProgress || notifiedCantLoadMore || AnyPawnCanLoadAnythingNow || leftToLoad[0]?.AnyThing == null)
      return;
    notifiedCantLoadMore = true;
    Messages.Message("MessageCantLoadMoreIntoPortal".Translate((NamedArgument) Label, (NamedArgument) Faction.OfPlayer.def.pawnsPlural, (NamedArgument) leftToLoad[0].AnyThing), (Thing) this, MessageTypeDefOf.CautionInput);
  }

  public void GetChildHolders(List<IThingHolder> outChildren)
  {
  }

  public ThingOwner GetDirectlyHeldThings() => caravan.GetDirectlyHeldThings();

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
    Lord oldLord = Map.lordManager.lords.FirstOrDefault((Predicate<Lord>) (l => l.LordJob is LordJob_LoadAndEnterTunnel lordJob && lordJob.tunnel == this));
    if (oldLord != null)
      Map.lordManager.RemoveLord(oldLord);
    leftToLoad.Clear();
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
    Building_Tunnel mapPortal = this;
    foreach (Gizmo gizmo in base.GetGizmos())
      yield return gizmo;
    Command_Action gizmo1 = new Command_Action();
    yield return new Command_Action
    {
      action = delegate
      {
        Dialog_EnterTunnel dialog_EnterPortal = new Dialog_EnterTunnel(this);
        Find.WindowStack.Add(dialog_EnterPortal);
      },
      icon = this.EnterTex,
      defaultLabel = this.EnterString + "...",
      defaultDesc = "CommandEnterPortalDesc".Translate(this.Label),
      Disabled = !this.IsEnterable(out string text),
      disabledReason = text
    };
    
    if (mapPortal.LoadInProgress)
    {
      Command_Action gizmo2 = new Command_Action();
      gizmo2.action = mapPortal.CancelLoad;
      gizmo2.icon = CancelEnterTex;
      gizmo2.defaultLabel = mapPortal.CancelEnterString;
      gizmo2.defaultDesc = "CommandCancelEnterPortalDesc".Translate();
      yield return gizmo2;
    }
  }

}