using Verse;

namespace DwarfFlavourPack;

public class TunnelDef : Def
{
    [NoTranslate]
    public string worldTransitionGroup = "";
    public float distortionFrequency = 1f;
    public float distortionIntensity;
    [Unsaved()]
    private float[] cachedLayerWidth;

    public float GetLayerWidth(TunnelWorldLayerDef def)
    {
        return 1;
    }

    public override void ClearCachedData()
    {
        base.ClearCachedData();
        cachedLayerWidth = null;
    }
    
}