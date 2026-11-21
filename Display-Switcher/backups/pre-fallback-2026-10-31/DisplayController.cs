using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GamingDisplayController
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    return PrintUsage();
                }

                string command = args[0].ToLowerInvariant();
                if (command == "status")
                {
                    bool json = args.Any(delegate(string value) { return value.Equals("--json", StringComparison.OrdinalIgnoreCase); });
                    Console.WriteLine(Controller.GetStatus(json));
                    return 0;
                }

                if (command == "validate")
                {
                    Console.WriteLine(Controller.ValidateEnvironment());
                    return 0;
                }

                if (command == "set-cap" && args.Length >= 2)
                {
                    int limit;
                    if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out limit) || limit < 1 || limit > 1000)
                    {
                        throw new ArgumentException("Frame limit must be an integer from 1 through 1000.");
                    }
                    RtssManager.SetLimit(limit);
                    Console.WriteLine("RTSS global frame limit: " + limit);
                    return 0;
                }

                if (command == "validate-stream-request")
                {
                    StreamRequest request = StreamRequest.FromEnvironment();
                    Console.WriteLine(request.ClientName + ": " + request.Width + "x" + request.Height + "@" + request.Fps + " HDR=" + request.Hdr);
                    return 0;
                }

                if (command == "set" && args.Length >= 2)
                {
                    return Controller.RunLocked(delegate
                    {
                        return Controller.SetMode(ParseMode(args[1]), true, "explicit-command");
                    });
                }

                if (command == "stream-start")
                {
                    return Controller.RunLocked(Controller.StreamStart);
                }

                if (command == "stream-end" || command == "restore-lg")
                {
                    return Controller.RunLocked(Controller.StreamEnd);
                }

                if (command == "reapply")
                {
                    return Controller.RunLocked(Controller.Reapply);
                }

                if (command == "tray")
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new TrayApplicationContext());
                    return 0;
                }

                return PrintUsage();
            }
            catch (Exception ex)
            {
                Logger.Error("Unhandled error", ex);
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static DisplayMode ParseMode(string value)
        {
            string normalized = value.Trim().ToLowerInvariant();
            if (normalized == "tv" || normalized == "lg" || normalized == "cx" || normalized == "c2") return DisplayMode.Tv;
            if (normalized == "alienware" || normalized == "aw" || normalized == "monitor") return DisplayMode.Alienware;
            if (normalized == "vdd" || normalized == "virtual") return DisplayMode.Vdd;
            throw new ArgumentException("Unknown display mode: " + value);
        }

        private static int PrintUsage()
        {
            Console.WriteLine("Gaming Display Controller");
            Console.WriteLine("  status [--json]");
            Console.WriteLine("  validate");
            Console.WriteLine("  set-cap <fps>");
            Console.WriteLine("  validate-stream-request");
            Console.WriteLine("  set tv|alienware|vdd");
            Console.WriteLine("  stream-start");
            Console.WriteLine("  stream-end");
            Console.WriteLine("  restore-lg");
            Console.WriteLine("  reapply");
            Console.WriteLine("  tray");
            return 2;
        }
    }

    internal enum DisplayMode
    {
        Tv,
        Alienware,
        Vdd,
        Stream,
        Transition
    }

    internal sealed class ControllerState
    {
        public DisplayMode Mode = DisplayMode.Tv;
        public DisplayMode TransitionDestination = DisplayMode.Tv;
        public string StreamClient = "";
        public int StreamWidth;
        public int StreamHeight;
        public int StreamFps;
        public bool StreamHdr;
        public DateTime StreamStartedUtc = DateTime.MinValue;
        public bool SawSunshineUdp;

        public ControllerState Clone()
        {
            return (ControllerState)MemberwiseClone();
        }

        public static ControllerState Load()
        {
            ControllerState state = new ControllerState();
            string path = Paths.StateFile;
            if (!File.Exists(path)) return state;

            foreach (string rawLine in File.ReadAllLines(path))
            {
                int separator = rawLine.IndexOf('=');
                if (separator <= 0) continue;
                string key = rawLine.Substring(0, separator).Trim();
                string value = rawLine.Substring(separator + 1).Trim();
                if (key.Equals("Mode", StringComparison.OrdinalIgnoreCase))
                {
                    DisplayMode parsed;
                    if (Enum.TryParse<DisplayMode>(value, true, out parsed)) state.Mode = parsed;
                }
                else if (key.Equals("TransitionDestination", StringComparison.OrdinalIgnoreCase))
                {
                    DisplayMode parsed;
                    if (Enum.TryParse<DisplayMode>(value, true, out parsed)) state.TransitionDestination = parsed;
                }
                else if (key.Equals("StreamClient", StringComparison.OrdinalIgnoreCase)) state.StreamClient = value;
                else if (key.Equals("StreamWidth", StringComparison.OrdinalIgnoreCase)) int.TryParse(value, out state.StreamWidth);
                else if (key.Equals("StreamHeight", StringComparison.OrdinalIgnoreCase)) int.TryParse(value, out state.StreamHeight);
                else if (key.Equals("StreamFps", StringComparison.OrdinalIgnoreCase)) int.TryParse(value, out state.StreamFps);
                else if (key.Equals("StreamHdr", StringComparison.OrdinalIgnoreCase)) bool.TryParse(value, out state.StreamHdr);
                else if (key.Equals("StreamStartedUtc", StringComparison.OrdinalIgnoreCase)) DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out state.StreamStartedUtc);
                else if (key.Equals("SawSunshineUdp", StringComparison.OrdinalIgnoreCase)) bool.TryParse(value, out state.SawSunshineUdp);
            }
            return state;
        }

        public void Save()
        {
            Directory.CreateDirectory(Paths.DataDirectory);
            string[] lines = new string[]
            {
                "Mode=" + Mode,
                "TransitionDestination=" + TransitionDestination,
                "StreamClient=" + StreamClient,
                "StreamWidth=" + StreamWidth.ToString(CultureInfo.InvariantCulture),
                "StreamHeight=" + StreamHeight.ToString(CultureInfo.InvariantCulture),
                "StreamFps=" + StreamFps.ToString(CultureInfo.InvariantCulture),
                "StreamHdr=" + StreamHdr.ToString(CultureInfo.InvariantCulture),
                "StreamStartedUtc=" + StreamStartedUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                "SawSunshineUdp=" + SawSunshineUdp.ToString(CultureInfo.InvariantCulture)
            };
            string temporary = Paths.StateFile + ".tmp";
            File.WriteAllLines(temporary, lines, Encoding.UTF8);
            if (File.Exists(Paths.StateFile)) File.Delete(Paths.StateFile);
            File.Move(temporary, Paths.StateFile);
        }
    }

    internal static class Paths
    {
        // Sunshine preparation commands and the interactive tray can run under
        // different Windows accounts. Keep one state/log location beside the
        // deployed executable so both contexts observe the same controller state.
        public static readonly string DataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        public static readonly string StateFile = Path.Combine(DataDirectory, "state.ini");
        public static readonly string LogFile = Path.Combine(DataDirectory, "controller.log");
        public const string RtssDirectory = @"C:\Program Files (x86)\RivaTuner Statistics Server";
        public static readonly string RtssExecutable = Path.Combine(RtssDirectory, "RTSS.exe");
#if RTSS_X86
        public static readonly string RtssHooks = Path.Combine(RtssDirectory, "RTSSHooks.dll");
#else
        public static readonly string RtssHooks = Path.Combine(RtssDirectory, "RTSSHooks64.dll");
#endif
    }

    internal static class Logger
    {
        private static readonly object Sync = new object();

        public static void Info(string message) { Write("INFO", message, null); }
        public static void Warning(string message) { Write("WARN", message, null); }
        public static void Error(string message, Exception error) { Write("ERROR", message, error); }

        private static void Write(string level, string message, Exception error)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(Paths.DataDirectory);
                    string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [" + level + "] " + message;
                    if (error != null) line += Environment.NewLine + error;
                    File.AppendAllText(Paths.LogFile, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }
    }

    internal sealed class DisplayTarget
    {
        public NativeMethods.DISPLAYCONFIG_PATH_INFO Path;
        public string FriendlyName;
        public string DevicePath;
        public string GdiName;
        public bool Active;
        public bool Available;
        public bool HdrQuerySucceeded;
        public bool HdrSupported;
        public bool HdrEnabled;
        public int Width;
        public int Height;
        public int RefreshRate;

        public string Identity
        {
            get
            {
                string upper = (DevicePath ?? "").ToUpperInvariant();
                if (upper.Contains("DISPLAY#MTT1337")) return "VDD";
                if (upper.Contains("DISPLAY#DELA1E4")) return "Alienware";
                // The loft CX exposes its native LG/GSM EDID. The office C2 is
                // connected through a Samsung HDMI 2.1 soundbar, which owns the
                // downstream EDID and is deliberately recognized by its exact ID.
                if (upper.Contains("DISPLAY#GSM") || upper.Contains("DISPLAY#SAM721B") ||
                    (FriendlyName ?? "").IndexOf("LG", StringComparison.OrdinalIgnoreCase) >= 0) return "TV";
                return "Other";
            }
        }
    }

    internal static class Controller
    {
        private const int TvWidth = 3840;
        private const int TvHeight = 2160;
        private const int TvRefresh = 120;
        private const int TvLimit = 117;
        private const int AwWidth = 3440;
        private const int AwHeight = 1440;
        private const int AwRefresh = 175;
        private const int AwLimit = 172;

        public static int RunLocked(Func<int> action)
        {
            using (Mutex mutex = new Mutex(false, @"Local\GamingDisplayController.Operation"))
            {
                if (!mutex.WaitOne(TimeSpan.FromSeconds(30)))
                {
                    Console.Error.WriteLine("Display controller is busy.");
                    return 20;
                }
                try { return action(); }
                finally { mutex.ReleaseMutex(); }
            }
        }

        public static string ValidateEnvironment()
        {
            StringBuilder output = new StringBuilder();
            List<DisplayTarget> displays = DisplayManager.EnumerateAllTargets();
            output.AppendLine("DisplayConfig query: PASS");
            try
            {
                DisplayManager.ValidateCurrentConfiguration();
                output.AppendLine("Current topology round-trip validation: PASS");
            }
            catch (Exception ex)
            {
                output.AppendLine("Current topology round-trip validation: FAIL (" + ex.Message + ")");
            }
            output.AppendLine("TV target available: " + displays.Any(delegate(DisplayTarget d) { return d.Identity == "TV" && d.Available; }));
            output.AppendLine("Alienware target available: " + displays.Any(delegate(DisplayTarget d) { return d.Identity == "Alienware" && d.Available; }));
            output.AppendLine("VDD target available: " + displays.Any(delegate(DisplayTarget d) { return d.Identity == "VDD" && d.Available; }));
            foreach (string identity in new string[] { "TV", "Alienware", "VDD" })
            {
                bool available = displays.Any(delegate(DisplayTarget d) { return d.Identity == identity && d.Available; });
                if (!available)
                {
                    output.AppendLine(identity + "-only topology validation: SKIP (not available)");
                    continue;
                }
                try
                {
                    DisplayManager.ValidateOnly(identity);
                    output.AppendLine(identity + "-only topology validation: PASS");
                }
                catch (Exception ex)
                {
                    output.AppendLine(identity + "-only topology validation: FAIL (" + ex.Message + ")");
                }
            }
            output.AppendLine("RTSS installed: " + (File.Exists(Paths.RtssExecutable) && File.Exists(Paths.RtssHooks)));
            output.AppendLine("State directory writable: " + CanWriteDataDirectory());
            return output.ToString().TrimEnd();
        }

        private static bool CanWriteDataDirectory()
        {
            try
            {
                Directory.CreateDirectory(Paths.DataDirectory);
                string file = Path.Combine(Paths.DataDirectory, "write-test.tmp");
                File.WriteAllText(file, "ok");
                File.Delete(file);
                return true;
            }
            catch { return false; }
        }

        public static string GetStatus(bool json)
        {
            ControllerState state = ControllerState.Load();
            List<DisplayTarget> displays = DisplayManager.EnumerateAllTargets();
            if (!json)
            {
                StringBuilder text = new StringBuilder();
                text.AppendLine("Controller state: " + state.Mode);
                if (state.Mode == DisplayMode.Stream)
                {
                    text.AppendLine("Stream: " + state.StreamClient + " " + state.StreamWidth + "x" + state.StreamHeight + "@" + state.StreamFps + " HDR=" + state.StreamHdr);
                }
                foreach (DisplayTarget display in displays.Where(delegate(DisplayTarget d) { return d.Active || d.Available; }))
                {
                    text.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0,-10} active={1,-5} available={2,-5} gdi={3,-12} mode={4}x{5}@{6} HDR={7}/{8} name={9}",
                        display.Identity, display.Active, display.Available, display.GdiName ?? "-", display.Width, display.Height, display.RefreshRate,
                        display.HdrQuerySucceeded ? display.HdrSupported.ToString() : "?",
                        display.HdrQuerySucceeded ? display.HdrEnabled.ToString() : "?",
                        display.FriendlyName));
                    text.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "           path source={0}:{1} target={2}:{3} sourceMode=0x{4:X8} targetMode=0x{5:X8} flags=0x{6:X8}",
                        display.Path.sourceInfo.adapterId, display.Path.sourceInfo.id,
                        display.Path.targetInfo.adapterId, display.Path.targetInfo.id,
                        display.Path.sourceInfo.modeInfoIdx, display.Path.targetInfo.modeInfoIdx, display.Path.flags));
                }
                return text.ToString().TrimEnd();
            }

            StringBuilder result = new StringBuilder();
            result.Append("{\"state\":\"").Append(EscapeJson(state.Mode.ToString())).Append("\",\"displays\":[");
            bool first = true;
            foreach (DisplayTarget display in displays.Where(delegate(DisplayTarget d) { return d.Active || d.Available; }))
            {
                if (!first) result.Append(',');
                first = false;
                result.Append("{\"identity\":\"").Append(EscapeJson(display.Identity)).Append("\"");
                result.Append(",\"active\":").Append(display.Active ? "true" : "false");
                result.Append(",\"available\":").Append(display.Available ? "true" : "false");
                result.Append(",\"gdi\":\"").Append(EscapeJson(display.GdiName ?? "")).Append("\"");
                result.Append(",\"width\":").Append(display.Width);
                result.Append(",\"height\":").Append(display.Height);
                result.Append(",\"refresh\":").Append(display.RefreshRate);
                result.Append(",\"hdrSupported\":").Append(display.HdrQuerySucceeded ? (display.HdrSupported ? "true" : "false") : "null");
                result.Append(",\"hdrEnabled\":").Append(display.HdrQuerySucceeded ? (display.HdrEnabled ? "true" : "false") : "null");
                result.Append(",\"name\":\"").Append(EscapeJson(display.FriendlyName ?? "")).Append("\"");
                result.Append(",\"devicePath\":\"").Append(EscapeJson(display.DevicePath ?? "")).Append("\"}");
            }
            result.Append("]}");
            return result.ToString();
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        public static int SetMode(DisplayMode mode, bool persist, string reason)
        {
            if (mode == DisplayMode.Stream || mode == DisplayMode.Transition) throw new ArgumentException("Use stream-start for Stream mode.");

            int width;
            int height;
            int refresh;
            int limit;
            bool hdr;
            string identity;
            switch (mode)
            {
                case DisplayMode.Tv:
                    identity = "TV"; width = TvWidth; height = TvHeight; refresh = TvRefresh; limit = TvLimit; hdr = true; break;
                case DisplayMode.Alienware:
                    identity = "Alienware"; width = AwWidth; height = AwHeight; refresh = AwRefresh; limit = AwLimit; hdr = true; break;
                case DisplayMode.Vdd:
                    identity = "VDD"; width = 1920; height = 1080; refresh = 120; limit = 120; hdr = false; break;
                default:
                    throw new ArgumentOutOfRangeException("mode");
            }

            return ApplyProfile(mode, identity, width, height, refresh, hdr, limit, persist, reason);
        }

        private static int ApplyProfile(DisplayMode finalMode, string identity, int width, int height, int refresh, bool hdr, int limit, bool persist, string reason)
        {
            ControllerState original = ControllerState.Load();
            ControllerState transition = original.Clone();
            transition.Mode = DisplayMode.Transition;
            transition.TransitionDestination = finalMode;
            transition.Save();
            Logger.Info("Transition requested: " + identity + " " + width + "x" + height + "@" + refresh + " HDR=" + hdr + " reason=" + reason);

            try
            {
                DisplayManager.ActivateOnly(identity);
                DisplayManager.SetActiveMode(identity, width, height, refresh);
                DisplayManager.SetHdr(identity, hdr);
                DisplayManager.Verify(identity, width, height, refresh, hdr, TimeSpan.FromSeconds(6));

                try
                {
                    RtssManager.SetLimit(limit);
                }
                catch (Exception capError)
                {
                    // A frame-cap failure must not strand the machine in an otherwise
                    // healthy display transition. It is reported and retried when the
                    // profile is next applied.
                    Logger.Error("Display transition succeeded, but RTSS cap failed", capError);
                    Console.Error.WriteLine("Warning: display profile is active, but RTSS cap failed: " + capError.Message);
                }

                ControllerState completed = ControllerState.Load();
                completed.Mode = finalMode;
                completed.TransitionDestination = finalMode;
                if (finalMode != DisplayMode.Stream)
                {
                    completed.StreamClient = "";
                    completed.StreamWidth = 0;
                    completed.StreamHeight = 0;
                    completed.StreamFps = 0;
                    completed.StreamHdr = false;
                    completed.StreamStartedUtc = DateTime.MinValue;
                    completed.SawSunshineUdp = false;
                }
                if (persist) completed.Save();
                Logger.Info("Transition verified: " + finalMode);
                Console.WriteLine("Active profile: " + finalMode + " (" + width + "x" + height + "@" + refresh + ", HDR=" + hdr + ", cap=" + limit + ")");
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Transition failed: " + finalMode, ex);
                BestEffortRestore(original);
                Console.Error.WriteLine("Failed to apply " + finalMode + ": " + ex.Message);
                return 10;
            }
        }

        private static void BestEffortRestore(ControllerState original)
        {
            try
            {
                string identity;
                int width;
                int height;
                int refresh;
                int limit;
                bool hdr;

                if (original.Mode == DisplayMode.Alienware)
                {
                    identity = "Alienware"; width = AwWidth; height = AwHeight; refresh = AwRefresh; limit = AwLimit; hdr = true;
                }
                else if (original.Mode == DisplayMode.Vdd || original.Mode == DisplayMode.Stream)
                {
                    identity = "VDD";
                    width = original.StreamWidth > 0 ? original.StreamWidth : 1920;
                    height = original.StreamHeight > 0 ? original.StreamHeight : 1080;
                    refresh = original.StreamFps > 0 ? original.StreamFps : 120;
                    limit = refresh;
                    hdr = original.Mode == DisplayMode.Stream && original.StreamHdr;
                }
                else
                {
                    identity = "TV"; width = TvWidth; height = TvHeight; refresh = TvRefresh; limit = TvLimit; hdr = true;
                }

                DisplayManager.ActivateOnly(identity);
                DisplayManager.SetActiveMode(identity, width, height, refresh);
                DisplayManager.SetHdr(identity, hdr);
                DisplayManager.Verify(identity, width, height, refresh, hdr, TimeSpan.FromSeconds(6));
                try { RtssManager.SetLimit(limit); }
                catch (Exception capError) { Logger.Error("RTSS rollback cap failed", capError); }
                original.Save();
                Logger.Info("Rollback verified: " + original.Mode);
            }
            catch (Exception rollbackError)
            {
                original.Save();
                Logger.Error("Rollback failed; saved the last known controller state", rollbackError);
            }
        }

        public static int StreamStart()
        {
            ControllerState state = ControllerState.Load();
            if (state.Mode == DisplayMode.Stream)
            {
                Console.Error.WriteLine("A streaming display mode is already active.");
                return 21;
            }

            StreamRequest request;
            try { request = StreamRequest.FromEnvironment(); }
            catch (Exception ex)
            {
                Logger.Error("Invalid stream request", ex);
                Console.Error.WriteLine(ex.Message);
                return 22;
            }

            Logger.Info("Stream request: profile=" + request.ClientName + " mode=" + request.Width + "x" + request.Height + "@" + request.Fps + " HDR=" + request.Hdr);
            int result = ApplyProfile(DisplayMode.Stream, "VDD", request.Width, request.Height, request.Fps, request.Hdr, request.Fps, false, "sunshine:" + request.ClientName);
            if (result != 0)
            {
                Logger.Warning("Stream setup failed; attempting LG rollback.");
                SetMode(DisplayMode.Tv, true, "stream-start-rollback");
                return result;
            }

            ControllerState streaming = ControllerState.Load();
            streaming.Mode = DisplayMode.Stream;
            streaming.TransitionDestination = DisplayMode.Stream;
            streaming.StreamClient = request.ClientName;
            streaming.StreamWidth = request.Width;
            streaming.StreamHeight = request.Height;
            streaming.StreamFps = request.Fps;
            streaming.StreamHdr = request.Hdr;
            streaming.StreamStartedUtc = DateTime.UtcNow;
            streaming.SawSunshineUdp = false;
            streaming.Save();
            return 0;
        }

        public static int StreamEnd()
        {
            Logger.Info("Stream end requested; restoring the selected TV after settle delay.");
            Thread.Sleep(3000);
            return SetMode(DisplayMode.Tv, true, "stream-end");
        }

        public static int Reapply()
        {
            ControllerState state = ControllerState.Load();
            if (state.Mode == DisplayMode.Stream)
            {
                return ApplyProfile(DisplayMode.Stream, "VDD", state.StreamWidth, state.StreamHeight, state.StreamFps, state.StreamHdr, state.StreamFps, true, "reapply");
            }
            if (state.Mode == DisplayMode.Transition)
            {
                Logger.Warning("Incomplete transition found; restoring the selected TV.");
                return SetMode(DisplayMode.Tv, true, "transition-recovery");
            }
            return SetMode(state.Mode, true, "reapply");
        }

        public static int WatchdogRecover()
        {
            ControllerState state = ControllerState.Load();
            if (state.Mode == DisplayMode.Transition)
            {
                return SetMode(DisplayMode.Tv, true, "stale-transition-watchdog");
            }

            string desired = DesiredIdentity(state.Mode);
            if (desired == "TV" || desired == "Alienware")
            {
                bool available = DisplayManager.EnumerateAllTargets().Any(delegate(DisplayTarget d) { return d.Identity == desired && d.Available; });
                if (!available)
                {
                    Logger.Warning(desired + " path was unavailable for five seconds; activating the VDD safety display.");
                    return SetMode(DisplayMode.Vdd, true, "selected-physical-path-unavailable");
                }
            }
            return Reapply();
        }

        private static string DesiredIdentity(DisplayMode mode)
        {
            if (mode == DisplayMode.Tv) return "TV";
            if (mode == DisplayMode.Alienware) return "Alienware";
            if (mode == DisplayMode.Vdd || mode == DisplayMode.Stream) return "VDD";
            return "";
        }

        public static bool IsTopologyCorrect(ControllerState state)
        {
            string desired = DesiredIdentity(state.Mode);
            if (desired.Length == 0) return true;
            List<DisplayTarget> active = DisplayManager.EnumerateAllTargets().Where(delegate(DisplayTarget d) { return d.Active; }).ToList();
            return active.Count == 1 && active[0].Identity == desired;
        }
    }

    internal sealed class StreamRequest
    {
        public string ClientName;
        public int Width;
        public int Height;
        public int Fps;
        public bool Hdr;

        public static StreamRequest FromEnvironment()
        {
            StreamRequest request = new StreamRequest();
            request.ClientName = Environment.GetEnvironmentVariable("SUNSHINE_CLIENT_NAME") ?? "";
            int width;
            int height;
            int fps;
            bool hdr;
            if (!int.TryParse(Environment.GetEnvironmentVariable("SUNSHINE_CLIENT_WIDTH"), NumberStyles.Integer, CultureInfo.InvariantCulture, out width)) throw new InvalidOperationException("Sunshine did not provide a valid client width.");
            if (!int.TryParse(Environment.GetEnvironmentVariable("SUNSHINE_CLIENT_HEIGHT"), NumberStyles.Integer, CultureInfo.InvariantCulture, out height)) throw new InvalidOperationException("Sunshine did not provide a valid client height.");
            double fpsValue;
            if (!double.TryParse(Environment.GetEnvironmentVariable("SUNSHINE_CLIENT_FPS"), NumberStyles.Float, CultureInfo.InvariantCulture, out fpsValue)) throw new InvalidOperationException("Sunshine did not provide a valid client FPS.");
            fps = (int)Math.Round(fpsValue);
            string hdrValue = (Environment.GetEnvironmentVariable("SUNSHINE_CLIENT_HDR") ?? "false").Trim();
            hdr = hdrValue.Equals("true", StringComparison.OrdinalIgnoreCase) || hdrValue.Equals("1", StringComparison.OrdinalIgnoreCase) || hdrValue.Equals("enabled", StringComparison.OrdinalIgnoreCase) || hdrValue.Equals("on", StringComparison.OrdinalIgnoreCase);

            bool supportedResolution = (width == 1920 && height == 1080) || (width == 3840 && height == 2160);
            bool supportedFps = fps == 60 || fps == 90 || fps == 120;
            if (!supportedResolution || !supportedFps)
            {
                throw new InvalidOperationException("Unsupported Sunshine client request: " + width + "x" + height + "@" + fps + ". Expected 1080p or 4K at 60, 90, or 120 FPS.");
            }

            if (request.ClientName.Length == 0)
            {
                request.ClientName = width == 3840 && height == 2160 ? "4K client" : hdr ? "1080p HDR client" : "1080p SDR client";
            }

            request.Width = width;
            request.Height = height;
            request.Fps = fps;
            request.Hdr = hdr;
            return request;
        }
    }

    internal static class DisplayManager
    {
        public static void ValidateCurrentConfiguration()
        {
            uint pathCount;
            uint modeCount;
            uint queryFlags = NativeMethods.QDC_ONLY_ACTIVE_PATHS | NativeMethods.QDC_VIRTUAL_MODE_AWARE;
            int code = NativeMethods.GetDisplayConfigBufferSizes(queryFlags, out pathCount, out modeCount);
            if (code != 0) throw new Win32Exception(code, "GetDisplayConfigBufferSizes failed (error " + code + ")");
            NativeMethods.DISPLAYCONFIG_PATH_INFO[] paths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
            NativeMethods.DISPLAYCONFIG_MODE_INFO[] modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];
            code = NativeMethods.QueryDisplayConfig(queryFlags, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (code != 0) throw new Win32Exception(code, "QueryDisplayConfig failed (error " + code + ")");
            uint setFlags = NativeMethods.SDC_VALIDATE | NativeMethods.SDC_USE_SUPPLIED_DISPLAY_CONFIG | NativeMethods.SDC_VIRTUAL_MODE_AWARE;
            code = NativeMethods.SetDisplayConfigWithModes(pathCount, paths, modeCount, modes, setFlags);
            if (code != 0) throw new Win32Exception(code, "SetDisplayConfig round trip failed (error " + code + ")");
        }

        public static List<DisplayTarget> EnumerateAllTargets()
        {
            uint pathCount;
            uint modeCount;
            uint queryFlags = NativeMethods.QDC_ALL_PATHS;
            int code = NativeMethods.GetDisplayConfigBufferSizes(queryFlags, out pathCount, out modeCount);
            if (code != 0) throw new Win32Exception(code, "GetDisplayConfigBufferSizes failed");
            NativeMethods.DISPLAYCONFIG_PATH_INFO[] paths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
            NativeMethods.DISPLAYCONFIG_MODE_INFO[] modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];
            code = NativeMethods.QueryDisplayConfig(queryFlags, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
            if (code != 0) throw new Win32Exception(code, "QueryDisplayConfig failed");

            Dictionary<string, DisplayTarget> unique = new Dictionary<string, DisplayTarget>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < pathCount; index++)
            {
                NativeMethods.DISPLAYCONFIG_PATH_INFO path = paths[index];
                string friendly;
                string devicePath;
                GetTargetName(path.targetInfo.adapterId, path.targetInfo.id, out friendly, out devicePath);
                string key = path.targetInfo.adapterId.ToString() + ":" + path.targetInfo.id;
                DisplayTarget existing;
                bool active = (path.flags & NativeMethods.DISPLAYCONFIG_PATH_ACTIVE) != 0;
                if (!unique.TryGetValue(key, out existing) || (active && !existing.Active))
                {
                    DisplayTarget target = new DisplayTarget();
                    target.Path = path;
                    target.FriendlyName = friendly;
                    target.DevicePath = devicePath;
                    target.Active = active;
                    target.Available = path.targetInfo.targetAvailable;
                    if (active)
                    {
                        target.GdiName = GetSourceName(path.sourceInfo.adapterId, path.sourceInfo.id);
                        ReadCurrentMode(target);
                        ReadHdr(target);
                    }
                    unique[key] = target;
                }
            }
            return unique.Values.OrderBy(delegate(DisplayTarget d) { return d.Identity; }).ThenBy(delegate(DisplayTarget d) { return d.FriendlyName; }).ToList();
        }

        public static void ActivateOnly(string identity)
        {
            NativeMethods.DISPLAYCONFIG_PATH_INFO[] supplied = BuildOnlyPath(identity);

            uint validateFlags = NativeMethods.SDC_VALIDATE | NativeMethods.SDC_USE_SUPPLIED_DISPLAY_CONFIG | NativeMethods.SDC_ALLOW_CHANGES;
            int code = NativeMethods.SetDisplayConfig(1, supplied, 0, IntPtr.Zero, validateFlags);
            if (code != 0) throw new Win32Exception(code, "Windows rejected the proposed " + identity + "-only topology (error " + code + ")");

            uint applyFlags = NativeMethods.SDC_APPLY | NativeMethods.SDC_USE_SUPPLIED_DISPLAY_CONFIG | NativeMethods.SDC_SAVE_TO_DATABASE | NativeMethods.SDC_ALLOW_CHANGES;
            code = NativeMethods.SetDisplayConfig(1, supplied, 0, IntPtr.Zero, applyFlags);
            if (code != 0) throw new Win32Exception(code, "Failed to activate only " + identity + " (error " + code + ")");
            WaitForOnly(identity, TimeSpan.FromSeconds(6));
        }

        public static void ValidateOnly(string identity)
        {
            NativeMethods.DISPLAYCONFIG_PATH_INFO[] supplied = BuildOnlyPath(identity);
            uint flags = NativeMethods.SDC_VALIDATE | NativeMethods.SDC_USE_SUPPLIED_DISPLAY_CONFIG | NativeMethods.SDC_ALLOW_CHANGES;
            int code = NativeMethods.SetDisplayConfig(1, supplied, 0, IntPtr.Zero, flags);
            if (code != 0) throw new Win32Exception(code, "Windows rejected the proposed " + identity + "-only topology (error " + code + ")");
        }

        private static NativeMethods.DISPLAYCONFIG_PATH_INFO[] BuildOnlyPath(string identity)
        {
            List<DisplayTarget> all = EnumerateAllTargets();
            DisplayTarget selected = all.FirstOrDefault(delegate(DisplayTarget d) { return d.Identity == identity && d.Active; });
            if (selected == null) selected = all.FirstOrDefault(delegate(DisplayTarget d) { return d.Identity == identity && d.Available; });
            if (selected == null) throw new InvalidOperationException(identity + " display path is not available.");

            NativeMethods.DISPLAYCONFIG_PATH_INFO path = selected.Path;
            path.flags = NativeMethods.DISPLAYCONFIG_PATH_ACTIVE;
            path.sourceInfo.modeInfoIdx = NativeMethods.DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
            path.targetInfo.modeInfoIdx = NativeMethods.DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
            return new NativeMethods.DISPLAYCONFIG_PATH_INFO[] { path };
        }

        private static void WaitForOnly(string identity, TimeSpan timeout)
        {
            Stopwatch timer = Stopwatch.StartNew();
            Exception last = null;
            while (timer.Elapsed < timeout)
            {
                try
                {
                    List<DisplayTarget> active = EnumerateAllTargets().Where(delegate(DisplayTarget d) { return d.Active; }).ToList();
                    if (active.Count == 1 && active[0].Identity == identity) return;
                }
                catch (Exception ex) { last = ex; }
                Thread.Sleep(250);
            }
            if (last != null) throw new InvalidOperationException("Timed out waiting for " + identity + " topology.", last);
            throw new InvalidOperationException("Timed out waiting for " + identity + " topology.");
        }

        public static void SetActiveMode(string identity, int width, int height, int refresh)
        {
            DisplayTarget target = EnumerateAllTargets().FirstOrDefault(delegate(DisplayTarget d) { return d.Active && d.Identity == identity; });
            if (target == null || string.IsNullOrEmpty(target.GdiName)) throw new InvalidOperationException("Active " + identity + " source has no GDI display name.");

            NativeMethods.DEVMODE mode = new NativeMethods.DEVMODE();
            mode.dmSize = (short)Marshal.SizeOf(typeof(NativeMethods.DEVMODE));
            if (!NativeMethods.EnumDisplaySettings(target.GdiName, NativeMethods.ENUM_CURRENT_SETTINGS, ref mode)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read current mode for " + identity);
            mode.dmPelsWidth = width;
            mode.dmPelsHeight = height;
            mode.dmDisplayFrequency = refresh;
            mode.dmFields = NativeMethods.DM_PELSWIDTH | NativeMethods.DM_PELSHEIGHT | NativeMethods.DM_DISPLAYFREQUENCY;
            int code = NativeMethods.ChangeDisplaySettingsEx(target.GdiName, ref mode, IntPtr.Zero, NativeMethods.CDS_UPDATEREGISTRY, IntPtr.Zero);
            if (code != NativeMethods.DISP_CHANGE_SUCCESSFUL) throw new InvalidOperationException("ChangeDisplaySettingsEx failed for " + identity + " with code " + code);
        }

        public static void SetHdr(string identity, bool enabled)
        {
            DisplayTarget target = EnumerateAllTargets().FirstOrDefault(delegate(DisplayTarget d) { return d.Active && d.Identity == identity; });
            if (target == null) throw new InvalidOperationException("Cannot set HDR because " + identity + " is not active.");
            NativeMethods.DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE packet = new NativeMethods.DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE();
            packet.header.type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE;
            packet.header.size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE));
            packet.header.adapterId = target.Path.targetInfo.adapterId;
            packet.header.id = target.Path.targetInfo.id;
            packet.enableAdvancedColor = enabled ? 1U : 0U;
            int code = NativeMethods.DisplayConfigSetDeviceInfo(ref packet);
            if (code != 0) throw new Win32Exception(code, "Failed to set HDR=" + enabled + " on " + identity);
        }

        public static void Verify(string identity, int width, int height, int refresh, bool hdr, TimeSpan timeout)
        {
            Stopwatch timer = Stopwatch.StartNew();
            string last = "";
            while (timer.Elapsed < timeout)
            {
                List<DisplayTarget> active = EnumerateAllTargets().Where(delegate(DisplayTarget d) { return d.Active; }).ToList();
                if (active.Count == 1 && active[0].Identity == identity)
                {
                    DisplayTarget target = active[0];
                    bool modeMatches = target.Width == width && target.Height == height && Math.Abs(target.RefreshRate - refresh) <= 1;
                    bool hdrMatches = target.HdrQuerySucceeded && target.HdrSupported && target.HdrEnabled == hdr;
                    if (modeMatches && hdrMatches) return;
                    last = "mode=" + target.Width + "x" + target.Height + "@" + target.RefreshRate + " HDRQuery=" + target.HdrQuerySucceeded + " HDRSupported=" + target.HdrSupported + " HDREnabled=" + target.HdrEnabled;
                }
                else last = "active display count=" + active.Count;
                Thread.Sleep(300);
            }
            throw new InvalidOperationException("Verification failed for " + identity + ": " + last);
        }

        private static void GetTargetName(NativeMethods.LUID adapter, uint id, out string friendly, out string devicePath)
        {
            NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME name = new NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME();
            name.header.type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
            name.header.size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME));
            name.header.adapterId = adapter;
            name.header.id = id;
            int code = NativeMethods.DisplayConfigGetDeviceInfo(ref name);
            if (code != 0)
            {
                friendly = "";
                devicePath = "";
                return;
            }
            friendly = name.monitorFriendlyDeviceName ?? "";
            devicePath = name.monitorDevicePath ?? "";
        }

        private static string GetSourceName(NativeMethods.LUID adapter, uint id)
        {
            NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME name = new NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            name.header.type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            name.header.size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME));
            name.header.adapterId = adapter;
            name.header.id = id;
            int code = NativeMethods.DisplayConfigGetDeviceInfo(ref name);
            return code == 0 ? name.viewGdiDeviceName : "";
        }

        private static void ReadCurrentMode(DisplayTarget target)
        {
            if (string.IsNullOrEmpty(target.GdiName)) return;
            NativeMethods.DEVMODE mode = new NativeMethods.DEVMODE();
            mode.dmSize = (short)Marshal.SizeOf(typeof(NativeMethods.DEVMODE));
            if (NativeMethods.EnumDisplaySettings(target.GdiName, NativeMethods.ENUM_CURRENT_SETTINGS, ref mode))
            {
                target.Width = mode.dmPelsWidth;
                target.Height = mode.dmPelsHeight;
                target.RefreshRate = mode.dmDisplayFrequency;
            }
        }

        private static void ReadHdr(DisplayTarget target)
        {
            NativeMethods.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO packet = new NativeMethods.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
            packet.header.type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
            packet.header.size = (uint)Marshal.SizeOf(typeof(NativeMethods.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO));
            packet.header.adapterId = target.Path.targetInfo.adapterId;
            packet.header.id = target.Path.targetInfo.id;
            int code = NativeMethods.DisplayConfigGetDeviceInfo(ref packet);
            target.HdrQuerySucceeded = code == 0;
            if (code == 0)
            {
                target.HdrSupported = (packet.value & 1U) != 0;
                target.HdrEnabled = (packet.value & 2U) != 0;
            }
        }
    }

    internal static class RtssManager
    {
        public static void SetLimit(int limit)
        {
            if (!File.Exists(Paths.RtssExecutable) || !File.Exists(Paths.RtssHooks)) throw new FileNotFoundException("RTSS is not installed at the expected location.");
            Process running = Process.GetProcessesByName("RTSS").FirstOrDefault();
            if (running == null)
            {
                ProcessStartInfo start = new ProcessStartInfo(Paths.RtssExecutable);
                start.WorkingDirectory = Paths.RtssDirectory;
                start.UseShellExecute = true;
                start.WindowStyle = ProcessWindowStyle.Minimized;
                Process.Start(start);
                Stopwatch timer = Stopwatch.StartNew();
                while (timer.Elapsed < TimeSpan.FromSeconds(8) && Process.GetProcessesByName("RTSS").Length == 0) Thread.Sleep(200);
                if (Process.GetProcessesByName("RTSS").Length == 0) throw new InvalidOperationException("RTSS did not start.");
            }

            IntPtr value = Marshal.AllocHGlobal(4);
            try
            {
                NativeMethods.RtssLoadProfile("");
                Marshal.WriteInt32(value, limit);
                if (!NativeMethods.RtssSetProfileProperty("FramerateLimit", value, 4)) throw new InvalidOperationException("RTSS rejected the frame limit.");
                NativeMethods.RtssSaveProfile("");
                NativeMethods.RtssUpdateProfiles();

                NativeMethods.RtssLoadProfile("");
                Marshal.WriteInt32(value, -1);
                if (!NativeMethods.RtssGetProfileProperty("FramerateLimit", value, 4)) throw new InvalidOperationException("Could not verify the RTSS frame limit.");
                int actual = Marshal.ReadInt32(value);
                if (actual != limit) throw new InvalidOperationException("RTSS frame limit verification failed: expected " + limit + ", found " + actual);
            }
            finally { Marshal.FreeHGlobal(value); }
        }
    }

    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon icon;
        private readonly Icon trayIcon;
        private readonly System.Windows.Forms.Timer timer;
        private readonly Mutex singleton;
        private DateTime topologyWrongSince = DateTime.MinValue;

        public TrayApplicationContext()
        {
            bool created;
            singleton = new Mutex(true, @"Local\GamingDisplayController.Tray", out created);
            if (!created)
            {
                singleton.Dispose();
                ExitThread();
                return;
            }

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("TV only (LG CX / LG C2)", null, delegate { RunFromTray(DisplayMode.Tv); });
            menu.Items.Add("Alienware only", null, delegate { RunFromTray(DisplayMode.Alienware); });
            menu.Items.Add("Virtual display only", null, delegate { RunFromTray(DisplayMode.Vdd); });
            menu.Items.Add("Reapply current lock", null, delegate { RunAction(Controller.Reapply); });
            menu.Items.Add("Show status", null, delegate { MessageBox.Show(Controller.GetStatus(false), "Gaming Display Controller"); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit controller", null, delegate { ExitThread(); });

            trayIcon = LoadTrayIcon();
            icon = new NotifyIcon();
            icon.Icon = trayIcon;
            icon.Text = "Gaming Display Controller";
            icon.ContextMenuStrip = menu;
            icon.Visible = true;

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 2000;
            timer.Tick += OnTimer;
            timer.Start();

            Logger.Info("Tray controller started.");
        }

        private static Icon LoadTrayIcon()
        {
            try
            {
                using (Stream stream = typeof(TrayApplicationContext).Assembly.GetManifestResourceStream("GamingDisplayController.TrayIcon"))
                {
                    if (stream != null)
                    {
                        using (Icon embedded = new Icon(stream, new Size(32, 32)))
                        {
                            return (Icon)embedded.Clone();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Could not load the custom tray icon; using the Windows fallback icon", ex);
            }

            return (Icon)SystemIcons.Application.Clone();
        }

        private void RunFromTray(DisplayMode mode)
        {
            RunAction(delegate { return Controller.SetMode(mode, true, "tray"); });
        }

        private void RunAction(Func<int> action)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                int result = Controller.RunLocked(action);
                if (result != 0 && icon != null)
                {
                    icon.ShowBalloonTip(5000, "Display switch failed", "See " + Paths.LogFile, ToolTipIcon.Error);
                }
            });
        }

        private void OnTimer(object sender, EventArgs args)
        {
            try
            {
                ControllerState state = ControllerState.Load();
                if (state.Mode == DisplayMode.Transition)
                {
                    if (topologyWrongSince == DateTime.MinValue) topologyWrongSince = DateTime.UtcNow;
                    if (DateTime.UtcNow - topologyWrongSince > TimeSpan.FromSeconds(10))
                    {
                        topologyWrongSince = DateTime.MinValue;
                        RunAction(Controller.WatchdogRecover);
                    }
                    return;
                }

                bool correct = Controller.IsTopologyCorrect(state);
                if (!correct)
                {
                    if (topologyWrongSince == DateTime.MinValue) topologyWrongSince = DateTime.UtcNow;
                    if (DateTime.UtcNow - topologyWrongSince > TimeSpan.FromSeconds(5))
                    {
                        topologyWrongSince = DateTime.MinValue;
                        RunAction(Controller.WatchdogRecover);
                    }
                }
                else topologyWrongSince = DateTime.MinValue;

                // Sunshine owns session lifetime and reliably runs stream-end on
                // Moonlight disconnect. The tray only enforces the saved display
                // topology; inferring a session from Sunshine's background UDP
                // sockets can produce false positives or false restores.
            }
            catch (Exception ex) { Logger.Error("Tray watchdog error", ex); }
        }

        protected override void ExitThreadCore()
        {
            if (timer != null) timer.Dispose();
            if (icon != null) { icon.Visible = false; icon.Dispose(); }
            if (trayIcon != null) trayIcon.Dispose();
            if (singleton != null) singleton.Dispose();
            Logger.Info("Tray controller stopped.");
            base.ExitThreadCore();
        }
    }

    internal static class NativeMethods
    {
        public const uint QDC_ALL_PATHS = 1;
        public const uint QDC_ONLY_ACTIVE_PATHS = 2;
        public const uint QDC_VIRTUAL_MODE_AWARE = 0x10;
        public const uint DISPLAYCONFIG_PATH_ACTIVE = 1;
        public const uint DISPLAYCONFIG_PATH_MODE_IDX_INVALID = 0xFFFFFFFF;
        public const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x20;
        public const uint SDC_TOPOLOGY_SUPPLIED = 0x10;
        public const uint SDC_VALIDATE = 0x40;
        public const uint SDC_APPLY = 0x80;
        public const uint SDC_SAVE_TO_DATABASE = 0x200;
        public const uint SDC_ALLOW_CHANGES = 0x400;
        public const uint SDC_ALLOW_PATH_ORDER_CHANGES = 0x2000;
        public const uint SDC_VIRTUAL_MODE_AWARE = 0x8000;
        public const int DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
        public const int DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;
        public const int DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;
        public const int DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10;
        public const int ENUM_CURRENT_SETTINGS = -1;
        public const int DM_PELSWIDTH = 0x80000;
        public const int DM_PELSHEIGHT = 0x100000;
        public const int DM_DISPLAYFREQUENCY = 0x400000;
        public const int CDS_UPDATEREGISTRY = 1;
        public const int DISP_CHANGE_SUCCESSFUL = 0;

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
            public override string ToString() { return HighPart.ToString("X8", CultureInfo.InvariantCulture) + LowPart.ToString("X8", CultureInfo.InvariantCulture); }
        }

        [StructLayout(LayoutKind.Sequential)] public struct DISPLAYCONFIG_RATIONAL { public uint Numerator; public uint Denominator; }
        [StructLayout(LayoutKind.Sequential)] public struct DISPLAYCONFIG_PATH_SOURCE_INFO { public LUID adapterId; public uint id; public uint modeInfoIdx; public uint statusFlags; }
        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public int outputTechnology;
            public int rotation;
            public int scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public int scanLineOrdering;
            [MarshalAs(UnmanagedType.Bool)] public bool targetAvailable;
            public uint statusFlags;
        }
        [StructLayout(LayoutKind.Sequential)] public struct DISPLAYCONFIG_PATH_INFO { public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo; public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo; public uint flags; }
        [StructLayout(LayoutKind.Explicit, Size = 64)] public struct DISPLAYCONFIG_MODE_INFO { [FieldOffset(0)] public int infoType; [FieldOffset(4)] public uint id; [FieldOffset(8)] public LUID adapterId; }
        [StructLayout(LayoutKind.Sequential)] public struct DISPLAYCONFIG_DEVICE_INFO_HEADER { public int type; public uint size; public LUID adapterId; public uint id; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint flags;
            public int outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string monitorFriendlyDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string monitorDevicePath;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string viewGdiDeviceName;
        }
        [StructLayout(LayoutKind.Sequential)] public struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO { public DISPLAYCONFIG_DEVICE_INFO_HEADER header; public uint value; public int colorEncoding; public uint bitsPerColorChannel; }
        [StructLayout(LayoutKind.Sequential)] public struct DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE { public DISPLAYCONFIG_DEVICE_INFO_HEADER header; public uint enableAdvancedColor; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)] public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }


        [DllImport("user32.dll")] public static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);
        [DllImport("user32.dll")] public static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] DISPLAYCONFIG_PATH_INFO[] paths, ref uint modeCount, [Out] DISPLAYCONFIG_MODE_INFO[] modes, IntPtr topologyId);
        [DllImport("user32.dll")] public static extern int SetDisplayConfig(uint pathCount, [In] DISPLAYCONFIG_PATH_INFO[] paths, uint modeCount, IntPtr modes, uint flags);
        [DllImport("user32.dll", EntryPoint = "SetDisplayConfig")] public static extern int SetDisplayConfigWithModes(uint pathCount, [In] DISPLAYCONFIG_PATH_INFO[] paths, uint modeCount, [In] DISPLAYCONFIG_MODE_INFO[] modes, uint flags);
        [DllImport("user32.dll")] public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME packet);
        [DllImport("user32.dll")] public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME packet);
        [DllImport("user32.dll")] public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO packet);
        [DllImport("user32.dll")] public static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE packet);
        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)] public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
        [DllImport("user32.dll", CharSet = CharSet.Ansi)] public static extern int ChangeDisplaySettingsEx(string deviceName, ref DEVMODE devMode, IntPtr hwnd, int flags, IntPtr lParam);

#if RTSS_X86
        private const string RtssHooksLibrary = @"C:\Program Files (x86)\RivaTuner Statistics Server\RTSSHooks.dll";
#else
        private const string RtssHooksLibrary = @"C:\Program Files (x86)\RivaTuner Statistics Server\RTSSHooks64.dll";
#endif
        [DllImport(RtssHooksLibrary, EntryPoint = "SetProfileProperty", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] public static extern bool RtssSetProfileProperty(string name, IntPtr data, uint size);
        [DllImport(RtssHooksLibrary, EntryPoint = "GetProfileProperty", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] public static extern bool RtssGetProfileProperty(string name, IntPtr data, uint size);
        [DllImport(RtssHooksLibrary, EntryPoint = "LoadProfile", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] public static extern void RtssLoadProfile(string profile);
        [DllImport(RtssHooksLibrary, EntryPoint = "SaveProfile", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] public static extern void RtssSaveProfile(string profile);
        [DllImport(RtssHooksLibrary, EntryPoint = "UpdateProfiles", CallingConvention = CallingConvention.Cdecl)] public static extern void RtssUpdateProfiles();
    }
}
