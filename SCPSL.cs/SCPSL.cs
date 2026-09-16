using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;

namespace WindowsGSM.Plugins
{
    // MeFriendos build based on WindowsGSM.SCPSL by Oscar Hurst.
    // Updated for Northwood LocalAdmin V2 and the current SCP:SL Dedicated Server.
    public class SCPSL : SteamCMDAgent
    {
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.SCPSL",
            author = "MeFriendos",
            description = "WindowsGSM plugin for SCP: Secret Laboratory Dedicated Server (MeFriendos build)",
            version = "0.1.2",
            url = "https://github.com/PapaGordon/WindowsGSM.SCPSL-MeFriendos",
            color = "#C91F37"
        };

        public override bool loginAnonymous => true;
        public override string AppId => "996560";
        public override string StartPath => @"LocalAdmin.exe";

        public SCPSL(ServerConfig serverData) : base(serverData)
        {
            base.serverData = _serverData = serverData;
        }

        private readonly ServerConfig _serverData;

        public string FullName = "SCP: Secret Laboratory Dedicated Server";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public object QueryMethod = null;

        public string Port = "7777";
        public string QueryPort = "7777";
        public string Defaultmap = "";
        public string Maxplayers = "20";

        // LocalAdmin V2 parameters. The server port is inserted automatically as the first argument.
        // --useDefault avoids the LocalAdmin configuration wizard when no LocalAdmin config exists yet.
        // EULA acceptance is intentionally NOT automated. Read and accept Northwood's EULA yourself.
        public string Additional = "--useDefault";

