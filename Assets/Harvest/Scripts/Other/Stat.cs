using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public class Stat
{
    [SerializeField] private readonly float baseValue;
    private readonly List<StatMod> modifiers = new();
    private int nextId = 0;

    public Stat(float baseValue)
    {
        this.baseValue = baseValue;
    }

    public float Evaluate() => modifiers.Aggregate(baseValue, (acc, mod) => mod.Apply(acc));

    public int AddAddMod(float amount)
    {
        int id = nextId++;
        modifiers.Add(new StatAddMod(id, amount));
        return id;
    }

    public int AddMultMod(float amount)
    {
        int id = nextId++;
        modifiers.Add(new StatMultMod(id, amount));
        return id;
    }

    public void RemoveMod(int id)
    {
        var mod = modifiers.FirstOrDefault(m => m.Id == id);
        Assert.IsNotNull(mod);
        modifiers.Remove(mod);
    }
}

public abstract class StatMod
{
    public StatMod(int id)
    {
        Id = id;
    }

    public abstract float Apply(float value);

    public int Id { get; private set; }
}

public class StatAddMod : StatMod
{
    public StatAddMod(int id, float amount) : base(id)
    {
        this.amount = amount;
    }

    public override float Apply(float value) => value + amount;

    private readonly float amount;
}

public class StatMultMod : StatMod
{
    public StatMultMod(int id, float amount) : base(id)
    {
        this.amount = amount;
    }

    public override float Apply(float value) => value * amount;

    private readonly float amount;
}
