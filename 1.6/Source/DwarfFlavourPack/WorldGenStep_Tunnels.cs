using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace DwarfFlavourPack;

/// <summary>
/// World-gen step that lays down a network of "tunnels" by reusing RimWorld's road-generation approach:
/// 1) pick endpoint tiles ("nodes")
/// 2) compute candidate links between nearby nodes using path costs
/// 3) choose a mostly-spanning set of links (plus a few extras) so everything is connected
/// 4) draw the chosen links onto the world as RoadDefs (your mod can interpret these as tunnels)
/// </summary>
public class WorldGenStep_Tunnels : WorldGenStep
{
  // Controls how many extra endpoints get sprinkled in, scaled by world size (tiles / 100k).
  private static readonly FloatRange ExtraTunnelNodesPer100kTiles = new FloatRange(50f, 100f);

  // Offsets endpoints away from settlements by a few neighbor-steps, so links aren't always settlement-to-settlement.
  // Negative values are clamped to 0 by Mathf.Max below (so "towards" isn't really used).
  private static readonly IntRange TunnelistanceFromSettlement = new IntRange(-1, 1);

  // Probability of allowing an extra connection even if it would create a cycle (i.e., not needed for spanning tree).
  private const float ChanceExtraNonSpanningTreeLink = 0.05f;

  // Probability of *not* adding a prospective link even if we decided it's eligible.
  // (This creates some missing connections so the network isn't too dense/regular.)
  private const float ChanceHideSpanningTreeLink = 0.1f;

  // Kept from the original constants set; not used by this implementation.
  private const float ChanceWorldObjectReclusive = 0.05f;

  // For each endpoint, we look for up to this many nearby endpoints to propose links to.
  private const int PotentialSpanningTreeLinksPerSettlement = 8;

  // SeedPart must be stable so this step has deterministic randomness relative to other steps.
  public override int SeedPart => 1538472345;

  public override void GenerateFresh(string seed, PlanetLayer layer)
  {
    // On a fresh generation, we decide which tiles are "nodes" (endpoints) first...
    GenerateTunnelEndpoints(layer);

    // ...then we build the network with deterministic randomness derived from the world seed.
    Rand.PushState();
    int seedFromSeedString = GenText.StableStringHash(seed);
    Rand.Seed = Gen.HashCombineInt(seedFromSeedString, SeedPart);
    GenerateTunnelNetwork(layer);
    Rand.PopState();
  }

  public override void GenerateWithoutWorldData(string seed, PlanetLayer layer)
  {
    // When world data isn't available, we skip endpoint selection and only generate links
    // using whatever tunnelNodes were already set up elsewhere.
    Rand.PushState();
    int seedFromSeedString = GenText.StableStringHash(seed);
    Rand.Seed = Gen.HashCombineInt(seedFromSeedString, SeedPart);
    GenerateTunnelNetwork(layer);
    Rand.PopState();
  }

