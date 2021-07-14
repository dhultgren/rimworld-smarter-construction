namespace SmarterConstruction
{
    public enum SCLogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4,
        Verbose = 5
    }

    public class SmarterConstructionSettings
    {
        public readonly SCLogLevel LogLevel = SCLogLevel.Debug;
        public readonly int FinishCacheTicks = 0;
        public readonly int GetJobCacheTicks = 20;
        public readonly int GetJobThrottleCacheTicks = 600;
        public readonly int MaxRegionSize = 50;
        public readonly int MaxDistanceForPriority = 15;
    }
}
