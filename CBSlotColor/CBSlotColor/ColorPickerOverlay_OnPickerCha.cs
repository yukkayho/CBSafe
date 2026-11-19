using System;
using System.Reflection;
using HarmonyLib;

namespace CBSlotColor;

[HarmonyPatch]
internal static class ColorPickerOverlay_OnPickerChanged_Patch
{
	public static MethodBase TargetMethod()
	{
		Type type = AccessTools.TypeByName("CBSafe.Infrastructure.ColorPickerOverlay");
		if (!(type == null))
		{
			return AccessTools.Method(type, "OnPickerChanged");
		}
		return null;
	}

	public static void Postfix()
	{
		try
		{
			CBSlotColorPlugin.PreserveAlphaOnPickerWrite();
			CBSlotColorPlugin.UpdateColors();
			CBSlotColorPlugin.PersistNow();
			CBSlotColorPlugin.ThrottleRefreshOpenIcons();
		}
		catch
		{
		}
	}
}
