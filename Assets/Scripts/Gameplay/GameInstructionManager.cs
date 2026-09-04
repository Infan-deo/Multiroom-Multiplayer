using System;
using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Timers;
using TMPro;
using UnityEngine;

public class GameInstructionManager : NetworkBehaviour
{
    [Header("GameInfo")] public GameObject InstructionPanel;
    public TextMeshProUGUI GameNameText;
    public TextMeshProUGUI GameInstructionText;

    [Header("Countdown")] [SerializeField] private TMP_Text countdownText;
    [SerializeField] private int countdownTime = 10;

    [Header("DOTween")] [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float scaleIn = 1.4f;
    [SerializeField] private float scaleOut = 0.7f;

    private readonly SyncVar<int> countdown = new();

    public Action OnTimerFinished;

    public override void OnStartServer()
    {
        base.OnStartServer();
        StartTimer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        countdownText.text = countdownTime.ToString();
        OpenInstructionPanel();
    }

    [ObserversRpc]
    public void OpenInstructionPanel()
    {
        InstructionPanel.SetActive(true);
    }

    [Server]
    private void StartTimer()
    {
        countdown.Value = countdownTime;

        Debug.Log(
            $"StartTimerServer: {countdown.Value}"
        );

        TimersManager.SetTimer(
            this,
            1f,
            (uint)countdown.Value,
            DecreaseCountdown
        );
    }

    [Server]
    private void DecreaseCountdown()
    {
        countdown.Value--;

        Debug.Log(
            $"Server Countdown: {countdown.Value}"
        );

        ShowCountdownRpc(countdown.Value);

        if (countdown.Value <= 0)
        {
            FinishCountdown();
        }
    }

    [ObserversRpc]
    private void ShowCountdownRpc(int seconds)
    {
        if (countdownText == null)
            return;

        countdownText.text = seconds.ToString();

        PlayNumberEffect();
    }

    private void PlayNumberEffect()
    {
        countdownText.DOKill();

        countdownText.alpha = 0f;

        countdownText.transform.localScale =
            Vector3.one * scaleIn;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            countdownText.DOFade(
                1f,
                fadeInDuration
            )
        );

        sequence.AppendInterval(0.5f);

        sequence.Append(
            countdownText.DOFade(
                0f,
                fadeOutDuration
            )
        );

        sequence.Join(
            countdownText.transform.DOScale(
                scaleOut,
                fadeOutDuration
            )
        );
    }

    [Server]
    private void FinishCountdown()
    {
        Debug.Log("Countdown Finished");

        FinishCountdownRpc();

        OnTimerFinished?.Invoke();
    }

    [ObserversRpc]
    private void FinishCountdownRpc()
    {
        if (InstructionPanel != null)
            InstructionPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (countdownText != null)
            countdownText.DOKill();
    }
}