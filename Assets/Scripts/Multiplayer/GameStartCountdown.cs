using System;
using DG.Tweening;
using FishNet.Object;
using TMPro;
using Timers;
using UnityEngine;

public class GameStartCountdown : NetworkBehaviour
{
    [Header("Countdown")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private int countdownTime = 5;

    [Header("DOTween")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float scaleIn = 1.4f;
    [SerializeField] private float scaleOut = 0.7f;
    
    public Action OnCountdownFinished;

    private int countdown;
    private bool countingDown;

  

    // =========================================================
    // SERVER
    // =========================================================

    public override void OnStartServer()
    {
        base.OnStartServer();

        countdown = 0;
        countingDown = false;

       
    }

    

    [ContextMenu("Start Countdown")]
    public void StartCountdownServer()
    {
        StartCountdown();
    }

    [Server]
    public void StartCountdown()
    {
        if (countingDown)
            return;

        countingDown = true;
        countdown = countdownTime;

        Debug.Log(
            $"[GameStartCountdown] Started: {countdown}s"
        );

        // Tell clients to display the countdown.
        StartCountdownRpc();

        // Tick every second.
        TimersManager.SetTimer(
            this,
            1f,
            (uint)countdownTime,
            CountdownTick
        );
    }

    [Server]
    private void CountdownTick()
    {
        countdown--;

       

        if (countdown > 0)
        {
            ShowCountdownRpc(countdown);
            return;
        }

        // Countdown finished.
        countdown = 0;
        countingDown = false;
        
        ShowGoRpc();

        StartGame();
    }

    // =========================================================
    // SERVER → CLIENT
    // =========================================================

    [ObserversRpc]
    private void StartCountdownRpc()
    {
        if (countdownText == null)
            return;

        countdownText.gameObject.SetActive(true);

        countdownText.DOKill();

        countdownText.alpha = 0f;
        countdownText.transform.localScale = Vector3.one;

        countdownText.text = countdownTime.ToString();

        PlayNumberEffect();
    }

    [ObserversRpc]
    private void ShowCountdownRpc(int seconds)
    {
        if (countdownText == null)
            return;

        countdownText.gameObject.SetActive(true);

        countdownText.text = seconds.ToString();

        PlayNumberEffect();
    }

    [ObserversRpc]
    private void ShowGoRpc()
    {
        if (countdownText == null)
            return;

        countdownText.gameObject.SetActive(true);

        countdownText.text = "GO!";

        PlayGoEffect();
    }

    // =========================================================
    // DOTWEEN
    // =========================================================

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

    private void PlayGoEffect()
    {
        countdownText.DOKill();

        countdownText.alpha = 0f;

        countdownText.transform.localScale =
            Vector3.one * 0.5f;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            countdownText.DOFade(
                1f,
                0.2f
            )
        );

        sequence.Join(
            countdownText.transform
                .DOScale(
                    1.3f,
                    0.35f
                )
                .SetEase(Ease.OutBack)
        );

        sequence.AppendInterval(0.5f);

        sequence.Append(
            countdownText.DOFade(
                0f,
                0.3f
            )
        );

        sequence.OnComplete(() =>
        {
            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
        });
    }

    // =========================================================
    // GAME START
    // =========================================================

    [Server]
    private void StartGame()
    {
        Debug.Log(
            "[GameStartCountdown] Countdown finished. Starting game."
        );
        
        OnCountdownFinished?.Invoke();
       
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (countdownText != null)
            countdownText.DOKill();

    }
}