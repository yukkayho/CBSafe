using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace CBSafe.Fps;

public static class OptConfig
{
	public static readonly List<ConfigEntry<bool>> Bools = new List<ConfigEntry<bool>>();

	public static bool MuteApply;

	public static ConfigEntry<bool> DisableGrass = null;

	public static ConfigEntry<bool> CoarseGrassLod = null;

	public static ConfigEntry<bool> DisableWeather = null;

	public static ConfigEntry<bool> DisableSecondaryParticles = null;

	public static ConfigEntry<bool> ReduceParticleCollision = null;

	public static ConfigEntry<bool> DisableParticleIk = null;

	public static ConfigEntry<bool> DisableDecals = null;

	public static ConfigEntry<bool> DisableGibs = null;

	public static ConfigEntry<bool> DisableWaterWaves = null;

	public static ConfigEntry<bool> DisableReflectionProbes = null;

	public static ConfigEntry<bool> DisablePlanarReflections = null;

	public static ConfigEntry<bool> DisableBloom = null;

	public static ConfigEntry<bool> DisableMotionBlur = null;

	public static ConfigEntry<bool> DisableSunRays = null;

	public static ConfigEntry<bool> DisableDof = null;

	public static ConfigEntry<bool> DisableSharpen = null;

	public static ConfigEntry<bool> DisableSsao = null;

	public static ConfigEntry<bool> DisableHeatHaze = null;

	public static ConfigEntry<bool> DisableFootprints = null;

	public static ConfigEntry<bool> DisableGrassDisp = null;

	public static ConfigEntry<bool> DisableWaterFoam = null;

	public static ConfigEntry<bool> DisableTreeWind = null;

	public static ConfigEntry<bool> DisableVmShadows = null;

	public static ConfigEntry<bool> DisableDamageOverlay = null;

	public static ConfigEntry<bool> DisableWetness = null;

	public static ConfigEntry<bool> DisableAnimalPhysics = null;

	public static ConfigEntry<bool> LimitCorpses = null;

	public static ConfigEntry<bool> CheapTrees = null;

	public static ConfigEntry<bool> DisableAa = null;

	public static ConfigEntry<bool> DisableLinearFog = null;

	public static ConfigEntry<bool> DisableExplosionBloom = null;

	public static ConfigEntry<bool> DisableHeavyPostProcess = null;

	public static ConfigEntry<bool> DisableContactShadows = null;

	public static ConfigEntry<bool> SmartShadows = null;

	public static ConfigEntry<bool> DisableShadows = null;

	public static ConfigEntry<bool> DisableImpostorShadows = null;

	public static ConfigEntry<bool> DisableSss = null;

	public static ConfigEntry<bool> DisableAtmosphere = null;

	public static ConfigEntry<bool> DisableVolumetricBeams = null;

	public static ConfigEntry<bool> CheapCamera = null;

	public static ConfigEntry<bool> CheapTerrain = null;

	public static ConfigEntry<bool> EnableInstancing = null;

	public static ConfigEntry<bool> SimplifyDistantAnims = null;

	public static ConfigEntry<bool> DisableCloth = null;

	public static ConfigEntry<bool> DisableCorpsePhysics = null;

	public static ConfigEntry<bool> BlockWorkshopSkins = null;

	public static ConfigEntry<bool> UnloadUnusedSkins = null;

	public static ConfigEntry<bool> SimplifySkinDeform = null;

	public static ConfigEntry<bool> AggressiveWorldLod = null;

	public static ConfigEntry<bool> AggressiveCulling = null;

	public static ConfigEntry<bool> DisableDecor = null;

	public static ConfigEntry<bool> DisableSky = null;

	public static ConfigEntry<bool> PointFilterTextures = null;

	public static ConfigEntry<bool> AutoMemory = null;

	public static ConfigEntry<bool> UnlockFps = null;

	public static ConfigEntry<bool> DisableShake = null;

	public static ConfigEntry<bool> CheapShaders = null;

	public static ConfigEntry<bool> MenuEnabled = null;

	public static ConfigEntry<int> MaxDynamicLights = null;

	public static ConfigEntry<int> TextureDownsample = null;

	public static ConfigEntry<float> DistantAnimRange = null;

