using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace DwarfFlavourPack;

public class JobDriver_EnterTunnel : JobDriver
{
    private TargetIndex PortalInd = TargetIndex.A;
    private const int EnterDelay = 90;

    public Building_Tunnel Tunnel => this.TargetThingA as Building_Tunnel;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        JobDriver_EnterPortal f = this;
        f.FailOnDespawnedOrNull<JobDriver_EnterPortal>(f.PortalInd);
        // ISSUE: reference to a compiler-generated method
        f.FailOn<JobDriver_EnterPortal>(new Func<bool>(f.\u003CMakeNewToils\u003Eb__5_0));
        yield return Toils_Goto.GotoThing(f.PortalInd, PathEndMode.Touch);
        Toil toil1 = Toils_General.Wait(90).FailOnCannotTouch<Toil>(f.PortalInd, PathEndMode.Touch).WithProgressBarToilDelay(f.PortalInd, true);
        // ISSUE: reference to a compiler-generated method
        toil1.tickIntervalAction += new Action<int>(f.\u003CMakeNewToils\u003Eb__5_2);
        toil1.handlingFacing = true;
        yield return toil1;
        Toil toil2 = ToilMaker.MakeToil(nameof (MakeNewToils));
        // ISSUE: reference to a compiler-generated method
        toil2.initAction = new Action(f.\u003CMakeNewToils\u003Eb__5_1);
        yield return toil2;
    }
}