using System.Collections;
using System.Collections.Generic;
using FishNet;
using UnityEngine;

public class WorldSpawnManager : MonoBehaviour
{
    [System.Serializable]
    public class WorldPrefab
    {
        public GameObject prefab;
        public Transform spawnTransform;
        
    }

    [Header("Client World Prefabs")]
    [SerializeField]
    private List<WorldPrefab> worldPrefabs = new List<WorldPrefab>();
    // [Header("Server World Prefabs")]
    // [SerializeField]
    // private List<WorldPrefab> ServerPrefabs = new List<WorldPrefab>();

    [Header("Loading UI")]
    [SerializeField]
    private GameObject loadingPanel;

    

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    public bool IsWorldLoaded { get; private set; }

    private void Awake()
    {
        IsWorldLoaded = false;
    }

    private void Start()
    {
        // Dedicated server doesn't need the client world.
        if (InstanceFinder.IsServerOnly)
        {
            IsWorldLoaded = true;

            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            return;
        }

        // Client loads the world.
        if (InstanceFinder.IsClient)
        {
            StartCoroutine(LoadWorld());
        }
    }
 



    private IEnumerator LoadWorld()
    {
        IsWorldLoaded = false;

        // Show loading screen.
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // Give Unity a frame to display the loading panel.
        yield return null;

        foreach (WorldPrefab worldPrefab in worldPrefabs)
        {
            if (worldPrefab == null ||
                worldPrefab.prefab == null ||
                worldPrefab.spawnTransform == null)
            {
                Debug.LogWarning(
                    "WorldSpawnManager: Prefab or Spawn Transform is missing.",
                    this
                );

                continue;
            }

            Transform spawnPoint = worldPrefab.spawnTransform;

            GameObject spawnedObject = Instantiate(
                worldPrefab.prefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            spawnedObjects.Add(spawnedObject);

            // Spread loading across frames.
            yield return null;
        }

        // World is completely loaded.
        IsWorldLoaded = true;

        // Hide loading screen.
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        Debug.Log("WorldSpawnManager: Client world loaded.");
    }
}

