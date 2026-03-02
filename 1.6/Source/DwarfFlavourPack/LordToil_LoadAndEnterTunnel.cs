using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace DwarfFlavourPack;

public class LordToil_LoadAndEnterTunnel: LordToil
{
    public Building_Tunnel tunnel;

    public override bool AllowSatisfyLongNeeds => false;

    public LordToil_LoadAndEnterTunnel(Building_Tunnel tunnel) => this.tunnel = tunnel;

    public override void UpdateAllDuties()
    {
        for (int index = 0; index < this.lord.ownedPawns.Count; ++index)
            this.lord.ownedPawns[index].mindState.duty = new PawnDuty(DutyDefOf.LoadAndEnterPortal)
            {
                focus = new LocalTargetInfo((Thing) this.tunnel)
            };
    }
}