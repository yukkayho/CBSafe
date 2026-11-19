using System.Reflection;
using HarmonyLib;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
[HarmonyPatch(typeof(ServerBrowser), "OnEnable")]
public class ServerBrowser_OnEnable_Patch
{
	public static ServerBrowser serverBrowser;

	private static void Postfix(ServerBrowser __instance)
	{
		try
		{
			serverBrowser = __instance;
			if (__instance == null)
			{
				return;
			}
			ServerBrowserColorPatch.ApplyServerBrowserColors(__instance);
			ServerBrowserCategoryFilter.Apply(__instance);
			try
			{
				__instance.showEmpty = true;
				__instance.SetShowEmpty(showFull: true);
			}
			catch
			{
			}
		}
		catch
		{
		}
	}
}
