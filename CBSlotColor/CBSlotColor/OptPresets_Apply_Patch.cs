using System;
using System.Reflection;
using HarmonyLib;

namespace CBSlotColor;

[HarmonyPatch]
internal static class OptPresets_Apply_Patch
{
	public static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("CBSafe.Fps.OptPresets");
		if (!(type == null))
		{
			return AccessTools.Method(type, "Apply");
		}
		return null;
	}

	public static void Postfix(object __0)
	{
		if (__0 != null)
		{
			string text = __0.ToString();
			switch (text)
			{
			case "SuperPotato":
			case "PotatoPc":
			case "Minecraft":
				FullBoosterTune.Apply(text);
				break;
			}
		}
	}
}
