using Verse;

namespace SmarterConstruction
{
    public class DebugUtils
    {
        public static void DebugLog(string text)
        {
            var message = "Smarter Construction: " + text;
//#if DEBUG
            Log.Message(message, true);
//#endif
        }

        public static void VerboseLog(string text)
        {
            Log.Message(Find.TickManager.TicksGame + ": " + text, true);
        }
    }
}