	public static ConfigEntry<float> MemoryIntervalSec = null;

	public static void Bind(ConfigFile cfg)
	{
		Bools.Clear();
		MenuEnabled = cfg.Bind("Opt", "F8Menu", defaultValue: false, "Unused (F8 overlay removed).");
		DisableGrass = T(cfg, "DisableGrass", delegate(bool v)
		{
			OptState.DisableGrass = v;
		});
		CoarseGrassLod = T(cfg, "CoarseGrassLod", delegate(bool v)
		{
			OptState.CoarseGrassLod = v;
		});
		DisableWeather = T(cfg, "DisableWeather", delegate(bool v)
		{
			OptState.DisableWeather = v;
		});
		DisableSecondaryParticles = T(cfg, "DisableSecondaryParticles", delegate(bool v)
		{
			OptState.DisableSecondaryParticles = v;
		});
		ReduceParticleCollision = T(cfg, "ReduceParticleCollision", delegate(bool v)
		{
			OptState.ReduceParticleCollision = v;
		});
		DisableParticleIk = T(cfg, "DisableParticleIk", delegate(bool v)
		{
			OptState.DisableParticleIk = v;
		});
		DisableDecals = T(cfg, "DisableDecals", delegate(bool v)
		{
			OptState.DisableDecals = v;
		});
		DisableGibs = T(cfg, "DisableGibs", delegate(bool v)
		{
			OptState.DisableGibs = v;
		});
		DisableWaterWaves = T(cfg, "DisableWaterWaves", delegate(bool v)
		{
			OptState.DisableWaterWaves = v;
		});
		DisableReflectionProbes = T(cfg, "DisableReflectionProbes", delegate(bool v)
		{
			OptState.DisableReflectionProbes = v;
		});
		DisablePlanarReflections = T(cfg, "DisablePlanarReflections", delegate(bool v)
		{
			OptState.DisablePlanarReflections = v;
		});
		DisableBloom = T(cfg, "DisableBloom", delegate(bool v)
		{
			OptState.DisableBloom = v;
		});
		DisableMotionBlur = T(cfg, "DisableMotionBlur", delegate(bool v)
		{
			OptState.DisableMotionBlur = v;
		});
		DisableSunRays = T(cfg, "DisableSunRays", delegate(bool v)
		{
			OptState.DisableSunRays = v;
		});
		DisableDof = T(cfg, "DisableDof", delegate(bool v)
		{
			OptState.DisableDof = v;
		});
		DisableSharpen = T(cfg, "DisableSharpen", delegate(bool v)
		{
			OptState.DisableSharpen = v;
		});
		DisableSsao = T(cfg, "DisableSsao", delegate(bool v)
		{
			OptState.DisableSsao = v;
		});
		DisableHeatHaze = T(cfg, "DisableHeatHaze", delegate(bool v)
		{
			OptState.DisableHeatHaze = v;
		});
		DisableFootprints = T(cfg, "DisableFootprints", delegate(bool v)
		{
			OptState.DisableFootprints = v;
		});
		DisableGrassDisp = T(cfg, "DisableGrassDisp", delegate(bool v)
		{
			OptState.DisableGrassDisp = v;
		});
		DisableWaterFoam = T(cfg, "DisableWaterFoam", delegate(bool v)
		{
			OptState.DisableWaterFoam = v;
		});
		DisableTreeWind = T(cfg, "DisableTreeWind", delegate(bool v)
		{
			OptState.DisableTreeWind = v;
		});
		DisableVmShadows = T(cfg, "DisableVmShadows", delegate(bool v)
		{
			OptState.DisableVmShadows = v;
		});
		DisableDamageOverlay = T(cfg, "DisableDamageOverlay", delegate(bool v)
		{
			OptState.DisableDamageOverlay = v;
		});
		DisableWetness = T(cfg, "DisableWetness", delegate(bool v)
		{
			OptState.DisableWetness = v;
		});
		DisableAnimalPhysics = T(cfg, "DisableAnimalPhysics", delegate(bool v)
		{
			OptState.DisableAnimalPhysics = v;
		});
		LimitCorpses = T(cfg, "LimitCorpses", delegate(bool v)
		{
			OptState.LimitCorpses = v;
		});
		CheapTrees = T(cfg, "CheapTrees", delegate(bool v)
		{
			OptState.CheapTrees = v;
		});
		DisableAa = T(cfg, "DisableAa", delegate(bool v)
		{
			OptState.DisableAa = v;
		});
		DisableLinearFog = T(cfg, "DisableLinearFog", delegate(bool v)
		{
			OptState.DisableLinearFog = v;
		});
		DisableExplosionBloom = T(cfg, "DisableExplosionBloom", delegate(bool v)
		{
			OptState.DisableExplosionBloom = v;
		});
		DisableHeavyPostProcess = T(cfg, "DisableHeavyPostProcess", delegate(bool v)
		{
			OptState.DisableHeavyPostProcess = v;
		});
		DisableContactShadows = T(cfg, "DisableContactShadows", delegate(bool v)
		{
			OptState.DisableContactShadows = v;
		});
		SmartShadows = T(cfg, "SmartShadows", delegate(bool v)
		{
			OptState.SmartShadows = v;
		});
		DisableShadows = T(cfg, "DisableShadows", delegate(bool v)
		{
			OptState.DisableShadows = v;
		});
		DisableImpostorShadows = T(cfg, "DisableImpostorShadows", delegate(bool v)
		{
			OptState.DisableImpostorShadows = v;
		});
		DisableSss = T(cfg, "DisableSss", delegate(bool v)
		{
			OptState.DisableSss = v;
		});
		DisableAtmosphere = T(cfg, "DisableAtmosphere", delegate(bool v)
		{
			OptState.DisableAtmosphere = v;
		});
		DisableVolumetricBeams = T(cfg, "DisableVolumetricBeams", delegate(bool v)
		{
			OptState.DisableVolumetricBeams = v;
		});
		CheapCamera = T(cfg, "CheapCamera", delegate(bool v)
		{
			OptState.CheapCamera = v;
		});
		CheapTerrain = T(cfg, "CheapTerrain", delegate(bool v)
		{
			OptState.CheapTerrain = v;
		});
		EnableInstancing = T(cfg, "EnableInstancing", delegate(bool v)
		{
			OptState.EnableInstancing = v;
		});
		SimplifyDistantAnims = T(cfg, "SimplifyDistantAnims", delegate(bool v)
		{
			OptState.SimplifyDistantAnims = v;
		});
		DisableCloth = T(cfg, "DisableCloth", delegate(bool v)
		{
			OptState.DisableCloth = v;
		});
		DisableCorpsePhysics = T(cfg, "DisableCorpsePhysics", delegate(bool v)
		{
			OptState.DisableCorpsePhysics = v;
		});
		BlockWorkshopSkins = T(cfg, "BlockWorkshopSkins", delegate(bool v)
		{
			OptState.BlockWorkshopSkins = v;
		});
		UnloadUnusedSkins = T(cfg, "UnloadUnusedSkins", delegate(bool v)
		{
			OptState.UnloadUnusedSkins = v;
		});
		SimplifySkinDeform = T(cfg, "SimplifySkinDeform", delegate(bool v)
		{
			OptState.SimplifySkinDeform = v;
		});
		AggressiveWorldLod = T(cfg, "AggressiveWorldLod", delegate(bool v)
		{
			OptState.AggressiveWorldLod = v;
		});
		AggressiveCulling = T(cfg, "AggressiveCulling", delegate(bool v)
		{
			OptState.AggressiveCulling = v;
		});
		DisableDecor = T(cfg, "DisableDecor", delegate(bool v)
		{
			OptState.DisableDecor = v;
		});
		DisableSky = T(cfg, "DisableSky", delegate(bool v)
		{
			OptState.DisableSky = v;
		});
		PointFilterTextures = T(cfg, "PointFilterTextures", delegate(bool v)
		{
			OptState.PointFilterTextures = v;
		});
		AutoMemory = T(cfg, "AutoMemory", delegate(bool v)
		{
			OptState.AutoMemory = v;
		});
		UnlockFps = T(cfg, "UnlockFps", delegate(bool v)
		{
			OptState.UnlockFps = v;
		});
		DisableShake = T(cfg, "DisableShake", delegate(bool v)
		{
			OptState.DisableShake = v;
		});
		CheapShaders = T(cfg, "CheapShaders", delegate(bool v)
		{
			OptState.CheapShaders = v;
		});
		ConfigEntry<bool> configEntry = cfg.Bind("Opt", "NightDarkFix", defaultValue: false);
		if (!configEntry.Value)
		{
			DisableSsao.Value = false;
			configEntry.Value = true;
		}
		ConfigEntry<bool> configEntry2 = cfg.Bind("Opt", "NightKeepDark", defaultValue: false);
		if (!configEntry2.Value)
		{
			DisableSsao.Value = false;
			DisableHeavyPostProcess.Value = false;
			DisableContactShadows.Value = false;
			DisableBloom.Value = false;
			configEntry2.Value = true;
		}
		ConfigEntry<bool> configEntry3 = cfg.Bind("Opt", "WaterDefaultsV2", defaultValue: false);
		if (!configEntry3.Value)
		{
			DisableReflectionProbes.Value = false;
			DisablePlanarReflections.Value = false;
			configEntry3.Value = true;
			SharedConfig.MarkDirty();
		}
		MaxDynamicLights = cfg.Bind("Opt", "MaxDynamicLights", 8);
		OptState.MaxDynamicLights = Math.Clamp(MaxDynamicLights.Value, 0, 8);
		MaxDynamicLights.SettingChanged += delegate
		{
			OptState.MaxDynamicLights = Math.Clamp(MaxDynamicLights.Value, 0, 8);
			OptRuntime.ForceApply = true;
		};
		TextureDownsample = cfg.Bind("Opt", "TextureDownsample", 0);
		OptState.TextureDownsample = Math.Clamp(TextureDownsample.Value, 0, 3);
		TextureDownsample.SettingChanged += delegate
		{
			OptState.TextureDownsample = Math.Clamp(TextureDownsample.Value, 0, 3);
			OptRuntime.ForceApply = true;
		};
		DistantAnimRange = cfg.Bind("Opt", "DistantAnimRange", 25f);
		OptState.DistantAnimRange = DistantAnimRange.Value;
		DistantAnimRange.SettingChanged += delegate
		{
			OptState.DistantAnimRange = DistantAnimRange.Value;
			OptRuntime.ForceApply = true;
		};
		MemoryIntervalSec = cfg.Bind("Opt", "MemoryIntervalSec", 1800f);
		OptState.MemoryIntervalSec = MemoryIntervalSec.Value;
		MemoryIntervalSec.SettingChanged += delegate
		{
			OptState.MemoryIntervalSec = MemoryIntervalSec.Value;
			OptRuntime.ForceApply = true;
		};
	}

