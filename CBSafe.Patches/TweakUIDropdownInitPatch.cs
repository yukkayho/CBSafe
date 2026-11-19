using System;
using HarmonyLib;
using UnityEngine;

namespace CBSafe.Patches;

[HarmonyPatch(typeof(TweakUIDropdown), "Init")]
internal static class TweakUIDropdownInitPatch
{
	private static bool Prefix(TweakUIDropdown __instance)
	{
		try
		{
			if (__instance == null)
			{
				return true;
			}
			GameObject gameObject = __instance.gameObject;
			if (gameObject == null)
			{
				return true;
			}
			string name = gameObject.name;
			if (string.IsNullOrEmpty(name))
			{
				return true;
			}
			if (IsManagedDropdown(name))
			{
				return false;
			}
			return true;
		}
		catch
		{
			return true;
		}
	}

	private static bool IsManagedDropdown(string name)
	{
		if (!name.StartsWith("CBSafe_Crosshair_Dropdown_", StringComparison.Ordinal) && !name.StartsWith("CBSafe_Color_Dropdown_", StringComparison.Ordinal))
		{
			return name.StartsWith("CBSafe_Dropdown_", StringComparison.Ordinal);
		}
		return true;
	}

	private static bool HasRequiredBindings(TweakUIDropdown dropdown)
	{
		try
		{
			return dropdown.Current != null && dropdown.Opener != null && dropdown.Dropdown != null && dropdown.DropdownContainer != null && dropdown.DropdownItemPrefab != null && dropdown.onValueChanged != null;
		}
		catch
		{
			return false;
		}
	}
}
