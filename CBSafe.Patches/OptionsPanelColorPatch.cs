using System.Reflection;
using CBSafe.Core;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
[HarmonyPatch(typeof(MenuBackgroundVideo), "ReadyVideo")]
internal static class OptionsPanelColorPatch
{
	private static readonly Color32 NavColor = new Color32(29, 32, 31, byte.MaxValue);

	[HarmonyPostfix]
	internal static void Postfix()
	{
		try
		{
			ApplyOptionsColors();
		}
		catch
		{
		}
	}

	internal static void ApplyOptionsColors()
	{
		GameObject gameObject = GameObject.Find(LocStrings.Path_OptionsRoot);
		if (gameObject == null)
		{
			return;
		}
		foreach (Image componentsInChild in gameObject.GetComponentsInChildren<Image>(includeInactive: true))
		{
			if (!(componentsInChild == null))
			{
				string name = componentsInChild.name;
				Transform parent = componentsInChild.transform.parent;
				string text = ((parent != null) ? parent.name : null);
				Transform transform = ((parent != null) ? parent.parent : null);
				string text2 = ((transform != null) ? transform.name : null);
				if (name == LocStrings.Marker_Sidebar_Background && text == LocStrings.Marker_Categories)
				{
					componentsInChild.color = NavColor;
				}
				else if (name == LocStrings.Marker_Image && text == LocStrings.Marker_Sidebar_Background && text2 == LocStrings.Marker_Categories)
				{
					componentsInChild.color = NavColor;
				}
				else if (name == LocStrings.Marker_Categories && text == LocStrings.Marker_Options)
				{
					componentsInChild.color = NavColor;
				}
			}
		}
	}
}
