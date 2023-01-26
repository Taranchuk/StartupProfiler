using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace ModStartupImpactStats
{
    public static class StartupImpactProfiling
    {
        public static bool CanBePatched(this MethodBase mi)
        {
            if (mi.HasMethodBody() && mi.DeclaringType.IsConstructedGenericType is false &&
                mi.IsGenericMethod is false && mi.ContainsGenericParameters is false && mi.IsGenericMethodDefinition is false)
            {
                var desc = mi.FullDescription();
                if (desc.Contains("LoadoutGenericDef"))
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public static float SecondsElapsed(this Stopwatch stopwatch)
        {
            return (float)stopwatch.ElapsedTicks / Stopwatch.Frequency;
        }
        public static void TryProfileMethod(MethodBase mi)
        {
            if (mi.HasMethodBody() && mi.DeclaringType.IsConstructedGenericType is false &&
                    mi.IsGenericMethod is false && mi.ContainsGenericParameters is false && mi.IsGenericMethodDefinition is false)
            {
                var desc = mi.FullDescription();
                ProfileMethod(mi);
            }
            else
            {
                Log.Message("Cannot profile " + mi.FullDescription());
            }
        }


        public static ConcurrentDictionary<MethodBase, StopwatchData> stopwatches = new ConcurrentDictionary<MethodBase, StopwatchData>();
        private static HarmonyMethod profilePrefix = new HarmonyMethod(AccessTools.Method(typeof(StartupImpactProfiling), nameof(ProfileMethodPrefix)));
        private static HarmonyMethod profilePostfix = new HarmonyMethod(AccessTools.Method(typeof(StartupImpactProfiling), nameof(ProfileMethodPostfix)));
        private static void ProfileMethod(MethodBase methodInfo)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                try
                {
                    if (methodInfo.IsStatic && methodInfo.Name.Contains("cctor"))
                    {
                        ProfileMethodPrefix(methodInfo, out var state);
                        RuntimeHelpers.RunClassConstructor(methodInfo.DeclaringType.TypeHandle);
                        ProfileMethodPostfix(state);
                    }
                    else
                    {
                        ModStartupImpactStatsMod.harmony.Patch(methodInfo, prefix: profilePrefix, postfix: profilePostfix);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });
        }
        public static void ProfileMethodPrefix(MethodBase __originalMethod, out StopwatchData __state)
        {
            if (stopwatches.TryGetValue(__originalMethod, out __state) is false)
            {
                stopwatches[__originalMethod] = __state = new StopwatchData(__originalMethod);
            }
            __state.Start();
        }
        public static void ProfileMethodPostfix(StopwatchData __state)
        {
            __state.Stop();
        }
    }
}

