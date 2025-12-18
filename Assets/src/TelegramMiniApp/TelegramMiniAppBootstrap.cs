using System;
using System.Text;
using TelegramMiniApp.Api;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

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
        private string _baseInfo;
        private string _apiStatus;
        private Button _registerButton;
        private Button _loginButton;
        private bool _apiRequestInFlight;

        private const string ApiBaseUrl = "https://194.147.90.24";

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

            EnsureEventSystem();
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
                "Open this build inside Telegram using a bot Web App button.\n";
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

            _baseInfo =
                "Telegram Mini App (Unity WebGL)\n\n" +
                "User-Agent:\n" +
                ua + "\n\n" +
                "Telegram.WebApp payload:\n" +
                json;

            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (string.IsNullOrWhiteSpace(_apiStatus))
            {
                _text.text = _baseInfo;
                return;
            }

            _text.text = _baseInfo + "\n\nAPI test:\n" + _apiStatus;
        }

        private void AppendApiStatus(string line)
        {
            _apiStatus = string.IsNullOrWhiteSpace(_apiStatus) ? line : _apiStatus + "\n" + line;
            RefreshDisplay();
        }

        private void OnRegisterClicked()
        {
            StartApiCall(isLogin: false);
        }

        private void OnLoginClicked()
        {
            StartApiCall(isLogin: true);
        }

        private void StartApiCall(bool isLogin)
        {
            if (_apiRequestInFlight)
            {
                AppendApiStatus("API request already running...");
                return;
            }

            if (string.IsNullOrWhiteSpace(_userDataJson))
            {
                AppendApiStatus("Telegram payload is empty.");
                return;
            }

            var apiBase = ApiBaseUrl;
            if (string.IsNullOrWhiteSpace(apiBase))
            {
                AppendApiStatus("API base URL is not configured.");
                return;
            }

            _apiRequestInFlight = true;
            AppendApiStatus($"Base URL: {apiBase}");
            StartCoroutine(RunTelegramAuthRequest(apiBase, isLogin));
        }

        private System.Collections.IEnumerator RunTelegramAuthRequest(string apiBase, bool isLogin)
        {
            var client = new TelegramAuthApiClient(apiBase);

            if (!isLogin)
            {
                if (!TelegramAuthRequestBuilder.TryBuildRegisterRequest(_userDataJson, out var registerRequest))
                {
                    AppendApiStatus("Register: failed to build request.");
                    _apiRequestInFlight = false;
                    yield break;
                }

                AppendApiStatus("Register: start");
                TelegramAuthRegisterResponse registerResponse = null;
                ApiError registerError = null;
                yield return client.Register(registerRequest, r => registerResponse = r, e => registerError = e);
                if (registerError != null)
                {
                    AppendApiStatus("Register: error - " + registerError.ToSummary());
                    _apiRequestInFlight = false;
                    yield break;
                }

                AppendApiStatus("Register: ok");
                _apiRequestInFlight = false;
                yield break;
            }

            var loginRequest = TelegramAuthRequestBuilder.BuildLoginRequest(_userDataJson);
            if (loginRequest == null)
            {
                AppendApiStatus("Login: failed to build request.");
                _apiRequestInFlight = false;
                yield break;
            }

            AppendApiStatus("Login: start");
            TelegramAuthLoginResponse loginResponse = null;
            ApiError loginError = null;
            yield return client.Login(loginRequest, r => loginResponse = r, e => loginError = e);
            if (loginError != null)
            {
                AppendApiStatus("Login: error - " + loginError.ToSummary());
                _apiRequestInFlight = false;
                yield break;
            }

            var tokenPreview = string.IsNullOrEmpty(loginResponse?.accessToken)
                ? "<empty>"
                : (loginResponse.accessToken.Length > 16 ? loginResponse.accessToken.Substring(0, 16) + "..." : loginResponse.accessToken);
            AppendApiStatus("Login: ok. Token: " + tokenPreview);
            _apiRequestInFlight = false;
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

        private Text CreateOverlayText()
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
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(24, 240);
            rect.offsetMax = new Vector2(-24, -24);

            CreateApiControls(panelGo.transform, font);

            return text;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            var eventSystemGo = new GameObject("EventSystem");
            DontDestroyOnLoad(eventSystemGo);
            eventSystemGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemGo.AddComponent<StandaloneInputModule>();
#endif
        }

        private void CreateApiControls(Transform parent, Font font)
        {
            var controls = new GameObject("ApiControls");
            controls.transform.SetParent(parent, false);
            var controlsRect = controls.AddComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0, 0);
            controlsRect.anchorMax = new Vector2(1, 0);
            controlsRect.pivot = new Vector2(0.5f, 0);
            controlsRect.sizeDelta = new Vector2(0, 96);
            controlsRect.anchoredPosition = new Vector2(0, 24);

            var background = controls.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0.35f);

            var buttonsRow = new GameObject("ButtonsRow");
            buttonsRow.transform.SetParent(controls.transform, false);
            var rowRect = buttonsRow.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 0);
            rowRect.anchorMax = new Vector2(1, 1);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            var layout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.padding = new RectOffset(24, 24, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _registerButton = CreateButton(buttonsRow.transform, "Register", font);
            _loginButton = CreateButton(buttonsRow.transform, "Login", font);

            _registerButton.onClick.AddListener(OnRegisterClicked);
            _loginButton.onClick.AddListener(OnLoginClicked);
        }

        private static Button CreateButton(Transform parent, string label, Font font)
        {
            var buttonGo = new GameObject(label + "Button");
            buttonGo.transform.SetParent(parent, false);
            var image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.18f, 0.52f, 0.95f, 0.95f);

            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(buttonGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}
