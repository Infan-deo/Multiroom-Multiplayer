using FishNet.Object;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class GameStartCountdown : NetworkBehaviour
{
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float countdownTime = 5f;

    [Header("DOTween")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float scaleIn = 1.4f;
    [SerializeField] private float scaleOut = 0.7f;

    private float timer;
    private int lastSecond = -1;
    private bool countingDown;

    public override void OnStartServer()
    {
        base.OnStartServer();
        timer = countdownTime;
       
    }

    [ContextMenu("StartCountdown")]
    public void StartCountdown()
    {
        countdownText.gameObject.SetActive(true);
        timer = countdownTime;
        countingDown = true;
    }

    private void Update()
    {
        if (!IsServerInitialized || !countingDown)
            return;

        timer -= Time.deltaTime;

        int seconds =
            Mathf.CeilToInt(timer);

        if (seconds != lastSecond)
        {
            lastSecond = seconds;

            ShowCountdownRpc(seconds);
        }

        if (timer <= 0f)
        {
            countingDown = false;

            ShowGoRpc();

            // Start your Bomb Tag game here.
            // StartGame();
        }
    }

    [ObserversRpc]
    private void ShowCountdownRpc(int seconds)
    {
        if (seconds <= 0)
            return;

        countdownText.text = seconds.ToString();

        PlayNumberEffect();
    }

    [ObserversRpc]
    private void ShowGoRpc()
    {
        countdownText.text = "GO!";

        PlayGoEffect();
    }

    private void PlayNumberEffect()
    {
        countdownText.DOKill();

        countdownText.alpha = 0f;
        countdownText.transform.localScale =
            Vector3.one * scaleIn;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            countdownText
                .DOFade(1f, fadeInDuration)
        );

        sequence.AppendInterval(0.5f);

        sequence.Append(
            countdownText
                .DOFade(0f, fadeOutDuration)
        );

        sequence.Join(
            countdownText.transform
                .DOScale(scaleOut, fadeOutDuration)
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
            countdownText
                .DOFade(1f, 0.2f)
        );

        sequence.Join(
            countdownText.transform
                .DOScale(1.3f, 0.35f)
                .SetEase(Ease.OutBack)
        );

        sequence.AppendInterval(0.5f);

        sequence.Append(
            countdownText
                .DOFade(0f, 0.3f)
        );

        sequence.OnComplete(() =>
        {
            countingDown = false;
            countdownText.gameObject.SetActive(false);
        });
    }

    private void OnDestroy()
    {
        if (countdownText != null)
            countdownText.DOKill();
    }
}