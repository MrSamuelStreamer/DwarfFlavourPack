using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

[StaticConstructorOnStartup]
public class Building_Annunciator : Building
{
    private static Material heartOrb;

    static Building_Annunciator()
    {
        heartOrb = MaterialPool.MatFrom("Things/Buildings/HeartHolder_Orb", ShaderDatabase.Cutout);
    }
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        if (!this.Spawned)
            return;
        float b = 0.5f * (1f + Mathf.Sin(6.2831853f * GenTicks.TicksGame / 500f)) * 0.30000001192092896f;
        drawLoc.z += b;
        drawLoc.y += 0.03658537f;
        Vector3 s = new Vector3(this.def.graphicData.drawSize.x, 1f, this.def.graphicData.drawSize.y);
        Graphics.DrawMesh(MeshPool.plane10Back, Matrix4x4.TRS(drawLoc, this.Rotation.AsQuat, s), heartOrb, 0);
    }
}