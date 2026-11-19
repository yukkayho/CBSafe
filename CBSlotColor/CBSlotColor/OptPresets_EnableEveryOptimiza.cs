using System;
using System.Reflection;
using HarmonyLib;

namespace CBSlotColor;

[HarmonyPatch]
internal static class OptPresets_EnableEveryOptimization_Patch
{
	public static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("CBSafe.Fps.OptPresets");
		if (!(type == null))
		{
			return AccessTools.Method(type, "EnableEveryOptimization");
		}
		return null;
	}

	public static void Postfix()
	{
		FullBoosterTune.Apply("full-preset");
	}
}
