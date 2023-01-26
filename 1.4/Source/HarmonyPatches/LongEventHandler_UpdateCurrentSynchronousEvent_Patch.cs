﻿using HarmonyLib;
using System.Diagnostics;
using System.Linq;
using Verse;

namespace ModStartupImpactStats
{
    [HarmonyPatch(typeof(LongEventHandler), "UpdateCurrentSynchronousEvent")]
    public static class LongEventHandler_UpdateCurrentSynchronousEvent_Patch
    {
        [HarmonyPrepare]
        public static bool Prepare()
        {
            return Prefs.LogVerbose;
        }

        public static Stopwatch stopwatch = new Stopwatch();
        public static void Prefix(out (string, string)? __state)
        {
            stopwatch.Restart();
            if (!LongEventHandler.currentEvent.ShouldWaitUntilDisplayed && LongEventHandler.currentEvent.eventAction != null)
            {
                var assembly = LongEventHandler.currentEvent.eventAction.Method.DeclaringType.Assembly;
                var mod = LoadedModManager.RunningMods.FirstOrDefault(x => x.assemblies.loadedAssemblies.Contains(assembly));
                if (mod != null && !mod.IsOfficialMod)
                {
                    __state = (mod.PackageIdPlayerFacing, LongEventHandler.currentEvent.eventAction.Method.FullDescription());
                }
                else
                {
                    __state = null;
                }
            }
            else
            {
                __state = null;
            }
        }

        public static void Postfix((string, string)? __state)
        {
            stopwatch.Stop();
            if (__state != null)
            {
                ModImpactData.RegisterImpact(__state.Value.Item1, "C#", "LongEventHandler (" + __state.Value.Item2 + ")", stopwatch.SecondsElapsed());
            }
        }
    }
}
