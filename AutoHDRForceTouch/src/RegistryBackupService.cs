using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace AutoHDRForceTouch
{
    internal interface IRegistryBackupService
    {
        RegistryBackupResult CreateBackup(string currentUserSubKeyPath, bool keyExists);
    }

    internal sealed class RegistryBackupResult
    {
        private RegistryBackupResult(bool success, string backupPath, string message)
        {
            Success = success;
            BackupPath = backupPath;
            Message = message;
        }

        public bool Success { get; private set; }
        public string BackupPath { get; private set; }
        public string Message { get; private set; }

        public static RegistryBackupResult Succeeded(string backupPath)
        {
            return new RegistryBackupResult(true, backupPath, string.Empty);
        }

        public static RegistryBackupResult Failed(string message)
        {
            return new RegistryBackupResult(false, string.Empty, message);
        }
    }

    internal sealed class RegistryBackupService : IRegistryBackupService
    {
        public RegistryBackupResult CreateBackup(string currentUserSubKeyPath, bool keyExists)
        {
            try
            {
                string localApplicationData = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);
                string backupDirectory = Path.Combine(
                    localApplicationData,
                    @"AutoHDRForceTouch\Backups");
                Directory.CreateDirectory(backupDirectory);

                string timestamp = DateTime.Now.ToString(
                    "yyyyMMdd-HHmmss-fff",
                    CultureInfo.InvariantCulture);
                string uniqueSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);

                if (!keyExists)
                {
                    // A marker is a truthful backup for an absent branch and lets a
                    // later restore distinguish "did not exist" from a lost export.
                    string markerPath = Path.Combine(
                        backupDirectory,
                        "Direct3D-" + timestamp + "-" + uniqueSuffix + "-absent.txt");
                    File.WriteAllText(
                        markerPath,
                        "The registry key HKCU\\" + currentUserSubKeyPath +
                        " did not exist before this change." + Environment.NewLine);
                    return RegistryBackupResult.Succeeded(markerPath);
                }

                string backupPath = Path.Combine(
                    backupDirectory,
                    "Direct3D-" + timestamp + "-" + uniqueSuffix + ".reg");
                string registryPath = @"HKCU\" + currentUserSubKeyPath;

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "reg.exe";
                startInfo.Arguments = "export " + Quote(registryPath) + " " +
                    Quote(backupPath) + " /y";
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return RegistryBackupResult.Failed(
                            "Backup failed because reg.exe could not be started.");
                    }

                    string standardOutput = process.StandardOutput.ReadToEnd();
                    string standardError = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0 || !File.Exists(backupPath))
                    {
                        string detail = string.IsNullOrWhiteSpace(standardError)
                            ? standardOutput
                            : standardError;
                        detail = detail == null ? string.Empty : detail.Trim();
                        return RegistryBackupResult.Failed(
                            "Backup failed while exporting HKCU\\" +
                            currentUserSubKeyPath +
                            (detail.Length == 0 ? "." : ": " + detail));
                    }
                }

                return RegistryBackupResult.Succeeded(backupPath);
            }
            catch (Exception exception)
            {
                return RegistryBackupResult.Failed(
                    "Backup failed: " + exception.Message);
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
