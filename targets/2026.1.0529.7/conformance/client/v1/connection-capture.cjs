'use strict';
// Client side of the conformance soak's HTTP/2 connection capture (#450).
//
// The conformance runner preloads this file into a JavaScript fixture with
// NODE_OPTIONS=--require, and only when it runs from a soak package built with
// -ConnectionCapture. It records what the client's HTTP/2 transport did, to the
// file named by BRIOSA_CONFORMANCE_CLIENT_CAPTURE, which the runner keeps only
// when the scenario fails. It never writes to the fixture's stdout or stderr,
// never reads them, and never changes what the client sends or receives.
//
// Two sources, both inside this process:
//   1. Node's own HTTP/2 diagnostics channels (http2.client.stream.*), and passive
//      listeners on the streams and sessions they name: stream IDs, the method
//      path, the response's :status and grpc-status, the stream's reset code,
//      and the session's window and queue sizes.
//   2. grpc-js's trace calls. Its logging module's exported trace function is
//      wrapped on the module instance the client loads, as that module finishes
//      loading. No GRPC_TRACE or GRPC_VERBOSITY variable is set, so grpc-js
//      writes nothing more to stderr than without this file; its trace text
//      reaches only this file's reducer, one complete call at a time.
//
// Every record is built from fixed names and validated values: times, the
// source, an event name, stream, call, channel and session numbers, a gRPC
// method path, status and reset codes, lengths, counts, window sizes, settings
// values, connectivity states and loopback endpoints. A trace text that matches
// no pattern below is recorded as "other" with its tracer name and nothing of
// its text. No header value other than the method path and status codes, no
// metadata, message, status detail, error message, peer address beyond a
// loopback endpoint, stack, path or process ID is ever written.
//
// Everything here is best effort: an internal failure records "capture-error"
// once and stops the source that failed; it never throws into the client.

const fs = require('node:fs');
const path = require('node:path');
const Module = require('node:module');
const diagnostics = require('node:diagnostics_channel');
const { performance } = require('node:perf_hooks');

const SCHEMA = 1;
const MAX_RECORDS = 20000;
const MAX_BYTES = 8 * 1024 * 1024;
const SAMPLE_MS = 5000;

const target = process.env.BRIOSA_CONFORMANCE_CLIENT_CAPTURE;

// The tracers whose calls are reduced into records. Any other tracer stays as
// grpc-js configured it, which without GRPC_TRACE is off.
const TRACERS = new Set([
  'channel', 'connectivity_state', 'subchannel', 'pick_first', 'transport', 'transport_flowctrl',
  'transport_internals', 'keepalive', 'resolving_call', 'retrying_call', 'load_balancing_call', 'subchannel_call',
]);

const STATES = new Set(['IDLE', 'CONNECTING', 'READY', 'TRANSIENT_FAILURE', 'SHUTDOWN']);
const PICKS = new Set(['COMPLETE', 'QUEUE', 'TRANSIENT_FAILURE', 'DROP']);
const SETTINGS = ['headerTableSize', 'enablePush', 'initialWindowSize', 'maxFrameSize', 'maxConcurrentStreams',
  'maxHeaderListSize', 'maxHeaderSize', 'enableConnectProtocol'];
const SESSION_STATE = ['effectiveLocalWindowSize', 'effectiveRecvDataLength', 'nextStreamID', 'localWindowSize',
  'lastProcStreamID', 'remoteWindowSize', 'outboundQueueSize'];
const STREAM_STATE = ['localWindowSize', 'state', 'localClose', 'remoteClose'];

function number(value) {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= 0 ? value : null;
}

function digits(text) {
  return typeof text === 'string' && /^[0-9]{1,15}$/.test(text) ? Number(text) : null;
}

// A gRPC method path is the client's own protocol name; anything else is withheld.
function method(text) {
  return typeof text === 'string' && text.length <= 160 &&
    /^\/[A-Za-z_][A-Za-z0-9_.]*\/[A-Za-z_][A-Za-z0-9_]*$/.test(text) ? text : 'unrecognized';
}

function endpoint(text) {
  return typeof text === 'string' && /^(?:dns:|ipv4:)?(?:127\.0\.0\.1|localhost|\[::1\]):[0-9]{1,5}$/.test(text)
    ? text.replace(/^(?:dns:|ipv4:)/, '') : 'non-loopback';
}

