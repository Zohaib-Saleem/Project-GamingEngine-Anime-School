using DG.Tweening;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using SolarEngine;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

[Serializable]
public class AdUnitId
{
    public string bannerAdUnitId;
    public string mrecAdUnitId;
    public string InterstitialadUnitId;
    public string RewardadUnitId;
    public string AppOpenAdUnitId;
}

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    private ConsentForm _consentForm;
    

#if UNITY_IOS || Use_RemoteConfing_Android
    
    public bool adSwitchFirebase, AdMobBannerADSwitch, AdMobMRECADSwitch, AdMobInterstitialADSwitch, ADMobAppOpenADSwitch
        , MAXBannerADSwitch, MAXMRECADSwitch, MAXInterstitialADSwitch, MAXAppOpenADSwitch, AppOpenFirstSession, Adtimers,  showAppOpenOnMaxInterFallBack;
#endif
    [Tooltip("Remote Config: dailyrewardbackad — interstitial after daily reward (returning users).")]
    public bool dailyrewardbackad = true;
    [Tooltip("Remote Config: beforemainmenu — first-flow profile / dual-bool daily ad.")]
    public bool beforemainmenu = true;
    [Tooltip("Remote Config: mainmenustartad — START button.")]
    public bool mainmenustartad = true;
    [Tooltip("Remote Config: modeselectionad — mode select (Open World) button.")]
    public bool modeselectionad = true;
    [Tooltip("Remote Config: levelselectad — level card play button.")]
    public bool levelselectad = true;
    [Tooltip("Remote Config: pausebuttonad — gameplay pause button.")]
    public bool pausebuttonad = true;
    [Tooltip("Remote Config: removeadspanelad — interstitial after closing Remove Ads panel.")]
    public bool removeadspanelad = true;
    [FormerlySerializedAs("UseOnlyADMobBanner")]
    [HideInInspector]
    public bool UseOnlyADMobAppOpen, RatedInThisSession;

    private bool ShowingMediumRectNow, AllowedToShowMediumRectAfterADPanel;
    public bool CanShowInterstitialAfterRewardedFallBack;
    [Space]
    public GameObject LoadingAdPanel;
    public GameObject NoInternetPanel;
    public Text AdStatusText;
    [HideInInspector]
    public GameObject IOS_InternetConnection, Android_InternetConnection;
    [Tooltip("If true, script will overwrite all AdMob IDs with official test IDs.")]

    public bool ShowBannerDataSolar;

    public bool TestAd = true;
    [Space(10)]
    public bool UseADMobBanner1;
    public string admobBannerAdUnitId1;
    [Tooltip("Banner Type for Banner1 (Simple=320x50, FullWidth=SmartBanner, Native=SmartBanner).")]
    public BannerType banner1Type = BannerType.Simple;
    public AdPosition banner1Position = AdPosition.TopLeft;

    public bool UseADMobBanner2;
    public string admobBannerAdUnitId2;
    [Tooltip("Banner Type for Banner2 (Simple=320x50, FullWidth=SmartBanner, Native=SmartBanner).")]
    public BannerType banner2Type = BannerType.Simple;
    public AdPosition banner2Position = AdPosition.TopRight;
    public string admobMediumRectAdUnitId;
    [Tooltip("Position of Medium Rectangle on the screen.")]
    public AdPosition mediumRectPosition = AdPosition.BottomLeft;

    public string admobInterstitialAdUnitId;
    public string admobInterstitialAdUnitId2;
    public string admobInterstitialAdUnitId3;

    public bool UsePreLoadRewarded;
    public string admobRewardedAdUnitId;
    public bool UsePreLoadRewardedInterstitial;
    public string admobRewardedInterstitialAdUnitId;
    public string admobAppOpenAdUnitId;

    private float lastAdCallTime = -10f; // initialize so it works at start
    private float adCooldown = 5f;      // 10 seconds cooldown



    /*
    [Header("------- Unity Ads (4.2.2) -------")]
    [Tooltip("Unity Game ID (distinct for iOS vs. Android).")]
    public string unityGameId = "1234567";

    [Header("Unity Ad Placements")]
    public string unityBannerPlacementId = "Banner_Android";
    [Tooltip("Position of Unity Banner on the screen.")]
    //UnityADsHamza public BannerPosition UnityBannerPositon = BannerPosition.TOP_LEFT;
    public string unityInterstitialPlacementId = "Interstitial_Android";
    public string unityRewardedPlacementId = "Rewarded_Android";
    */

    //public int banner_imppCount;
    #region Official Test IDs (AdMob)

    private const string TEST_BANNER_ID = "ca-app-pub-3940256099942544/6300978111";
    private const string TEST_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";
    private const string TEST_REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
    private const string TEST_REWARDED_INTERSTITIAL_ID = "ca-app-pub-3940256099942544/5354046379";
    private const string TEST_APP_OPEN_ID = "ca-app-pub-3940256099942544/9257395921";

    [HideInInspector] public bool AllowedToShowAppOpenAd = true;
    private bool showedNoInternetOnInterstitial = false;
    private float CurrentTimeScale;
    private float CurrentAudioValue;
    private Action AdToCall;
    #endregion

    #region BannerType Enum
    public enum BannerType
    {
        Simple,
        FullWidth,
        Native
    }

    private enum ADType
    {
        Interstitial, Rewarded, RewardedInterstitial, AdmobInterstitial, AdmobInterstitial2, AdmobInterstitial3, AdmobRewarded
    }

    private ADType AdTypeToCall;
    #endregion

    #region AdMob Banner Instances

    private BannerView admobBannerView1;
    private BannerView admobBannerView2;
    private BannerView admobMediumRectBannerView;
    private BannerView admobAdaptiveBannerView;

    private bool isBanner1Created = false, isBanner1Hidden = false, ShowBanner1AfterAppOpen = false, UsingUnityBannerInsteadOfBanner1 = false;
    private bool isBanner2Created = false, isBanner2Hidden = false, ShowBanner2AfterAppOpen = false;
    private bool isMediumRectCreated = false, isMediumRectHidden = false, ShowBigBannerAfterAppOpen = false, HideMediumRectAtAllCost = false;

    private InterstitialAd admobInterstitial;
    private InterstitialAd admobInterstitial2;
    private InterstitialAd admobInterstitial3;
    private RewardedAd admobRewardedAd;
    private RewardedInterstitialAd admobRewardedInterstitialAd;
    private AppOpenAd admobAppOpenAd;



    #endregion

    #region Max Decleration
    [Space(10)]
    [Header("------- MAX ADs -------")]
    [Space(10)]
    public string maxSDK;

    public MaxSdkBase.BannerPosition bannerPosition;
    public MaxSdkBase.AdViewPosition MediumRectanglePosition;

#if UNITY_ANDROID
    public AdUnitId Android_Max_ID = new AdUnitId();
#elif UNITY_IOS
    public AdUnitId Ios_Max_ID = new AdUnitId();
#endif


    [HideInInspector]
    public AdUnitId maxID = new AdUnitId();

    private static Action NotifyReward;

    private DateTime _expireTime;
    [HideInInspector]
    public bool AdShowing = false;

    private static readonly AndroidJavaClass _maxUnityPluginClass = new AndroidJavaClass("com.applovin.mediation.unity.MaxUnityPlugin");



    #endregion

    #region Unity Ads Fields

    private bool unityAdsInitialized = false;
    private bool isUnityInterstitialLoaded = false;
    private bool isUnityRewardedLoaded = false;

    #endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

#if UNITY_ANDROID
        maxID = Android_Max_ID;
#elif UNITY_IOS
            maxID = Ios_Max_ID;
