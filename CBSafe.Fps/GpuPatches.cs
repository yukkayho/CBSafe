using System;
using UnityEngine;

namespace CBSafe.Fps;

internal static class GpuPatches
{
	public static bool FoliageApplyMesh()
	{
		return FoliageMeshPass();
	}

	public static bool FoliageRefreshMesh()
	{
		return FoliageMeshPass();
	}

	private static bool FoliageMeshPass()
	{
		if (!OptState.DisableGrass)
		{
			return true;
		}
		try
		{
			if (FoliageGrid.Paused)
			{
				FeatureWatch.Skip(FeatureId.Grass);
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	public static void FoliageToggleMesh(ref bool __0)
	{
		if (OptState.DisableGrass)
		{
			__0 = false;
			FeatureWatch.Skip(FeatureId.Grass);
		}
	}

	public static bool CreateGibs()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Gibs, OptState.DisableGibs);
	}

	public static bool ImpostorShadowBatch()
	{
		return FeatureWatch.PassOrSkip(FeatureId.ImpostorSh, OptState.DisableShadows || OptState.DisableImpostorShadows);
	}

	public static bool LightCloneShadow()
	{
		return FeatureWatch.PassOrSkip(FeatureId.Shadows, OptState.DisableShadows || OptState.CheapCamera);
	}

	public static bool WindZoneUpdate()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Weather, OptState.DisableWeather || OptState.DisableGrass || OptState.DisableGrassDisp || OptState.DisableTreeWind);
	}

	public static bool WindForce(ref Vector4 __result)
	{
		if (!OptState.DisableTreeWind && !OptState.DisableGrassDisp && !OptDark.On(OptState.DisableWeather) && !OptState.DisableGrass)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Weather);
		__result = Vector4.zero;
		return false;
	}

	public static bool FoliageDispOff()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.GrassDisp, OptState.DisableGrassDisp || OptState.DisableGrass);
	}

	public static void TreeBillboard(TreeLOD __instance, ref int __result)
	{
		if (!OptState.CheapTrees || __instance == null)
		{
			return;
		}
		try
		{
			int billboardLOD = __instance.BillboardLOD;
			if (__result < billboardLOD)
			{
				__result = billboardLOD;
			}
		}
		catch
		{
		}
	}

	public static void TerrainSetup(TerrainQuality __instance)
	{
		if (OptState.CheapTerrain && !(__instance == null))
		{
			FeatureWatch.Skip(FeatureId.Terrain);
			__instance.HeightMapError = Math.Max(__instance.HeightMapError, 20f);
			__instance.BaseMapDistance = Math.Min(__instance.BaseMapDistance, 80f);
		}
	}
}
