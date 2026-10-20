using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Single entry point for Firebase Analytics events (GamEmpires event standard: GF_ / BF_ / AF_ / MF_).
/// Never throws: any Firebase problem is logged as a warning and the game keeps running.
/// Events sent before Firebase is ready are queued and flushed once it initializes.
/// </summary>
public static class AnalyticsEvents
{
    // Firebase limit is 40, keep names strictly under it
    const int MaxNameLength = 39;
    const int MaxQueuedEvents = 100;

    // Mission names by gameplay slot (Level_Handler.levelNo)
    static readonly string[] MissionNames =
    {
        "GetReadyForSchool",
        "RideToSchool",
        "ClassQuizTest",
        "MeetFriendsAtGate",
        "LostHomework",
        "WrongNotebook",
        "StudyDate",
        "SportsDayPractice",
        "TheMisunderstanding",
        "SunsetOnTheCourt"
    };

    static readonly object queueLock = new object();
    static readonly Queue<string> queued = new Queue<string>();
    static volatile bool firebaseReady;
    static int mainThreadId = -1;
    static string lastEventName;
    static int lastEventFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CaptureMainThread()
    {
        mainThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public static string MissionName(int gameplaySlot)
    {
        if (gameplaySlot >= 0 && gameplaySlot < MissionNames.Length)
            return MissionNames[gameplaySlot];
        return "Mission" + (gameplaySlot + 1);
    }

    /// <summary>Called by MyFirebase once Firebase is initialized; sends anything queued so far.</summary>
    public static void SetFirebaseReady()
    {
        firebaseReady = true;

        string[] pending;
        lock (queueLock)
        {
            pending = queued.ToArray();
            queued.Clear();
        }
        for (int i = 0; i < pending.Length; i++)
            Send(pending[i]);
    }

    public static void Log(string eventName)
    {
        try
        {
            string name = Sanitize(eventName);
            if (string.IsNullOrEmpty(name))
                return;

            // same event twice in one frame is a double-bound button, not a real second action
            if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
            {
                if (name == lastEventName && Time.frameCount == lastEventFrame)
                    return;
                lastEventName = name;
                lastEventFrame = Time.frameCount;
            }

            if (!firebaseReady)
            {
                lock (queueLock)
                {
                    if (queued.Count < MaxQueuedEvents)
                        queued.Enqueue(name);
                }
                return;
            }

            Send(name);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Analytics: could not log " + eventName + ": " + e.Message);
        }
    }

    static void Send(string name)
    {
        try
        {
            Firebase.Analytics.FirebaseAnalytics.LogEvent(name);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Analytics event: " + name);
#endif
        }
        catch (Exception e)
        {
            Debug.LogWarning("Analytics: Firebase LogEvent failed for " + name + ": " + e.Message);
        }
    }

    // Firebase names: letters, digits and underscores, starting with a letter, under 40 chars
    static string Sanitize(string eventName)
    {
        if (string.IsNullOrEmpty(eventName))
            return null;

        var sb = new StringBuilder(eventName.Length);
        for (int i = 0; i < eventName.Length && sb.Length < MaxNameLength; i++)
        {
            char c = eventName[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
                sb.Append(c);
        }

        while (sb.Length > 0 && !char.IsLetter(sb[0]))
            sb.Remove(0, 1);

        if (sb.Length == 0)
            return null;

#if UNITY_EDITOR
        if (eventName.Length > MaxNameLength)
            Debug.LogWarning("Analytics: event name too long, truncated: " + eventName);
#endif
        return sb.ToString();
    }
}
