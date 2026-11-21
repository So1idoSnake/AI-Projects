using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GamingDisplayController
{
    internal static class RecoveryTests
    {
        private static int checks;

        public static int Main()
        {
            Check("native monitor is always preferred", new DisplayMode[] { DisplayMode.Tv, DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[0], "Alienware", 0);
            Check("missing TV still uses monitor", new DisplayMode[] { DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[0], "Alienware", 0);
            Check("available TV is never a recovery candidate", new DisplayMode[] { DisplayMode.Tv, DisplayMode.Vdd }, new DisplayMode[0], "Vdd", 0);
            Check("missing physical displays uses VDD", new DisplayMode[] { DisplayMode.Vdd }, new DisplayMode[0], "Vdd", 0);
            Check("failed native monitor uses VDD not TV", new DisplayMode[] { DisplayMode.Tv, DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[] { DisplayMode.Alienware }, "Alienware,Vdd", 0);
            Check("missing TV and failed monitor uses VDD", new DisplayMode[] { DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[] { DisplayMode.Alienware }, "Alienware,Vdd", 0);
            Check("all failures never select available TV", new DisplayMode[] { DisplayMode.Tv, DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[] { DisplayMode.Alienware, DisplayMode.Vdd }, "Alienware,Vdd", 10);
            Check("TV alone cannot satisfy monitor recovery", new DisplayMode[] { DisplayMode.Tv }, new DisplayMode[0], "", 10);
            Check("no displays remains an error", new DisplayMode[0], new DisplayMode[0], "", 10);

            ControllerState state = new ControllerState();
            state.Mode = DisplayMode.Alienware;
            state.StreamClient = "test";
            state.StreamWidth = 3840;
            state.StreamHeight = 2160;
            state.StreamFps = 120;
            state.StreamHdr = true;
            state.StreamStartedUtc = DateTime.UtcNow;
            state.SawSunshineUdp = true;
            state.ClearStreamMetadata();
            if (state.Mode != DisplayMode.Alienware || state.StreamClient != "" || state.StreamWidth != 0 || state.StreamHeight != 0 ||
                state.StreamFps != 0 || state.StreamHdr || state.StreamStartedUtc != DateTime.MinValue || state.SawSunshineUdp)
                throw new Exception("Finished stream metadata was not cleared correctly.");
            checks++;
            CheckMonitorProfiles();
            CheckHardwareModeTracking();
            CheckPendingMonitorRecovery();
            CheckCenteredPlan();
            CheckPresentationSettling();
            CheckStreamRequests();
            Console.WriteLine("PASS: " + checks + " recovery/profile/scaling checks (no display, HDR, RTSS or state-file changes).");
            return 0;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            checks++;
        }

        private static void Reject(Action action, string message)
        {
            try { action(); }
            catch (InvalidOperationException) { checks++; return; }
            throw new Exception(message);
        }

        private static void CheckMonitorProfiles()
        {
            foreach (string alias in new string[] { "alienware", "aw", "monitor", "alienware-native", "monitor-native", "monitor-5k2k" })
                Assert(Program.ParseMode(alias) == DisplayMode.Alienware, "Default monitor alias did not select native: " + alias);
            Assert(Program.ParseMode("MONITOR-330") == DisplayMode.Alienware330, "330 alias not parsed.");
            Assert(Program.ParseMode("alienware-4k") == DisplayMode.Alienware4K, "4K alias not parsed.");
            Assert((int)DisplayMode.Transition == 4 && (int)DisplayMode.Alienware330 == 5, "Old enum values changed.");
            foreach (DisplayMode mode in new DisplayMode[] { DisplayMode.Alienware, DisplayMode.Alienware330, DisplayMode.Alienware4K })
            {
                MonitorProfile profile = MonitorProfile.Get(mode);
                int width = mode == DisplayMode.Alienware ? 5120 : mode == DisplayMode.Alienware330 ? 2560 : 3840;
                int height = mode == DisplayMode.Alienware330 ? 1080 : 2160;
                int refresh = mode == DisplayMode.Alienware330 ? 330 : 165;
                Assert(profile.Width == width && profile.Height == height && profile.Refresh == refresh && profile.Limit == refresh - 3,
                    "Incorrect monitor parameters: " + mode);
                Assert(Controller.DesiredIdentity(mode) == "Alienware", "Optional mode changed physical identity.");
                ControllerState parsed = ControllerState.Parse(new string[] { "Mode=" + mode, "TransitionDestination=" + mode });
                Assert(parsed.Mode == mode && parsed.Clone().TransitionDestination == mode, "Saved explicit profile was lost.");
            }
            Assert(ControllerState.Parse(new string[] { "Mode=Alienware" }).Mode == DisplayMode.Alienware, "Legacy monitor state not retained.");
            Assert(ControllerState.Parse(new string[0]).Mode == DisplayMode.Tv, "Default TV state changed.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#DELA2F9#example" }.Identity == "Alienware", "New EDID unrecognized.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#DELA2FA#example" }.Identity == "Alienware", "DisplayPort DUAL EDID unrecognized.");
            Assert(new DisplayTarget { DevicePath = @"\\?\display#dela2fa#example" }.Identity == "Alienware", "Dual-mode EDID matching is case-sensitive.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#DELA2FA0#example" }.Identity == "Other", "Unrelated EDID prefix accepted.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#DELA1E4#example" }.Identity == "Other", "Old monitor receives new timings.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#SAM721B#example" }.Identity == "TV", "Soundbar identity changed.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#GSMC0C8#example" }.Identity == "TV", "LG identity changed.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#MTT1337#example" }.Identity == "VDD", "VDD identity changed.");
            // Optional variants are deliberately absent from the automatic candidate list.
            Check("optional-only display modes are never automatic candidates", new DisplayMode[] { DisplayMode.Alienware330, DisplayMode.Alienware4K, DisplayMode.Vdd }, new DisplayMode[0], "Vdd", 0);
        }

        private static DisplayTarget HardwareTarget(DisplayMode mode)
        {
            MonitorProfile profile = MonitorProfile.Get(mode);
            return new DisplayTarget {
                DevicePath = @"\\?\DISPLAY#" + (mode == DisplayMode.Alienware330 ? MonitorProfile.DualProductId : MonitorProfile.NativeProductId) + "#test",
                Active = true, Available = true, HdrQuerySucceeded = true, HdrSupported = true, HdrEnabled = true,
                Width = profile.Width, Height = profile.Height, RefreshRate = profile.Refresh
            };
        }

        private static void CheckHardwareModeTracking()
        {
            ControllerState native = new ControllerState { Mode = DisplayMode.Alienware };
            ControllerState dual = new ControllerState { Mode = DisplayMode.Alienware330 };
            ControllerState centered = new ControllerState { Mode = DisplayMode.Alienware4K };
            DisplayTarget nativeTarget = HardwareTarget(DisplayMode.Alienware);
            DisplayTarget dualTarget = HardwareTarget(DisplayMode.Alienware330);
            DisplayTarget fourK = HardwareTarget(DisplayMode.Alienware4K);
            Assert(MonitorHardwareTracker.FindMode(native, new[] { dualTarget }) == DisplayMode.Alienware330, "Native->DUAL not followed.");
            Assert(MonitorHardwareTracker.FindMode(dual, new[] { nativeTarget }) == DisplayMode.Alienware, "DUAL->native not followed.");
            Assert(MonitorHardwareTracker.FindMode(dual, new[] { fourK }) == DisplayMode.Alienware, "Native hardware return adopted optional4K.");
            Assert(MonitorHardwareTracker.FindMode(centered, new[] { dualTarget }) == DisplayMode.Alienware330, "Explicit hardware DUAL did not supersede centered4K.");
            Assert(MonitorHardwareTracker.FindMode(centered, new[] { fourK }) == null, "Explicit centered4K was cancelled by native EDID.");
            Assert(MonitorHardwareTracker.FindMode(centered, new[] { nativeTarget }) == null, "Centered4K was silently reset to native.");
            Assert(MonitorHardwareTracker.FindMode(native, new[] { nativeTarget }) == null, "Unchanged native mode queued work.");
            Assert(MonitorHardwareTracker.FindMode(dual, new[] { dualTarget }) == null, "Unchanged DUAL mode queued work.");
            foreach (DisplayMode mode in new[] { DisplayMode.Tv, DisplayMode.Vdd, DisplayMode.Stream, DisplayMode.Transition })
                Assert(MonitorHardwareTracker.FindMode(new ControllerState { Mode = mode }, new[] { dualTarget }) == null,
                    "Monitor hardware affected " + mode);
            Assert(MonitorHardwareTracker.FindMode(native, new[] { nativeTarget, dualTarget }) == null, "Ambiguous active monitor identities accepted.");
            DisplayTarget tv = new DisplayTarget { DevicePath = @"\\?\DISPLAY#SAM721B#test", Active = true, Available = true };
            Assert(MonitorHardwareTracker.FindMode(native, new[] { dualTarget, tv }) == DisplayMode.Alienware330,
                "OSD-restored extra output blocked hardware cleanup.");
            Assert(!Controller.IsTopologyCorrect(native, new[] { dualTarget, tv }), "Multiple active outputs treated as correct.");
            dualTarget.HdrEnabled = false;
            Assert(MonitorHardwareTracker.FindMode(native, new[] { dualTarget }) == DisplayMode.Alienware330, "HDR-off hardware mode was not repairable.");
            dualTarget.HdrQuerySucceeded = false;
            Assert(MonitorHardwareTracker.FindMode(native, new[] { dualTarget }) == null, "Unknown HDR state accepted.");
            dualTarget.HdrQuerySucceeded = true; dualTarget.HdrSupported = false;
            Assert(MonitorHardwareTracker.FindMode(native, new[] { dualTarget }) == null, "Missing HDR support accepted.");
            dualTarget.HdrSupported = true; dualTarget.Available = false;
            Assert(MonitorHardwareTracker.FindMode(native, new[] { dualTarget }) == null, "Unavailable hardware mode accepted.");
            dualTarget.Available = true; dualTarget.Active = false;
            Assert(MonitorHardwareTracker.FindMode(native, new[] { dualTarget }) == null, "Inactive hardware mode adopted.");
            dualTarget.Active = true; dualTarget.Width = 5120;
            Assert(MonitorHardwareTracker.FindMode(native, new[] { dualTarget }) == null, "Transient dual EDID/native geometry accepted.");
            dualTarget.Width = 2560; dualTarget.RefreshRate = 165;
            Assert(MonitorHardwareTracker.FindMode(native, new[] { dualTarget }) == null, "Unsettled DUAL refresh accepted.");
            dualTarget.RefreshRate = 330;
            MonitorHardwareTracker tracker = new MonitorHardwareTracker();
            DateTime now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            bool pending;
            Assert(tracker.Observe(native, new[] { dualTarget }, now, out pending) == null && pending, "Single OSD observation queued work.");
            Assert(tracker.Observe(native, new[] { dualTarget }, now.AddSeconds(1), out pending) == null && pending, "Hardware settling delay ignored.");
            Assert(tracker.Observe(native, new[] { dualTarget }, now.AddSeconds(2), out pending) == DisplayMode.Alienware330 && pending,
                "Stable OSD choice did not synchronize.");
            Assert(tracker.Observe(native, new[] { dualTarget }, now.AddSeconds(4), out pending) == null && pending, "Duplicate hardware action queued before cooldown.");
            Assert(tracker.Observe(native, new[] { dualTarget }, now.AddSeconds(8), out pending) == DisplayMode.Alienware330,
                "Failed synchronization could not retry.");
            Assert(tracker.Observe(dual, new[] { dualTarget }, now.AddSeconds(9), out pending) == null && !pending, "Completed sync did not clear observation.");
            Assert(tracker.Observe(dual, new[] { nativeTarget }, now.AddSeconds(10), out pending) == null && pending, "Native return was not debounced.");
            Assert(tracker.Observe(new ControllerState { Mode = DisplayMode.Stream }, new[] { nativeTarget }, now.AddSeconds(11), out pending) == null && !pending,
                "Stream did not cancel pending monitor sync.");
            Assert(tracker.Observe(dual, new[] { nativeTarget }, now.AddSeconds(12), out pending) == null && pending, "Old observation survived stream selection.");
            Assert(tracker.Observe(dual, new[] { nativeTarget }, now.AddSeconds(14), out pending) == DisplayMode.Alienware, "Native return was not adopted.");
            nativeTarget.DevicePath = @"\\?\DISPLAY#DELA2F9#replacement";
            Assert(tracker.Observe(dual, new[] { nativeTarget }, now.AddSeconds(15), out pending) == null && pending, "Device change reused old stability window.");
            Assert(tracker.Observe(dual, new[] { nativeTarget }, now.AddSeconds(17), out pending) == DisplayMode.Alienware, "Stable replacement observation rejected.");
            Assert(tracker.Observe(dual, new[] { nativeTarget }, now.AddSeconds(16), out pending) == null, "Backward clock ignored cooldown.");
        }

        private static void CheckPendingMonitorRecovery()
        {
            ControllerState stream = new ControllerState { Mode = DisplayMode.Stream, StreamClient = "test", StreamWidth = 3840,
                StreamHeight = 2160, StreamFps = 120, StreamHdr = true, SawSunshineUdp = true };
            ControllerState waiting = stream.ForProfile(DisplayMode.Vdd, true);
            Assert(DisplayRecovery.IsAwaitingMonitor(waiting) && waiting.Mode == DisplayMode.Vdd && waiting.TransitionDestination == DisplayMode.Vdd,
                "Finished-stream recovery did not retain monitor intent on temporary VDD.");
            Assert(waiting.StreamClient == "" && waiting.StreamWidth == 0 && waiting.StreamFps == 0 && !waiting.SawSunshineUdp,
                "Pending recovery retained finished-stream metadata.");
            Assert(stream.Mode == DisplayMode.Stream && stream.StreamWidth == 3840, "Recovery intent mutated its source state.");
            ControllerState persisted = ControllerState.Parse(new[] { "Mode=Vdd", "AwaitingMonitor=True" });
            Assert(DisplayRecovery.IsAwaitingMonitor(persisted) && persisted.Clone().AwaitingMonitor, "Pending intent was lost across parsing/cloning.");
            Assert(!ControllerState.Parse(new[] { "Mode=Vdd" }).AwaitingMonitor, "Legacy/manual VDD acquired automatic monitor intent.");
            foreach (DisplayMode mode in new[] { DisplayMode.Tv, DisplayMode.Alienware, DisplayMode.Alienware330, DisplayMode.Alienware4K, DisplayMode.Vdd, DisplayMode.Stream })
                Assert(!waiting.ForProfile(mode).AwaitingMonitor, "Successful explicit selection did not cancel waiting: " + mode);
            Assert(!waiting.ForProfile(DisplayMode.Alienware).AwaitingMonitor, "Successful native return retained pending intent.");
            Assert(!DisplayRecovery.IsAwaitingMonitor(new ControllerState { Mode = DisplayMode.Stream, AwaitingMonitor = true }),
                "Stale pending flag could interrupt an active stream.");
            DisplayTarget native = HardwareTarget(DisplayMode.Alienware);
            DisplayTarget dual = HardwareTarget(DisplayMode.Alienware330);
            DisplayTarget vdd = new DisplayTarget { DevicePath = @"\\?\DISPLAY#MTT1337#test", Active = true, Available = true };
            DisplayTarget tv = new DisplayTarget { DevicePath = @"\\?\DISPLAY#SAM721B#test", Active = true, Available = true };
            native.Active = false;
            Assert(DisplayRecovery.NativeMonitorAvailable(new[] { native, vdd }), "Inactive but available native monitor was not eligible to return.");
            Assert(!DisplayRecovery.NativeMonitorAvailable(new[] { dual, vdd, tv }), "Optional DUAL or TV counted as native fallback.");
            native.Available = false;
            Assert(!DisplayRecovery.NativeMonitorAvailable(new[] { native, tv }), "Unavailable monitor counted as activatable.");
            MonitorReturnTracker tracker = new MonitorReturnTracker();
            DateTime now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert(!tracker.Observe(waiting, new[] { native, vdd }, now), "Correct temporary VDD was repeatedly reapplied.");
            native.Available = true;
            Assert(!tracker.Observe(waiting, new[] { native, vdd }, now.AddSeconds(1)), "Single native return observation triggered recovery.");
            Assert(tracker.Observe(waiting, new[] { native, vdd }, now.AddSeconds(3)), "Stable native return was not retried.");
            Assert(!tracker.Observe(waiting, new[] { native, vdd }, now.AddSeconds(4)), "Duplicate recovery work ignored cooldown.");
            Assert(tracker.Observe(waiting, new[] { native, vdd }, now.AddSeconds(9)), "Failed native return could not retry.");
            Assert(!tracker.Observe(waiting.ForProfile(DisplayMode.Tv), new[] { native, tv }, now.AddSeconds(10)), "Manual TV selection did not cancel pending recovery.");
            Assert(!tracker.Observe(waiting.ForProfile(DisplayMode.Stream), new[] { native, vdd }, now.AddSeconds(11)), "Stream selection did not cancel pending recovery.");
            native.Available = false;
            Assert(!tracker.Observe(waiting, new[] { native, tv }, now.AddSeconds(12)), "Virtual safety repair ignored settle delay.");
            Assert(!tracker.Observe(waiting, new[] { native, tv }, now.AddSeconds(16)), "Virtual safety repair ran before five seconds.");
            Assert(tracker.Observe(waiting, new[] { native, tv }, now.AddSeconds(17)), "Missing virtual safety output was not repaired.");
            Assert(!tracker.Observe(new ControllerState { Mode = DisplayMode.Vdd }, new[] { native, tv }, now.AddSeconds(18)),
                "Explicit VDD selection unexpectedly acquired monitor return behavior.");
        }

        private static ActiveDisplayConfiguration NativeSnapshot()
        {
            NativeMethods.LUID adapter = new NativeMethods.LUID { LowPart = 123, HighPart = 0 };
            NativeMethods.DISPLAYCONFIG_PATH_INFO path = new NativeMethods.DISPLAYCONFIG_PATH_INFO();
            path.flags = NativeMethods.DISPLAYCONFIG_PATH_ACTIVE;
            path.sourceInfo.adapterId = adapter; path.sourceInfo.id = 1; path.sourceInfo.modeInfoIdx = 0;
            path.targetInfo.adapterId = adapter; path.targetInfo.id = 2; path.targetInfo.modeInfoIdx = 1;
            path.targetInfo.targetAvailable = true;
            path.targetInfo.refreshRate = new NativeMethods.DISPLAYCONFIG_RATIONAL { Numerator = 16499, Denominator = 100 };
            path.targetInfo.scaling = 1;
            path.targetInfo.rotation = 1;
            NativeMethods.DISPLAYCONFIG_MODE_INFO source = new NativeMethods.DISPLAYCONFIG_MODE_INFO();
            source.infoType = 1; source.id = 1; source.adapterId = adapter;
            source.sourceMode.width = 5120; source.sourceMode.height = 2160; source.sourceMode.pixelFormat = 4;
            NativeMethods.DISPLAYCONFIG_MODE_INFO target = new NativeMethods.DISPLAYCONFIG_MODE_INFO();
            target.infoType = 2; target.id = 2; target.adapterId = adapter;
            target.targetMode.targetVideoSignalInfo.activeSize = new NativeMethods.DISPLAYCONFIG_2DREGION { cx = 5120, cy = 2160 };
            target.targetMode.targetVideoSignalInfo.totalSize = new NativeMethods.DISPLAYCONFIG_2DREGION { cx = 5500, cy = 2300 };
            target.targetMode.targetVideoSignalInfo.vSyncFreq = path.targetInfo.refreshRate;
            target.targetMode.targetVideoSignalInfo.pixelRate = 2167080000UL;
            return new ActiveDisplayConfiguration { Identity = "Alienware", Hdr = true,
                Paths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[] { path }, Modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[] { source, target } };
        }

        private static void CheckCenteredPlan()
        {
            Assert(Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_MODE_INFO)) == 64, "MODE_INFO layout changed.");
            Assert(Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_SOURCE_MODE)) == 20, "Source mode layout incorrect.");
            Assert(Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_VIDEO_SIGNAL_INFO)) == 48, "Target signal layout incorrect.");
            Assert(Marshal.OffsetOf(typeof(NativeMethods.DISPLAYCONFIG_MODE_INFO), "targetMode").ToInt32() == 16, "Target union offset incorrect.");
            ActiveDisplayConfiguration original = NativeSnapshot();
            ActiveDisplayConfiguration centered = original.CreateCentered4K();
            centered.RequireCentered4K(false);
            Assert(Marshal.SizeOf(typeof(NvidiaDisplayQuery.PathInfo)) == (IntPtr.Size == 4 ? 28 : 48), "NVIDIA path ABI mismatch.");
            Assert(Marshal.SizeOf(typeof(NvidiaDisplayQuery.TargetInfo)) == (IntPtr.Size == 4 ? 12 : 24), "NVIDIA target ABI mismatch.");
            Assert(Marshal.SizeOf(typeof(NvidiaDisplayQuery.AdvancedTargetInfo)) == 128 &&
                Marshal.OffsetOf(typeof(NvidiaDisplayQuery.AdvancedTargetInfo), "Scaling").ToInt32() == 8, "NVIDIA scaling ABI mismatch.");
            ActiveDisplayConfiguration nvidia = centered.Clone();
            nvidia.Paths[0].targetInfo.scaling = 1;
            Reject(delegate { nvidia.RequireCentered4K(false); }, "Unconfirmed CCD identity accepted for smaller desktop.");
            nvidia.NvidiaScaling = 2;
            Reject(delegate { nvidia.RequireCentered4K(false); }, "NVIDIA GPU full-screen stretching accepted.");
            nvidia.NvidiaScaling = NvidiaDisplayQuery.GpuCentered;
            nvidia.RequireCentered4K(false);
            Assert(centered.HasSamePresentation(nvidia) && nvidia.HasSamePresentation(centered), "NVIDIA-confirmed centering rejected.");
            Assert(Controller.SnapshotMatchesState(nvidia, new ControllerState { Mode = DisplayMode.Alienware4K }), "NVIDIA-centered state rejected.");
            ActiveDisplayConfiguration restoreRequest = nvidia.CreateRestoreRequest();
            Assert(restoreRequest.Paths[0].targetInfo.scaling == 2 && restoreRequest.NvidiaScaling == null &&
                restoreRequest.HasSamePresentation(nvidia) && nvidia.Paths[0].targetInfo.scaling == 1,
                "NVIDIA rollback request was not normalized without mutating original.");
            nvidia.Paths[0].targetInfo.scaling = 3;
            Reject(delegate { nvidia.RequireCentered4K(false); }, "CCD stretching accepted despite NVIDIA setting.");
            nvidia.Paths[0].targetInfo.scaling = 1;
            nvidia.Modes[0].sourceMode.position.x = 1;
            Reject(delegate { nvidia.RequireCentered4K(false); }, "Offset NVIDIA desktop accepted.");
            nvidia.Modes[0].sourceMode.position.x = 0;
            nvidia.Paths[0].targetInfo.rotation = 2;
            Reject(delegate { nvidia.RequireCentered4K(false); }, "Rotated NVIDIA desktop accepted.");
            nvidia.Paths[0].targetInfo.rotation = 1;
            nvidia.Modes[1].targetMode.targetVideoSignalInfo.pixelRate++;
            Assert(!centered.HasSamePresentation(nvidia), "NVIDIA centering bypassed timing verification.");
            Assert(centered.Modes[0].sourceMode.width == 3840 && centered.Modes[0].sourceMode.height == 2160 && centered.Hdr,
                "Centered desktop or HDR metadata incorrect.");
            Assert(centered.Modes[1].Equals(original.Modes[1]) && centered.Paths[0].targetInfo.refreshRate.Equals(original.Paths[0].targetInfo.refreshRate),
                "Centering modified exact native signal timings.");
            Assert(original.Modes[0].sourceMode.width == 5120 && original.Paths[0].targetInfo.scaling == 1,
                "Centering mutated rollback snapshot.");
            Assert((centered.Modes[1].targetMode.targetVideoSignalInfo.activeSize.cx - centered.Modes[0].sourceMode.width) / 2 == 640,
                "Unexpected pillarbox geometry.");
            Assert(original.HasSamePresentation(original.Clone()), "An unchanged rollback configuration did not match.");
            ActiveDisplayConfiguration altered = original.Clone(); altered.Modes[0].sourceMode.position.x = 1;
            Assert(!original.HasSamePresentation(altered), "Rollback verification ignored source position.");
            altered = original.Clone(); altered.Modes[0].sourceMode.pixelFormat = 3;
            Assert(!original.HasSamePresentation(altered), "Rollback verification ignored source pixel format.");
            altered = original.Clone(); altered.Modes[1].targetMode.targetVideoSignalInfo.pixelRate++;
            Assert(!original.HasSamePresentation(altered), "Rollback verification ignored pixel clock.");
            altered = original.Clone(); altered.Modes[1].targetMode.targetVideoSignalInfo.totalSize.cx++;
            Assert(!original.HasSamePresentation(altered), "Rollback verification ignored total signal size.");
            altered = original.Clone(); altered.Paths[0].targetInfo.rotation = 2;
            Assert(!original.HasSamePresentation(altered), "Rollback verification ignored rotation.");
            altered = original.Clone(); altered.Hdr = false;
            Assert(!original.HasSamePresentation(altered), "Rollback verification ignored HDR state.");
            altered = original.Clone(); altered.Paths[0].flags ^= NativeMethods.DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE;
            Assert(!original.HasSamePresentation(altered), "Presentation verification ignored dynamic-refresh behavior.");
            altered = original.Clone(); altered.Paths[0].targetInfo.outputTechnology++;
            Assert(!original.HasSamePresentation(altered), "Presentation verification ignored output technology.");
            altered = original.Clone();
            altered.Modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[] { original.Modes[1], original.Modes[0] };
            altered.Paths[0].sourceInfo.modeInfoIdx = 1; altered.Paths[0].targetInfo.modeInfoIdx = 0;
            Assert(original.HasSamePresentation(altered), "Valid mode-array reordering changed presentation identity.");
            Assert(altered.CreateCentered4K().Modes[1].sourceMode.width == 3840, "Centering used a stale source index.");
            Assert(Controller.SnapshotMatchesState(original, new ControllerState { Mode = DisplayMode.Alienware }), "Matching native rollback snapshot rejected.");
            Assert(!Controller.SnapshotMatchesState(original, new ControllerState { Mode = DisplayMode.Tv }), "Saved TV and actual monitor were treated as the same rollback.");
            Assert(Controller.SnapshotMatchesState(centered, new ControllerState { Mode = DisplayMode.Alienware4K }), "Matching centered rollback snapshot rejected.");
            altered = centered.Clone(); altered.Paths[0].targetInfo.scaling = 3;
            Assert(!Controller.SnapshotMatchesState(altered, new ControllerState { Mode = DisplayMode.Alienware4K }), "Stretched 4K accepted for exact rollback.");
            altered = original.Clone();
            altered.Modes[0].sourceMode.width = 2560; altered.Modes[0].sourceMode.height = 1080;
            altered.Modes[1].targetMode.targetVideoSignalInfo.vSyncFreq = new NativeMethods.DISPLAYCONFIG_RATIONAL { Numerator = 330, Denominator = 1 };
            altered.Paths[0].targetInfo.refreshRate = altered.Modes[1].targetMode.targetVideoSignalInfo.vSyncFreq;
            Assert(Controller.SnapshotMatchesState(altered, new ControllerState { Mode = DisplayMode.Alienware330 }), "Matching optional 330 rollback snapshot rejected.");
            altered.Paths[0].targetInfo.refreshRate = original.Paths[0].targetInfo.refreshRate;
            Assert(!Controller.SnapshotMatchesState(altered, new ControllerState { Mode = DisplayMode.Alienware330 }), "Mismatched signal/path refresh accepted for rollback.");
            altered.Paths[0].targetInfo.refreshRate.Denominator = 0;
            Assert(!Controller.SnapshotMatchesState(altered, new ControllerState { Mode = DisplayMode.Alienware330 }), "Invalid path refresh accepted for rollback.");
            ActiveDisplayConfiguration bad = NativeSnapshot();
            bad.Paths[0].sourceInfo.modeInfoIdx = uint.MaxValue;
            Reject(delegate { bad.CreateCentered4K(); }, "Invalid source index accepted.");
            bad = NativeSnapshot(); bad.Paths[0].flags |= NativeMethods.DISPLAYCONFIG_PATH_SUPPORT_VIRTUAL_MODE;
            Reject(delegate { bad.CreateCentered4K(); }, "Packed virtual indexes accepted.");
            bad = NativeSnapshot(); bad.Modes[1].id = 55;
            Reject(delegate { bad.CreateCentered4K(); }, "Mismatched target ID accepted.");
            bad = NativeSnapshot(); bad.Modes[0].adapterId.LowPart = 77;
            Reject(delegate { bad.CreateCentered4K(); }, "Mismatched adapter accepted.");
            bad = NativeSnapshot(); bad.Modes[1].targetMode.targetVideoSignalInfo.vSyncFreq.Denominator = 0;
            Reject(delegate { bad.CreateCentered4K(); }, "Invalid refresh accepted.");
            bad = NativeSnapshot(); bad.Modes[1].targetMode.targetVideoSignalInfo.activeSize.cx = 3840;
            Reject(delegate { bad.CreateCentered4K(); }, "Nonnative target signal accepted.");
            bad = centered.Clone(); bad.Paths[0].targetInfo.scaling = 3;
            Reject(delegate { bad.RequireCentered4K(false); }, "Driver's stretched scaling accepted as centered.");
            bad = NativeSnapshot(); bad.Hdr = false;
            Reject(delegate { bad.CreateCentered4K(); }, "Transient SDR snapshot accepted for an HDR-on centered profile.");
        }

        private static void CheckPresentationSettling()
        {
            ActiveDisplayConfiguration native = NativeSnapshot();
            ActiveDisplayConfiguration stale = native.Clone(); stale.Hdr = false;
            int reads = 0;
            ActiveDisplayConfiguration result = DisplayManager.WaitForConfiguration(delegate
            {
                reads++;
                if (reads == 1) throw new Win32Exception(122);
                return reads == 2 ? stale : native;
            }, delegate(ActiveDisplayConfiguration value) { value.RequireCentered4K(true); }, TimeSpan.FromSeconds(1), delegate { });
            Assert(reads == 3 && object.ReferenceEquals(native, result), "Transient capture/HDR errors were not retried to the matching configuration.");
            reads = 0;
            Reject(delegate
            {
                DisplayManager.WaitForConfiguration(delegate { reads++; return stale; },
                    delegate(ActiveDisplayConfiguration value) { value.RequireCentered4K(true); }, TimeSpan.Zero, delegate { });
            }, "Persistent HDR mismatch did not terminate.");
            Assert(reads == 1, "Zero-timeout verification exceeded one capture.");
            Reject(delegate
            {
                DisplayManager.WaitForConfiguration(delegate { throw new Win32Exception(122); },
                    delegate(ActiveDisplayConfiguration value) { value.RequireCentered4K(true); }, TimeSpan.Zero, delegate { });
            }, "Persistent capture errors did not terminate.");
        }

        private static void CheckStreamRequests()
        {
            string[] names = { "SUNSHINE_CLIENT_WIDTH", "SUNSHINE_CLIENT_HEIGHT", "SUNSHINE_CLIENT_FPS", "SUNSHINE_CLIENT_HDR" };
            string[] previous = Array.ConvertAll(names, delegate(string name) { return Environment.GetEnvironmentVariable(name); });
            try
            {
                foreach (int width in new int[] { 1920, 3840 })
                    foreach (int fps in new int[] { 60, 90, 120 })
                        foreach (bool hdr in new bool[] { false, true })
                        {
                            Environment.SetEnvironmentVariable(names[0], width.ToString());
                            Environment.SetEnvironmentVariable(names[1], width == 1920 ? "1080" : "2160");
                            Environment.SetEnvironmentVariable(names[2], fps.ToString());
                            Environment.SetEnvironmentVariable(names[3], hdr.ToString());
                            StreamRequest request = StreamRequest.FromEnvironment();
                            Assert(request.Width == width && request.Fps == fps && request.Hdr == hdr, "Existing stream request contract changed.");
                        }
                Environment.SetEnvironmentVariable(names[0], "5120");
                Environment.SetEnvironmentVariable(names[2], "165");
                Reject(delegate { StreamRequest.FromEnvironment(); }, "New physical monitor timings leaked into Sunshine requests.");
            }
            finally
            {
                for (int i = 0; i < names.Length; i++) Environment.SetEnvironmentVariable(names[i], previous[i]);
            }
        }

        private static void Check(string name, DisplayMode[] available, DisplayMode[] failures, string expectedAttempts, int expectedResult)
        {
            List<DisplayMode> attempts = new List<DisplayMode>();
            int result = DisplayRecovery.Restore(
                delegate(DisplayMode mode) { return Array.IndexOf(available, mode) >= 0; },
                delegate(DisplayMode mode) { attempts.Add(mode); return Array.IndexOf(failures, mode) >= 0 ? 10 : 0; });
            string actualAttempts = string.Join(",", attempts.ConvertAll(delegate(DisplayMode mode) { return mode.ToString(); }).ToArray());
            if (result != expectedResult || actualAttempts != expectedAttempts)
                throw new Exception(name + ": result=" + result + ", attempts=" + actualAttempts);
            checks++;
        }
    }
}
