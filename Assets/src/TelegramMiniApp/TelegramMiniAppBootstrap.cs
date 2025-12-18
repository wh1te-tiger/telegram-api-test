using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace TelegramMiniApp
{
    public sealed class TelegramMiniAppBootstrap : MonoBehaviour
    {
        private Text _text;
        private TelegramMiniAppBridge _bridge;
        private float _timeSinceStart;
        private bool _received;
        private string _userAgent;
        private string _userDataJson;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var existing = GameObject.Find(TelegramWebApp.BridgeGameObjectName);
            if (existing != null)
                return;

            var root = new GameObject("TelegramMiniAppRoot");
            DontDestroyOnLoad(root);
            root.AddComponent<TelegramMiniAppBootstrap>();
        }

        private void Awake()
        {
            _bridge = new GameObject(TelegramWebApp.BridgeGameObjectName).AddComponent<TelegramMiniAppBridge>();
            _bridge.UserDataJsonReceived += HandleUserDataJson;
            _bridge.UserAgentReceived += HandleUserAgent;

            _text = CreateOverlayText();
            _text.text =
                "Telegram Mini App (Unity WebGL)\n" +
                "Waiting for Telegram.WebApp...\n\n" +
                "If you run this in a normal browser, user data will be empty.\n";

            TelegramWebApp.RequestUserDataJson();
            TelegramWebApp.RequestUserAgent();
        }

        private void OnDestroy()
        {
            if (_bridge != null)
                _bridge.UserDataJsonReceived -= HandleUserDataJson;
            if (_bridge != null)
                _bridge.UserAgentReceived -= HandleUserAgent;
        }

        private void Update()
        {
            if (_received)
                return;

            _timeSinceStart += Time.unscaledDeltaTime;
            if (_timeSinceStart < 2.0f)
                return;

            _text.text =
                "Telegram Mini App (Unity WebGL)\n" +
                "No response from Telegram.WebApp yet.\n\n" +
                "Open this build inside Telegram using a bot Web App button.\n" +
                "For local test: Telegram Desktop + URL http://127.0.0.1:PORT/\n";
            _received = true;
        }

        private void HandleUserDataJson(string json)
        {
            _received = true;
            _userDataJson = json ?? "";
            RenderConfirmedInfo();
        }

        private void HandleUserAgent(string userAgent)
        {
            _received = true;
            _userAgent = userAgent ?? "";
            RenderConfirmedInfo();
        }

        private void RenderConfirmedInfo()
        {
            var ua = string.IsNullOrWhiteSpace(_userAgent) ? "<empty>" : _userAgent;
            var json = string.IsNullOrWhiteSpace(_userDataJson) ? "<empty>" : PrettyPrintJson(_userDataJson);

            _text.text =
                "Telegram Mini App (Unity WebGL)\n\n" +
                "User-Agent:\n" +
                ua + "\n\n" +
                "Telegram.WebApp payload:\n" +
                json;
        }

        private static string PrettyPrintJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            var builder = new StringBuilder(json.Length * 2);
            var indent = 0;
            var inString = false;
            var escape = false;

            foreach (var ch in json)
            {
                if (inString)
                {
                    builder.Append(ch);
                    if (escape)
                    {
                        escape = false;
                        continue;
                    }

                    if (ch == '\\')
                    {
                        escape = true;
                        continue;
                    }

                    if (ch == '"')
                        inString = false;

                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inString = true;
                        builder.Append(ch);
                        break;
                    case '{':
                    case '[':
                        builder.Append(ch);
                        builder.Append('\n');
                        indent++;
                        AppendIndent(builder, indent);
                        break;
                    case '}':
                    case ']':
                        builder.Append('\n');
                        indent = Math.Max(0, indent - 1);
                        AppendIndent(builder, indent);
                        builder.Append(ch);
                        break;
                    case ',':
                        builder.Append(ch);
                        builder.Append('\n');
                        AppendIndent(builder, indent);
                        break;
                    case ':':
                        builder.Append(": ");
                        break;
                    default:
                        if (!char.IsWhiteSpace(ch))
                            builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        private static void AppendIndent(StringBuilder builder, int indent)
        {
            for (var i = 0; i < indent; i++)
                builder.Append("  ");
        }

        private static Text CreateOverlayText()
        {
            var canvasGo = new GameObject("TelegramMiniAppCanvas");
            DontDestroyOnLoad(canvasGo);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1.0f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.65f);

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.offsetMin = new Vector2(24, 24);
            panelRect.offsetMax = new Vector2(-24, -24);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(panelGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(24, 24);
            rect.offsetMax = new Vector2(-24, -24);

            return text;
        }
    }
}