        public Task<Process> Start()
        {
            string serverFiles = ServerPath.GetServersServerFiles(_serverData.ServerID);
            string localAdminPath = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            string gamePath = ServerPath.GetServersServerFiles(_serverData.ServerID, "SCPSL.exe");

            if (!File.Exists(localAdminPath))
            {
                Error = $"File not found: {localAdminPath}";
                return Task.FromResult<Process>(null);
            }

            if (!File.Exists(gamePath))
            {
                Error = $"File not found: {gamePath}";
                return Task.FromResult<Process>(null);
            }

            if (!ValidateConfiguration(out string validationError))
            {
                Error = validationError;
                return Task.FromResult<Process>(null);
            }

            // Use Northwood's own hoster mode. This keeps LocalAdmin, SCP:SL and LabAPI
            // data in serverfiles\AppData instead of the Windows user profile.
            if (!PrepareServerLocalData(serverFiles, out string dataError))
            {
                Error = dataError;
                return Task.FromResult<Process>(null);
            }

            // Match the hardened MeFriendos WindowsGSM policy: do not leave unrestricted
            // application firewall exceptions behind. Targeted UDP port rules stay untouched.
            if (!RemoveAutomaticBroadFirewallRules(localAdminPath, gamePath))
            {
                Error = "Automatic firewall access could not be disabled. Start WindowsGSM as administrator or remove the broad LocalAdmin.exe / SCPSL.exe application rules manually.";
                return Task.FromResult<Process>(null);
            }

            bool embedConsole = _serverData.EmbedConsole;
            string parameters = BuildParameters(embedConsole);

            var process = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = serverFiles,
                    FileName = localAdminPath,
                    Arguments = parameters,
                    WindowStyle = embedConsole ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    CreateNoWindow = embedConsole
                },
                EnableRaisingEvents = true
            };

            if (embedConsole)
            {
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                var serverConsole = new ServerConsole(_serverData.ServerID);
                process.OutputDataReceived += serverConsole.AddOutput;
                process.ErrorDataReceived += serverConsole.AddOutput;
            }

            try
            {
                process.Start();

                if (embedConsole)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                return Task.FromResult(process);
            }
            catch (FileNotFoundException e)
            {
                Error = $"File not found: {e.Message}";
            }
            catch (UnauthorizedAccessException e)
            {
                Error = $"Access denied: {e.Message}";
            }
            catch (Exception e)
            {
                Error = e.Message;
            }

            process.Dispose();
            return Task.FromResult<Process>(null);
        }

        public async Task Stop(Process process)
        {
            if (process == null)
                return;

            await Task.Run(() =>
            {
                try
                {
                    process.Refresh();
                    if (process.HasExited)
                        return;
                }
                catch
                {
                    return;
                }

                try
                {
                    if (process.StartInfo.RedirectStandardInput)
                    {
                        process.StandardInput.WriteLine("exit");
                        process.StandardInput.Flush();

                        if (process.WaitForExit(20000))
                            return;
                    }
                }
                catch
                {
                }

                try
                {
                    process.Refresh();
                    if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                    {
                        ServerConsole.SendMessageToMainWindow(process.MainWindowHandle, "exit");
                        if (process.WaitForExit(20000))
                            return;
                    }
                }
                catch
                {
                }

                try
                {
                    process.Refresh();
                    if (!process.HasExited)
                        process.Kill();
                }
                catch
                {
                }
            });
        }

        private bool ValidateConfiguration(out string error)
        {
            error = null;

            if (!ushort.TryParse(_serverData.ServerPort, out ushort port) || port == 0)
            {
                error = "SCP:SL Server Port must be a number between 1 and 65535.";
                return false;
            }

            string extra = (_serverData.ServerParam ?? string.Empty).Trim();
            if (Regex.IsMatch(extra, @"^\d{1,5}(?:\s|$)"))
            {
                error = "Do not put the SCP:SL port in Server Start Param. WindowsGSM already passes Server Port as LocalAdmin's first argument.";
                return false;
            }

            return true;
        }

        private static bool PrepareServerLocalData(string serverFiles, out string error)
        {
            error = null;

            try
            {
                string appDataPath = Path.Combine(serverFiles, "AppData");
                string hosterPolicyPath = Path.Combine(serverFiles, "hoster_policy.txt");

                // v0.1.1 used a junction to a sibling ServerData directory. If that exact
                // plugin-created junction is still present, replace it with a normal AppData folder.
                if (Directory.Exists(appDataPath))
                {
                    FileAttributes attributes = File.GetAttributes(appDataPath);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        DirectoryInfo serverFilesDirectory = new DirectoryInfo(serverFiles);
                        string oldServerData = serverFilesDirectory.Parent == null
                            ? null
                            : Path.Combine(serverFilesDirectory.Parent.FullName, "ServerData");

                        if (string.IsNullOrWhiteSpace(oldServerData) ||
                            !Directory.Exists(oldServerData) ||
                            !JunctionPointsToDirectory(appDataPath, oldServerData))
                        {
                            error = $"serverfiles\\AppData is a junction that is not managed by this plugin: {appDataPath}";
                            return false;
                        }

                        Directory.Delete(appDataPath);
                    }
                }
                else if (File.Exists(appDataPath))
                {
                    error = $"Cannot create SCP:SL local AppData because a file already exists at: {appDataPath}";
                    return false;
                }

                Directory.CreateDirectory(appDataPath);
                EnsureHosterPolicy(hosterPolicyPath);
                return true;
            }
            catch (UnauthorizedAccessException e)
            {
                error = $"Could not prepare SCP:SL local AppData. Start WindowsGSM as administrator. {e.Message}";
                return false;
            }
            catch (Exception e)
            {
                error = $"Could not prepare SCP:SL local AppData: {e.Message}";
                return false;
            }
        }

        private static void EnsureHosterPolicy(string hosterPolicyPath)
        {
            string policy = File.Exists(hosterPolicyPath)
                ? File.ReadAllText(hosterPolicyPath)
                : string.Empty;

            if (Regex.IsMatch(policy, @"(?im)^\s*gamedir_for_configs\s*:\s*true\s*$"))
                return;

            if (!string.IsNullOrEmpty(policy) &&
                !policy.EndsWith("\r\n", StringComparison.Ordinal) &&
                !policy.EndsWith("\n", StringComparison.Ordinal))
            {
                policy += Environment.NewLine;
            }

            policy += "gamedir_for_configs: true" + Environment.NewLine;
            File.WriteAllText(hosterPolicyPath, policy);
        }

        private static bool JunctionPointsToDirectory(string linkPath, string targetPath)
        {
            string probeName = ".wgsm-scpsl-" + Guid.NewGuid().ToString("N") + ".tmp";
            string targetProbe = Path.Combine(targetPath, probeName);
            string linkProbe = Path.Combine(linkPath, probeName);

            try
            {
                File.WriteAllText(targetProbe, string.Empty);
                return File.Exists(linkProbe);
            }
            finally
            {
                try
                {
                    if (File.Exists(targetProbe))
                        File.Delete(targetProbe);
                }
                catch
                {
                }
            }
        }

        private string BuildParameters(bool embedConsole)
        {
            var parameters = new StringBuilder();
            parameters.Append((_serverData.ServerPort ?? string.Empty).Trim());

            string serverParameters = (_serverData.ServerParam ?? string.Empty).Trim();

            if (embedConsole)
            {
                AppendFlagIfMissing(parameters, serverParameters, "--printStd");
                AppendFlagIfMissing(parameters, serverParameters, "--noSetCursor");
                AppendFlagIfMissing(parameters, serverParameters, "--disableTrueColor");
            }

            if (!string.IsNullOrWhiteSpace(serverParameters))
            {
                parameters.Append(' ');
                parameters.Append(serverParameters);
            }

            return parameters.ToString();
        }

        private static void AppendFlagIfMissing(StringBuilder parameters, string userParameters, string flag)
        {
            if (!HasArgument(userParameters, flag))
            {
                parameters.Append(' ');
                parameters.Append(flag);
            }
        }

        private static bool HasArgument(string arguments, string name)
        {
            if (string.IsNullOrWhiteSpace(arguments))
                return false;

            return Regex.IsMatch(
                arguments,
                $@"(?:^|\s){Regex.Escape(name)}(?:\s|$)",
                RegexOptions.IgnoreCase
            );
        }

        private static bool RemoveAutomaticBroadFirewallRules(params string[] programPaths)
        {
            try
            {
                Type managerType = Type.GetTypeFromProgID("HNetCfg.FwMgr");
                if (managerType == null)
                    return false;

                object manager = Activator.CreateInstance(managerType);
                object localPolicy = manager.GetType().InvokeMember(
                    "LocalPolicy", BindingFlags.GetProperty, null, manager, null);
                object currentProfile = localPolicy.GetType().InvokeMember(
                    "CurrentProfile", BindingFlags.GetProperty, null, localPolicy, null);

                foreach (string programPath in programPaths)
                {
                    if (string.IsNullOrWhiteSpace(programPath))
                        continue;

                    object applications = currentProfile.GetType().InvokeMember(
                        "AuthorizedApplications", BindingFlags.GetProperty, null, currentProfile, null);

                    IEnumerable entries = applications as IEnumerable;
                    if (entries == null)
                        return false;

                    bool found = false;
                    foreach (object application in entries)
                    {
                        string applicationPath = Convert.ToString(application.GetType().InvokeMember(
                            "ProcessImageFileName", BindingFlags.GetProperty, null, application, null));

                        if (string.Equals(applicationPath, programPath, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        continue;

                    applications.GetType().InvokeMember(
                        "Remove", BindingFlags.InvokeMethod, null, applications, new object[] { programPath });

                    applications = currentProfile.GetType().InvokeMember(
                        "AuthorizedApplications", BindingFlags.GetProperty, null, currentProfile, null);
                    entries = applications as IEnumerable;
                    if (entries == null)
                        return false;

                    foreach (object application in entries)
                    {
                        string applicationPath = Convert.ToString(application.GetType().InvokeMember(
                            "ProcessImageFileName", BindingFlags.GetProperty, null, application, null));

                        if (string.Equals(applicationPath, programPath, StringComparison.OrdinalIgnoreCase))
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
