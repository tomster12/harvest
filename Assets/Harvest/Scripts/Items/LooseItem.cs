using UnityEngine;

public class LooseItem : MonoBehaviour, IItemContainer
{
    public static LooseItem Spawn(ItemInstance itemInstance, Vector3 position, Quaternion rotation)
    {
        GameObject looseItemObject = Instantiate(AssetDatabase.GetPrefab("Loose Item"), position, rotation);
        looseItemObject.name = $"Loose Item ({itemInstance.Data.Name})";
        LooseItem looseItem = looseItemObject.GetComponent<LooseItem>();

        GameObject itemMesh = Instantiate(itemInstance.Data.MeshPrefab, looseItemObject.transform);
        itemMesh.transform.localPosition = Vector3.zero;
        looseItem.outline.Initialize();

        looseItem.SetItemInstance(itemInstance);

        return looseItem;
    }

    public void SetItemInstance(ItemInstance itemInstance)
    {
        this.itemInstance = itemInstance;
        itemInstance.SetContainer(this);
    }

    public ItemInstance Pickup()
    {
        Destroy(gameObject);
        return itemInstance;
    }

    public void OnHoverEnter()
    {
        outline.enabled = true;
    }

    public void OnHoverExit()
    {
        outline.enabled = false;
    }

    [Header("References")]
    [SerializeField] private Outline outline;
    [SerializeField] private ItemInstance itemInstance;

    private void Awake()
    {
        outline.enabled = false;
    }
}
