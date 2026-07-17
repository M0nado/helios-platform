const $ = id => document.getElementById(id);
const terminalStatuses = new Set(['completed', 'awaiting-approval', 'failed']);
let currentRunId = localStorage.getItem('helios.latestRunId');
let currentRunStatus;
let pollTimer;
let pollFailures = 0;
let installPrompt;

window.addEventListener('beforeinstallprompt', event => {
  event.preventDefault();
  installPrompt = event;
  $('installApp').classList.remove('hidden');
});

window.addEventListener('appinstalled', () => {
  installPrompt = undefined;
  $('installApp').classList.add('hidden');
});

$('installApp').addEventListener('click', async () => {
  if (!installPrompt) return;
  await installPrompt.prompt();
  await installPrompt.userChoice;
  installPrompt = undefined;
  $('installApp').classList.add('hidden');
});

if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => navigator.serviceWorker.register('/wizard/sw.js', { scope: '/wizard/' }).catch(() => {
    // The authenticated online control surface remains usable if PWA registration is unavailable.
  }));
}

const tabs = [...document.querySelectorAll('.tab')];
function activateTab(button) {
  document.querySelectorAll('.tab,.panel').forEach(element => element.classList.remove('active'));
  tabs.forEach(tab => tab.setAttribute('aria-selected', String(tab === button)));
  button.classList.add('active');
  $(button.dataset.panel).classList.add('active');
}
tabs.forEach((button, index) => {
  button.addEventListener('click', () => activateTab(button));
  button.addEventListener('keydown', event => {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
    event.preventDefault();
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1
      : (index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
    tabs[next].focus();
    activateTab(tabs[next]);
  });
});

async function api(url, options = {}) {
  const response = await fetch(url, options);
  if (response.status === 401) {
    window.location.assign(`/.auth/login/aad?post_login_redirect_uri=${encodeURIComponent('/setup')}`);
    throw new Error('Microsoft sign-in is required.');
  }
  const result = await response.json().catch(() => ({}));
  if (!response.ok) {
    const error = new Error(result.error || `Request failed (${response.status})`);
    error.status = response.status;
    throw error;
  }
  return result;
}

async function loadConnectors() {
  try {
    const result = await api('/control/connectors');
    $('connectorMode').textContent = `${result.mode} delivery · endpoints and secrets never displayed`;
    $('connectors').replaceChildren(...result.bindings.map(binding => {
      const label = document.createElement('label');
      label.className = 'connector';
      const checkbox = document.createElement('input');
      checkbox.type = 'checkbox';
      checkbox.value = binding.connector;
      checkbox.checked = true;
      const name = document.createElement('span');
      name.textContent = binding.connector;
      const state = document.createElement('i');
      state.className = `binding${binding.configured ? ' on' : ''}`;
      state.title = binding.configured ? 'Relay binding configured' : 'Relay binding not configured';
      state.setAttribute('aria-hidden', 'true');
      const bindingLabel = document.createElement('small');
      bindingLabel.className = 'binding-label';
      bindingLabel.textContent = binding.configured ? 'bound' : 'unbound';
      label.append(checkbox, name, state, bindingLabel);
      return label;
    }));
  } catch (error) {
    $('connectorMode').textContent = error.message;
  }
}

function selectedConnectors() {
  return [...document.querySelectorAll('#connectors input:checked')].map(input => input.value);
}

$('runNow').addEventListener('click', async () => {
  clearTimeout(pollTimer);
  $('runNow').disabled = true;
  $('runStatus').className = 'run-status running';
  $('statusText').textContent = 'Starting';
  $('runMeta').textContent = 'Creating an idempotent saved run…';
  try {
    const request = {
      intent: $('runIntent').value,
      environment: $('runEnvironment').value,
      target: $('runTarget').value || null,
      connectors: selectedConnectors()
    };
    const requestBody = JSON.stringify(request);
    let pending;
    try { pending = JSON.parse(localStorage.getItem('helios.pendingRun') || 'null'); } catch { pending = null; }
    if (!pending || pending.requestBody !== requestBody) {
      pending = { idempotencyKey: crypto.randomUUID(), requestBody };
      localStorage.setItem('helios.pendingRun', JSON.stringify(pending));
    }
    const result = await api('/control/runs', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': pending.idempotencyKey },
      body: requestBody
    });
    currentRunId = result.id;
    localStorage.setItem('helios.latestRunId', currentRunId);
    localStorage.removeItem('helios.pendingRun');
    renderRun(result);
    schedulePoll();
  } catch (error) {
    renderError(error.message);
  }
});

function schedulePoll(delay = 900) {
  clearTimeout(pollTimer);
  pollTimer = setTimeout(pollRun, document.hidden ? Math.max(delay, 5_000) : delay);
}

async function pollRun() {
  if (!currentRunId) return;
  try {
    const run = await api(`/control/runs/${currentRunId}`);
    pollFailures = 0;
    renderRun(run);
    if (!terminalStatuses.has(run.status)) schedulePoll();
  } catch (error) {
    if (error.status === 404) {
      currentRunId = null;
      currentRunStatus = undefined;
      localStorage.removeItem('helios.latestRunId');
      $('runStatus').className = 'run-status idle';
      $('statusText').textContent = 'Ready';
      $('runMeta').textContent = 'The prior owner-scoped run is no longer available.';
      $('runNow').disabled = false;
      return;
    }
    pollFailures += 1;
    renderConnectionIssue(error.message);
    schedulePoll(Math.min(30_000, 900 * (2 ** Math.min(pollFailures, 5))));
  }
}

