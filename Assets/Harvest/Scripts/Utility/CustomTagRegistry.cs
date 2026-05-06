using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomTagRegistry : MonoBehaviour
{
    public void RebuildRegistry()
    {
        registry.Clear();

        foreach (var tag in GetComponentsInChildren<CustomTag>())
        {
            foreach (var type in tag.Tags)
            {
                if (!registry.TryGetValue(type, out var typeRegistry))
                {
                    registry[type] = typeRegistry = new Dictionary<string, Transform>();
                }

                typeRegistry[tag.name] = tag.transform;
            }
        }
    }

    public Transform Get(CustomTagType type, string tagName)
    {
        Transform result = null;
        var found = registry.TryGetValue(type, out var typeRegistry) && typeRegistry.TryGetValue(tagName, out result);
        Debug.Assert(found, $"Tag '{tagName}' not found of type {type} on {gameObject.name}");
        return result;
    }

    private readonly Dictionary<CustomTagType, Dictionary<string, Transform>> registry = new();

    private void Awake() => RebuildRegistry();
}