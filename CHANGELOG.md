# Changelog

## 0.1.1 - 2026-09-16

SCP:SL now keeps its generated server data with the matching WindowsGSM instance instead of putting it in the Windows user profile.

For example, server ID 12 will use:

`F:\WindowsGSM\servers\12\ServerData`

The plugin sets up Northwood's hoster policy automatically and links the local `serverfiles\AppData` folder to that server-specific data folder. This also keeps LocalAdmin and LabAPI data with the server.

No migration code was added because this change is intended for a new server setup. The plugin also avoids touching an existing non-empty `serverfiles\AppData` folder if one is already there.

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
