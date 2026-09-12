using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Boots_Flow 
{
    // 供外部使用 不需要在当前项目中使用
    public async UniTask Init()
    {
        var _dis = new GameObject("AndroidJavaMessageDispatcher");
        _dis.AddComponent<Bizza.Unity.Android.AndroidJavaMessageDispatcher>();

        #if WKY_SDK
        Bizza.Unity.Android.AndroidJavaMessageDispatcher.SetHttpMessageHandler(
            HttpUtil.LoginHTTPDict);
        _dis.AddComponent<TipNativeBridge>();
        var _wkyFLow = new WKY_Flow();
        await _wkyFLow.Start_Init_Flow();
        #endif

        #if USER_VERIFICATION_SDK
        var authFlow = new Bizza.TokenClientSystem.AuthFlow(null);
        // AppId 由 WKY SDK 注入的 DeviceInfoUtil 提供；独立运行时由 AuthConfig.bytes 提供。
        await authFlow.Init();
        #endif
    }
}