#endif
    }

    private void Start()
    {
        ShowBanner2AfterAppOpen = true;
#if UNITY_IOS
if(IOS_InternetConnection)
        IOS_InternetConnection.SetActive(true);
 if(Android_InternetConnection)
        Android_InternetConnection.SetActive(false);
       
#elif UNITY_ANDROID
        if (IOS_InternetConnection)
        {
            IOS_InternetConnection.SetActive(false);
        }

        if (Android_InternetConnection)
        {
            Android_InternetConnection.SetActive(true);
        }
#endif

        var debugSettings = new ConsentDebugSettings
        {
            DebugGeography = DebugGeography.EEA,
            TestDeviceHashedIds = new List<string>
            {
                ""
            }
        };
        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
            ConsentDebugSettings = debugSettings,
        };
        ConsentInformation.Update(request, OnConsentInfoUpdated);


        if (TestAd)
        {
            EditorLogger.Log("[AdsManager] Overwriting AdMob IDs with official TEST IDs...");
            admobBannerAdUnitId1 = TEST_BANNER_ID;
            admobBannerAdUnitId2 = TEST_BANNER_ID;
            admobMediumRectAdUnitId = TEST_BANNER_ID;
            admobInterstitialAdUnitId = TEST_INTERSTITIAL_ID;
            admobInterstitialAdUnitId2 = TEST_INTERSTITIAL_ID;
            admobInterstitialAdUnitId3 = TEST_INTERSTITIAL_ID;
            admobRewardedAdUnitId = TEST_REWARDED_ID;
            admobRewardedInterstitialAdUnitId = TEST_REWARDED_INTERSTITIAL_ID;
            admobAppOpenAdUnitId = TEST_APP_OPEN_ID;
        }

        MobileAds.Initialize(status =>
        {
            EditorLogger.Log("AdMob Initialized");
        });
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        //UnityADsHamza Advertisement.Initialize(unityGameId, TestAd, this);

        //  CreateAdMobBanner1();
        //  CreateAdMobBanner2();
        //   CreateMediumRectBanner();
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (UsePreLoadRewarded)
            {
                RequestAdMobRewarded();
            }

            RequestAdMobInterstitial();
            if (admobInterstitialAdUnitId2 != null)
            {
                RequestAdMobInterstitial2();
            }

            if (admobInterstitialAdUnitId3 != null)
            {
                RequestAdMobInterstitial3();
            }

            if (UsePreLoadRewardedInterstitial)
            {
                RequestAdMobRewardedInterstitial();
            }

            RequestAdMobAppOpenAd();
            MaxStart();
        });

    }

    #region AdMob - Banner Show/Hide

    private void CreateAdMobBanner1()
    {
        if (!UseADMobBanner1)
        {
            return;
        }

        if (isBanner1Created)
        {
            return;
        }

        AdSize size = GetAdSizeFromBannerType(banner1Type);
        admobBannerView1 = new BannerView(admobBannerAdUnitId1, size, banner1Position);

        EditorLogger.LogError("Banner1 Creating Pos:" + banner1Position, Color.green);

        admobBannerView1.OnBannerAdLoaded += () =>
        {
            EditorLogger.Log("AdMob Banner1 Loaded & Displayed.");
            UsingUnityBannerInsteadOfBanner1 = false;
            if (!HideMediumRectAtAllCost || !isMediumRectHidden)
            {
                HideAdMobBanner1();
            }
        };
        admobBannerView1.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            EditorLogger.Log("AdMob Banner1 Failed to Load: " + error.GetMessage());
            //UnityADsHamza  ShowUnityBanner();
        };

        if (ShowBannerDataSolar)
        {
            admobBannerView1.OnAdPaid += onAdmobBanner1AdRevenuePaidEventBothFirebaseSolar;
        }

        var request = new AdRequest();
        admobBannerView1.LoadAd(request);

        isBanner1Created = true;
    }

    private void HideAdMobBanner1()
    {
        if (!UseADMobBanner1)
        {
            return;
        }

        if (UsingUnityBannerInsteadOfBanner1)
        {
            //UnityADsHamza HideUnityBanner();
            // isBanner1Hidden  = true;
            // return;
        }

        //if (!isBanner1Created || admobBannerView1 == null) return;

        if (admobBannerView1 != null)
        {
            admobBannerView1.Hide();
            isBanner1Hidden = true;
            EditorLogger.Log("AdMob Banner1 hidden off-screen.");
        }
    }

    private void ShowAdMobBanner1()
    {
        EditorLogger.LogError("Banner1 Pos:" + banner1Position, Color.green);

#if UNITY_IOS || Use_RemoteConfing_Android
        if (!AdMobBannerADSwitch)
            return;
#endif


        if (!UseADMobBanner1)
        {
            return;
        }

        if (!isBanner1Created)
        {
            CreateAdMobBanner1();
            return;
        }

        if (isBanner1Hidden)
        {
            admobBannerView1.Show();
            admobBannerView1.SetPosition(banner1Position);
            isBanner1Hidden = false;
            HideADMobMediumRect();
            EditorLogger.Log("AdMob Banner1 shown at AdPosition.");
        }
    }

    private void CreateAdMobBanner2()
    {
        if (!UseADMobBanner2)
        {
            return;
        }

        if (isBanner2Created)
        {
            return;
        }

        AdSize size = GetAdSizeFromBannerType(banner2Type);
        admobBannerView2 = new BannerView(admobBannerAdUnitId2, size, banner2Position);

        admobBannerView2.OnBannerAdLoaded += () =>
        {
            EditorLogger.Log("AdMob Banner2 Loaded & Displayed.");
        };
        admobBannerView2.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            EditorLogger.Log("AdMob Banner2 Failed to Load: " + error.GetMessage());
        };
        if (ShowBannerDataSolar)
        {
            admobBannerView2.OnAdPaid += onAdmobBanner2AdRevenuePaidEventBothFirebaseSolar;
        }

        var request = new AdRequest();
        admobBannerView2.LoadAd(request);

        isBanner2Created = true;
        isBanner2Hidden = false;
    }


    private void HideAdMobBanner2()
    {
        if (!UseADMobBanner2)
        {
            return;
        }

        if (admobBannerView2 == null)
        {
            return;
        }

        if (admobBannerView2 != null)
        {
            admobBannerView2.Hide();
            isBanner2Hidden = true;
        }

        EditorLogger.Log("AdMob Banner2 hidden off-screen.");
    }

    private void ShowAdMobBanner2()
    {
#if UNITY_IOS|| Use_RemoteConfing_Android
        if (!AdMobBannerADSwitch)
            return;
#endif
        if (!UseADMobBanner2)
        {
            return;
        }
        if (!isBanner2Created)
        {
            CreateAdMobBanner2();
            return;
        }

        if (admobBannerView2 != null)
        {
            admobBannerView2.Show();
            admobBannerView2.SetPosition(banner2Position);
            isBanner2Hidden = false;
        }
    }

    private void ShowAdmobTopBanners()
    {
        ShowAdMobBanner1();
        ShowAdMobBanner2();
        HideADMobMediumRect();

    }

    public void ShowTopBannerWithMediation()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }

        ShowingMediumRectNow = false;
#if UNITY_IOS || Use_RemoteConfing_Android
        if (!adSwitchFirebase)
        {
            return;
        }

#endif
#if UNITY_IOS || Use_RemoteConfing_Android
        if (MaxBannerADLoaded && MAXBannerADSwitch)
#elif UNITY_ANDROID
        
        if (MaxBannerADLoaded)
#endif
        {
            ShowMaxBanner();
            HideAdMobBanner1();
            HideAdMobBanner2();
        }
#if UNITY_IOS|| Use_RemoteConfing_Android
        else if (AdMobBannerADSwitch)
#elif UNITY_ANDROID

        else
#endif
        {
            ShowAdMobBanner1();
            ShowAdMobBanner2();
            HideMaxBanner();
        }
        HideMediumRect();
    }

    public void HideAllBanners()
    {
        HideAdMobBanner1();
        HideAdMobBanner2();
        //UnityADsHamza HideUnityBanner();
        HideMaxBanner();
        HideMaxMRECBanner();
        HideADMobMediumRect();
    }

    #endregion

    #region Medium Rect
    private bool isMediumRectCreating = false;
    private void ShowAdMobMediumRect()
    {
        if (HideMediumRectAtAllCost)
        {
            return;
        }

        if (!isMediumRectCreated && !isMediumRectCreating)
        {
            isMediumRectCreating = true;
            CreateMediumRectBanner();
        }
        else if (isMediumRectHidden && admobMediumRectBannerView != null)
        {
            admobMediumRectBannerView.Show();
            isMediumRectHidden = false;
            HideAdMobBanner1();
            //UnityADsHamza HideUnityBanner();
            EditorLogger.Log("AdMob MediumRect shown at AdPosition.");
        }
    }

    private void HideADMobMediumRect()
    {
        HideMediumRectAtAllCost = true;
        if (!isMediumRectCreated || admobMediumRectBannerView == null)
        {
            return;
        }

        admobMediumRectBannerView.Hide();
        isMediumRectHidden = true;

        EditorLogger.Log("AdMob MediumRect hidden off-screen.");
    }

    private void CreateMediumRectBanner()
    {
        admobMediumRectBannerView = new BannerView(admobMediumRectAdUnitId, AdSize.MediumRectangle, mediumRectPosition);

        admobMediumRectBannerView.OnBannerAdLoaded += () =>
        {
            EditorLogger.Log("AdMob MediumRect Loaded & Displayed.");
            isMediumRectCreated = true;
            isMediumRectCreating = false;
            if (HideMediumRectAtAllCost && !isBanner1Hidden)
            {
                HideADMobMediumRect();
            }
        };
        admobMediumRectBannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            EditorLogger.Log("AdMob MediumRect Failed to Load: " + error.GetMessage());
            isMediumRectCreating = false;
        };

        if (ShowBannerDataSolar)
        {
            admobMediumRectBannerView.OnAdPaid += onAdmobBigBannerAdRevenuePaidEventBothFirebaseSolar;
        }

        var request = new AdRequest();
        admobMediumRectBannerView.LoadAd(request);
    }

    public void ShowMediumRect()
    {

        EditorLogger.LogError("SHowinn Medium Rect", Color.green);
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }

        ShowingMediumRectNow = true;
