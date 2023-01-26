using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ModStartupImpactStats
{
    public class ModImpactData
    {
        public static Dictionary<string, ModImpactData> modsImpact = new();

        public Dictionary<string, Dictionary<string, float>> impactByCategories = new();
        public float TotalImpactTime()
        {
            float time = 0f;
            foreach (var category in impactByCategories)
            {
                foreach (var subCategory in category.Value)
                {
                    time += subCategory.Value;
                }
            }
            return time;
        }

        public const float MinModImpactLogging = 0.1f;
        public const float MinCategoryImpactLogging = 0.01f;
        public const float MinSubCategoryImpactLogging = 0.001f;
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

        public static void RegisterImpact(string modPackageId, string category, string subCategory, float impact)
        {
            if (!modsImpact.TryGetValue(modPackageId, out var modImpact))
            {
                modsImpact[modPackageId] = modImpact = new ModImpactData();
            }
            modImpact.RegisterImpact(category, subCategory, impact);
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
    }

}
