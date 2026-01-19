using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using UnityMCP.Monetization;

public class MCPTokenWindow : EditorWindow
{
    private const string PREF_API_KEY = "MCP_ApiKey";
    private const string PREF_API_URL = "MCP_ApiUrl";
    private const string PREF_BRIDGE_PATH = "MCP_BridgePath";
    private const string DEFAULT_API_URL = "http://localhost:8787";

    private string apiKey = "";
    private string apiUrl = DEFAULT_API_URL;
    private string bridgePath = "";
    private bool autoRefresh = true;
    private double lastRefreshTime;
    private const float REFRESH_INTERVAL = 30f;

    private UserInfo currentUserInfo;
    private string errorMessage;
    private bool isLoading;
    private bool isLoggedIn = false;

    private int selectedTab = 0;
    private readonly string[] tabNames = { "Login", "Dashboard", "Config" };

    private int selectedIde = 0;
    private readonly string[] ideNames = { "Windsurf", "Claude Desktop", "Cursor", "Continue", "Other" };

    [MenuItem("Window/MCP Token Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<MCPTokenWindow>("MCP Tokens");
        window.minSize = new Vector2(400, 350);
    }

    private void OnEnable()
    {
        apiKey = EditorPrefs.GetString(PREF_API_KEY, "");
        apiUrl = EditorPrefs.GetString(PREF_API_URL, DEFAULT_API_URL);
        bridgePath = EditorPrefs.GetString(PREF_BRIDGE_PATH, "");
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            isLoggedIn = true;
            selectedTab = 1;
            RefreshData();
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("MCP Token Manager", headerStyle);
        GUILayout.Space(5);

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        GUILayout.Space(10);

        switch (selectedTab)
        {
            case 0: DrawLoginTab(); break;
            case 1: DrawDashboardTab(); break;
            case 2: DrawConfigTab(); break;
        }
    }

    private void DrawLoginTab()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("API Configuration", EditorStyles.boldLabel);
        GUILayout.Space(5);

        apiUrl = EditorGUILayout.TextField("Server URL", apiUrl);
        apiKey = EditorGUILayout.PasswordField("API Key", apiKey);

        GUILayout.Space(10);

        if (GUILayout.Button("Get Key from Telegram", GUILayout.Height(30)))
        {
            Application.OpenURL("https://t.me/UnityMCPBot");
        }

        GUILayout.Space(5);

        GUI.enabled = !string.IsNullOrEmpty(apiKey);
        if (GUILayout.Button("Login", GUILayout.Height(35)))
        {
            EditorPrefs.SetString(PREF_API_KEY, apiKey);
            EditorPrefs.SetString(PREF_API_URL, apiUrl);
            isLoggedIn = true;
            selectedTab = 1;
            RefreshData();
        }
        GUI.enabled = true;

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "1. Open Telegram and find @UnityMCPBot\n" +
            "2. Send /key command to get your API key\n" +
            "3. Paste the key above and click Login",
            MessageType.Info
        );
    }

    private void DrawDashboardTab()
    {
        if (!isLoggedIn || string.IsNullOrEmpty(apiKey))
        {
            EditorGUILayout.HelpBox("Please login first.", MessageType.Warning);
            if (GUILayout.Button("Go to Login")) selectedTab = 0;
            return;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", GUILayout.Width(80))) RefreshData();
        EditorGUILayout.EndHorizontal();

        if (isLoading) { GUILayout.Label("Loading...", EditorStyles.centeredGreyMiniLabel); return; }
        if (!string.IsNullOrEmpty(errorMessage)) { EditorGUILayout.HelpBox(errorMessage, MessageType.Error); return; }
        if (currentUserInfo != null && currentUserInfo.user != null) DrawUserInfo(currentUserInfo);
        else EditorGUILayout.HelpBox("No data. Click Refresh.", MessageType.Info);
    }

    private void DrawConfigTab()
    {
        if (!isLoggedIn || string.IsNullOrEmpty(apiKey))
        {
            EditorGUILayout.HelpBox("Please login first.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("IDE Configuration Generator", EditorStyles.boldLabel);
        GUILayout.Space(5);

        selectedIde = EditorGUILayout.Popup("Select IDE", selectedIde, ideNames);
        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        bridgePath = EditorGUILayout.TextField("Bridge Path", bridgePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select mcp-proxy.js", "", "js");
            if (!string.IsNullOrEmpty(path))
            {
                bridgePath = path;
                EditorPrefs.SetString(PREF_BRIDGE_PATH, bridgePath);
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        if (GUILayout.Button("Generate & Copy Config", GUILayout.Height(35))) GenerateIdeConfig();
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Quick Setup", EditorStyles.boldLabel);
        if (GUILayout.Button("Copy Config for Windsurf")) { selectedIde = 0; GenerateIdeConfig(); }
        if (GUILayout.Button("Copy Config for Cursor (HTTP)")) { selectedIde = 2; GenerateIdeConfig(); }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "After copying, paste the config into your IDE's MCP settings:\n" +
            "- Windsurf: .windsurf/mcp.json\n" +
            "- Claude Desktop: claude_desktop_config.json\n" +
            "- Cursor: Uses HTTP mode on port 8788",
            MessageType.Info
        );
    }

    private void DrawUserInfo(UserInfo info)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label($"User: {info.user.telegram_username}", EditorStyles.largeLabel);
        EditorGUILayout.Space();
        
        GUIStyle balanceStyle = new GUIStyle(EditorStyles.boldLabel);
        balanceStyle.fontSize = 18;
        GUILayout.Label($"Balance: {info.user.token_balance} Tokens", balanceStyle);
        if (info.user.token_reserved > 0)
            GUILayout.Label($"(Reserved: {info.user.token_reserved})", EditorStyles.miniLabel);

        EditorGUILayout.Space();
        GUILayout.Label("Subscription Status:", EditorStyles.boldLabel);
        if (info.subscription != null && !string.IsNullOrEmpty(info.subscription.title))
        {
            EditorGUILayout.LabelField("Plan:", info.subscription.title);
            EditorGUILayout.LabelField("Daily Refill:", $"+{info.subscription.daily_refill}");
            if (DateTime.TryParse(info.subscription.ends_at, out DateTime endDate))
            {
                int daysLeft = (int)(endDate - DateTime.Now).TotalDays;
                EditorGUILayout.LabelField("Ends At:", $"{endDate.ToShortDateString()} ({daysLeft} days)");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No active subscription. Commands are disabled.", MessageType.Warning);
            if (GUILayout.Button("Buy Subscription")) Application.OpenURL("https://t.me/UnityMCPBot");
        }

        EditorGUILayout.Space();
        if (info.stats != null)
        {
            GUILayout.Label("Statistics:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Used Today:", info.stats.used_today.ToString());
            EditorGUILayout.LabelField("Total Commands:", info.stats.total_commands.ToString());
            EditorGUILayout.LabelField("Success Rate:", $"{info.stats.success_rate}%");
            EditorGUILayout.LabelField("Avg Per Day:", info.stats.avg_per_day.ToString("F1"));
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
        if (GUILayout.Button("Logout"))
        {
            apiKey = "";
            EditorPrefs.DeleteKey(PREF_API_KEY);
            isLoggedIn = false;
            currentUserInfo = null;
            selectedTab = 0;
        }
    }

    private void GenerateIdeConfig()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            EditorUtility.DisplayDialog("Error", "Please enter your API Key first.", "OK");
            return;
        }

        string configJson;
        string ideName = ideNames[selectedIde];
        string actualBridgePath = string.IsNullOrEmpty(bridgePath) ? "PATH_TO/ide-bridge/mcp-proxy.js" : bridgePath.Replace("\\", "/");

        if (selectedIde == 2)
        {
            configJson = "{\n  \"mcpServers\": {\n    \"unity-mcp\": {\n      \"url\": \"http://localhost:8788/mcp\"\n    }\n  }\n}";
            EditorUtility.DisplayDialog("Cursor Config", 
                "Config copied!\n\n" +
                "Note: For Cursor, you need to start the bridge in HTTP mode first:\n" +
                $"MCP_API_KEY={apiKey} MCP_MODE=http node {actualBridgePath}\n\n" +
                "The bridge will listen on port 8788.", 
                "OK");
        }
        else
        {
            configJson = $"{{\n  \"mcpServers\": {{\n    \"unity-mcp\": {{\n      \"command\": \"node\",\n      \"args\": [\"{actualBridgePath}\"],\n      \"env\": {{\n        \"MCP_API_KEY\": \"{apiKey}\",\n        \"MCP_API_URL\": \"{apiUrl}/api\",\n        \"UNITY_LOCAL_URL\": \"http://127.0.0.1:8010\"\n      }}\n    }}\n  }}\n}}";
        }
        
        GUIUtility.systemCopyBuffer = configJson;
        
        if (selectedIde != 2)
        {
            string configPath = selectedIde == 0 ? ".windsurf/mcp.json" : 
                               selectedIde == 1 ? "claude_desktop_config.json" : "mcp_config.json";
            EditorUtility.DisplayDialog("Config Generated", 
                $"Configuration for {ideName} copied to clipboard!\n\n" +
                $"Paste into: {configPath}\n\n" +
                (string.IsNullOrEmpty(bridgePath) ? "Don't forget to set the correct bridge path!" : ""),
                "OK");
        }
    }

    private void Update()
    {
        if (autoRefresh && !string.IsNullOrEmpty(apiKey))
        {
            if (EditorApplication.timeSinceStartup - lastRefreshTime > REFRESH_INTERVAL)
                RefreshData();
        }
    }

    private void RefreshData()
    {
        lastRefreshTime = EditorApplication.timeSinceStartup;
        isLoading = true;
        errorMessage = "";

        var client = new MCPTokenClient(apiUrl, apiKey);
        EditorCoroutine.Start(client.GetUserInfo(
            (info) => { currentUserInfo = info; isLoading = false; Repaint(); },
            (error) => { errorMessage = error; isLoading = false; Repaint(); }
        ));
    }

    private static class EditorCoroutine
    {
        public static void Start(System.Collections.IEnumerator routine)
        {
            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                try
                {
                    if (!routine.MoveNext()) EditorApplication.update -= callback;
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