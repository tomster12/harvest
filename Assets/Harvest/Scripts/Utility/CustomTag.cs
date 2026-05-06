using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CustomTagType
{
    MeshPoint,
    AnimationTransform,
    AnimationPoint
}

public class CustomTag : MonoBehaviour
{
    public IReadOnlyList<CustomTagType> Tags => tags;

    public bool HasTag(CustomTagType tag) => tags.Contains(tag);

    public bool HasAnyTag(params CustomTagType[] checkTags) => checkTags.Any(tags.Contains);

    public bool HasAllTags(params CustomTagType[] checkTags) => checkTags.All(tags.Contains);

    [SerializeField] private List<CustomTagType> tags = new();
}
