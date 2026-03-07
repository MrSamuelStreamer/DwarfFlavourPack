using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DwarfFlavourPack.Comps;

public class FormTunnelCaravanComp: FormCaravanComp
{
  public TunnelCaravansBattlefield Battlefield => (TunnelCaravansBattlefield) parent;
  public WorldObject_TunnelCaravan Caravan => Battlefield.Caravan;
  
  public override IEnumerable<Gizmo> GetGizmos()
  {
    MapParent mapParent = (MapParent) parent;
    if (mapParent.HasMap)
    {
      if (Reform && CanFormOrReformCaravanNow && mapParent.Map.mapPawns.FreeColonistsSpawnedCount != 0)
      {
        Command_Action gizmo = new Command_Action
        {
          defaultLabel = "CommandReformCaravan".Translate(),
          defaultDesc = "CommandReformCaravanDesc".Translate(),
          icon = FormCaravanCommand,
          hotKey = KeyBindingDefOf.Misc2,
          tutorTag = "ReformCaravan",
          action = () =>
          {
            Find.WindowStack.Add(new Dialog_EnterTunnel(Battlefield, Caravan));
          }
        };
        if (GenHostility.AnyHostileActiveThreatToPlayer(mapParent.Map, true))
          gizmo.Disable("CommandReformCaravanFailHostilePawns".Translate());
        yield return gizmo;
      }
    }
  }
}