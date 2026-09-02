using System.Collections.Generic;
using UnityEngine;



public class Container : MonoBehaviour
{
    [SerializeField] private List<MonoBehaviour> injectables;

    private ObjectResolver _resolver;

    private void Awake()
    {
        _resolver = new ObjectResolver();
    }

    private void OnEnable()
    {
        foreach (var obj in injectables)
        {
            if (obj == null)
                continue;

            _resolver.RegisterInstance(obj);
        }

        foreach (var obj in injectables)
        {
            if (obj == null)
                continue;

            _resolver.InjectInto(obj);
        }
    }

    private void OnDisable()
    {
        foreach (var obj in injectables)
        {
            if (obj == null)
                continue;

            _resolver.UnregisterInstance(obj.GetType());
        }
    }
}

    //example:
    // [Inject]
    // public void Inject(MainMenuService mainMenuService)
    // {
    //     _mainMenuService = mainMenuService;
    // }