function state(text) {
  return STATES.has(text) ? text : 'unrecognized';
}

// grpc-js's own internal state names, such as COMMITTED or PROCESSED.
function word(text) {
  return /^[A-Z][A-Z_]{0,19}$/.test(text) ? text : 'unrecognized';
}

function deadline(text) {
  if (text === 'Infinity') return 'none';
  return /^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{3}Z$/.test(text) ? text : 'unrecognized';
}

// --- The capture file. ---

let fd = null;
let disabled = !(typeof target === 'string' && path.isAbsolute(target) && /\.jsonl$/.test(target));
let records = 0;
let bytes = 0;
const pending = [];
let flushScheduled = false;
const started = performance.now();

function open() {
  // The first process of the fixture that records owns the named file; another,
  // such as a second Node process, writes beside it instead of interleaving.
  for (let index = 1; index <= 4 && fd === null; index++) {
    const file = index === 1 ? target : target.replace(/\.jsonl$/, `.${index}.jsonl`);
    try {
      fd = fs.openSync(file, 'wx');
    } catch (error) {
      if (!error || error.code !== 'EEXIST') break;
    }
  }
  if (fd === null) disabled = true;
}

function flush() {
  flushScheduled = false;
  if (fd === null || pending.length === 0) return;
  const text = pending.join('');
  pending.length = 0;
  try {
    fs.writeSync(fd, text);
  } catch {
    disabled = true;
  }
}

// Records written in one turn of the event loop reach the file together, at its
// end, so a forced kill loses at most that turn.
function record(source, event, fields) {
  if (disabled) return;
  try {
    if (fd === null) {
      open();
      if (disabled) return;
      write('capture', 'start', { schema: SCHEMA, node: /^[0-9]+\.[0-9]+\.[0-9]+$/.test(process.versions.node) ? process.versions.node : 'unrecognized' });
    }
    write(source, event, fields);
  } catch {
    disabled = true;
  }
}

function write(source, event, fields) {
  if (records >= MAX_RECORDS || bytes >= MAX_BYTES) {
    if (records !== Infinity) {
      pending.push(JSON.stringify({ t: new Date().toISOString(), ms: elapsed(), src: 'capture', ev: 'limit-reached' }) + '\n');
      records = Infinity;
      schedule();
    }
    return;
  }
  const line = JSON.stringify({ t: new Date().toISOString(), ms: elapsed(), src: source, ev: event, ...fields }) + '\n';
  records++;
  bytes += line.length;
  pending.push(line);
  schedule();
}

function schedule() {
  if (flushScheduled) return;
  flushScheduled = true;
  setImmediate(flush).unref();
}

function elapsed() {
  return Math.round((performance.now() - started) * 1000) / 1000;
}

const failed = new Set();
function guard(source, action) {
  if (failed.has(source)) return;
  try {
    action();
  } catch {
    failed.add(source);
    record('capture', 'capture-error', { source });
  }
}

// --- Node's HTTP/2 transport. ---

const sessions = new WeakMap();
const liveSessions = new Set();
let sessionCount = 0;
const streamInfo = new WeakMap();
let lastCreated = null;

function sessionNumber(session) {
  if (!session || typeof session !== 'object') return null;
  let entry = sessions.get(session);
  if (entry) return entry.number;
  entry = { number: ++sessionCount, streams: new Set() };
  sessions.set(session, entry);
  liveSessions.add(session);
  const number = entry.number;
  record('http2', 'session-seen', { session: number, connecting: session.connecting === true });
  session.on('goaway', (code, lastStreamId) => guard('http2', () =>
    record('http2', 'session-goaway', { session: number, code: numberOrNull(code), lastStream: numberOrNull(lastStreamId) })));
  session.on('frameError', (type, code, id) => guard('http2', () =>
    record('http2', 'session-frame-error', { session: number, frameType: numberOrNull(type), code: numberOrNull(code), stream: numberOrNull(id) })));
  session.on('ping', () => guard('http2', () => record('http2', 'session-ping-received', { session: number })));
  session.on('remoteSettings', (settings) => guard('http2', () =>
    record('http2', 'session-remote-settings', { session: number, settings: pickNumbers(settings, SETTINGS) })));
  session.on('localSettings', (settings) => guard('http2', () =>
    record('http2', 'session-local-settings', { session: number, settings: pickNumbers(settings, SETTINGS) })));
  session.once('close', () => guard('http2', () => {
    liveSessions.delete(session);
    record('http2', 'session-close', { session: number });
  }));
  return number;
}

