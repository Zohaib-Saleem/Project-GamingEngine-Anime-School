using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Analytics;
//using GameAnalyticsSDK;
using UnityEngine;

public class FireBase_Event : MonoBehaviour
{
    public static FireBase_Event instance;
    public AdsManager MyManager;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
      //  GameAnalytics.Initialize();
    }

    public void Log_FB_Event(string log)
    {
      FirebaseAnalytics.LogEvent(log);
     LogDesignEvent(log);
    }
    public void Intser()
    {
        if (MyManager)
        {
            MyManager.ShowInterstitialWithLoading();
        }
    }
    public void TopBan()
    {
        if (MyManager)
        {
            MyManager.ShowTopBannerWithMediation();
        }
    }
    public void Mrec()
    {
        if (MyManager)
        {
            MyManager.ShowMediumRect();
        }
    }
    public void AppOpenStart()
    {
        if (MyManager)
        {
            MyManager.ShowAppOpenWithMediation();
        }
    }

    public void hideallads()
    {
        if (MyManager)
        {
            MyManager.HideAllBanners();
        }
    }
    public void rew()
    {
        if (MyManager)
        {
            MyManager.ShowRewardedWithLoading(rewGrant,true);
        }
    }
    public void rewGrant()
    {
        Log_FB_Event("RewardGranted");
    }
    public void MissionOrLevelStartedEventLog(string GameMode, int LevelNumber)
    {
        FirebaseAnalytics.LogEvent("LevelStarted_" + GameMode + "_Level_" + LevelNumber.ToString());
      //  LogProgressionEvent(GAProgressionStatus.Start, "LevelStarted_" + GameMode + "_Level_" + LevelNumber);
    }
    public void MissionOrLevelFailEventLog(string GameMode, int LevelNumber)
    {
      //  LogProgressionEvent(GAProgressionStatus.Fail, "LevelFail_" + GameMode + "_Level_" + LevelNumber);
        FirebaseAnalytics.LogEvent("LevelFail_" + GameMode + "_Level_" + LevelNumber.ToString());

    }

    public void MissionOrLevelCompletedEventLog(string GameMode, int LevelNumber)
    {
        FirebaseAnalytics.LogEvent("LevelComplete_" + GameMode + "_Level_" + LevelNumber);
       // LogProgressionEvent(GAProgressionStatus.Complete, "LevelComplete_" + GameMode + "_Level_" + LevelNumber);
    }
    public void MissionOrLevelPauseEventLog(string GameMode, int LevelNumber)
    {
        FirebaseAnalytics.LogEvent("LevelPaused_" + GameMode + "_Level_" + LevelNumber);
    }

    // Example method to log a design event (e.g., player action)
    public void LogDesignEvent(string eventId)
    {
      //  GameAnalytics.NewDesignEvent(eventId);
        Debug.Log($"Design event logged: {eventId}");
    }
    public void LogDesignEvent(string eventId, int Value)
    {
      //  GameAnalytics.NewDesignEvent(eventId, Value);
        Debug.Log($"Design event logged: {eventId}");
    }

    // Example method to log a progression event (e.g., level completion)
  //  public void LogProgressionEvent(GAProgressionStatus status, string progression01, string progression02 = "", string progression03 = "", int score = 0)
   // {
     //   if (string.IsNullOrEmpty(progression02))
     //   {
         //   GameAnalytics.NewProgressionEvent(status, progression01, score);
        //    Debug.Log($"Progression event logged: {status} {progression01} with score {score}");
        //}
        //else if (string.IsNullOrEmpty(progression03))
        //{
        //    GameAnalytics.NewProgressionEvent(status, progression01, progression02, score);
        //    Debug.Log($"Progression event logged: {status} {progression01}:{progression02} with score {score}");
        //}
        //else
        //{
        //    GameAnalytics.NewProgressionEvent(status, progression01, progression02, progression03, score);
        //    Debug.Log($"Progression event logged: {status} {progression01}:{progression02}:{progression03} with score {score}");
        //}
        //  }
    }