  private void GenerateTunnelEndpoints(PlanetLayer layer)
  {
    // Start with a subset of existing world objects' tiles as candidate nodes.
    // The Rand.Value > 0.05f check randomly filters out ~5% of objects, to reduce clustering/density.
    List<PlanetTile> candidateNodes = Find.WorldObjects.AllWorldObjects
      .Where(wo => Rand.Value > ChanceWorldObjectReclusive && wo.Tile.Layer == layer)
      .Select(wo => wo.Tile)
      .ToList();

    // Add additional random "settlement-like" tiles to increase network complexity with world size.
    // TilesCount/100000 scales the extra count for small vs huge worlds.
    int num1 = GenMath.RoundRandom(Find.WorldGrid.TilesCount / 100000f * ExtraTunnelNodesPer100kTiles.RandomInRange);
    for (int index = 0; index < num1; ++index)
      candidateNodes.Add(TileFinder.RandomSettlementTileFor(layer, null));

    // Temporary buffer reused for neighbor queries to avoid allocations in the inner loop.
    List<PlanetTile> planetTileList = new List<PlanetTile>();

    // For each candidate endpoint, optionally "walk" a few steps to a nearby tile.
    // This shifts nodes away from the exact settlement/object tile, producing less uniform paths.
    for (int index1 = 0; index1 < candidateNodes.Count; ++index1)
    {
      // Negative values are clamped out, so we effectively walk 0..4 tiles.
      int num2 = Mathf.Max(0, TunnelistanceFromSettlement.RandomInRange);

      PlanetTile planetTile = candidateNodes[index1];
      for (int index2 = 0; index2 < num2; ++index2)
      {
        // Get neighbors of the current tile and pick one at random to step to.
        Find.WorldGrid.GetTileNeighbors(planetTile, planetTileList);
        planetTile = planetTileList.RandomElement();
      }

      // Only accept the shifted endpoint if it is reachable from the original endpoint.
      // This avoids generating nodes on unreachable pockets (e.g., separated by impassable barriers).
      if (Find.WorldReachability.CanReach(candidateNodes[index1], planetTile))
        candidateNodes[index1] = planetTile;
    }

    // Remove duplicates so we don't build redundant nodes/links.
    List<PlanetTile> distinctCandidateNodes = candidateNodes.Distinct().ToList();

    // Store nodes into the world generation data; later steps read from here.
    TunnelGenData.Instance.tunnelNodes[layer] = distinctCandidateNodes;
  }

  private void GenerateTunnelNetwork(PlanetLayer layer)
  {
    // Ensure the path grid has up-to-date perceived costs for this layer before pathing/flooding.
    Find.WorldPathGrid.RecalculateLayerPerceivedPathCosts(layer, 0);

    // 1) build candidate links between nearby endpoints...
    // 2) reduce them to a mostly-connected final link set...
    // 3) draw them onto the world as "roads" (your tunnels).
    List<Link> finalLinks = GenerateFinalLinks(
      GenerateProspectiveLinks(TunnelGenData.Instance.tunnelNodes[layer], layer),
      TunnelGenData.Instance.tunnelNodes[layer].Count);

    DrawLinksOnWorld(layer, finalLinks, TunnelGenData.Instance.tunnelNodes[layer]);
  }

  private List<Link> GenerateProspectiveLinks(
    List<PlanetTile> indexToTile,
    PlanetLayer layer)
  {
    // Map from the actual tile to its endpoint index (stored as a PlanetTile whose tileId is the index).
    // This lets us quickly detect "we reached another endpoint" during flood fill.
    Dictionary<PlanetTile, PlanetTile> tileToIndexLookup = new Dictionary<PlanetTile, PlanetTile>();
    for (int index = 0; index < indexToTile.Count; ++index)
      tileToIndexLookup[indexToTile[index]] = new PlanetTile(index, layer);

    // All candidate (A,B) links, each with a path distance/cost between the endpoints.
    List<Link> linkProspective = new List<Link>();

    // Reused list for FloodPathsWithCost start set.
    List<PlanetTile> startTiles = new List<PlanetTile>();

    // For each endpoint, flood outward and record the first few other endpoints we encounter.
    for (int index = 0; index < indexToTile.Count; ++index)
    {
      int srcLocal = index;
      PlanetTile srcTile = indexToTile[index];

      startTiles.Clear();
      startTiles.Add(srcTile);

      // Counts how many endpoints we've found from this source so we can early-stop.
      int found = 0;

      layer.Pather.FloodPathsWithCost(
        startTiles,
        // Movement cost function (higher cost => "further" in our link distance metric).
        (src, dst) => Caravan_PathFollower.CostToMove(3300, src, dst, perceivedStatic: true),
        terminator: (tile, distance) =>
        {
          // If the flood reaches another endpoint tile, record a prospective link.
          if (tile != srcTile && tileToIndexLookup.TryGetValue(tile, out PlanetTile planetTile))
          {
            ++found;

            linkProspective.Add(new Link
            {
              // Store the flood cost as the link weight.
              distance = distance,

              // Save endpoints as indices into indexToTile.
              indexA = srcLocal,
              indexB = planetTile.tileId
            });
          }

          // Stop searching once we've found enough nearby endpoints from this source.
          return found >= PotentialSpanningTreeLinksPerSettlement;
        });
    }

    // Sort by ascending cost so we consider cheaper/shorter links first when building the network.
    linkProspective.Sort((lhs, rhs) => lhs.distance.CompareTo(rhs.distance));
    return linkProspective;
  }

