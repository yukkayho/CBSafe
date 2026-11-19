using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Core;
using Facepunch.UI;
using HarmonyLib;
using UnityEngine;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class EspPlayerInfoSetColorPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(ESPPlayerInfo), LocStrings.Meth_SetColor, new Type[1] { typeof(Color) });
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Prefix(ESPPlayerInfo __instance, ref Color __0)
	{
		try
		{
			TeamPlayerNameColorApplier.OnEspSetColor(__instance, ref __0);
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(29, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[EspPlayerInfoSetColorPatch] ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}
}
