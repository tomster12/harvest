using UnityEngine;

public class DebugItemSpawner : MonoBehaviour
{
    [SerializeField] private ItemData currentData;

    [ContextMenu("Spawn Current Item")]
    public void SpawnCurrentItem()
    {
        ItemInstance itemInstance = currentData.Type == ItemType.Resource
            ? ItemInstance.NewResource(currentData, 1)
            : ItemGenerator.GenerateComplex(currentData, 1, null);

        LooseItem.Spawn(itemInstance, transform.position, transform.rotation);
    }
}
