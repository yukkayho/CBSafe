using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class TreeMarkerClientInitPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(TreeMarker), LocStrings.Meth_ClientInit);
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix(TreeMarker __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			GameObject gameObject = __instance.gameObject;
			if (!(gameObject == null))
			{
				Scene scene = default(Scene);
				try
				{
					scene = gameObject.scene;
				}
				catch
				{
					return;
				}
				if (scene.IsValid())
				{
					TreeCrosshairColorApplier.OnTreeMarkerClientInit(__instance);
				}
			}
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(28, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TreeMarkerClientInitPatch] ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}
}
