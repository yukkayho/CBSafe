using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
[HydraNoObfuscate]
internal static class HolosightReticleUpdatePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(HolosightReticlePositioning), LocStrings.Meth_Update);
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix(HolosightReticlePositioning __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			HoloReticleColorApplier.OnReticleUpdate(__instance);
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(30, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HolosightReticleUpdatePatch] ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}
}
