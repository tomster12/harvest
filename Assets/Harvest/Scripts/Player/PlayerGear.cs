using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerGear
{
    public bool IsEquipped => CurrentTool != null;
    public PlayerTool CurrentTool => currentTool;

    public void Init(Player player)
    {
        this.player = player;
        this.player.Persistent.Gear.OnItemAdded += OnItemEquipped;
        this.player.Persistent.Gear.OnItemRemoved += OnItemUnequipped;
    }

    public void OnItemEquipped(ItemInstance itemInstance)
    {
        if (itemInstance.Data.Type == ItemType.Tool)
        {
            switch (itemInstance.Data.ToolType)
            {
                case ToolType.Axe:
                    currentTool = new PlayerAxeTool(player, itemInstance);
                    break;

                default:
                    Debug.LogWarning($"Unhandled tool type: {itemInstance.Data.ToolType}");
                    break;
            }

            currentTool?.Equip();
        }

        else if (itemInstance.Data.Type == ItemType.Equipment)
        {
            var type = itemInstance.Data.EquipmentType;

            var mpGear = player.CustomTags.Get(CustomTagType.MeshPoint, $"MP - Gear - {type}");

            var gearMesh = GameObject.Instantiate(itemInstance.Data.MeshPrefab, mpGear);

            gearMesh.TryGetComponent<CustomTagRegistry>(out var gearTags);
            var mpGearMain = gearTags.Get(CustomTagType.MeshPoint, "MP - Main");

            MeshAttachmentUtility.AlignTransforms(mpGear, gearMesh.transform, mpGearMain);
            MeshAttachmentUtility.SetCollidersTrigger(gearMesh, true);

            equipmentMeshes[type] = gearMesh;
        }
    }

    public void OnItemUnequipped(ItemInstance itemInstance)
    {
        if (itemInstance.Data.Type == ItemType.Tool && currentTool != null)
        {
            currentTool.Unequip();
            currentTool = null;
        }

        else if (itemInstance.Data.Type == ItemType.Equipment)
        {
            var type = itemInstance.Data.EquipmentType;

            if (equipmentMeshes.TryGetValue(type, out var gearMesh))
            {
                GameObject.DestroyImmediate(gearMesh);
            }
        }
    }

    [SerializeField] private PlayerTool currentTool;

    private Player player;
    private Dictionary<EquipmentType, GameObject> equipmentMeshes = new();
}
