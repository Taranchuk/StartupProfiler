using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(Root), "Update")]
    public static class ModStartupReport
    {
        public static int updateCount;
        internal static ReportSummary Summary { get; private set; } = new ReportSummary();
        public static bool shouldReport;
        public static void Postfix()
        {
            if (shouldReport && LongEventHandler.currentEvent is null && LongEventHandler.eventThread is null)
            {
                EndStartupProfile();
            }
        }


        public static void EndStartupProfile()
        {
            if (StartupImpactProfiling.harmonyPatchStopwatches.Any())
            {
                foreach (MethodInfo registeredMethod in HarmonyPatches_Profile.registeredMethods)
                {
                    StartupProfilerMod.harmony.Unpatch(registeredMethod, StartupImpactProfiling.profilePrefix.method);
                    StartupProfilerMod.harmony.Unpatch(registeredMethod, StartupImpactProfiling.profilePostfix.method);
                    if (StartupImpactProfiling.harmonyPatchStopwatches.Remove(registeredMethod, out StopwatchData stopWatchData))
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
                                ModImpactData.RegisterImpact(mod, "C#", $"Harmony patch impact ({registeredMethod.FullMethodName()})", stopWatchData.totalTimeInSeconds);
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
            Prefs.LogVerbose = StartupProfilerMod.oldVerbose;
        }
    }
}
