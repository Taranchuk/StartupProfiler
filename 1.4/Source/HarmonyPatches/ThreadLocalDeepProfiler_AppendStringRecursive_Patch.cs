using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Verse;
using static Verse.ThreadLocalDeepProfiler;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(ThreadLocalDeepProfiler), "AppendStringRecursive")]
    public static class ThreadLocalDeepProfiler_AppendStringRecursive_Patch
    {
        static Regex modNameRegex = new Regex(@" for mod (.*)");
        static Regex assetRegex = new Regex(@" assets of type ([^ ]+)");
        static Regex impactTimeRegex = new Regex(@"self: ([^ ]+)");
        static Regex modClassRegex = new Regex(@"([^ ]+) mod class");
        static Regex modClassRegex2 = new Regex(@"([^ ]+) -> [a-z]", RegexOptions.IgnoreCase);

        public static HashSet<string> processedLines = new HashSet<string>();

        [HarmonyPrepare]
        public static bool Prepare()
        {
            return Prefs.LogVerbose;
        }

        public static void Postfix(ref StringBuilder sb, string label, List<Watcher> children)
        {
            string[] delim = { Environment.NewLine, "\n" };
            string[] lines = sb.ToString().Split(delim, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (!processedLines.Contains(line))
                {
                    processedLines.Add(line);
                    if (line.Contains(" for mod "))
                    {
                        var packageId = modNameRegex.Match(line).Groups[1].Value;
                        var assetType = assetRegex.Match(line).Groups[1].Value;
                        assetType = assetType.Replace("UnityEngine.", "").Replace("System.", "");
                        var impact = float.Parse(impactTimeRegex.Match(line).Groups[1].Value);
                        var mod = LoadedModManager.RunningModsListForReading.FirstOrDefault(mod => mod.PackageIdPlayerFacing == packageId);
                        ModImpactData.RegisterImpact(mod, assetType, "Asset loading", impact / 1000f);
                    }
                    else if (modClassRegex2.IsMatch(line))
                    {
                        var typeName = modClassRegex2.Match(line).Groups[1].Value;
                        var type = AccessTools.TypeByName(typeName);
                        if (type != null && !typeof(Mod).IsAssignableFrom(type))
                        {
                            var mod = LoadedModManager.RunningMods.FirstOrDefault(x => x.assemblies.loadedAssemblies.Contains(type.Assembly));
                            if (mod != null && !mod.IsOfficialMod)
                            {
                                var impact = float.Parse(impactTimeRegex.Match(line).Groups[1].Value);
                                ModImpactData.RegisterImpact(mod, "C#", "LongEventHandler (" + typeName + ")", impact / 1000f);
                            }
                        }
                    }
                }
            }

            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child != null)
                    {
                        if (!processedLines.Contains(child.label))
                        {
                            processedLines.Add(child.label);
                            if (child.label.Contains(" mod class"))
                            {
                                var typeName = modClassRegex.Match(child.label).Groups[1].Value;
                                var type = AccessTools.TypeByName(typeName);
                                if (LoadedModManager.runningModClasses.TryGetValue(type, out var modClass))
                                {
                                    var mod = modClass.Content;
                                    if (!mod.IsOfficialMod)
                                    {
                                        ModImpactData.RegisterImpact(mod, "C#", "Mod constructor (" + typeName + ")", (float)child.ElapsedMilliseconds / 1000f);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

