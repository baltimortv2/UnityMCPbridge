const axios = require('axios');
const readline = require('readline');
const http = require('http');

// Configuration
// In Windsurf mcp_config.json, these will be passed via env vars
const API_URL = process.env.MCP_API_URL || 'http://localhost:8787/api';
const API_KEY = process.env.MCP_API_KEY;
const UNITY_LOCAL_URL = process.env.UNITY_LOCAL_URL || 'http://127.0.0.1:8010';
const MODE = process.env.MCP_BRIDGE_MODE || 'stdio'; // 'stdio' or 'http'
const HTTP_PORT = process.env.MCP_HTTP_PORT || 8788;
const TIMEOUT_MS = 30000; // 30 seconds timeout for Unity/API calls

if (!API_KEY) {
  console.error('Error: MCP_API_KEY environment variable is required.');
  process.exit(1);
}

// Axios instance with defaults
const httpClient = axios.create({
    timeout: TIMEOUT_MS,
    validateStatus: status => status >= 200 && status < 500 // Handle 4xx gracefully
});

// Response Sender Abstraction
const sendResponse = (response, resObject) => {
    if (MODE === 'http' && resObject) {
        resObject.writeHead(200, { 'Content-Type': 'application/json' });
        resObject.end(JSON.stringify(response));
    } else {
        console.log(JSON.stringify(response));
    }
};

const sendError = (id, code, message, resObject) => {
    const err = {
        jsonrpc: '2.0',
        id,
        error: { code, message }
    };
    sendResponse(err, resObject);
};

// Request Handler
async function handleRequest(request, resObject = null) {
  const { method, id, params } = request;

  try {
    if (method === 'initialize') {
      const unityRes = await proxyToUnity(request);
      sendResponse(unityRes, resObject);
      return;
    }

    if (method === 'notifications/initialized') {
        await proxyToUnity(request);
        // Notifications don't expect a response, but HTTP might need 200 OK
        if (MODE === 'http' && resObject) {
            resObject.writeHead(200);
            resObject.end();
        }
        return;
    }

    if (method === 'tools/list') {
      try {
        const apiRes = await httpClient.get(`${API_URL}/mcp/commands`, {
          headers: { 'X-API-Key': API_KEY }
        });
        
        const tools = apiRes.data.commands.map(t => ({
          name: t.name,
          description: t.description,
          inputSchema: t.inputSchema || t.parameters
        }));

        sendResponse({
          jsonrpc: '2.0',
          id,
          result: { tools }
        }, resObject);
      } catch (err) {
        console.error('API tools/list error:', err.message);
        sendError(id, -32000, 'Failed to fetch tools list from Monetization Server', resObject);
      }
      return;
    }

    if (method === 'tools/call') {
      const toolName = params.name;
      const toolArgs = params.arguments;

      // 1. Request Execution (Hold Tokens)
      let execData;
      try {
        const execRes = await httpClient.post(`${API_URL}/mcp/execute`, {
            command: toolName,
            args: toolArgs
        }, {
            headers: { 'X-API-Key': API_KEY }
        });
        
        if (execRes.status !== 200) {
             throw { response: execRes };
        }
        execData = execRes.data;
      } catch (err) {
        if (err.response && err.response.status === 402) {
             sendError(id, -32000, `Payment Required: ${err.response.data.message || 'Insufficient tokens'}`, resObject);
             return;
        }
        if (err.response && err.response.status === 403) {
            sendError(id, -32000, `Access Denied: ${err.response.data.message || 'Subscription required'}`, resObject);
            return;
       }
        console.error('API execute error:', err.message);
        sendError(id, -32603, 'Internal Monetization API Error', resObject);
        return;
      }

      const { mcp_command, transaction } = execData;
      const holdId = transaction?.hold_id;

      // 2. Execute on Unity
      let unityResult;
      let success = false;
      let errorMessage = null;

      try {
        const unityRes = await httpClient.post(UNITY_LOCAL_URL, mcp_command);
        
        if (unityRes.data.error) {
            success = false;
            errorMessage = unityRes.data.error.message;
            unityResult = unityRes.data;
        } else {
            success = true;
            unityResult = unityRes.data;
        }
      } catch (err) {
        success = false;
        errorMessage = `Unity Connection Error: ${err.message}`;
        unityResult = {
            jsonrpc: '2.0',
            id: id,
            error: { code: -32000, message: errorMessage }
        };
      }

      // 3. Report Result (Commit/Release)
      if (holdId) {
          try {
            await httpClient.post(`${API_URL}/mcp/report`, {
                hold_id: holdId,
                success: success,
                error_message: errorMessage,
                result: success ? unityResult.result : null
            }, {
                headers: { 'X-API-Key': API_KEY }
            });
          } catch (reportErr) {
              console.error('Failed to report transaction status:', reportErr.message);
          }
      }

      unityResult.id = id; 
      sendResponse(unityResult, resObject);
      return;
    }

    // Default: Forward other methods
    const forwardRes = await proxyToUnity(request);
    sendResponse(forwardRes, resObject);

  } catch (err) {
    console.error('Handler error:', err);
    sendError(id, -32603, `Internal Proxy Error: ${err.message}`, resObject);
  }
}

async function proxyToUnity(requestObj) {
    try {
        const res = await httpClient.post(UNITY_LOCAL_URL, requestObj);
        return res.data;
    } catch (err) {
        throw new Error(`Failed to connect to Unity at ${UNITY_LOCAL_URL}: ${err.message}`);
    }
}

// Start Bridge
if (MODE === 'stdio') {
    const rl = readline.createInterface({
        input: process.stdin,
        output: process.stdout,
        terminal: false
    });

    rl.on('line', async (line) => {
        if (!line.trim()) return;
        try {
            const request = JSON.parse(line);
            await handleRequest(request);
        } catch (err) {
            console.error('Failed to parse JSON input:', err);
        }
    });
    
    // Log startup to stderr so it doesn't interfere with stdout JSON-RPC
    console.error(`MCP Bridge started in STDIO mode`);

} else if (MODE === 'http') {
    const server = http.createServer(async (req, res) => {
        if (req.method !== 'POST') {
            res.writeHead(405);
            res.end('Method Not Allowed');
            return;
        }

        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', async () => {
            try {
                const request = JSON.parse(body);
                await handleRequest(request, res);
            } catch (err) {
                res.writeHead(400);
                res.end('Invalid JSON');
            }
        });
    });

    server.listen(HTTP_PORT, () => {
        console.error(`MCP Bridge started in HTTP mode on port ${HTTP_PORT}`);
    });
} else {
    console.error(`Unknown MODE: ${MODE}`);
    process.exit(1);
}
