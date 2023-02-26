using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace StartupProfiler
{
	[StaticConstructorOnStartup]
	public class ReportWindow : Window
	{
		private const float RightWindowRatio = 0.65f;
		private const float ModEntryHeight = 25;

		private static readonly Color ColorOdd = new Color(0.2f, 0.2f, 0.2f);
		public static Texture2D MenuIcon = ContentFinder<Texture2D>.Get("StartupImpactStats_MenuIcon");

		private static readonly Listing_Standard modLister = new Listing_Standard(GameFont.Small);
		private static readonly Listing_Standard reportLister = new Listing_Standard(GameFont.Small);

		private static Vector2 modList_ScrollPos;
		private static Vector2 reportList_ScrollPos;

		private List<ModImpactData> cachedModDatas;
		private ModImpactData selectedMod;
		private SortedList<float, (string category, string summary)> categorySummaries = new SortedList<float, (string category, string summary)>(new DecendingComparer<float>());

		public ReportWindow() : base()
		{
			closeOnClickedOutside = true;
			closeOnCancel = true;
			doCloseX = true;
			//Order is non-deterministic. Needs to be reordered based on impact times
			cachedModDatas = ModStartupReport.Summary.AllEntries.Where(modImpactData => !modImpactData.mod.IsOfficialMod)
																.OrderByDescending(modImpactData => modImpactData.TotalImpactTime).ToList();
			RecacheModListHeight();
		}

		public override Vector2 InitialSize => new Vector2(1024, 768);

		private float ModListHeight { get; set; }

		private float ReportListHeight { get; set; }

		public override void DoWindowContents(Rect inRect)
		{
			float leftWidth = inRect.width * (1 - RightWindowRatio);
			Rect leftRect = new Rect(inRect)
			{
				width = leftWidth
			}.ContractedBy(2);
			Rect rightRect = new Rect(inRect)
			{
				x = leftWidth,
				width = inRect.width * RightWindowRatio
			}.ContractedBy(2);

			float reportWidth = DrawReport(rightRect);
			DrawModList(leftRect, reportWidth);
		}

		private void DrawModList(Rect rect, float width)
		{
			Rect listerRect = MenuScrollView(rect, ModListHeight, ref modList_ScrollPos);
			{
				for (int i = 0; i < cachedModDatas.Count; i++)
				{
					ModImpactData modImpactData = cachedModDatas[i];

					Rect entryRect = new Rect(listerRect.x, listerRect.y + i * ModEntryHeight, listerRect.width, ModEntryHeight);
					if (i % 2 != 0)
					{
						//Draw colored background every other entry
						Widgets.DrawBoxSolid(entryRect, ColorOdd);
					}
					Draw(entryRect, modImpactData, width);
				}
				modLister.Begin(listerRect);
				{
					
				}
				modLister.End();
			}
			Widgets.EndScrollView();
		}

		private float DrawReport(Rect rect)
		{
			Rect listerRect = MenuScrollView(rect, ReportListHeight, ref reportList_ScrollPos);
			{
				modLister.Begin(listerRect);
				{
					foreach ((float totalElapsed, (string category, string summary)) in categorySummaries)
					{
						modLister.Label($"{category} ({totalElapsed:0.##}s)");
						modLister.Label(summary);
						modLister.Gap();
					}
				}
				modLister.End();
			}
			Widgets.EndScrollView();

			return rect.width;
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
				width = rect.width - 16, //Space for scrollbar
				height = height
			};
			Widgets.BeginScrollView(rect, ref scrollPos, viewRect);

			return rect;
		}

		private void Draw(Rect rect, ModImpactData modImpactData, float width)
		{
			TextAnchor anchor = Text.Anchor;
			{
				Text.Anchor = TextAnchor.MiddleLeft;
				Rect buttonRect = rect.ContractedBy(2);
				string label = $"({modImpactData.TotalImpactTime:F1}s) {modImpactData.mod.Name}";
				Widgets.Label(buttonRect, label);
				if (Widgets.ButtonInvisible(buttonRect))
				{
					SoundDefOf.Click.PlayOneShotOnCamera();
					selectedMod = modImpactData;
					RegenerateReport(width);
				}
			}
			Text.Anchor = anchor;
		}

		private void RegenerateReport(float width)
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
					totalElapsed += seconds;
					innerList.Add((subCategory, seconds));
				}

				foreach ((string subCategory, float seconds) in innerList.OrderByDescending(subCategoryResult => subCategoryResult.seconds))
				{
					if (seconds >= ModImpactData.MinSubCategoryImpactLogging)
					{
						stringBuilder.AppendLine($"({seconds.ToStringDecimalIfSmall():0.###}s): {subCategory}");
					}
				}
				if (totalElapsed >= ModImpactData.MinCategoryImpactLogging && stringBuilder.Length > 0)
				{
					categorySummaries.Add(totalElapsed, (category, stringBuilder.ToString()));
				}
			}
			RecacheReportHeight(width);
		}

		private void RecacheModListHeight()
		{
			ModListHeight = cachedModDatas.Count * ModEntryHeight;
		}

		private void RecacheReportHeight(float width)
		{
			ReportListHeight = 0;
			foreach ((_, (string category, string summary)) in categorySummaries)
			{
				ReportListHeight += Text.CalcHeight(category, width);
				ReportListHeight += Text.CalcHeight(summary, width);
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
}
