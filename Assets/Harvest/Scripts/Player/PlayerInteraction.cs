using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class PlayerInteraction
{
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

    public void HandleInteractingContainers()
    {
        if (!player.IsRestricted(Player.Restriction.InteractContainers))
        {
            // Update hovered container UI
            var newHoveredContainerUI = UIUtil.GetEventSystemRaycastResults()
                .Select(r => r.gameObject.GetComponent<IItemContainerUI>())
                .FirstOrDefault(c => c != null);

            if (hoveredContainerUI != newHoveredContainerUI) hoveredContainerUI?.DisablePreview();
            hoveredContainerUI = newHoveredContainerUI;

            // Click inside or outside a container
            if (player.Input.IsMousePressed)
            {
                if (hoveredContainerUI != null)
                {
                    hoveredContainerUI.Click(HeldItemUI, Input.mousePosition);
                    player.Input.IsMousePressed = false;
                }
                else if (IsHoldingItem)
                {
                    DropHeldItem();
                    player.Input.IsMousePressed = false;
                }
            }

            // Otherwise preview a click
            else hoveredContainerUI?.PreviewClick(HeldItemUI, Input.mousePosition);
        }
        else
        {
            // Clear hovered container UI if restricted
            hoveredContainerUI?.DisablePreview();
            hoveredContainerUI = null;
        }
    }

    public void HandleInteractingWorld()
    {
        if (!IsHoveringContainer && !IsHoldingItem && !player.IsRestricted(Player.Restriction.InteractLooseItems))
        {
            // If we are already picking up an item check if its done
            if (IsPickingUpItem)
            {
                if (looseItemPickupAction.IsRunning) return;
                looseItemPickupAction = null;
            }

            // Get first hovered transform that is a loose item
            LooseItem newHoveredLooseItem = null;
            foreach (RaycastHit hit in player.Input.RaycastHits)
            {
                if (hit.rigidbody != null)
                {
                    newHoveredLooseItem = hit.rigidbody.GetComponent<LooseItem>();
                    if (newHoveredLooseItem != null) break;
                }
            }

            // Hovering a new loose item so update and preview
            if (newHoveredLooseItem != hoveredLooseItem)
            {
                if (hoveredLooseItem != null) hoveredLooseItem.OnHoverExit();
                hoveredLooseItem = newHoveredLooseItem;
                if (hoveredLooseItem != null) hoveredLooseItem.OnHoverEnter();
            }

            // Clicking on a hovered loose item so try pickup
            if (hoveredLooseItem != null && player.Input.IsMousePressed)
            {
                player.Input.IsMousePressed = false;
                Vector3 targetPosition = hoveredLooseItem.transform.position - (hoveredLooseItem.transform.position - player.transform.position).normalized * 0.1f;
                looseItemPickupAction = new PlayerPickupItemAction(player, hoveredLooseItem, targetPosition);
                player.Actions.StartAction(looseItemPickupAction);
            }
        }

        if (!IsHoveringContainer && !IsHoldingItem && !IsPickingUpItem && !player.IsRestricted(Player.Restriction.InteractLargeObjects))
        {
        }
    }

    [Header("Prefab")]
    [SerializeField] private GameObject gridInventoryUIPrefab;
    [SerializeField] private GameObject gearInventoryUIPrefab;
    [SerializeField] private GameObject itemUIPrefab;

    private Player player;
    private GridInventoryUI inventoryUI;
    private GearInventoryUI gearUI;
    private IItemContainerUI hoveredContainerUI;
    private LooseItem hoveredLooseItem;
    private PlayerPickupItemAction looseItemPickupAction;

    private bool IsHoldingItem => HeldItemUI != null && HeldItemUI.State != ItemUI.StateType.Empty;
    private bool IsPickingUpItem => looseItemPickupAction != null;
    private bool IsHoveringContainer => hoveredContainerUI != null;

    private void DropHeldItem()
    {
        Debug.Assert(IsHoldingItem, "Cannot drop item when not holding one");

        // Drop the held item as a loose item in front of the player
        Vector3 droppedPosition = player.transform.position + player.Movement.TargetFacingDir * 0.5f + Vector3.up * 0.5f;
        Quaternion droppedRotation = Quaternion.LookRotation(player.Movement.TargetFacingDir, Vector3.up);
        LooseItem.Spawn(HeldItemUI.ItemInstance, droppedPosition, droppedRotation);
        HeldItemUI.SetItem(null);
        player.Input.IsMousePressed = false;
    }
}
