using UnityEngine;

public class LooseItem : MonoBehaviour
{
    public void OnHoverEnter()
    {
        outline.enabled = true;
    }

    public void OnHoverExit()
    {
        outline.enabled = false;
    }

    public ItemInstance Pickup()
    {
        Destroy(gameObject);
        return itemInstance;
    }

    public void SetItemInstance(ItemInstance itemInstance)
    {
        this.itemInstance = itemInstance;
    }

    [Header("References")]
    [SerializeField] private Outline outline;
    [SerializeField] private ItemInstance itemInstance;

    private void Awake()
    {
        outline.enabled = false;
    }
}
