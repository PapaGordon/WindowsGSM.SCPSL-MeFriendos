WindowsGSM.SCPSL - MeFriendos build
Version 0.1.2

INSTALL
=======
1. Copy the complete SCPSL.cs folder into:
   <WindowsGSM>\plugins\SCPSL.cs\

2. Reload plugins or restart WindowsGSM.

3. Add "SCP: Secret Laboratory Dedicated Server" and click Install.
   SteamCMD App ID: 996560
   SteamCMD login: anonymous

4. Keep the default Server Port 7777 unless you intentionally use another UDP port.

5. First start / EULA:
   The plugin does NOT auto-accept Northwood's EULA.
   Read the EULA and accept it through LocalAdmin yourself.

6. SCP:SL data stays with the WindowsGSM server files.
   The plugin uses Northwood's own hoster mode and stores the generated data in:

   <WindowsGSM>\servers\<server ID>\serverfiles\AppData\

   Example for server ID 12:
   F:\WindowsGSM\servers\12\serverfiles\AppData\

   Main gameplay config:
   serverfiles\AppData\config\<port>\config_gameplay.txt

   LabAPI:
   serverfiles\AppData\SCP Secret Laboratory\LabAPI\

7. For a public server, create a narrow firewall rule for the selected UDP port only.
   Default: 7777/UDP
   Do not create a broad application allow rule for LocalAdmin.exe or SCPSL.exe.

NOTES
=====
- The plugin automatically creates serverfiles\AppData and enables Northwood's
  gamedir_for_configs hoster policy.
- No junctions or Windows user-profile redirection are used.
- WindowsGSM's Server Name field does not directly set SCP:SL's public server name.
  Set server_name in config_gameplay.txt.
- LabAPI is bundled with current SCP:SL Dedicated Server builds.
- Server Start Param is for LocalAdmin V2 arguments. Do not put the port there;
  WindowsGSM passes Server Port as LocalAdmin's first positional argument.
- When Embed Console is enabled, the plugin adds:
    --printStd --noSetCursor --disableTrueColor
  unless you already supplied those flags.
- The default plugin parameter is:
    --useDefault
  This avoids the LocalAdmin configuration wizard when no LocalAdmin config exists.
- The plugin does not automate server verification or acceptance of community guidelines.
