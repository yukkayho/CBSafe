using System.Reflection;
using BepInEx.Configuration;
using CBSafe.Fps;

namespace CBSafe;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
public class Defines
{
	private static bool _loaded;

	public static ConfigEntry<bool> OptimizeGrass { get; set; }

	public static ConfigEntry<bool> OptimizeShadows { get; set; }

	public static ConfigEntry<bool> OptimizeRamCleanup { get; set; }

	public static ConfigEntry<bool> OptimizeGraphic { get; set; }

	public static ConfigEntry<bool> LeftRightHand { get; set; }

	public static ConfigEntry<bool> LongHand { get; set; }

	public static ConfigEntry<bool> FastAlt { get; set; }

	public static ConfigEntry<bool> NewHolo { get; set; }

	public static ConfigEntry<int> StretchIndex { get; set; }

	public static ConfigEntry<bool> GlovesSkinEnabled { get; set; }

	public static ConfigEntry<int> GlovesSkinIndex { get; set; }

	public static bool OptimizeGrassCached { get; private set; }

	public static bool OptimizeShadowsCached { get; private set; }

	public static bool OptimizeRamCleanupCached { get; private set; }

	public static bool OptimizeGraphicCached { get; private set; }

	public static void LoadSettings()
	{
		if (_loaded)
		{
			UpdateCache();
			return;
		}
		ConfigFile file = SharedConfig.File;
		OptimizeGrass = file.Bind("CBSafe", "Optimize Grass", defaultValue: false, "Оптимизация травы");
		OptimizeShadows = file.Bind("CBSafe", "Optimize Shadows", defaultValue: false, "Оптимизация теней");
		OptimizeRamCleanup = file.Bind("CBSafe", "Optimize RAM Cleanup", defaultValue: false, "Очистка RAM каждые 30 минут");
		OptimizeGraphic = file.Bind("CBSafe", "Optimize Graphic", defaultValue: false, "Оптимизация графики");
		LeftRightHand = file.Bind("HandSettings", "Left Right Hand", defaultValue: false, "Левая/Правая рука");
		LongHand = file.Bind("HandSettings", "Long Hand", defaultValue: false, "Длинная рука");
		FastAlt = file.Bind("HandSettings", "Fast Alt", defaultValue: false, "Быстрый ALT (client.headlerp 1000)");
		NewHolo = file.Bind("CBSafeHolo", "New Holo", defaultValue: false, "Новый холо-прицел (3D модель и текстуры)");
		OptConfig.Bind(file);
		StretchIndex = file.Bind("Enhancement", "Stretch Index", 0, "0=дефолт, 1=4:3, 2=5:4, 3=3:2, 4=16:10");
		GlovesSkinEnabled = file.Bind("Enhancement", "Gloves Skin Enabled", defaultValue: true, "Применять скин к тактическим перчаткам");
		GlovesSkinIndex = file.Bind("Enhancement", "Gloves Skin Index", 0, "0=Стелс, 1-6=Воркшоп");
		UpdateCache();
		_loaded = true;
		try
		{
			if (OptimizeRamCleanup.Value)
			{
				OptConfig.AutoMemory.Value = true;
			}
		}
		catch
		{
		}
	}

	public static void UpdateCache()
	{
		try
		{
			OptimizeGrassCached = OptimizeGrass.Value;
		}
		catch
		{
		}
		try
		{
			OptimizeShadowsCached = OptimizeShadows.Value;
		}
		catch
		{
		}
		try
		{
			OptimizeRamCleanupCached = OptimizeRamCleanup.Value;
		}
		catch
		{
		}
		try
		{
			OptimizeGraphicCached = OptimizeGraphic.Value;
		}
		catch
		{
		}
	}

	public static void SaveSettings()
	{
		UpdateCache();
		SharedConfig.MarkDirty();
	}
}
