using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    public enum StateType
    { EMPTY, INVENTORY, MOUSE };

    public RectTransform Rect => (RectTransform)transform;
    public ItemInstance ItemInstance { get; private set; }
    public InventoryUI InventoryUI { get; private set; }
    public StateType State { get; private set; } = StateType.EMPTY;
    public Vector2 MouseOffset { get; private set; } = Vector2.zero;

    public void SetItem(ItemInstance newItemInstance)
    {
        // Unsubscribe from old Item
        if (ItemInstance != null) ItemInstance.OnAmountChanged -= OnAmountChanged;

        ItemInstance = newItemInstance;

        if (ItemInstance != null)
        {
            // Set up item UI with new Item
            ItemInstance.OnAmountChanged += OnAmountChanged;
            gameObject.SetActive(true);
            gameObject.name = $"Inventory Item UI ({newItemInstance.Data.Name})";
            Rect.sizeDelta = InventoryUI.GetGridSize(newItemInstance.Data.SizeX, newItemInstance.Data.SizeY);
            iconImage.sprite = newItemInstance.Data.Icon;
            amountText.text = ItemInstance.Amount.ToString();
        }
        else
        {
            // Turn off item UI if no item is set
            InventoryUI = null;
            gameObject.SetActive(false);
            State = StateType.EMPTY;
        }
    }

    public void SetHeldByMouse(Vector2 offset)
    {
        MouseOffset = offset;
        InventoryUI = null;
        State = StateType.MOUSE;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent, Input.mousePosition, null, out Vector2 localPoint);
        Rect.localPosition = localPoint - MouseOffset;
    }

    public void SetInInventory(InventoryUI inventoryUI, int x, int y)
    {
        InventoryUI = inventoryUI;
        Rect.localPosition = inventoryUI.ConvertGridPosToLocalPos(x, y);
        State = StateType.INVENTORY;
    }

    public void OnAmountChanged()
    {
        // Update Item amount
        amountText.text = ItemInstance.Amount.ToString();
    }

    [Header("References")]
    [SerializeField] private TMPro.TextMeshProUGUI amountText;
    [SerializeField] private Image iconImage;

    private void Awake()
    {
        gameObject.SetActive(false);
        Rect.sizeDelta = Vector2.zero;
        iconImage.sprite = null;
        amountText.text = string.Empty;
        State = StateType.EMPTY;
        MouseOffset = Vector2.zero;
        InventoryUI = null;
        ItemInstance = null;
    }

    private void OnDestroy()
    {
        if (ItemInstance != null)
        {
            ItemInstance.OnAmountChanged -= OnAmountChanged;
        }
    }

    private void Update()
    {
        if (State == StateType.MOUSE)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent, Input.mousePosition, null, out Vector2 localPoint);
            Rect.localPosition = localPoint - MouseOffset;
        }
    }
}
