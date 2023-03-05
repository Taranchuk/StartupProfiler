using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch]
    public static class Def_Patch
    {
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

        public static void Prefix(MethodBase __originalMethod)
        {
            var stopwatch = __originalMethod.GetStopwatch();
            stopwatch.Restart();
        }
        public static void Postfix(MethodBase __originalMethod, Def __instance)
        {
            var stopwatch = __originalMethod.GetStopwatch();
            stopwatch.Stop();
            if (stopwatch.SecondsElapsed() >= 0.00001f)
            {
                var mod = __instance.modContentPack;
                if (mod != null)
                {
                    ModImpactData.RegisterImpact(mod, "Defs", "Def initializing (" + __originalMethod.FullMethodName() + ")", stopwatch.SecondsElapsed());
                }
            }
        }
    }
}

