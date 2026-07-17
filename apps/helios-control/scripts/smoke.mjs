import { spawn } from "node:child_process";
import process from "node:process";

const port = 18473;
const child = spawn(process.execPath, ["dist/server.js"], {
  env: { ...process.env, PORT: String(port) },
  stdio: ["ignore", "pipe", "pipe"],
});

const deadline = Date.now() + 8_000;
try {
  let health;
  while (Date.now() < deadline) {
    try {
      health = await fetch(`http://127.0.0.1:${port}/health`).then((response) => response.json());
      break;
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 100));
    }
  }
  if (health?.status !== "ok") throw new Error("Health endpoint did not become ready");

  const response = await fetch(`http://127.0.0.1:${port}/mcp`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
      accept: "application/json, text/event-stream",
    },
    body: JSON.stringify({
      jsonrpc: "2.0",
      id: 1,
      method: "initialize",
      params: {
        protocolVersion: "2025-06-18",
        capabilities: {},
        clientInfo: { name: "helios-smoke", version: "0.1.0" },
      },
    }),
  });
  const body = await response.text();
  if (!response.ok || !body.includes("helios-control")) {
    throw new Error(`MCP initialize failed (${response.status}): ${body}`);
  }
  console.log(JSON.stringify({ health, mcpInitialize: "ok" }));
} finally {
  child.kill("SIGTERM");
}
