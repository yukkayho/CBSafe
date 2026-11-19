using CBSafe.Colors.UI;
using CBSafe.Core;
using CBSafe.Crosshair.UI;
using HarmonyLib;
using UnityEngine.UI;

namespace CBSafe.Patches;

[HarmonyPatch(typeof(TabToggle), "SwitchTo")]
internal static class TabToggleSwitchToPatch
{
	private static bool Prefix(Button sourceTab)
	{
		if (sourceTab == null)
		{
			return true;
		}
		string name = sourceTab.transform.name;
		if (name == LocStrings.Name_Optimization || name == LocStrings.Name_Prochee || name == LocStrings.Name_Color_TabButton || name == LocStrings.Name_Crosshair_TabButton)
		{
			return false;
		}
		try
		{
			TabSystemManager.PrepareNativeTabSwitch();
		}
		catch
		{
		}
		try
		{
			CrosshairTabManager.PrepareNativeTabSwitch();
		}
		catch
		{
		}
		return true;
	}
}
