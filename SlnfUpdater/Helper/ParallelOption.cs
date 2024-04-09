using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlnfUpdater.Helper
{
    public static class ParallelOption
    {
        public static int MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount - 2);
    }
}
