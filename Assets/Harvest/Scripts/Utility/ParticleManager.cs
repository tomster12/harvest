using UnityEngine;
using System.Collections.Generic;

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }


    public ParticleSystem Spawn(ParticleEffect type, Vector3 position, Quaternion rotation)
    {
        if (!_registry.TryGetValue(type, out var prefab)) return null;

        var particles = Instantiate(prefab, position, rotation);

        Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);

        return particles;
    }

    [Header("Entries")]
    [SerializeField] private ParticleEntry[] entries;

    private Dictionary<ParticleEffect, ParticleSystem> _registry;

    protected void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _registry = new();
        foreach (var e in entries)
        {
            _registry[e.type] = e.prefab;
        }
    }
}

public enum ParticleEffect
{
    TreeChop
}

[System.Serializable]
public struct ParticleEntry
{
    public ParticleEffect type;
    public ParticleSystem prefab;
}
