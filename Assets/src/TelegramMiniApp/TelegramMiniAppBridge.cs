using System;
using UnityEngine;

namespace TelegramMiniApp
{
    public sealed class TelegramMiniAppBridge : MonoBehaviour
    {
        public event Action<string> UserDataJsonReceived;
        public event Action<string> UserAgentReceived;

        private void Awake()
        {
            gameObject.name = TelegramWebApp.BridgeGameObjectName;
            DontDestroyOnLoad(gameObject);
        }

        public void OnTelegramUserDataJson(string json)
        {
            UserDataJsonReceived?.Invoke(json);
        }

        public void OnTelegramUserAgent(string userAgent)
        {
            UserAgentReceived?.Invoke(userAgent);
        }
    }
}
