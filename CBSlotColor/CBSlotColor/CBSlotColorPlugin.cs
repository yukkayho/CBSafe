using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CBSlotColor;

[BepInPlugin("net.rust.cbsafe.slotcolor", "CBSlotColor", "1.0.8")]
[BepInDependency("net.rust.cbsafe", BepInDependency.DependencyFlags.HardDependency)]
public class CBSlotColorPlugin : BasePlugin
{
	public static Color ParsedSlotColor = new Color(0.5f, 0.16f, 1f, 0.71f);

	public static Color ParsedEmptySlotColor = new Color(0.33f, 0.08f, 0.71f, 0.45f);

	public static Color ParsedSelectedSlotColor = new Color(0.67f, 0.27f, 1f, 0.9f);

	private static float _lastRefreshTime;

	private static bool _started;

	private static bool _restoringAlpha;

	public static ManualLogSource PluginLog { get; private set; }

	public static ConfigFile CustomConfig { get; private set; }

	public static ConfigEntry<string> SlotColor { get; private set; }

	public static ConfigEntry<string> EmptySlotColor { get; private set; }

	public static ConfigEntry<string> SelectedSlotColor { get; private set; }

	public static ConfigEntry<bool> SlotColorEnabled { get; private set; }

	public override void Load()
	{
		PluginLog = base.Log;
		Initialize(base.Log);
	}

	public static void Initialize(ManualLogSource log)
	{
		if (_started)
		{
			return;
		}
		PluginLog = log ?? PluginLog;
		PluginLog?.LogInfo("[CBSlotColor] Embedded init (v1.0.8, shared CBSafe.cfg)...");
		try
		{
			CustomConfig = ResolveSharedConfig() ?? new ConfigFile(Path.Combine(Paths.ConfigPath, "CBSafe.cfg"), saveOnInit: true);
			try
			{
				CustomConfig.SaveOnConfigSet = false;
			}
			catch
			{
			}
			SlotColor = CustomConfig.Bind("Colors", "SlotColor", "97,0,255,180", "Цвет фона слотов с предметами в инвентаре и ящиках (RGBA: R,G,B,A)");
			EmptySlotColor = CustomConfig.Bind("Colors", "EmptySlotColor", "75,15,160,110", "Цвет фона пустых слотов в инвентаре и ящиках (RGBA: R,G,B,A)");
			SelectedSlotColor = CustomConfig.Bind("Colors", "SelectedSlotColor", "140,50,255,220", "Цвет фона выбранного слота (RGBA: R,G,B,A)");
			SlotColorEnabled = CustomConfig.Bind("Colors", "SlotColorEnabled", defaultValue: true, "Включить кастомные цвета фона слотов инвентаря и ящиков");
			UpdateColors();
			SlotColor.SettingChanged += OnSlotSettingChanged;
			EmptySlotColor.SettingChanged += OnSlotSettingChanged;
			SelectedSlotColor.SettingChanged += OnSlotSettingChanged;
			SlotColorEnabled.SettingChanged += OnSlotSettingChanged;
			try
			{
				new Harmony("net.rust.cbsafe.slotcolor").PatchAll(typeof(CBSlotColorPlugin).Assembly);
			}
			catch (Exception ex)
			{
				PluginLog?.LogWarning("[CBSlotColor] Harmony: " + ex.Message);
			}
			_started = true;
			FullBoosterTune.Apply("startup");
			PluginLog?.LogInfo("[CBSlotColor] Slot colors bound to CBSafe.cfg");
		}
		catch (Exception ex2)
		{
			PluginLog?.LogError("[CBSlotColor] Init error: " + ex2);
		}
	}

	private static ConfigFile ResolveSharedConfig()
	{
		try
		{
			Type type = AccessTools.TypeByName("CBSafe.SharedConfig");
			if (type == null)
			{
				return null;
			}
			return type.GetProperty("File", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null) as ConfigFile;
		}
		catch
		{
			return null;
		}
	}

