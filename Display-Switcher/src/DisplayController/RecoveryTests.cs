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
            Check("TV remains preferred", new DisplayMode[] { DisplayMode.Tv, DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[0], "Tv", 0);
            Check("missing TV uses monitor", new DisplayMode[] { DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[0], "Alienware", 0);
            Check("failed TV uses monitor", new DisplayMode[] { DisplayMode.Tv, DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[] { DisplayMode.Tv }, "Tv,Alienware", 0);
            Check("missing physical displays uses VDD", new DisplayMode[] { DisplayMode.Vdd }, new DisplayMode[0], "Vdd", 0);
            Check("failed physical displays uses VDD", new DisplayMode[] { DisplayMode.Tv, DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[] { DisplayMode.Tv, DisplayMode.Alienware }, "Tv,Alienware,Vdd", 0);
            Check("missing TV and failed monitor uses VDD", new DisplayMode[] { DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[] { DisplayMode.Alienware }, "Alienware,Vdd", 0);
            Check("all failures remain an error", new DisplayMode[] { DisplayMode.Tv, DisplayMode.Alienware, DisplayMode.Vdd }, new DisplayMode[] { DisplayMode.Tv, DisplayMode.Alienware, DisplayMode.Vdd }, "Tv,Alienware,Vdd", 10);
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
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#DELA1E4#example" }.Identity == "Other", "Old monitor receives new timings.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#SAM721B#example" }.Identity == "TV", "Soundbar identity changed.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#GSMC0C8#example" }.Identity == "TV", "LG identity changed.");
            Assert(new DisplayTarget { DevicePath = @"\\?\DISPLAY#MTT1337#example" }.Identity == "VDD", "VDD identity changed.");
            // Optional variants are deliberately absent from the automatic candidate list.
            Check("optional-only display modes are never automatic candidates", new DisplayMode[] { DisplayMode.Alienware330, DisplayMode.Alienware4K, DisplayMode.Vdd }, new DisplayMode[0], "Vdd", 0);
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
