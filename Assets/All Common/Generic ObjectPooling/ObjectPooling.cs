using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> pool = new();

    private readonly T prefab;
    private readonly Transform parent;

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateObject();
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    private T CreateObject()
    {
        return Object.Instantiate(prefab, parent);
    }

    public T Get()
    {
        T obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            obj = CreateObject();
        }

        obj.gameObject.SetActive(true);

        if (obj.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnSpawn();

        return obj;
    }

    public void Return(T obj)
    {
        if (obj == null)
            return;

        if (obj.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnDespawn();

        obj.gameObject.SetActive(false);

        pool.Enqueue(obj);
    }

    public void PreWarm(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            T obj = CreateObject();
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }
}