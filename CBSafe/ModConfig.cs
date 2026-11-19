using BepInEx.Configuration;

namespace CBSafe;

public static class ModConfig
{
	private static ConfigFile _file;

	public static ConfigEntry<bool> OptimizeShadows;

	public static ConfigEntry<bool> OptimizeGrass;

	public static ConfigEntry<bool> OptimizeWeather;

	public static ConfigEntry<bool> SuperPotatoMode;

	public static ConfigEntry<bool> GlassAkEnabled;

	public static ConfigEntry<string> GlassColor;

	public static ConfigEntry<int> GlassColorIndex;

	public static ConfigEntry<bool> RgbEnabled;

	public static readonly string[] ColorNames = new string[17]
	{
		"Ледяной (Официальный)", "Рубин (Красный)", "Сапфир (Синий)", "Изумруд (Зелёный)", "Аметист (Фиолетовый)", "Золото (Жёлтый)", "Розовый (Неон)", "Алмаз (Белый кристалл)", "Тонированный (Тёмный)", "Янтарь (Оранжевый)",
		"Аквамарин (Бирюзовый)", "Лайм (Ярко-зелёный)", "Кровавый (Blood)", "Глубокий фиолет", "Радужный (RGB)", "RGB (анимация цвета)", "Чистое стекло (50%)"
	};

	public static readonly string[] ColorKeys = new string[17]
	{
		"ice", "red", "blue", "green", "purple", "gold", "pink", "diamond", "smoked", "amber",
		"cyan", "lime", "blood", "violet", "rainbow", "rgb", "clear"
	};

	public static string CurrentColorKey
	{
		get
		{
			if (RgbEnabled == null || !RgbEnabled.Value)
			{
				if (GlassColor == null)
				{
					return "ice";
				}
				return GlassColor.Value;
			}
			return "rgb";
		}
	}

	public static void Init(ConfigFile config)
	{
		_file = config;
		OptimizeShadows = _file.Bind("Optimization", "OptimizeShadows", defaultValue: false, "Отключение всех теней");
		OptimizeGrass = _file.Bind("Optimization", "OptimizeGrass", defaultValue: false, "Отключение травы");
		OptimizeWeather = _file.Bind("Optimization", "OptimizeWeather", defaultValue: false, "Отключение погоды");
		SuperPotatoMode = _file.Bind("Optimization", "SuperPotatoMode", defaultValue: false, "Super Potato Mode");
		GlassAkEnabled = _file.Bind("GlassAK", "GlassAkEnabled", defaultValue: true, "Включить стеклянный калаш");
		GlassColor = _file.Bind("GlassAK", "GlassColor", "ice", "Выбранный цвет стеклянного калаша");
		GlassColorIndex = _file.Bind("GlassAK", "GlassColorIndex", 0, "Индекс цвета в дропдауне");
		RgbEnabled = _file.Bind("GlassAK", "RgbEnabled", defaultValue: false, "RGB-анимация калаша (отдельная функция, имеет приоритет над палитрой)");
	}

	public static void Save()
	{
		try
		{
			_file?.Save();
		}
		catch
		{
		}
	}
}
