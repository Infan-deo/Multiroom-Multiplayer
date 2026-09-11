using System.Collections.Generic;
using FishNet.Object;
using FishNet;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpectateSystem : NetworkBehaviour
{
    public Transform defaultPosition;

    [Header("Spectating Panel")]
    public GameObject spectatePanel;
    public TextMeshProUGUI spectateText;
    public Button NextButton;
    public Button PreviousButton;

    private AllRoomPlayerManager allRoomPlayerManager;

    private readonly List<GetPlayerInfo> spectatablePlayers = new();
    private Camera localPlayerCamera;   // the camera to restore when spectating stops
    private Camera currentSpectateCamera;
    private int currentIndex = -1;
    private bool isSpectating;

    [Inject]
    public void Construct(AllRoomPlayerManager allRoomPlayerManager)
    {
        this.allRoomPlayerManager = allRoomPlayerManager;
    }

    /// <summary>
    /// Called by the local player's PlayerController/camera setup once it knows
    /// which camera belongs to the owning client.
    /// </summary>
    public void SetLocalPlayerCamera(Camera cam)
    {
        localPlayerCamera = cam;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        allRoomPlayerManager.roomPlayersinfo.OnChange += OnRoomPlayersInfoChanged;
        RebuildSpectatablePlayers();

        if (NextButton != null) NextButton.onClick.AddListener(SpectateNext);
        if (PreviousButton != null) PreviousButton.onClick.AddListener(SpectatePrevious);

        if (spectatePanel != null) spectatePanel.SetActive(false);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (allRoomPlayerManager != null)
            allRoomPlayerManager.roomPlayersinfo.OnChange -= OnRoomPlayersInfoChanged;

        if (NextButton != null) NextButton.onClick.RemoveListener(SpectateNext);
        if (PreviousButton != null) PreviousButton.onClick.RemoveListener(SpectatePrevious);
    }

    private void OnRoomPlayersInfoChanged(SyncDictionaryOperation op, int key, GetPlayerInfo value, bool asServer)
    {
        if (asServer) return;
        RebuildSpectatablePlayers();
    }

    private void RebuildSpectatablePlayers()
    {
        spectatablePlayers.Clear();

        int localClientId = InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Connection != null
            ? InstanceFinder.ClientManager.Connection.ClientId
            : -1;

        foreach (var kvp in allRoomPlayerManager.roomPlayersinfo)
        {
            if (kvp.Value == null) continue;
            if (kvp.Key == localClientId) continue; // don't spectate yourself
            spectatablePlayers.Add(kvp.Value);
        }

        if (isSpectating && currentSpectateCamera != null)
        {
            currentIndex = spectatablePlayers.FindIndex(p => p.playerCamera == currentSpectateCamera);
            if (currentIndex == -1)
                StopSpectating(); // our target left/despawned
        }
    }

    public void SpectateNext() => CycleSpectateTarget(1);
    public void SpectatePrevious() => CycleSpectateTarget(-1);

    private void CycleSpectateTarget(int direction)
    {
        if (spectatablePlayers.Count == 0)
        {
            StopSpectating();
            return;
        }

        currentIndex = (currentIndex + direction + spectatablePlayers.Count) % spectatablePlayers.Count;
        GetPlayerInfo target = spectatablePlayers[currentIndex];

        if (target == null || target.playerCamera == null)
        {
            RebuildSpectatablePlayers();
            return;
        }

        // Swap target.PlayerName for target.SyncedPlayerName.Value if you applied
        // the SyncVar-based name fix from earlier.
        SwitchToCamera(target.playerCamera, target.PlayerName);
    }

    private void SwitchToCamera(Camera newCamera, string label = null)
    {
        if (!isSpectating)
        {
            // First time entering spectate mode: turn off our own view.
            SetCameraActive(localPlayerCamera, false);
        }
        else if (currentSpectateCamera != null)
        {
            SetCameraActive(currentSpectateCamera, false);
        }

        currentSpectateCamera = newCamera;
        isSpectating = true;
        SetCameraActive(currentSpectateCamera, true);

        if (spectatePanel != null) spectatePanel.SetActive(true);
        if (spectateText != null) spectateText.text = label ?? string.Empty;
    }

    public void StopSpectating()
    {
        if (currentSpectateCamera != null)
            SetCameraActive(currentSpectateCamera, false);

        currentSpectateCamera = null;
        currentIndex = -1;
        isSpectating = false;

        SetCameraActive(localPlayerCamera, true);

        if (spectatePanel != null) spectatePanel.SetActive(false);
    }

    private static void SetCameraActive(Camera cam, bool active)
    {
        if (cam == null) return;

        cam.enabled = active;

        if (cam.TryGetComponent(out AudioListener listener))
            listener.enabled = active;
    }
    /// <summary>
    /// Call this when the local player should enter spectate mode
    /// (e.g. on death, after the round ends, etc).
    /// </summary>
    public void StartSpectating()
    {
        if (isSpectating)
            return;

        RebuildSpectatablePlayers();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (spectatablePlayers.Count == 0)
        {
            // Nobody to spectate yet — still hide the local view and show
            // the panel/defaultPosition so the player isn't stuck looking
            // at their own dead body.
            SetCameraActive(localPlayerCamera, false);
            isSpectating = true;

            if (spectatePanel != null) spectatePanel.SetActive(true);
            if (spectateText != null) spectateText.text = "Waiting for players...";
            return;
        }

        currentIndex = -1;
        CycleSpectateTarget(1); // lands on index 0, handles the localPlayerCamera → first target switch
    }
    
    // Called by the server (e.g. from PlayerController when this player dies/is eliminated)
    [Server]
    public void ServerBeginSpectating(NetworkConnection conn)
    {
        TargetBeginSpectating(conn);
    }

    [TargetRpc]
    private void TargetBeginSpectating(NetworkConnection conn)
    {
        StartSpectating();
    }

    [Server]
    public void ServerEndSpectating(NetworkConnection conn)
    {
        TargetEndSpectating(conn);
    }

    [TargetRpc]
    private void TargetEndSpectating(NetworkConnection conn)
    {
        StopSpectating();
    }
}