using System;
using System.Reflection;
using CBSafe.Core;
using CBSafe.Crosshair.UI;
using HarmonyLib;

namespace CBSafe.Crosshair.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class CrosshairTweakUIDropdownUpdatePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(TweakUIDropdown), LocStrings.Meth_UpdateDropdownOptions);
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix(TweakUIDropdown __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			SystemDropdownFactory.OnDropdownOptionsRebuilt(__instance);
		}
		catch (Exception ex)
		{
			CBSafeCrosshairPlugin.Instance?.Log?.LogError("[CrosshairTweakUIDropdownUpdatePatch] " + ex);
		}
	}
}