function numberOrNull(value) {
  return number(value);
}

function pickNumbers(source, keys) {
  const result = {};
  if (!source || typeof source !== 'object') return result;
  for (const key of keys) {
    const value = source[key];
    if (typeof value === 'boolean') result[key] = value;
    else if (number(value) !== null) result[key] = value;
  }
  return result;
}

function track(stream, headers) {
  let info = streamInfo.get(stream);
  if (info) return info;
  info = {
    session: sessionNumber(stream.session),
    method: method(headers && headers[':path']),
    opened: performance.now(),
    call: null,
  };
  streamInfo.set(stream, info);
  const entry = stream.session && sessions.get(stream.session);
  if (entry) entry.streams.add(stream);
  // Passive listeners only: none of these changes how the stream flows.
  stream.once('trailers', (trailers) => guard('http2', () =>
    record('http2', 'response-trailers', { session: info.session, stream: number(stream.id), grpcStatus: digits(trailers && trailers['grpc-status']) })));
  stream.once('end', () => guard('http2', () =>
    record('http2', 'response-read-end', { session: info.session, stream: number(stream.id) })));
  stream.once('finish', () => guard('http2', () =>
    record('http2', 'request-write-end', { session: info.session, stream: number(stream.id) })));
  stream.once('aborted', () => guard('http2', () =>
    record('http2', 'stream-aborted', { session: info.session, stream: number(stream.id) })));
  return info;
}

function subscribe(name, handler) {
  diagnostics.subscribe(name, (message) => guard('http2', () => handler(message || {})));
}

if (!disabled) {
  subscribe('http2.client.stream.created', ({ stream, headers }) => {
    if (!stream) return;
    const info = track(stream, headers);
    lastCreated = stream;
    record('http2', 'stream-created', { session: info.session, stream: number(stream.id), method: info.method });
  });
  subscribe('http2.client.stream.start', ({ stream, headers }) => {
    if (!stream) return;
    const info = track(stream, headers);
    record('http2', 'stream-start', { session: info.session, stream: number(stream.id), method: info.method });
    if (info.call !== null) bind(info.call, stream, info);
  });
  subscribe('http2.client.stream.finish', ({ stream, headers, flags }) => {
    if (!stream) return;
    const info = track(stream, null);
    record('http2', 'response-headers', {
      session: info.session, stream: number(stream.id), status: digits(String(headers && headers[':status'])),
      grpcStatus: digits(headers && headers['grpc-status']), endStream: (number(flags) & 1) === 1,
    });
  });
  subscribe('http2.client.stream.error', ({ stream, error }) => {
    if (!stream) return;
    const info = track(stream, null);
    const code = error && typeof error.code === 'string' && /^[A-Z0-9_]{1,64}$/.test(error.code) ? error.code : 'unrecognized';
    record('http2', 'stream-error', { session: info.session, stream: number(stream.id), code });
  });
  subscribe('http2.client.stream.close', ({ stream }) => {
    if (!stream) return;
    const info = track(stream, null);
    const entry = stream.session && sessions.get(stream.session);
    if (entry) entry.streams.delete(stream);
    record('http2', 'stream-close', { session: info.session, stream: number(stream.id), rstCode: number(stream.rstCode) });
  });

  // While a stream stays open, the session's windows and outbound queue every few
  // seconds, from nghttp2's own state: what a stalled call's connection held. The
  // socket's byte counters are not used: HTTP/2 writes bypass them.
  setInterval(() => guard('sample', () => {
    const now = performance.now();
    for (const session of liveSessions) {
      const entry = sessions.get(session);
      const open = entry ? [...entry.streams].filter((stream) => {
        const info = streamInfo.get(stream);
        return info && now - info.opened >= 2000;
      }) : [];
      if (open.length === 0) continue;
      let sessionState = {};
      try {
        sessionState = pickNumbers(session.state, SESSION_STATE);
      } catch {
        sessionState = {};
      }
      record('http2', 'session-sample', {
        session: entry.number, openStreams: open.length, state: sessionState,
        streams: open.slice(0, 8).map((stream) => {
          let streamState = {};
          try {
            streamState = pickNumbers(stream.state, STREAM_STATE);
          } catch {
            streamState = {};
          }
          const info = streamInfo.get(stream);
          return { stream: number(stream.id), call: info.call, method: info.method, openMs: Math.round(now - info.opened), state: streamState };
        }),
      });
    }
  }), SAMPLE_MS).unref();
}

