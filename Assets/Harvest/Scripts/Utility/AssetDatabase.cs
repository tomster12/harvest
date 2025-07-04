using System.Collections.Generic;
using UnityEngine;

public class AssetDatabase : MonoBehaviour
{
    public static ItemData GetItemData(string itemID)
    {
        itemDictionary.TryGetValue(itemID, out ItemData data);
        Debug.Assert(data != null, $"Item with ID '{itemID}' not found in the database.");
        return data;
    }

    public static GameObject GetPrefab(string itemID)
    {
        prefabDictionary.TryGetValue(itemID, out GameObject prefab);
        Debug.Assert(prefab != null, $"Prefab with name '{itemID}' not found in the database.");
        return prefab;
    }

    public static Material GetMaterial(string materialName)
    {
        materialDictionary.TryGetValue(materialName, out Material material);
        Debug.Assert(material != null, $"Material with name '{materialName}' not found in the database.");
        return material;
    }

    private static bool isInitialized = false;
    private static readonly Dictionary<string, ItemData> itemDictionary = new();
    private static readonly Dictionary<string, GameObject> prefabDictionary = new();
    private static readonly Dictionary<string, Material> materialDictionary = new();

    [Header("Items")]
    [SerializeField] private List<ItemData> items = new();

    [Header("Prefabs")]
    [SerializeField] private List<GameObject> prefabs = new();

    [Header("Materials")]
    [SerializeField] private List<Material> materials = new();

    private void Awake()
    {
        ReloadDatabase();
    }

    [ContextMenu("Reload Database")]
    private void ReloadDatabase()
    {
        if (isInitialized) return;
        isInitialized = true;

        // Load the locally defined variables into the static dictionaries
        itemDictionary.Clear();
        prefabDictionary.Clear();
        materialDictionary.Clear();
        for (int i = 0; i < items.Count; i++) itemDictionary.Add(items[i].ID, items[i]);
        for (int i = 0; i < prefabs.Count; i++) prefabDictionary.Add(prefabs[i].name, prefabs[i]);
        for (int i = 0; i < materials.Count; i++) materialDictionary.Add(materials[i].name, materials[i]);
    }
}
