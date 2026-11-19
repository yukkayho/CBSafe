using System.Reflection;
using HarmonyLib;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
[HarmonyPatch(typeof(ServerBrowser), "RefreshAll")]
internal static class ServerBrowser_RefreshAll_Patch
{
	[HarmonyPostfix]
	private static void Postfix(ServerBrowser __instance)
	{
		try
		{
			ServerBrowserColorPatch.ApplyServerBrowserColors(__instance);
			ServerBrowserCategoryFilter.Apply(__instance);
		}
		catch
		{
		}
	}
}
