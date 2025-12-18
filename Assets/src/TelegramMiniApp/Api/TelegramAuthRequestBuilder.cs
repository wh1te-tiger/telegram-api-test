using System;
using System.Collections.Generic;
using UnityEngine;

namespace TelegramMiniApp.Api
{
    public static class TelegramAuthRequestBuilder
    {
        public static bool TryBuildRegisterRequest(string payloadJson, out TelegramAuthRegisterRequest request, string connectionType = "telegram")
        {
            request = BuildRegisterRequest(payloadJson, connectionType);
            return request != null;
        }

        public static bool TryBuildLoginRequest(string payloadJson, out TelegramAuthLoginRequest request, string connectionType = "telegram")
        {
            request = BuildLoginRequest(payloadJson, connectionType);
            return request != null;
        }

        public static TelegramAuthRegisterRequest BuildRegisterRequest(string payloadJson, string connectionType = "telegram")
        {
            var session = BuildSession(payloadJson);
            if (session == null)
                return null;

            return new TelegramAuthRegisterRequest
            {
                telegramSessionDto = session,
                connectionData = new ConnectionData { connectionType = connectionType }
            };
        }

        public static TelegramAuthLoginRequest BuildLoginRequest(string payloadJson, string connectionType = "telegram")
        {
            var session = BuildSession(payloadJson);
            if (session == null)
                return null;

            return new TelegramAuthLoginRequest
            {
                telegramSessionDto = session,
                connectionData = new ConnectionData { connectionType = connectionType }
            };
        }

        private static TelegramSessionDto BuildSession(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
                return null;

            var payload = JsonUtility.FromJson<TelegramWebAppPayload>(payloadJson);
            if (payload == null || payload.user == null)
                return null;

            var query = ParseQueryString(payload.initData);
            var authDate = GetLong(query, "auth_date");
            var hash = GetString(query, "hash");

            var session = new TelegramSessionDto
            {
                id = payload.user.id,
                firstName = payload.user.first_name,
                lastName = payload.user.last_name,
                username = payload.user.username,
                photoUrl = payload.user.photo_url,
                authDate = authDate,
                hash = hash
            };
            return session;
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query))
                return result;

            var trimmed = query.TrimStart('?');
            var pairs = trimmed.Split('&');
            foreach (var pair in pairs)
            {
                if (string.IsNullOrWhiteSpace(pair))
                    continue;

                var idx = pair.IndexOf('=');
                if (idx <= 0)
                    continue;

                var key = UrlDecode(pair.Substring(0, idx));
                var value = UrlDecode(pair.Substring(idx + 1));
                result[key] = value;
            }

            return result;
        }

        private static string UrlDecode(string value)
        {
            if (value == null)
                return string.Empty;
            return Uri.UnescapeDataString(value.Replace("+", "%20"));
        }

        private static long GetLong(Dictionary<string, string> data, string key)
        {
            if (data.TryGetValue(key, out var value) && long.TryParse(value, out var parsed))
                return parsed;
            return 0;
        }

        private static string GetString(Dictionary<string, string> data, string key)
        {
            return data.TryGetValue(key, out var value) ? value : string.Empty;
        }

        [Serializable]
        private class TelegramWebAppPayload
        {
            public string initData;
            public TelegramWebAppUser user;
        }

        [Serializable]
        private class TelegramWebAppUser
        {
            public long id;
            public string first_name;
            public string last_name;
            public string username;
            public string photo_url;
        }
    }
}
