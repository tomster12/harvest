using UnityEngine;

public class DebugItemSpawner : MonoBehaviour
{
    [SerializeField] private ItemData currentData;

    [ContextMenu("Spawn Current Item")]
    public void SpawnCurrentItem()
    {
        ItemInstance itemInstance = new(currentData, 1);
        LooseItem.Spawn(itemInstance, transform.position, transform.rotation);
    }
}
