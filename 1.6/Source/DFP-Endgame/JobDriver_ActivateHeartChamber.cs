using System.Collections.Generic;
using EndGame;
using RimWorld;
using Verse;
using Verse.AI;

namespace DwarfFlavourPack.Endgame;

public class JobDriver_ActivateHeartChamber : JobDriver
{
    private CompEndGame CompEndGame => TargetThingA.TryGetComp<CompEndGame>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, errorOnFailed: errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

        Toil activate = ToilMaker.MakeToil("ActivateHeartChamber");
        activate.defaultCompleteMode = ToilCompleteMode.Instant;
        activate.initAction = () =>
        {
            bool hasWraithguard = pawn.apparel?.WornApparel
                .Any(a => a.def.defName == "MSSD_Wraithguard") ?? false;

            if (!hasWraithguard)
            {
                GenExplosion.DoExplosion(
                    center: pawn.Position,
                    map: pawn.Map,
                    radius: 3.9f,
                    damType: DamageDefOf.Bomb,
                    instigator: TargetThingA,
                    explosionSound: DwarfFlavourPackDefOf.DFP_Explosion
                );

                if (!pawn.Dead)
                    pawn.Kill(null);

                Find.LetterStack.ReceiveLetter(
                    "DFP_HeartChamberDeath_Label".Translate(pawn.Named("PAWN")),
                    "DFP_HeartChamberDeath_Text".Translate(pawn.Named("PAWN")),
                    LetterDefOf.Death,
                    new LookTargets(pawn));

                return;
            }

            CompEndGame comp = CompEndGame;
            if (comp == null || !comp.IsActivatingPossible)
                return;

            DiaNode node = new DiaNode("EndGameWarning".Translate());

            DiaOption confirm = new DiaOption("Confirm".Translate());
            confirm.action = () => comp.StartEndGame();
            confirm.resolveTree = true;
            node.options.Add(confirm);

            DiaOption goBack = new DiaOption("GoBack".Translate());
            goBack.resolveTree = true;
            node.options.Add(goBack);

            Find.WindowStack.Add(new Dialog_NodeTree(node));
        };
        yield return activate;
    }
}
