using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PartSlotDefinition
{
    public PartType RequiredType;
    public ItemData DefaultItem;
}

[Serializable]
[CreateAssetMenu(fileName = "Item_", menuName = "ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string ID;
    public string DisplayName;
    public string Description;

    [Header("Inventory")]
    public int SizeX;
    public int SizeY;
    public int MaxStackSize;

    [Header("Assets")]
    public Sprite Icon;
    public GameObject MeshPrefab;

    [Header("Type")]
    public ItemType Type;
    public ToolType ToolType = ToolType.None;
    public EquipmentType EquipmentType = EquipmentType.None;
    public PartType PartType = PartType.None;
    public float BaseGoldValue;
    public List<PartSlotDefinition> PartSlotDefinitions = new();
    public AffixPool AffixPool;

    public bool IsStackable => MaxStackSize > 1;

    private void OnValidate()
    {
        if (Type != ItemType.Tool) ToolType = ToolType.None;
        if (Type != ItemType.Equipment) EquipmentType = EquipmentType.None;
    }
}

[Serializable]
public enum ItemType
{
    Resource, Tool, Equipment, Part
}

[Serializable]
public enum EquipmentType
{
    None, Head, Body, Legs, Feet, Hand
}

[Serializable]
public enum ToolType
{
    None, Axe, Pickaxe, Shovel, FishingRod, Hoe, Scythe
}

[Serializable]
public enum PartType
{
    None, AxeHead, AxeHandle, PickaxeHead, PickaxeHandle, ShovelHead,
    ShovelHandle, FishingRodHandle, FishingRodReel, HoeHead, HoeHandle,
    ScytheBlade, ScytheHandle
}
