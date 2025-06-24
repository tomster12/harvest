using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemData GetItemData(string itemID)
    {
        instance.itemDictionary.TryGetValue(itemID, out ItemData data);
        Debug.Assert(data != null, $"Item with ID '{itemID}' not found in the database.");
        return data;
    }

    private static ItemDatabase instance;

    [SerializeField] private List<ItemData> items = new();

    private readonly Dictionary<string, ItemData> itemDictionary = new();

    private void Awake()
    {
        // Ensure it is the only instance and last for full lifetime
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Load the locally defined items into the static list and dictionary
        for (int i = 0; i < items.Count; i++) itemDictionary.Add(items[i].ID, items[i]);
    }
}
