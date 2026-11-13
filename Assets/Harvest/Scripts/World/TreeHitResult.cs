using UnityEngine;

public enum TreeHitOutcome
{
    Miss, Blunt, Clean, Splinter
}

public struct TreeHitInput
{
    public Vector3 Position;
    public float Strength;
    public float Sharpness;
    public float Accuracy;
    public float BitWidth;
}

public struct TreeHitResult
{
    public TreeHitOutcome Outcome;
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2Int Cell;
    public float BiteQuality;
    public float Depth;
    public float LitresRemoved;
    public float Damage;
    public bool CausedSplit;
    public ChoppableTree Log;
}
