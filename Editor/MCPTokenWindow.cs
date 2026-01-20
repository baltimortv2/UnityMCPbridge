using UnityEngine;
using UnityEditor;
using System;
using UnityMCP.Monetization;

public class MCPTokenWindow : EditorWindow
{
    private const string PREF_API_KEY = "MCP_ApiKey";
    private const string PREF_API_URL = "MCP_ApiUrl";
    private const string DEFAULT_API_URL = "http://localhost:8787";

    private string apiKey = "";
    private string apiUrl = DEFAULT_API_URL;
    private bool autoRefresh = true;
    private double lastRefreshTime;
    private const float REFRESH_INTERVAL = 30f;

    private UserInfo currentUserInfo;
    private string errorMessage;
    private bool isLoading;

    [MenuItem("Tools/MCP Token Manager")]
    public static void ShowWindow()
    {
        GetWindow<MCPTokenWindow>("MCP Tokens");
    }

    private void OnEnable()
    {
        apiKey = EditorPrefs.GetString(PREF_API_KEY, "");
        apiUrl = EditorPrefs.GetString(PREF_API_URL, DEFAULT_API_URL);
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            RefreshData();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("MCP Monetization Settings", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // Settings Section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Configuration", EditorStyles.boldLabel);
        
        string newUrl = EditorGUILayout.TextField("Server URL", apiUrl);
        if (newUrl != apiUrl)
        {
            apiUrl = newUrl;
            EditorPrefs.SetString(PREF_API_URL, apiUrl);
        }

        string newKey = EditorGUILayout.TextField("API Key", apiKey);
        if (newKey != apiKey)
        {
            apiKey = newKey;
            EditorPrefs.SetString(PREF_API_KEY, apiKey);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Get Key (Telegram Bot)"))
        {
            Application.OpenURL("https://t.me/UnityMCPBot");
        }
        if (GUILayout.Button("Refresh Data"))
        {
            RefreshData();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate IDE Config (Windsurf/Claude)"))
        {
            GenerateIdeConfig();
        }
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Status Section
        if (isLoading)
        {
            GUILayout.Label("Loading...", EditorStyles.centeredGreyMiniLabel);
        }
        else if (!string.IsNullOrEmpty(errorMessage))
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
        }
        else if (currentUserInfo != null && currentUserInfo.user != null)
        {
            DrawUserInfo(currentUserInfo);
        }
        else
        {
            EditorGUILayout.HelpBox("Please enter a valid API Key and refresh.", MessageType.Info);
        }
    }

    private void DrawUserInfo(UserInfo info)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label($"👤 User: {info.user.telegram_username}", EditorStyles.largeLabel);
        
        EditorGUILayout.Space();
        
        // Balance
        GUIStyle balanceStyle = new GUIStyle(EditorStyles.boldLabel);
        balanceStyle.fontSize = 18;
        GUILayout.Label($"💎 Balance: {info.user.token_balance} Tokens", balanceStyle);
        if (info.user.token_reserved > 0)
        {
            GUILayout.Label($"(Reserved: {info.user.token_reserved})", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // Subscription
        GUILayout.Label("Subscription Status:", EditorStyles.boldLabel);
        if (info.subscription != null && !string.IsNullOrEmpty(info.subscription.title))
        {
            EditorGUILayout.LabelField("Plan:", info.subscription.title);
            EditorGUILayout.LabelField("Daily Refill:", $"+{info.subscription.daily_refill}");
            
            // Parse date if possible
            string endDateStr = info.subscription.ends_at;
            if (DateTime.TryParse(endDateStr, out DateTime endDate))
            {
                 EditorGUILayout.LabelField("Ends At:", endDate.ToShortDateString());
            }
            else
            {
                 EditorGUILayout.LabelField("Ends At:", endDateStr);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No active subscription. Commands are disabled.", MessageType.Warning);
            if (GUILayout.Button("Buy Subscription"))
            {
                Application.OpenURL("https://t.me/UnityMCPBot");
            }
        }

        EditorGUILayout.Space();
        
        // Stats
        if (info.stats != null)
        {
            GUILayout.Label("Statistics:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Used Today:", $"{info.stats.used_today} commands");
            EditorGUILayout.LabelField("Total Usage:", $"{info.stats.total_commands} commands");
            EditorGUILayout.LabelField("Avg Daily:", $"{info.stats.avg_per_day:F1}");
            if (info.stats.days_remaining > 0)
            {
                 EditorGUILayout.LabelField("Days Remaining:", $"{info.stats.days_remaining} days");
            }
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndVertical();
    }

    private void GenerateIdeConfig()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            EditorUtility.DisplayDialog("Error", "Please enter your API Key first.", "OK");
            return;
        }

        string configJson = $@"{{
  ""mcpServers"": {{
    ""unity-mcp"": {{
      ""command"": ""node"",
      ""args"": [""PATH_TO_YOUR/ide-bridge/mcp-proxy.js""],
      ""env"": {{
        ""MCP_API_KEY"": ""{apiKey}"",
        ""MCP_API_URL"": ""{apiUrl}/api"",
        ""UNITY_LOCAL_URL"": ""http://127.0.0.1:8010""
      }}
    }}
  }}
}}";
        
        // Copy to clipboard
        GUIUtility.systemCopyBuffer = configJson;
        EditorUtility.DisplayDialog("Config Generated", 
            "Configuration JSON copied to clipboard!\n\n" +
            "Paste this into your IDE's MCP config file (e.g. mcp_config.json).\n" +
            "Don't forget to replace 'PATH_TO_YOUR' with the actual path to 'ide-bridge/mcp-proxy.js'.", 
            "OK");
    }

    private void Update()
    {
        if (autoRefresh && !string.IsNullOrEmpty(apiKey))
        {
            if (EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshData();
            }
        }
    }

    private void RefreshData()
    {
        lastRefreshTime = EditorApplication.timeSinceStartup;
        isLoading = true;
        errorMessage = "";

        var client = new MCPTokenClient(apiUrl, apiKey);
        EditorCoroutine.Start(client.GetUserInfo(
            (info) => {
                currentUserInfo = info;
                isLoading = false;
                Repaint();
            },
            (error) => {
                errorMessage = error;
                isLoading = false;
                Repaint();
            }
        ));
    }

    // Simple coroutine runner for Editor
    private static class EditorCoroutine
    {
        public static void Start(IEnumerator routine)
        {
            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                try
                {
                    if (!routine.MoveNext())
                    {
                        EditorApplication.update -= callback;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("EditorCoroutine Error: " + ex);
                    EditorApplication.update -= callback;
                }
            };
            EditorApplication.update += callback;
        }
    }
}
