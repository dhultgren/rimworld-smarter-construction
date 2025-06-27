using Verse;

namespace SmarterConstruction
{
    public class DebugUtils
    {
        public static void ErrorLog(string text) => LogMessage(FormatLog(text), SCLogLevel.Error);
        public static void InfoLog(string text) => LogMessage(FormatLog(text), SCLogLevel.Info);
        public static void DebugLog(string text) => LogMessage(FormatLog(text), SCLogLevel.Debug);
        public static void VerboseLog(string text) => LogMessage(FormatLogWithTicks(text), SCLogLevel.Verbose);

        private static void LogMessage(string text, SCLogLevel level)
        {
            if (SmarterConstruction.Settings.LogLevel >= level) {
                Log.Message(text);
            }
        }

        private static string FormatLog(string log) => $"Smarter Construction: {log}";
        private static string FormatLogWithTicks(string log) => $"Smarter Construction @ {Find.TickManager.TicksGame}: {log}";
    }
}
