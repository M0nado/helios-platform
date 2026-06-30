#!/usr/bin/env node
/**
 * HELIOS autonomous workflow preflight.
 *
 * Runs lightweight repository checks without requiring Azure credentials,
 * network access, or sibling repositories. Optional Azure and fleet workspace
 * checks are reported as informational by default and can be made strict with
 * AZURE_CLI_REQUIRED=true or FLEET_WORKSPACES_REQUIRED=true.
 */

const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const root = path.resolve(__dirname, '..');
const checks = [];
const strictAzure = process.env.AZURE_CLI_REQUIRED === 'true';
const strictFleet = process.env.FLEET_WORKSPACES_REQUIRED === 'true';

function record(name, status, detail) {
  checks.push({ name, status, detail });
  const icons = { pass: '✅', info: 'ℹ️', fail: '❌' };
  console.log(`${icons[status] || icons.fail} ${name}${detail ? ` - ${detail}` : ''}`);
}

function exists(relativePath) {
  return fs.existsSync(path.join(root, relativePath));
}

function command(args) {
  return spawnSync(args[0], args.slice(1), { cwd: root, encoding: 'utf8' });
}

record('package manifest', exists('package.json') ? 'pass' : 'fail', 'package.json present');
record('cache strategy module', exists('src/cache/cache-strategy.js') ? 'pass' : 'fail', 'runtime cache module present');

const node = command([process.execPath, '-e', "require('./src'); console.log('exports-ok')"]);
record('public API load', node.status === 0 ? 'pass' : 'fail', (node.stdout || node.stderr).trim());

const branches = command(['git', 'branch', '--format=%(refname:short)']);
const branchList = branches.status === 0
  ? branches.stdout.split('\n').map(branch => branch.trim()).filter(Boolean)
  : [];
record(
  'git branch inventory',
  branches.status === 0 ? 'pass' : 'fail',
  branchList.length > 0 ? `${branchList.length} local branch(es): ${branchList.join(', ')}` : branches.stderr.trim()
);

const az = spawnSync('az', ['--version'], { encoding: 'utf8' });
record(
  'Azure CLI availability',
  az.status === 0 ? 'pass' : strictAzure ? 'fail' : 'info',
  az.status === 0 ? az.stdout.split('\n')[0] : 'not installed in this agent; run scripts/bootstrap-azure-cli.sh on deployment hosts'
);

for (const sibling of ['helios-control', 'hermes-fleet-production']) {
  const siblingPath = path.resolve(root, '..', sibling);
  const present = fs.existsSync(siblingPath);
  record(
    `${sibling} workspace`,
    present ? 'pass' : strictFleet ? 'fail' : 'info',
    present ? siblingPath : `not present at ${siblingPath}; skipping optional cross-repo scan`
  );
}

const failed = checks.filter(check => check.status === 'fail');
if (failed.length > 0) {
  process.exitCode = 1;
}
