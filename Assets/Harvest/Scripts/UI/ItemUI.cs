using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public enum StateType
    { Empty, Container, Mouse };

    public RectTransform Rect => (RectTransform)transform;
    public ItemInstance ItemInstance { get; private set; }
    public IItemContainerUI ContainerUI { get; private set; }
    public StateType State { get; private set; } = StateType.Empty;
    public Vector2 MouseOffset { get; private set; } = Vector2.zero;

    public void SetItem(ItemInstance itemInstance)
    {
        // Unsubscribe from old Item
        if (ItemInstance != null) ItemInstance.OnAmountChanged -= OnAmountChanged;

        ItemInstance = itemInstance;

        if (ItemInstance != null)
        {
            // Set up item UI with new Item
            ItemInstance.OnAmountChanged += OnAmountChanged;
            gameObject.SetActive(true);
            gameObject.name = $"Inventory Item UI ({itemInstance.Data.Name})";
            Rect.sizeDelta = GridInventoryUI.GetGridSize(itemInstance.Data.SizeX, itemInstance.Data.SizeY);
            iconImage.sprite = itemInstance.Data.Icon;
            amountText.gameObject.SetActive(itemInstance.Data.IsStackable);
            amountText.text = ItemInstance.Amount.ToString();
        }
        else
        {
            // Turn off item UI if no item is set
            ContainerUI = null;
            gameObject.SetActive(false);
            State = StateType.Empty;
        }
    }

    public void SetHeldByMouse(Vector2 offset)
    {
        MouseOffset = offset;
        ContainerUI = null;
        State = StateType.Mouse;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent, Input.mousePosition, null, out Vector2 localPoint);
        Rect.localPosition = localPoint - MouseOffset;
    }

    public void SetLocalPosition(IItemContainerUI containerUI, RectTransform parent, float x, float y)
    {
        ContainerUI = containerUI;
        Rect.transform.SetParent(parent);
        Rect.localPosition = new Vector3(x, y, 0);
        State = StateType.Container;
    }

    public void UpdateWithContainerResponse(ItemContainerInteractResponse response, Vector2 offset)
    {
        // Placed or stacked, so turn off held item
        if (response.type == ItemContainerInteractType.Placed || (response.type == ItemContainerInteractType.Stacked && this.ItemInstance.Amount == 0))
        {
            this.SetItem(null);
        }

        // Replaced, so update the held item to the new item
        else if (response.type == ItemContainerInteractType.Replaced)
        {
            this.SetHeldByMouse(offset);
        }

        // Removed, so set the held item to the hovered item
        else if (response.type == ItemContainerInteractType.Removed)
        {
            this.SetItem(response.itemInstance);
            this.SetHeldByMouse(offset);
        }
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
        State = StateType.Empty;
        MouseOffset = Vector2.zero;
        ContainerUI = null;
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
        if (State == StateType.Mouse)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent, Input.mousePosition, null, out Vector2 localPoint);
            Rect.localPosition = localPoint - MouseOffset;
        }
    }
}
