using System;
using System.Collections;
using System.Linq;
using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Timers;
using TMPro;
using UnityEngine;
using MEC;


public class BoomTagSystem : NetworkBehaviour
{
    private GameInstructionManager instructionManager;
    private GameStartCountdown gameStartCountdown;
    private AllRoomPlayerManager allRoomPlayerManager;
    private SpectateSystem spectateSystem;
    public TextMeshProUGUI boomTimerText;
    public TextMeshProUGUI ShowPlayerEliminatedText;
    public int boomTiggerCountdown;
    public Transform boomParent;

    [Header("DOTween")] [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float scaleIn = 1.4f;
    [SerializeField] private float scaleOut = 0.7f;

    private readonly SyncVar<int> bombcountdown = new();
    public readonly SyncDictionary<NetworkObject, bool> playerHasBomb = new();
    public readonly SyncList<NetworkObject> allThePlayersInMatch = new();
    public readonly SyncList<GetPlayerInfo> GetPlayerInfos = new();
    public readonly SyncVar<bool> canTransferBomb = new();


    [Inject]
    public void Construct(GameInstructionManager instructionManager, AllRoomPlayerManager allRoomPlayerManager,
        GameStartCountdown gameStartCountdown,SpectateSystem spectateSystem)
    {
        this.instructionManager = instructionManager;
        this.allRoomPlayerManager = allRoomPlayerManager;
        this.gameStartCountdown = gameStartCountdown;
        this.spectateSystem = spectateSystem;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        allRoomPlayerManager.OnAllPlayerSpawned += AllRoomPlayerManager_OnAllPlayerSpawned;
        instructionManager.OnTimerFinished += InstructionManager_OnTimerFinished;
        gameStartCountdown.OnCountdownFinished += GameStartCountdown_OnCountdownFinished;
        canTransferBomb.Value = false;
    }


    [Server]
    private void AllRoomPlayerManager_OnAllPlayerSpawned()
    {
        foreach (var player in allRoomPlayerManager.RoomPlayerspPlayerControllers)
        {
            playerHasBomb.Add(player.NetworkObject, false);
            allThePlayersInMatch.Add(player.NetworkObject);
            GetPlayerInfos.Add(player.itsOwnInfo);
        }

        foreach (GetPlayerInfo playerInfo in GetPlayerInfos)
        {
            playerInfo.OnPlayerInteractWithAnother += PlayerInteractInfo_OnPlayerInteractWithAnother;
        }
    }

    [Server]
    private void PlayerInteractInfo_OnPlayerInteractWithAnother(object sender, GetPlayerInfo.NetworkObjEventArgs e)
    {
        Debug.Log("Sd");
        if (TryTransferBomb(e.from, e.to))
        {
            Debug.Log("Sd1");
            TransferBomb(e.from, e.to);
        }
    }

    public void OnDestroy()
    {
        instructionManager.OnTimerFinished -= InstructionManager_OnTimerFinished;
        gameStartCountdown.OnCountdownFinished -= GameStartCountdown_OnCountdownFinished;
    }

    [Server]
    private void GameStartCountdown_OnCountdownFinished()
    {
        allRoomPlayerManager.SetAllPlayersMove(true);
        StartGame();
    }

    [Server]
    private void InstructionManager_OnTimerFinished()
    {
        gameStartCountdown.StartCountdownServer();
        SelectRandomBombPlayer();
    }

    [Server]
    private void RestartGameUntilSinglePlayerExits()
    {
        if (playerHasBomb.Count == 1)
        {
            var player = playerHasBomb.First();
            NetworkObject playerObject = player.Key;
            UpdatePlayerEliminatedText(allRoomPlayerManager.GetPlayerName(playerObject) + " Won!!");
            PlayShowPlayerEliminatedEffect(false);
        }
        else
        {
            PlayShowPlayerEliminatedEffect(true);
            StartCoroutine(RestartGame());
        }
    }

    [Server]
    public IEnumerator RestartGame()
    {
        yield return new WaitForSeconds(2.0f);
        canTransferBomb.Value = false;
        gameStartCountdown.StartCountdownServer();
        SelectRandomBombPlayer();
    }

    private void StartGame()
    {
        canTransferBomb.Value = true;
        bombActive(true);
        StartTimer();
    }

    [ObserversRpc]
    private void bombActive(bool state)
    {
        boomParent.gameObject.SetActive(state);
    }

    [Server]
    private void SelectRandomBombPlayer()
    {
        if (playerHasBomb.Count == 0)
        {
            Debug.Log("[BOMB] No players available.");
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, playerHasBomb.Count);

        int currentIndex = 0;

        foreach (var player in playerHasBomb.Keys)
        {
            if (currentIndex == randomIndex)
            {
                SetBombPlayer(player);
                break;
            }

            currentIndex++;
        }
    }

    [Server]
    private void SetBombPlayer(NetworkObject player)
    {
        playerHasBomb[player] = true;
        SetBoomToClient(player);
    }

    [ObserversRpc]
    private void SetBoomToClient(NetworkObject player)
    {
        ManageBoom manageBoom =
            player.GetComponent<ManageBoom>();

        manageBoom.BoomEnabled = true;
    }

    [Server]
    public bool TryTransferBomb(NetworkObject from, NetworkObject to)
    {
        if (!canTransferBomb.Value)
            return false;
        if (playerHasBomb.ContainsKey(from))
        {
            if (!playerHasBomb[from])
            {
                Debug.Log("Player(form) doesn't have bomb");
                return false;
            }
        }

        if (playerHasBomb.ContainsKey(to))
        {
            if (playerHasBomb[to])
            {
                Debug.Log("Player(to) does have bomb");
                return false;
            }
        }

        return true;
    }

    [Server]
    public void TransferBomb(
        NetworkObject from,
        NetworkObject to)
    {
        Debug.Log("Sd2");
        playerHasBomb[from] = false;
        playerHasBomb[to] = true;
        EnableBombInNetworkObject(from, to);
    }

    [ObserversRpc]
    public void EnableBombInNetworkObject(NetworkObject from, NetworkObject to)
    {
        Debug.Log("Sd5");
        ManageBoom manageBoomFrom =
            from.GetComponent<ManageBoom>();
        ManageBoom manageBoomTo =
            to.GetComponent<ManageBoom>();
        if (manageBoomFrom == null)
        {
            Debug.Log("Sd5 null");
            
            return;
        }
        if (manageBoomTo == null)
        {
            Debug.Log("Sd5 null ");
            
            return;
        }
        manageBoomFrom.BoomEnabled = false;
        manageBoomTo.BoomEnabled = true;
    }

    [Server]
    private void StartTimer()
    {
        bombcountdown.Value = boomTiggerCountdown;

        Debug.Log(
            $"StartTimerServer: {bombcountdown.Value}"
        );

        TimersManager.SetTimer(
            this,
            1f,
            (uint)bombcountdown.Value,
            DecreaseCountdown
        );
    }

    [Server]
    private void DecreaseCountdown()
    {
        bombcountdown.Value--;

        // Debug.Log(
        //     $"Server Countdown: {bombcountdown.Value}"
        // );

        ShowCountdownRpc(bombcountdown.Value);

        if (bombcountdown.Value <= 0)
        {
            FinishCountdown();
        }
    }

    [ObserversRpc]
    private void ShowCountdownRpc(int seconds)
    {
        if (boomTimerText == null)
            return;

        boomTimerText.text = seconds.ToString();

        PlayNumberEffect();
    }

    private void PlayNumberEffect()
    {
        boomTimerText.DOKill();

        boomTimerText.alpha = 0f;

        boomTimerText.transform.localScale =
            Vector3.one * scaleIn;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            boomTimerText.DOFade(
                1f,
                fadeInDuration
            )
        );

        sequence.AppendInterval(0.5f);

        sequence.Append(
            boomTimerText.DOFade(
                0f,
                fadeOutDuration
            )
        );

        sequence.Join(
            boomTimerText.transform.DOScale(
                scaleOut,
                fadeOutDuration
            )
        );
    }