function renderRun(run) {
  currentRunStatus = run.status;
  $('runNow').disabled = !terminalStatuses.has(run.status);
  $('runStatus').className = `run-status ${run.status}`;
  $('statusText').textContent = run.status.replaceAll('-', ' ');
  $('runMeta').textContent = run.error || `Run ${run.id} · correlation ${run.correlationId} · ${run.resourceCount} resources`;
  $('timeline').replaceChildren(...run.steps.map(step => {
    const row = document.createElement('div');
    row.className = `step ${step.status}`;
    const icon = document.createElement('span');
    icon.className = 'step-icon';
    icon.textContent = step.status === 'completed' ? '✓' : step.status === 'running' ? '›' : '·';
    const name = document.createElement('strong');
    name.textContent = step.name;
    const detail = document.createElement('small');
    detail.textContent = step.detail;
    row.append(icon, name, detail);
    return row;
  }));

  $('evidence').classList.toggle('hidden', !run.evidenceSha256);
  $('digestValue').textContent = run.evidenceSha256 || '';
  $('planDigest').classList.toggle('hidden', !run.plan);
  $('planId').textContent = run.plan ? `${run.plan.planId.slice(0, 16)} · ${run.plan.mode}` : '';
  $('planSteps').replaceChildren(...(run.plan?.steps || []).map(step => {
    const item = document.createElement('li');
    const action = document.createElement('strong');
    action.textContent = step.action;
    const detail = document.createElement('small');
    detail.textContent = `${step.executor} · ${step.mutating ? 'mutation locked' : 'read/plan'} · ${step.gate}`;
    item.append(action, detail);
    return item;
  }));
  $('receipts').replaceChildren(...run.receipts.map(receipt => {
    const card = document.createElement('div');
    card.className = 'receipt';
    const name = document.createElement('strong');
    name.textContent = receipt.connector;
    const status = document.createElement('span');
    status.className = receipt.status;
    status.textContent = receipt.status;
    const detail = document.createElement('small');
    detail.textContent = `${receipt.detail}${receipt.attempts ? ` · ${receipt.attempts} attempt${receipt.attempts === 1 ? '' : 's'}` : ''}`;
    card.append(name, status, detail);
    return card;
  }));
  $('resumeRun').classList.toggle('hidden', run.status !== 'failed');
}

function renderError(message) {
  currentRunStatus = 'failed';
  $('runNow').disabled = false;
  $('runStatus').className = 'run-status failed';
  $('statusText').textContent = 'Run failed';
  $('runMeta').textContent = message;
  $('resumeRun').classList.add('hidden');
}

function renderConnectionIssue(message) {
  $('runStatus').className = 'run-status running';
  $('statusText').textContent = 'Connection interrupted';
  $('runMeta').textContent = `${message} Retrying safely with the saved run ID…`;
  $('runNow').disabled = true;
  $('resumeRun').classList.add('hidden');
}

$('resumeRun').addEventListener('click', async () => {
  if (!currentRunId) return;
  try {
    const run = await api(`/control/runs/${currentRunId}/resume`, { method: 'POST' });
    renderRun(run);
    schedulePoll();
  } catch (error) {
    renderError(error.message);
  }
});

$('copyDigest').addEventListener('click', () => navigator.clipboard.writeText($('digestValue').textContent));

document.addEventListener('visibilitychange', () => {
  if (!document.hidden && currentRunId && !terminalStatuses.has(currentRunStatus)) {
    clearTimeout(pollTimer);
    pollRun();
  }
});

async function post(url, data, status) {
  $(status).textContent = 'Working…';
  const result = await api(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  });
  $(status).textContent = 'Ready for review';
  return result;
}

$('generate').addEventListener('click', async () => {
  try {
    const result = await post('/setup/bootstrap', {
      tenantId: $('tenant').value,
      subscriptionId: $('subscription').value,
      resourceGroup: $('group').value,
      environment: $('environment').value
    }, 'setupStatus');
    $('script').textContent = result.script;
    const roles = Object.keys(result.shellPackets).join(', ');
    $('digest').textContent = `SHA-256: ${result.scriptSha256} · ${result.subscriptionSelection} · roles: ${roles} · plan only · no secrets`;
  } catch (error) {
    $('setupStatus').textContent = error.message;
  }
});

$('copy').addEventListener('click', () => navigator.clipboard.writeText($('script').textContent));

$('propose').addEventListener('click', async () => {
  try {
    const result = await post('/upgrades/propose', {
      capability: $('capability').value,
      reason: $('reason').value,
      target: 'helios-control'
    }, 'upgradeStatus');
    $('proposal').textContent = JSON.stringify(result, null, 2);
  } catch (error) {
    $('upgradeStatus').textContent = error.message;
  }
});

loadConnectors().then(() => {
  if (currentRunId) pollRun();
});
