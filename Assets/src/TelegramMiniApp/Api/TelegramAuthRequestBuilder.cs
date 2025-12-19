using System;
using System.Collections.Generic;
using UnityEngine;

namespace TelegramMiniApp.Api
{
    public static class TelegramAuthRequestBuilder
    {
        private const string DefaultConnectionType = "TelegramMiniAppConnectionData";

        public static bool TryBuildRegisterRequest(string payloadJson, out TelegramAuthRegisterRequest request, string connectionType = DefaultConnectionType)
        {
            request = BuildRegisterRequest(payloadJson, null, connectionType);
            return request != null;
        }

        public static bool TryBuildRegisterRequest(string payloadJson, string userAgent, out TelegramAuthRegisterRequest request, string connectionType = DefaultConnectionType)
        {
            request = BuildRegisterRequest(payloadJson, userAgent, connectionType);
            return request != null;
        }

        public static bool TryBuildLoginRequest(string payloadJson, out TelegramAuthLoginRequest request, string connectionType = DefaultConnectionType)
        {
            request = BuildLoginRequest(payloadJson, null, connectionType);
            return request != null;
        }

        public static bool TryBuildLoginRequest(string payloadJson, string userAgent, out TelegramAuthLoginRequest request, string connectionType = DefaultConnectionType)
        {
            request = BuildLoginRequest(payloadJson, userAgent, connectionType);
            return request != null;
        }

        public static TelegramAuthRegisterRequest BuildRegisterRequest(string payloadJson, string connectionType = DefaultConnectionType)
        {
            return BuildRegisterRequest(payloadJson, null, connectionType);
        }

        public static TelegramAuthRegisterRequest BuildRegisterRequest(string payloadJson, string userAgent, string connectionType = DefaultConnectionType)
        {
            var session = BuildSession(payloadJson);
            if (session == null)
                return null;

            return new TelegramAuthRegisterRequest
            {
                telegramSessionDto = session,
                connectionData = BuildConnectionData(userAgent, connectionType)
            };
        }

        public static TelegramAuthLoginRequest BuildLoginRequest(string payloadJson, string connectionType = DefaultConnectionType)
        {
            return BuildLoginRequest(payloadJson, null, connectionType);
        }

        public static TelegramAuthLoginRequest BuildLoginRequest(string payloadJson, string userAgent, string connectionType = DefaultConnectionType)
        {
            var session = BuildSession(payloadJson);
            if (session == null)
                return null;

            return new TelegramAuthLoginRequest
            {
                telegramSessionDto = session,
                connectionData = BuildConnectionData(userAgent, connectionType)
            };
        }

        private static ConnectionData BuildConnectionData(string userAgent, string connectionType)
        {
            var data = new ConnectionData
            {
                connectionType = connectionType ?? string.Empty,
                userAgent = userAgent ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(userAgent))
                return data;

            FillFromUserAgent(data, userAgent);
            return data;
        }

        private static void FillFromUserAgent(ConnectionData data, string userAgent)
        {
            if (data == null || string.IsNullOrWhiteSpace(userAgent))
                return;

            const string marker = "Telegram-Android/";
            var markerIndex = userAgent.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                return;

            var appVersionStart = markerIndex + marker.Length;
            var openParen = userAgent.IndexOf('(', appVersionStart);
            var appVersionEnd = userAgent.IndexOf(' ', appVersionStart);
            if (openParen >= 0 && (appVersionEnd < 0 || openParen < appVersionEnd))
                appVersionEnd = openParen;
            if (appVersionEnd < 0)
                appVersionEnd = userAgent.Length;

            data.appVersion = userAgent.Substring(appVersionStart, Math.Max(0, appVersionEnd - appVersionStart)).Trim();

            if (openParen < 0)
                return;

            var closeParen = userAgent.IndexOf(')', openParen + 1);
            if (closeParen < 0)
                closeParen = userAgent.Length;

            var inside = userAgent.Substring(openParen + 1, Math.Max(0, closeParen - openParen - 1));
            var parts = inside.Split(';');
            if (parts.Length > 0)
                ParseDevicePart(data, parts[0]);
            if (parts.Length > 1)
                data.androidVersion = TrimPrefix(parts[1], "Android");
            if (parts.Length > 2)
                data.sdkVersion = TrimPrefix(parts[2], "SDK");
            if (parts.Length > 3)
                data.performanceClass = parts[3].Trim();
        }

        private static void ParseDevicePart(ConnectionData data, string devicePart)
        {
            if (data == null || string.IsNullOrWhiteSpace(devicePart))
                return;

            var trimmed = devicePart.Trim();
            var split = trimmed.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 0)
                data.manufacturer = split[0];
            if (split.Length > 1)
                data.model = split[1];
        }

        private static string TrimPrefix(string value, string prefix)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var trimmed = value.Trim();
            if (!string.IsNullOrEmpty(prefix) && trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return trimmed.Substring(prefix.Length).Trim();

            return trimmed;
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
