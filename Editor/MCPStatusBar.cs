using UnityEngine;
using UnityEditor;
using UnityMCP.Monetization;

[InitializeOnLoad]
public class MCPStatusBar
{
    private static string statusText = "";
    private static double lastUpdate;
    private const float UPDATE_INTERVAL = 60f; // Update every minute
    private static bool isUpdating = false;

    static MCPStatusBar()
    {
        // Subscribe to GUI events in SceneView
        SceneView.duringSceneGui += OnSceneGUI;
        // Check for updates
        EditorApplication.update += OnUpdate;
        
        // Initial fetch
        UpdateBalance();
    }

    private static void OnUpdate()
    {
        if (EditorApplication.timeSinceStartup - lastUpdate > UPDATE_INTERVAL)
        {
            UpdateBalance();
        }
    }

    private static void UpdateBalance()
    {
        string apiKey = EditorPrefs.GetString("MCP_ApiKey", "");
        string apiUrl = EditorPrefs.GetString("MCP_ApiUrl", "http://localhost:8787");

        if (string.IsNullOrEmpty(apiKey))
        {
            statusText = "MCP: No API Key";
            return;
        }

        if (isUpdating) return;
        isUpdating = true;

        var client = new MCPTokenClient(apiUrl, apiKey);
        
        // We need a runner for coroutine in Editor. 
        // We can reuse the one from MCPTokenWindow or create a simple one here.
        EditorCoroutine.Start(client.GetUserInfo(
            (info) => {
                statusText = $"💎 MCP: {info.user.token_balance}";
                if (info.user.token_reserved > 0) statusText += $" ({info.user.token_reserved})";
                lastUpdate = EditorApplication.timeSinceStartup;
                isUpdating = false;
                SceneView.RepaintAll();
            },
            (err) => {
                statusText = "MCP: Connection Error";
                isUpdating = false;
            }
        ));
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();
        
        var style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.LowerRight;

        // Draw in bottom right corner
        float padding = 10f;
        float width = 200f;
        float height = 20f;
        Rect rect = new Rect(sceneView.position.width - width - padding, sceneView.position.height - height - padding - 20, width, height);

        // Background
        GUI.Box(rect, GUIContent.none, GUI.skin.box);
        GUI.Label(rect, statusText, style);

        Handles.EndGUI();
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
                    if (!routine.MoveNext())
                    {
                        EditorApplication.update -= callback;
                    }
                }
                catch
                {
                    EditorApplication.update -= callback;
                }
            };
            EditorApplication.update += callback;
        }
    }
}
