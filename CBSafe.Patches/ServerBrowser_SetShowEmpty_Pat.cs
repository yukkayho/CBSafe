using System.Reflection;
using HarmonyLib;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch(typeof(ServerBrowser), "SetShowEmpty")]
internal static class ServerBrowser_SetShowEmpty_Patch
{
	[HarmonyPrefix]
	public static bool Prefix(ServerBrowser __instance, bool showFull)
	{
		try
		{
			__instance.showEmpty = true;
			__instance.RefreshAll();
		}
		catch
		{
		}
		return false;
	}
}
