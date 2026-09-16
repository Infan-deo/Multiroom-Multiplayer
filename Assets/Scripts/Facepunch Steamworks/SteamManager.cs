using System;
using System.Collections;
using Steamworks;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance;
    public SO_PlayerInfo  PlayerInfo;

    [SerializeField]
    private uint appId = 480;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        InitializeSteam();
    }

   

    private void InitializeSteam()
    {
        try
        {
            SteamClient.Init(appId, true);

            Debug.Log(
                $"Steam initialized: {SteamClient.Name}"
            );
            
            
            PlayerInfo.playerName = SteamClient.Name;

            Debug.Log(
                $"Steam ID: {SteamClient.SteamId}"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                $"Steam initialization failed: {e}"
            );
        }
    }

    private void Update()
    {
        SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }
}