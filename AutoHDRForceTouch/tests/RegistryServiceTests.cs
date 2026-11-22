using System;
using System.IO;
using Microsoft.Win32;
using AutoHDRForceTouch;

namespace AutoHDRForceTouchTests
{
    internal static class RegistryServiceTests
    {
        private static int _passed;
        private static int _failed;

        public static int Main()
        {
            Run("Missing root allocates Application0", TestMissingRoot);
            Run("Existing path is reused and unrelated values survive", TestReuseAndPreserve);
            Run("Path matching is case-insensitive", TestCaseInsensitiveMatch);
            Run("First numeric application gap is used", TestFirstAvailableGap);
            Run("MostRecentApplication and malformed values are ignored", TestIgnoredEntries);
            Run("Auto HDR behavior without 10-bit is exact", TestAutoHdrOnly);
            Run("Auto HDR behavior with 10-bit is exact", TestAutoHdrWith10Bit);
            Run("Disable removes only D3DBehaviors", TestDisableExisting);
            Run("Disable without a match is a no-op", TestDisableMissing);
            Run("Paths with spaces and Unicode round-trip", TestUnicodePath);
            Run("Failed backup prevents mutation", TestBackupFailure);
            Run("Read-back mismatch is reported as failure", TestVerificationFailure);

            Console.WriteLine(
                "RegistryService tests: " + _passed + " passed, " +
                _failed + " failed.");
            return _failed == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                _passed++;
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception exception)
            {
                _failed++;
                Console.Error.WriteLine("FAIL: " + name);
                Console.Error.WriteLine(exception.ToString());
            }
        }