    [ObserversRpc]
    private void PlayShowPlayerEliminatedEffect(bool CanRestart)
    {
        ShowPlayerEliminatedText.gameObject.SetActive(true);

        ShowPlayerEliminatedText.DOKill();

        ShowPlayerEliminatedText.alpha = 0f;

        ShowPlayerEliminatedText.transform.localScale =
            Vector3.one * 0.5f;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            ShowPlayerEliminatedText.DOFade(
                1f,
                0.2f
            )
        );

        sequence.Join(
            ShowPlayerEliminatedText.transform
                .DOScale(
                    1.3f,
                    0.35f
                )
                .SetEase(Ease.OutBack)
        );

        sequence.AppendInterval(0.5f);

        sequence.Append(
            ShowPlayerEliminatedText.DOFade(
                0f,
                0.3f
            )
        );

        sequence.OnComplete(() =>
        {
            if (CanRestart)
            {
            }

            if (ShowPlayerEliminatedText != null)
                ShowPlayerEliminatedText.gameObject.SetActive(false);
        });
    }

    [Server]
    private void FinishCountdown()
    {
        Debug.Log("Countdown Finished");
        canTransferBomb.Value = false;
        CheckWhichPlayerHasBomb();
    }

    [Server]
    public void CheckWhichPlayerHasBomb()
    {
        foreach (var player in playerHasBomb)
        {
            if (player.Value)
            {
                //Transfer Player to spectate
                ExplodeBoom(player.Key);
                allRoomPlayerManager.DisablePlayer(player.Key);
                spectateSystem.ServerBeginSpectating(player.Key.Owner);
                UpdatePlayerEliminatedText(allRoomPlayerManager.GetPlayerName(player.Key) + " Eliminated!!");
                playerHasBomb.Remove(player.Key);
                RestartGameUntilSinglePlayerExits();
                return;
            }
        }
    }

    [ObserversRpc]
    public void ExplodeBoom(NetworkObject playerNetworkObject)
    {
        ManageBoom manageBoom = playerNetworkObject.GetComponent<ManageBoom>();
        Timing.RunCoroutine(manageBoom._ExplodeBoom());
    }

    [ObserversRpc]
    private void UpdatePlayerEliminatedText(string text)
    {
        ShowPlayerEliminatedText.text = text;
    }
}