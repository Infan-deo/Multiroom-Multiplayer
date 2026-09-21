using FishNet;
using Steamworks;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance;
    public SO_PlayerInfo PlayerInfo;

    [SerializeField]
    private uint appId = 480;

    private bool steamInitialized;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (InstanceFinder.IsServerOnly)
            return;

        InitializeSteam();
    }

    private void InitializeSteam()
    {
        try
        {
            SteamClient.Init(appId, true);

            steamInitialized = true;

            Debug.Log($"Steam initialized: {SteamClient.Name}");
            Debug.Log($"Steam ID: {SteamClient.SteamId}");

            if (PlayerInfo != null)
            {
                PlayerInfo.playerName = SteamClient.Name;
            }
            else
            {
                Debug.LogError("PlayerInfo is NULL!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Steam initialization failed: {e}");
            steamInitialized = false;
        }
    }

    private void Update()
    {
        if (InstanceFinder.IsServerOnly)
            return;

        if (!steamInitialized)
            return;

        SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if (InstanceFinder.IsServerOnly)
            return;

        if (!steamInitialized)
            return;

        SteamClient.Shutdown();
        steamInitialized = false;
    }
}