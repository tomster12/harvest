using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class PlayerInteractor
{
    public bool IsHoveringUI { get; private set; }

    public void Init(Player player)
    {
        this.player = player;

        // Setup inventory UIs
        inventoryUI = PlayerUI.InstantiateElement<GridInventoryUI>(gridInventoryUIPrefab, "Player Grid Inventory UI");
        inventoryUI.SetInventory(PlayerManager.Instance.Inventory);
        gearUI = PlayerUI.InstantiateElement<GearInventoryUI>(gearInventoryUIPrefab, "Player Gear Inventory UI");
        gearUI.SetInventory(PlayerManager.Instance.Gear);
        heldItemUI = PlayerUI.InstantiateElement<ItemUI>(itemUIPrefab, "Player Held Inventory Item UI");
        heldItemUI.SetItem(null);
    }

    public void UpdateInteractions()
    {
        UpdateInteractingItemContainers();
        UpdateInteractingWorld();
    }

    [Header("Prefab")]
    [SerializeField] private GameObject gridInventoryUIPrefab;
    [SerializeField] private GameObject gearInventoryUIPrefab;
    [SerializeField] private GameObject itemUIPrefab;

    private Player player;
    private GridInventoryUI inventoryUI;
    private GearInventoryUI gearUI;
    private ItemUI heldItemUI;
    private IItemContainerUI lastHoveredContainerUI;
    private LooseItem hoveredLooseItem;

    private bool IsHoldingItemUI => heldItemUI.State != ItemUI.StateType.Empty;

    private void UpdateInteractingItemContainers()
    {
        // Find what container and item UIs are being hovered
        var hoveredContainerUI = UIUtility.GetEventSystemRaycastResults()
            .Select(r => r.gameObject.GetComponent<IItemContainerUI>())
            .FirstOrDefault(c => c != null);
        IsHoveringUI = hoveredContainerUI != null;

        // Remove preview when unhovering an inventory
        if (lastHoveredContainerUI != hoveredContainerUI && lastHoveredContainerUI != null) lastHoveredContainerUI.DisablePreview();
        lastHoveredContainerUI = hoveredContainerUI;

        if (player.input.IsMousePressed)
        {
            // Click inside or outside an inventory
            player.input.IsMousePressed = false;
            if (hoveredContainerUI != null) hoveredContainerUI.Click(heldItemUI, Input.mousePosition);
            else if (IsHoldingItemUI) DropHeldItem();
            else player.input.IsMousePressed = true;
        }

        // Preview a click otherwise
        else hoveredContainerUI?.PreviewClick(heldItemUI, Input.mousePosition);
    }

    private void UpdateInteractingWorld()
    {
        // Only interact with world if not interacting with inventory
        if (!IsHoldingItemUI && !IsHoveringUI)
        {
            // Raycast to check for loose items
            Ray ray = player.camera.Camera.ScreenPointToRay(Input.mousePosition);
            LooseItem newHoveredLooseItem = null;
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.rigidbody) newHoveredLooseItem = hit.rigidbody.GetComponent<LooseItem>();
            }

            // If we have a new item, update the hovered item
            if (newHoveredLooseItem != hoveredLooseItem)
            {
                if (hoveredLooseItem != null) hoveredLooseItem.OnHoverExit();
                hoveredLooseItem = newHoveredLooseItem;
                if (hoveredLooseItem != null) hoveredLooseItem.OnHoverEnter();
            }

            // If we are hovering an item and click then try to pick it up
            if (hoveredLooseItem != null && player.input.IsMousePressed)
            {
                ItemInstance itemInstance = hoveredLooseItem.Pickup();
                heldItemUI.SetItem(itemInstance);
                Vector2 offset = new(heldItemUI.Rect.sizeDelta.x / 2, -heldItemUI.Rect.sizeDelta.y / 2);
                heldItemUI.SetStateToMouse(offset);
                player.input.IsMousePressed = false;
            }
        }
    }

    private void DropHeldItem()
    {
        Debug.Assert(IsHoldingItemUI, "Cannot drop item when not holding one");

        // Drop the held item as a loose item in front of the player
        Vector3 droppedPosition = player.transform.position + player.movement.TargetForward * 0.5f + Vector3.up * 0.5f;
        Quaternion droppedRotation = Quaternion.LookRotation(player.movement.TargetForward, Vector3.up);
        LooseItem.Spawn(heldItemUI.ItemInstance, droppedPosition, droppedRotation);
        heldItemUI.SetItem(null);
        player.input.IsMousePressed = false;
    }
}
