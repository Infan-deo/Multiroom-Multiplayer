using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
public class ObjectResolver
{
    public readonly HashSet<Type> _registrations = new();
    private readonly Dictionary<Type, object> _instancePerTypeMap = new();

    public void Register<T>()
    {
        _registrations.Add(typeof(T));
    }
    // public void RegisterInstance<T>(T instance)
    // {
    //     _instancePerTypeMap[typeof(T)] = instance;

    //     _registrations.Add(instance.GetType());
    // }

    // public void UnregisterInstance<T>()
    // {
    //     _instancePerTypeMap.Remove(typeof(T));
    //     _registrations.Remove(typeof(T));
    // }
    public void RegisterInstance(object instance)
    {
        Type type = instance.GetType();

        _instancePerTypeMap[type] = instance;
        _registrations.Add(type);
    }

    public void UnregisterInstance(Type type)
    {
        _instancePerTypeMap.Remove(type);
        _registrations.Remove(type);
    }

    public T Resolve<T>()
    {
        return (T)Resolve(typeof(T));
    }

    private object Resolve(Type instanceType)
    {
        if (_instancePerTypeMap.TryGetValue(instanceType, out var instance))
        {
            return instance;
        }

        if (!_registrations.Contains(instanceType))
        {
            Debug.LogError($"Couldn't resolve type {instanceType}");
            return default;
        }

        // Only allow creation of non-MonoBehaviour objects
        if (typeof(MonoBehaviour).IsAssignableFrom(instanceType))
        {
            Debug.LogError($"MonoBehaviour must be registered as an instance: {instanceType}");
            return null;
        }

        instance = Activator.CreateInstance(instanceType);
        _instancePerTypeMap[instanceType] = instance;

        InjectInto(instance);
        return instance;

    }

    public void InjectInto(object target)
    {
        var methods = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttribute<InjectAttribute>() != null);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var args = parameters
                .Select(p => Resolve(p.ParameterType))
                .ToArray();

            method.Invoke(target, args);
        }
    }
}
