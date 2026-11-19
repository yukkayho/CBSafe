using System.Collections.Generic;
using System.Reflection;
using CBSafe.Colors.Infrastructure;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
[HydraNoObfuscate]
internal static class BlackGatherImageFillPatch
{
	public static List<Color> KnownCustomColors = new List<Color>();

	public static bool IsKnownColor(Color c)
	{
		foreach (Color knownCustomColor in KnownCustomColors)
		{
			if (Mathf.Abs(c.r - knownCustomColor.r) < 0.01f && Mathf.Abs(c.g - knownCustomColor.g) < 0.01f && Mathf.Abs(c.b - knownCustomColor.b) < 0.01f)
			{
				return true;
			}
		}
		return false;
	}

	public static void RegisterColor(Color c)
	{
		if (!IsKnownColor(c))
		{
			KnownCustomColors.Add(c);
		}
	}

	private static MethodBase TargetMethod()
	{
		return AccessTools.PropertySetter(typeof(Image), "fillAmount");
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Prefix(Image __instance)
	{
		try
		{
			if (PluginConfig.TryResolveGatherCircleColor(out var color) && !(__instance == null) && __instance.type == Image.Type.Filled && __instance.fillMethod == Image.FillMethod.Radial360)
			{
				Color color2 = __instance.color;
				if ((color2.g > color2.r && color2.g > color2.b) || IsKnownColor(color2))
				{
					__instance.color = color;
					RegisterColor(color);
				}
			}
		}
		catch
		{
		}
	}
}