	private static void OnSlotSettingChanged(object sender, EventArgs e)
	{
		PreserveAlphaOnPickerWrite();
		UpdateColors();
		PersistNow();
		ThrottleRefreshOpenIcons();
	}

	public static void ApplyToIcon(ItemIcon icon)
	{
		if (!((UnityEngine.Object)(object)icon == null) && !(((Il2CppObjectBase)(object)icon).Pointer == IntPtr.Zero) && !((Il2CppObjectBase)(object)icon).WasCollected && SlotColorEnabled != null && SlotColorEnabled.Value)
		{
			icon.backgroundColor = ParsedSlotColor;
			icon.selectedBackgroundColor = ParsedSelectedSlotColor;
			icon.unoccupiedColor = ParsedEmptySlotColor;
			icon.unoccupiedAlpha = ParsedEmptySlotColor.a;
		}
	}

	public static void UpdateColors()
	{
		if (TryParseColor(SlotColor?.Value, out var color))
		{
			ParsedSlotColor = color;
		}
		if (TryParseColor(EmptySlotColor?.Value, out var color2))
		{
			ParsedEmptySlotColor = color2;
		}
		if (TryParseColor(SelectedSlotColor?.Value, out var color3))
		{
			ParsedSelectedSlotColor = color3;
		}
	}

	public static void PersistNow()
	{
		try
		{
			CustomConfig?.Save();
		}
		catch
		{
		}
	}

	public static void ThrottleRefreshOpenIcons()
	{
		if (!(Time.unscaledTime - _lastRefreshTime < 0.08f))
		{
			_lastRefreshTime = Time.unscaledTime;
			RefreshAllOpenIcons();
		}
	}

	public static void RefreshAllOpenIcons()
	{
		try
		{
			Il2CppArrayBase<ItemIcon> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<ItemIcon>();
			if (il2CppArrayBase != null)
			{
				for (int i = 0; i < il2CppArrayBase.Length; i++)
				{
					ApplyToIcon(il2CppArrayBase[i]);
				}
			}
		}
		catch
		{
		}
	}

	public static bool TryParseColor(string val, out Color color)
	{
		color = Color.white;
		if (string.IsNullOrEmpty(val))
		{
			return false;
		}
		string[] array = val.Split(',');
		if (array.Length < 3)
		{
			return false;
		}
		if (byte.TryParse(array[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) && byte.TryParse(array[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2) && byte.TryParse(array[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result3))
		{
			byte result4 = byte.MaxValue;
			if (array.Length >= 4)
			{
				byte.TryParse(array[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result4);
			}
			color = new Color((float)(int)result / 255f, (float)(int)result2 / 255f, (float)(int)result3 / 255f, (float)(int)result4 / 255f);
			return true;
		}
		return false;
	}

	public static string ColorToString(Color c)
	{
		int num = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
		int num2 = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
		int num3 = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
		int num4 = Mathf.Clamp(Mathf.RoundToInt(c.a * 255f), 0, 255);
		return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", num, num2, num3, num4);
	}

	internal static void PreserveAlphaOnPickerWrite()
	{
		RestoreAlpha(SlotColor, ParsedSlotColor);
		RestoreAlpha(EmptySlotColor, ParsedEmptySlotColor);
		RestoreAlpha(SelectedSlotColor, ParsedSelectedSlotColor);
	}

	private static void RestoreAlpha(ConfigEntry<string> entry, Color previous)
	{
		if (_restoringAlpha || entry == null || string.IsNullOrEmpty(entry.Value) || !TryParseColor(entry.Value, out var color))
		{
			return;
		}
		int num = Mathf.RoundToInt(previous.a * 255f);
		if (Mathf.RoundToInt(color.a * 255f) != 255 || num <= 0 || num >= 255)
		{
			return;
		}
		color.a = previous.a;
		string text = ColorToString(color);
		if (!(entry.Value != text))
		{
			return;
		}
		_restoringAlpha = true;
		try
		{
			entry.Value = text;
		}
		finally
		{
			_restoringAlpha = false;
		}
	}
}
