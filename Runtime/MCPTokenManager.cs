using UnityEngine;
using System;
using System.Collections;

namespace UnityMCP.Monetization
{
    /// <summary>
    /// Singleton manager for accessing Token API at runtime.
    /// Useful for in-game debug consoles or runtime UI.
    /// </summary>
    public class MCPTokenManager : MonoBehaviour
    {
        public static MCPTokenManager Instance { get; private set; }

        [SerializeField] private string apiUrl = "http://localhost:8787";
        [SerializeField] private string apiKey = "";
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private float refreshInterval = 60f;

        public UserInfo CurrentUserInfo { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(apiKey);

        private MCPTokenClient client;
        private Coroutine refreshCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeClient();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeClient()
        {
            // Load from EditorPrefs if in Editor and keys are empty
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(apiKey))
                apiKey = UnityEditor.EditorPrefs.GetString("MCP_ApiKey", "");
            if (string.IsNullOrEmpty(apiUrl))
                apiUrl = UnityEditor.EditorPrefs.GetString("MCP_ApiUrl", "http://localhost:8787");
#endif

            client = new MCPTokenClient(apiUrl, apiKey);
            if (autoRefresh && IsLoggedIn)
            {
                StartAutoRefresh();
            }
        }

        public void SetCredentials(string url, string key)
        {
            this.apiUrl = url;
            this.apiKey = key;
            client = new MCPTokenClient(url, key);
            
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetString("MCP_ApiKey", key);
            UnityEditor.EditorPrefs.SetString("MCP_ApiUrl", url);
#endif
            
            RefreshData();
        }

        public void RefreshData(Action<UserInfo> onSuccess = null, Action<string> onError = null)
        {
            if (!IsLoggedIn) return;

            StartCoroutine(client.GetUserInfo(
                (info) => {
                    CurrentUserInfo = info;
                    onSuccess?.Invoke(info);
                },
                (error) => {
                    Debug.LogError($"[MCPTokenManager] Error: {error}");
                    onError?.Invoke(error);
                }
            ));
        }

        private void StartAutoRefresh()
        {
            if (refreshCoroutine != null) StopCoroutine(refreshCoroutine);
            refreshCoroutine = StartCoroutine(AutoRefreshRoutine());
        }

        private IEnumerator AutoRefreshRoutine()
        {
            while (autoRefresh)
            {
                RefreshData();
                yield return new WaitForSeconds(refreshInterval);
            }
        }
    }
}
