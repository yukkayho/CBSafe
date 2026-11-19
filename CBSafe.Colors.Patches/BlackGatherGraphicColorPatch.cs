using System.Reflection;
using CBSafe.Colors.Infrastructure;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
[HydraNoObfuscate]
internal static class BlackGatherGraphicColorPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.PropertySetter(typeof(Graphic), "color");
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Prefix(Graphic __instance, ref Color value)
	{
		try
		{
			if (PluginConfig.TryResolveGatherCircleColor(out var color) && !(__instance == null))
			{
				Image image = __instance.TryCast<Image>();
				if (!(image == null) && image.type == Image.Type.Filled && image.fillMethod == Image.FillMethod.Radial360 && ((value.g > value.r && value.g > value.b) || BlackGatherImageFillPatch.IsKnownColor(value)))
				{
					value = color;
					BlackGatherImageFillPatch.RegisterColor(color);
				}
			}
		}
		catch
		{
		}
	}
}
