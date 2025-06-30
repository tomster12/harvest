using UnityEditor.ShaderKeywordFilter;
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

        // Turn off item UI if no item is set
        if (ItemInstance == null)
        {
            ContainerUI = null;
            gameObject.SetActive(false);
            State = StateType.Empty;
            return;
        }

        // Set up item UI with new Item
        ItemInstance.OnAmountChanged += OnAmountChanged;
        gameObject.SetActive(true);
        gameObject.name = $"Inventory Item UI ({itemInstance.Data.Name})";
        Rect.sizeDelta = GridInventoryUI.GetGridSize(itemInstance.Data.SizeX, itemInstance.Data.SizeY);
        iconImage.sprite = itemInstance.Data.Icon;
        amountText.gameObject.SetActive(itemInstance.Data.IsStackable);
        amountText.text = ItemInstance.Amount.ToString();
    }

    public void SetItemWithResponse(ItemContainerInteractResponse response, Vector2 offset)
    {
        // Placed or stacked this item, so turn off
        if (response.type == ItemContainerInteractType.Placed || (response.type == ItemContainerInteractType.Stacked && this.ItemInstance.Amount == 0))
        {
            this.SetItem(null);
        }

        // Removed / replaced an existing item, so update
        else if (response.type == ItemContainerInteractType.Replaced || response.type == ItemContainerInteractType.Pickup)
        {
            this.SetStateToMouse(offset);
            this.SetItem(response.itemInstance);
        }
    }

    public void SetStateToMouse(Vector2 offset)
    {
        State = StateType.Mouse;
        ContainerUI = null;
        MouseOffset = offset;
        UpdatePositionWithMouse();
    }

    public void SetStateToContainer(IItemContainerUI containerUI, RectTransform parent, float x, float y)
    {
        State = StateType.Container;
        ContainerUI = containerUI;
        Rect.transform.SetParent(parent);
        Rect.localPosition = new Vector3(x, y, 0);
    }

    public bool GetPointInside(Vector2 pos, out Vector2 localPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, pos, null, out localPos);
        return Rect.rect.Contains(localPos);
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
        if (State == StateType.Mouse) UpdatePositionWithMouse();
    }

    private void UpdatePositionWithMouse()
    {
        if (State != StateType.Mouse) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Rect.parent, Input.mousePosition, null, out Vector2 localPoint);
        Rect.localPosition = localPoint - MouseOffset;
    }

    private void OnAmountChanged()
    {
        // Update Item amount
        amountText.text = ItemInstance.Amount.ToString();
    }
}
