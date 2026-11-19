using System.Reflection;
using HarmonyLib;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch(typeof(ServerBrowserList), "InitFilters")]
internal static class ServerBrowserList_InitFilters_Patch
{
	[HarmonyPrefix]
	public static void Prefix(ref bool showEmpty)
	{
		if (!showEmpty)
		{
			showEmpty = true;
		}
	}
}
