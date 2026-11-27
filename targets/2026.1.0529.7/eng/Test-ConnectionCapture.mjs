// Tests the conformance runner's client connection capture module (#450) with Node alone:
//   node eng/Test-ConnectionCapture.mjs <connection-capture.cjs> <empty scratch directory>
//
// A stand-in for grpc-js's logging module, laid out where grpc-js keeps it, receives the
// trace texts the #452 reviews used, and a real Node HTTP/2 exchange carries marker
// values in its headers and trailers. The capture file must keep the reviewed fields and
// none of the markers, and the fixture's own stdout and stderr must be exactly what the
// fixture wrote. Without the capture variable, nothing is written at all.
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const [modulePath, scratch] = process.argv.slice(2);
if (!modulePath || !scratch) throw new Error('Usage: Test-ConnectionCapture.mjs <module> <scratch directory>');
const [major, minor] = process.versions.node.split('.').map(Number);
if (major < 22 || (major === 22 && minor < 15)) {
  throw new Error(`The connection capture needs Node 22.15 or later for its HTTP/2 channels; this is ${process.versions.node}.`);
}

const grpc = join(scratch, 'node_modules', '@grpc', 'grpc-js');
mkdirSync(join(grpc, 'build', 'src'), { recursive: true });
writeFileSync(join(grpc, 'package.json'), JSON.stringify({ name: '@grpc/grpc-js', version: '1.14.6', main: 'build/src/index.js' }));
// The exported shape of grpc-js 1.14's logging module: trace and isTracerEnabled are
// read from the module's exports at each call, and the default logger writes to stderr.
writeFileSync(join(grpc, 'build', 'src', 'logging.js'), `"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.trace = trace;
exports.isTracerEnabled = isTracerEnabled;
const enabled = new Set((process.env.GRPC_NODE_TRACE ?? process.env.GRPC_TRACE ?? '').split(',').filter(Boolean));
function trace(severity, tracer, text) {
  if (isTracerEnabled(tracer)) console.error('D ' + new Date().toISOString() + ' | v1.14.6 ' + process.pid + ' | ' + tracer + ' | ' + text);
}
function isTracerEnabled(tracer) { return enabled.has('all') || enabled.has(tracer); }
`);
writeFileSync(join(grpc, 'build', 'src', 'index.js'), `"use strict";
const logging = require("./logging");
exports.emit = (tracer, text) => { if (logging.isTracerEnabled(tracer)) logging.trace(0, tracer, text); };
`);

const marker = 'CAPTURE-MARKER';
const traces = [
  ['subchannel_call', `[7] Received server headers:\n\t\t:status: 200\n\t\tx_test: ${marker}-1\n\t\tgrpc-message: ${marker}-2\n`],
  ['subchannel_call', `[7] Received server trailers:\n\t\tgrpc-status: 7\n\t\tgrpc-message: part one%0Aquoted section"%0A${marker}-3\n\t\tx.test: ${marker}-4\n`],
  ['resolving_call', `[7] cancelWithStatus code: 1 details: "part one\nquoted section"\n${marker}-5"`],
  ['resolving_call', `[7] ended with status: code=1 details="part one\nquoted section"\n${marker}-6"`],
  ['load_balancing_call', `[7] ended with status: code=14 details="${marker}-7" start time=2026-11-27T12:00:00.000Z`],
  ['load_balancing_call', `[7] Pick result: COMPLETE subchannel: (1) 127.0.0.1:5000 status: undefined ${marker}-8`],
  ['subchannel_call', `[7] Node error event: message=${marker}-9 code=ECONNRESET errno=ECONNRESET syscall=read`],
  ['subchannel_call', `[7] received status details string "${marker}-10" from server`],
  ['transport', `(1) 10.20.30.40:443 connection closed by GOAWAY with code 2 and data ${marker}-11`],
  ['transport', `(1) 127.0.0.1:5000 connection closed with error ${marker}-12`],
  ['transport', `(1) 127.0.0.1:5000 new settings received: {"initialWindowSize":65535,"${marker}-13":"${marker}-14"}`],
  ['channel', `(1) dns:${marker.toLowerCase()}.example.com:443 Channel constructed with options {"grpc.primary_user_agent":"${marker}-15"}`],
  ['channel', `(1) dns:127.0.0.1:5000 createResolvingCall [7] method="/${marker} path/x", deadline=Infinity`],
  ['channel', `(1) dns:127.0.0.1:5000 createLoadBalancingCall [8] method="/briosa.DiscoveryService/GetServerInfo"`],
  ['keepalive', `(1) 127.0.0.1:5000 Ping failed with error ${marker}-16`],
  ['subchannel_call', `[7] something grpc-js may say later ${marker}-17`],
  // Not a captured tracer: neither recorded nor written to stderr.
  ['channel_stacktrace', `(1) Channel constructed \n    at C:\\Users\\${marker}-18\\fixture.ts:1:1`],
];

