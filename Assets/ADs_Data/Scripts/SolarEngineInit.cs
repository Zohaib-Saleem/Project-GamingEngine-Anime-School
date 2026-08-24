using UnityEngine;
using System;
using System.Collections.Generic;
using SolarEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class SolarEngineInit : MonoBehaviour
{
   public String AppKey;

    void Start()
    {
       

        SEConfig seConfig = new SEConfig();
        RCConfig rcConfig = new RCConfig();
        seConfig.logEnabled = true;
        rcConfig.enable = true;

        SolarEngine.Analytics.SEAttributionCallback callback = new SolarEngine.Analytics.SEAttributionCallback(attributionCallback);
        seConfig.attributionCallback = callback;

        SolarEngine.Analytics.SESDKInitCompletedCallback initCallback = new SolarEngine.Analytics.SESDKInitCompletedCallback(onInitCallback);
        seConfig.initCompletedCallback = initCallback;


        SolarEngine.Analytics.preInitSeSdk(AppKey);
        SolarEngine.Analytics.initSeSdk(AppKey, seConfig, rcConfig);


    }

    public void onInitCallback(int code)
    {
        Debug.Log("initialization Success: " + code);

    }

    private void attributionCallback(int errorCode, Dictionary<string, object> attribution)
    {

        if (errorCode != 0)
        {
            // Attribution succeed
            Debug.Log("errorCode = " + errorCode);

        }
        else
        {
            // Attribution fail
            Debug.Log("attSuccessCallback =" + attribution);
        }
    }
}
