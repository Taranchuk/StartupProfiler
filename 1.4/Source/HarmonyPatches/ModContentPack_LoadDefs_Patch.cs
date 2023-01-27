using HarmonyLib;
using System.Diagnostics;
using Verse;

namespace ModStartupImpactStats
{
    [HarmonyPatch(typeof(ModContentPack), "LoadDefs")]
    public static class ModContentPack_LoadDefs_Patch
    {
        public static Stopwatch stopwatch = new Stopwatch();

        [HarmonyPrepare]
        public static bool Prepare()
        {
            return Prefs.LogVerbose;
        }

        static void Prefix()
        {
            stopwatch.Restart();
        }

        public static void Postfix(ModContentPack __instance)
        {
            stopwatch.Stop();
            ModImpactData.RegisterImpact(__instance, "Defs", "LoadDefs", stopwatch.SecondsElapsed());
        }
    }
}

