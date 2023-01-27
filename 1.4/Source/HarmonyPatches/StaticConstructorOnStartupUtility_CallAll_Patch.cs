using HarmonyLib;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace ModStartupImpactStats
{
    [HarmonyPatch(typeof(StaticConstructorOnStartupUtility), "CallAll")]
    public static class StaticConstructorOnStartupUtility_CallAll_Patch
    {
        public static Stopwatch stopwatch = new Stopwatch();

        [HarmonyPrepare]
        public static bool Prepare()
        {
            return Prefs.LogVerbose;
        }
        public static bool Prefix()
        {
            foreach (Type item in GenTypes.AllTypesWithAttribute<StaticConstructorOnStartup>())
            {
                bool isModded = item.Assembly != typeof(Game).Assembly;
                try
                {
                    stopwatch.Restart();
                    RuntimeHelpers.RunClassConstructor(item.TypeHandle);
                    stopwatch.Stop();
                    if (isModded)
                    {
                        var mod = LoadedModManager.RunningMods.FirstOrDefault(x => x.assemblies.loadedAssemblies.Contains(item.Assembly));
                        if (mod != null && !mod.IsOfficialMod)
                        {
                            ModImpactData.RegisterImpact(mod, "C#", "StaticConstructorOnStartup (" + item.FullName + "." + item.Name + ")", stopwatch.SecondsElapsed());
                        }
                        else
                        {
                            Log.Message("Missing mod for " + item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat("Error in static constructor of ", item, ": ", ex));
                }
            }
            StaticConstructorOnStartupUtility.coreStaticAssetsLoaded = true;
            return false;
        }
    }
}

