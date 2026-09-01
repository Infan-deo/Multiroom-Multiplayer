using System;
using TMPro;
using Unity.Services.LevelPlay;
using UnityEngine;

public class LevelPlayAdsMobile : MonoBehaviour
{
    [Header("App key")] [SerializeField] private string androidKey;
    [SerializeField] private string iosKey;

    [Header("Banner ad unit id")] [SerializeField]
    private string androidBannerAdUnitId;

    [SerializeField] private string iosBannerAdUnitId;

    [Header("Interstitial ad unit id")] [SerializeField]
    private string androidInterstitialAdUnitId;

    [SerializeField] private string iosInterstitialAdUnitId;

    [Header("Rewarded ad unit id")] [SerializeField]
    private string androidRewardedAdUnitId;

    [SerializeField] private string iosRewardedAdUnitId;
    
    [Header("Coin Text")]
    public TextMeshProUGUI cointext;

    private LevelPlayBannerAd bannerAd;
    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedAd;

    public int Coins
    {
        get => PlayerPrefs.GetInt("Coins", 0);
        set
        {
            PlayerPrefs.SetInt("Coins", value);
            PlayerPrefs.Save();

            UpdateCoins();
        }
    }

    private void UpdateCoins()
    {
        cointext.text = Coins.ToString();
    }

    private string AppKey
    {
        get
        {
#if UNITY_ANDROID
            return androidKey;
#elif UNITY_IOS
                return iosKey;
#else
                return "";
#endif
        }
    }

    private string BannerAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidBannerAdUnitId;
#elif UNITY_IOS
                return iosBannerAdUnitId;
#else
                return "";
#endif
        }
    }

    private string InterstitialAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidInterstitialAdUnitId;
#elif UNITY_IOS
                return iosInterstitialAdUnitId;
#else
                return "";
#endif
        }
    }

    private string RewardedAdUnitId
    {
        get
        {
#if UNITY_ANDROID
            return androidRewardedAdUnitId;
#elif UNITY_IOS
                return iosRewardedAdUnitId;
#else
                return "";
#endif
        }
    }


    public void Start()
    {
        LevelPlay.ValidateIntegration();
        // Register OnInitFailed and OnInitSuccess listeners
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        // SDK init
        LevelPlay.Init(AppKey);
        UpdateCoins();
    }

    private void SdkInitializationCompletedEvent(LevelPlayConfiguration obj)
    {
        CreateBannerAd();
        CreateInterstitialAd();
        CreateRewardedAd();
        Debug.Log("SDK Initialization Completed");
    }

    private void SdkInitializationFailedEvent(LevelPlayInitError obj)
    {
        Debug.Log("SDK Initialization Failed");
    }

    #region Banner Ads

    private void CreateBannerAd()
    {
        var adConfig = new LevelPlayBannerAd.Config.Builder()
            .SetPosition(LevelPlayBannerPosition.BottomCenter).Build();

        bannerAd = new LevelPlayBannerAd(BannerAdUnitId, adConfig);

        bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
        bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
        bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
        bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
        bannerAd.OnAdClicked += BannerOnAdClickedEvent;
        bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
        bannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
        bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;
    }

    public void ShowBannerAd()
    {
        bannerAd.ShowAd();
        Debug.Log("#MYGAME Banner Ad Clicked");
    }

    public void HideBannerAd()
    {
        bannerAd.HideAd();
    }

    public void DestroyBannerAd()
    {
        bannerAd.DestroyAd();
    }

    void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void BannerOnAdLoadFailedEvent(LevelPlayAdError ironSourceError)
    {
    }

    void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
        Debug.Log("#MYGAME Banner Ad Clicked");
    }

    void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void BannerOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
    }

    void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo)
    {
    }

    void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo)
    {
    }

    #endregion

    #region Interstitial Ads

    private void CreateInterstitialAd()
    {
        interstitialAd = new LevelPlayInterstitialAd(InterstitialAdUnitId);
        // Register to interstitial events
        interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
        interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
        interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
        interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
        interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
        interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
        interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;
        // LoadInterstitialAd();
    }

    public void LoadInterstitialAd()
    {
        interstitialAd.LoadAd();
        Debug.Log("#MYGAME Interstitial Ad Loaded");
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
            Debug.Log("#MYGAME Interstitial Ad Shown");
        }
    }

    // Implement the events
    void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
    {
    }

    void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
    }

    void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
        // LoadInterstitialAd();
    }

    void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
    {
    }

    #endregion

    #region Rewarded ads

    private void CreateRewardedAd()
    {
        rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitId);
        // Register to interstitial events
        // Register to Rewarded events
        rewardedAd.OnAdLoaded += RewardedOnAdLoadedEvent;
        rewardedAd.OnAdLoadFailed += RewardedOnAdLoadFailedEvent;
        rewardedAd.OnAdDisplayed += RewardedOnAdDisplayedEvent;
        rewardedAd.OnAdDisplayFailed += RewardedOnAdDisplayFailedEvent;
        rewardedAd.OnAdRewarded += RewardedOnAdRewardedEvent;
        rewardedAd.OnAdClosed += RewardedOnAdClosedEvent;
// Optional
        rewardedAd.OnAdClicked += RewardedOnAdClickedEvent;
        rewardedAd.OnAdInfoChanged += RewardedOnAdInfoChangedEvent;
        LoadRewardedAdAd();
    }

    public void LoadRewardedAdAd()
    {
        rewardedAd.LoadAd();
        Debug.Log("#MYGAME rewardedAd Ad Loaded");
    }

    public void ShowRewardedAdAd()
    {
        if (rewardedAd.IsAdReady())
        {
            rewardedAd.ShowAd();
            Debug.Log("#MYGAME rewardedAd Ad shown");
        }
    }

    // Implement the events
    void RewardedOnAdLoadedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void RewardedOnAdLoadFailedEvent(LevelPlayAdError error)
    {
    }

    void RewardedOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void RewardedOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
    }

    void RewardedOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward adReward)
    {
        string rewardName = adReward.Name;
        int rewardAmount = adReward.Amount;
        Coins += rewardAmount;
        Debug.Log("#MYGAME Rewarded Ad Reward Loaded: " + rewardName + ".  Reward Amount: " + rewardAmount);
    }

    void RewardedOnAdClosedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void RewardedOnAdClickedEvent(LevelPlayAdInfo adInfo)
    {
    }

    void RewardedOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
    {
    }

    #endregion
}