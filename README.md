# OblivionTravel

Standalone OB:MP map fast-travel control for the **default character/save system**. Version 0.1.0 is a buildable preview, not a validated discovery-persistence release. See [VERIFICATION.md](VERIFICATION.md) and [ACCEPTANCE.md](ACCEPTANCE.md).

## Exact map behavior

The default configuration submits `EnableFastTravel 0` after the default player restoration completes, a live local pawn and area exist, and the readiness delay passes. It reapplies after observed area/pawn/readiness changes and every 60 seconds. Failed command submissions retry after one second; warnings are limited to one per 30 seconds. Native command submission is not confirmation that the engine applied the setting.

`MapFastTravel` accepts `Block`, `Allow` (explicitly submits `EnableFastTravel 1`), or `Unmanaged` (submits nothing). `Enabled: false` also submits nothing. Configuration is read once at mod start. Unknown options, invalid intervals, and unsupported discovery modes disable the mod with a logged error. The example config documents all supported settings.

`Discovery: "PreserveNative"` preserves the map supplied by the default save/game. Existing visible and discovered destinations, including starting cities, stay as they are. Exploration continues using the game's native discovery behavior. The mod never hides markers, reveals all markers, changes per-marker travel flags, or creates its own approximation of discovery from player position. Fast-travel blocking does not require clearing discoveries.

There is **no separate map persistence store** and no custom character identity. The mod neither reads nor writes save files, calls save/load APIs, replaces saves, intercepts save ownership, nor changes autosave/disconnect handling. It does not import Characters maps. Native/default persistence is left to the host and is **not established as sufficient in rc.1**. A legacy client save registry still exists in the binary but is not exposed by the active DI container and cannot establish the current default save behavior. Cross-session discoveries may therefore fail to return even though this mod does not erase them. This requirement remains a release blocker, not an implemented persistence guarantee. No safe migration or full persistence adapter is shipped.

## Travel separation

Only the two fixed `EnableFastTravel` commands can reach the native adapter. No position, rotation, respawn, resurrection, door, scripted teleport, or server travel handler is changed. The setting controls the native map-travel UI path; it is not a server anti-cheat boundary. Other mods can change the same global setting. Allow does not reveal destinations or bypass the game's other travel restrictions.

Default save restoration is observed through a read-only rc.1 reflection adapter (`PlayerSaveStatusComponent.Applied` on the current local main character and `CurrentAreaId`). Missing or changed contracts defer native commands and log warnings. This is a version-sensitive integration point. The component is internal in rc.1; the adapter closes the public `EntityHandle.TryGetComponent<T>` read method over its inspected type. The mod never changes the component or invokes a save coordinator.

## Requirements and build

Windows x64, .NET SDK 10.0, C# 14, ReadyM/OB:MP **1.0.0-rc.1** client SDK and its UE4SS runtime. The SDK manifest reports `OblivionMP.SDK` 1.0.0; the minimumVersion cannot distinguish rc.1 from other builds. Other SDK builds are unsupported until checked. See [SDK-REQUIREMENTS.md](SDK-REQUIREMENTS.md) for inspected fingerprints and actual dependencies.

```powershell
./Build.ps1 -SdkDirectory 'C:\path\to\rc1\client\dlls' -Package
```

`SdkDirectory` must contain the client references listed in `Client/OblivionTravel.Client.csproj`, from an existing authorized SDK install. The script does not download or redistribute SDK binaries. The logging compile package is restored from NuGet. If packages are already cached and network access is unavailable, use `-Offline`; this explicitly disables NuGet vulnerability auditing during restore. The initial online restore here could not obtain advisory data.

Run just the pure automated checks without game/SDK files:

```powershell
dotnet run --project Tests/OblivionTravel.Tests.csproj -c Release
```

This runs 27 policy/config checks. Build.ps1 also passes `--sdk` to the test runner, adding seven metadata-only checks of the actual rc.1 status/accessor/area contracts (34 checks total). No host code is loaded or executed by those metadata checks.

`-Package` builds an allowlisted preview ZIP under `artifacts/`, including two mod DLLs, manifest, example-derived `config.json`, documentation and SHA-256 hashes. No SDK/game assets or private server configs are included. `Package.ps1` alone packages an already-built tree; use `Build.ps1 -Package` for a checked build.

## Manual installation for acceptance testing

No deployment or launch is automated. On a separate backed-up default-save test installation, place the ZIP's `OblivionTravel` directory under the host's mods directory, preserving `manifest.json` and `client/`. Confirm the host distributes the client payload and `config.json`; configuration is resolved from the modloader-provided `ModDirectory`, whose actual path is logged. This client-only package contains no server executable or server policy RPC.

Use `config.example.json` as the reference for editing that mod's `config.json`. Restart the mod/session to apply edits. Verify the effective policy and config path in logs. Do not install this alongside Characters' map module, its marker-hiding ESP, or another map-policy owner. No Characters/UI/MySQL mod is required.

On uninstall/disable, the mod does not guess the prior global fast-travel value. A setting already submitted can last for the current native game session. For an intentional switch, test the explicit Allow policy, or reload the native session as appropriate for the host. This does not change discovery flags.

## Scope and distribution

No server/game was launched, no deployment was performed, and no credentials/access permissions were created or changed during development. The original map reset, custom GUID sidecars, proximity catalogue, ESP and custom UI are intentionally absent. Attribution and licensing status are in [NOTICE.md](NOTICE.md). Finish all runtime gates before calling this production-ready or claiming cross-session native discovery persistence.
