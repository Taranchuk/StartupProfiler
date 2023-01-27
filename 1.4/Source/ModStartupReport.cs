using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(EditWindow_Log), MethodType.Constructor)]
    internal static class ModStartupReport
    {
        public static bool initialized = false;
        public static StringBuilder mainMessage = new StringBuilder();
        public static StringBuilder secondaryMessage = new StringBuilder();

        internal static ReportSummary Summary { get; private set; } = new ReportSummary();

        private static void Postfix()
        {
            if (!initialized)
            {
                initialized = true;
                LogStartupImpact();
                Prefs.LogVerbose = StartupProfilerMod.oldVerbose;
            }
        }

        private static void LogStartupImpact()
        {
            if (StartupImpactProfiling.stopwatches.Any())
            {
                foreach (MethodInfo registeredMethod in HarmonyPatches_Profile.registeredMethods)
                {
                    if (StartupImpactProfiling.stopwatches.Remove(registeredMethod, out StopwatchData stopWatchData))
                    {
                        ModContentPack mod = LoadedModManager.RunningMods.FirstOrDefault(x => x.assemblies.loadedAssemblies.Contains(registeredMethod.DeclaringType.Assembly));
                        if (mod != null && !mod.IsOfficialMod)
                        {
                            if (registeredMethod.DeclaringType.Assembly.GetName().Name == "GraphicSetter")
                            {
                                ModImpactData.RegisterImpact(mod, "Texture2D", "GraphicSetter", stopWatchData.totalTimeInSeconds);
                            }
                            else
                            {
                                ModImpactData.RegisterImpact(mod, "C#", $"HarmonyPatch ({registeredMethod.FullMethodName()})", stopWatchData.totalTimeInSeconds);
                            }
                        }
                    }
                }
                HarmonyPatches_Profile.registeredMethods.Clear();
                foreach ((_, StopwatchData stopwatchData) in StartupImpactProfiling.stopwatches.OrderByDescending(x => x.Value.totalTimeInSeconds))
                {
                    stopwatchData.LogTime();
                }
            }
            
            if (StartupProfilerMod.stopwatch != null)
            {
                StartupProfilerMod.stopwatch.Stop();
                Summary.TotalElapsed = StartupProfilerMod.stopwatch.Elapsed;
            }
        }

        /// <summary>
        /// Dictionary wrapper class for storing additional summary data
        /// </summary>
        public class ReportSummary : Dictionary<ModContentPack, ModImpactData>
        {
            public ValueCollection AllEntries => Values;

            public TimeSpan TotalElapsed { get; set; }
		}
    }
}
