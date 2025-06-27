using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch]
    public static class ProfileHugslibMods
    {
        public static bool Prepare() => LongEventHandler_RunEventFromAnotherThread_Patch.HugslibController != null;

        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> GetMethods()
        {
            var modBaseType = AccessTools.TypeByName("HugsLib.ModBase");
            foreach (var method in AccessTools.GetDeclaredMethods(modBaseType))
            {
                if (method.CanBePatched())
                {
                    yield return method;
                }
            }
            foreach (var type in modBaseType.AllSubclasses())
            {
                foreach (var method in AccessTools.GetDeclaredMethods(type))
                {
                    if (method.CanBePatched() && IsOverride(method))
                    {
                        yield return method;
                    }
                }
            }
        }

        public static bool IsOverride(MethodInfo m)
        {
            return m.GetBaseDefinition().DeclaringType != m.DeclaringType;
        }
        public static void Prefix(MethodBase __originalMethod, object __instance, out Stopwatch __state)
        {
            __state = new Stopwatch();
            __state.Restart();
        }
        public static void Postfix(MethodBase __originalMethod, object __instance, Stopwatch __state)
        {
            __state.Stop();
            if (__instance != null)
            {
                var mod = Traverse.Create(__instance).Field("modContentPackInt").GetValue() as ModContentPack;
                if (mod != null)
                {
                    ModImpactData.RegisterImpact(mod, "C#", "Hugslib controller (" + __originalMethod.FullMethodName() + ")", __state.SecondsElapsed());
                }
            }
            else
            {
                var mod = LoadedModManager.RunningMods.FirstOrDefault(x => x.assemblies.loadedAssemblies.Contains(__originalMethod.DeclaringType.Assembly));
                if (mod != null)
                {
                    ModImpactData.RegisterImpact(mod, "C#", "Hugslib controller (" + __originalMethod.FullMethodName() + ")", __state.SecondsElapsed());
                }
            }
        }
    }
}