        private static void TestMissingRoot()
        {
            WithTestRoot(delegate(string basePath)
            {
                FakeBackupService backup = new FakeBackupService(true);
                RegistryApplyResult result = new RegistryService(basePath, backup)
                    .Apply(@"C:\Games\Alpha.exe", true, true);

                AssertTrue(result.Success, result.Message);
                AssertTrue(result.Changed, "Expected a registry mutation.");
                AssertEqual(1, backup.Calls, "Backup call count");
                AssertFalse(backup.LastKeyExists, "The backup should record an absent root.");
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    basePath + @"\Application0"))
                {
                    AssertNotNull(key, "Application0 was not created.");
                    AssertEqual(
                        FullPath(@"C:\Games\Alpha.exe"),
                        key.GetValue("Name") as string,
                        "Name");
                }
            });
        }

        private static void TestReuseAndPreserve()
        {
            WithTestRoot(delegate(string basePath)
            {
                string originalSpelling = FullPath(@"C:\Games\MixedCase.exe");
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    basePath + @"\Application7"))
                {
                    key.SetValue("Name", originalSpelling, RegistryValueKind.String);
                    key.SetValue("D3DBehaviors", "old", RegistryValueKind.String);
                    key.SetValue("KeepMe", 37, RegistryValueKind.DWord);
                }

                RegistryApplyResult result = new RegistryService(
                    basePath,
                    new FakeBackupService(true)).Apply(
                        originalSpelling.ToLowerInvariant(),
                        true,
                        false);

                AssertTrue(result.Success, result.Message);
                using (RegistryKey root = Registry.CurrentUser.OpenSubKey(basePath))
                using (RegistryKey key = root.OpenSubKey("Application7"))
                {
                    AssertEqual(1, root.GetSubKeyNames().Length, "Subkey count");
                    AssertEqual(37, Convert.ToInt32(key.GetValue("KeepMe")), "KeepMe");
                    AssertEqual(originalSpelling, key.GetValue("Name") as string, "Name spelling");
                }
            });
        }

        private static void TestCaseInsensitiveMatch()
        {
            WithTestRoot(delegate(string basePath)
            {
                string selectedPath = FullPath(@"C:\Games\CaseTest.exe");
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    basePath + @"\Application4"))
                {
                    key.SetValue("Name", selectedPath.ToUpperInvariant());
                    key.SetValue(
                        "D3DBehaviors",
                        RegistryService.AutoHdr10BitBehavior);
                }

                FakeBackupService backup = new FakeBackupService(true);
                RegistryApplyResult result = new RegistryService(basePath, backup)
                    .Apply(selectedPath.ToLowerInvariant(), true, true);

                AssertTrue(result.Success, result.Message);
                AssertFalse(result.Changed, "Matching state should not be rewritten.");
                AssertEqual(0, backup.Calls, "No backup is needed without a mutation.");
            });
        }

        private static void TestFirstAvailableGap()
        {
            WithTestRoot(delegate(string basePath)
            {
                Registry.CurrentUser.CreateSubKey(basePath + @"\Application0").Dispose();
                Registry.CurrentUser.CreateSubKey(basePath + @"\Application2").Dispose();
                Registry.CurrentUser.CreateSubKey(basePath + @"\ApplicationBad").Dispose();

                RegistryApplyResult result = new RegistryService(
                    basePath,
                    new FakeBackupService(true)).Apply(
                        @"C:\Games\Gap.exe",
                        true,
                        false);

                AssertTrue(result.Success, result.Message);
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    basePath + @"\Application1"))
                {
                    AssertNotNull(key, "Application1 was not allocated.");
                }
            });
        }

        private static void TestIgnoredEntries()
        {
            WithTestRoot(delegate(string basePath)
            {
                string path = FullPath(@"C:\Games\Ignore.exe");
                using (RegistryKey recent = Registry.CurrentUser.CreateSubKey(
                    basePath + @"\MostRecentApplication"))
                {
                    recent.SetValue("Name", path);
                }

                using (RegistryKey malformed = Registry.CurrentUser.CreateSubKey(
                    basePath + @"\Application0"))
                {
                    malformed.SetValue("Name", 123, RegistryValueKind.DWord);
                }

                RegistryApplyResult result = new RegistryService(
                    basePath,
                    new FakeBackupService(true)).Apply(path, true, false);

                AssertTrue(result.Success, result.Message);
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    basePath + @"\Application1"))
                {
                    AssertNotNull(key, "A new application entry was not created.");
                    AssertEqual(path, key.GetValue("Name") as string, "Name");
                }
            });
        }

        private static void TestAutoHdrOnly()
        {
            TestBehavior(false, RegistryService.AutoHdrBehavior);
        }

        private static void TestAutoHdrWith10Bit()
        {
            TestBehavior(true, RegistryService.AutoHdr10BitBehavior);
        }

        private static void TestBehavior(bool enable10Bit, string expected)
        {
            WithTestRoot(delegate(string basePath)
            {
                RegistryApplyResult result = new RegistryService(
                    basePath,
                    new FakeBackupService(true)).Apply(
                        @"C:\Games\Behavior.exe",
                        true,
                        enable10Bit);

                AssertTrue(result.Success, result.Message);
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    basePath + @"\Application0"))
                {
                    AssertEqual(expected, key.GetValue("D3DBehaviors") as string, "D3DBehaviors");
                }
            });
        }

        private static void TestDisableExisting()
        {
            WithTestRoot(delegate(string basePath)
            {
                string path = FullPath(@"C:\Games\Disable.exe");
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    basePath + @"\Application3"))
                {
                    key.SetValue("Name", path);
                    key.SetValue("D3DBehaviors", RegistryService.AutoHdrBehavior);
                    key.SetValue("KeepMe", "untouched");
                }

                RegistryApplyResult result = new RegistryService(
                    basePath,
                    new FakeBackupService(true)).Apply(path, false, true);

                AssertTrue(result.Success, result.Message);
                AssertTrue(result.Changed, "Expected D3DBehaviors to be removed.");
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    basePath + @"\Application3"))
                {
                    AssertNull(key.GetValue("D3DBehaviors", null), "D3DBehaviors");
                    AssertEqual("untouched", key.GetValue("KeepMe") as string, "KeepMe");
                }
            });
        }

        private static void TestDisableMissing()
        {
            WithTestRoot(delegate(string basePath)
            {
                FakeBackupService backup = new FakeBackupService(true);
                RegistryApplyResult result = new RegistryService(basePath, backup)
                    .Apply(@"C:\Games\NotRegistered.exe", false, true);

                AssertTrue(result.Success, result.Message);
                AssertFalse(result.Changed, "Missing entry should be a no-op.");
                AssertEqual(0, backup.Calls, "No-op should not create a backup.");
                using (RegistryKey root = Registry.CurrentUser.OpenSubKey(basePath))
                {
                    AssertNull(root, "No-op unexpectedly created the base key.");
                }
            });
        }

        private static void TestUnicodePath()
        {
            WithTestRoot(delegate(string basePath)
            {
                string path = @"C:\My Games\Tést 世界\Game.exe";
                RegistryApplyResult result = new RegistryService(
                    basePath,
                    new FakeBackupService(true)).Apply(path, true, false);

                AssertTrue(result.Success, result.Message);
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    basePath + @"\Application0"))
                {
                    AssertEqual(FullPath(path), key.GetValue("Name") as string, "Unicode Name");
                }
            });
        }

        private static void TestBackupFailure()
        {
            WithTestRoot(delegate(string basePath)
            {
                string path = FullPath(@"C:\Games\BackupFail.exe");
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    basePath + @"\Application0"))
                {
                    key.SetValue("Name", path);
                    key.SetValue("D3DBehaviors", "original");
                }

                RegistryApplyResult result = new RegistryService(
                    basePath,
                    new FakeBackupService(false)).Apply(path, true, true);

                AssertFalse(result.Success, "Backup failure must fail the operation.");
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    basePath + @"\Application0"))
                {
                    AssertEqual("original", key.GetValue("D3DBehaviors") as string, "D3DBehaviors");
                }
            });
        }

        private static void TestVerificationFailure()
        {
            WithTestRoot(delegate(string basePath)
            {
                RegistryApplyResult result = new VerificationFailingRegistryService(
                    basePath,
                    new FakeBackupService(true)).Apply(
                        @"C:\Games\Verify.exe",
                        true,
                        false);

                AssertFalse(result.Success, "Verification mismatch must be reported.");
                AssertTrue(
                    result.Message.IndexOf("Verification failed", StringComparison.Ordinal) >= 0,
                    "Failure phase was not identified.");
                AssertTrue(result.BackupPath.Length > 0, "Backup path should be retained.");
            });
        }

        private static void WithTestRoot(Action<string> test)
        {
            string basePath = @"Software\AutoHDRForceTouch.Tests\" +
                Guid.NewGuid().ToString("N");
            try
            {
                test(basePath);
            }
            finally
            {
                // Each test deletes only its own GUID-named branch. The shared test
                // parent and every production registry path are deliberately untouched.
                Registry.CurrentUser.DeleteSubKeyTree(basePath, false);
            }
        }

        private static string FullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            AssertTrue(!condition, message);
        }

        private static void AssertNull(object value, string name)
        {
            AssertTrue(value == null, name + " should be null.");
        }

        private static void AssertNotNull(object value, string message)
        {
            AssertTrue(value != null, message);
        }

        private static void AssertEqual<T>(T expected, T actual, string name)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    name + " expected <" + expected + "> but was <" + actual + ">.");
            }
        }

        private sealed class FakeBackupService : IRegistryBackupService
        {
            private readonly bool _succeed;

            public FakeBackupService(bool succeed)
            {
                _succeed = succeed;
            }

            public int Calls { get; private set; }
            public bool LastKeyExists { get; private set; }

            public RegistryBackupResult CreateBackup(
                string currentUserSubKeyPath,
                bool keyExists)
            {
                Calls++;
                LastKeyExists = keyExists;
                return _succeed
                    ? RegistryBackupResult.Succeeded(@"C:\fake\backup.reg")
                    : RegistryBackupResult.Failed("Backup failed for test.");
            }
        }

        private sealed class VerificationFailingRegistryService : RegistryService
        {
            public VerificationFailingRegistryService(
                string basePath,
                IRegistryBackupService backupService)
                : base(basePath, backupService)
            {
            }

            protected override bool VerifyState(
                string subKeyName,
                string normalizedPath,
                bool enableAutoHdr,
                bool enable10Bit,
                out string failure)
            {
                failure = "simulated read-back mismatch.";
                return false;
            }
        }
    }
}
