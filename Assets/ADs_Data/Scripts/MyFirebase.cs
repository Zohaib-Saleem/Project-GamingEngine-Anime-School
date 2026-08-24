using Firebase.Analytics;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class MyFirebase : MonoBehaviour
{
    public static MyFirebase Instance;
    private Firebase.FirebaseApp app;
    private bool isFirebaseInitialized = false;

#if UNITY_IOS || Use_RemoteConfing_Android
    public string adSwitchFirebasestring, AdMobBannerADSwitchstring, AdMobMRECADSwitchstring, AdMobInterstitialADSwitchstring, ADMobAppOpenADSwitchstring
        , MAXBannerADSwitchstring, MAXMRECADSwitchstring, MAXInterstitialADSwitchstring, MAXAppOpenADSwitchstring, AdsTimerString,
       showAppOpenOnMaxInterFallBackString, dailyrewardbackadstring = "dailyrewardbackad", beforemainmenustring = "beforemainmenu",
       mainmenustartadstring = "mainmenustartad", modeselectionadstring = "modeselectionad", levelselectadstring = "levelselectad",
       pausebuttonadstring = "pausebuttonad", removeadspaneladstring = "removeadspanelad";
#endif
#if UNITY_IOS || Use_RemoteConfing_Android
    public bool adSwitchFirebase, AdMobBannerADSwitch, AdMobMRECADSwitch, AdMobInterstitialADSwitch, ADMobAppOpenADSwitch
        , MAXBannerADSwitch, MAXMRECADSwitch, MAXInterstitialADSwitch, MAXAppOpenADSwitch, AdsTimerSwitch
        , showAppOpenOnMaxInterFallBack, dailyrewardbackad = true, beforemainmenu = true
        , mainmenustartad = true, modeselectionad = true, levelselectad = true, pausebuttonad = true, removeadspanelad = true;
#endif


    public AdsManager adsData;
    public bool IsInternet;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Instance = this;
        StartCoroutine(CheckInternetAndInitialize());
    }

    IEnumerator CheckInternetAndInitialize()
    {
        UnityWebRequest request = new UnityWebRequest("https://www.google.com");
        request.method = UnityWebRequest.kHttpVerbHEAD;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("No internet connection. Firebase Remote Config will be skipped.");
#if UNITY_IOS || Use_RemoteConfing_Android
            adsData.adSwitchFirebase = true;
            adsData.AdMobBannerADSwitch = true;
            adsData.AdMobMRECADSwitch = true;
            adsData.AdMobInterstitialADSwitch = true;
            adsData.ADMobAppOpenADSwitch = true;
            adsData.MAXBannerADSwitch = false;
            adsData.MAXMRECADSwitch = true;
            adsData.MAXInterstitialADSwitch = true;
            adsData.MAXAppOpenADSwitch = true;
            adsData.Adtimers = false;
            adsData.dailyrewardbackad = true;
            adsData.beforemainmenu = true;
            adsData.mainmenustartad = true;
            adsData.modeselectionad = true;
            adsData.levelselectad = true;
            adsData.pausebuttonad = true;
            adsData.removeadspanelad = true;
#endif

        }
        else
        {
            InitializeFirebase();
        }
    }

    public void InitializeFirebase()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                app = Firebase.FirebaseApp.DefaultInstance;
                isFirebaseInitialized = true;
                Debug.Log("Firebase is initialized successfully!");
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                remoteFetch();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    public void remoteFetch()
    {
        Dictionary<string, object> defaults = new Dictionary<string, object>
        {
            { "dailyrewardbackad", true },
            { "beforemainmenu", true },
            { "mainmenustartad", true },
            { "modeselectionad", true },
            { "levelselectad", true },
            { "pausebuttonad", true },
            { "removeadspanelad", true }
        };

        try
        {
            Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults)
                .ContinueWithOnMainThread(task =>
                {
                    Debug.Log("RemoteConfig configured and ready!");
                    FetchDataAsync();
                });
        }
        catch (Exception ex)
        {
            Debug.LogWarning("RemoteConfig initialization failed: " + ex.Message);
        }
    }

    public void showFetchData()
    {
        try
        {
#if UNITY_IOS || Use_RemoteConfing_Android
            adSwitchFirebase = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(adSwitchFirebasestring).BooleanValue;
            adsData.adSwitchFirebase = adSwitchFirebase;

            AdMobBannerADSwitch = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(AdMobBannerADSwitchstring).BooleanValue;
            adsData.AdMobBannerADSwitch = AdMobBannerADSwitch;

            AdMobMRECADSwitch = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(AdMobMRECADSwitchstring).BooleanValue;
            adsData.AdMobMRECADSwitch = AdMobMRECADSwitch;

            AdMobInterstitialADSwitch = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(AdMobInterstitialADSwitchstring).BooleanValue;
            adsData.AdMobInterstitialADSwitch = AdMobInterstitialADSwitch;

            ADMobAppOpenADSwitch = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(ADMobAppOpenADSwitchstring).BooleanValue;
            adsData.ADMobAppOpenADSwitch = ADMobAppOpenADSwitch;

            MAXBannerADSwitch = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(MAXBannerADSwitchstring).BooleanValue;
            adsData.MAXBannerADSwitch = MAXBannerADSwitch;

            MAXMRECADSwitch = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(MAXMRECADSwitchstring).BooleanValue;
            adsData.MAXMRECADSwitch = MAXMRECADSwitch;

            MAXInterstitialADSwitch = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(MAXInterstitialADSwitchstring).BooleanValue;
            adsData.MAXInterstitialADSwitch = MAXInterstitialADSwitch;

            MAXAppOpenADSwitch = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(MAXAppOpenADSwitchstring).BooleanValue;
            adsData.MAXAppOpenADSwitch = MAXAppOpenADSwitch;

            AdsTimerSwitch = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(AdsTimerString).BooleanValue;
            adsData.Adtimers = AdsTimerSwitch;

           

            showAppOpenOnMaxInterFallBack= Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(showAppOpenOnMaxInterFallBackString).BooleanValue;
            adsData.showAppOpenOnMaxInterFallBack = showAppOpenOnMaxInterFallBack;

            string dailyKey = string.IsNullOrEmpty(dailyrewardbackadstring) ? "dailyrewardbackad" : dailyrewardbackadstring;
            dailyrewardbackad = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(dailyKey).BooleanValue;
            adsData.dailyrewardbackad = dailyrewardbackad;

            string beforeMenuKey = string.IsNullOrEmpty(beforemainmenustring) ? "beforemainmenu" : beforemainmenustring;
            beforemainmenu = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(beforeMenuKey).BooleanValue;
            adsData.beforemainmenu = beforemainmenu;

            string startKey = string.IsNullOrEmpty(mainmenustartadstring) ? "mainmenustartad" : mainmenustartadstring;
            mainmenustartad = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(startKey).BooleanValue;
            adsData.mainmenustartad = mainmenustartad;

            string modeKey = string.IsNullOrEmpty(modeselectionadstring) ? "modeselectionad" : modeselectionadstring;
            modeselectionad = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(modeKey).BooleanValue;
            adsData.modeselectionad = modeselectionad;

            string levelKey = string.IsNullOrEmpty(levelselectadstring) ? "levelselectad" : levelselectadstring;
            levelselectad = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(levelKey).BooleanValue;
            adsData.levelselectad = levelselectad;

            string pauseKey = string.IsNullOrEmpty(pausebuttonadstring) ? "pausebuttonad" : pausebuttonadstring;
            pausebuttonad = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(pauseKey).BooleanValue;
            adsData.pausebuttonad = pausebuttonad;

            string removeAdsKey = string.IsNullOrEmpty(removeadspaneladstring) ? "removeadspanelad" : removeadspaneladstring;
            removeadspanelad = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.GetValue(removeAdsKey).BooleanValue;
            adsData.removeadspanelad = removeadspanelad;

            

            IsInternet = true;
#endif
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Error fetching remote config value: " + ex.Message);
#if UNITY_IOS || Use_RemoteConfing_Android
#if true
            adsData.adSwitchFirebase = true;
            adsData.AdMobBannerADSwitch = true;
            adsData.AdMobMRECADSwitch = true;
            adsData.AdMobInterstitialADSwitch = true;
            adsData.ADMobAppOpenADSwitch = true;
            adsData.MAXBannerADSwitch = false;
            adsData.MAXMRECADSwitch = true;
            adsData.MAXInterstitialADSwitch = true;
            adsData.MAXAppOpenADSwitch = true;
            adsData.Adtimers = false;
            adsData.dailyrewardbackad = true;
            adsData.beforemainmenu = true;
            adsData.mainmenustartad = true;
            adsData.modeselectionad = true;
            adsData.levelselectad = true;
            adsData.pausebuttonad = true;
            adsData.removeadspanelad = true;
#endif
#endif

        }
    }

    void FetchComplete(Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
            Debug.Log("Fetch canceled.");
        }
        else if (fetchTask.IsFaulted)
        {
            Debug.Log("Fetch encountered an error.");
#if UNITY_IOS || Use_RemoteConfing_Android
            adsData.adSwitchFirebase = true;
            adsData.AdMobBannerADSwitch = true;
            adsData.AdMobMRECADSwitch = true;
            adsData.AdMobInterstitialADSwitch = true;
            adsData.ADMobAppOpenADSwitch = true;
            adsData.MAXBannerADSwitch = false;
            adsData.MAXMRECADSwitch = true;
            adsData.MAXInterstitialADSwitch = true;
            adsData.MAXAppOpenADSwitch = true;
            adsData.Adtimers = false;
            adsData.dailyrewardbackad = true;
            adsData.beforemainmenu = true;
            adsData.mainmenustartad = true;
            adsData.modeselectionad = true;
            adsData.levelselectad = true;
            adsData.pausebuttonad = true;
            adsData.removeadspanelad = true;
#endif
        }
        else if (fetchTask.IsCompleted)
        {
            Debug.Log("Fetch completed successfully!");
        }

        var info = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.Info;

        switch (info.LastFetchStatus)
        {
            case Firebase.RemoteConfig.LastFetchStatus.Success:
                Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                    .ContinueWithOnMainThread(task =>
                    {
                        Debug.Log($"Remote data loaded (last fetch time {info.FetchTime}).");
                        showFetchData();
                    });
                break;

            case Firebase.RemoteConfig.LastFetchStatus.Failure:
                Debug.Log("RemoteConfig Fetch failed.");
#if UNITY_IOS || Use_RemoteConfing_Android
                adsData.adSwitchFirebase = true;
                adsData.AdMobBannerADSwitch = true;
                adsData.AdMobMRECADSwitch = true;
                adsData.AdMobInterstitialADSwitch = true;
                adsData.ADMobAppOpenADSwitch = true;
                adsData.MAXBannerADSwitch = true;
                adsData.MAXMRECADSwitch = true;
                adsData.MAXInterstitialADSwitch = true;
                adsData.MAXAppOpenADSwitch = true;
                adsData.dailyrewardbackad = true;
                adsData.beforemainmenu = true;
                adsData.mainmenustartad = true;
                adsData.modeselectionad = true;
                adsData.levelselectad = true;
                adsData.pausebuttonad = true;
                adsData.removeadspanelad = true;
#endif

                break;

            case Firebase.RemoteConfig.LastFetchStatus.Pending:
                Debug.Log("Latest Fetch call still pending.");
                break;
        }
    }

    public Task FetchDataAsync()
    {
        Debug.Log("Fetching data...");
        Task fetchTask = Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
        return fetchTask.ContinueWithOnMainThread(FetchComplete);
    }
}