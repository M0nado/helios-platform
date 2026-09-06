import {
  registerAppResource,
  registerAppTool,
  RESOURCE_MIME_TYPE,
} from "@modelcontextprotocol/ext-apps/server";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import cors from "cors";
import express from "express";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { z } from "zod";

const VERSION = "0.1.0";
const CONTROL_URI = "ui://helios/control-center-v1.html";
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const widgetHtml = fs.readFileSync(path.join(root, "public", "control-center.html"), "utf8");

type SystemRecord = {
  id: string;
  name: string;
  purpose: string;
  url: string | null;
  state: "connected" | "configured" | "needs_configuration" | "blocked";
  nextAction: string;
};

function present(name: string): boolean {
  return Boolean(process.env[name]?.trim());
}

function systems(): SystemRecord[] {
  const deployEnabled = process.env.AZURE_DEPLOY_ENABLED === "true";
  return [
    {
      id: "github",
      name: "GitHub",
      purpose: "Canonical code, pull requests, CI, and deployment gates",
      url: process.env.HELIOS_GITHUB_URL ?? "https://github.com/M0nado/helios-platform",
      state: "connected",
      nextAction: "Clear PR #174 legacy workflow reds, complete review/tests, merge, then rebase Azure PR #176.",
    },
    {
      id: "azure",
      name: "Azure",
      purpose: "Container Apps runtime, Entra identity, Key Vault, ACR, and telemetry",
      url: null,
      state: ["AZURE_TENANT_ID", "AZURE_SUBSCRIPTION_ID", "AZURE_CLIENT_ID", "AZURE_RESOURCE_GROUP"].every(present)
        ? deployEnabled ? "configured" : "blocked"
        : "needs_configuration",
      nextAction: deployEnabled
        ? "Use the protected azure-dev GitHub environment."
        : "Keep deploy disabled: repairs are on PR #174 but still need review/merge; then rebase and harden PR #176.",
    },
    {
      id: "azure-cli",
      name: "HELIOS Azure CLI",
      purpose: "Guarded local doctor, login, OIDC planning, Bicep what-if, and explicitly approved deployment",
      url: "https://github.com/M0nado/helios-platform/tree/integration/helios-chatgpt-copilot-app/plugins/helios-azure-cli",
      state: "configured",
      nextAction: "Run doctor and plan first. Mutating commands remain blocked until PRs #174 and #176 are merged.",
    },
    ...[
      ["sharepoint", "SharePoint", "Governed architecture and runbooks", "HELIOS_SHAREPOINT_URL", "https://heli0s-my.sharepoint.com/personal/jmore_heli0s_onmicrosoft_com/Documents/Helios/Governance/Architecture/Integration-Fabric"],
      ["slack", "Slack #helios-control-plane", "Engineering authority, operations, and alerts", "HELIOS_SLACK_URL", "https://helios-xk97943.slack.com/archives/C0BHWDBHG1W"],
      ["slack-all", "Slack #all-helios", "Company-wide HELIOS availability announcements", "HELIOS_SLACK_ANNOUNCEMENTS_URL", "https://helios-xk97943.slack.com/archives/C0B8PU2RGLF"],
      ["teams", "Microsoft Teams", "Microsoft operations and Copilot-facing coordination", "HELIOS_TEAMS_URL", "https://teams.microsoft.com/l/message/19%3Ad860092598794564808e4d58f87a47d4%40thread.tacv2/1784052543434?groupId=63311034-cc8f-4a5f-bb8e-459342f0575c&tenantId=349e1399-dccf-45b1-af7e-05d7b0676abf&createdTime=1784052543434&parentMessageId=1784052543434"],
      ["linear", "Linear — Helios Integration Fabric", "Milestones, blockers, ownership, and implementation issue JOH-35", "HELIOS_LINEAR_URL", "https://linear.app/641974/project/helios-integration-fabric-40e1dd9caa39"],
    ].map(([id, name, purpose, env, fallback]): SystemRecord => ({
      id,
      name,
      purpose,
      url: process.env[env] || fallback,
      state: "connected",
      nextAction: "Keep the canonical setup link synchronized.",
    })),
    {
      id: "copilot",
      name: "Microsoft Copilot Studio",
      purpose: "Second governed host for the same HELIOS MCP tools",
      url: null,
      state: present("PUBLIC_BASE_URL") ? "configured" : "needs_configuration",
      nextAction: "Add the deployed /mcp URL with the Copilot Studio MCP onboarding wizard.",
    },
  ];
}

