#!/usr/bin/env node
// Read a bounded local report. No subprocess, network, authentication or deployment.
import { open, lstat, realpath } from 'node:fs/promises';
import { resolve, parse, sep } from 'node:path';
import { createHash } from 'node:crypto';
import { MAX_REPORT_BYTES, parseReport, projectReport } from './lib/workspace-report.mjs';

async function readRegularFile(path) {
  const absolute = resolve(path);
  let current = parse(absolute).root;
  for (const segment of absolute.slice(current.length).split(sep).filter(Boolean)) {
    current = resolve(current, segment);
    if ((await lstat(current)).isSymbolicLink()) throw new Error('Linked report paths are not supported.');
  }
  if (await realpath(absolute) !== absolute) throw new Error('Report path must resolve directly.');
  const before = await lstat(absolute);
  if (!before.isFile() || before.size > MAX_REPORT_BYTES) throw new Error('Report must be a regular file no larger than 256 KiB.');
  const file = await open(absolute, 'r');
  try {
    const info = await file.stat();
    if (!info.isFile() || info.dev !== before.dev || info.ino !== before.ino) throw new Error('Report changed while opening.');
    const buffer = Buffer.alloc(MAX_REPORT_BYTES + 1);
    let used = 0;
    while (used < buffer.length) {
      const { bytesRead } = await file.read(buffer, used, buffer.length - used, null);
      if (!bytesRead) break;
      used += bytesRead;
    }
    if (used > MAX_REPORT_BYTES) throw new Error('Report is too large.');
    return buffer.subarray(0, used);
  } finally { await file.close(); }
}

try {
  const options = {};
  const args = process.argv.slice(2);
  if (args.length === 1 && args[0] === '--help') {
    console.log('node plugins/helios-control-fabric/scripts/workspace-readiness.mjs --report FILE --source-sha COMMIT [--repository Yolkster64/helios-platform]');
  } else {
    for (let i = 0; i < args.length; i += 2) {
      if (!['--report', '--source-sha', '--repository'].includes(args[i]) || !args[i + 1] || options[args[i]]) throw new Error('Invalid arguments.');
      options[args[i]] = args[i + 1];
    }
    if (!options['--report'] || !options['--source-sha']) throw new Error('A report and its workspace source commit are required.');
    const bytes = await readRegularFile(options['--report']);
    const payload = projectReport(parseReport(new TextDecoder('utf-8', { fatal: true }).decode(bytes)), {
      repository: options['--repository'] || 'Yolkster64/helios-platform', sourceSha: options['--source-sha']
    });
    payload.inputSha256 = createHash('sha256').update(bytes).digest('hex');
    console.log(JSON.stringify(payload, null, 2));
    process.exitCode = payload.workspaceChecksReady ? 0 : 2;
  }
} catch {
  // Never echo raw child output, report content, file paths or parse exception text.
  console.error('HELIOS: report unavailable or invalid. No connection or deployment was attempted.');
  process.exitCode = 1;
}
