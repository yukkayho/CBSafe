using System;
using UnityEngine;

namespace CBSafe.Fps;

internal static class RenderPatches
{
	public static bool ProbeRender()
	{
		return FeatureWatch.PassOrSkip(FeatureId.Probes, OptState.DisableReflectionProbes);
	}

	public static bool ProbePreRender()
	{
		return FeatureWatch.PassOrSkip(FeatureId.Probes, OptState.DisableReflectionProbes);
	}

	public static bool AddDecal(DeferredDecal decal)
	{
		if (!OptState.DisableDecals || IsTreeMark(decal))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Decals);
		return false;
	}

	public static bool RefreshDecal(DeferredDecal decal)
	{
		if (!OptState.DisableDecals || IsTreeMark(decal))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Decals);
		return false;
	}

	public static bool DecalCmd()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Decals, OptState.DisableDecals);
	}

	private static bool IsTreeMark(DeferredDecal decal)
	{
		if (decal == null)
		{
			return false;
		}
		try
		{
			Transform transform = decal.transform;
			while (transform != null)
			{
				string name = transform.name;
				if (!string.IsNullOrEmpty(name) && (name.IndexOf("tree_mark", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("treemark", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("tree_marker", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					return true;
				}
				transform = transform.parent;
			}
			Material material = decal.material;
			if (material == null)
			{
				return false;
			}
			string name2 = material.name;
			return !string.IsNullOrEmpty(name2) && name2.IndexOf("tree_mark", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	public static bool MaxLights(ref int __result)
	{
		if (OptDark.Night || OptState.MaxDynamicLights >= 8)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Lights);
		__result = Math.Max(0, OptState.MaxDynamicLights);
		return false;
	}

	public static bool WaterOverlayUpdate()
	{
		return true;
	}

	public static bool AoUpdate()
	{
		if (!OptDark.Night)
		{
			return !OptState.DisableSsao;
		}
		return true;
	}

	public static bool AoPreRender()
	{
		if (!OptDark.Night)
		{
			return !OptState.DisableSsao;
		}
		return true;
	}

	public static bool AoPostRender()
	{
		if (!OptDark.Night)
		{
			return !OptState.DisableSsao;
		}
		return true;
	}

	public static bool ContactOff()
	{
		return OptDark.On(OptState.DisableShadows || OptState.DisableContactShadows);
	}

	public static void ContactToggle(ref bool state)
	{
		if (ContactOff())
		{
			FeatureWatch.Skip(FeatureId.ContactShadows);
			state = false;
		}
	}

	public static bool ContactPreRender()
	{
		return !ContactOff();
	}

	public static bool ContactPostRender()
	{
		return !ContactOff();
	}

	public static bool ParticleShow()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Particles, OptState.DisableSecondaryParticles);
	}

	public static bool ItemSkinUnload(ref bool __result)
	{
		return true;
	}

	public static bool DecorEnable()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Decor, OptState.DisableDecor);
	}

	public static bool DecorQuality(ref float __result)
	{
		if (!OptState.DisableDecor)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Decor);
		__result = 0f;
		return false;
	}

	public static bool ParticleQuality(ref float __result)
	{
		if (!OptState.DisableSecondaryParticles)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Particles);
		__result = 0f;
		return false;
	}

	public static bool HlodOn(ref bool __result)
	{
		if (!OptState.AggressiveWorldLod)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.WorldLod);
		__result = true;
		return false;
	}

	public static bool TreeQuality(ref float __result)
	{
		if (!OptState.AggressiveWorldLod && !OptState.CheapTrees)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.WorldLod);
		__result = (OptState.CheapTrees ? 0.05f : 0.15f);
		return false;
	}

	public static bool TreeMeshes(ref int __result)
	{
		if (!OptState.AggressiveWorldLod && !OptState.CheapTrees)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.WorldLod);
		__result = 1;
		return false;
	}

	public static bool MeshQuality(ref float __result)
	{
		if (!OptState.AggressiveWorldLod)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.WorldLod);
		__result = 0.15f;
		return false;
	}

	public static bool GrassQuality(ref float __result)
	{
		if (OptState.DisableGrass)
		{
			FeatureWatch.Skip(FeatureId.Grass);
			__result = 0f;
			return false;
		}
		if (!OptState.CoarseGrassLod)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.GrassLod);
		__result = 0.2f;
		return false;
	}

	public static bool ImpostorShadowsOff(ref bool __result)
	{
		if (OptDark.Night || (!OptState.DisableImpostorShadows && !OptState.DisableShadows))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.ImpostorSh);
		__result = false;
		return false;
	}

	public static bool WavesOff(ref bool __result)
	{
		if (!OptState.DisableWaterWaves)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Waves);
		__result = false;
		return false;
	}

	public static bool ParallaxOff(ref int __result)
	{
		if (!OptState.PointFilterTextures && !OptState.CheapTerrain)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.PointFilter);
		__result = 0;
		return false;
	}

	public static bool AnisoOff(ref int __result)
	{
		if (!OptState.PointFilterTextures)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.PointFilter);
		__result = 1;
		return false;
	}

	public static bool OverlaySkip()
	{
		return FeatureWatch.PassOrSkip(FeatureId.PostProcess, OptState.DisableHeavyPostProcess);
	}

	public static bool ColdOverlayUpdate()
	{
		return true;
	}

	public static bool HurtIntensity(ref float __result)
	{
		if (OptDark.Night || (!OptState.DisableDamageOverlay && !OptState.DisableHeavyPostProcess))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.PostProcess);
		__result = 0f;
		return false;
	}

	public static bool WeatherWind(ref float __result)
	{
		if (!OptState.DisableTreeWind)
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.TreeWind);
		__result = 0f;
		return false;
	}

	public static bool GcCollectOff()
	{
		return !OptState.AutoMemory;
	}
}
