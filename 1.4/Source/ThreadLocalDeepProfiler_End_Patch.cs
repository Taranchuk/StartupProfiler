using HarmonyLib;
using Verse;
using static Verse.ThreadLocalDeepProfiler;

namespace StartupProfiler
{

    [HarmonyPatch(typeof(ThreadLocalDeepProfiler), "End")]
    public static class ThreadLocalDeepProfiler_End_Patch
    {
        public static bool Prefix(ThreadLocalDeepProfiler __instance)
        {
            if (!Prefs.LogVerbose)
            {
                return false;
            }
            if (__instance.watchers.Count == 0)
            {
                return false;
            }
            Watcher watcher = __instance.watchers.Pop();
            watcher.Watch.Stop();
            if (__instance.watchers.Count > 0)
            {
                __instance.watchers.Peek().AddChildResult(watcher);
            }
            else
            {
                __instance.Output(watcher);
            }
            return false;
        }
    }
}
