using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace DwarfFlavourPack;

public static class SurfaceTileExtensions
{
  extension(SurfaceTile tile)
  {
    public List<TunnelGenData.TunnelLink> potentialTunnels
    {
      get
      {
        if (!TunnelGenData.Instance.potentialTunnels.ContainsKey(tile))
        {
          TunnelGenData.Instance.potentialTunnels.Add(tile, []);
        }

        return TunnelGenData.Instance.potentialTunnels[tile];
      }
    }
  }
}

public class WorldDrawLayer_Tunnels: WorldDrawLayer_Paths
{
  private readonly ModuleBase tunnelDisplacementX = new Perlin(1.0, 2.0, 0.5, 3, 74173887, QualityMode.Medium);
  private readonly ModuleBase tunnelDisplacementY = new Perlin(1.0, 2.0, 0.5, 3, 67515931, QualityMode.Medium);
  private readonly ModuleBase tunnelDisplacementZ = new Perlin(1.0, 2.0, 0.5, 3, 87116801, QualityMode.Medium);

  public override bool VisibleWhenLayerNotSelected => false;

  public override bool VisibleInBackground => false;

  public override IEnumerable Regenerate()
  {
    foreach (object obj in base.Regenerate())
      yield return obj;
    
    LayerSubMesh subMesh = GetSubMesh(WorldMaterials.Roads);
    List<TunnelWorldLayerDef> tunnelLayerDefs = DefDatabase<TunnelWorldLayerDef>.AllDefs.OrderBy(def => def.order).ToList();
    for (int i = 0; i < planetLayer.TilesCount; ++i)
    {
      if (i % 1000 == 0)
        yield return null;
      if (subMesh.verts.Count > 60000)
        subMesh = GetSubMesh(WorldMaterials.Roads);
      SurfaceTile surfaceTile = (SurfaceTile) planetLayer[i];
      if (!surfaceTile.WaterCovered)
      {
        List<OutputDirection> nodes = new List<OutputDirection>();
        if (surfaceTile.potentialTunnels != null)
        {
          bool allowSmoothTransition = true;
          for (int index = 0; index < surfaceTile.potentialTunnels.Count - 1; ++index)
          {
            if (surfaceTile.potentialTunnels[index].tunnel.worldTransitionGroup != surfaceTile.potentialTunnels[index + 1].tunnel.worldTransitionGroup)
              allowSmoothTransition = false;
          }
          for (int index1 = 0; index1 < tunnelLayerDefs.Count; ++index1)
          {
            bool flag = false;
            nodes.Clear();
            for (int index2 = 0; index2 < surfaceTile.potentialTunnels.Count; ++index2)
            {
              TunnelDef tunnel = surfaceTile.potentialTunnels[index2].tunnel;
              float layerWidth = tunnel.GetLayerWidth(tunnelLayerDefs[index1]);
              if (layerWidth > 0.0)
                flag = true;
              nodes.Add(new OutputDirection
              {
                neighbor = surfaceTile.potentialTunnels[index2].neighbor,
                width = layerWidth,
                distortionFrequency = tunnel.distortionFrequency,
                distortionIntensity = tunnel.distortionIntensity
              });
            }
            if (flag)
              GeneratePaths(subMesh, new PlanetTile(i, planetLayer), nodes, tunnelLayerDefs[index1].color, allowSmoothTransition);
          }
        }
      }
    }
    FinalizeMesh(MeshParts.All);
  }

  public override Vector3 FinalizePoint(
    Vector3 inp,
    float distortionFrequency,
    float distortionIntensity)
  {
    Vector3 coordinate = inp * distortionFrequency;
    float magnitude = inp.magnitude;
    Vector3 vector3 = new Vector3(tunnelDisplacementX.GetValue(coordinate), tunnelDisplacementY.GetValue(coordinate), tunnelDisplacementZ.GetValue(coordinate));
    if (vector3.magnitude > 0.0001)
    {
      float num = (float) ((1.0 / (1.0 + Mathf.Exp((float) (-(double) vector3.magnitude / 1.0 * 2.0))) * 2.0 - 1.0) * 1.0);
      vector3 = vector3.normalized * num;
    }
    inp = (inp + vector3 * distortionIntensity).normalized * magnitude;
    return inp + inp.normalized * 0.02f;
  }
}