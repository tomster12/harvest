using System.Collections.Generic;
using UnityEngine;

public class GearInventoryUI : MonoBehaviour, IItemContainerUI
{
    public RectTransform Rect => (RectTransform)transform;
    public GearInventory Inventory { get; private set; }

    public void SetInventory(GearInventory newInventory)
    {
    }

    public void PlaceHeldItem(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
    }

    public void PickupItem(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
    }

    public void HoverPreview(ItemUI heldItemUI, ItemUI hoveredItemUI)
    {
    }

    public void DisablePreview()
    {
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject itemUIPrefab;

    private void Awake()
    {
        // Move the rect to an arbitrary offset from top left
        Rect.pivot = new(0, 1);
        Rect.anchorMin = new(0, 1);
        Rect.anchorMax = new(0, 1);
        Rect.anchoredPosition = new(100f, -100f);
    }

    private void OnDestroy()
    {
    }

    private void OnItemAdded(ItemInstance itemInstance)
    {
    }

    private void OnItemRemoved(ItemInstance item)
    {
    }
}