writeFileSync(join(scratch, 'child.cjs'), `"use strict";
const http2 = require('node:http2');
const grpc = require('@grpc/grpc-js');
process.stdout.write('report\\n');
process.stderr.write('fixture-own-error-line ${marker}-own\\n');
for (const [tracer, text] of ${JSON.stringify(traces)}) grpc.emit(tracer, text);
const server = http2.createServer();
server.on('stream', (stream) => {
  stream.on('data', () => {});
  stream.on('end', () => {
    stream.respond({ ':status': 200, 'content-type': 'application/grpc', 'x-secret': '${marker}-19' }, { waitForTrailers: true });
    stream.on('wantTrailers', () => stream.sendTrailers({ 'grpc-status': '0', 'grpc-message': '${marker}-20', 'x-bin': '${marker}-21' }));
    stream.end(Buffer.from([0, 0, 0, 0, 0]));
  });
});
server.listen(0, '127.0.0.1', async () => {
  const session = http2.connect('http://127.0.0.1:' + server.address().port);
  for (const path of ['/briosa.DiscoveryService/GetServerInfo', '/${marker}-path/x']) {
    await new Promise((resolve) => {
      const request = session.request({ ':method': 'POST', ':path': path, 'content-type': 'application/grpc', 'x-client': '${marker}-22' });
      request.on('response', () => {});
      request.on('data', () => {});
      request.on('error', () => {});
      request.on('close', resolve);
      request.end(Buffer.from([0, 0, 0, 0, 0]));
    });
  }
  session.close();
  server.close();
});
`);

function run(capture) {
  const env = { ...process.env, NODE_PATH: join(scratch, 'node_modules') };
  delete env.GRPC_TRACE;
  delete env.GRPC_NODE_TRACE;
  delete env.GRPC_VERBOSITY;
  delete env.GRPC_NODE_VERBOSITY;
  if (capture) env.BRIOSA_CONFORMANCE_CLIENT_CAPTURE = capture;
  else delete env.BRIOSA_CONFORMANCE_CLIENT_CAPTURE;
  return spawnSync(process.execPath, ['--require', modulePath, join(scratch, 'child.cjs')], {
    cwd: scratch, env, encoding: 'utf8', timeout: 60000,
  });
}

function check(condition, message) {
  if (!condition) throw new Error(message);
}

const capture = join(scratch, 'client-connection.jsonl');
const captured = run(capture);
check(captured.status === 0, `The fixture failed with the capture: ${captured.status} ${captured.stderr}`);
check(captured.stdout === 'report\n', `The capture changed the fixture's stdout: ${JSON.stringify(captured.stdout)}`);
check(captured.stderr === `fixture-own-error-line ${marker}-own\n`,
  `The capture changed the fixture's stderr: ${JSON.stringify(captured.stderr)}`);