	public static void AllOff()
	{
		MuteApply = true;
		try
		{
			foreach (ConfigEntry<bool> @bool in Bools)
			{
				@bool.Value = false;
			}
			MaxDynamicLights.Value = 8;
			TextureDownsample.Value = 0;
			DistantAnimRange.Value = 25f;
			MemoryIntervalSec.Value = 1800f;
		}
		finally
		{
			MuteApply = false;
			OptRuntime.ForceApply = true;
		}
		OptState.ActivePreset = PresetId.Custom;
	}

	private static ConfigEntry<bool> T(ConfigFile cfg, string key, Action<bool> apply)
	{
		ConfigEntry<bool> e = cfg.Bind("Opt", key, defaultValue: false);
		apply(e.Value);
		e.SettingChanged += delegate
		{
			apply(e.Value);
			if (!MuteApply)
			{
				OptRuntime.ForceApply = true;
			}
		};
		Bools.Add(e);
		return e;
	}

	private static void ForceOn(ConfigEntry<bool> e, Action<bool> apply)
	{
		if (e == null)
		{
			return;
		}
		try
		{
			if (!e.Value)
			{
				e.Value = true;
			}
			apply(obj: true);
		}
		catch
		{
			apply(obj: true);
		}
		Bools.Remove(e);
	}

	private static void KillRemoved(ConfigEntry<bool> e, Action<bool> apply)
	{
		if (e == null)
		{
			return;
		}
		try
		{
			if (e.Value)
			{
				e.Value = false;
			}
			apply(obj: false);
		}
		catch
		{
			apply(obj: false);
		}
	}
}
