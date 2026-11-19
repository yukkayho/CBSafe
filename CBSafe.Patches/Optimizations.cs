using System;
using ConVar;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace CBSafe.Patches;

public static class Optimizations
{
	[HarmonyPatch(typeof(FoliageCell), "CalculateLOD")]
	internal static class FoliageCellPatch
	{
		[HarmonyPostfix]
		public static void Postfix(ref float __result)
		{
			if (ModConfig.OptimizeGrass.Value)
			{
				__result = 1f;
			}
		}
	}

	[HarmonyPatch(typeof(CameraSettings), "Update")]
	internal static class CameraSettingsPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(CameraSettings __instance)
		{
			if (ModConfig.SuperPotatoMode.Value)
			{
				__instance.cam.farClipPlane = 400f;
				return false;
			}
			return true;
		}
	}

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
			if (ModConfig.OptimizeShadows.Value)
			{
				lightIntensity = 0f;
				shadowIntensity = 0f;
			}
		}
	}

	[HarmonyPatch(typeof(SunSettings), "Update")]
	internal static class SunSettingsPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(SunSettings __instance)
		{
			if (ModConfig.OptimizeShadows.Value)
			{
				__instance.light.shadows = LightShadows.None;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(TreeLOD.State), "Show")]
	internal static class TreeLODPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(TreeLOD.State __instance)
		{
			if (ModConfig.OptimizeShadows.Value)
			{
				__instance.shadowMode = ShadowCastingMode.Off;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(MeshLOD.State), "Show")]
	internal static class MeshLODPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref ShadowCastingMode shadowMode)
		{
			if (ModConfig.OptimizeShadows.Value)
			{
				shadowMode = ShadowCastingMode.Off;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(RendererLOD.State), "Show")]
	internal static class RendererLODPatch
	{
		[HarmonyPrefix]
		public static void Prefix(ref bool forceShadowsOff)
		{
			if (ModConfig.OptimizeShadows.Value)
			{
				forceShadowsOff = true;
			}
		}
	}

	[HarmonyPatch(typeof(ConVar.Graphics), "EnforceShadowDistanceBounds")]
	internal static class ShadowDistanceBoundsPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (ModConfig.OptimizeShadows.Value)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Climate), "GetFog")]
	internal static class FogPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (ModConfig.OptimizeWeather.Value)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Climate), "GetRain")]
	internal static class RainPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (ModConfig.OptimizeWeather.Value)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Climate), "GetSnow")]
	internal static class SnowPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (ModConfig.OptimizeWeather.Value)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Climate), "GetWind")]
	internal static class WindPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (ModConfig.OptimizeWeather.Value)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Climate), "GetThunder")]
	internal static class ThunderPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (ModConfig.OptimizeWeather.Value)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Climate), "GetAurora")]
	internal static class AuroraPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref float __result)
		{
			if (ModConfig.OptimizeWeather.Value)
			{
				__result = 0f;
				return false;
			}
			return true;
		}
	}

	public static void ApplySettings()
	{
		UpdateShadows();
		UpdatePotatoMode();
	}

	public static void UpdateShadows()
	{
		try
		{
			if (ModConfig.OptimizeShadows.Value)
			{
				ConVar.Graphics.shadowdistance = 0f;
				ConVar.Graphics.shadowcascades = 0;
				ConVar.Graphics.shadowmode = 0;
				ConVar.Graphics.shadowquality = 0;
				ConVar.Graphics.shadowlights = 0;
			}
			else
			{
				ConVar.Graphics.shadowcascades = 1;
				ConVar.Graphics.shadowdistance = 40f;
				ConVar.Graphics.shadowmode = 1;
				ConVar.Graphics.shadowquality = 1;
				ConVar.Graphics.shadowlights = 1;
			}
		}
		catch
		{
		}
	}

	public static void UpdatePotatoMode()
	{
		try
		{
			if (ModConfig.SuperPotatoMode.Value)
			{
				ConVar.Terrain.quality = 1f;
				ConVar.Tree.quality = 0f;
				Water.quality = 0;
				Water.reflections = 0;
				Particle.quality = 0f;
				ConVar.Graphics.drawdistance = 400f;
				ConVar.Graphics.showtexeldensity = 0;
				ConVar.Graphics.quality = 0;
				ConVar.Graphics.lodbias = 0.25f;
				ConVar.Graphics.af = 1;
				ConVar.Graphics.shaderlod = 100;
				Grass.quality = 0f;
				Grass.quality01 = 0f;
				ConVar.Tree.meshes = 0;
				ConVar.Mesh.quality = 0f;
				Decor.quality = 0f;
				ConVar.Graphics.contactshadows = false;
				ConVar.Graphics.parallax = 0;
				ConVar.Graphics.grassshadows = false;
			}
		}
		catch
		{
		}
	}
}