check(existsSync(capture), 'The capture wrote no file.');
const text = readFileSync(capture, 'utf8');
check(!text.includes(marker) && !text.includes(marker.toLowerCase()), 'A marker reached the capture file.');
check(!/\\\\|Users|[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+:443/.test(text.replaceAll('127.0.0.1', '')), 'A path or a non-loopback address reached the capture file.');
const records = text.trim().split('\n').map((line) => JSON.parse(line));
const has = (wanted) => records.some((record) => Object.entries(wanted).every(([key, value]) => JSON.stringify(record[key]) === JSON.stringify(value)));
for (const wanted of [
  { src: 'capture', ev: 'start', schema: 1 },
  { src: 'capture', ev: 'grpc-js-hooked', instance: 1, version: '1.14.6' },
  { src: 'grpc', ev: 'response-headers', tracer: 'subchannel_call', call: 7, status: 200 },
  { src: 'grpc', ev: 'response-trailers', tracer: 'subchannel_call', call: 7, grpcStatus: 7 },
  { src: 'grpc', ev: 'cancel-with-status', tracer: 'resolving_call', call: 7, code: 1 },
  { src: 'grpc', ev: 'ended', tracer: 'resolving_call', call: 7, code: 1 },
  { src: 'grpc', ev: 'ended', tracer: 'load_balancing_call', call: 7, code: 14 },
  { src: 'grpc', ev: 'pick-result', tracer: 'load_balancing_call', call: 7, pick: 'COMPLETE' },
  { src: 'grpc', ev: 'node-error', tracer: 'subchannel_call', call: 7, code: 'ECONNRESET' },
  { src: 'grpc', ev: 'status-details-received', tracer: 'subchannel_call', call: 7 },
  { src: 'grpc', ev: 'goaway-received', tracer: 'transport', transport: 1, address: 'non-loopback', code: 2 },
  { src: 'grpc', ev: 'session-error', tracer: 'transport', address: '127.0.0.1:5000' },
  { src: 'grpc', ev: 'remote-settings', tracer: 'transport', settings: { initialWindowSize: 65535 } },
  { src: 'grpc', ev: 'constructed', tracer: 'channel', channel: 1 },
  { src: 'grpc', ev: 'create-resolving-call', tracer: 'channel', call: 7, method: 'unrecognized', deadline: 'none' },
  { src: 'grpc', ev: 'create-load-balancing-call', tracer: 'channel', call: 8, method: '/briosa.DiscoveryService/GetServerInfo' },
  { src: 'grpc', ev: 'ping-failed', tracer: 'keepalive' },
  { src: 'grpc', ev: 'other', tracer: 'subchannel_call', call: 7 },
  { src: 'http2', ev: 'stream-start', stream: 1, method: '/briosa.DiscoveryService/GetServerInfo' },
  { src: 'http2', ev: 'response-headers', stream: 1, status: 200, endStream: false },
  { src: 'http2', ev: 'response-trailers', stream: 1, grpcStatus: 0 },
  { src: 'http2', ev: 'stream-close', stream: 1, rstCode: 0 },
  { src: 'http2', ev: 'stream-start', stream: 3, method: 'unrecognized' },
]) {
  check(has(wanted), `The capture has no record ${JSON.stringify(wanted)}.`);
}
check(!records.some((record) => record.tracer === 'channel_stacktrace'), 'An uncaptured tracer was recorded.');
const keys = new Set(records.flatMap((record) => Object.keys(record)));
for (const key of ['message', 'details', 'detail', 'peer', 'host', 'authority', 'path', 'metadata', 'headers', 'text', 'pid']) {
  check(!keys.has(key), `The capture recorded a '${key}' field.`);
}

// The preload alone, without the variable the runner sets, records nothing.
const unset = run(null);
check(unset.status === 0 && unset.stdout === 'report\n' && unset.stderr === `fixture-own-error-line ${marker}-own\n`,
  `Without the capture variable the fixture's output changed: ${JSON.stringify(unset.stderr)}`);
check(readFileSync(capture, 'utf8') === text && !existsSync(join(scratch, 'client-connection.2.jsonl')),
  'A run without the capture variable wrote a capture file.');

// A destination that cannot be written turns the capture off; the fixture runs as without it.
const unwritable = run(join(scratch, 'no-such-directory', 'client-connection.jsonl'));
check(unwritable.status === 0 && unwritable.stdout === 'report\n' && unwritable.stderr === `fixture-own-error-line ${marker}-own\n`,
  `An unwritable capture destination changed the fixture: ${unwritable.status} ${JSON.stringify(unwritable.stderr)}`);
check(!existsSync(join(scratch, 'no-such-directory')), 'An unwritable capture destination was created.');
console.log(`Connection capture module checks passed on Node ${process.versions.node}: ${records.length} records, no marker.`);
