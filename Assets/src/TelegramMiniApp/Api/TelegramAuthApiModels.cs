using System;
using UnityEngine;
using UnityEngine.Networking;

namespace TelegramMiniApp.Api
{
    [Serializable]
    public class ConnectionData
    {
        public string connectionType;
    }

    [Serializable]
    public class TelegramSessionDto
    {
        public long id;
        public string firstName;
        public string lastName;
        public string username;
        public string photoUrl;
        public long authDate;
        public string hash;
    }

    [Serializable]
    public class TelegramAuthRegisterRequest
    {
        public TelegramSessionDto telegramSessionDto;
        public ConnectionData connectionData;
    }

    [Serializable]
    public class TelegramAuthRegisterResponse
    {
        public UserDto user;
    }

    [Serializable]
    public class TelegramAuthLoginRequest
    {
        public TelegramSessionDto telegramSessionDto;
        public ConnectionData connectionData;
    }

    [Serializable]
    public class TelegramAuthLoginResponse
    {
        public string accessToken;
        public string sessionId;
        public string expiresAt;
        public string tokenExpiresAt;
        public UserDto user;
    }

    [Serializable]
    public class UserDto
    {
        public string displayName;
        public int avatarId;
        public PlayerDto player;
    }

    [Serializable]
    public class PlayerDto
    {
        public int id;
        public double money;
        public int rating;
    }

    [Serializable]
    public class ErrorResponse
    {
        public ErrorResponseEntry[] errors;
    }

    [Serializable]
    public class ErrorResponseEntry
    {
        public string errorCode;
        public string[] messages;
    }

    [Serializable]
    public class ProblemDetails
    {
        public string type;
        public string title;
        public int status;
        public string detail;
        public string instance;
    }

    [Serializable]
    public class ApiError
    {
        public long statusCode;
        public string networkError;
        public string rawBody;
        public ErrorResponse errorResponse;
        public ProblemDetails problemDetails;

        public static ApiError FromRequest(UnityWebRequest request)
        {
            var error = new ApiError
            {
                statusCode = request.responseCode,
                networkError = request.error,
                rawBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty
            };

            if (!string.IsNullOrEmpty(error.rawBody))
            {
                error.errorResponse = TryParse<ErrorResponse>(error.rawBody);
                error.problemDetails = TryParse<ProblemDetails>(error.rawBody);
            }

            return error;
        }

        public string ToSummary()
        {
            if (problemDetails != null && !string.IsNullOrEmpty(problemDetails.title))
                return problemDetails.title;

            if (errorResponse != null && errorResponse.errors != null && errorResponse.errors.Length > 0)
            {
                var entry = errorResponse.errors[0];
                if (entry != null && entry.messages != null && entry.messages.Length > 0)
                    return entry.messages[0];
            }

            if (!string.IsNullOrEmpty(networkError))
                return networkError;

            return $"HTTP {statusCode}";
        }

        private static T TryParse<T>(string json) where T : class
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
