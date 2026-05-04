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
            DefinitionID = data.ID,
            RolledValue = rolledValue
        };
    }

    public void Deserialize(AffixInstanceDTO affixDTO)
    {

        data = AssetDatabase.GetAffixData(affixDTO.DefinitionID);
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
    public string DefinitionID;
    public float RolledValue;
}
