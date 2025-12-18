using System.Runtime.InteropServices;
using UnityEngine;

namespace TelegramMiniApp
{
    public static class TelegramWebApp
    {
        public const string BridgeGameObjectName = "TelegramMiniAppBridge";
        public const string UserDataCallbackMethodName = "OnTelegramUserDataJson";
        public const string UserAgentCallbackMethodName = "OnTelegramUserAgent";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void TelegramWebApp_RequestUserDataJson(string gameObjectName, string callbackMethodName);

        [DllImport("__Internal")]
        private static extern void TelegramWebApp_RequestUserAgent(string gameObjectName, string callbackMethodName);
#else
        private static void TelegramWebApp_RequestUserDataJson(string gameObjectName, string callbackMethodName) { }
        private static void TelegramWebApp_RequestUserAgent(string gameObjectName, string callbackMethodName) { }
#endif

        public static void RequestUserDataJson()
        {
            TelegramWebApp_RequestUserDataJson(BridgeGameObjectName, UserDataCallbackMethodName);
        }

        public static void RequestUserAgent()
        {
            TelegramWebApp_RequestUserAgent(BridgeGameObjectName, UserAgentCallbackMethodName);
        }
    }
}