function bind(call, stream, info) {
  if (number(stream.id) === null) return;
  record('capture', 'call-stream', { call, session: info.session, stream: stream.id, method: info.method });
}

// --- grpc-js's trace calls. ---

// The method of each recent call number, to check a stream's binding; bounded.
const callMethods = new Map();
const CALL_METHOD_LIMIT = 4096;
function rememberMethod(callNumber, value) {
  callMethods.set(callNumber, value);
  if (callMethods.size > CALL_METHOD_LIMIT) callMethods.delete(callMethods.keys().next().value);
}

// Each pattern takes a complete trace text, after grpc-js's own prefix of a
// channel, subchannel or call number, and returns the event name and fields, or
// null. Only the captured groups are kept; the rest of the text never is.
const prefixed = /^\((\d{1,9})\) (\S{1,200}) ([\s\S]*)$/;
const call = /^\[(\d{1,9})\] ([\s\S]*)$/;

function reduce(tracer, text) {
  let match;
  switch (tracer) {
    case 'channel':
      if (!(match = prefixed.exec(text))) return null;
      return channel(Number(match[1]), match[3]);
    case 'connectivity_state':
      if (!(match = prefixed.exec(text))) return null;
      if ((match = /^(\w{1,20}) -> (\w{1,20})$/.exec(match[3]))) return ['state', { from: state(match[1]), to: state(match[2]) }];
      return null;
    case 'subchannel': {
      if (!(match = prefixed.exec(text))) return null;
      const fields = { subchannel: Number(match[1]), address: endpoint(match[2]) };
      const rest = match[3];
      if ((match = /^(\w{1,20}) -> (\w{1,20})$/.exec(rest))) return ['state', { ...fields, from: state(match[1]), to: state(match[2]) }];
      if (/^Subchannel constructed with options /.test(rest)) return ['constructed', fields];
      return ['other', fields];
    }
    case 'pick_first':
      if ((match = /^(\w{1,20}) -> (\w{1,20})$/.exec(text))) return ['state', { from: state(match[1]), to: state(match[2]) }];
      return null;
    case 'transport':
      return transport(text);
    case 'transport_flowctrl':
      if (!(match = prefixed.exec(text))) return null;
      if ((match = /^local window size: (\d{1,12}) remote window size: (\d{1,12})$/.exec(match[3]))) {
        return ['window', { local: Number(match[1]), remote: Number(match[2]) }];
      }
      return null;
    case 'transport_internals':
      if (!(match = prefixed.exec(text))) return null;
      if ((match = /^session\.closed=(true|false) session\.destroyed=(true|false) session\.socket\.destroyed=(true|false)$/.exec(match[3]))) {
        return ['session', { closed: match[1] === 'true', destroyed: match[2] === 'true', socketDestroyed: match[3] === 'true' }];
      }
      return null;
    case 'keepalive':
      if (!(match = prefixed.exec(text))) return null;
      return keepalive(match[3]);
    case 'subchannel_call':
      if (!(match = call.exec(text))) return null;
      return withCall(Number(match[1]), subchannelCall(match[2]));
    case 'resolving_call':
    case 'retrying_call':
    case 'load_balancing_call':
      if (!(match = call.exec(text))) return null;
      return withCall(Number(match[1]), callEvent(tracer, Number(match[1]), match[2]));
    default:
      return null;
  }
}

function withCall(number, reduced) {
  if (!reduced) return ['other', { call: number }];
  return [reduced[0], { call: number, ...reduced[1] }];
}

