using System;
using UnityEngine;

[Serializable]
public class AffixInstance
{
    public AffixData Data => data;
    public float RolledValue => rolledValue;

    public AffixInstance(AffixData data, float rolledValue)
    {
        this.data = data;
        this.rolledValue = rolledValue;
    }

    [SerializeField] private AffixData data;
    [SerializeField] private float rolledValue;

    // -------------------- Serialization  --------------------

    public AffixInstanceDTO Serialize()
    {
        return new()
        {
            DataID = data.ID,
            RolledValue = rolledValue
        };
    }

    public void Deserialize(AffixInstanceDTO affixDTO)
    {

        data = AssetDatabase.GetAffixData(affixDTO.DataID);
        rolledValue = affixDTO.RolledValue;
    }

    public static AffixInstance DeserializeNew(AffixInstanceDTO affixDTO)
    {
        AffixInstance instance = new();
        instance.Deserialize(affixDTO);
        return instance;
    }

    private AffixInstance() { }
}

[Serializable]
public struct AffixInstanceDTO
{
    public string DataID;
    public float RolledValue;
}
