using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{ Resource, Tool, Equipment, Part }

public enum EquipmentType
{ None, Head, Body, Legs, Feet, Hand }

public enum ToolType
{ None, Axe, Pickaxe, Shovel, FishingRod, Hoe, Scythe }

public enum ToolPartType
{ None, AxeHead, AxeHandle, PickaxeHead, PickaxeHandle, ShovelHead, ShovelHandle, FishingRodHandle, FishingRodReel, HoeHead, HoeHandle, ScytheBlade, ScytheHandle }

public static class ItemMetadata
{
    public static readonly Dictionary<ToolType, ToolPartType[]> ToolsRequiredParts = new() {
        { ToolType.Axe, new[] { ToolPartType.AxeHead, ToolPartType.AxeHandle } },
        { ToolType.Pickaxe, new[] { ToolPartType.PickaxeHead, ToolPartType.PickaxeHandle } },
        { ToolType.Shovel, new[] { ToolPartType.ShovelHead, ToolPartType.ShovelHandle } },
        { ToolType.FishingRod, new[] { ToolPartType.FishingRodHandle, ToolPartType.FishingRodReel } },
        { ToolType.Hoe, new[] { ToolPartType.HoeHead, ToolPartType.HoeHandle } },
        { ToolType.Scythe, new[] { ToolPartType.ScytheBlade, ToolPartType.ScytheHandle } }
    };
}

[Serializable]
public class ToolItemData
{
    public ToolType toolType;
}

[Serializable]
public class EquipmentItemData
{
    public EquipmentType equipmentType;
}

[Serializable]
[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Info")]
    public string ID;
    public string Name;
    public string Description;

    [Header("Inventory")]
    public int SizeX, SizeY;
    public int MaxStackSize;

    [Header("Assets")]
    public Sprite Icon;
    public GameObject MeshPrefab;

    [Header("Type")]
    public ItemType Type;
    public ToolItemData ToolData = null;
    public EquipmentItemData EquipmentData = null;
    public ToolPartType? PartType;

    public bool IsStackable => MaxStackSize > 1;

    private void OnValidate()
    {
        if (Type != ItemType.Tool) ToolData = null;
        if (Type != ItemType.Equipment) EquipmentData = null;
    }
}
