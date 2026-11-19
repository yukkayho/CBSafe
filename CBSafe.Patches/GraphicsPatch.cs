using System;
using System.Collections.Generic;
using System.Reflection;
using CBSafe.Fps;
using ConVar;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "virtualization")]
internal class GraphicsPatch
{
	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(FoliageCell), "CalculateLOD")]
	internal static class FoliageCellPatch
	{
		[HarmonyPostfix]
		public static void Postfix(ref float __result)
		{
			if (Defines.OptimizeGrassCached)
			{
				__result = 1f;
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(CameraSettings), "Update")]
	internal static class CameraSettingsPatch
	{
		[HarmonyPostfix]
		public static void Postfix(CameraSettings __instance)
		{
			if (Defines.OptimizeGraphicCached && !(((__instance != null) ? __instance.cam : null) == null))
			{
				__instance.cam.farClipPlane = 400f;
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(LightLOD), "UpdateLight", new Type[]
	{
		typeof(float),
		typeof(float)
	})]
	internal static class LightLODPatch
	{
		[HarmonyPrefix]
		public static void Prefix(ref float lightIntensity, ref float shadowIntensity)
		{
			if (Defines.OptimizeShadowsCached)
			{
				lightIntensity = 0f;
				shadowIntensity = 0f;
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(SunSettings), "Update")]
	internal static class SunSettingsPatch
	{
		[HarmonyPostfix]
		public static void Postfix(SunSettings __instance)
		{
			if (Defines.OptimizeShadowsCached && !(((__instance != null) ? __instance.light : null) == null))
			{
				__instance.light.shadows = LightShadows.None;
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(TreeLOD.State), "Show")]
	internal static class TreeLODPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(TreeLOD.State __instance)
		{
			if (Defines.OptimizeShadowsCached)
			{
				__instance.shadowMode = ShadowCastingMode.Off;
			}
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(MeshLOD.State), "Show")]
	internal static class MeshLODPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref ShadowCastingMode shadowMode)
		{
			if (Defines.OptimizeShadowsCached)
			{
				shadowMode = ShadowCastingMode.Off;
			}
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(RendererLOD.State), "Show")]
	internal static class RendererLODPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(RendererLOD.State __instance)
		{
			if (Defines.OptimizeShadowsCached)
			{
				__instance.shadowMode = ShadowCastingMode.Off;
			}
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(ConVar.Graphics), "EnforceShadowDistanceBounds")]
	internal static class GraphicsShadowPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (Defines.OptimizeShadowsCached)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class SkipTaaPatch
	{
		private static MethodBase TargetMethod()
		{
			return FindRenderMethod("RustTemporalAntialiasingRenderer");
		}

		private static bool Prepare()
		{
			return TargetMethod() != null;
		}

		[HarmonyPrefix]
		public static bool Prefix(PostProcessRenderContext context)
		{
			return !BypassPostProcess(context, SkipPostFx || OptDark.On(OptState.DisableAa) || OptDark.On(OptState.CheapCamera));
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class DisableAntialiasingPatch
	{
		private static MethodBase TargetMethod()
		{
			return FindRenderMethod(typeof(PostProcessLayer));
		}

		private static bool Prepare()
		{
			return TargetMethod() != null;
		}

		[HarmonyPrefix]
		public static void Prefix(PostProcessLayer __instance)
		{
			if (!(__instance == null))
			{
				int instanceID = __instance.GetInstanceID();
				bool flag = SkipPostFx || OptDark.On(OptState.DisableAa) || OptDark.On(OptState.CheapCamera);
				if (!AaModes.TryGetValue(instanceID, out var value))
				{
					value = __instance.antialiasingMode;
					AaModes[instanceID] = value;
				}
				__instance.antialiasingMode = ((!flag) ? value : PostProcessLayer.Antialiasing.None);
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class SkipSharpenVignettePatch
	{
		private static MethodBase TargetMethod()
		{
			return FindRenderMethod(typeof(SharpenAndVignetteRenderer));
		}

		private static bool Prepare()
		{
			if (SkipPostFx)
			{
				return TargetMethod() != null;
			}
			return false;
		}

		[HarmonyPrefix]
		public static bool Prefix(PostProcessRenderContext context)
		{
			return !BypassPostProcess(context, SkipPostFx || OptDark.On(OptState.DisableSharpen));
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class SkipGodRaysPatch
	{
		private static MethodBase TargetMethod()
		{
			return FindRenderMethod(typeof(GodRaysRenderer));
		}

		private static bool Prepare()
		{
			return TargetMethod() != null;
		}

		[HarmonyPrefix]
		public static bool Prefix(PostProcessRenderContext context)
		{
			return !BypassPostProcess(context, SkipPostFx || OptDark.On(OptState.DisableSunRays));
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class SkipLensDirtPatch
	{
		private static MethodBase TargetMethod()
		{
			return FindRenderMethod(typeof(LensDirtinessRenderer));
		}

		private static bool Prepare()
		{
			return TargetMethod() != null;
		}

		[HarmonyPrefix]
		public static bool Prefix(PostProcessRenderContext context)
		{
			return !BypassPostProcess(context, SkipPostFx || OptDark.On(OptState.DisableHeavyPostProcess));
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class SkipMotionBlurPatch
	{
		private static MethodBase TargetMethod()
		{
			return FindRenderMethod(typeof(MotionBlurRenderer));
		}

		private static bool Prepare()
		{
			return TargetMethod() != null;
		}

		[HarmonyPrefix]
		public static bool Prefix(PostProcessRenderContext context)
		{
			return !BypassPostProcess(context, SkipPostFx || OptDark.On(OptState.DisableMotionBlur));
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class SkipBloomPatch
	{
		private static MethodBase TargetMethod()
		{
			return FindRenderMethod(typeof(BloomRenderer));
		}

		private static bool Prepare()
		{
			return TargetMethod() != null;
		}

		[HarmonyPrefix]
		public static bool Prefix(PostProcessRenderContext context)
		{
			return !BypassPostProcess(context, SkipPostFx || OptDark.On(OptState.DisableBloom));
		}
	}

	private static readonly Dictionary<int, PostProcessLayer.Antialiasing> AaModes = new Dictionary<int, PostProcessLayer.Antialiasing>();

	private static bool SkipPostFx => Defines.OptimizeGraphicCached;

	private static bool BypassPostProcess(PostProcessRenderContext context, bool enabled)
	{
		if (!enabled || context == null || context.command == null)
		{
			return false;
		}
		try
		{
			context.command.Blit(context.source, context.destination);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static MethodBase FindRenderMethod(Type type)
	{
		if (!(type == null))
		{
			return AccessTools.Method(type, "Render");
		}
		return null;
	}

	private static MethodBase FindRenderMethod(string typeName)
	{
		return FindRenderMethod(AccessTools.TypeByName(typeName));
	}
}
