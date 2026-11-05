using UnityEngine;

public enum TreeHitOutcome
{
    Miss, Glance, Clean, Splinter
}

public struct TreeHitResult
{
    public TreeHitOutcome Outcome;
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2Int Cell;
    public float Depth;
    public float Sharpness;
    public float Damage;
    public bool CausedSplit;
    public ChoppableTree Log;
}
