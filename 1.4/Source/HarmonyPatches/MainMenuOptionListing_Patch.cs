using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace ModStartupImpactStats
{
	[HarmonyPatch(declaringType: typeof(OptionListingUtility), methodName: nameof(OptionListingUtility.DrawOptionListing))]
	public static class MainMenuOptionListing_Patch
	{
		private static void Prefix(List<ListableOption> optList)
		{
			//OptionListing is used twice: Main menu buttons and web links. Only insert on weblinks column
			if (optList.Any(opt => opt is ListableOption_WebLink))
			{
				optList.Add(new ListableOption_WebLink("SI_Label".Translate(), delegate ()
				{
					Find.WindowStack.Add(new ReportWindow());
				}, ReportWindow.MenuIcon));
			}
		}
	}
}
