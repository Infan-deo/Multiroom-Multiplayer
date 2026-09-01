using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
    private readonly Dictionary<Component, object> pools = new();


    public void CreatePool<T>(T prefab, int size) where T : Component
    {
        if (pools.ContainsKey(prefab))
            return;

        pools[prefab] = new ObjectPool<T>(prefab, size, transform);
    }

    public T Get<T>(T prefab) where T : Component
    {
        if (!pools.TryGetValue(prefab, out object pool))
        {
            CreatePool(prefab, 5);
            pool = pools[prefab];
        }

        return ((ObjectPool<T>)pool).Get();
    }

    public void Return<T>(T prefab, T obj) where T : Component
    {
        if (!pools.TryGetValue(prefab, out object pool))
        {
            Destroy(obj.gameObject);
            print($"No pool found for {prefab.name}. Destroying object instead.");
            return;
        }

        ((ObjectPool<T>)pool).Return(obj);
    }
}