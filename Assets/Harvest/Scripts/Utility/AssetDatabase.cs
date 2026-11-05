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

    public static AffixData GetAffixData(string affixID)
    {
        affixDictionary.TryGetValue(affixID, out AffixData data);
        Debug.Assert(data != null, $"Affix with ID '{affixID}' not found in the database.");
        return data;
    }

    public static WoodType GetWoodType(string woodID)
    {
        woodDictionary.TryGetValue(woodID, out WoodType data);
        Debug.Assert(data != null, $"Wood type with ID '{woodID}' not found in the database.");
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
    private static readonly Dictionary<string, AffixData> affixDictionary = new();
    private static readonly Dictionary<string, WoodType> woodDictionary = new();
    private static readonly Dictionary<string, GameObject> prefabDictionary = new();
    private static readonly Dictionary<string, Material> materialDictionary = new();

    [Header("Items")]
    [SerializeField] private List<ItemData> items = new();

    [Header("Affixes")]
    [SerializeField] private List<AffixData> affixes = new();

    [Header("Wood Types")]
    [SerializeField] private List<WoodType> woods = new();

    [Header("Prefabs")]
    [SerializeField] private List<GameObject> prefabs = new();

    [Header("Materials")]
    [SerializeField] private List<Material> materials = new();

    private void Awake()
    {
        ReloadDatabase();
    }

#if UNITY_EDITOR
    [ContextMenu("Collect Items And Affixes")]
    private void OnValidate()
    {
        var collectedItems = CollectAssets<ItemData>();
        var collectedAffixes = CollectAssets<AffixData>();
        var collectedWoods = CollectAssets<WoodType>();

        if (items.Count == collectedItems.Count && affixes.Count == collectedAffixes.Count && woods.Count == collectedWoods.Count)
        {
            bool isSame = true;
            for (int i = 0; i < items.Count && isSame; i++) isSame = items[i] == collectedItems[i];
            for (int i = 0; i < affixes.Count && isSame; i++) isSame = affixes[i] == collectedAffixes[i];
            for (int i = 0; i < woods.Count && isSame; i++) isSame = woods[i] == collectedWoods[i];
            if (isSame) return;
        }

        items = collectedItems;
        affixes = collectedAffixes;
        woods = collectedWoods;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static List<T> CollectAssets<T>() where T : Object
    {
        var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        var paths = new List<string>();
        for (int i = 0; i < guids.Length; i++) paths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
        paths.Sort(System.StringComparer.Ordinal);

        var assets = new List<T>();
        for (int i = 0; i < paths.Count; i++)
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(paths[i]);
            if (asset != null) assets.Add(asset);
        }
        return assets;
    }
#endif

    [ContextMenu("Reload Database")]
    private void ReloadDatabase()
    {
        if (isInitialized) return;
        isInitialized = true;

        // Load the locally defined variables into the static dictionaries
        itemDictionary.Clear();
        affixDictionary.Clear();
        woodDictionary.Clear();
        prefabDictionary.Clear();
        materialDictionary.Clear();

        for (int i = 0; i < items.Count; i++) itemDictionary.Add(items[i].ID, items[i]);
        for (int i = 0; i < affixes.Count; i++) affixDictionary.Add(affixes[i].ID, affixes[i]);
        for (int i = 0; i < woods.Count; i++) woodDictionary.Add(woods[i].ID, woods[i]);
        for (int i = 0; i < prefabs.Count; i++) prefabDictionary.Add(prefabs[i].name, prefabs[i]);
        for (int i = 0; i < materials.Count; i++) materialDictionary.Add(materials[i].name, materials[i]);
    }
}
