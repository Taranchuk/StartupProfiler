using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace StartupProfiler
{
    public class StartupProfilerMod : Mod
    {
        public static Stopwatch stopwatch;
        public static Harmony harmony;
        public static StartupProfilerMod Instance;

        public static bool oldVerbose;
        public StartupProfilerMod(ModContentPack pack) : base(pack)
        {
            oldVerbose = Prefs.LogVerbose;
            if (!oldVerbose)
            {
                Prefs.LogVerbose = true;
                DeepProfiler.Start("InitializeMods()");
                DeepProfiler.Start(string.Concat("Loading ", typeof(StartupProfilerMod), " mod class"));
            }
            Instance = this;
            harmony = new Harmony("StartupProfiler.MyPatches");
            harmony.PatchAll();
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }
    }
}
