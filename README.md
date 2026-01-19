# Unity MCP Token Manager

Unity Editor package for managing MCP monetization - API keys, tokens, and subscriptions.

## Installation

### Via Unity Package Manager (Git URL)

1. Open Unity → Window → Package Manager
2. Click **+** → **Add package from git URL**
3. Enter: `https://github.com/baltimortv2/UnityMCPbridge.git`
4. Click **Add**

### Manual Installation

1. Download the latest release from [Releases](https://github.com/baltimortv2/UnityMCPbridge/releases)
2. Extract to your project's `Packages/` folder

## Setup

1. Get your API key from Telegram: [@UnityMCPBot](https://t.me/UnityMCPBot)
   - Send `/start` to register
   - Send `/key` to generate API key
   - Send `/buy` to purchase subscription

2. Open Unity → **Window** → **MCP Token Manager**

3. In the **Login** tab:
   - Paste your API key
   - Click **Login**

4. In the **Config** tab:
   - Select your IDE (Windsurf, Cursor, Claude Desktop)
   - Set the path to `mcp-proxy.js`
   - Click **Generate & Copy Config**

5. Paste the config into your IDE's MCP settings

## Features

### Login Tab
- Enter API key and server URL
- Secure storage in EditorPrefs

### Dashboard Tab
- View current token balance
- Subscription status and expiration
- Usage statistics (commands today, total, success rate)
- Auto-refresh every 30 seconds

### Config Tab
- Generate IDE-specific MCP configuration
- Support for:
  - **Windsurf** (STDIO mode)
  - **Claude Desktop** (STDIO mode)
  - **Cursor** (HTTP mode on port 8788)
  - **Continue** (STDIO mode)

## Requirements

- Unity 2021.3 or higher
- Active internet connection
- Valid API key from Telegram bot

## Subscription Plans

| Plan | Price | Tokens | Daily Refill |
|------|-------|--------|--------------|
| Starter | 149₽ | 1,000 | +50/day |
| Pro | 599₽ | 5,000 | +150/day |
| Max | 999₽ | 10,000 | +300/day |

All subscriptions are for 30 days.

## Token Usage

- **Free**: `initialize`, `tools/list`, `prompts/list`, `resources/list`
- **1 Token**: Each successful `tools/call` command
- **0 Tokens**: Failed commands (token refunded)

## Support

- Telegram Bot: [@UnityMCPBot](https://t.me/UnityMCPBot)
- Commands: `/help`, `/balance`, `/history`

## License

MIT License