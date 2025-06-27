using HarmonyLib;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(LongEventHandler), "RunEventFromAnotherThread")]
    public static class LongEventHandler_RunEventFromAnotherThread_Patch
    {
        public static MethodInfo HugslibController = AccessTools.Method("HugsLib.HugsLibController:LoadReloadInitialize");
        public static void Prefix(Action action, out (ModContentPack, string)? __state)
        {
            var stopwatch = action.Method.GetStopwatch();
            stopwatch.Restart();
            if (HugslibController == action.Method)
            {
                __state = null;
            }
            else
            {
                var assembly = action.Method.DeclaringType.Assembly;
                var mod = LoadedModManager.RunningMods.FirstOrDefault(x => x.assemblies.loadedAssemblies.Contains(assembly));
                if (mod != null && !mod.IsOfficialMod)
                {
                    __state = (mod, action.Method.FullDescription());
                }
                else
                {
                    __state = null;
                }
            }

        }

        public static void Postfix(Action action, (ModContentPack, string)? __state)
        {
            var stopwatch = action.Method.GetStopwatch();
            stopwatch.Stop();
            if (__state != null)
            {
                ModImpactData.RegisterImpact(__state.Value.Item1, "C#", "LongEventHandler (" + __state.Value.Item2 + ")", stopwatch.SecondsElapsed());
            }
        }
    }
}

