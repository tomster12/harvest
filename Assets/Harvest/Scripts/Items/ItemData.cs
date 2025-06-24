using System;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData")]
public class ItemData : ScriptableObject
{
    public string ID;
    public string Name;
    public string Description;
    public int MaxStackSize;
    public Sprite Icon;
    public int SizeX, SizeY;
}
