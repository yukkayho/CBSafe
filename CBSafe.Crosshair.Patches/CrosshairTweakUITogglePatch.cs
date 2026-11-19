using System;
using System.Reflection;
using CBSafe.Core;
using CBSafe.Crosshair.UI;
using HarmonyLib;

namespace CBSafe.Crosshair.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class CrosshairTweakUITogglePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(TweakUIToggle), LocStrings.Meth_Init);
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix(TweakUIToggle __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			CrosshairTabManager.TryBuild(__instance);
		}
		catch (Exception ex)
		{
			CBSafeCrosshairPlugin.Instance?.Log?.LogError("[CrosshairTweakUITogglePatch] " + ex);
		}
	}
}
