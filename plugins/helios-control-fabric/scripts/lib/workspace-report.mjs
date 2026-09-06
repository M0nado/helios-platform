// Shared browser/CLI projection. This module never executes a command or calls a service.
export const MAX_REPORT_BYTES = 262144;
export const MAX_AGE_MS = 30 * 60 * 1000;
export const STEP_IDS = Object.freeze(['toolchain', 'identity', 'auth', 'inventory', 'stack-smoke', 'rest-connect']);
export const REQUIRED_STEPS = Object.freeze(STEP_IDS.slice(0, 5));
export const REPOSITORIES = Object.freeze(['Yolkster64/helios-platform', 'M0nado/helios-platform']);
const STATES = new Set(['ok', 'degraded', 'failed', 'unavailable']);
const SCRIPT = 'scripts/bootstrap/setup-everything.ps1';
const invalid = () => { throw new Error('Unsupported or incomplete setup report. Run the reviewed setup-readiness script again.'); };
const object = value => value !== null && typeof value === 'object' && !Array.isArray(value);

export function parseReport(text) {
  if (typeof text !== 'string' || new TextEncoder().encode(text).length > MAX_REPORT_BYTES) invalid();
  try { return JSON.parse(text.replace(/^\uFEFF/, '')); } catch { return invalid(); }
}

export function projectReport(report, { repository, sourceSha, now = Date.now() } = {}) {
  if (!REPOSITORIES.includes(repository) || typeof sourceSha !== 'string' || !/^[a-f0-9]{40}$/.test(sourceSha)) invalid();
  if (!object(report) || report.script !== SCRIPT || report.mode !== 'report-only') invalid();
  if (typeof report.executionSucceeded !== 'boolean' || typeof report.ready !== 'boolean') invalid();
  if (!Number.isInteger(report.exitCode) || ![0, 1, 2].includes(report.exitCode)) invalid();
  for (const name of ['ownerActions', 'readinessIssues']) {
    if (!Array.isArray(report[name]) || report[name].length > 128 || report[name].some(v => typeof v !== 'string')) invalid();
  }
  if (typeof report.generatedUtc !== 'string' || !/^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(?:\.\d+)?(?:Z|[+-]\d\d:\d\d)$/.test(report.generatedUtc)) invalid();
  const when = Date.parse(report.generatedUtc);
  if (!Number.isFinite(when) || !Number.isFinite(now)) invalid();
  if (!Array.isArray(report.steps) || report.steps.length > STEP_IDS.length) invalid();
  const seen = new Set();
  const steps = report.steps.map(step => {
    if (!object(step) || !STEP_IDS.includes(step.step) || seen.has(step.step) || !STATES.has(step.state)) invalid();
    if (!Number.isInteger(step.exitCode) || step.exitCode < -1 || step.exitCode > 2) invalid();
    seen.add(step.step);
    // Free-form summary, ownerAction, commands, paths and all unknown fields are discarded.
    return { step: step.step, state: step.state, exitCode: step.exitCode };
  });
  const missingSteps = REQUIRED_STEPS.filter(id => !seen.has(id));
  const stale = now - when > MAX_AGE_MS || when - now > 60000;
  const ready = !stale && missingSteps.length === 0 && report.exitCode === 0 &&
    report.executionSucceeded === true && report.ready === true &&
    report.ownerActions.length === 0 && report.readinessIssues.length === 0 &&
    steps.every(step => step.state === 'ok' && step.exitCode === 0);
  return {
    schemaVersion: '1.0', recordType: 'helios.workspace-readiness',
    claimLevel: 'operator-supplied-unattested', classification: 'internal',
    source: { repository, commit: sourceSha },
    reportedAt: new Date(when).toISOString(), checkedAt: new Date(now).toISOString(), stale,
    workspaceChecksReady: ready, fullSetupReady: false, deploymentAuthorized: false,
    executionSucceeded: report.executionSucceeded, steps, missingSteps,
    ownerActionCount: report.ownerActions.length, readinessIssueCount: report.readinessIssues.length,
    liveVerification: {
      github: 'not-proven-by-report', codex: 'not-proven-by-report', claude: 'not-proven-by-report',
      slack: 'not-proven-by-report', linear: 'not-proven-by-report', sharepoint: 'not-proven-by-report',
      teams: 'not-proven-by-report', m365Copilot: 'not-proven-by-report', azure: 'not-proven-by-report',
      foundry: 'not-proven-by-report', azureDevOps: 'not-proven-by-report', hermes: 'not-proven-by-report',
      xcore: 'not-proven-by-report', aihub: 'not-proven-by-report'
    }
  };
}
