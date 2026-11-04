using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class PlayerInteractor
{
    public ItemUI HeldItemUI { get; private set; }
    public bool IsHoveringContainer => hoveredContainerUI != null;
    public bool IsHoldingItem => HeldItemUI != null && HeldItemUI.State != ItemUI.StateType.Empty;

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

        itemInspectorUI = PlayerUI.InstantiateElement<ItemInspectorUI>(itemInspectorUIPrefab, "Item Inspector UI");
        itemInspectorUI.Init();

        // Setup actions
        looseItemPickupAction = new PlayerPickupItemAction(player);
        player.Actions.Register(looseItemPickupAction);

        dragAction = new PlayerDragAction(player);
        player.Actions.Register(dragAction);
    }

    public void HandleInteractingUI()
    {
        if (player.Actions.IsActing)
        {
            if (hoveredItemUI != null)
            {
                itemInspectorUI.HideInspector();
                hoveredItemUI = null;
            }
            if (hoveredContainerUI != null)
            {
                hoveredContainerUI?.DisablePreview();
                hoveredContainerUI = null;
            }
            return;
        }

        var raycastResults = UIUtil.GetEventSystemRaycastResults();

        // Handle hovering item UIs
        var newHoveredItemUI = HeldItemUI.State == ItemUI.StateType.Mouse ? null : raycastResults
            .Select(r => r.gameObject.GetComponent<ItemUI>())
            .FirstOrDefault(c => c != null);

        if (newHoveredItemUI != hoveredItemUI)
        {
            if (hoveredItemUI != null) itemInspectorUI.HideInspector();
            hoveredItemUI = newHoveredItemUI;
            if (hoveredItemUI != null) itemInspectorUI.ShowInspector(hoveredItemUI.ItemInstance);
        }

        // Handle hovering container UIs
        var newHoveredContainerUI = raycastResults
                .Select(r => r.gameObject.GetComponent<IItemContainerUI>())
                .FirstOrDefault(c => c != null);

        if (hoveredContainerUI != newHoveredContainerUI)
        {
            hoveredContainerUI?.DisablePreview();
            hoveredContainerUI = newHoveredContainerUI;
        }

        // Click inside or outside a container
        if (player.Input.IsMousePressed)
        {
            if (hoveredContainerUI != null)
            {
                hoveredContainerUI.Click(HeldItemUI, PlayerInput.PointerPosition);
                player.Input.IsMousePressed = false;
            }
            else if (IsHoldingItem)
            {
                DropHeldItem();
                player.Input.IsMousePressed = false;
            }
        }

        // Otherwise preview a click on the container
        else hoveredContainerUI?.PreviewHover(HeldItemUI, PlayerInput.PointerPosition);
    }

    public void OnDrawGizmos()
    {
        if (dragAction != null && dragAction.IsRunning)
        {
            dragAction.DrawGizmosActive();
        }
    }

    [Header("Prefab")]
    [SerializeField] private GameObject gridInventoryUIPrefab;
    [SerializeField] private GameObject gearInventoryUIPrefab;
    [SerializeField] private GameObject itemUIPrefab;
    [SerializeField] private GameObject itemInspectorUIPrefab;

    private Player player;
    private GridInventoryUI inventoryUI;
    private GearInventoryUI gearUI;
    private ItemInspectorUI itemInspectorUI;

    private IItemContainerUI hoveredContainerUI;
    private ItemUI hoveredItemUI;
    private PlayerPickupItemAction looseItemPickupAction;
    private PlayerDragAction dragAction;

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
