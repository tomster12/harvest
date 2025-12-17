using UnityEngine;

public class PlayerAxeTool : PlayerTool
{
    public PlayerAxeTool(Player player, ItemInstance itemInstance) : base(player, itemInstance) { }

    public override void Equip()
    {
        // Create tool mesh attached to the left hand
        var leftHandSlot = player.Animator.GetAttachmentSlot("Left Hand");
        axeMesh = GameObject.Instantiate(itemInstance.Data.MeshPrefab, leftHandSlot);

        // Set the local offset to match the handle point
        Transform toolHandlePoint = axeMesh.transform.Find("Handle Point");
        axeMesh.transform.localPosition = -toolHandlePoint.localPosition;
        axeMesh.transform.localRotation = toolHandlePoint.localRotation;

        // Make colliders trigger
        var colliders = axeMesh.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders) collider.isTrigger = true;

        chopAction = new PlayerAxeChopAction(player, axeMesh);
    }

    public override void Update()
    {
        chopAction.Preview();
    }

    public override void Unequip()
    {
        GameObject.Destroy(axeMesh);

        chopAction.Dispose();
    }

    public override void DebugGizmos()
    {
        chopAction.DebugGizmos();
    }

    private PlayerAxeChopAction chopAction;
    private GameObject axeMesh;
}
