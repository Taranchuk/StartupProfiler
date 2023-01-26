using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace ModStartupImpactStats
{
    public class ModStartupImpactStatsMod : Mod
    {
        public static Stopwatch stopwatch;
        public static Harmony harmony;
        public static ModStartupImpactStatsMod Instance;
        public ModStartupImpactStatsMod(ModContentPack pack) : base(pack)
        {
            Prefs.LogVerbose = true;
            Instance = this;
            stopwatch = new Stopwatch();
            stopwatch.Start();
            harmony = new Harmony("ModStartupImpactStats.MyPatches");
            harmony.PatchAll();
            Log.TryOpenLogWindow();
        }
    }
}
