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
            version = "0.1.1",
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

            // Northwood's hoster policy makes LocalAdmin, SCP:SL and LabAPI use a local
            // "AppData" directory. Keep that directory outside serverfiles through a
            // per-instance junction so all generated data stays beside the game files.
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
                // LocalAdmin V2 accepts stdin when redirected and can mirror SCPSL stdout/stderr
                // into its own stream through --printStd. This gives WindowsGSM useful live output
                // while still allowing a graceful "exit" command on shutdown.
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

                // Preferred path for WindowsGSM Embedded Console.
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

                // Native LocalAdmin console fallback.
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

                // Last resort only. LocalAdmin's official "exit" command is always attempted first.
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

            // LocalAdmin requires the port as its first positional argument. Prevent users from
            // accidentally replacing it with another positional port in Server Start Param.
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
                DirectoryInfo serverFilesDirectory = new DirectoryInfo(serverFiles);
                if (serverFilesDirectory.Parent == null)
                {
                    error = $"Could not determine the WindowsGSM server directory from: {serverFiles}";
                    return false;
                }

                string serverRoot = serverFilesDirectory.Parent.FullName;
                string serverData = Path.Combine(serverRoot, "ServerData");
                string appDataLink = Path.Combine(serverFiles, "AppData");
                string hosterPolicy = Path.Combine(serverFiles, "hoster_policy.txt");

                Directory.CreateDirectory(serverData);
                EnsureHosterPolicy(hosterPolicy);

                if (Directory.Exists(appDataLink))
                {
                    FileAttributes attributes = File.GetAttributes(appDataLink);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        if (!JunctionPointsToDirectory(appDataLink, serverData))
                        {
                            error = $"SCP:SL AppData junction already exists but does not point to this server's ServerData directory: {appDataLink}";
                            return false;
                        }

                        return true;
                    }

                    if (Directory.GetFileSystemEntries(appDataLink).Length != 0)
                    {
                        error = $"SCP:SL local AppData already exists and is not managed by this plugin: {appDataLink}. Move or remove it before starting so ServerData can be linked safely.";
                        return false;
                    }

                    Directory.Delete(appDataLink);
                }
                else if (File.Exists(appDataLink))
                {
                    error = $"Cannot create SCP:SL local AppData because a file already exists at: {appDataLink}";
                    return false;
                }

                return CreateDirectoryJunction(appDataLink, serverData, out error);
            }
            catch (UnauthorizedAccessException e)
            {
                error = $"Could not prepare SCP:SL ServerData. Start WindowsGSM as administrator. {e.Message}";
                return false;
            }
            catch (Exception e)
            {
                error = $"Could not prepare SCP:SL ServerData: {e.Message}";
                return false;
            }
        }

        private static void EnsureHosterPolicy(string hosterPolicyPath)
        {
            string policy = File.Exists(hosterPolicyPath)
                ? File.ReadAllText(hosterPolicyPath)
                : string.Empty;

            if (Regex.IsMatch(
                policy,
                @"(?im)^\s*gamedir_for_configs\s*:\s*true\s*$"))
            {
                return;
            }

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

        private static bool CreateDirectoryJunction(string linkPath, string targetPath, out string error)
        {
            error = null;

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/d /c mklink /J \"{linkPath}\" \"{targetPath}\"",
                WorkingDirectory = Path.GetDirectoryName(linkPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using (Process junctionProcess = Process.Start(startInfo))
                {
                    if (junctionProcess == null)
                    {
                        error = $"Could not create the ServerData junction: {linkPath}";
                        return false;
                    }

                    string standardOutput = junctionProcess.StandardOutput.ReadToEnd();
                    string standardError = junctionProcess.StandardError.ReadToEnd();
                    junctionProcess.WaitForExit();

                    if (junctionProcess.ExitCode != 0)
                    {
                        string details = string.IsNullOrWhiteSpace(standardError)
                            ? standardOutput.Trim()
                            : standardError.Trim();

                        error = $"Could not create the ServerData junction from \"{linkPath}\" to \"{targetPath}\". {details}";
                        return false;
                    }
                }

                if (!Directory.Exists(linkPath) ||
                    (File.GetAttributes(linkPath) & FileAttributes.ReparsePoint) == 0 ||
                    !JunctionPointsToDirectory(linkPath, targetPath))
                {
                    error = $"ServerData junction creation could not be verified: {linkPath}";
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                error = $"Could not create the ServerData junction: {e.Message}";
                return false;
            }
        }

        private string BuildParameters(bool embedConsole)
        {
            var parameters = new StringBuilder();
            parameters.Append((_serverData.ServerPort ?? string.Empty).Trim());

            string serverParameters = (_serverData.ServerParam ?? string.Empty).Trim();

            // These are LocalAdmin V2 flags, not SCPSL.exe flags. They are added only when
            // WindowsGSM Embedded Console is enabled and never override explicit user values.
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

                    // Reacquire and verify after removal instead of assuming success.
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
