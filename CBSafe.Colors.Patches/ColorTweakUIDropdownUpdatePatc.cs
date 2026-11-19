using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.UI;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class ColorTweakUIDropdownUpdatePatch
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
		catch (Exception t)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance != null)
			{
				ManualLogSource log = instance.Log;
				bool isEnabled = false;
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(34, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[ColorTweakUIDropdownUpdatePatch] ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				log.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
		}
	}
}
