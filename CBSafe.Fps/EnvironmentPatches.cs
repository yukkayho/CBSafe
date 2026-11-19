using UnityEngine;

namespace CBSafe.Fps;

internal static class EnvironmentPatches
{
	public static bool FoliageRendererOnEnable()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Grass, OptState.DisableGrass);
	}

	public static bool WaterOceanSim()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Waves, OptState.DisableWaterWaves);
	}

	public static bool WaterDynamicsUpdate()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Waves, OptState.DisableWaterWaves);
	}

	public static bool WaterProbe()
	{
		return FeatureWatch.PassOrSkip(FeatureId.Probes, OptState.DisableReflectionProbes);
	}

	public static bool WaterCheckProbe()
	{
		return FeatureWatch.PassOrSkip(FeatureId.Probes, OptState.DisableReflectionProbes);
	}

	public static bool WaterReflectionsOff(ref int __result)
	{
		if (OptDark.Night || !OptState.DisableReflectionProbes)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Probes);
		__result = 0;
		return false;
	}

	public static bool WeatherIntensity(ref float __result)
	{
		if (OptDark.Night || !OptState.DisableWeather)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Weather);
		__result = 0f;
		return false;
	}

	public static void WeatherAfterUpdate(WeatherEffect __instance)
	{
		if (OptDark.Night || !OptState.DisableWeather || __instance == null || __instance is UnderwaterEffect || (!(__instance is RainEffect) && !(__instance is SnowEffect)))
		{
			return;
		}
		FeatureWatch.Skip(FeatureId.Weather);
		try
		{
			__instance.currentIntensity = 0f;
		}
		catch
		{
		}
	}

	public static void TodWeatherPost(TOD_WeatherManager __instance)
	{
		if (OptDark.Night || !OptState.DisableWeather || __instance == null)
		{
			return;
		}
		FeatureWatch.Skip(FeatureId.Weather);
		try
		{
			__instance.SetRainEmission(0f);
			ParticleSystem rainParticleSystem = __instance.RainParticleSystem;
			if (rainParticleSystem != null && rainParticleSystem.isPlaying)
			{
				rainParticleSystem.Stop(withChildren: true);
			}
		}
		catch
		{
		}
	}

	public static bool TodProbe()
	{
		return FeatureWatch.PassOrSkip(FeatureId.Probes, OptState.DisableReflectionProbes);
	}

	public static bool TodReflection()
	{
		return FeatureWatch.PassOrSkip(FeatureId.Probes, OptState.DisableReflectionProbes);
	}

	public static bool TodRays(RenderTexture source, RenderTexture destination)
	{
		if (OptState.DisableSunRays)
		{
			FeatureWatch.Skip(FeatureId.SunRays);
		}
		return true;
	}
}
