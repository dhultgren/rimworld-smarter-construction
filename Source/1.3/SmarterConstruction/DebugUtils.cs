using Verse;

namespace SmarterConstruction
{
    public class DebugUtils
    {
        public static void InfoLog(string text)
        {
            if (SmarterConstruction.Settings.LogLevel >= SCLogLevel.Info)
            {
                Log.Message(FormatLog(text));
            }
        }

        public static void DebugLog(string text)
        {
            if (SmarterConstruction.Settings.LogLevel >= SCLogLevel.Debug)
            {
                Log.Message(FormatLog(text));
            }
        }

        public static void VerboseLog(string text)
        {
            if (SmarterConstruction.Settings.LogLevel >= SCLogLevel.Verbose)
            {
                Log.Message(FormatLogWithTicks(text));
            }
        }

        private static string FormatLog(string log) => $"Smarter Construction: {log}";
        private static string FormatLogWithTicks(string log) => $"Smarter Construction @ {Find.TickManager.TicksGame}: {log}";
    }
}
