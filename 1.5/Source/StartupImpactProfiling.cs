using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace StartupProfiler
{
    public static class StartupImpactProfiling
    {
        public static string FullMethodName(this MethodBase methodInfo)
        {
            return methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
        }
        public static bool CanBePatched(this MethodBase mi)
        {
            if (mi.HasMethodBody() && mi.DeclaringType.IsConstructedGenericType is false &&
                mi.IsGenericMethod is false && mi.ContainsGenericParameters is false && mi.IsGenericMethodDefinition is false)
            {
                try
                {
                    var desc = mi.FullDescription();
                    if (desc.Contains("LoadoutGenericDef"))
                    {
                        return false;
                    }
                    return true;
                }
                catch { };
            }
            return false;
        }

        public static ConcurrentDictionary<MethodBase, Stopwatch> stopwatches = new();
        public static Stopwatch GetStopwatch(this MethodBase method)
        {
            if (!stopwatches.TryGetValue(method, out var stopwatch))
            {
                stopwatches[method] = stopwatch = new Stopwatch();
            }
            return stopwatch;
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


        public static ConcurrentDictionary<MethodBase, StopwatchData> harmonyPatchStopwatches = new ConcurrentDictionary<MethodBase, StopwatchData>();
        public static HarmonyMethod profilePrefix = new HarmonyMethod(AccessTools.Method(typeof(StartupImpactProfiling), nameof(ProfileMethodPrefix)));
        public static HarmonyMethod profilePostfix = new HarmonyMethod(AccessTools.Method(typeof(StartupImpactProfiling), nameof(ProfileMethodPostfix)));
        private static void ProfileMethod(MethodBase methodInfo)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                ProfileMethodThreadSafe(methodInfo);
            });
        }

        private static void ProfileMethodThreadSafe(MethodBase methodInfo)
        {
            try
            {
                if (methodInfo.IsStatic && methodInfo.Name.Contains("cctor"))
                {
                    ProfileMethodPrefix(methodInfo, out var state);
                    try
                    {
                        RuntimeHelpers.RunClassConstructor(methodInfo.DeclaringType.TypeHandle);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception in Startup Profiler: " + ex.ToString());
                    }
                    ProfileMethodPostfix(state);
                }
                else
                {
                    try
                    {
                        StartupProfilerMod.harmony.Patch(methodInfo, prefix: profilePrefix, postfix: profilePostfix);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception in Startup Profiler: " + ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception in Startup Profiler: " + ex.ToString());
            }
        }

        public static void ProfileMethodPrefix(MethodBase __originalMethod, out StopwatchData __state)
        {
            if (harmonyPatchStopwatches.TryGetValue(__originalMethod, out __state) is false)
            {
                harmonyPatchStopwatches[__originalMethod] = __state = new StopwatchData(__originalMethod);
            }
            __state.Start();
        }
        public static void ProfileMethodPostfix(StopwatchData __state)
        {
            __state.Stop();
        }
    }
}

