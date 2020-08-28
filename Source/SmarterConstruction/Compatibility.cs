using System.Linq;
using Verse;

namespace SmarterConstruction
{
    class Compatibility
    {
        public static void InitCompatibility()
        {
            if (LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == "kapitanoczywisty.changemapedge"))
            {
                SmarterConstruction.Settings.ChangeMapEdgesCompatibility = true;
                DebugUtils.InfoLog("Activating Change map edge limit compatibility mode");
            }
        }
    }
}
