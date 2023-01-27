using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace ModStartupImpactStats
{
    [HarmonyPatch]
    public static class Def_Patch
    {
        [HarmonyPrepare]
        public static bool Prepare()
        {
            return Prefs.LogVerbose;
        }
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> GetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(Def)))
            {
                if (method.CanBePatched() && method.IsStatic is false)
                {
                    yield return method;
                }
            }
            foreach (var defType in typeof(Def).AllSubclasses())
            {
                foreach (var method in AccessTools.GetDeclaredMethods(defType))
                {
                    if (method.CanBePatched() && method.IsStatic is false)
                    {
                        yield return method;
                    }
                }
            }
        }

        public static Stopwatch stopwatch = new Stopwatch();
        public static void Prefix()
        {
            stopwatch.Restart();
        }
        public static void Postfix(Def __instance)
        {
            stopwatch.Stop();
            var mod = __instance.modContentPack;
            if (mod != null)
            {
                ModImpactData.RegisterImpact(mod, "Defs", "Def init", stopwatch.SecondsElapsed());
            }
        }
    }
}

