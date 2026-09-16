# Changelog

## 0.1.2 - 2026-09-16

Simplified the local data setup.

SCP:SL now uses Northwood's own hoster mode directly and keeps its generated files in:

`serverfiles\AppData`

No junctions, no separate `ServerData` folder and no Windows user-profile path. The plugin creates the local AppData folder and makes sure `gamedir_for_configs: true` is enabled before LocalAdmin starts.

The short-lived 0.1.1 junction setup is also cleaned up automatically if the plugin finds the junction it created itself.

## 0.1.1 - 2026-09-16

SCP:SL was changed to keep its generated server data with the matching WindowsGSM instance instead of putting it in the Windows user profile.

This version used a separate `ServerData` folder with a junction from `serverfiles\AppData`. It was replaced by the simpler native hoster setup in 0.1.2.

## 0.1.0 - 2026-09-11

Initial MeFriendos SCP: Secret Laboratory WindowsGSM build.

- SteamCMD install/update support using App ID `996560`.
- Current LocalAdmin V2 startup handling.
- WindowsGSM Embedded Console support.
- Graceful shutdown through LocalAdmin's `exit` command.
- Firewall cleanup for broad `LocalAdmin.exe` and `SCPSL.exe` application rules.
- LabAPI support notes.
- No automatic EULA acceptance.

Based on `WindowsGSM.SCPSL` by Oscar Hurst (MIT).
