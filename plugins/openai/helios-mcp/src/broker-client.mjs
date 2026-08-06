export class BrokerClient {
  constructor({ baseUrl = process.env.HELIOS_BROKER_URL ?? 'http://127.0.0.1:5080', token = process.env.HELIOS_BROKER_TOKEN } = {}) {
    this.baseUrl = baseUrl.replace(/\/$/, '');
    this.token = token;
  }

  status() { return this.#request('/api/v1/status'); }
  listTools() { return this.#request('/api/v1/tools'); }

  searchEvents({ correlationId = null, limit = 20 } = {}) {
    const query = new URLSearchParams({ limit: String(limit) });
    if (correlationId) query.set('correlationId', correlationId);
    return this.#request(`/api/v1/events?${query.toString()}`);
  }

  previewAction(request) {
    return this.#request('/api/v1/tools/preview', { method: 'POST', body: JSON.stringify(request) });
  }

  requestAction(request) {
    return this.#request('/api/v1/tools/requests', { method: 'POST', body: JSON.stringify(request) });
  }

  async #request(path, init = {}) {
    if (!this.token) throw new Error('HELIOS_BROKER_TOKEN is required and must come from secure storage.');

    const response = await fetch(`${this.baseUrl}${path}`, {
      ...init,
      headers: {
        accept: 'application/json',
        authorization: `Bearer ${this.token}`,
        ...(init.body ? { 'content-type': 'application/json' } : {}),
        ...(init.headers ?? {})
      },
      signal: AbortSignal.timeout(15000)
    });

    const text = await response.text();
    let payload = null;
    if (text) {
      try { payload = JSON.parse(text); } catch { payload = { message: text }; }
    }

    if (!response.ok) {
      throw new Error(payload?.message ?? payload?.title ?? `Broker returned HTTP ${response.status}`);
    }

    return payload;
  }
}
