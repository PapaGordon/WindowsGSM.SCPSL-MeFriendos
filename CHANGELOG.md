# Changelog

All notable changes to the MeFriendos build are documented here.

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