  private List<Link> GenerateFinalLinks(
    List<Link> linkProspective,
    int endpointCount)
  {
    // Disjoint-set / union-find-like structure to keep track of which endpoints are already connected.
    // (This is a very small, simple version: Group() climbs parent pointers recursively.)
    List<Connectedness> connectednessList = new List<Connectedness>();
    for (int index = 0; index < endpointCount; ++index)
      connectednessList.Add(new Connectedness());

    // The final set of links we will actually draw.
    List<Link> list = new List<Link>();

    // Iterate links in increasing distance so we mostly build a minimal spanning structure.
    for (int index = 0; index < linkProspective.Count; ++index)
    {
      Link prospective = linkProspective[index];

      bool differentGroups =
        connectednessList[prospective.indexA].Group() != connectednessList[prospective.indexB].Group();

      // Allow the link if it connects two currently-disconnected groups (spanning tree behavior),
      // OR rarely allow an extra cycle link (adds alternate routes / loops).
      bool allowExtraNonTreeLink =
        Rand.Value <= ChanceExtraNonSpanningTreeLink
        && !list.Any(link => link.indexB == prospective.indexA && link.indexA == prospective.indexB);

      if (differentGroups || allowExtraNonTreeLink)
      {
        // Even if eligible, sometimes "hide" the link to avoid overly dense networks.
        if (Rand.Value > ChanceHideSpanningTreeLink)
          list.Add(prospective);

        // If this link would connect two components, union them (regardless of whether we hid the visual link).
        // Note: This means the connectivity structure can become "more connected" than what gets drawn.
        if (differentGroups)
        {
          Connectedness connectedness = new Connectedness();

          // Attach both roots to a new parent node (effectively merging the two sets).
          connectednessList[prospective.indexA].Group().parent = connectedness;
          connectednessList[prospective.indexB].Group().parent = connectedness;
        }
      }
    }

    return list;
  }

  private void DrawLinksOnWorld(
    PlanetLayer layer,
    List<Link> linkFinal,
    List<PlanetTile> indexToTile)
  {
    // For each final link, compute an actual path and lay tunnel overlays along each step.
    foreach (Link link in linkFinal)
    {
      // Find the best path between the endpoints using the layer's pather.
      WorldPath path = layer.Pather.FindPath(indexToTile[link.indexA], indexToTile[link.indexB], null);

      // NodesReversed is a sequence of tiles along the path.
      List<PlanetTile> nodesReversed = path.NodesReversed;

      // Pick a TunnelDef to draw with.
      TunnelDef tunnelDef = DefDatabase<TunnelDef>.AllDefsListForReading
        .RandomElementWithFallback();

      // Lay the overlay for each edge along the path (tile i -> tile i+1).
      for (int index = 0; index < nodesReversed.Count - 1; ++index)
        TunnelGenData.Instance.OverlayTunnel(nodesReversed[index], nodesReversed[index + 1], tunnelDef);

      // Return the path object to the pool to avoid allocations/leaks.
      path.ReleaseToPool();
    }
  }

  /// <summary>
  /// Represents a potential or final connection between two endpoint indices.
  /// </summary>
  private struct Link
  {
    // Path cost between endpoints (used as a "distance" metric for sorting).
    public float distance;

    // Indices into the endpoint list.
    public int indexA;
    public int indexB;
  }

  /// <summary>
  /// Tiny union-find node: parent pointers define components; Group() returns the root.
  /// </summary>
  private class Connectedness
  {
    public Connectedness parent;

    public Connectedness Group()
    {
      // If no parent, this node is the root; otherwise walk upward.
      // (No path compression here; it's still fine for these small counts.)
      return parent == null ? this : parent.Group();
    }
  }
}