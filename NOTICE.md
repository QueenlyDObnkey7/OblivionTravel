# Attribution and source provenance

The source concept is the user's Oblivion Characters project, manifest author **QueenlyDonkey**. This standalone adaptation was requested by its owner. The fixed `EnableFastTravel` command strategy and narrow UE4SS console approach derive from inspection of `Client/MapClient.cs`, `Client/NativeScene.cs`, and `Client/UnrealBridge.cs`. OblivionTravel's configuration, scheduling, tests, and limited adapter were authored for this extraction. Original attribution is retained here; no third-party relicensing is asserted.

Inspected source roots (read-only during this work):

- `Desktop/mods-1.0/OblivionCharacters` (SDK migration snapshot).
- `Documents/Codex/2026-10-16/task/combined-tree-0432-stage/OblivionCharacters` (verified integration reference).

SHA-256 provenance:

| File | mods-1.0 | combined-tree-0432-stage |
| --- | --- | --- |
| Client/MapClient.cs | `4D31CE8F6F9D2391E66E4B59E7CB5E497DD8EEF1D62D5A66C63552525C934488` | same |
| Core/MapDiscovery.cs | `4804E6F0809E4C033F7BF46FD38931837277A841A05BBF20030965875C3972D9` | same |
| Server/CharacterSystem.Maps.cs | `A7399A6E9839FE32C62E576DC755476DD1001D382A32BF9E055AB331282C2FFE` | same |
| Client/UnrealBridge.cs | `545673FE35DCB9CED7E2019E201789C8BAEE2A669977DB6E5AAD8BC932AA354E` | `391B4131D2C3114F5FD0A60237F1073EE48C9255E27A39974501A63A3ADB9191` |

The map policy sources match across both references; the larger bridge differs. OblivionTravel does not copy that larger bridge or its unrelated UI, asset, save, and inventory functionality. It uses only the inspected native console contract with reflected parameter bounds.

No root LICENSE/COPYING file was found for the inspected Characters project. No permissive/open-source license has been invented for this extraction. Keep distribution private under the owner's authorization pending a licensing decision. SDK libraries, UE4SS, and the game retain their respective owners' rights and must be obtained independently. Their binaries and decompiled code are not included in the tracked project or release ZIP.

Excluded: secrets, credentials, deployment/server configs, saves, custom character sidecars, databases, game assets, extracted marker datasets, ESP/ESM/PAK files, SDK binaries, Harmony, custom UI and Characters assemblies.
