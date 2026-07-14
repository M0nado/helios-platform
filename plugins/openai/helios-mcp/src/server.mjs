import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { BrokerClient } from './broker-client.mjs';

const asResult = (payload) => ({
  content: [{ type: 'text', text: JSON.stringify(payload, null, 2) }],
  structuredContent: payload
});

const asFailure = (error) => ({
  isError: true,
  content: [{ type: 'text', text: error instanceof Error ? error.message : String(error) }]
});

export function createServer({ client = new BrokerClient() } = {}) {
  const server = new McpServer({ name: 'helios-integration-mcp', version: '0.1.0' });

  server.registerTool('helios_status', {
    title: 'Read HELIOS integration status',
    description: 'Read broker, catalog, event, and pending-request readiness without changing state.',
    inputSchema: {},
    annotations: { readOnlyHint: true, openWorldHint: false, destructiveHint: false }
  }, async () => {
    try { return asResult(await client.status()); } catch (error) { return asFailure(error); }
  });

  server.registerTool('helios_list_tools', {
    title: 'List approved HELIOS tools',
    description: 'Return the authoritative HELIOS tool catalog and approval annotations.',
    inputSchema: {},
    annotations: { readOnlyHint: true, openWorldHint: false, destructiveHint: false }
  }, async () => {
    try { return asResult(await client.listTools()); } catch (error) { return asFailure(error); }
  });

  server.registerTool('helios_search_events', {
    title: 'Search HELIOS integration events',
    description: 'Search bounded normalized events by correlation ID.',
    inputSchema: {
      correlationId: z.string().max(128).nullable().optional(),
      limit: z.number().int().min(1).max(100).default(20)
    },
    annotations: { readOnlyHint: true, openWorldHint: false, destructiveHint: false }
  }, async ({ correlationId = null, limit = 20 }) => {
    try { return asResult(await client.searchEvents({ correlationId, limit })); } catch (error) { return asFailure(error); }
  });

  const actionSchema = {
    toolName: z.string().min(1).max(128),
    correlationId: z.string().min(4).max(128),
    environment: z.enum(['local', 'development', 'test', 'staging', 'production']),
    arguments: z.record(z.unknown()).default({}),
    requestedBy: z.string().max(256).nullable().optional(),
    reason: z.string().max(1000).nullable().optional()
  };

  server.registerTool('helios_preview_action', {
    title: 'Preview a HELIOS action request',
    description: 'Evaluate an approved catalog tool against policy without recording or executing it.',
    inputSchema: actionSchema,
    annotations: { readOnlyHint: true, openWorldHint: false, destructiveHint: false }
  }, async (request) => {
    try { return asResult(await client.previewAction(request)); } catch (error) { return asFailure(error); }
  });

  server.registerTool('helios_request_action', {
    title: 'Create a pending HELIOS action request',
    description: 'Record an approval-pending request. This tool does not execute the connector action.',
    inputSchema: actionSchema,
    annotations: { readOnlyHint: false, openWorldHint: false, destructiveHint: false }
  }, async (request) => {
    try { return asResult(await client.requestAction(request)); } catch (error) { return asFailure(error); }
  });

  return server;
}
