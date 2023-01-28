using HarmonyLib;
using RimWorld;
using RimWorld.IO;
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
    [HarmonyPatch(typeof(AbstractFilesystem), "ClearAllCache")]
    internal static class ModStartupReport
    {
        public static bool initialized = false;
        internal static ReportSummary Summary { get; private set; } = new ReportSummary();

        private static void Postfix()
        {
            if (!initialized)
            {
                initialized = true;
                EndStartupProfile();
                Prefs.LogVerbose = StartupProfilerMod.oldVerbose;
            }
        }

        private static void EndStartupProfile()
        {
            if (StartupImpactProfiling.stopwatches.Any())
            {
                foreach (MethodInfo registeredMethod in HarmonyPatches_Profile.registeredMethods)
                {
                    StartupProfilerMod.harmony.Unpatch(registeredMethod, StartupImpactProfiling.profilePrefix.method);
                    StartupProfilerMod.harmony.Unpatch(registeredMethod, StartupImpactProfiling.profilePostfix.method);
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
            }
            
            if (StartupProfilerMod.stopwatch != null)
            {
                StartupProfilerMod.stopwatch.Stop();
                Summary.TotalElapsed = StartupProfilerMod.stopwatch.Elapsed;
            }

            StartupProfilerMod.harmony.UnpatchAll(StartupProfilerMod.harmony.Id);
            var listingMethod = AccessTools.Method(typeof(OptionListingUtility), nameof(OptionListingUtility.DrawOptionListing));
            StartupProfilerMod.harmony.Patch(listingMethod, prefix: new HarmonyMethod(AccessTools.Method(typeof(MainMenuOptionListing_Patch),
                nameof(MainMenuOptionListing_Patch.Prefix))));
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
