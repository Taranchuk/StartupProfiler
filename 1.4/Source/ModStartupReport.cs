using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;

namespace ModStartupImpactStats
{

    [HarmonyPatch(typeof(EditWindow_Log), MethodType.Constructor)]
    internal static class ModStartupReport
    {
        public static StringBuilder mainMessage = new StringBuilder();
        public static StringBuilder secondaryMessage = new StringBuilder();
        private static void Postfix()
        {
            LogStopwatches();
        }

        private static void LogStopwatches()
        {
            if (StartupImpactProfiling.stopwatches.Any())
            {
                foreach (var registeredMethod in HarmonyPatches_Profile.registeredMethods)
                {
                    if (StartupImpactProfiling.stopwatches.Remove(registeredMethod, out var watch))
                    {
                        var mod = LoadedModManager.RunningMods.FirstOrDefault(x => x.assemblies.loadedAssemblies.Contains(registeredMethod.DeclaringType.Assembly));
                        if (mod != null && !mod.IsOfficialMod)
                        {
                            if (registeredMethod.DeclaringType.Assembly.GetName().Name == "GraphicSetter")
                            {
                                ModImpactData.RegisterImpact(mod.PackageIdPlayerFacing, "Texture2D", "GraphicSetter", watch.totalTimeInSeconds);
                            }
                            else
                            {
                                ModImpactData.RegisterImpact(mod.PackageIdPlayerFacing, "C#", "Harmony patch (" + registeredMethod.DeclaringType.FullName + "." + registeredMethod.Name + ")", watch.totalTimeInSeconds);
                            }
                        }
                    }
                }

                foreach (var stopwatch in StartupImpactProfiling.stopwatches.OrderByDescending(x => x.Value.totalTimeInSeconds))
                {
                    stopwatch.Value.LogTime();
                }
            }

            if (ModStartupImpactStatsMod.stopwatch != null)
            {
                ModStartupImpactStatsMod.stopwatch.Stop();
                mainMessage.AppendLine("Mods: " + ModLister.AllInstalledMods.Where(x => x.Active).Count() + " - STARTUP TIME TOOK " + ModStartupImpactStatsMod.stopwatch.Elapsed.ToString(@"m\:ss") + " - " + DateTime.Now.ToString());
                if (Prefs.LogVerbose)
                {
                    var allCategoryImpacts = new Dictionary<string, float>();
                    foreach (var modImpact in ModImpactData.modsImpact.OrderByDescending(x => x.Value.TotalImpactTime()))
                    {
                        var impactTime = modImpact.Value.TotalImpactTime();
                        if (impactTime > 0.1f)
                        {
                            var packageId = modImpact.Key;
                            var mod = LoadedModManager.RunningMods.FirstOrDefault(x => x.PackageIdPlayerFacing.ToLower() == packageId.ToLower());
                            if (mod != null)
                            {
                                if (!mod.IsOfficialMod)
                                {
                                    secondaryMessage.AppendLine("Mod impact: " + mod.Name + " - " + impactTime.ToStringDecimalIfSmall() + "s, summary:\n" + modImpact.Value.ModSummary());
                                }
                            }
                            else
                            {
                                secondaryMessage.AppendLine("Total impact: - " + packageId + " - " + impactTime.ToStringDecimalIfSmall() + "s, summary:\n" + modImpact.Value.ModSummary());
                            }
                        }

                        foreach (var category in modImpact.Value.impactByCategories)
                        {
                            if (allCategoryImpacts.ContainsKey(category.Key))
                            {
                                allCategoryImpacts[category.Key] += category.Value.Sum(x => x.Value);
                            }
                            else
                            {
                                allCategoryImpacts[category.Key] = category.Value.Sum(x => x.Value);
                            }
                        }
                    }

                    foreach (var category in allCategoryImpacts.OrderByDescending(x => x.Value))
                    {
                        mainMessage.AppendLine("Category " + category.Key + " took " + (category.Value).ToStringDecimalIfSmall() + "s");
                    }
                    mainMessage.AppendLine("Total impact: " + (ModImpactData.modsImpact.Sum(x => x.Value.TotalImpactTime()).ToStringDecimalIfSmall() + "s"));
                    //DisableXMlOnlyMods(mess);
                }

                Log.Warning("Mod info report: \n" + mainMessage.ToString() + "\n" + secondaryMessage.ToString());
            }
        }
    }
}
