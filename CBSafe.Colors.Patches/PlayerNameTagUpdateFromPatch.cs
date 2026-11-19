using System;
using System.Reflection;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class PlayerNameTagUpdateFromPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(PlayerNameTag), LocStrings.Meth_UpdateFrom, new Type[1] { typeof(BasePlayer) });
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix(PlayerNameTag __instance, BasePlayer __0)
	{
		try
		{
			TeamPlayerNameColorApplier.OnPlayerNameTagUpdateFrom(__instance, __0);
		}
		catch
		{
		}
	}
}