#if UNITY_IOS || Use_RemoteConfing_Android
        if (!adSwitchFirebase)
        {
            return;
        }
#endif
        EditorLogger.LogError("SHowinn Medium Rect :2", Color.green);



        if (UseADMobBanner2 && UseADMobBanner1)
        {
            HideAdMobBanner1();
        }

#if UNITY_IOS || Use_RemoteConfing_Android
        if (MaxBannerADLoaded && MAXBannerADSwitch)

#else
        if (MaxBannerADLoaded)
#endif
        {
            ShowMaxBanner();
            HideAdMobBanner1();
            HideAdMobBanner2();
        }

#if UNITY_IOS|| Use_RemoteConfing_Android
        else if (AdMobBannerADSwitch)

#elif UNITY_ANDROID
        else
#endif
        {
            ShowAdMobBanner2();
        }
        HideMediumRectAtAllCost = false;

        EditorLogger.LogError("SHowinn Medium Rect :3", Color.green);


        EditorLogger.LogError("AdMobMRECADSwitch Value :" + AdMobMRECADSwitch, Color.green);
        EditorLogger.LogError("admobMediumRectBannerView Value :" + admobMediumRectBannerView, Color.green);

#if UNITY_IOS || Use_RemoteConfing_Android
        if (AdMobMRECADSwitch /*&& admobMediumRectBannerView!= null*/)
#elif UNITY_ANDROID
        if (admobMediumRectBannerView!= null)
#endif
        {

            /*#if UNITY_EDITOR
                        ShowAdMobMediumRect();
                        return;
            #endif*/

            ShowAdMobMediumRect();
            EditorLogger.LogError("ShowAdMobMediumRect ", Color.green);
        }
#if UNITY_IOS || Use_RemoteConfing_Android
        else if (MAXMRECADSwitch)
#elif UNITY_ANDROID
        else
#endif
            EditorLogger.LogError("Fallback MRect", Color.green);

        ShowMaxMRECBanner();

    }

    public void HideMediumRect()
    {
        HideADMobMediumRect();
        HideMaxMRECBanner();
    }


    #endregion

    #region AdMob - Interstitial / Rewarded / Rewarded Interstitial

    private void RequestAdMobInterstitial()
    {
        var adRequest = new AdRequest();
        InterstitialAd.Load(admobInterstitialAdUnitId, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null && error.GetCode() != 0)
                {
                    EditorLogger.Log("AdMob Interstitial Failed to Load: " + error.GetMessage());
                    return;
                }
                EditorLogger.Log("AdMob Interstitial Loaded.");
                admobInterstitial = ad;

                admobInterstitial.OnAdFullScreenContentClosed += () =>
                {
                    admobInterstitial.Destroy();
                    admobInterstitial = null;
                    EditorLogger.Log("AdMob Interstitial Closed -> Reloading...");
                    RequestAdMobInterstitial();
                };
            });
    }
    private void RequestAdMobInterstitial2()
    {
        var adRequest = new AdRequest();
        InterstitialAd.Load(admobInterstitialAdUnitId2, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null && error.GetCode() != 0)
                {
                    EditorLogger.Log("AdMob Interstitial Failed to Load: " + error.GetMessage());
                    return;
                }
                EditorLogger.Log("AdMob Interstitial Loaded.");
                admobInterstitial2 = ad;

                admobInterstitial2.OnAdFullScreenContentClosed += () =>
                {
                    admobInterstitial2.Destroy();
                    admobInterstitial2 = null;
                    EditorLogger.Log("AdMob Interstitial Closed -> Reloading...");
                    RequestAdMobInterstitial2();
                };
            });
    }
    private void RequestAdMobInterstitial3()
    {
        var adRequest = new AdRequest();
        InterstitialAd.Load(admobInterstitialAdUnitId3, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null && error.GetCode() != 0)
                {
                    EditorLogger.Log("AdMob Interstitial Failed to Load: " + error.GetMessage());
                    return;
                }
                EditorLogger.Log("AdMob Interstitial Loaded.");
                admobInterstitial3 = ad;

                admobInterstitial3.OnAdFullScreenContentClosed += () =>
                {
                    admobInterstitial3.Destroy();
                    admobInterstitial3 = null;
                    EditorLogger.Log("AdMob Interstitial Closed -> Reloading...");
                    RequestAdMobInterstitial3();
                };
            });
    }

    private void RequestAdMobRewarded()
    {
        var adRequest = new AdRequest();
        RewardedAd.Load(admobRewardedAdUnitId, adRequest,
            (RewardedAd ad, LoadAdError loadError) =>
            {
                if (loadError != null && loadError.GetCode() != 0)
                {
                    EditorLogger.Log("AdMob Rewarded Failed to Load: " + loadError.GetMessage());
                    return;
                }
                EditorLogger.Log("AdMob Rewarded Loaded.");

                admobRewardedAd = ad;
                if (!UsePreLoadRewarded && AdYetToDisplayed)
                {
                    DOVirtual.DelayedCall(1, PerformAdCallAfterDelay, ignoreTimeScale: true);
                }

                admobRewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    admobRewardedAd.Destroy();
                    admobRewardedAd = null;
                    EditorLogger.Log("AdMob Rewarded Closed -> Reloading...");
                    if (UsePreLoadRewarded)
                    {
                        RequestAdMobRewarded();
                    }
                };
            });
    }

    private void RequestAdMobRewardedInterstitial()
    {
        var adRequest = new AdRequest();
        RewardedInterstitialAd.Load(admobRewardedInterstitialAdUnitId, adRequest,
            (RewardedInterstitialAd ad, LoadAdError loadError) =>
            {
                if (loadError != null && loadError.GetCode() != 0)
                {
                    EditorLogger.Log("AdMob Rewarded Interstitial Failed to Load: " + loadError.GetMessage());
                    return;
                }
                EditorLogger.Log("AdMob Rewarded Interstitial Loaded.");
                admobRewardedInterstitialAd = ad;
                if (!UsePreLoadRewarded && AdYetToDisplayed)
                {
                    DOVirtual.DelayedCall(1, PerformAdCallAfterDelay, ignoreTimeScale: true);
                }
                admobRewardedInterstitialAd.OnAdFullScreenContentClosed += () =>
                {
                    admobRewardedInterstitialAd.Destroy();
                    admobRewardedInterstitialAd = null;
                    EditorLogger.Log("AdMob Rewarded Interstitial Closed -> Reloading...");
                    if (UsePreLoadRewardedInterstitial)
                    {
                        RequestAdMobRewardedInterstitial();
                    }
                };
            });
    }

    private void ShowInterstitialWithMediation()
    {
#if UNITY_IOS || Use_RemoteConfing_Android
        if (MaxSdk.IsInterstitialReady(maxID.InterstitialadUnitId) && MAXInterstitialADSwitch)

#elif UNITY_ANDROID
        if (MaxSdk.IsInterstitialReady(maxID.InterstitialadUnitId))
#endif
        {
            AdShowing = true;
            ShowMaxInterstitial();
        }

#if UNITY_IOS || Use_RemoteConfing_Android
        else if (AdMobInterstitialADSwitch)
        {
            if (MAXInterstitialADSwitch)
            {
                LoadMaxInterstitial();
            }

            ShowAdMobInterstitial();
        }
        else
        {
            if (MAXInterstitialADSwitch)
            {
                LoadMaxInterstitial();
            }
        }

#elif UNITY_ANDROID
        else
        {
            LoadMaxInterstitial();
            ShowAdMobInterstitial();
        }

#endif


        CanShowAppOpenAfterDelay(false);
    }

    private void ShowAdMobInterstitial()
    {
        EditorLogger.LogError("Showing_Interstitial", Color.cyan);
        if (admobInterstitial != null)
        {
            admobInterstitial.Show();
        }
    }
    private void ShowAdMobInterstitial2()
    {
        EditorLogger.LogError("Showing_Interstitial2", Color.cyan);

        if (admobInterstitial2 != null)
        {
            admobInterstitial2.Show();
        }
    }
    private void ShowAdMobInterstitial3()
    {
        EditorLogger.LogError("Showing_Interstitial3", Color.cyan);

        if (admobInterstitial3 != null)
        {
            admobInterstitial3.Show();
        }
    }

    private void ShowRewardedWithMediation(Action onReward)
    {
        if (MaxSdk.IsRewardedAdReady(maxID.RewardadUnitId))
        {
            AdShowing = true;
            ShowMaxRewardedAd(onReward);
        }
        else if (admobRewardedAd != null)
        {
            ShowADMobRewarded(onReward);
        }
        else if (CanShowInterstitialAfterRewardedFallBack)
        {
            NotifyReward?.Invoke();
            NotifyReward = null;
            CanShowInterstitialAfterRewardedFallBack = false;
            ShowInterstitialWithMediation();

        }
        CanShowAppOpenAfterDelay(false);
    }

    private void ShowADMobRewarded(Action onRewarded)
    {
        admobRewardedAd.Show(reward =>
        {
            onRewarded?.Invoke();
        });
        AdToCall = null;
    }

    private void ShowAdMobRewardedInterstitial(Action onReward)
    {
        if (admobRewardedInterstitialAd != null)
        {
            admobRewardedInterstitialAd.Show(reward =>
            {
                onReward?.Invoke();
            });
        }
        else
        {
            //UnityADsHamza  ShowUnityInterstitial();
            onReward?.Invoke();
        }
        CanShowAppOpenAfterDelay(false);
        AdToCall = null;
    }

    #endregion

    #region AdMob - App Open


    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {

        }
        else
        {
            ShowAppOpenWithMediation();
        }
    }

    private void RequestAdMobAppOpenAd()
    {
        EditorLogger.Log("Requesting AdMob App Open Ad...");
        var adRequest = new AdRequest();

        AppOpenAd.Load(admobAppOpenAdUnitId,
            adRequest,
            (appOpen, error) =>
            {
                if (error != null && error.GetCode() != 0)
                {
                    EditorLogger.Log($"AdMob App Open Ad Failed to Load: {error.GetMessage()} (Code: {error.GetCode()})");
                    return;
                }

                if (appOpen == null)
                {
                    EditorLogger.Log("App Open Ad loaded but is null. Check AdMob settings.");
                    return;
                }

                CanShowAppOpenAfterDelay(false);
                EditorLogger.Log("AdMob App Open Ad Loaded Successfully.");
                admobAppOpenAd = appOpen;

                // Add event handlers for the loaded ad
                admobAppOpenAd.OnAdFullScreenContentClosed += () =>
                {
                    EditorLogger.Log("App Open Ad closed -> reloading...");
                    admobAppOpenAd = null;
                    RequestAdMobAppOpenAd();
                    ShowBannersAfterAppOpenAd();
                    CanShowAppOpenAfterDelay(false);
                };

                admobAppOpenAd.OnAdFullScreenContentFailed += (adError) =>
                {
                    EditorLogger.Log($"AdMob App Open Ad Failed to Show: {adError.GetMessage()} (Code: {adError.GetCode()})");
                };

                admobAppOpenAd.OnAdImpressionRecorded += () =>
                {
                    EditorLogger.Log("AdMob App Open Ad Impression recorded.", Color.green);
                    OnAppOpenCall();
                };

                admobAppOpenAd.OnAdClicked += () =>
                {
                    EditorLogger.Log("AdMob App Open Ad Clicked.");
                };
            });
    }

    private void OnAppOpenCall()
    {
        HideBannersOnAppOpenAd();
        CanShowAppOpenAfterDelay(false);
    }
    public void ShowAppOpenWithMediation()
    {
        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }

