using HarmonyLib;
using System.Diagnostics;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(PatchOperation), "Apply")]
    public static class PatchOperation_Apply_Patch
    {
        public static Stopwatch stopwatch = new Stopwatch();
        public static PatchOperation currentPatchOp;
        public static void Prefix(PatchOperation __instance)
        {
            if (currentPatchOp is null)
            {
                currentPatchOp = __instance;
                if (__instance.sourceFile == null)
                {
                    return;
                }
                stopwatch.Restart();
            }
        }
        public static void Postfix(PatchOperation __instance)
        {
            if (__instance == currentPatchOp)
            {
                currentPatchOp = null;
                if (__instance.sourceFile == null)
                {
                    return;
                }
                stopwatch.Stop();
                if (ModContentPack_LoadPatches_Patch.modsByPatches.TryGetValue(__instance.sourceFile, out var mod))
                {
                    ModImpactData.RegisterImpact(mod, "XML Patches", "XML patch operations: (" + __instance.sourceFile + ")", stopwatch.SecondsElapsed());
                }
            }
        }
    }
}

