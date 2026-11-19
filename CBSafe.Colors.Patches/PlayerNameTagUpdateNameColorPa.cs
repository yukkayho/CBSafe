using System;
using System.Reflection;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class PlayerNameTagUpdateNameColorPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(PlayerNameTag), LocStrings.Meth_UpdateNameColor, Type.EmptyTypes);
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix(PlayerNameTag __instance)
	{
		try
		{
			TeamPlayerNameColorApplier.OnPlayerNameTagUpdateColor(__instance);
		}
		catch
		{
		}
	}
}
