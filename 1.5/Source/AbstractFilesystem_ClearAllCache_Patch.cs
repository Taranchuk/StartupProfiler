using HarmonyLib;
using RimWorld;
using RimWorld.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;

namespace StartupProfiler
{
    [HarmonyPatch(typeof(AbstractFilesystem), "ClearAllCache")]
    public static class AbstractFilesystem_ClearAllCache_Patch
    {
        private static void Postfix()
        {
            ModStartupReport.shouldReport = true;
        }
    }
}
