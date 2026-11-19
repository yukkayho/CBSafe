using System;
using System.Linq;
using BepInEx.Configuration;

namespace CBSafe.Fps;

internal static class OptPresets
{
	private static bool[] _bools;

	private static int _lights = 8;

	private static int _mips;

	private static float _range = 25f;

	private static float _mem = 180f;

	public static string Name => OptState.ActivePreset switch
	{
		PresetId.User => "Свой", 
		PresetId.Combat => "Боевой", 
		PresetId.Simple => "Боевой", 
		PresetId.Potato => "Слабый ПК", 
		PresetId.SuperPotato => "Минимум", 
		PresetId.Minecraft => "Минимум", 
		PresetId.PotatoPc => "Минимум", 
		_ => "Свой", 
	};

	public static void Apply(PresetId id)
	{
		if (id == PresetId.User)
		{
			RestoreUser();
			return;
		}
		if (_bools == null)
		{
			Capture();
		}
		OptState.ActivePreset = id;
		OptConfig.MuteApply = true;
		try
		{
			foreach (ConfigEntry<bool> @bool in OptConfig.Bools)
			{
				@bool.Value = false;
			}
			OptConfig.MaxDynamicLights.Value = 8;
			OptConfig.TextureDownsample.Value = 0;
			OptConfig.DistantAnimRange.Value = 25f;
			Fill(id);
		}
		finally
		{
			OptConfig.MuteApply = false;
			OptRuntime.ForceApply = true;
		}
	}

	private static void Capture()
	{
		_bools = OptConfig.Bools.Select((ConfigEntry<bool> b) => b.Value).ToArray();
		_lights = OptConfig.MaxDynamicLights.Value;
		_mips = OptConfig.TextureDownsample.Value;
		_range = OptConfig.DistantAnimRange.Value;
		_mem = OptConfig.MemoryIntervalSec.Value;
	}

	private static void RestoreUser()
	{
		if (_bools == null)
		{
			OptConfig.AllOff();
			OptState.ActivePreset = PresetId.User;
			return;
		}
		OptConfig.MuteApply = true;
		try
		{
			int num = Math.Min(_bools.Length, OptConfig.Bools.Count);
			for (int i = 0; i < num; i++)
			{
				OptConfig.Bools[i].Value = _bools[i];
			}
			OptConfig.MaxDynamicLights.Value = _lights;
			OptConfig.TextureDownsample.Value = _mips;
			OptConfig.DistantAnimRange.Value = _range;
			OptConfig.MemoryIntervalSec.Value = _mem;
		}
		finally
		{
			OptConfig.MuteApply = false;
			OptRuntime.ForceApply = true;
		}
		OptState.ActivePreset = PresetId.User;
	}

	private static void On(ConfigEntry<bool> e)
	{
		e.Value = true;
	}

	private static void EnableEveryOptimization()
	{
		foreach (ConfigEntry<bool> @bool in OptConfig.Bools)
		{
			@bool.Value = true;
		}
	}

	private static void CombatFx()
	{
		On(OptConfig.DisableSecondaryParticles);
		On(OptConfig.DisableParticleIk);
		On(OptConfig.DisableDecals);
		On(OptConfig.DisableGibs);
		On(OptConfig.DisableCloth);
		On(OptConfig.DisableWeather);
		On(OptConfig.DisableSunRays);
		On(OptConfig.DisableDof);
		On(OptConfig.DisableHeatHaze);
		On(OptConfig.DisableGrassDisp);
		On(OptConfig.DisableWaterFoam);
		On(OptConfig.DisableTreeWind);
		On(OptConfig.DisableVmShadows);
		On(OptConfig.DisableDamageOverlay);
		On(OptConfig.DisableWetness);
		On(OptConfig.DisableAnimalPhysics);
		On(OptConfig.LimitCorpses);
		On(OptConfig.CheapTrees);
		On(OptConfig.DisableVolumetricBeams);
		On(OptConfig.CheapShaders);
		On(OptConfig.DisableShake);
		On(OptConfig.DisableImpostorShadows);
		On(OptConfig.CoarseGrassLod);
		On(OptConfig.DisableWaterWaves);
		On(OptConfig.AggressiveCulling);
		On(OptConfig.UnlockFps);
		On(OptConfig.DisableHeavyPostProcess);
		On(OptConfig.DisableAtmosphere);
	}

	private static void Fill(PresetId id)
	{
		switch (id)
		{
		case PresetId.Combat:
			CombatFx();
			On(OptConfig.SmartShadows);
			On(OptConfig.DisableDecor);
			On(OptConfig.SimplifyDistantAnims);
			On(OptConfig.DisableCorpsePhysics);
			On(OptConfig.EnableInstancing);
			OptConfig.MaxDynamicLights.Value = 2;
			OptConfig.DistantAnimRange.Value = 28f;
			break;
		case PresetId.Simple:
			CombatFx();
			On(OptConfig.CoarseGrassLod);
			On(OptConfig.DisableWaterWaves);
			On(OptConfig.SmartShadows);
			On(OptConfig.DisableDecor);
			OptConfig.MaxDynamicLights.Value = 4;
			break;
		case PresetId.Potato:
			CombatFx();
			On(OptConfig.DisableGrass);
			On(OptConfig.CoarseGrassLod);
			On(OptConfig.DisableWaterWaves);
			On(OptConfig.DisableShadows);
			On(OptConfig.DisableImpostorShadows);
			On(OptConfig.CheapCamera);
			On(OptConfig.CheapTerrain);
			On(OptConfig.DisableDecor);
			On(OptConfig.SimplifyDistantAnims);
			On(OptConfig.DisableCorpsePhysics);
			On(OptConfig.EnableInstancing);
			OptConfig.MaxDynamicLights.Value = 2;
			OptConfig.TextureDownsample.Value = 1;
			break;
		case PresetId.SuperPotato:
			EnableEveryOptimization();
			OptConfig.MaxDynamicLights.Value = 1;
			OptConfig.TextureDownsample.Value = 2;
			break;
		case PresetId.Minecraft:
			CombatFx();
			On(OptConfig.DisableGrass);
			On(OptConfig.DisableWaterWaves);
			On(OptConfig.DisableShadows);
			On(OptConfig.DisableImpostorShadows);
			On(OptConfig.CheapCamera);
			On(OptConfig.PointFilterTextures);
			On(OptConfig.DisableDecor);
			On(OptConfig.EnableInstancing);
			OptConfig.TextureDownsample.Value = 1;
			OptConfig.MaxDynamicLights.Value = 1;
			break;
		case PresetId.PotatoPc:
			CombatFx();
			On(OptConfig.DisableGrass);
			On(OptConfig.CoarseGrassLod);
			On(OptConfig.DisableWaterWaves);
			On(OptConfig.DisableShadows);
			On(OptConfig.DisableImpostorShadows);
			On(OptConfig.CheapCamera);
			On(OptConfig.CheapTerrain);
			On(OptConfig.PointFilterTextures);
			On(OptConfig.DisableDecor);
			On(OptConfig.SimplifyDistantAnims);
			On(OptConfig.DisableCorpsePhysics);
			On(OptConfig.EnableInstancing);
			On(OptConfig.AggressiveWorldLod);
			On(OptConfig.AggressiveCulling);
			OptConfig.MaxDynamicLights.Value = 1;
			OptConfig.TextureDownsample.Value = 1;
			break;
		}
	}
}
