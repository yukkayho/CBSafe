using System;
using System.Reflection;
using HarmonyLib;

namespace CBSlotColor;

[HarmonyPatch]
internal static class ColorPickerOverlay_Close_Patch
{
	public static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("CBSafe.Infrastructure.ColorPickerOverlay");
		if (!(type == null))
		{
			return AccessTools.Method(type, "Close");
		}
		return null;
	}

	public static void Postfix()
	{
		try
		{
			CBSlotColorPlugin.PersistNow();
		}
		catch
		{
		}
	}
}
