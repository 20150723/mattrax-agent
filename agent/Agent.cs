using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;

namespace agent
{
    class Agent
    {
        static string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MattraxAgent", "Data");
        static StreamWriter globalLog = new StreamWriter(Path.Combine(dataFolder, "agent.log"), append: true);
        static double PowershellExecutionTimeout = 15; // In Minutes

        static void Main(string[] args)
        {
            Console.WriteLine("Agent running...");
            globalLog.WriteLine($"[{DateTime.Now}] Starting Agent...");

            DirectorySecurity dirSec = new DirectorySecurity();
            FileSystemAccessRule administratorSecurityRule = new FileSystemAccessRule("Administrators", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
            dirSec.AddAccessRule(administratorSecurityRule);
            FileSystemAclExtensions.CreateDirectory(dirSec, dataFolder);

            var scriptID = "39866b17-38c4-43a9-bb78-0aeea286d592";
            var scriptRaw = "V3JpdGUtSG9zdCAnSGVsbG8sIFdvcmxkISc=";
            LoadScript(scriptID, scriptRaw);
            ExecuteScript(scriptID);

            globalLog.Close();
        }

        static void LoadScript(string scriptID, string encodedScript)
        {
            var powershellScript = System.Convert.FromBase64String(encodedScript);
            File.WriteAllBytes(Path.Combine(dataFolder, scriptID + ".ps1"), powershellScript);
            globalLog.WriteLine($"[{DateTime.Now}] Loaded script '{scriptID}'");
        }

        static void ExecuteScript(string scriptID)
        {
            var logStream = new StreamWriter(Path.Combine(dataFolder, scriptID + ".log"));

            var ps = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy bypass -File \"{Path.Combine(dataFolder, scriptID + ".ps1")}\"",
                    WorkingDirectory = Path.GetTempPath(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            globalLog.WriteLine($"[{DateTime.Now}] Executing script '{scriptID}' with command '{ps.StartInfo.FileName + " " + ps.StartInfo.Arguments}'");

            ps.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                logStream.WriteLine(e.Data);
            });

            ps.Start();
            ps.BeginOutputReadLine();
            ps.WaitForExit((int)TimeSpan.FromMinutes(PowershellExecutionTimeout).TotalMilliseconds);
            globalLog.WriteLine($"[{DateTime.Now}] Completed execution of script '{scriptID}' returned exit code '{ps.ExitCode}'");
            ps.Close();
            logStream.Close();

            
        }
    }
}
