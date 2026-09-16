<p align="center">
  <img src="SCPSL.cs/SCPSL.png" alt="SCP: Secret Laboratory" width="128">
</p>

<h1 align="center">WindowsGSM.SCPSL</h1>

<p align="center">
  MeFriendos build for running an SCP: Secret Laboratory dedicated server with WindowsGSM.
</p>

<p align="center">
  <a href="https://github.com/Raziel7893/WindowsGSM/releases/tag/v1.25.1.22"><img src="https://img.shields.io/badge/WindowsGSM-Raziel%20v1.25.1.22-38CDD4" alt="Raziel WindowsGSM v1.25.1.22"></a>
  <a href="CHANGELOG.md"><img src="https://img.shields.io/badge/version-0.1.0-C91F37" alt="Version 0.1.0"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="MIT License"></a>
</p>

This plugin installs, updates, starts and stops the official SCP: Secret Laboratory Dedicated Server through SteamCMD and Northwood's LocalAdmin V2. The MeFriendos build is based on the original `WindowsGSM.SCPSL` plugin by Oscar Hurst, but the runtime handling has been rewritten for current LocalAdmin V2 behavior and the hardened MeFriendos WindowsGSM setup.

## Features

- Installs and updates the official SCP:SL Dedicated Server through SteamCMD.
- Uses the current SteamCMD App ID `996560` with anonymous login.
- Starts `LocalAdmin.exe` from the server root, as required by Northwood.
- Passes the WindowsGSM **Server Port** as LocalAdmin's first positional argument.
- Validates the selected port before launch.
- Prevents accidentally specifying a second positional port in **Server Start Param**.
- Supports WindowsGSM Embedded Console with LocalAdmin V2.
- Adds `--printStd`, `--noSetCursor` and `--disableTrueColor` automatically while Embedded Console is enabled.
- Sends the official LocalAdmin `exit` command for graceful shutdown before falling back to a forced kill.
- Removes unrestricted Windows Firewall application exceptions for the exact `LocalAdmin.exe` and `SCPSL.exe` paths before startup.
- Leaves narrow manually configured UDP port rules untouched.
- Does **not** automatically accept Northwood's EULA.
- Does **not** automate SCP:SL server verification or Community Server Guideline acceptance.
- Is ready for the official LabAPI modding framework bundled with current dedicated-server builds.

## Quick overview

| Setting | Value |
| --- | --- |
| SteamCMD App ID | `996560` |
| SteamCMD login | Anonymous |
| Start executable | `LocalAdmin.exe` |
| Game executable | `SCPSL.exe` |
| Default game port | `7777/UDP` |
| Port increment | `1` |
| Default max players | `20` |
| Query method | None in WindowsGSM |
| Embedded Console | Supported through LocalAdmin V2 |
| Mod framework | LabAPI bundled by Northwood |
| Firewall | Manual UDP port rules only |

## Requirements

- Raziel7893/WindowsGSM v1.25.1.22 or a compatible WindowsGSM build
- 64-bit Windows supported by the installed SCP:SL Dedicated Server build
- Microsoft .NET Framework 4.8 and the current Visual C++ Redistributable required by SCP:SL
- Administrator rights for WindowsGSM when the MeFriendos firewall hardening needs to remove broad application exceptions

Northwood's public hosting guide currently lists Windows 8.1/10 and notes that Windows 11 is not officially supported but should work. Windows Server editions are not explicitly listed there, so server-host compatibility should be verified in your environment.

## Plugin installation

