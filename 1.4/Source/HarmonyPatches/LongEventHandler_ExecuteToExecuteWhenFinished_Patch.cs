using HarmonyLib;
using System;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(LongEventHandler), "ExecuteWhenFinished")]
    public static class LongEventHandler_ExecuteWhenFinished_Patch
    {
        public static void Prefix(Action action)
        {
            if (action is null)
            {
                Log.Warning("Null action was added");
            }
        }
    }
    [HarmonyPatch(typeof(LongEventHandler), "ExecuteToExecuteWhenFinished")]
    public static class LongEventHandler_ExecuteToExecuteWhenFinished_Patch
    {
        public static void Prefix()
        {
            var removedCount = LongEventHandler.toExecuteWhenFinished.RemoveAll(x => x is null);
            if (removedCount > 0)
            {
                Log.Warning("Removed " + removedCount + " null actions");
            }
        }
    }
}

