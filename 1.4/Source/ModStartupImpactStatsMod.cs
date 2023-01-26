﻿using HarmonyLib;
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

        public static bool oldVerbose;
        public ModStartupImpactStatsMod(ModContentPack pack) : base(pack)
        {
            oldVerbose = Prefs.LogVerbose;
            Prefs.LogVerbose = true;
            if (!oldVerbose)
            {
                DeepProfiler.Start("InitializeMods()");
                DeepProfiler.Start(string.Concat("Loading ", typeof(ModStartupImpactStatsMod), " mod class"));
            }
            Instance = this;
            harmony = new Harmony("ModStartupImpactStats.MyPatches");
            harmony.PatchAll();
            stopwatch = new Stopwatch();
            stopwatch.Start();
            Log.TryOpenLogWindow();
        }
    }
}
