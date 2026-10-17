# Supported SDK and dependency evidence

Inspected 2026-10-17 using the refreshed `mods-1.0/ArenaMod/Dependencies/Client` reference set and its `SDK-RC1-MIGRATION.md`. Built with .NET SDK 10.0.400. Target: Windows x64 ReadyM/OB:MP 1.0.0-rc.1; C# 14 extension properties are used for the public local main-character pawn.

The manifest dependency remains `OblivionMP.SDK >= 1.0.0` because rc.1 still reports 1.0.0 metadata. This loader minimum is not a claim of compatibility with every newer or older build. Revalidate the reflection/native adapters if any relevant host binary changes.

Direct compile references are explicitly listed in `Client/OblivionTravel.Client.csproj`; all host references have `Private=false`. Logging abstractions 10.0.8 is compile-only (`ExcludeAssets=runtime`); the host must supply the compatible logging API. Core uses only .NET. No NuGet gameplay library, LiteNetLib/RPC, database, Harmony or UI library is needed.

The built client DLL's actual assembly references were read back: `OblivionTravel.Core` 0.1.0.0, `ReadyM.Sdk.Common`, `ReadyM.Api`, `ReadyM.Modloader`, `ReadyM.SDK`, `OblivionMp.Sdk`, `OblivionMp.Sdk.Common` (all 1.0.0.0), `Microsoft.Extensions.Logging.Abstractions` 10.0.0.0 (package 10.0.8), and .NET 10 System.Runtime/Collections/Runtime.InteropServices/Linq/Memory. The Core DLL references only .NET 10 System.Runtime/Collections/Text.Json. Other project SDK references supply transitive compile types; none are copied. The host's own SDK still requires its normal transitive runtime files.

Inspected SHA-256 client reference fingerprints:

| Assembly | Assembly version | SHA-256 |
| --- | --- | --- |
| OblivionMp.Sdk.dll | 1.0.0.0 | `C7CF696D19758216E1C7DE7A13B2D71A25DC95517E5857EB3CDAF07871D1FAFC` |
| OblivionMp.Sdk.Common.dll | 1.0.0.0 | `72653C5D8CB579ABCBD0E029C02C2E5BFC070BB5F11C0D2B372867CC178008B9` |
| OblivionMpCSharpMod.dll | 0.2.0.0 | `651375E71FDCB74111DFE418F14A1A0C8E5EBF40503A44AEAAA43D59574E494B` |
| ReadyM.Api.dll | 1.0.0.0 | `EE573492C105293E6A87B15C8B01C051153B1D6C98604998FD41F6C05E2DDDB1` |
| ReadyM.Api.Multiplayer.dll | 1.0.0.0 | `CB1774446E41F6E215A16B295F11F6F3CC0ECEFFC288CF6DDCD5ECA264B92DF2` |
| ReadyM.Modloader.dll | 1.0.0.0 | `1C39CB237CFE837B9BBF7DB14EBB313F9CC3B839884CB2C9E151007EEB13865E` |
| ReadyM.Sdk.Common.dll | 1.0.0.0 | `064C55CB961BD20E31EBBEF076410DE84E5D280F7EFC053465E0AFD1F25E1821` |
| ReadyM.SDK.dll | 1.0.0.0 | `DF2288E67691A3C54341BF995BA580D21B28B13044EE3B7C12F04DF1784E4323` |
| ReadyM.SDK.Client.dll | 1.0.0.0 | `3CD082D711C4CF3DC908CB318A24B2E75F9620150755A97DDBE6B221E8D19D31` |
| ReadyM.Relay.Common.Oblivion.dll | 1.0.0.0 | `5558A757C5E803A5826B1FB7A7BC680600E732FBE92F6D8436B802B164ADE201` |
| Yooni.Native.Container.dll | 1.0.0.0 | `44E0C690F8CC666EED062D62B3D738CE3E255BB8934717A58AE3ACF491BC6A26` |

`OblivionMpCSharpMod` and `ReadyM.Api.Multiplayer` are inspected/resolved at runtime for the read-only readiness contract, not copied or statically referenced by the project. UE4SS.dll must export the C++ wrappers bound in `NativeConsole.cs`. The adapter reflects parameter names, widths and bounds rather than embedding game property offsets. This ABI is not exercised by an offline build.

## Default save findings and uncertainty

The active rc.1 readiness signal is the internal `ReadyM.Api.Multiplayer.ECS.Components.PlayerSaveStatusComponent.Applied` on the current local character. The adapter reflects its type, calls the public generic `EntityHandle.TryGetComponent<T>` read method, and reads Applied. It never changes the component. Area readiness comes from DI.OblivionClientState.CurrentAreaId. Automated metadata checks verify these exact members without executing host code.

Legacy `PlayerSaveComponents`/`PlayerSaveCoordinator` types still exist in the binary. The active DI container does not expose DI.PlayerSave; those legacy registrations cannot establish the active default save behavior and are not used by this mod. The existing source's HostSaveGuard/HostComponents confirms the rc.1 Applied signal. The inspected `CellDiscoveryManager` concerns replicated actor/spawner/leveled-list discovery, not map icons.

These observations do not establish how every native map bit is persisted by the full game/host. No supported public map-discovery snapshot/restore contract was identified. No default-save character GUID/slot API is assumed. Retaining native visibility is safe; promising cross-session discovery persistence requires further host/native API evidence and the acceptance tests. The custom Characters tmm/reset/GUID design is not compatible with this ownership constraint.