#if UNITY_IOS || Use_RemoteConfing_Android
        if (!adSwitchFirebase)
        {
            return;
        }
#endif



        if (!AllowedToShowAppOpenAd || !HasInternet() || AppOpenCalls != 0)
        {
            return;
        }

#if UNITY_IOS || Use_RemoteConfing_Android
        if (MaxSdk.IsAppOpenAdReady(maxID.AppOpenAdUnitId) && MAXAppOpenADSwitch && !UseOnlyADMobAppOpen)
#elif UNITY_ANDROID
        if(MaxSdk.IsAppOpenAdReady(maxID.AppOpenAdUnitId) && !UseOnlyADMobAppOpen)
#endif
        {
            ShowMaxAppOpen();

        }
#if UNITY_IOS || Use_RemoteConfing_Android
        else if (ADMobAppOpenADSwitch)

#elif UNITY_ANDROID
        else
#endif
        {
            UseOnlyADMobAppOpen = false;
            ShowAdMobAppOpen();
        }
    }

    public void ShowAdMobAppOpen()
    {
        if (!AllowedToShowAppOpenAd || AppOpenCalls != 0)
        {
            return;
        }
        if (admobAppOpenAd != null)
        {
            HideBannersOnAppOpenAd();
            admobAppOpenAd.Show();
            CanShowAppOpenAfterDelay(false);

        }
        else
        {
            RequestAdMobAppOpenAd();
            EditorLogger.LogWarning("AppOpen Ad not ready yet.");
        }
    }

    private int AppOpenCalls;

    private void CanShowAppOpenAfterDelay(bool checkFromFirebase)
    {
        AppOpenCalls++;
        if (checkFromFirebase)
        {
            if (showAppOpenOnMaxInterFallBack)
            {
                AllowedToShowAppOpenAd = true;
            }
            else
            {
                AllowedToShowAppOpenAd = false;
            }
        }
        else
        {
            AllowedToShowAppOpenAd = false;
        }
        DOVirtual.DelayedCall(5, AllowedToShowAppOpenAdNow, ignoreTimeScale: true);
    }

    public void StopShowingAppOpenAtInApp()
    {
        CanShowAppOpenAfterDelay(false);
    }

    private void CanShowAppOpenManually(bool Status)
    {
        AllowedToShowAppOpenAd = Status;
    }

    private void AllowedToShowAppOpenAdNow()
    {
        AppOpenCalls--;
        AllowedToShowAppOpenAd = true;
    }

    #endregion


    #region Banner Ad Helper

    private AdSize GetAdSizeFromBannerType(BannerType type)
    {
        switch (type)
        {
            case BannerType.FullWidth:
                return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            case BannerType.Native:
                // For demonstration, also treat "Native" as SmartBanner
                return AdSize.SmartBanner;
            case BannerType.Simple:
            default:
                // 320x50
                return AdSize.Banner;
        }
    }

    private void HideBannersOnAppOpenAd()
    {
        ShowBanner1AfterAppOpen = false;
        ShowBanner2AfterAppOpen = false;
        ShowBigBannerAfterAppOpen = false;


        if (isBanner2Created && !isBanner2Hidden)
        {
            ShowBanner2AfterAppOpen = true;
            HideAdMobBanner2();
        }

        if (isBanner1Created && !isBanner1Hidden)
        {
            ShowBanner1AfterAppOpen = true;
            HideAdMobBanner1();
        }
        else if (isMediumRectCreated && !isMediumRectHidden)
        {
            ShowBigBannerAfterAppOpen = true;
        }
        if (admobMediumRectBannerView != null)
        {
            HideADMobMediumRect();
        }

        if (MaxBannerADLoaded)
        {
            MaxSdk.HideBanner(maxID.bannerAdUnitId);
        }
    }

    private void ShowBannersAfterAppOpenAd()
    {
        if (ShowBanner1AfterAppOpen || ShowBanner2AfterAppOpen)
        {
            ShowTopBannerWithMediation();
        }
        else if (ShowBigBannerAfterAppOpen)
        {
            ShowMediumRect();
        }
    }

    #endregion

    #region Unity Ads (4.x)

    private void OnInitializationComplete()
    {


        EditorLogger.Log("Unity Ads Initialization Complete.");
        unityAdsInitialized = true;

        // Pre-load Unity Banner, Interstitial, Rewarded
        /*LoadUnityBanner();
        LoadUnityInterstitial();
        LoadUnityRewarded();*/
    }

    /*
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        EditorLogger.Log($"Unity Ads Initialization Failed: {error} - {message}");
    }
    */

    /*
    private void LoadUnityBanner()
    {
        if (!unityAdsInitialized) return;

        Advertisement.Banner.SetPosition(UnityBannerPositon);

        BannerLoadOptions loadOptions = new BannerLoadOptions
        {
            loadCallback = () =>
            {
                EditorLogger.Log("Unity Banner Loaded (won't auto-show).");
            },
            errorCallback = (msg) =>
            {
                EditorLogger.Log("Unity Banner Load Error: " + msg);
            }
        };

        Advertisement.Banner.Load(unityBannerPlacementId, loadOptions);
    }
    */

    /*
    private void LoadUnityInterstitial()
    {
        if (!unityAdsInitialized) return;
        Advertisement.Load(unityInterstitialPlacementId, this);
    }
    */

    /*private void LoadUnityRewarded()
    {
        if (!unityAdsInitialized) return;
        Advertisement.Load(unityRewardedPlacementId, this);
    }*/

    private void OnUnityAdsAdLoaded(string placementId)
    {
        /*EditorLogger.Log("Unity Ads Loaded: " + placementId);

        if (placementId.Equals(unityInterstitialPlacementId))
        {
            isUnityInterstitialLoaded = true;
        }
        else if (placementId.Equals(unityRewardedPlacementId))
        {
            isUnityRewardedLoaded = true;
        }
        else if (placementId.Equals(unityBannerPlacementId))
        {
            EditorLogger.Log("Unity Banner is loaded, show it manually with ShowUnityBanner().");
        }*/
    }

    /*public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        EditorLogger.Log($"Unity Ads Failed to Load: {placementId}, {error}, {message}");

        if (placementId.Equals(unityInterstitialPlacementId))
        {
            isUnityInterstitialLoaded = false;
        }
        else if (placementId.Equals(unityRewardedPlacementId))
        {
            isUnityRewardedLoaded = false;
        }
    }*/

    /*
    public void ShowUnityBanner()
    {
        if (!unityAdsInitialized)
        {
            EditorLogger.LogWarning("Unity Ads not initialized for banner.");
            return;
        }

        if (!HideMediumRectAtAllCost || !isMediumRectHidden)
        {
            return;
        }

        BannerOptions showOptions = new BannerOptions
        {
            clickCallback = () =>
            {
                EditorLogger.Log("Unity Banner Clicked");
            },
            hideCallback = () =>
            {
                EditorLogger.Log("Unity Banner Hidden");
            }
        };

        UsingUnityBannerInsteadOfBanner1 = true;
        Advertisement.Banner.Show(unityBannerPlacementId, showOptions);
    }
    */

    /*
    public void HideUnityBanner()
    {
        Advertisement.Banner.Hide(false);
    }
    */

    /*public void ShowUnityInterstitial()
    {
        if (!isUnityInterstitialLoaded)
        {
            EditorLogger.LogWarning("Unity Interstitial not loaded yet.");
            return;
        }

        Advertisement.Show(unityInterstitialPlacementId, this);
        CanShowAppOpenAfterDelay();
    }

    public Action RewardToGive;
    public void ShowUnityRewarded(Action RewardToGiveAction)
    {
        if (!isUnityRewardedLoaded)
        {
            EditorLogger.LogWarning("Unity Rewarded not loaded yet.");
            return;
        }

        RewardToGive = RewardToGiveAction;
        Advertisement.Show(unityRewardedPlacementId, this);
        CanShowAppOpenAfterDelay();
    }*/

    /*public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        EditorLogger.Log($"Unity Ads Show Failure: {placementId}, {error}, {message}");
    }*/

    private void OnUnityAdsShowStart(string placementId)
    {
        EditorLogger.Log("Unity Ads Show Start: " + placementId);
    }

    private void OnUnityAdsShowClick(string placementId)
    {
        EditorLogger.Log("Unity Ads Show Click: " + placementId);
    }

    /*public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        EditorLogger.Log($"Unity Ads Show Complete: {placementId}, {showCompletionState}");

        if (placementId.Equals(unityRewardedPlacementId)
            && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            EditorLogger.Log("Unity Rewarded completed. Grant reward here.");
            RewardToGive.Invoke();
            RewardToGive = null;
            
        }

        // Reload after showing
        if (placementId.Equals(unityInterstitialPlacementId))
        {
            isUnityInterstitialLoaded = false;
            LoadUnityInterstitial();
        }
        else if (placementId.Equals(unityRewardedPlacementId))
        {
            isUnityRewardedLoaded = false;
            LoadUnityRewarded();
        }
    }*/

    #endregion

    #region Optional Reward Examples

    public void ShowRewardedExample()
    {
        ShowRewardedWithMediation(() =>
        {
            EditorLogger.Log("User earned reward (AdMob Rewarded).");
        });
    }

    public void ShowRewardedInterstitialExample()
    {
        ShowAdMobRewardedInterstitial(() =>
        {
            EditorLogger.Log("User earned reward (AdMob Rewarded Interstitial).");
        });
    }

    #endregion

    #region InternetAvailibility

    public bool HasInternet()
    {

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private void ShowNoInternetPanel()
    {
        NoInternetPanel.SetActive(true);
    }
    public void HideNoInternetPanel()
    {
        NoInternetPanel.SetActive(false);
        bool keepPaused = PauseMenu.instance != null && PauseMenu.instance.IsPaused;
        Time.timeScale = keepPaused ? 0f : 1f;
        AudioListener.volume = CurrentAudioValue;
    }

    private void ShowLoadingPanel()
    {
        LoadingAdPanel.SetActive(true);
        AdStatusText.text = "Loading AD";

    }

    private void HideLoadingPanel()
    {
        AdStatusText.text = "Back To Game";
        DOVirtual.DelayedCall(0.3f, OnHideLoading);
    }

    private void OnHideLoading()
    {
        bool keepPaused = PauseMenu.instance != null && PauseMenu.instance.IsPaused;
        Time.timeScale = keepPaused ? 0f : CurrentTimeScale;
        AudioListener.volume = CurrentAudioValue;
        if (LoadingAdPanel != null)
            LoadingAdPanel.SetActive(false);
    }
    public void OpenWiFiSettings()
    {


#if UNITY_ANDROID
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.settings.WIFI_SETTINGS");

                currentActivity.Call("startActivity", intent);
            }
            EditorLogger.Log("Attempted to open Wi-Fi settings on Android.");
        }
        catch (Exception e)
        {
            EditorLogger.Log("Failed to open Wi-Fi settings on Android: " + e.Message);
        }
