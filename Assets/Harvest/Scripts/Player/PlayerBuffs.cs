using System;
using System.Collections.Generic;

[Serializable]
public class PlayerBuffs
{
    public event Action OnChanged = delegate { };

    public PlayerBuffHandle Apply(params PlayerBuffEffect[] effects)
    {
        int id = nextId++;
        activeBuffs[id] = effects;
        OnChanged?.Invoke();
        return new PlayerBuffHandle(id, OnBuffRemoved);
    }

    public void AccumulateStatFromBuffs(Stat stat, StatValues values)
    {
        foreach (var effects in activeBuffs.Values)
        {
            foreach (var effect in effects)
            {
                if (effect.Stat == stat)
                {
                    effect.Accumulate(values);
                }
            }
        }
    }

    private readonly Dictionary<int, PlayerBuffEffect[]> activeBuffs = new();
    private int nextId = 0;

    private void OnBuffRemoved(int id)
    {
        if (!activeBuffs.ContainsKey(id)) return;
        activeBuffs.Remove(id);
        OnChanged?.Invoke();
    }
}

[Serializable]
public class PlayerBuffEffect
{
    public Stat Stat;
    public float Additive = 0f;
    public float Multiplicative = 0f;

    public void Accumulate(StatValues values)
    {
        values.Additive += Additive;
        values.Multiplicative += Multiplicative;
    }
}

public class PlayerBuffHandle : IDisposable
{
    public readonly int Id;
    private readonly Action<int> onRemoved;

    public PlayerBuffHandle(int id, Action<int> onRemoved)
    {
        Id = id;
        this.onRemoved = onRemoved;
    }

    public void Dispose() => onRemoved(Id);
}