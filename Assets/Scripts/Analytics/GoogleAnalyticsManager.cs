using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleAnalyticsManager : MonoBehaviour
{
    [Header("GA4 Settings")]
    [SerializeField] private string _measurementId = ""; // G-XXXXXXXXXX
    [SerializeField] private string _apiSecret = "";     // API Secret
    [SerializeField] private bool _showLog = true;

    [Header("Debug")]
    [Tooltip("チェックを入れると、データ送信の代わりにGoogleの検証サーバーに問い合わせて、エラー理由を表示します。")]
    [SerializeField] private bool _useValidationEndpoint = false;

    private const string ClientIdKey = "GA4_ClientId";
    private const string GaEndPoint = "https://www.google-analytics.com/mp/collect";
    private const string GaDebugEndPoint = "https://www.google-analytics.com/debug/mp/collect";

    private string _sessionId;
    private string _clientId;

    private void Awake()
    {
        InitializeUser();
        SendGameLaunch();
        DontDestroyOnLoad(gameObject);
    }

    private void InitializeUser()
    {
        if (PlayerPrefs.HasKey(ClientIdKey))
        {
            _clientId = PlayerPrefs.GetString(ClientIdKey);
        }
        else
        {
            _clientId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(ClientIdKey, _clientId);
            PlayerPrefs.Save();
        }
        _sessionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }

    public void SendScreenView(string screenName)
    {
        var parameters = new Dictionary<string, object>
        {
            { "screen_name", screenName },
            { "screen_class", screenName }
        };
        SendEvent("screen_view", parameters);
    }

    public void SendGameLaunch()
    {
        SendEvent("game_launch", new Dictionary<string, object>());
    }

    public void SendPieceSolved(string regionName, string stageName, string pieceName, float duration)
    {
        var parameters = new Dictionary<string, object>
        {
            { "region_name", regionName },
            { "stage_name", stageName },
            { "piece_name", pieceName },
            { "duration", duration }
        };
        SendEvent("piece_solved", parameters);
    }

    public void SendStageCleared(string regionName, string stageName, float duration)
    {
        var parameters = new Dictionary<string, object>
        {
            { "region_name", regionName },
            { "stage_name", stageName },
            { "duration", duration }
        };
        SendEvent("stage_cleared", parameters);
    }

    public void SendRegionCleared(string regionName, float duration)
    {
        var parameters = new Dictionary<string, object>
        {
            { "region_name", regionName },
            { "duration", duration }
        };
        SendEvent("region_cleared", parameters);
    }

    private void SendEvent(string eventName, Dictionary<string, object> parameters)
    {
        StringBuilder paramBuilder = new StringBuilder();
        paramBuilder.Append("{");
        paramBuilder.Append($"\"session_id\": \"{_sessionId}\"");
        paramBuilder.Append($",\"engagement_time_msec\": 100");

        if (_useValidationEndpoint)
        {
            paramBuilder.Append($",\"debug_mode\": true");
        }

        foreach (var param in parameters)
        {
            paramBuilder.Append(",");
            if (param.Value is string strVal)
                paramBuilder.Append($"\"{param.Key}\": \"{strVal}\"");
            else
                paramBuilder.Append($"\"{param.Key}\": {param.Value}");
        }
        paramBuilder.Append("}");

        string jsonPayload = $@"
        {{
            ""client_id"": ""{_clientId}"",
            ""events"": [
                {{
                    ""name"": ""{eventName}"",
                    ""params"": {paramBuilder}
                }}
            ]
        }}";

        if (_showLog)
        {
            Debug.Log($"[GA4] Sending Event: {eventName}\nPayload:\n{jsonPayload}");
        }

        PostRequest(jsonPayload).Forget();
    }

    private async UniTaskVoid PostRequest(string jsonPayload)
    {
        if (string.IsNullOrEmpty(_measurementId))
        {
            Debug.LogError("[GA4] Error: Measurement ID is empty!");
            return;
        }

        string baseUrl = _useValidationEndpoint ? GaDebugEndPoint : GaEndPoint;
        string url = $"{baseUrl}?measurement_id={_measurementId}&api_secret={_apiSecret}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            // request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Content-Type", "text/plain");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GA4] Network Error: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                if (_useValidationEndpoint)
                {
                    Debug.Log($"[GA4 Validation Result] Response:\n{request.downloadHandler.text}");
                }
                else if (_showLog)
                {
                    Debug.Log($"[GA4] Send Success! (Code: {request.responseCode})");
                }
            }
        }
    }
}