using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(ModContentPack), "LoadPatches")]
    public static class ModContentPack_LoadPatches_Patch
    {
        public static Dictionary<string, ModContentPack> modsByPatches = new Dictionary<string, ModContentPack>();
        
        public static Stopwatch stopwatch = new Stopwatch();

        [HarmonyPrepare]
        public static bool Prepare()
        {
            return Prefs.LogVerbose;
        }

        public static void Prefix(ModContentPack __instance)
        {
            stopwatch.Restart();
        }

        public static void Postfix(ModContentPack __instance)
        {
            stopwatch.Stop();
            ModImpactData.RegisterImpact(__instance, "XML Patches", "LoadPatches", stopwatch.SecondsElapsed());
            foreach (var patch in __instance.Patches)
            {
                modsByPatches[patch.sourceFile] = __instance;
            }
        }
    }
}

