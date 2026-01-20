using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityMCP.Monetization
{
    [Serializable]
    public class UserInfo
    {
        public bool success;
        public UserData user;
        public SubscriptionData subscription;
        public StatsData stats;
    }

    [Serializable]
    public class UserData
    {
        public string telegram_username;
        public int token_balance;
        public int token_reserved;
        public bool is_blocked;
    }

    [Serializable]
    public class SubscriptionData
    {
        public string title;
        public int daily_refill;
        public string ends_at;
    }

    [Serializable]
    public class StatsData
    {
        public int used_today;
        public int total_commands;
        public float avg_per_day;
        public int days_remaining;
    }

    public class MCPTokenClient
    {
        private string apiUrl;
        private string apiKey;

        public MCPTokenClient(string url, string key)
        {
            this.apiUrl = url.TrimEnd('/');
            this.apiKey = key;
        }

        public IEnumerator GetUserInfo(Action<UserInfo> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(apiUrl + "/api/user/info"))
            {
                request.SetRequestHeader("X-API-Key", apiKey);
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    onError?.Invoke(request.error + ": " + request.downloadHandler.text);
                }
                else
                {
                    try
                    {
                        UserInfo info = JsonUtility.FromJson<UserInfo>(request.downloadHandler.text);
                        onSuccess?.Invoke(info);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke("JSON Parse Error: " + ex.Message);
                    }
                }
            }
        }
    }
}
