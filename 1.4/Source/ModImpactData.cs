using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace StartupProfiler
{
    public class ModImpactData
    {
        public const float MinModImpactLogging = 0.1f;
        public const float MinCategoryImpactLogging = 0.01f;
        public const float MinSubCategoryImpactLogging = 0.001f;

        //public static Dictionary<ModContentPack, ModImpactData> modsImpact = new();


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

        public string ModSummary()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var category in impactByCategories.OrderByDescending(x => x.Value.Sum(y => y.Value)))
            {
                var categoryImpact = category.Value.Sum(x => x.Value);
                if (categoryImpact >= MinCategoryImpactLogging)
                {
                    sb.AppendLine("\t" + category.Key + ": " + categoryImpact.ToString("0.###") + "s (total)");
                    foreach (var subCategory in category.Value.OrderByDescending(x => x.Value))
                    {
                        if (subCategory.Value >= MinSubCategoryImpactLogging)
                        {
                            sb.AppendLine("\t\t" + subCategory.Key + " " + subCategory.Value.ToString("0.###") + "s");
                        }
                    }
                }

            }
            return sb.ToString();
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
            if (!ModStartupReport.Summary.TryGetValue(mod, out ModImpactData modImpact))
            {
                ModStartupReport.Summary[mod] = modImpact = new ModImpactData(mod);
            }
            modImpact.RegisterImpact(category, subCategory, impact);
        }
    }
}
