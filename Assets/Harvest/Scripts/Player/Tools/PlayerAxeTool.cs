using System;
using UnityEngine;

[Serializable]
public class PlayerAxeTool : PlayerTool
{
    public PlayerAxeTool(Player player, ItemInstance itemInstance) : base(player, itemInstance) { }

    public override void Equip()
    {
        // Create tool mesh attached to the left hand
        var mpLeftHand = player.CustomTags.Get(CustomTagType.MeshPoint, "Left Hand");

        axeMesh = GameObject.Instantiate(itemInstance.Data.MeshPrefab, mpLeftHand);

        axeMesh.TryGetComponent<CustomTagRegistry>(out var axeTags);
        var mpHandle = axeTags.Get(CustomTagType.MeshPoint, "MP - Handle");

        MeshAttachmentUtility.AlignTransforms(mpLeftHand, axeMesh.transform, mpHandle);
        MeshAttachmentUtility.SetCollidersTrigger(axeMesh, true);

        chopAction = new PlayerAxeChopAction(player, axeMesh, axeTags);
        player.Actions.Register(chopAction);
    }

    public override void Unequip()
    {
        GameObject.Destroy(axeMesh);
        chopAction.Dispose();
        player.Actions.Unregister(chopAction);
    }

    private GameObject axeMesh;
    private PlayerAxeChopAction chopAction;
}
