using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class PlayerInteractor
{
    public bool IsHoveringUI { get; private set; }
    public ItemUI HeldItemUI { get; private set; }

    public void Init(Player player)
    {
        this.player = player;

        // Setup inventory UIs
        inventoryUI = PlayerUI.InstantiateElement<GridInventoryUI>(gridInventoryUIPrefab, "Player Grid Inventory UI");
        inventoryUI.SetInventory(player.Persistent.Inventory);
        gearUI = PlayerUI.InstantiateElement<GearInventoryUI>(gearInventoryUIPrefab, "Player Gear Inventory UI");
        gearUI.SetInventory(player.Persistent.Gear);
        HeldItemUI = PlayerUI.InstantiateElement<ItemUI>(itemUIPrefab, "Player Held Inventory Item UI");
        HeldItemUI.SetItem(null);
    }

    public void HandleInteractingItemContainers()
    {
        // Find what container UIs and item UIs are being hovered
        var hoveredContainerUI = UIUtility.GetEventSystemRaycastResults()
            .Select(r => r.gameObject.GetComponent<IItemContainerUI>())
            .FirstOrDefault(c => c != null);
        IsHoveringUI = hoveredContainerUI != null;

        // Remove preview when unhovering a container
        if (lastHoveredContainerUI != hoveredContainerUI && lastHoveredContainerUI != null) lastHoveredContainerUI.DisablePreview();
        lastHoveredContainerUI = hoveredContainerUI;

        // Click inside or outside a container
        if (player.Input.IsMousePressed && !player.IsBlocked(PlayerBlockFlags.Inventory))
        {
            player.Input.IsMousePressed = false;
            if (hoveredContainerUI != null) hoveredContainerUI.Click(HeldItemUI, Input.mousePosition);
            else if (IsHoldingItemUI) DropHeldItem();
            else player.Input.IsMousePressed = true;
        }

        // Preview a click otherwise
        else hoveredContainerUI?.PreviewClick(HeldItemUI, Input.mousePosition);
    }

    public void HandleInteractingWorld()
    {
        // Only interact with world if not interacting with inventory
        if (IsHoldingItemUI || IsHoveringUI) return;

        // Raycast to check for loose items
        Ray ray = player.Camera.Camera.ScreenPointToRay(Input.mousePosition);
        LooseItem newHoveredLooseItem = null;
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.rigidbody) newHoveredLooseItem = hit.rigidbody.GetComponent<LooseItem>();
        }

        // Hovering a new item so update and preview
        if (newHoveredLooseItem != hoveredLooseItem)
        {
            if (hoveredLooseItem != null) hoveredLooseItem.OnHoverExit();
            hoveredLooseItem = newHoveredLooseItem;
            if (hoveredLooseItem != null) hoveredLooseItem.OnHoverEnter();
        }

        // Clicking on a hovered loose item so try pickup
        if (hoveredLooseItem != null && player.Input.IsMousePressed)
        {
            Vector3 targetPosition = hoveredLooseItem.transform.position - (hoveredLooseItem.transform.position - player.transform.position).normalized * 0.1f;
            PlayerAction action = new PickupLooseItemAction(hoveredLooseItem, targetPosition);
            player.Actions.StartAction(action);
        }
    }

    [Header("Prefab")]
    [SerializeField] private GameObject gridInventoryUIPrefab;
    [SerializeField] private GameObject gearInventoryUIPrefab;
    [SerializeField] private GameObject itemUIPrefab;

    private Player player;
    private GridInventoryUI inventoryUI;
    private GearInventoryUI gearUI;
    private IItemContainerUI lastHoveredContainerUI;
    private LooseItem hoveredLooseItem;

    private bool IsHoldingItemUI => HeldItemUI.State != ItemUI.StateType.Empty;

    private void DropHeldItem()
    {
        Debug.Assert(IsHoldingItemUI, "Cannot drop item when not holding one");

        // Drop the held item as a loose item in front of the player
        Vector3 droppedPosition = player.transform.position + player.Movement.TargetForward * 0.5f + Vector3.up * 0.5f;
        Quaternion droppedRotation = Quaternion.LookRotation(player.Movement.TargetForward, Vector3.up);
        LooseItem.Spawn(HeldItemUI.ItemInstance, droppedPosition, droppedRotation);
        HeldItemUI.SetItem(null);
        player.Input.IsMousePressed = false;
    }
}
