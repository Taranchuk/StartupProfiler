﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using System.Threading;

namespace StartupProfiler
{
    public class ModImpactData
    {
        public const float MinCategoryImpactLogging = 0.01f;
        public const float MinSubCategoryImpactLogging = 0.001f;
        private float cachedImpactTime = -1;
        public ModContentPack mod;
        public string summary;

        /// <summary>
        /// (category, (subCategory, float))
        /// </summary>
        public Dictionary<string, Dictionary<string, float>> impactByCategories = new();

        public ModImpactData(ModContentPack mod)
		{
            this.mod = mod;
		}

        public float TotalImpactTime
		{
			get
			{
                if ( cachedImpactTime < 0)
				{
                    cachedImpactTime = impactByCategories.Values.Sum(dict => dict.Values.Sum());
                }
                return cachedImpactTime;
			}
		}

        public void RegisterImpact(string category, string subCategory, float impact)
        {
            if (impactByCategories.TryGetValue(category, out var data))
            {
                if (data.ContainsKey(subCategory))
                {
                    data[subCategory] += impact;
                }
                else
                {
                    data[subCategory] = impact;
                }
            }
            else
            {
                impactByCategories[category] = new Dictionary<string, float>
                {
                    {subCategory, impact},
                };
            }
        }

        public static void RegisterImpact(ModContentPack mod, string category, string subCategory, float impact)
        {
            if (mod.IsOfficialMod || mod == StartupProfilerMod.Instance.Content)
            {
                return;
            }
            if (StartupProfilerMod.thread == Thread.CurrentThread || UnityData.IsInMainThread)
            {
                RegisterImpactThreadSafe(mod, category, subCategory, impact);
            }
        }

        private static void RegisterImpactThreadSafe(ModContentPack mod, string category, string subCategory, float impact)
        {
            if (!ModStartupReport.Summary.TryGetValue(mod, out ModImpactData modImpact))
            {
                ModStartupReport.Summary[mod] = modImpact = new ModImpactData(mod);
            }
            modImpact.RegisterImpact(category, subCategory, impact);
        }
    }
}