function channel(id, rest) {
  let match;
  if ((match = /^createResolvingCall \[(\d{1,9})\] method="([^"]{0,200})", deadline=(\S{1,40})$/.exec(rest))) {
    rememberMethod(Number(match[1]), method(match[2]));
    return ['create-resolving-call', { channel: id, call: Number(match[1]), method: method(match[2]), deadline: deadline(match[3]) }];
  }
  if ((match = /^create(Retrying|LoadBalancing)Call \[(\d{1,9})\] method="([^"]{0,200})"$/.exec(rest))) {
    rememberMethod(Number(match[2]), method(match[3]));
    return [match[1] === 'Retrying' ? 'create-retrying-call' : 'create-load-balancing-call',
      { channel: id, call: Number(match[2]), method: method(match[3]) }];
  }
  if (/^Channel constructed with options /.test(rest)) return ['constructed', { channel: id }];
  if ((match = /^callRefTimer\.(ref|unref) \| configSelectionQueue\.length=(\d{1,9}) pickQueue\.length=(\d{1,9})$/.exec(rest))) {
    return ['call-ref-timer', { channel: id, ref: match[1] === 'ref', configQueue: Number(match[2]), pickQueue: Number(match[3]) }];
  }
  if ((match = /^Idle timer triggered after (\d{1,12})ms of inactivity$/.exec(rest))) return ['idle', { channel: id, idleMs: Number(match[1]) }];
  if (/^Name resolution failed with calls queued for config selection$/.test(rest)) return ['resolution-failed', { channel: id }];
  return ['other', { channel: id }];
}

function transport(text) {
  // grpc-js writes two shapes: "(id) address text" from a transport and
  // "target address text" from its connector.
  let match = /^(?:\((\d{1,9})\)|(\S{1,200})) (\S{1,200}) ([\s\S]*)$/.exec(text);
  if (!match) return null;
  const fields = { address: endpoint(match[3]) };
  if (match[1] !== undefined) fields.transport = Number(match[1]);
  const rest = match[4];
  if (rest === 'session closed') return ['session-closed', fields];
  if ((match = /^connection closed by GOAWAY with code (\d{1,9}) and data /.exec(rest))) return ['goaway-received', { ...fields, code: Number(match[1]) }];
  if (/^connection closed with error /.test(rest)) return ['session-error', fields];
  if ((match = /^connection closed\. hadError=(true|false)$/.exec(rest))) return ['socket-closed', { ...fields, hadError: match[1] === 'true' }];
  if ((match = /^(new settings received|local settings acknowledged by remote)( on the old connection)?: (\{[^{}]{0,1000}\})$/.exec(rest))) {
    let settings = {};
    try {
      settings = pickNumbers(JSON.parse(match[3]), SETTINGS);
    } catch {
      settings = {};
    }
    return [match[1] === 'new settings received' ? 'remote-settings' : 'local-settings-acknowledged',
      { ...fields, oldConnection: match[2] !== undefined, settings }];
  }
  if (/^connection failed with error /.test(rest)) return ['connect-failed', fields];
  if (rest === 'Waiting for secureConnector to be ready') return ['connector-waiting', fields];
  if (rest === 'secureConnector is ready') return ['connector-ready', fields];
  if (rest === 'Established TCP connection') return ['tcp-established', fields];
  if (rest === 'Established secure connection') return ['secure-established', fields];
  return ['other', fields];
}

function keepalive(rest) {
  let match;
  if ((match = /^Sending ping with timeout (\d{1,12})ms$/.exec(rest))) return ['ping-sent', { timeoutMs: Number(match[1]) }];
  if (rest === 'Ping timeout passed without response') return ['ping-timeout', {}];
  if (/^Ping failed with error /.test(rest)) return ['ping-failed', {}];
  if (rest === 'Received ping response') return ['ping-response', {}];
  if (/^Ping send failed: /.test(rest)) return ['ping-send-failed', {}];
  if ((match = /^Starting keepalive timer for (\d{1,12})ms$/.exec(rest))) return ['timer-started', { delayMs: Number(match[1]) }];
  return ['other', {}];
}

