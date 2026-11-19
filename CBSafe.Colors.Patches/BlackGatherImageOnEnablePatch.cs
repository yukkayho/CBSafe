using System.Reflection;
using CBSafe.Colors.Infrastructure;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
[HydraNoObfuscate]
internal static class BlackGatherImageOnEnablePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(Image), "OnEnable");
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
				if ((color2.g > color2.r && color2.g > color2.b) || BlackGatherImageFillPatch.IsKnownColor(color2))
				{
					__instance.color = color;
					BlackGatherImageFillPatch.RegisterColor(color);
				}
			}
		}
		catch
		{
		}
	}
}
