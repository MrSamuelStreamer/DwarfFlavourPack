using RimWorld;

namespace DwarfFlavourPack.Comps;

public class StorytellerCompProperties_TunnelCaravanCategoryIndividualMTBByBiome : StorytellerCompProperties
{
    public IncidentCategoryDef category;
    public bool applyCaravanVisibility;

    public StorytellerCompProperties_TunnelCaravanCategoryIndividualMTBByBiome()
    {
        this.compClass = typeof (StorytellerComp_TunnelCaravanCategoryIndividualMTBByBiome);
    }
}