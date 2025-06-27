using System;
using UnityEngine;

public enum ItemType
{ Resource, Tool, Equipment }

[Serializable]
[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData")]
public class ItemData : ScriptableObject
{
    public string ID;
    public ItemType type;
    public string Name;
    public string Description;
    public int SizeX, SizeY;
    public int MaxStackSize;
    public bool IsStackable => MaxStackSize > 1;
    public Sprite Icon;
    public GameObject MeshPrefab;
}