function subchannelCall(rest) {
  let match;
  if (/^Received server headers:\n/.test(rest)) {
    match = /^\t\t:status: (\d{3})$/m.exec(rest);
    return ['response-headers', { status: match ? Number(match[1]) : null }];
  }
  if (/^Received server trailers:\n/.test(rest)) {
    match = /^\t\tgrpc-status: (\d{1,2})$/m.exec(rest);
    return ['response-trailers', { grpcStatus: match ? Number(match[1]) : null }];
  }
  if ((match = /^receive HTTP\/2 data frame of length (\d{1,12})$/.exec(rest))) return ['data-received', { length: Number(match[1]) }];
  if ((match = /^parsed message of length (\d{1,12})$/.exec(rest))) return ['message-parsed', { length: Number(match[1]) }];
  if ((match = /^HTTP\/2 stream closed with code (\d{1,9})$/.exec(rest))) return ['stream-closed', { rstCode: Number(match[1]) }];
  if ((match = /^Node error event: message=[\s\S]* code=([A-Z0-9_]{1,64}) errno=([A-Z0-9_]{1,64}|undefined|Unknown system error -?\d{1,9}) syscall=([a-z]{1,32}|undefined)$/.exec(rest))) {
    return ['node-error', { code: match[1] }];
  }
  if (/^Node error event: /.test(rest)) return ['node-error', { code: 'unrecognized' }];
  if ((match = /^ended with status: code=(\d{1,2}) details=/.exec(rest))) return ['ended', { code: Number(match[1]) }];
  if ((match = /^pushing to reader message of length (\d{1,12})$/.exec(rest))) return ['message-pushed', { length: Number(match[1]) }];
  if ((match = /^unpushedReadMessages\.push message of length (\d{1,12})$/.exec(rest))) return ['message-queued', { length: Number(match[1]) }];
  if ((match = /^received status code (\d{1,2}) from server$/.exec(rest))) return ['status-received', { code: Number(match[1]) }];
  if (/^received status details string "/.test(rest)) return ['status-details-received', {}];
  if ((match = /^close http2 stream with code (\d{1,9})$/.exec(rest))) return ['stream-close-requested', { rstCode: Number(match[1]) }];
  if ((match = /^cancelWithStatus code: (\d{1,2}) details: /.exec(rest))) return ['cancel-with-status', { code: Number(match[1]) }];
  if ((match = /^write\(\) called with message of length (\d{1,12})$/.exec(rest))) return ['write', { length: Number(match[1]) }];
  if ((match = /^sending data chunk of length (\d{1,12})$/.exec(rest))) return ['data-chunk-sent', { length: Number(match[1]) }];
  if (rest === 'end() called') return ['end-called', {}];
  if (rest === 'calling end() on HTTP/2 stream') return ['stream-end-called', {}];
  return null;
}

function callEvent(tracer, number, rest) {
  let match;
  if ((match = /^ended with status: code=(\d{1,2}) details=/.exec(rest))) return ['ended', { code: Number(match[1]) }];
  if ((match = /^cancelWithStatus code: (\d{1,2}) details: /.exec(rest))) return ['cancel-with-status', { code: Number(match[1]) }];
  if ((match = /^write\(\) called with message of length (\d{1,12})$/.exec(rest))) return ['write', { length: Number(match[1]) }];
  if ((match = /^Pick result: (\w{1,20}) subchannel: /.exec(rest))) return ['pick-result', { pick: PICKS.has(match[1]) ? match[1] : 'unrecognized' }];
  if ((match = /^Picked subchannel [\s\S]* has state (\w{1,20}) after getting credentials metadata\. Retrying pick$/.exec(rest))) {
    return ['pick-retry', { state: state(match[1]) }];
  }
  if (/^Failed to start call on picked subchannel /.test(rest)) return ['start-failed', {}];
  if ((match = /^Created child call \[(\d{1,9})\]( for attempt (\d{1,9}))?$/.exec(rest))) {
    const child = Number(match[1]);
    if (tracer === 'load_balancing_call') bindCreated(child);
    return ['child-created', { child, attempt: match[3] === undefined ? null : Number(match[3]) }];
  }
  if ((match = /^Created child \[(\d{1,9})\]$/.exec(rest))) return ['child-created', { child: Number(match[1]) }];
  if ((match = /^Deadline: (\S{1,40})$/.exec(rest))) return ['deadline', { deadline: deadline(match[1]) }];
  if ((match = /^Deadline will be reached in (\d{1,12})ms$/.exec(rest))) return ['deadline-timer', { inMs: Number(match[1]) }];
  if (/^Propagating deadline from parent: /.test(rest)) return ['deadline-propagated', {}];
  if ((match = /^Committing call \[(\d{1,9})\] at index (\d{1,9})$/.exec(rest))) return ['commit', { child: Number(match[1]), index: Number(match[2]) }];
  if ((match = /^state=(\w{1,20}) handling status with progress (\w{1,20}) from child \[(\d{1,9})\] in state (\w{1,20})$/.exec(rest))) {
    return ['status-handling', { state: word(match[1]), progress: word(match[2]), child: Number(match[3]), childState: word(match[4]) }];
  }
  if ((match = /^Received (metadata|message|status) from child \[(\d{1,9})\]$/.exec(rest))) {
    return [`child-${match[1]}`, { child: Number(match[2]) }];
  }
  if ((match = /^Sending halfClose immediately (after message )?to child \[(\d{1,9})\] - /.exec(rest))) {
    return ['half-close-child', { child: Number(match[2]), afterMessage: match[1] !== undefined }];
  }
  const fixed = {
    'Pick called': 'pick-called', 'Received metadata': 'received-metadata', 'Received message': 'received-message',
    'Received status': 'received-status', 'Created': 'created', 'start called': 'start-called',
    'startRead called': 'start-read-called', 'halfClose called': 'half-close-called',
    'Finished filtering received message': 'message-filtered', 'Retry attempt denied by throttling policy': 'retry-throttled',
    'Credentials metadata generation finished after call ended': 'credentials-after-end',
  };
  if (Object.prototype.hasOwnProperty.call(fixed, rest)) return [fixed[rest], {}];
  return null;
}

// grpc-js creates the HTTP/2 stream inside the load-balancing call and traces the
// child call right after, in the same synchronous turn, so the stream created last
// is that call's stream.
function bindCreated(child) {
  const stream = lastCreated;
  lastCreated = null;
  if (!stream) return;
  const info = streamInfo.get(stream);
  if (!info || info.call !== null) return;
  const expected = callMethods.get(child);
  if (expected !== undefined && expected !== info.method) return;
  info.call = child;
  bind(child, stream, info);
}

const hooked = new WeakSet();
let instances = 0;

function install(exports, filename) {
  if (!exports || typeof exports !== 'object' || hooked.has(exports)) return;
  const originalTrace = exports.trace;
  const originalEnabled = exports.isTracerEnabled;
  if (typeof originalTrace !== 'function' || typeof originalEnabled !== 'function') {
    record('capture', 'grpc-js-not-hooked', {});
    return;
  }
  const wrappedEnabled = (tracer) => TRACERS.has(tracer) || originalEnabled(tracer);
  const wrappedTrace = (severity, tracer, text) => {
    if (TRACERS.has(tracer)) {
      guard('grpc', () => {
        const reduced = typeof text === 'string' ? reduce(tracer, text) : null;
        record('grpc', reduced ? reduced[0] : 'other', { tracer, ...(reduced ? reduced[1] : {}) });
      });
    }
    // A tracer the environment turned on is still written as grpc-js writes it.
    if (originalEnabled(tracer)) originalTrace(severity, tracer, text);
  };
  exports.isTracerEnabled = wrappedEnabled;
  exports.trace = wrappedTrace;
  if (exports.trace !== wrappedTrace || exports.isTracerEnabled !== wrappedEnabled) {
    record('capture', 'grpc-js-not-hooked', {});
    return;
  }
  hooked.add(exports);
  instances++;
  let version = 'unrecognized';
  try {
    const manifest = JSON.parse(fs.readFileSync(path.join(path.dirname(filename), '..', '..', 'package.json'), 'utf8'));
    if (manifest.name === '@grpc/grpc-js' && /^[0-9]+\.[0-9]+\.[0-9]+$/.test(manifest.version)) version = manifest.version;
  } catch {
    version = 'unrecognized';
  }
  record('capture', 'grpc-js-hooked', { instance: instances, version });
}

const loggingModule = /[\\/]@grpc[\\/]grpc-js[\\/]build[\\/]src[\\/]logging\.js$/;

if (!disabled) {
  // Every CommonJS module passes Module.prototype._compile, whichever loader asked
  // for it (require, import of CommonJS, or tsx's loader). Only grpc-js's logging
  // module is touched, after it has run and before any other grpc-js module can
  // call it.
  const originalCompile = Module.prototype._compile;
  Module.prototype._compile = function compile(content, filename, ...rest) {
    const result = originalCompile.call(this, content, filename, ...rest);
    if (typeof filename === 'string' && loggingModule.test(filename)) {
      guard('hook', () => install(this.exports, filename));
    }
    return result;
  };
  process.on('exit', () => {
    try {
      flush();
    } catch {
      // Nothing can be reported this late.
    }
  });
}
