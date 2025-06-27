using System;
using System.Collections.Generic;
using Verse;

namespace StartupProfiler
{
    public class ReportSummary : Dictionary<ModContentPack, ModImpactData>
    {
        public ValueCollection AllEntries => Values;
        public TimeSpan TotalElapsed { get; set; }
    }
}
