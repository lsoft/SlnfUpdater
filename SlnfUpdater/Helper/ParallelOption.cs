using System;

namespace SlnfUpdater.Helper
{
    public static class ParallelOption
    {
        public static int MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount - 2);
    }
}
