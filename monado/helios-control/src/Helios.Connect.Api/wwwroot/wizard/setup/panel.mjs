import { MAX_REPORT_BYTES, parseReport, projectReport } from './report.mjs';
const byId = id => document.getElementById(id);
let lastInput = null;
let refreshTimer;

function showProjection() {
  clearTimeout(refreshTimer);
  byId('steps').replaceChildren();
  if (!lastInput) return;
  try {
    const view = projectReport(lastInput, { repository: byId('repository').value, sourceSha: byId('sourceSha').value.trim() });
    byId('status').textContent = view.stale ? 'Report expired or future-dated — run checks again' :
      view.workspaceChecksReady ? 'Imported workspace checks passed — live connections still unverified' : 'Setup needs attention';
    byId('details').textContent = `Reported ${view.reportedAt}. Owner actions: ${view.ownerActionCount}. Readiness gaps: ${view.readinessIssueCount}. Missing steps: ${view.missingSteps.join(', ') || 'none'}.`;
    for (const step of view.steps) {
      const row = document.createElement('li');
      row.textContent = `${step.step}: ${step.state} (exit ${step.exitCode})`;
      byId('steps').append(row);
    }
    // Re-evaluate age while the page remains open; green imported evidence must expire.
    refreshTimer = setTimeout(showProjection, 30000);
  } catch {
    lastInput = null;
    byId('status').textContent = 'Report rejected — provide a current report-only setup result and a full source commit.';
    byId('details').textContent = 'No report text was displayed or sent to a service.';
  }
}

byId('report').addEventListener('change', async event => {
  lastInput = null;
  byId('steps').replaceChildren();
  byId('details').textContent = '';
  const file = event.target.files?.[0];
  try {
    if (!file || file.size > MAX_REPORT_BYTES) throw new Error('size');
    lastInput = parseReport(new TextDecoder('utf-8', { fatal: true }).decode(await file.arrayBuffer()));
    showProjection();
  } catch {
    clearTimeout(refreshTimer);
    byId('status').textContent = 'Report rejected — select a valid JSON file no larger than 256 KiB.';
  }
  event.target.value = '';
});
byId('forget').addEventListener('click', () => {
  lastInput = null;
  clearTimeout(refreshTimer);
  byId('steps').replaceChildren();
  byId('details').textContent = '';
  byId('status').textContent = 'No machine report loaded';
});
byId('repository').addEventListener('change', showProjection);
byId('sourceSha').addEventListener('change', showProjection);
for (const button of document.querySelectorAll('[data-copy]')) {
  button.addEventListener('click', async () => {
    try {
      await navigator.clipboard.writeText(byId(button.dataset.copy).textContent);
      button.textContent = 'Copied';
    } catch { button.textContent = 'Select and copy the command below'; }
  });
}
