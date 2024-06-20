using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;
using System.Threading;

namespace StartupProfiler
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }
    [HotSwappable]
	[StaticConstructorOnStartup]
	public class ReportWindow : Window
	{
		private const float RightWindowRatio = 0.5f;
		private static readonly Color ColorOdd = new Color(0.2f, 0.2f, 0.2f);
		public static Texture2D MenuIcon = ContentFinder<Texture2D>.Get("StartupImpactStats_MenuIcon");
		private static Vector2 modList_ScrollPos;
		private static Vector2 reportList_ScrollPos;
		private List<ModImpactData> cachedModDatas;
		private ModImpactData selectedMod;
		private SortedList<float, (string category, string summary)> categorySummaries = new SortedList<float, (string category, string summary)>(new DecendingComparer<float>());
		private string summaryText;
		public ReportWindow() : base()
		{
			closeOnClickedOutside = true;
			closeOnCancel = true;
			doCloseX = true;
			//Order is non-deterministic. Needs to be reordered based on impact times
			cachedModDatas = ModStartupReport.Summary.AllEntries.Where(modImpactData => !modImpactData.mod.IsOfficialMod && modImpactData.TotalImpactTime >= 0.01f)
																.OrderByDescending(modImpactData => modImpactData.TotalImpactTime).ToList();
			summaryText = GetCachedSummaryText();
			selectedMod = cachedModDatas.First();
            RegenerateReport();
			//foreach (var  mod in cachedModDatas)
			//{
			//	if (mod.TotalImpactTime >= 0.5f)
			//	{
			//		Log.Message("<li>" + mod.mod.PackageId + "</li>");
			//	}
			//}
        }

        public override Vector2 InitialSize => new Vector2(1024, 768);

		private Vector2 topLabelScrollPosition;
		public override void DoWindowContents(Rect inRect)
        {
            var summaryTop = new Rect(inRect.x, inRect.y, inRect.width, 180).ContractedBy(5f);
            Widgets.DrawMenuSection(summaryTop);
            Text.Font = GameFont.Small;
            Widgets.LabelScrollable(summaryTop.ContractedBy(5f), summaryText, ref topLabelScrollPosition);
            float leftWidth = inRect.width * (1 - RightWindowRatio);
            Rect leftRect = new Rect(inRect.x, summaryTop.yMax + 5, leftWidth, inRect.height - 180).ContractedBy(5);
            Rect rightRect = new Rect(leftWidth, summaryTop.yMax + 5, inRect.width * RightWindowRatio, inRect.height - 180).ContractedBy(5);
            DrawReport(rightRect);
            DrawModList(leftRect);
            Text.Font = GameFont.Small;
        }

        private string GetCachedSummaryText()
        {
            Dictionary<string, float> impactByCategories = new Dictionary<string, float>();
            foreach (var mod in cachedModDatas)
            {
                foreach (var category in mod.impactByCategories)
                {
					foreach (var subCategory in category.Value)
					{
						if (subCategory.Key != null && subCategory.Value >= 0.01f)
                        {
                            if (impactByCategories.ContainsKey(subCategory.Key))
                            {
                                impactByCategories[subCategory.Key] += subCategory.Value;
                            }
                            else
                            {
                                impactByCategories[subCategory.Key] = subCategory.Value;
                            }
                        }
                    }
                }
            }

			var totalTimeProfiled = impactByCategories.Sum(x => x.Value);
            var summaryText = "SP.TotalTimeProfiled".Translate(totalTimeProfiled.ToStringDecimalIfSmall() + "s") + "\n";
			summaryText += "SP.TopTimeConsumingMethodsListedBelow".Translate() + "\n";
            foreach (var category in impactByCategories.OrderByDescending(x => x.Value))
            {
                summaryText += " - " + category.Key + " - " + category.Value.ToStringDecimalIfSmall() + "s" + " (" + (category.Value / totalTimeProfiled).ToStringPercent() + ")\n";
            }
            return summaryText;
        }

		static float modListHeight;
        private void DrawModList(Rect rect)
		{
			Rect listerRect = MenuScrollView(rect, modListHeight, ref modList_ScrollPos);
			Vector2 pos = new Vector2(listerRect.x, listerRect.y);
            modListHeight = 0;
			var totalImpact = cachedModDatas.Sum(x => x.TotalImpactTime);
            for (int i = 0; i < cachedModDatas.Count; i++)
            {
                ModImpactData modImpactData = cachedModDatas[i];
                string label = $" {modImpactData.TotalImpactTime.ToStringDecimalIfSmall()}s ({(modImpactData.TotalImpactTime / totalImpact).ToStringPercent()}) - {modImpactData.mod.Name}";
                var height = Text.CalcHeight(label, listerRect.width);
                modListHeight += height;
                Rect entryRect = new Rect(pos.x, pos.y, listerRect.width, height);
                if (i % 2 != 0)
                {
                    Widgets.DrawBoxSolid(entryRect, ColorOdd);
                }
                if (selectedMod == modImpactData)
                {
					Widgets.DrawHighlightSelected(entryRect);
                }
                DrawModName(entryRect, modImpactData, label);
                pos.y += height;
            }
            Widgets.EndScrollView();
		}

		static float reportListHeight;
        private void DrawReport(Rect rect)
		{
			Rect listerRect = MenuScrollView(rect, reportListHeight, ref reportList_ScrollPos);
            reportListHeight = 0;
			Vector2 pos = new Vector2(listerRect.x, listerRect.y);
            foreach ((float totalElapsed, (string category, string summary)) in categorySummaries)
            {
				var categoryElapsed = $"{category} ({totalElapsed:0.##}s)";
				var height = Text.CalcHeight(categoryElapsed, listerRect.width);
                reportListHeight += height;
				Widgets.Label(new Rect(pos.x, pos.y, listerRect.width, height), categoryElapsed);
				pos.y += height;
				height = Text.CalcHeight(summary, listerRect.width);
                reportListHeight += height;
                Widgets.Label(new Rect(pos.x + 15, pos.y, listerRect.width, height), summary);
                pos.y += height;
            }
            Widgets.EndScrollView();
		}
		
		/// <summary>
		/// Small setup class for scroll views in this window.
		/// </summary>
		/// <remarks>Implemented so the scroll rects can all be consistent.</remarks>
		/// <param name="rect">OutRect</param>
		/// <param name="height">ViewRect height</param>
		/// <param name="scrollPos">Scroll Pos of scrollbar handle</param>
		/// <returns><paramref name="rect"/> contracted for padding from menu section background.</returns>
		private Rect MenuScrollView(Rect rect, float height, ref Vector2 scrollPos)
		{
			Widgets.DrawMenuSection(rect);
			rect = rect.ContractedBy(5);
			Rect viewRect = new Rect(rect)
			{
				height = height
			};
			if (viewRect.height > rect.height) 
			{
				viewRect.width = rect.width - 18; //Space for scrollbar
			}
            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);
			return viewRect;
		}

		private void DrawModName(Rect rect, ModImpactData modImpactData, string label)
		{
			TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(rect, label);
            if (Widgets.ButtonInvisible(rect))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                selectedMod = modImpactData;
                RegenerateReport();
            }
            Text.Anchor = anchor;
		}

		private void RegenerateReport()
		{
			categorySummaries.Clear();
			StringBuilder stringBuilder = new StringBuilder();
			foreach ((string category, Dictionary<string, float> dict) in selectedMod.impactByCategories)
			{
				float totalElapsed = 0;
				stringBuilder.Clear();
				List<(string subCategory, float seconds)> innerList = new List<(string subCategory, float seconds)>();
				foreach ((string subCategory, float seconds) in dict)
				{
					if (seconds >= ModImpactData.MinSubCategoryImpactLogging)
					{
                        totalElapsed += seconds;
                        innerList.Add((subCategory, seconds));
                    }
				}

				foreach ((string subCategory, float seconds) in innerList.OrderByDescending(subCategoryResult => subCategoryResult.seconds))
				{
                    stringBuilder.AppendLine($"{seconds.ToStringWithThreeDecimals()}s: {subCategory}");
                }
                if (totalElapsed >= ModImpactData.MinCategoryImpactLogging && stringBuilder.Length > 0)
				{
					categorySummaries.Add(totalElapsed, (category, stringBuilder.ToString()));
				}
			}
		}

		private class DecendingComparer<TKey> : IComparer<float>
		{
			public int Compare(float lhs, float rhs)
			{
				return rhs.CompareTo(lhs);
			}
		}
	}

	[HotSwappable]
	public static class FormatUtils
	{
		public static string ToStringWithThreeDecimals(this float f)
		{
             return String.Format("{0:#,0.000}", f);
        }
	}
}
