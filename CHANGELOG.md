# Changelog

All notable changes to the MeFriendos build are documented here.

## 0.1.1 - 2026-09-16

### Changed
- Keeps SCP:SL runtime data inside the matching WindowsGSM server instance instead of the Windows user profile.
- Uses Northwood's `gamedir_for_configs: true` hoster policy so LocalAdmin, SCP:SL and LabAPI use a local `AppData` directory.
- Stores that local data in `<WindowsGSM>\servers\<server ID>\ServerData` and links `serverfiles\AppData` to it with an NTFS junction.
- Creates and verifies the per-instance data junction automatically before LocalAdmin starts.
- Refuses to overwrite an existing unmanaged `serverfiles\AppData` directory containing data.

## 0.1.0 - 2026-09-11

### Added
- Initial MeFriendos SCP: Secret Laboratory WindowsGSM build.
- SteamCMD installation/update support using App ID `996560` and anonymous login.
- Current Northwood LocalAdmin V2 startup format with the server port as the first positional argument.
- Server-port validation and duplicate positional-port protection.
- WindowsGSM Embedded Console support using LocalAdmin V2 `--printStd`, `--noSetCursor` and `--disableTrueColor`.
- Graceful LocalAdmin shutdown via the official `exit` command with a forced-kill fallback only as a last resort.
- MeFriendos firewall hardening for broad `LocalAdmin.exe` and `SCPSL.exe` application exceptions while preserving narrow manual port rules.
- Documentation for the per-port `%APPDATA%` configuration location.
- LabAPI notes for current SCP:SL server-side modding.
- Explicit protection against silently accepting Northwood's EULA from the plugin defaults.

### Based on
- `WindowsGSM.SCPSL` by Oscar Hurst (MIT), originally published in 2022.
