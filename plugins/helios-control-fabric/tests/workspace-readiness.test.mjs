import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, writeFile, rm, symlink } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { MAX_REPORT_BYTES, projectReport, parseReport } from '../../../monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/setup/report.mjs';
const now = Date.parse('2026-09-06T01:00:00Z');
const options = { repository: 'Yolkster64/helios-platform', sourceSha: 'a'.repeat(40), now };
function report() {
  return { script: 'scripts/bootstrap/setup-everything.ps1', generatedUtc: new Date(now).toISOString(),
    mode: 'report-only', executionSucceeded: true, ready: true, exitCode: 0,
    ownerActions: [], readinessIssues: [],
    steps: ['toolchain', 'identity', 'auth', 'inventory', 'stack-smoke'].map(step => ({ step, state: 'ok', exitCode: 0 })) };
}

test('valid report proves only imported workspace checks', () => {
  const p = projectReport(report(), options);
  assert.equal(p.workspaceChecksReady, true);
  assert.equal(p.fullSetupReady, false);
  assert.equal(p.deploymentAuthorized, false);
  assert.ok(Object.values(p.liveVerification).every(v => v === 'not-proven-by-report'));
});
test('raw summaries, commands, paths, keys and unknown fields are discarded', () => {
  const r = report(); r.unknown = 'SENSITIVE_FIXTURE'; r.steps[0].summary = 'SENSITIVE_FIXTURE';
  r.ownerActions = ['SENSITIVE_FIXTURE']; r.readinessIssues = ['SENSITIVE_FIXTURE'];
  const p = projectReport(r, options);
  assert.equal(p.workspaceChecksReady, false); assert.equal(p.ownerActionCount, 1);
  assert.ok(!JSON.stringify(p).includes('SENSITIVE_FIXTURE'));
});
test('report with readiness gaps cannot claim ready', () => {
  const r = report(); r.readinessIssues = ['gap']; assert.equal(projectReport(r, options).workspaceChecksReady, false);
});
test('old format missing executionSucceeded is rejected', () => {
  const r = report(); delete r.executionSucceeded; assert.throws(() => projectReport(r, options));
});
test('truthy strings are not booleans', () => {
  for (const field of ['ready', 'executionSucceeded']) { const r = report(); r[field] = 'true'; assert.throws(() => projectReport(r, options)); }
});
test('missing required steps cannot claim ready', () => {
  const r = report(); r.steps.splice(1, 1); const p = projectReport(r, options);
  assert.equal(p.workspaceChecksReady, false); assert.deepEqual(p.missingSteps, ['identity']);
});
test('identity failure and skipped smoke both fail readiness', () => {
  for (const state of ['failed', 'degraded', 'unavailable']) {
    const r = report(); r.steps[1].state = state; assert.equal(projectReport(r, options).workspaceChecksReady, false);
  }
});
test('child failure cannot be hidden by aggregate booleans', () => {
  const r = report(); r.steps[0].exitCode = 2; assert.equal(projectReport(r, options).workspaceChecksReady, false);
});
test('duplicate and unknown steps are rejected', () => {
  const r = report(); r.steps.push({ ...r.steps[0] }); assert.throws(() => projectReport(r, options));
  const s = report(); s.steps[0].step = 'shell'; assert.throws(() => projectReport(s, options));
});
test('apply report is never imported as read-only', () => {
  const r = report(); r.mode = 'apply'; assert.throws(() => projectReport(r, options));
});
test('reports expire and future dates fail readiness', () => {
  for (const offset of [-1800001, 60001]) {
    const r = report(); r.generatedUtc = new Date(now + offset).toISOString();
    assert.equal(projectReport(r, options).stale, true); assert.equal(projectReport(r, options).workspaceChecksReady, false);
  }
});
test('timestamp must contain a timezone', () => {
  const r = report(); r.generatedUtc = '2026-09-06T01:00:00'; assert.throws(() => projectReport(r, options));
});
test('metadata requires a full commit and known repository', () => {
  assert.throws(() => projectReport(report(), { ...options, sourceSha: 'main' }));
  assert.throws(() => projectReport(report(), { ...options, repository: 'https://host/token' }));
});
test('bounded parsing rejects oversized and malformed input', () => {
  assert.throws(() => parseReport(' '.repeat(MAX_REPORT_BYTES + 1))); assert.throws(() => parseReport('{'));
  assert.deepEqual(parseReport('\ufeff' + JSON.stringify(report())), report());
});
test('large action arrays and bad shape are rejected', () => {
  const r = report(); r.ownerActions = new Array(129).fill('x'); assert.throws(() => projectReport(r, options));
  assert.throws(() => projectReport([], options)); assert.throws(() => projectReport(null, options));
});
test('CLI emits sanitized JSON and a content digest', async () => {
  const root = await mkdtemp(join(tmpdir(), 'helios-readiness-'));
  try {
    const r = report(); r.generatedUtc = new Date().toISOString(); r.steps[0].summary = 'SENSITIVE_FIXTURE';
    const path = join(root, 'report.json'); await writeFile(path, JSON.stringify(r));
    const cli = fileURLToPath(new URL('../scripts/workspace-readiness.mjs', import.meta.url));
    const p = spawnSync(process.execPath, [cli, '--report', path, '--source-sha', options.sourceSha], { encoding: 'utf8', timeout: 10000 });
    assert.equal(p.status, 0, p.stderr); const output = JSON.parse(p.stdout);
    assert.match(output.inputSha256, /^[a-f0-9]{64}$/); assert.ok(!p.stdout.includes('SENSITIVE_FIXTURE'));
    assert.equal(output.fullSetupReady, false);
  } finally { await rm(root, { recursive: true, force: true }); }
});
test('CLI reports missing and symbolic-link inputs without leaking paths', async () => {
  const root = await mkdtemp(join(tmpdir(), 'helios-readiness-'));
  try {
    const path = join(root, 'PRIVATE_PATH_FIXTURE'); await writeFile(path, '{}');
    const linked = join(root, 'linked.json'); await symlink(path, linked);
    const cli = fileURLToPath(new URL('../scripts/workspace-readiness.mjs', import.meta.url));
    for (const input of [linked, path + '-missing']) {
      const p = spawnSync(process.execPath, [cli, '--report', input, '--source-sha', options.sourceSha], { encoding: 'utf8', timeout: 10000 });
      assert.equal(p.status, 1); assert.equal(p.stdout, ''); assert.ok(!p.stderr.includes(root));
    }
  } finally { await rm(root, { recursive: true, force: true }); }
});
test('CLI rejects apply-like flags', () => {
  const cli = fileURLToPath(new URL('../scripts/workspace-readiness.mjs', import.meta.url));
  const p = spawnSync(process.execPath, [cli, '--apply'], { encoding: 'utf8', timeout: 10000 });
  assert.equal(p.status, 1); assert.equal(p.stdout, '');
});
test('browser and CLI reject malformed UTF-8 even in discarded fields', async () => {
  const root = await mkdtemp(join(tmpdir(), 'helios-readiness-'));
  const previousDocument = globalThis.document;
  const elements = new Map();
  globalThis.document = {
    getElementById(id) {
      if (!elements.has(id)) elements.set(id, {
        value: '', textContent: '', children: [], listeners: {},
        addEventListener(type, listener) { this.listeners[type] = listener; },
        replaceChildren() { this.children = []; },
        append(child) { this.children.push(child); }
      });
      return elements.get(id);
    },
    createElement: () => ({ textContent: '' }),
    querySelectorAll: () => []
  };
  try {
    await import('../../../monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/setup/panel.mjs');
    elements.get('repository').value = options.repository;
    elements.get('sourceSha').value = options.sourceSha;
    const r = report(); r.generatedUtc = new Date().toISOString(); r.unknown = 'SENSITIVE_FIXTURE';
    const valid = Buffer.from(JSON.stringify(r));
    const malformed = Buffer.from(valid); malformed[malformed.indexOf('SENSITIVE_FIXTURE')] = 0xff;
    const path = join(root, 'report.json');
    const cli = fileURLToPath(new URL('../scripts/workspace-readiness.mjs', import.meta.url));
    for (const [bytes, accepted] of [[valid, true], [Buffer.concat([Buffer.from([0xef, 0xbb, 0xbf]), valid]), true], [malformed, false]]) {
      const target = { files: [new Blob([bytes])], value: 'report.json' };
      await elements.get('report').listeners.change({ target });
      assert.equal(target.value, '');
      assert.match(elements.get('status').textContent, accepted ? /^Imported workspace checks passed/ : /^Report rejected/);
      assert.equal(elements.get('steps').children.length, accepted ? 5 : 0);
      if (!accepted) assert.equal(elements.get('details').textContent, '');
      await writeFile(path, bytes);
      const p = spawnSync(process.execPath, [cli, '--report', path, '--source-sha', options.sourceSha], { encoding: 'utf8', timeout: 10000 });
      assert.equal(p.status, accepted ? 0 : 1, p.stderr);
      if (!accepted) assert.equal(p.stdout, '');
    }
  } finally {
    elements.get('forget')?.listeners.click?.();
    if (previousDocument === undefined) delete globalThis.document;
    else globalThis.document = previousDocument;
    await rm(root, { recursive: true, force: true });
  }
});
test('browser projection is byte-identical to the plugin source', async () => {
  const { readFile } = await import('node:fs/promises');
  const source = await readFile(new URL('../scripts/lib/workspace-report.mjs', import.meta.url));
  const web = await readFile(new URL('../../../monado/helios-control/src/Helios.Connect.Api/wwwroot/wizard/setup/report.mjs', import.meta.url));
  assert.deepEqual(web, source);
});
