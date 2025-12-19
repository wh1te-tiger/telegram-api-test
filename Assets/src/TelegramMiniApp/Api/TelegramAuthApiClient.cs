using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TelegramMiniApp.Api
{
    public sealed class TelegramAuthApiClient
    {
        private readonly string _baseUrl;

        public TelegramAuthApiClient(string baseUrl)
        {
            _baseUrl = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        }

        public IEnumerator Register(TelegramAuthRegisterRequest request, Action<TelegramAuthRegisterResponse, string> onSuccess, Action<ApiError> onError)
        {
            return Post("/api/v1/telegram_auth/register", request, onSuccess, onError);
        }

        public IEnumerator Login(TelegramAuthLoginRequest request, Action<TelegramAuthLoginResponse, string> onSuccess, Action<ApiError> onError)
        {
            return Post("/api/v1/telegram_auth/login", request, onSuccess, onError);
        }

        private IEnumerator Post<TResponse>(string path, object body, Action<TResponse, string> onSuccess, Action<ApiError> onError)
        {
            var url = $"{_baseUrl}{path}";
            var json = JsonUtility.ToJson(body);
            var payload = Encoding.UTF8.GetBytes(json);

            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var responseText = request.downloadHandler.text;
                    var response = string.IsNullOrWhiteSpace(responseText)
                        ? default
                        : JsonUtility.FromJson<TResponse>(responseText);
                    onSuccess?.Invoke(response, responseText);
                    yield break;
                }

                onError?.Invoke(ApiError.FromRequest(request));
            }
        }
    }
}
