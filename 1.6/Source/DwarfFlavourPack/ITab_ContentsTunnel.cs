using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace DwarfFlavourPack;

public class ITab_ContentsTunnel : ITab_ContentsBase
{
  private List<Thing> tmpContainer = new List<Thing>();
  private Vector2 scroll;
  private float lastHeight;

  public override IList<Thing> container => tmpContainer;

  public override bool UseDiscardMessage => false;

  public Building_Tunnel Tunnel => SelThing as Building_Tunnel;

  public override bool IsVisible => Tunnel != null && Tunnel.LoadInProgress;

  public override IntVec3 DropOffset => IntVec3.Zero;

  public ITab_ContentsTunnel()
  {
    labelKey = "TabMapPortalContents";
    containedItemsKey = "";
  }

  protected override void DoItemsLists(Rect inRect, ref float curY)
  {
    Text.Font = GameFont.Small;
    bool flag = false;
    float curY1 = 0.0f;
    Widgets.BeginGroup(inRect);
    Rect viewRect = inRect with
    {
      y = 0.0f,
      height = lastHeight
    };
    if (lastHeight > (double) inRect.height)
      viewRect.width -= 16f;
    Widgets.BeginScrollView(inRect, ref scroll, viewRect);
    Widgets.ListSeparator(ref curY1, inRect.width, Tunnel.EnteringString);
    if (Tunnel.leftToLoad != null)
    {
      foreach (TransferableOneWay t in Tunnel.leftToLoad.Where(t => t.CountToTransfer > 0 && t.HasAnyThing))
      {
        flag = true;
        TransferableOneWay t1 = t;
        DoThingRow(t.ThingDef, t.CountToTransfer, t.things, viewRect.width, ref curY1, x => OnDropToLoadThing(t1, x));
      }
    }
    lastHeight = curY1;
    Widgets.EndScrollView();
    if (!flag)
      Widgets.NoneLabel(ref curY1, inRect.width);
    Widgets.EndGroup();
  }

  protected override void OnDropThing(Thing t, int count)
  {
    base.OnDropThing(t, count);
    if (!(t is Pawn pawn))
      return;
    RemovePawnFromLoadLord(pawn);
  }

  private void RemovePawnFromLoadLord(Pawn pawn)
  {
    Lord lord = pawn.GetLord();
    if (lord is not { LordJob: LordJob_LoadAndEnterTunnel })
      return;
    lord.Notify_PawnLost(pawn, PawnLostCondition.LeftVoluntarily);
  }

  private void OnDropToLoadThing(TransferableOneWay t, int count)
  {
    t.ForceTo(t.CountToTransfer - count);
    EndJobForEveryoneHauling(t);
    foreach (Thing thing in t.things)
    {
      if (thing is Pawn pawn)
        RemovePawnFromLoadLord(pawn);
    }
  }

  private void EndJobForEveryoneHauling(TransferableOneWay t)
  {
    IReadOnlyList<Pawn> allPawnsSpawned = SelThing.Map.mapPawns.AllPawnsSpawned;
    foreach (Pawn t1 in allPawnsSpawned)
    {
      if (t1.CurJobDef == DwarfFlavourPackDefOf.DFP_HaulToTunnel)
      {
        JobDriver_HaulToTunnel curDriver = (JobDriver_HaulToTunnel) t1.jobs.curDriver;
        if (curDriver.Tunnel == Tunnel && curDriver.ThingToCarry != null && curDriver.ThingToCarry.def == t.ThingDef)
          t1.jobs.EndCurrentJob(JobCondition.InterruptForced);
      }
    }
  }
}