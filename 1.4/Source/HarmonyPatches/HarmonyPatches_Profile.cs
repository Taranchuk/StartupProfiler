using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace ModStartupImpactStats
{
    [HarmonyPatch]
    public static class HarmonyPatches_Profile
    {
        public static HashSet<MethodBase> registeredMethods = new HashSet<MethodBase>();

        public static bool preventRecursion;

        [HarmonyPrepare]
        public static bool Prepare()
        {
            return Prefs.LogVerbose;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(PatchInfo), nameof(PatchInfo.AddPrefixes));
            yield return AccessTools.Method(typeof(PatchInfo), nameof(PatchInfo.AddPostfixes));
        }

        public static void Postfix(string owner, params HarmonyMethod[] methods)
        {
            if (preventRecursion || methods is null)
            {
                return;
            }
            foreach (MethodInfo methodInfo in methods.Where(x => x != null).Select(x => x.method))
            {
                if (methodInfo != null && !registeredMethods.Contains(methodInfo) && methodInfo.DeclaringType.Assembly != typeof(HarmonyPatches_Profile).Assembly)
                {
                    preventRecursion = true;
                    registeredMethods.Add(methodInfo);
                    StartupImpactProfiling.TryProfileMethod(methodInfo);
                    preventRecursion = false;
                }
            }
        }
    }
}

