# Verification status — 2026-10-17

- Local independent Git repository initialized with branch `main`.
- Release client build against the inspected rc.1 reference set: passed (0 warnings, 0 errors on the final offline rebuild).
- Automated runner: 34 checks passed (27 policy/config checks and 7 metadata checks of the actual rc.1 readiness contracts). Coverage includes restore/readiness gating, area/pawn transitions, death/revival, retry/backoff, periodic policy, config rejection and fixed map-only commands. Pure tests without the optional SDK path run the 27 policy/config checks.
- Source/package audit: required by `Build.ps1`; only allowlisted mod DLLs and text docs/config are packaged. SDK binaries, game assets, saves, private configs and inspection decompilations are excluded from Git and the ZIP.
- Actual emitted DLL assembly references were read back and recorded in SDK-REQUIREMENTS.md. The client references no Characters/UI/MySQL/RPC assembly.
- Initial online package restore reported NU1900 because NuGet vulnerability data was unreachable. Offline rebuild explicitly skips advisory lookup; it is not a vulnerability audit.
- No game/server launch, native command execution, installation, deployment, or save migration was performed.

**Not production-ready:** all native runtime checks in ACCEPTANCE.md remain pending. Cross-session native map-discovery persistence with the default save system has not been implemented or verified through a supported API. This preview preserves native map state and default save ownership, but cannot certify that rc.1 saves discoveries. That gap blocks the requested full persistence release.

The build/package scripts do not create repositories, push, publish releases, install mods, start games or alter credentials. Source upload, if authorized, is a separate Git operation; this verification document does not imply it happened.
