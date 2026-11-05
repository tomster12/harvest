using UnityEngine;

[CreateAssetMenu(fileName = "Wood_", menuName = "WoodType")]
public class WoodType : ScriptableObject
{
    public string ID;
    public string DisplayName;
    public float Density;
    public float BaseHardness;
    public float PricePerVolume;
    public Color BarkColor = Color.white;
    public Color InnerColor = Color.white;
    public Color CoreColor = Color.white;
    [Range(0f, 0.3f)] public float ColourVariance = 0.05f;
}
