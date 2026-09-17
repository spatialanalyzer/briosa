# Windows Control Center

- Status: Accepted initial design; implementation tracked by #181 under #158.
- Decision date: 2026-09-15 (maintainer approved the proposal and implementation).

## Process boundaries

The exact-target product ships a .NET 10 WPF application behind the root
`Briosa.ControlCenter.exe` launcher, using
Windows Forms NotifyIcon for the notification area. It runs in the signed-in
user's desktop session. The public API host and SDK worker remain separate
processes. The controller never activates COM, executes MPs, or owns an SDK
adapter. Closing a window hides it; exiting the controller leaves the API host
running. API shutdown leaves SpatialAnalyzer open.

Each controller monitors one server instance. Interactive packaged API hosts
automatically start a hidden monitoring companion; `Briosa:Desktop:Mode=Disabled`
suppresses integration for CI and headless deployment. A controller-owned launch
selects Owned mode, suppresses a second companion, and starts only the API host.
SDK startup, attachment, and recovery remain explicit.

## Ownership and management

The server exposes a randomly named, current-user-only Windows named pipe.
Messages are versioned, length-prefixed, and bounded to 64 KiB. Every request
names the exact random server instance. Status is available to local monitors;
management additionally requires a 256-bit per-launch ownership credential.
The controller passes that credential through the child's environment, never
command-line arguments. The host removes the variables before launching workers
or companions. The credential is not logged, displayed, or exported.

The private adapter delegates SDK actions to the same existing lifecycle
coordinator used by the public gRPC services, with the same generation guards,
typed errors, identity gates, and recovery behavior. Public protobuf state is
serialized inside the private envelope. Routing desktop mutations through this
instance-bound channel avoids a check-then-call race if another host acquires a
previous host's public TCP port. Public gRPC contracts remain unchanged.

A private StopServer request acknowledges the owner and invokes the host's
ordinary shutdown path. The existing supervisor closes admission and performs
bounded worker cleanup. The controller observes the exact owned process exit;
it does not infer shutdown from a lost pipe or force-kill processes by name.
If shutdown cannot be confirmed, restart stops and reports the uncertainty.

Ownership receipts live under the current user's protected ControlCenter
directory. Reopening the matching distribution may reclaim controller authority
only by proving the original credential to that exact live instance. PID plus
creation time and executable identity are additional liveness checks, never
shutdown authority. External/client-owned servers have no controller credential;
their companion is read-only. This mechanism operates within Briosa's existing
trusted-local-process boundary and is not isolation from a malicious same-user
process or administrator.

## Observation and recovery

The controller polls cached/read-only state on a bounded interval. Only a current
running-host snapshot with both authoritative ready flags produces Ready.
Lost observation clears Ready and management controls and labels retained values
stale. Monitoring never sends an MP probe or creates another SDK client.

Reconnect, recovery, and server restart are distinct operator actions. Recovery
creates a disconnected SDK generation and never replays an MP. Unknown command
completion remains visible independently of restored availability. A lifecycle
timeout is reported as unconfirmed and is not automatically retried.

## Activity and local data

Connection setup stores two independent, optional version/evidence-reference
pairs keyed by distribution path in the protected per-user directory. Editing
requires the selected host to be stopped. Saved pairs enter only future managed
host environments; empty pairs retain existing host configuration. References
are removed from worker environments and excluded from diagnostics/exports.
The SDK's observed process version wins over an attestation, including on a
mismatch. The controller never infers evidence from its own target or changes
SDK registration. Operators must recheck evidence when their SA environment changes.

The activity reader tails at most three files and 256 KiB per file, retains at
most 2,000 projected events, and accepts only schema-1 records for the selected
log instance and exact target. It handles partial writes and rotation. Unknown
events are omitted. Raw message text, exceptions, scopes, unknown properties, and
original records never enter the displayed or exported event model.

Support export explicitly projects version coordinates, manifest hash, safe
runtime state, stable codes, and recent curated events. It excludes the private
envelope, endpoint, paths, ownership receipts, configuration, environment,
credentials, raw exceptions, and command values. Exports are operator-selected
files; no upload occurs. Existing server log retention continues to apply.

Preferences and ownership state are stored outside immutable product payloads
in `%LOCALAPPDATA%\Briosa\ControlCenter\2024.1.0508.5`. The directory grants access
to the current user and SYSTEM. Notifications are opt-out, transition-based,
deduplicated, and rate-limited. Start-at-login is deferred.

## Distribution and validation

### Desktop appearance

The maintainer requested consistency with Briosa Installer. The native Fluent
control styles, brand palette mapping, and vector Layered Planes background are
adapted from `spatialanalyzer/briosa-installer` commit
`8577b56630cb7673ff2b1870ad7d35e4e0ede952` (`BrandTheme.cs`, `LayeredPlanes.cs`,
and `Styles.xaml`), under Apache-2.0. Only application namespaces/resource paths
and target-local presentation details differ. The approved brand assets remain
byte-exact, with provenance and Inter's font license retained.

Control Center has the same neutral sidebar, Inter typography, opaque cards,
deep-blue light theme, cyan accents in dark graphite, and Windows high-contrast
priority. Appearance can follow Windows or select Light/Dark in Details & support;
the choice persists outside the package. Compact windows stack the status cards.

Extend the existing self-contained target-qualified ZIP with the controller and
desktop runtime. The existing installer verifies and stores the complete payload;
installation does not launch it. Updates remain separate immutable products.
The root launcher shares the host's core runtime; the WPF executable and its
desktop runtime are isolated under `desktop/` to avoid mixed framework DLLs.
An early per-instance announcement lets a monitor retain a failed startup;
a clean-exit marker removes automatic monitors after normal shutdown.
Inactive private instance records expire after seven days on subsequent launches.
An active controller can keep files in use; close it and stop its server before
repair/removal. Uninstall never stops an engineering application or SA.

Ordinary validation uses portable presentation/redaction tests, fake workers,
private-channel ownership tests, WPF layout/automation smoke checks, and packaged
process tests. No real-SA claim follows from those tests. Licensed integration
requires separately authorized exact-target evidence.