#elif UNITY_IOS
        try
        {
            // Open the App Settings page on iOS
            Application.OpenURL("app-settings:");
            EditorLogger.Log("Attempted to open App Settings on iOS.");
        }
        catch (Exception e)
        {
            EditorLogger.Log("Failed to open App Settings on iOS: " + e.Message);
        }
#else
        EditorLogger.LogWarning("OpenWiFiSettings: Platform not supported for opening settings.");
#endif
        HideNoInternetPanel();
    }

    #endregion

    #region LoadingScreen
    public void ShowInterstitialWithLoading()
    {
        if (Adtimers == true)
        {
            if (Time.time - lastAdCallTime < adCooldown)
            {
                return;
            }
        }
        if (PlayerPrefs.GetInt("RemoveAdsTimer", 0) == 1)
        {
            return;
        }

        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }
        InterNet();

#if UNITY_IOS || Use_RemoteConfing_Android
        if (!adSwitchFirebase)
        {
            return;
        }

        if (!MAXInterstitialADSwitch && !AdMobInterstitialADSwitch)
        {
            return;
        }
#endif
        string curSceneName = SceneManager.GetActiveScene().name;
        //if (curSceneName == "PlayScene")
        //{
        //    ScenesController.Instance.ResetOfferTime();
        //}
        lastAdCallTime = Time.time;
        AdTypeToCall = ADType.Interstitial;
        LoadingScreenBeforeAd(0.5f);
    }

    public void InterNet()
    {
        if (!MyFirebase.Instance.IsInternet)
        {
            MyFirebase.Instance.InitializeFirebase();

        }
    }
    public void ShowAdmobInterstitialWithLoading()
    {
        if (Adtimers == true)
        {
            if (Time.time - lastAdCallTime < adCooldown)
            {
                return;
            }
        }
        if (PlayerPrefs.GetInt("RemoveAdsTimer", 0) == 1)
        {
            return;
        }

        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }

#if UNITY_IOS || Use_RemoteConfing_Android
        if (!adSwitchFirebase)
        {
            return;
        }

        if (!AdMobInterstitialADSwitch)
        {
            return;
        }
#endif
        lastAdCallTime = Time.time;
        LoadingScreenBeforeAd(0.5f);
        AdTypeToCall = ADType.AdmobInterstitial;
    }
    public void ShowAdmobInterstitial2WithLoading()
    {
        if (Adtimers == true)
        {
            if (Time.time - lastAdCallTime < adCooldown)
            {
                return;
            }
        }

        if (PlayerPrefs.GetInt("RemoveAdsTimer", 0) == 1)
        {
            return;
        }

        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }

#if UNITY_IOS || Use_RemoteConfing_Android
        if (!adSwitchFirebase)
        {
            return;
        }

        if (!AdMobInterstitialADSwitch)
        {
            return;
        }
