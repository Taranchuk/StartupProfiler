using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Verse;
using static Verse.ThreadLocalDeepProfiler;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(ThreadLocalDeepProfiler), nameof(ThreadLocalDeepProfiler.Output))]
    public static class ThreadLocalDeepProfiler_Output_Patch
    {
        public static bool Prefix(ThreadLocalDeepProfiler __instance, Watcher root)
        {
            if (StartupProfilerMod.oldVerbose is false)
            {
                StringBuilder stringBuilder = new StringBuilder();
                List<Watcher> list = new List<Watcher>();
                list.Add(root);
                __instance.AppendStringRecursive(stringBuilder, root.Label, root.Children, root.ElapsedMilliseconds, 0, list, -1.0);
                return false;
            }
            return true;
        }
    }
}

