using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "Item_", menuName = "ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string ID;
    public string DisplayName;
    public string Description;
    public ItemType Type;
    public ToolType ToolType = ToolType.None;
    public EquipmentType EquipmentType = EquipmentType.None;
    public PartType PartType = PartType.None;

    [Header("Assets")]
    public Sprite Icon;
    public GameObject MeshPrefab;

    [Header("Inventory")]
    public int SizeX;
    public int SizeY;
    public int MaxStackSize;

    [Header("Itemisation")]
    public List<PartSlotData> PartSlots = new();
    public AffixPool AffixPool;
    public float BaseGoldValue;
    public List<BaseStat> BaseStats = new();

    public bool IsStackable => MaxStackSize > 1;

    private void OnValidate()
    {
        if (Type != ItemType.Tool) ToolType = ToolType.None;
        if (Type != ItemType.Equipment) EquipmentType = EquipmentType.None;
    }
}

[Serializable]
public struct PartSlotData
{
    public PartType RequiredType;
    public ItemData DefaultItem;
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
