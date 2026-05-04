using UnityEngine;


[CreateAssetMenu(fileName = "Affix_", menuName = "AffixData")]
public class AffixData : ScriptableObject
{
    public string ID;
    public string DisplayName;
    public StatType Stat;
    public float MinValue;
    public float MaxValue;
    public AffixValueType ValueType = AffixValueType.Additive;
    [Range(0f, 1f)] public float Weight = 1f;
    public bool IsUnique;
}

public enum AffixValueType
{
    Additive, Multiplicative
}