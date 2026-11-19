using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBSafe.Fps;

public static class FeatureProbe
{
	public static void Tick()
	{
		Set(FeatureId.Grass, () => "OK Paused=" + FoliageGrid.Paused, "WAIT no FoliageGrid");
		Set(FeatureId.Waves, () => (!(WaterSystem.Instance == null)) ? "OK water" : "WAIT no WaterSystem", "WAIT no WaterSystem");
		Set(FeatureId.Sky, delegate
		{
			TOD_Sky instance2 = TOD_Sky.Instance;
			if (((instance2 != null) ? instance2.Components : null) == null)
			{
				return "WAIT no TOD_Sky";
			}
			bool flag = instance2.Components.Clouds != null && instance2.Components.Clouds.activeSelf;
			if (!OptState.DisableSky)
			{
				return "OFF";
			}
			return (!flag) ? "OK clouds off" : "IDLE clouds on";
		}, "WAIT no TOD_Sky");
		Set(FeatureId.SunRays, delegate
		{
			TOD_Sky instance = TOD_Sky.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				TOD_Components components = instance.Components;
				obj = ((components != null) ? components.Rays : null);
			}
			if ((UnityEngine.Object)obj == null)
			{
				return "WAIT no rays";
			}
			return (!instance.Components.Rays.enabled) ? "OK rays off" : "IDLE rays on";
		}, "WAIT no TOD_Sky");
		Dictionary<FeatureId, FeatureInfo> all = FeatureWatch.All;
		all[FeatureId.Shadows].Runtime = "OK shadows=" + QualitySettings.shadows;
		all[FeatureId.SmartShadows].Runtime = "OK casc=" + QualitySettings.shadowCascades + " dist=" + QualitySettings.shadowDistance.ToString("0");
		all[FeatureId.Textures].Runtime = "OK mip=" + QualitySettings.masterTextureLimit;
		all[FeatureId.PointFilter].Runtime = "OK aniso=" + QualitySettings.anisotropicFiltering;
		all[FeatureId.SkinDeform].Runtime = "OK weights=" + QualitySettings.skinWeights;
		all[FeatureId.Lights].Runtime = "OK pixelLights=" + QualitySettings.pixelLightCount;
		all[FeatureId.ParticleColl].Runtime = "OK raycasts=" + QualitySettings.particleRaycastBudget;
		all[FeatureId.AutoMemory].Runtime = (OptState.AutoMemory ? "OK armed" : "OFF");
		all[FeatureId.CheapCam].Runtime = "OK lodBias=" + QualitySettings.lodBias.ToString("0.00") + " aa=" + QualitySettings.antiAliasing;
		all[FeatureId.Terrain].Runtime = ((Terrain.activeTerrain == null) ? "WAIT no terrain" : ("OK err=" + Terrain.activeTerrain.heightmapPixelError.ToString("0")));
		all[FeatureId.Gibs].Runtime = (OptState.DisableGibs ? "OK skip CreateGibs" : "OFF");
		all[FeatureId.Atmosphere].Runtime = "OFF kept";
		all[FeatureId.LinearFog].Runtime = "OFF kept";
		all[FeatureId.Bloom].Runtime = (OptState.DisableBloom ? "OK skip bloom" : "OFF");
		all[FeatureId.MotionBlur].Runtime = (OptState.DisableHeavyPostProcess ? "OK skip MB" : "OFF");
		all[FeatureId.Aa].Runtime = ((OptState.CheapCamera || OptState.DisableHeavyPostProcess) ? "OK skip AA blit" : "OFF");
		all[FeatureId.Planar].Runtime = (OptState.DisablePlanarReflections ? "OK skip planar" : "OFF");
		all[FeatureId.Sss].Runtime = "OFF";
		all[FeatureId.Cloth].Runtime = (OptState.DisableCloth ? "OK skip cloth" : "OFF");
		all[FeatureId.Instancing].Runtime = InstancingRuntime();
		all[FeatureId.ContactShadows].Runtime = (OptState.DisableContactShadows ? "OK skip contact" : "OFF");
	}

	private static string InstancingRuntime()
	{
		try
		{
			if (!OptConvarBridge.TryGetBool("Render", "IsInstancingEnabled", out var value))
			{
				return OptState.EnableInstancing ? "WAIT Render" : "OFF";
			}
			return OptState.EnableInstancing ? ("OK force on getter=" + value) : ("OFF cfg=" + value);
		}
		catch
		{
			return OptState.EnableInstancing ? "WAIT Render" : "OFF";
		}
	}

	private static void Set(FeatureId id, Func<string> ok, string wait)
	{
		try
		{
			FeatureWatch.All[id].Runtime = ok();
		}
		catch
		{
			FeatureWatch.All[id].Runtime = wait;
		}
	}
}