function createServer(): McpServer {
  const server = new McpServer({ name: "helios-control", version: VERSION });

  registerAppTool(server, "search", {
    title: "Search HELIOS systems",
    description: "Use this when the user needs to find a HELIOS setup surface, integration, owner path, or next action across GitHub, Azure, SharePoint, Slack, Teams, Linear, and Copilot Studio.",
    inputSchema: { query: z.string().min(1).describe("Plain-language search query") },
    annotations: { readOnlyHint: true, destructiveHint: false, openWorldHint: false },
    _meta: {},
  }, async ({ query }) => {
    const words = query.toLowerCase().split(/\s+/).filter(Boolean);
    const results = systems().filter((item) => words.some((word) => JSON.stringify(item).toLowerCase().includes(word)));
    return {
      content: [{ type: "text", text: `Found ${results.length} HELIOS system record(s).` }],
      structuredContent: { results },
    };
  });

  registerAppTool(server, "fetch", {
    title: "Fetch a HELIOS system",
    description: "Use this when the user has a system ID from search and needs its authoritative purpose, state, link, and next action.",
    inputSchema: { id: z.enum(["github", "azure", "azure-cli", "sharepoint", "slack", "slack-all", "teams", "linear", "copilot"]) },
    annotations: { readOnlyHint: true, destructiveHint: false, openWorldHint: false },
    _meta: {},
  }, async ({ id }) => {
    const record = systems().find((item) => item.id === id);
    return {
      content: [{ type: "text", text: record ? `${record.name}: ${record.state}. ${record.nextAction}` : "System not found." }],
      structuredContent: { record },
    };
  });

  registerAppTool(server, "get_control_plane_status", {
    title: "Get HELIOS control-plane status",
    description: "Use this when the user wants the current setup state and safe next actions for all HELIOS integrations.",
    inputSchema: {},
    annotations: { readOnlyHint: true, destructiveHint: false, openWorldHint: false },
    _meta: {},
  }, async () => ({
    content: [{ type: "text", text: "HELIOS control-plane status loaded. Deployment remains governed by GitHub and the protected Azure environment." }],
    structuredContent: { version: VERSION, generatedAt: new Date().toISOString(), systems: systems() },
  }));

  registerAppTool(server, "validate_azure_identity", {
    title: "Validate Azure workload identity configuration",
    description: "Use this when the user needs to verify the non-secret Azure GitHub OIDC settings and expected federation subject before deployment.",
    inputSchema: { repository: z.string().default("M0nado/helios-platform"), environment: z.string().default("azure-dev") },
    annotations: { readOnlyHint: true, destructiveHint: false, openWorldHint: false },
    _meta: {},
  }, async ({ repository, environment }) => {
    const required = ["AZURE_CLIENT_ID", "AZURE_TENANT_ID", "AZURE_SUBSCRIPTION_ID", "AZURE_RESOURCE_GROUP"];
    const variables = Object.fromEntries(required.map((name) => [name, present(name)]));
    const federationSubject = `repo:${repository}:environment:${environment}`;
    return {
      content: [{ type: "text", text: `Expected federation subject: ${federationSubject}. ${Object.values(variables).every(Boolean) ? "Required variables are present." : "One or more required variables are missing."}` }],
      structuredContent: { federationSubject, variables, deployEnabled: process.env.AZURE_DEPLOY_ENABLED === "true" },
    };
  });

  registerAppTool(server, "render_control_center", {
    title: "Open Monado Control Center",
    description: "Use this after loading HELIOS status when the user wants the interactive Monado dashboard rendered in ChatGPT or another MCP Apps-compatible host.",
    inputSchema: {},
    annotations: { readOnlyHint: true, destructiveHint: false, openWorldHint: false },
    _meta: { ui: { resourceUri: CONTROL_URI } },
  }, async () => ({
    content: [{ type: "text", text: "The Monado Control Center is displaying the current HELIOS integration state." }],
    structuredContent: { version: VERSION, generatedAt: new Date().toISOString(), systems: systems() },
  }));

  registerAppResource(server, "Monado Control Center", CONTROL_URI, {
    mimeType: RESOURCE_MIME_TYPE,
    description: "Interactive HELIOS integration status dashboard",
    _meta: {
      ui: {
        prefersBorder: false,
        csp: { connectDomains: [], resourceDomains: [] },
      },
      "openai/widgetDescription": "Monado Control Center shows HELIOS GitHub, Azure, SharePoint, Slack, Teams, Linear, and Copilot setup state.",
    },
  }, async () => ({
    contents: [{ uri: CONTROL_URI, mimeType: RESOURCE_MIME_TYPE, text: widgetHtml }],
  }));

  return server;
}

const port = Number.parseInt(process.env.PORT ?? "8000", 10);
const app = express();
app.disable("x-powered-by");
app.use(cors({ origin: false }));
app.use(express.json({ limit: "1mb" }));
app.get("/health", (_req, res) => res.json({ status: "ok", service: "helios-control", version: VERSION }));
app.all("/mcp", async (req, res) => {
  const server = createServer();
  const transport = new StreamableHTTPServerTransport({ sessionIdGenerator: undefined });
  res.on("close", () => {
    void transport.close();
    void server.close();
  });
  try {
    await server.connect(transport);
    await transport.handleRequest(req, res, req.body);
  } catch (error) {
    console.error("MCP request failed", error);
    if (!res.headersSent) res.status(500).json({ jsonrpc: "2.0", error: { code: -32603, message: "Internal server error" }, id: null });
  }
});
app.listen(port, () => console.log(`HELIOS MCP listening at http://127.0.0.1:${port}/mcp`));
