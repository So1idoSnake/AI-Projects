using System;
using System.Collections.Generic;

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
            Console.WriteLine("PASS: " + checks + " display recovery checks (no display or RTSS changes).");
            return 0;
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
