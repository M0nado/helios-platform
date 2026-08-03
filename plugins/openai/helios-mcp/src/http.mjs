import crypto from 'node:crypto';
import express from 'express';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import { createServer } from './server.mjs';

const app = express();
app.use(express.json({ limit: '1mb' }));

function sameToken(expected, supplied) {
  const expectedDigest = crypto.createHash('sha256').update(expected).digest();
  const suppliedDigest = crypto.createHash('sha256').update(supplied).digest();
  return crypto.timingSafeEqual(expectedDigest, suppliedDigest);
}

app.get('/healthz', (_request, response) => {
  response.json({ service: 'helios-integration-mcp', state: 'ready' });
});

app.use('/mcp', (request, response, next) => {
  const expected = process.env.HELIOS_MCP_TOKEN;
  if (!expected) {
    response.status(503).json({ error: 'mcp_not_configured' });
    return;
  }

  const authorization = request.header('authorization') ?? '';
  const supplied = authorization.startsWith('Bearer ') ? authorization.slice(7).trim() : '';
  if (!supplied || !sameToken(expected, supplied)) {
    response.sendStatus(401);
    return;
  }

  next();
});

app.post('/mcp', async (request, response) => {
  const server = createServer();
  const transport = new StreamableHTTPServerTransport({ sessionIdGenerator: undefined });

  response.on('close', () => {
    void transport.close();
    void server.close();
  });

  try {
    await server.connect(transport);
    await transport.handleRequest(request, response, request.body);
  } catch (error) {
    console.error(error);
    if (!response.headersSent) response.status(500).json({ error: 'mcp_request_failed' });
  }
});

app.get('/mcp', (_request, response) => response.sendStatus(405));
app.delete('/mcp', (_request, response) => response.sendStatus(405));

const port = Number.parseInt(process.env.PORT ?? '5081', 10);
app.listen(port, '0.0.0.0', () => {
  console.log(`HELIOS MCP listening on port ${port}`);
});
