using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CBSafe.Fps;

public static class FeatureWatch
{
	public static readonly Dictionary<FeatureId, FeatureInfo> All;

	public static readonly FeatureInfo[] ById;

	static FeatureWatch()
	{
		All = new Dictionary<FeatureId, FeatureInfo>();
		ById = new FeatureInfo[64];
		Add(FeatureId.Grass, "Grass / foliage", () => OptState.DisableGrass);
		Add(FeatureId.GrassLod, "Grass displacement LOD", () => OptState.CoarseGrassLod);
		Add(FeatureId.Waves, "Water waves", () => OptState.DisableWaterWaves);
		Add(FeatureId.Probes, "Reflection probes", () => OptState.DisableReflectionProbes);
		Add(FeatureId.Weather, "Weather particles", () => OptState.DisableWeather);
		Add(FeatureId.Particles, "Secondary particles", () => OptState.DisableSecondaryParticles);
		Add(FeatureId.ParticleColl, "Particle collision", () => OptState.ReduceParticleCollision);
		Add(FeatureId.Decals, "Decals", () => OptState.DisableDecals);
		Add(FeatureId.PostProcess, "Heavy post-process", () => false);
		Add(FeatureId.SunRays, "Sun rays (god rays only, not sky scatter)", () => OptState.DisableSunRays);
		Add(FeatureId.SmartShadows, "Smart shadows", () => OptState.SmartShadows);
		Add(FeatureId.Shadows, "Disable all shadows", () => OptState.DisableShadows);
		Add(FeatureId.DistantAnims, "Distant IK / cloth", () => OptState.SimplifyDistantAnims);
		Add(FeatureId.Cloth, "Disable clothing cloth sim", () => OptState.DisableCloth);
		Add(FeatureId.Corpse, "Corpse physics", () => OptState.DisableCorpsePhysics);
		Add(FeatureId.AutoMemory, "Auto GC", () => OptState.AutoMemory);
		Add(FeatureId.SkinDeform, "Skin deform 1-bone", () => OptState.SimplifySkinDeform);
		Add(FeatureId.Workshop, "Block workshop skins", () => OptState.BlockWorkshopSkins);
		Add(FeatureId.SkinUnload, "Unload unused skins", () => OptState.UnloadUnusedSkins);
		Add(FeatureId.WorldLod, "Aggressive world LOD", () => OptState.AggressiveWorldLod);
		Add(FeatureId.Culling, "Aggressive culling", () => OptState.AggressiveCulling);
		Add(FeatureId.Sky, "Disable sky/clouds", () => OptState.DisableSky);
		Add(FeatureId.PointFilter, "Minecraft point filter", () => OptState.PointFilterTextures);
		Add(FeatureId.Lights, "Dynamic light cap", () => OptState.MaxDynamicLights < 8);
		Add(FeatureId.Textures, "Texture downsample", () => OptState.TextureDownsample > 0);
		Add(FeatureId.Gibs, "Disable gibs / debris", () => OptState.DisableGibs);
		Add(FeatureId.Atmosphere, "Disable volumetric fog", () => OptState.DisableAtmosphere);
		Add(FeatureId.ImpostorSh, "Disable impostor shadows", () => OptState.DisableImpostorShadows);
		Add(FeatureId.Terrain, "Cheap terrain (pixel error)", () => OptState.CheapTerrain);
		Add(FeatureId.CheapCam, "Cheap camera (no MSAA/HDR, LOD bias)", () => OptState.CheapCamera);
		Add(FeatureId.ParticleIk, "Particle IK", () => OptState.DisableParticleIk);
		Add(FeatureId.Planar, "Planar water reflections", () => OptState.DisablePlanarReflections);
		Add(FeatureId.Bloom, "Bloom / lens dirt", () => false);
		Add(FeatureId.MotionBlur, "Motion blur", () => false);
		Add(FeatureId.Aa, "FXAA / SMAA / TSSAA", () => OptState.CheapCamera);
		Add(FeatureId.LinearFog, "Linear fog blit", () => OptState.DisableLinearFog);
		Add(FeatureId.ExplosionBloom, "Explosion bloom", () => false);
		Add(FeatureId.ContactShadows, "Contact shadows", () => OptState.DisableShadows);
		Add(FeatureId.Sss, "SSS / refraction", () => false);
		Add(FeatureId.VolBeams, "Volumetric light beams", () => OptState.DisableVolumetricBeams);
		Add(FeatureId.Instancing, "Force GPU instancing ON", () => OptState.EnableInstancing);
		Add(FeatureId.Decor, "Disable world decor clutter", () => OptState.DisableDecor);
		Add(FeatureId.UnlockFps, "Unlock FPS (no VSync)", () => OptState.UnlockFps);
		Add(FeatureId.Dof, "Depth of field", () => OptState.DisableDof);
		Add(FeatureId.Sharpen, "Sharpen / vignette", () => false);
		Add(FeatureId.Ssao, "SSAO", () => false);
		Add(FeatureId.HeatHaze, "Heat haze", () => OptState.DisableHeatHaze);
		Add(FeatureId.Footprints, "Footprints", () => OptState.DisableFootprints);
		Add(FeatureId.GrassDisp, "Grass displacement", () => OptState.DisableGrassDisp || OptState.DisableGrass);
		Add(FeatureId.WaterFoam, "Water foam", () => OptState.DisableWaterFoam);
		Add(FeatureId.TreeWind, "Tree foliage wind", () => OptState.DisableTreeWind);
		Add(FeatureId.VmShadows, "Viewmodel shadows", () => OptState.DisableVmShadows);
		Add(FeatureId.DamageOverlay, "Hurt overlay", () => OptState.DisableDamageOverlay);
		Add(FeatureId.Wetness, "Wet surfaces", () => OptState.DisableWetness);
		Add(FeatureId.AnimalPhysics, "Animal ragdolls", () => OptState.DisableAnimalPhysics);
		Add(FeatureId.LimitCorpses, "Limit ragdolls", () => OptState.LimitCorpses);
		Add(FeatureId.CheapTrees, "Tree billboards", () => OptState.CheapTrees);
		Add(FeatureId.Shake, "Camera shake", () => OptState.DisableShake);
		Add(FeatureId.CheapShaders, "Cheap shader LOD", () => OptState.CheapShaders);
	}

