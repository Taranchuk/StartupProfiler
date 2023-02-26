using HarmonyLib;
using System.Diagnostics;
using Verse;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(DirectXmlLoader), "DefFromNode")]
    public static class DirectXmlLoader_DefFromNode_Patch
    {
        public static Stopwatch stopwatch = new Stopwatch();
        static void Prefix()
        {
            stopwatch.Restart();
        }

        static void Postfix(LoadableXmlAsset loadingAsset)
        {
            stopwatch.Stop();
            if (loadingAsset?.mod != null)
            {
                ModImpactData.RegisterImpact(loadingAsset.mod, "Defs", "DefFromNode", stopwatch.SecondsElapsed());
            }
        }
    }
}

