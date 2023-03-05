using HarmonyLib;
using System.Diagnostics;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(ModContentPack), "LoadDefs")]
    public static class ModContentPack_LoadDefs_Patch
    {
        public static Stopwatch stopwatch = new Stopwatch();
        static void Prefix()
        {
            stopwatch.Restart();
        }

        public static void Postfix(ModContentPack __instance)
        {
            stopwatch.Stop();
            ModImpactData.RegisterImpact(__instance, "Defs", "Loading XML files", stopwatch.SecondsElapsed());
        }
    }
}