	private static void Add(FeatureId id, string name, Func<bool> on)
	{
		FeatureInfo featureInfo = new FeatureInfo
		{
			Id = id,
			Name = name,
			IsOn = on
		};
		All[id] = featureInfo;
		ById[(int)id] = featureInfo;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool PassOrSkip(FeatureId id, bool disable)
	{
		if (OptDark.Night || !disable)
		{
			return true;
		}
		ById[(int)id].Skips++;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool PassOrSkipPerf(FeatureId id, bool disable)
	{
		if (!disable)
		{
			return true;
		}
		ById[(int)id].Skips++;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Skip(FeatureId id)
	{
		FeatureInfo featureInfo = ById[(int)id];
		if (featureInfo.Skips < 32)
		{
			featureInfo.Skips++;
		}
	}

	public static void MarkHook(FeatureId id, bool ok, string error = null)
	{
		FeatureInfo featureInfo = ById[(int)id];
		featureInfo.HooksTotal++;
		if (ok)
		{
			featureInfo.HooksOk++;
		}
		else if (!string.IsNullOrEmpty(error))
		{
			featureInfo.Error = error;
		}
	}

	public static void MarkRuntimeOnly(FeatureId id)
	{
		FeatureInfo featureInfo = ById[(int)id];
		if (featureInfo.HooksTotal == 0)
		{
			featureInfo.HooksTotal = 1;
			featureInfo.HooksOk = 1;
		}
	}

	public static string Status(FeatureId id)
	{
		FeatureInfo featureInfo = ById[(int)id];
		if (featureInfo.HooksTotal > 0 && featureInfo.HooksOk < featureInfo.HooksTotal)
		{
			if (featureInfo.Error.Length <= 0)
			{
				return "ошибка хука";
			}
			return "ошибка";
		}
		if (!featureInfo.IsOn())
		{
			return "выкл";
		}
		if (featureInfo.Skips > 0)
		{
			return "работает";
		}
		return "вкл";
	}

	public static int CountLive()
	{
		int num = 0;
		for (int i = 0; i < ById.Length; i++)
		{
			FeatureInfo featureInfo = ById[i];
			if (featureInfo != null && featureInfo.IsOn() && featureInfo.Skips > 0)
			{
				num++;
			}
		}
		return num;
	}

	public static int CountFail()
	{
		int num = 0;
		for (int i = 0; i < ById.Length; i++)
		{
			FeatureInfo featureInfo = ById[i];
			if (featureInfo != null && featureInfo.HooksTotal > 0 && featureInfo.HooksOk < featureInfo.HooksTotal)
			{
				num++;
			}
		}
		return num;
	}

	public static int CountOn()
	{
		int num = 0;
		for (int i = 0; i < ById.Length; i++)
		{
			FeatureInfo featureInfo = ById[i];
			if (featureInfo != null && featureInfo.IsOn())
			{
				num++;
			}
		}
		return num;
	}
}