1. Download the release archive.
2. Extract the complete `SCPSL.cs` folder into `<WindowsGSM>\plugins\`.
3. Click **Reload Plugins** or restart WindowsGSM.
4. Add **SCP: Secret Laboratory Dedicated Server** and click **Install**.
5. Keep `7777` as **Server Port** or choose another unused UDP port.
6. Start the server once and review/accept Northwood's EULA yourself when LocalAdmin asks.
7. Configure the generated SCP:SL files under `%APPDATA%\SCP Secret Laboratory\config\<port>\`.
8. Create a narrow inbound UDP firewall rule for the selected game port.

The default **Server Start Param** is:

```text
--useDefault
```

`--useDefault` is a LocalAdmin V2 argument. It avoids the LocalAdmin configuration wizard when no LocalAdmin config exists. It does **not** accept the SCP:SL EULA.

## Server configuration

SCP:SL stores the gameplay configuration per port under:

```text
%APPDATA%\SCP Secret Laboratory\config\<port>\config_gameplay.txt
```

Important public-server fields include:

```text
server_name: MeFriendos | EU | DE/EN | 16+
server_ip: auto
max_players: 20
contact_email: YOUR_CONTACT_EMAIL
```

For a verified server, configure the remaining server-info and Community Server Guideline requirements according to Northwood's current documentation.

WindowsGSM's **Server Name** field is kept as the WindowsGSM instance label; SCP:SL's actual browser name is controlled by `server_name` in `config_gameplay.txt`.

## Ports and firewall

Current Northwood documentation requires the selected **UDP** game port. TCP is not required for the normal SCP:SL game connection.

Default:

| Purpose | Protocol | Port |
| --- | --- | --- |
| SCP:SL game traffic | UDP | `7777` |

For the MeFriendos firewall policy, create a narrow inbound rule for the chosen UDP port only.

The plugin removes broad Windows Firewall application exceptions for the exact paths of:

```text
LocalAdmin.exe
SCPSL.exe
```

If removal cannot be completed or verified, startup is stopped instead of silently continuing with unrestricted application access. Existing targeted port rules are not touched.

## Embedded Console

LocalAdmin V2 can mirror the game process stdout/stderr into its own output with `--printStd`. When **Embed Console** is enabled in WindowsGSM, the plugin automatically adds:

```text
--printStd --noSetCursor --disableTrueColor
```

These options make LocalAdmin's output more suitable for WindowsGSM redirection while preserving command input through stdin.

Your own **Server Start Param** remains appended after those automatically generated arguments. If you already supplied one of these flags, the plugin does not add a duplicate.

### EULA note

The plugin intentionally does not add LocalAdmin's `--acceptEULA` flag. Accepting the EULA is a decision for the server operator.

Read the SCP:SL EULA first. If you have accepted it and intentionally want LocalAdmin's non-interactive acceptance mechanism, add the official LocalAdmin argument yourself.

## Graceful shutdown

Northwood LocalAdmin V2 exposes `exit` as the command that stops the server.

The plugin therefore uses this sequence:

1. If stdin is redirected for Embedded Console, write `exit` to LocalAdmin and wait up to 20 seconds.
2. Otherwise send `exit` to the native LocalAdmin console window and wait up to 20 seconds.
3. Use `Process.Kill()` only as a last resort if LocalAdmin does not exit.

This is safer than killing `SCPSL.exe` directly because LocalAdmin remains responsible for the game process lifecycle.

## LabAPI / plugins

Current SCP:SL Dedicated Server builds include Northwood's official **LabAPI** framework. This WindowsGSM plugin does not download or modify LabAPI itself.

Typical Windows LabAPI locations are under:

```text
%APPDATA%\SCP Secret Laboratory\LabAPI\
```

Use only plugins compatible with the current SCP:SL/LabAPI version and review their permissions and data handling before deployment.

## Verification readiness

This plugin only handles WindowsGSM installation and process lifecycle. It does not grant or request SCP:SL server verification.

Before applying for Northwood verification, configure at minimum the public server identity/contact fields, make the server reachable on its UDP port, review the current Community Server Guidelines, and complete Northwood's verification process separately.

## Testing checklist

- WindowsGSM loads `SCPSL.cs` without a plugin error.
- Install and Update complete through SteamCMD App ID `996560`.
- `LocalAdmin.exe` and `SCPSL.exe` exist after installation.
- Invalid or empty Server Port values are rejected.
- A second positional port in Server Start Param is rejected.
- LocalAdmin starts with the selected WindowsGSM Server Port as its first argument.
- The first start does not silently accept Northwood's EULA.
- With Embed Console enabled, LocalAdmin output is visible in WindowsGSM.
- With Embed Console enabled, the server process stdout/stderr is surfaced through `--printStd`.
- Stop sends `exit` before any forced termination.
- No broad LocalAdmin.exe or SCPSL.exe application firewall exception remains after startup.
- A narrow manual `<port>/UDP` firewall rule remains untouched.
- SCP:SL creates/uses its per-port configuration under `%APPDATA%\SCP Secret Laboratory\config\<port>\`.

## Project links

- Source: https://github.com/PapaGordon/WindowsGSM.SCPSL-MeFriendos
- Original WindowsGSM plugin: https://github.com/Voidoz/WindowsGSM.SCPSL
- SCP:SL technical wiki: https://techwiki.scpslgame.com/
- LocalAdmin V2: https://github.com/northwood-studios/LocalAdmin-V2
- LabAPI: https://github.com/northwood-studios/LabAPI
- WindowsGSM: https://github.com/Raziel7893/WindowsGSM
- Community: https://mefriendos.de

This is an independent community plugin. It is not affiliated with or endorsed by Northwood Studios, SCP: Secret Laboratory, WindowsGSM, Oscar Hurst, or any third-party plugin developer.

## License

The original WindowsGSM.SCPSL plugin by Oscar Hurst was released under the MIT License. The original copyright and permission notice are retained in [LICENSE](LICENSE), together with the MeFriendos modification notice.
