# Runtime acceptance checklist — not yet executed

Run on an isolated test server/client with the default character system. Back up saves and record the exact host/client build and mod list. This document is a manual checklist; it does not authorize or automate a launch/deployment.

- [ ] Loader resolves only OblivionTravel and OblivionMP.SDK. No Characters/UI/MySQL assembly is required. The effective configuration path and policy appear in logs.
- [ ] A pre-existing default character's known cities and legitimately discovered destinations remain visible. Capture the marker list/screenshots before and after; none may be removed.
- [ ] A fresh default character keeps the host/game's normal starting map. No reveal-all or empty-map behavior is introduced.
- [ ] With Block, clicking a discovered map destination cannot initiate native fast travel. Walking and normal discovery still work.
- [ ] With Allow, the same eligible destination is travelable; normal combat/interior restrictions remain. With Unmanaged/Enabled false no command is submitted.
- [ ] Walk to a previously unknown destination. Native discovery notification and map visibility occur, while Block still prevents map travel. Record actual flags/visible state, not only console submission logs.
- [ ] **Persistence release gate:** after native/default autosave, disconnect/rejoin, then clean server restart, the new discovery returns to the same default player and another player's map is unchanged. Repeat for existing discoveries. If it fails, stop release; this preview has no persistence workaround.
- [ ] **Default lifecycle gate:** reproduce death/respawn and a native reload of the default character. Inventory/skills/appearance/location remain owned by the default system; discoveries meet the preceding persistence gate.
- [ ] Server-authorized respawn, a scripted teleport, ordinary doors and server/admin travel still reach their intended destination. Map policy reapplies after readiness without delaying or altering those paths.
- [ ] Area transitions and pawn replacement reapply Block after the configured ready delay. No command runs before default restoration finishes or while the local character is dead/missing.
- [ ] Missing/changed reflection contract or missing UE4SS exports result in bounded warning logs and no marker/save writes. Validate the actual native ABI in the supported runtime.
- [ ] Config is delivered to the client, invalid config disables the mod, and switching to Allow/reloading clears the previous session's Block as documented. Verify disable/uninstall semantics.
- [ ] No new save store/account identifier is created by the mod. Compare default save lifecycle and other character data with the mod absent.

Release approval requires evidence for every applicable gate. Automated tests validate policy logic only; they cannot certify native command execution, default save isolation, discovery visibility/persistence, or teleport coexistence.
