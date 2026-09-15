# Briosa Control Center

The Windows package includes a notification-area companion and an on-demand
status window for SpatialAnalyzer 2026.1.0529.7.

## Open or start

- Start `Briosa.ControlCenter.exe` to start an owned API host in the background.
- Use `Briosa.ControlCenter.exe --show` to open the window first. Select **Start
  server** when needed. A matching running instance is reused for observation.
- An interactive packaged `Briosa.Server.exe` launch automatically starts a
  hidden monitor. This also applies to client-library launches. Its original
  launcher retains ownership, so the monitor cannot stop it or change its SDK.
- Right-click the notification-area icon to open status, activity, copy the
  endpoint, or access actions permitted by ownership and current state.
- Windows may put the icon in the notification-area overflow. Its tooltip
  identifies the target and current status.

Closing the window hides it. **Exit tray application** leaves the API host running.
Reopen the same distribution to observe it again. **Stop server** stops the owned
API host and its worker; SpatialAnalyzer remains open.

## Become ready

Starting the API host does not start SpatialAnalyzer or its SDK. Open the
separately installed and licensed exact-target SpatialAnalyzer application,
select **Start SDK**, and then **Connect**. Existing server configuration supplies
the worker, endpoint, operation policy, and independent identity evidence.
Control Center does not infer identity from installed files or edit attestations.

Only **Ready for commands** means that current execution readiness is established.
Inspect **Overview** for attachment, SDK/SA evidence sources and match states,
recovery requirements, and the latest observation time. **Status unavailable**
means retained details are stale; the controller does not keep showing Ready.

## Recovery and shutdown

- **Reconnect** reuses a healthy SDK generation when connection state permits.
- **Recover SDK** replaces a faulted generation without connecting or replaying.
  Correct competing-client/environment problems first, then connect explicitly.
- **Restart server** waits for confirmed owned-process shutdown before launching
  a replacement from the same distribution and startup configuration sources.
  Existing clients must establish a new runtime generation.
- A timed-out action is unconfirmed. Refresh before acting again; no lifecycle
  action or MP is automatically retried.
- Unknown command completion remains a separate incident even after readiness
  returns. Inspect SpatialAnalyzer before deciding whether a command should run
  again. Recovery does not undo or determine an earlier command's effects.

The controller requests graceful shutdown and never kills an unowned process.
Windows logoff/shutdown may interrupt the session; do not treat that as confirmed
MP cancellation. Restart explicitly after establishing the intended environment.

## Activity and support

**Activity** renders curated metadata from the existing per-instance JSONL logs.
Use search, severity filters, pause, and **Jump to latest**. Select rows and use
Ctrl+C to copy them. Correlation IDs connect RPC observations with later execution
observations; an RPC timeout is not a terminal SDK outcome.

The viewer keeps a bounded recent history. Logs are best effort and can be lost
through crashes, quota limits, or provider failure. Missing logs do not establish
whether a command ran. File diagnostics can be disabled or inaccessible while
the API remains available.

**Details & support** provides offline package diagnostics, endpoint copying,
notification preferences, an app theme selector, and a sanitized ZIP export.
Appearance matches Briosa Installer: select Windows, Light, or Dark; Windows
high-contrast settings take priority. Exports contain safe
version/state metadata and recent curated activity. They exclude endpoint
addresses, paths, configuration, credentials, raw exceptions, and MP values.
Keep or delete exported files according to your own support-retention policy.

## Headless operation and packaging

Set `Briosa:Desktop:Mode` to `Disabled`, for example:

```powershell
./Briosa.Server.exe --Briosa:Desktop:Mode=Disabled
```

The equivalent environment variable is `Briosa__Desktop__Mode=Disabled`.
`Auto` is the default and only launches a companion when its executable is
present and the server runs in an interactive Windows session. `Owned` is
reserved for the controller's private launch handshake.

Keep the complete package together. Use the existing Briosa installer to acquire,
verify, repair, update, or remove it. Installation is inert. Each update remains
a separate exact-target product, and a running server is never replaced by an
automatic update. Close the controller and stop its owned server before removal.
Per-user preferences live outside the package under
`%LOCALAPPDATA%\Briosa\ControlCenter\2026.1.0529.7`; package removal preserves them.

This initial implementation is validated with fake workers and Windows package/UI
tests. Real SpatialAnalyzer startup/connection/recovery through the desktop flow
requires separate licensed validation before it is claimed as release evidence.
