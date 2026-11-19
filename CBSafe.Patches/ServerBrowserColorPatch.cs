using System.Reflection;
using CBSafe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
internal static class ServerBrowserColorPatch
{
	private static readonly Color32 SidebarColor = new Color32(0, 0, 0, byte.MaxValue);

	internal static void ApplyServerBrowserColors(ServerBrowser browser)
	{
		if (browser == null)
		{
			return;
		}
		foreach (Image componentsInChild in browser.GetComponentsInChildren<Image>(includeInactive: true))
		{
			if (!(componentsInChild == null) && componentsInChild.name == LocStrings.Marker_Sidebar_Background)
			{
				componentsInChild.color = SidebarColor;
			}
		}
	}
}
