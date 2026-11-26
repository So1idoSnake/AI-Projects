using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

namespace AutoHDRForceTouch
{
    internal sealed class RegistryApplyResult
    {
        private RegistryApplyResult(
            bool success,
            bool changed,
            string message,
            string backupPath)
        {
            Success = success;
            Changed = changed;
            Message = message;
            BackupPath = backupPath;
        }

        public bool Success { get; private set; }
        public bool Changed { get; private set; }
        public string Message { get; private set; }
        public string BackupPath { get; private set; }

        internal static RegistryApplyResult Succeeded(
            bool changed,
            string message,
            string backupPath)
        {
            return new RegistryApplyResult(true, changed, message, backupPath);
        }

        internal static RegistryApplyResult Failed(
            string message,
            string backupPath)
        {
            return new RegistryApplyResult(false, false, message, backupPath);
        }
    }

    internal class RegistryService
    {
        internal const string ProductionBasePath = @"SOFTWARE\Microsoft\Direct3D";
        internal const string AutoHdrBehavior = "BufferUpgradeOverride=1";
        internal const string AutoHdr10BitBehavior =
            "BufferUpgradeOverride=1;BufferUpgradeEnable10Bit=1";

        private readonly string _basePath;
        private readonly IRegistryBackupService _backupService;

        public RegistryService()
            : this(ProductionBasePath, new RegistryBackupService())
        {
        }

        public RegistryService(
            string currentUserBasePath,
            IRegistryBackupService backupService)
        {
            if (string.IsNullOrWhiteSpace(currentUserBasePath))
            {
                throw new ArgumentException(
                    "A current-user registry path is required.",
                    "currentUserBasePath");
            }

            if (backupService == null)
            {
                throw new ArgumentNullException("backupService");
            }

            _basePath = currentUserBasePath.Trim().Trim('\\');
            _backupService = backupService;
        }

