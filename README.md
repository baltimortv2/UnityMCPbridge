# Unity MCP Bridge

[![npm version](https://badge.fury.io/js/com.unitymcp.monetization.svg)](https://badge.fury.io/js/com.unitymcp.monetization)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black.svg)](https://unity.com/)

The official client package for the **Unity MCP Monetization System**. 
This package bridges the gap between your AI Agent (via Model Context Protocol) and the Unity Editor, while enforcing token-based access control.

## 📦 Contents

*   **Editor Window**: `MCPTokenWindow` for managing API keys and viewing balance/stats.
*   **IDE Bridge**: A Node.js proxy (`ide-bridge/mcp-proxy.js`) that intercepts MCP calls and communicates with the Monetization Server.
*   **Runtime Client**: `MCPTokenClient` for accessing user info at runtime (useful for in-game debug tools).

## 🔧 Installation

### Option 1: Copy to Project
1.  Download the latest release.
2.  Copy the `UnityMCPBridge` folder into your Unity project's `Assets` folder (or `Packages` if using UPM format).

### Option 2: Git URL (UPM)
Add the following to your `Packages/manifest.json`:
```json
"com.unitymcp.monetization": "https://github.com/baltimortv2/UnityMCPbridge.git"
```

## ⚙️ Configuration

1.  **Open the Manager**:
    In Unity, go to menu: **Tools > MCP Token Manager**.

2.  **Enter API Key**:
    *   Don't have a key? Click **Get Key (Telegram Bot)**.
    *   Follow the bot instructions (`/start` -> `/key`).
    *   Paste the key into the window and click **Refresh Data**.

3.  **Setup AI Agent**:
    *   Click **Generate IDE Config**.
    *   This copies a JSON configuration to your clipboard.
    *   **Windsurf**: Paste into `~/.codeium/windsurf/mcp_config.json`.
    *   **Cursor**: Paste into your MCP settings.

4.  **Install Bridge Dependencies**:
    *   Navigate to the `ide-bridge` folder inside the package.
    *   Run `npm install` to install the required Node.js modules for the proxy.

## 🖥 Usage

### In Editor
The **MCP Token Manager** window shows your:
*   💎 **Balance**: Available tokens.
*   📊 **Statistics**: Commands used today, average usage, and subscription remaining days.
*   🚦 **Status**: Active subscription plan.

### In IDE (Windsurf/Cursor)
Just use the Unity tools as normal (e.g., "Create a cube", "Find script").
*   The **Bridge** intercepts the call.
*   It asks the **Server** to hold tokens.
*   If successful, the command executes in Unity.
*   The result is returned to the IDE.

## 🐛 Troubleshooting

*   **"Payment Required"**: Your balance is low. Use the Telegram Bot (`/buy`) to top up.
*   **"Subscription Required"**: Your subscription expired. Renew via the Bot.
*   **Connection Error**: Ensure the Monetization Server is running and the URL in settings is correct (default: `http://localhost:8787`).

## 📜 License

MIT License.