#endif
        lastAdCallTime = Time.time;
        LoadingScreenBeforeAd(0.5f);
        AdTypeToCall = ADType.AdmobInterstitial2;
    }
    public void ShowAdmobInterstitial3WithLoading()
    {
        if (Adtimers == true)
        {
            if (Time.time - lastAdCallTime < adCooldown)
            {
                return;
            }
        }
        if (PlayerPrefs.GetInt("RemoveAdsTimer", 0) == 1)
        {
            return;
        }

        if (PlayerPrefs.GetInt("RemoveAds") == 1)
        {
            return;
        }

#if UNITY_IOS || Use_RemoteConfing_Android
        if (!adSwitchFirebase)
        {
            return;
        }

        if (!AdMobInterstitialADSwitch)
        {
            return;
        }
#endif
        lastAdCallTime = Time.time;
        LoadingScreenBeforeAd(0.5f);
        AdTypeToCall = ADType.AdmobInterstitial3;
    }
    private void ShowRewardedADMobWithLoading(Action onReward)
    {
        AdToCall = onReward;
        AdTypeToCall = ADType.Rewarded;

        if (!UsePreLoadRewarded)
        {
            RequestAdMobRewarded();
            LoadingScreenBeforeAd(1);
        }
        else
        {
            LoadingScreenBeforeAd(1);
        }

    }
    public void ShowRewardedWithLoading(Action onReward, bool CanShowInterstitialOnFallBack)
    {

        CanShowInterstitialAfterRewardedFallBack = CanShowInterstitialOnFallBack;
        AdToCall = onReward;
        AdTypeToCall = ADType.Rewarded;

        if (MaxSdk.IsRewardedAdReady(maxID.RewardadUnitId))
        {
            LoadingScreenBeforeAd(1);
        }

        else
        {
            LoadMaxRewardedAd();
            if (!UsePreLoadRewarded)
            {
                RequestAdMobRewarded();
                LoadingScreenBeforeAd(1);
            }
            else
            {
                LoadingScreenBeforeAd(1);
            }
        }

    }

    public void ShowAdRewardedInterstitialWithLoading(Action onReward)
    {
        AdToCall = onReward;
        AdTypeToCall = ADType.RewardedInterstitial;
        if (!UsePreLoadRewardedInterstitial)
        {
            RequestAdMobRewardedInterstitial();
            LoadingScreenBeforeAd(1);
        }
        else
        {
            LoadingScreenBeforeAd(1);
        }

    }

    public void ShowAdMobRewardedInterstitialWithLoadingExample()
    {
        ShowAdRewardedInterstitialWithLoading(() =>
        {
            EditorLogger.Log("User watched rewarded");
        });
    }

    public void ShowAdMobRewardedWithLoadingExample()
    {
        ShowRewardedWithLoading(() => { EditorLogger.Log("User watched rewarded"); }, false);
    }

    private bool AdYetToDisplayed;
    private void LoadingScreenBeforeAd(float delay = 5)
    {
        CurrentTimeScale = Time.timeScale;
        CurrentAudioValue = AudioListener.volume;
        Time.timeScale = 0;
        switch (AdTypeToCall)
        {
            case ADType.Interstitial:

                if (!HasInternet())
                {

                    if (!showedNoInternetOnInterstitial)
                    {
                        ShowNoInternetPanel();
                        showedNoInternetOnInterstitial = true;
                    }
                    else
                    {
                        HideNoInternetPanel();
                    }
                    return;
                }
                break;


            case ADType.Rewarded:
                if (!HasInternet())
                {
                    ShowNoInternetPanel();
                    return;
                }
                break;

            case ADType.RewardedInterstitial:
                if (!HasInternet())
                {
                    ShowNoInternetPanel();
                    return;
                }
                break;
            default:
                if (!HasInternet())
                {
                    if (!showedNoInternetOnInterstitial)
                    {
                        ShowNoInternetPanel();
                        showedNoInternetOnInterstitial = true;
                    }
                    else
                    {
                        HideNoInternetPanel();
                    }
                    return;
                }

                break;
        }

        AllowedToShowMediumRectAfterADPanel = ShowingMediumRectNow;
        ShowMediumRect();
        ShowLoadingPanel();
        AdYetToDisplayed = true;
        DOVirtual.DelayedCall(delay, PerformAdCallAfterDelay, ignoreTimeScale: true);

    }

    private void PerformAdCallAfterDelay()
    {

        if (!AdYetToDisplayed)
        {
            return;
        }
        AdYetToDisplayed = false;
        HideLoadingPanel();

        if (!AllowedToShowMediumRectAfterADPanel)
        {
            ShowTopBannerWithMediation();
        }

        switch (AdTypeToCall)
        {
            case ADType.Interstitial:
                ShowInterstitialWithMediation();
                CanShowAppOpenAfterDelay(true);
                break;

            case ADType.Rewarded:
                ShowRewardedWithMediation(AdToCall);
                CanShowAppOpenAfterDelay(false);
                break;

            case ADType.RewardedInterstitial:
                ShowAdMobRewardedInterstitial(AdToCall);
                CanShowAppOpenAfterDelay(false);
                break;
            case ADType.AdmobInterstitial:
                ShowAdMobInterstitial();
                CanShowAppOpenAfterDelay(false);
                break;
            case ADType.AdmobInterstitial2:
                ShowAdMobInterstitial2();
                CanShowAppOpenAfterDelay(false);
                break;
            case ADType.AdmobInterstitial3:
                ShowAdMobInterstitial3();
                CanShowAppOpenAfterDelay(false);
                break;
        }

    }

    #endregion

    #region GDPR_Consent

    private void OnConsentInfoUpdated(FormError error)
    {
        if (error != null)
        {
            Debug.Log(error);
            Debug.Log(error + " " + error.Message + " " + error.ErrorCode);
            return;
        }

        if (ConsentInformation.IsConsentFormAvailable())
        {
            LoadForm();
        }
    }

    private void LoadForm()
    {
        ConsentForm.Load(OnLoadConsentForm);
    }

    private void OnLoadConsentForm(ConsentForm consentForm, FormError error)
    {
        if (error != null)
        {
            Debug.Log(error);
            return;
        }
        _consentForm = consentForm;

        if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
        {
            _consentForm.Show(OnShowForm);
        }
    }

    private void OnShowForm(FormError error)
    {
        if (error != null)
        {
            return;
        }
        LoadForm();
    }

    #endregion

    private void onAdmobBanner1AdRevenuePaidEventBothFirebaseSolar(AdValue adValue)
    {
        //double revenue = adValue.Value;
        //string currency = adValue.CurrencyCode;
        string adUnitId = admobBannerAdUnitId1;
        //string networkName = "AdMob";
        //string adFormat = "Banner1";

        // admobFirebaseLogEvent(adUnitId, networkName, adFormat, revenue, currency);
        admobSolarSDKEvent(adUnitId, adValue);
    }
    private void onAdmobBanner2AdRevenuePaidEventBothFirebaseSolar(AdValue adValue)
    {
        //double revenue = adValue.Value;
        //string currency = adValue.CurrencyCode;
        string adUnitId = admobBannerAdUnitId2;
        //string networkName = "AdMob";
        //string adFormat = "Banner2";

        //admobFirebaseLogEvent(adUnitId, networkName, adFormat, revenue, currency);
        admobSolarSDKEvent(adUnitId, adValue);
    }
    private void onAdmobBigBannerAdRevenuePaidEventBothFirebaseSolar(AdValue adValue)
    {
        //double revenue = adValue.Value;
        //string currency = adValue.CurrencyCode;
        string adUnitId = admobMediumRectAdUnitId;
        //string networkName = "AdMob";
        //string adFormat = "Banner2";

        //admobFirebaseLogEvent(adUnitId, networkName, adFormat, revenue, currency);
        admobSolarSDKEvent(adUnitId, adValue);
    }

    private void admobSolarSDKEvent(string adUnitid, AdValue adInfo)
    {
        BannerImpCount++;
        EditorLogger.Log("Banner ad revenue paid");
        EditorLogger.Log("Banner ad record an impression" + BannerImpCount);
#if UNITY_IOS
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();
        
#endif
#if UNITY_ANDROID
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();

#endif

        impressionAttributes.ad_platform = "AdMob";
        impressionAttributes.ad_id = adUnitid;
        impressionAttributes.ad_type = 5;
        impressionAttributes.ad_ecpm = adInfo.Value / 1000.00;
        impressionAttributes.currency_type = "USD";
        impressionAttributes.mediation_platform = "Google";
        impressionAttributes.is_rendered = true;
        SolarEngine.Analytics.trackIAI(impressionAttributes);
    }


    #region Max_AD_Handler




    private void MaxStart()
    {
        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
        {
            // AppLovin SDK is initialized, start loading ads
            if (PlayerPrefs.GetInt("RemoveAds") != 1)
            {
                EditorLogger.LogError("2");
                // LoadMaxAppOpen();
                //MaxSdk.LoadAppOpenAd(maxID.AppOpenAdUnitId);

                // InitializeMaxBannerAds();
                InitializeMaxMRecAds();
                InitializeMaxInterstitialAds();
            }
            InitializeMaxRewardedAds();
        };



        // MaxSdk.SetSdkKey(maxSDK);
        //MaxSdk.SetUserId("USER_ID");
        MaxSdk.InitializeSdk();

        // Check if GDPR consent is already set
        if (!MaxSdk.HasUserConsent())
        {
            // If consent is not set, show the built-in consent dialog
            _maxUnityPluginClass.CallStatic("showConsentDialog");
        }
    }



    #region Banner Code Block
    private void InitializeMaxBannerAds()
    {
        // Banners are automatically sized to 320�50 on phones and 728�90 on tablets
        // You may call the utility method MaxSdkUtils.isTablet() to help with view sizing adjustments
        MaxSdk.CreateBanner(maxID.bannerAdUnitId, bannerPosition);
        MaxSdk.SetBannerPlacement(maxID.bannerAdUnitId, "�placement�");


        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnMaxBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnMaxBannerAdLoadFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnMaxBannerAdClickedEvent;
        //MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnMaxBannerAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnMaxBannerAdExpandedEvent;
        MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnMaxBannerAdCollapsedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        if (ShowBannerDataSolar)
        {
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += onBannerAdRevenuePaidEventSolarSDK;
        }
    }

    private void ShowMaxBanner()
    {
        if (PlayerPrefs.GetInt("RemoveAds") != 1)
        {
            MaxSdk.ShowBanner(maxID.bannerAdUnitId);
        }
    }

    private void HideMaxBanner()
    {
        MaxSdk.HideBanner(maxID.bannerAdUnitId);
    }


    #endregion

    private bool MaxBannerADLoaded = false;
    #region Banner Event Call Backs

    private void OnMaxBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        HideAdMobBanner1();
        HideAdMobBanner2();
        MaxBannerADLoaded = true;
    }

    private void OnMaxBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        MaxBannerADLoaded = false;
    }

    private void OnMaxBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnMaxBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnMaxBannerAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnMaxBannerAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
    #endregion

    #region Medium Rectangle AD Code Block
    private void InitializeMaxMRecAds()
    {
        // MRECs are sized to 300x250 on phones and tablets
        MaxSdk.CreateMRec(maxID.mrecAdUnitId, MediumRectanglePosition);
        MaxSdk.SetMRecPlacement(maxID.mrecAdUnitId, "�placement�");
        MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMaxMRecAdLoadedEvent;
        MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMaxMRecAdLoadFailedEvent;
        MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMaxMRecAdClickedEvent;
        //MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMaxMRecAdRevenuePaidEvent;
        MaxSdkCallbacks.MRec.OnAdExpandedEvent += OnMaxMRecAdExpandedEvent;
        MaxSdkCallbacks.MRec.OnAdCollapsedEvent += OnMaxMRecAdCollapsedEvent;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        if (ShowBannerDataSolar)
        {
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += onMediumRectangleRevenuePaidEventSolarSDK;
        }
    }

    private void ShowMaxMRECBanner()
    {
#if UNITY_IOS || Use_RemoteConfing_Android
        if (!MAXMRECADSwitch)
        {
            return;
        }
#endif

        if (PlayerPrefs.GetInt("RemoveAds") != 1)
        {
            MaxSdk.ShowMRec(maxID.mrecAdUnitId);
        }
    }

    private void HideMaxMRECBanner()
    {
        MaxSdk.HideMRec(maxID.mrecAdUnitId);
    }

    #endregion

    private bool MaxMRecADLoaded = false;
    #region Medium Rectangle AD Event Call Back

    private void OnMaxMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        MaxMRecADLoaded = true;

    }

    private void OnMaxMRecAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
    {
        MaxMRecADLoaded = false;
    }
    private void OnMaxMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
    private void OnMaxMRecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
    private void OnMaxMRecAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
    private void OnMaxMRecAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }
    #endregion

    #region Interstitial AD Code Block

    private int retryAttempt;

    private void InitializeMaxInterstitialAds()
    {
        // Attach callback
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += onInterstitialRevenuePaidEventSolarSDK;



        // Load the first interstitial
        LoadMaxInterstitial();
    }

    private void LoadMaxInterstitial()
    {
        MaxSdk.LoadInterstitial(maxID.InterstitialadUnitId);
    }

    private void ShowMaxInterstitial()
    {
        MaxSdk.ShowInterstitial(maxID.InterstitialadUnitId);
        MaxSdk.ShowInterstitial(maxID.InterstitialadUnitId, "�placement�");

    }

    #endregion

    #region Interstitial AD Event Call Back
    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is ready for you to show. MaxSdk.IsInterstitialReady(adUnitId) now returns 'true'

        // Reset retry attempt
        retryAttempt = 0;
    }

    private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Interstitial ad failed to load 
        // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds)

        retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, retryAttempt));

        Invoke("LoadMaxInterstitial", (float)retryDelay);
    }

    private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad failed to display. AppLovin recommends that you load the next ad.
        LoadMaxInterstitial();
    }

    private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is hidden. Pre-load the next ad

        LoadMaxInterstitial();
    }
    #endregion

    #region Reward AD Code Block

    private int Reward_retryAttempt;

    private void InitializeMaxRewardedAds()
    {
        // Attach callback
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEventSolarSDK;



        // Load the first rewarded ad
        LoadMaxRewardedAd();
    }

    private void LoadMaxRewardedAd()
    {
        MaxSdk.LoadRewardedAd(maxID.RewardadUnitId);
    }

    private void ShowMaxRewardedAd(Action _delegate)
    {
        NotifyReward = _delegate;
        MaxSdk.ShowRewardedAd(maxID.RewardadUnitId);
        MaxSdk.ShowRewardedAd(maxID.RewardadUnitId, "�placement�");

    }

    #endregion

    #region Reward AD Event Call Back

    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is ready for you to show. MaxSdk.IsRewardedAdReady(adUnitId) now returns 'true'.

        // Reset retry attempt
        Reward_retryAttempt = 0;
    }

    private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Rewarded ad failed to load 
        // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds).

        Reward_retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, Reward_retryAttempt));

        Invoke("LoadMaxRewardedAd", (float)retryDelay);
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad failed to display. AppLovin recommends that you load the next ad.
        LoadMaxRewardedAd();
    }

    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is hidden. Pre-load the next ad
        LoadMaxRewardedAd();
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        // The rewarded ad displayed and the user should receive the reward.

        if (NotifyReward != null)
        {
            NotifyReward.Invoke();
        }
        NotifyReward = null;
    }

    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Ad revenue paid. Use this callback to track user revenue.
    }

    #endregion

    #region Reward Video Calling Sample

    private void WatchReward()
    {
        ShowMaxRewardedAd(rewardDone);
    }

    private void rewardDone()
    {
        EditorLogger.LogError("rewardCompleted");
    }

    #endregion

    #region AppOpen Ad Code Block
    public void LoadMaxAppOpen()
    {
        MaxSdk.LoadAppOpenAd(maxID.AppOpenAdUnitId);
    }
    #endregion

    #region AppOpenCallBacks
    private void OnAppOpenAdLoaded(string adUnitid, MaxSdkBase.AdInfo adInfo)
    {

    }
    private void OnAppOpenAdLoadingFailed(string adUnitid, MaxSdkBase.AdInfo adInfo)
    {
    }
    private void OnAppOpenAdDisplayed(string adUnitid, MaxSdkBase.AdInfo adInfo)
    {
        OnAppOpenCall();
    }
    private void OnAppOpenAdClosed(string adUnitid, MaxSdkBase.AdInfo adInfo)
    {
        ShowTopBannerWithMediation();
        CanShowAppOpenAfterDelay(false);
    }
    #endregion

    #region AppOpen Code Block
    public bool IsAdAvailable
    {
        get
        {
            return MaxSdk.IsAppOpenAdReady(maxID.AppOpenAdUnitId)
                   && DateTime.Now < _expireTime;
        }
    }

    public void InitilializedAppOpenAD()
    {
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenDismissedEvent;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenRevenuePaidEventSolarSDK;
        MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoadingFailed;
        MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayed;
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdClosed;

#if UNITY_EDITOR
        EditorLogger.LogError("<color=yellow>App Open Initialized</color>");
#endif
        showAppOpen_IfReady();
    }

    private void showAppOpen_IfReady()
    {
        if (PlayerPrefs.GetInt("RemoveAds") != 1)
        {
#if UNITY_EDITOR
            EditorLogger.LogError("<color=yellow>AppOpen Checked</color>");
#endif
            if (MaxSdk.IsAppOpenAdReady(maxID.AppOpenAdUnitId))
            {
#if UNITY_EDITOR
                EditorLogger.LogError("<color=yellow>App Open Called</color>");
#endif
                HideMaxBanner();
                MaxSdk.ShowAppOpenAd(maxID.AppOpenAdUnitId);
            }
            else
            {
#if UNITY_EDITOR
                EditorLogger.LogError("<color=yellow>App Open Loaded Again</color>");
#endif
                LoadMaxAppOpen();
                // MaxSdk.LoadAppOpenAd(maxID.AppOpenAdUnitId);
            }
        }
    }

    private void ShowMaxAppOpen()
    {
        if (AllowedToShowAppOpenAd && AppOpenCalls == 0)
        {
            CanShowAppOpenAfterDelay(false);
            MaxSdk.ShowAppOpenAd(maxID.AppOpenAdUnitId);
        }

    }

    /*private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            if (!AdShowing)
            {
                AdShowing = false;
                ShowAppOpen();
               /* if (IsAdAvailable)
                {
                    showAppOpen_IfReady();
                }#1#

            }
            else
            {
                AdShowing = false;
            }
        }
    }*/
    #endregion

    #region AppOpen Event Call Backs
    private void OnAppOpenDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {


        AdsManager.Instance.ShowAdmobTopBanners();
        LoadMaxAppOpen();
        //MaxSdk.LoadAppOpenAd(maxID.AppOpenAdUnitId);
    }
    #endregion

    [HideInInspector]
    public int BannerImpCount, RewardedImpCount, InterstitialImpCount, MRECImpCount, AppOpenImpCount;

    private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo impressionData)
    {
        if (impressionData.NetworkName == "Google AdMob" || impressionData.NetworkName == "Google AdMob " || impressionData.NetworkName == "AdMob")
        {
            return;
            double revenue = impressionData.Revenue / 2;
            var impressionParameters = new[]

            {
                new Firebase.Analytics.Parameter("ad_platform", "AppLovin"),
                new Firebase.Analytics.Parameter("ad_source", impressionData.NetworkName),
                new Firebase.Analytics.Parameter("ad_unit_name", impressionData.AdUnitIdentifier),
                new Firebase.Analytics.Parameter("ad_format", impressionData.AdFormat),
                new Firebase.Analytics.Parameter("value", revenue),
                new Firebase.Analytics.Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
            };
            Firebase.Analytics.FirebaseAnalytics.LogEvent("impressionCall");
            Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);
        }
        else
        {
            double revenue = impressionData.Revenue;
            var impressionParameters = new[] {
                new Firebase.Analytics.Parameter("ad_platform", "AppLovin"),
                new Firebase.Analytics.Parameter("ad_source", impressionData.NetworkName),
                new Firebase.Analytics.Parameter("ad_unit_name", impressionData.AdUnitIdentifier),
                new Firebase.Analytics.Parameter("ad_format", impressionData.AdFormat),
                new Firebase.Analytics.Parameter("value", revenue),
                new Firebase.Analytics.Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
            };
            Firebase.Analytics.FirebaseAnalytics.LogEvent("impressionCall");
            Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);

        }
    }
    private void onBannerAdRevenuePaidEventSolarSDK(string adUnitid, MaxSdkBase.AdInfo adInfo)
    {
        BannerImpCount++;
        EditorLogger.Log("Banner ad revenue paid");
        EditorLogger.Log("Banner ad record an impression" + BannerImpCount);
#if UNITY_IOS
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();
        
#endif
#if UNITY_ANDROID
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();

#endif
        impressionAttributes.ad_platform = adInfo.NetworkName;
        impressionAttributes.ad_id = adUnitid;
        impressionAttributes.ad_type = 5;
        impressionAttributes.ad_ecpm = adInfo.Revenue * 1000.00;
        impressionAttributes.currency_type = "USD";
        impressionAttributes.mediation_platform = "MAX";
        impressionAttributes.is_rendered = true;
        SolarEngine.Analytics.trackAdImpression(impressionAttributes);
    }
    private void OnRewardedAdRevenuePaidEventSolarSDK(string adUnitid, MaxSdkBase.AdInfo adInfo)
    {
        RewardedImpCount++;
        EditorLogger.Log("Rewarded ad revenue paid");
        EditorLogger.Log("Rewarded ad record an impression" + RewardedImpCount);
#if UNITY_IOS
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();
        
#endif
#if UNITY_ANDROID
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();

#endif
        impressionAttributes.ad_platform = adInfo.NetworkName;
        impressionAttributes.ad_id = adUnitid;
        impressionAttributes.ad_type = 1;
        impressionAttributes.ad_ecpm = adInfo.Revenue * 1000.00;
        impressionAttributes.currency_type = "USD";
        impressionAttributes.mediation_platform = "MAX";
        impressionAttributes.is_rendered = true;
        SolarEngine.Analytics.trackAdImpression(impressionAttributes);
    }
    private void OnAppOpenRevenuePaidEventSolarSDK(string adUnitid, MaxSdkBase.AdInfo adInfo)
    {
        AppOpenImpCount++;
        EditorLogger.Log("AppOpen ad revenue paid");
        EditorLogger.Log("AppOpen ad record an impression" + AppOpenImpCount);
#if UNITY_IOS
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();
        
#endif
#if UNITY_ANDROID
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();

#endif
        impressionAttributes.ad_platform = adInfo.NetworkName;
        impressionAttributes.ad_id = adUnitid;
        impressionAttributes.ad_type = 0;
        impressionAttributes.ad_ecpm = adInfo.Revenue * 1000.00;
        impressionAttributes.currency_type = "USD";
        impressionAttributes.mediation_platform = "MAX";
        impressionAttributes.is_rendered = true;
        SolarEngine.Analytics.trackAdImpression(impressionAttributes);
    }
    private void onInterstitialRevenuePaidEventSolarSDK(string adUnitid, MaxSdkBase.AdInfo adInfo)
    {
        InterstitialImpCount++;
        EditorLogger.Log("Interstitial ad revenue paid");
        EditorLogger.Log("Interstitial ad record an impression" + InterstitialImpCount);
#if UNITY_IOS
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();
        
#endif
#if UNITY_ANDROID
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();

#endif
        impressionAttributes.ad_platform = adInfo.NetworkName;
        impressionAttributes.ad_id = adUnitid;
        impressionAttributes.ad_type = 3;
        impressionAttributes.ad_ecpm = adInfo.Revenue * 1000.00;
        impressionAttributes.currency_type = "USD";
        impressionAttributes.mediation_platform = "MAX";
        impressionAttributes.is_rendered = true;
        SolarEngine.Analytics.trackAdImpression(impressionAttributes);
    }
    private void onMediumRectangleRevenuePaidEventSolarSDK(string adUnitid, MaxSdkBase.AdInfo adInfo)
    {
        MRECImpCount++;
        EditorLogger.Log("Medium Rectangle ad revenue paid");
        EditorLogger.Log("Medium Rectangle ad record an impression" + MRECImpCount);
#if UNITY_IOS
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();
        
#endif
#if UNITY_ANDROID
        ImpressionAttributes impressionAttributes = new ImpressionAttributes();

#endif
        impressionAttributes.ad_platform = adInfo.NetworkName;
        impressionAttributes.ad_id = adUnitid;
        impressionAttributes.ad_type = 10;
        impressionAttributes.ad_ecpm = adInfo.Revenue * 1000.00;
        impressionAttributes.currency_type = "USD";
        impressionAttributes.mediation_platform = "MAX";
        impressionAttributes.is_rendered = true;
        SolarEngine.Analytics.trackAdImpression(impressionAttributes);
    }

    #endregion

    private bool ShowedAppOpenAtStart;

    public void ShowAdMobAppOpenAtStart()
    {
        if (!ShowedAppOpenAtStart)
        {
            ShowedAppOpenAtStart = true;
            ShowAdMobAppOpen();
            InitilializedAppOpenAD();
            LoadMaxAppOpen();
        }
    }

    #region IAP

    public void ForceStopAppOpen()
    {
        CanShowAppOpenAfterDelay(false);
    }

    public void RemoveADs()
    {
        PlayerPrefs.SetInt("RemoveAds", 1);
        PlayerPrefs.Save();
        HideAllBanners();
    }
    #endregion
}
