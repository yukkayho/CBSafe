using System.Reflection;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Patches;

[HydraNoObfuscate]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class CBSafeSettingsUiLoadPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(LoadingScreen), LocStrings.Meth_UpdateFromServer);
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	[HarmonyPostfix]
	private static void Postfix()
	{
		try
		{
			CBSafeSettingsUiLoadBootstrap.TickDuringLoadScreen();
		}
		catch
		{
		}
	}
}