        public RegistryApplyResult Apply(
            string executablePath,
            bool enableAutoHdr,
            bool enable10Bit)
        {
            string normalizedPath;
            try
            {
                normalizedPath = NormalizeExecutablePath(executablePath);
            }
            catch (Exception exception)
            {
                return RegistryApplyResult.Failed(
                    "Selection failed: " + exception.Message,
                    string.Empty);
            }

            string matchingSubKey;
            bool baseKeyExists;
            try
            {
                using (RegistryKey baseKey = Registry.CurrentUser.OpenSubKey(_basePath, false))
                {
                    baseKeyExists = baseKey != null;
                    matchingSubKey = FindMatchingEntry(baseKey, normalizedPath);

                    if (!enableAutoHdr && matchingSubKey == null)
                    {
                        return RegistryApplyResult.Succeeded(
                            false,
                            "Auto HDR was already disabled for this executable.",
                            string.Empty);
                    }

                    if (matchingSubKey != null &&
                        IsAlreadyInRequestedState(
                            baseKey,
                            matchingSubKey,
                            enableAutoHdr,
                            enable10Bit))
                    {
                        return RegistryApplyResult.Succeeded(
                            false,
                            enableAutoHdr
                                ? "The requested Auto HDR settings were already applied."
                                : "Auto HDR was already disabled for this executable.",
                            string.Empty);
                    }
                }
            }
            catch (Exception exception)
            {
                return RegistryApplyResult.Failed(
                    "Registry inspection failed: " + exception.Message,
                    string.Empty);
            }

            RegistryBackupResult backup = _backupService.CreateBackup(
                _basePath,
                baseKeyExists);
            if (!backup.Success)
            {
                return RegistryApplyResult.Failed(
                    backup.Message,
                    backup.BackupPath);
            }

            try
            {
                using (RegistryKey baseKey = Registry.CurrentUser.CreateSubKey(_basePath))
                {
                    if (baseKey == null)
                    {
                        return RegistryApplyResult.Failed(
                            "Registry write failed: the Direct3D key could not be opened.",
                            backup.BackupPath);
                    }

                    if (matchingSubKey == null)
                    {
                        matchingSubKey = FindMatchingEntry(baseKey, normalizedPath);
                    }

                    if (enableAutoHdr)
                    {
                        if (matchingSubKey == null)
                        {
                            matchingSubKey = FindFirstFreeApplicationName(baseKey);
                        }

                        using (RegistryKey applicationKey =
                            baseKey.CreateSubKey(matchingSubKey))
                        {
                            if (applicationKey == null)
                            {
                                throw new InvalidOperationException(
                                    "The application registry entry could not be opened.");
                            }

                            // Name is written only for a newly allocated entry. Reused
                            // entries keep their original spelling and unrelated values.
                            if (applicationKey.GetValue("Name", null) == null)
                            {
                                applicationKey.SetValue(
                                    "Name",
                                    normalizedPath,
                                    RegistryValueKind.String);
                            }

                            applicationKey.SetValue(
                                "D3DBehaviors",
                                GetDesiredBehavior(enable10Bit),
                                RegistryValueKind.String);
                        }
                    }
                    else
                    {
                        using (RegistryKey applicationKey =
                            baseKey.OpenSubKey(matchingSubKey, true))
                        {
                            if (applicationKey == null)
                            {
                                throw new InvalidOperationException(
                                    "The matching application registry entry disappeared.");
                            }

                            applicationKey.DeleteValue("D3DBehaviors", false);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                return RegistryApplyResult.Failed(
                    "Registry write failed: " + exception.Message,
                    backup.BackupPath);
            }

            string verificationFailure;
            if (!VerifyState(
                matchingSubKey,
                normalizedPath,
                enableAutoHdr,
                enable10Bit,
                out verificationFailure))
            {
                return RegistryApplyResult.Failed(
                    "Verification failed: " + verificationFailure,
                    backup.BackupPath);
            }

            return RegistryApplyResult.Succeeded(
                true,
                enableAutoHdr
                    ? "Auto HDR settings were applied successfully."
                    : "Auto HDR was disabled successfully.",
                backup.BackupPath);
        }

        protected virtual bool VerifyState(
            string subKeyName,
            string normalizedPath,
            bool enableAutoHdr,
            bool enable10Bit,
            out string failure)
        {
            try
            {
                using (RegistryKey baseKey = Registry.CurrentUser.OpenSubKey(_basePath, false))
                using (RegistryKey applicationKey = baseKey == null
                    ? null
                    : baseKey.OpenSubKey(subKeyName, false))
                {
                    if (applicationKey == null)
                    {
                        failure = "the application registry entry was not found after writing.";
                        return false;
                    }

                    string storedName = applicationKey.GetValue("Name", null) as string;
                    if (!string.Equals(
                        storedName,
                        normalizedPath,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        failure = "the executable path did not match after writing.";
                        return false;
                    }

                    object behaviorValue = applicationKey.GetValue(
                        "D3DBehaviors",
                        null,
                        RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (!enableAutoHdr)
                    {
                        if (behaviorValue != null)
                        {
                            failure = "the Auto HDR override was still present after removal.";
                            return false;
                        }
                    }
                    else if (!string.Equals(
                        behaviorValue as string,
                        GetDesiredBehavior(enable10Bit),
                        StringComparison.Ordinal))
                    {
                        failure = "the Auto HDR behavior did not match after writing.";
                        return false;
                    }
                }

                failure = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                failure = exception.Message;
                return false;
            }
        }

        private static string NormalizeExecutablePath(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Choose a game executable first.");
            }

            string normalizedPath = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(executablePath.Trim()));
            if (!string.Equals(
                Path.GetExtension(normalizedPath),
                ".exe",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The selected file must be an .exe file.");
            }

            return normalizedPath;
        }

        private static string FindMatchingEntry(
            RegistryKey baseKey,
            string normalizedPath)
        {
            if (baseKey == null)
            {
                return null;
            }

            foreach (string subKeyName in baseKey.GetSubKeyNames())
            {
                if (string.Equals(
                    subKeyName,
                    "MostRecentApplication",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    using (RegistryKey applicationKey =
                        baseKey.OpenSubKey(subKeyName, false))
                    {
                        string storedPath = applicationKey == null
                            ? null
                            : applicationKey.GetValue("Name", null) as string;
                        if (string.Equals(
                            storedPath,
                            normalizedPath,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return subKeyName;
                        }
                    }
                }
                catch
                {
                    // One malformed or unreadable entry must not prevent another
                    // valid application entry from being found.
                }
            }

            return null;
        }

        private static string FindFirstFreeApplicationName(RegistryKey baseKey)
        {
            HashSet<int> usedNumbers = new HashSet<int>();
            foreach (string subKeyName in baseKey.GetSubKeyNames())
            {
                if (!subKeyName.StartsWith(
                    "Application",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string suffix = subKeyName.Substring("Application".Length);
                int number;
                if (int.TryParse(
                    suffix,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out number) && number >= 0)
                {
                    usedNumbers.Add(number);
                }
            }

            int candidate = 0;
            while (usedNumbers.Contains(candidate))
            {
                candidate++;
            }

            return "Application" + candidate.ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsAlreadyInRequestedState(
            RegistryKey baseKey,
            string matchingSubKey,
            bool enableAutoHdr,
            bool enable10Bit)
        {
            using (RegistryKey applicationKey =
                baseKey.OpenSubKey(matchingSubKey, false))
            {
                if (applicationKey == null)
                {
                    return false;
                }

                object behavior = applicationKey.GetValue(
                    "D3DBehaviors",
                    null,
                    RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (!enableAutoHdr)
                {
                    return behavior == null;
                }

                return string.Equals(
                    behavior as string,
                    GetDesiredBehavior(enable10Bit),
                    StringComparison.Ordinal);
            }
        }

        private static string GetDesiredBehavior(bool enable10Bit)
        {
            return enable10Bit ? AutoHdr10BitBehavior : AutoHdrBehavior;
        }
    }
}
