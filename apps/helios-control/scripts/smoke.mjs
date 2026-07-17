import { spawn } from "node:child_process";
import { readFile } from "node:fs/promises";
import process from "node:process";

const port = 18473;
const child = spawn(process.execPath, ["dist/server.js"], {
  env: { ...process.env, PORT: String(port) },
  stdio: ["ignore", "pipe", "pipe"],
});

const deadline = Date.now() + 8_000;
try {
  const widget = await readFile(new URL("../public/control-center.html", import.meta.url), "utf8");
  if (widget.includes(".innerHTML") || !widget.includes("safeHttpsUrl")) {
    throw new Error("Widget must use safe DOM rendering and HTTPS-only links");
  }
  const serverSource = await readFile(new URL("../src/server.ts", import.meta.url), "utf8");
  for (const canonicalTarget of ["C0BHWDBHG1W", "C0B8PU2RGLF", "Helios Integration Fabric", "JOH-35", "helios-azure-cli"]) {
    if (!serverSource.includes(canonicalTarget) && !process.env.HELIOS_LINEAR_ISSUE_URL?.includes(canonicalTarget)) {
      throw new Error(`Missing canonical integration target: ${canonicalTarget}`);
    }
  }
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
