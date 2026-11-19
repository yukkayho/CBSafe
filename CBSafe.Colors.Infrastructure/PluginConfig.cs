using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using CBSafe.Core;
using UnityEngine;

namespace CBSafe.Colors.Infrastructure;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
internal static class PluginConfig
{
	public static ConfigEntry<string> ReticleColor;

	public static ConfigEntry<string> CrosshairColor;

	public static ConfigEntry<string> HpBarColor;

	public static ConfigEntry<string> WaterBarColor;

	public static ConfigEntry<string> FoodBarColor;

	public static ConfigEntry<string> TeamNameColor;

	public static ConfigEntry<string> PlayerNameColor;

	public static ConfigEntry<string> AllyNameColor;

	public static ConfigEntry<string> EnemyNameColor;

	public static ConfigEntry<string> BloodColor;

	public static ConfigEntry<bool> ReticleColorEnabled;

	public static ConfigEntry<bool> CrosshairColorEnabled;

	public static ConfigEntry<bool> HpColorEnabled;

	public static ConfigEntry<bool> WaterColorEnabled;

	public static ConfigEntry<bool> FoodColorEnabled;

	public static ConfigEntry<bool> TeamNameColorEnabled;

	public static ConfigEntry<bool> PlayerNameColorEnabled;

	public static ConfigEntry<bool> AllyNameColorEnabled;

	public static ConfigEntry<bool> EnemyNameColorEnabled;

	public static ConfigEntry<bool> BloodColorEnabled;

	public static ConfigEntry<string> GatherCircleColor;

	public static ConfigEntry<bool> GatherCircleColorEnabled;

	public static ConfigEntry<bool> DlcUnlockEnabled;

	private static bool _initialized;

	public static ConfigEntry<string> Template;

	public const string TemplateCustom = "Custom";

	public const float TreeCrosshairDarkenFactor = 0.62f;

	private static Dictionary<ConfigEntry<string>, (string raw, Color parsed)> _colorCache = new Dictionary<ConfigEntry<string>, (string, Color)>();

	public static bool IsDebugEnabled => false;

	public static void Initialize(ConfigFile cfg)
	{
		if (!_initialized)
		{
			_initialized = true;
			try
			{
				SharedConfig.EnsureDeferredSave();
			}
			catch
			{
			}
			ReticleColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_ReticleColor, "0,255,0,255", "Р¦РІРµС‚ РїСЂРёС†РµР»Р°");
			CrosshairColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_CrosshairColor, "255,0,0,255", "Р¦РІРµС‚ РєСЂРµСЃС‚РёРєР°");
			HpBarColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_HpBarColor, "0,200,0,255", "Р¦РІРµС‚ Р±Р°СЂР° HP");
			WaterBarColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_WaterBarColor, "0,100,255,255", "Р¦РІРµС‚ Р±Р°СЂР° РІРѕРґС‹");
			FoodBarColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_FoodBarColor, "200,120,0,255", "Р¦РІРµС‚ Р±Р°СЂР° РµРґС‹");
			TeamNameColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_TeamNameColor, "163,233,49,255", "Р¦РІРµС‚ РєРѕРјР°РЅРґС‹");
			PlayerNameColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_PlayerNameColor, "196,196,196,255", "Р¦РІРµС‚ РёРіСЂРѕРєРѕРІ");
			AllyNameColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_AllyNameColor, "13,152,247,255", "Р¦РІРµС‚ СЃРѕСЋР·РЅРёРєРѕРІ");
			EnemyNameColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_EnemyNameColor, "178,53,0,255", "Р¦РІРµС‚ РїСЂРѕС‚РёРІРЅРёРєРѕРІ");
			BloodColor = cfg.Bind(LocStrings.Cfg_Colors, LocStrings.Key_Cfg_BloodColor, "255,0,0,255", "Р¦РІРµС‚ РєСЂРѕРІРё");
			ReticleColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "ReticleColorEnabled", defaultValue: true, "Р’РєР»СЋС‡РёС‚СЊ");
			CrosshairColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "CrosshairColorEnabled", defaultValue: true, "Р’РєР»СЋС‡РёС‚СЊ");
			HpColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "HpColorEnabled", defaultValue: true, "Р’РєР»СЋС‡РёС‚СЊ");
			WaterColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "WaterColorEnabled", defaultValue: true, "Р’РєР»СЋС‡РёС‚СЊ");
			FoodColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "FoodColorEnabled", defaultValue: true, "Р’РєР»СЋС‡РёС‚СЊ");
			TeamNameColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "TeamNameColorEnabled", defaultValue: true, "Р’РєР»СЋС‡РёС‚СЊ");
			PlayerNameColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "PlayerNameColorEnabled", defaultValue: true, "Р’РєР»СЋС‡РёС‚СЊ");
			AllyNameColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "AllyNameColorEnabled", defaultValue: true, "Р’РєР»СЋС‡РёС‚СЊ");
			BloodColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "BloodColorEnabled", defaultValue: true, "Р’РєР»СЋС‡РёС‚СЊ С†РІРµС‚ РєСЂРѕРІРё");
			GatherCircleColor = cfg.Bind(LocStrings.Cfg_Colors, "GatherCircleColor", "0,0,0,255", "Цвет кружка сбора");
			GatherCircleColorEnabled = cfg.Bind(LocStrings.Cfg_Colors, "GatherCircleColorEnabled", defaultValue: true, "Включить цвет кружка сбора");
			DlcUnlockEnabled = cfg.Bind("DLC", "DlcUnlockEnabled", defaultValue: true, "Все DLC, скины и предметы инвентаря считаются купленными (сервер может выключить удалённо командой dlccheck.receive)");
		}
	}

	public static bool IsCustomActive()
	{
		if (Template != null && Template.Value == "Custom")
		{
			return true;
		}
		ConfigEntry<bool> reticleColorEnabled = ReticleColorEnabled;
		if (reticleColorEnabled == null || !reticleColorEnabled.Value)
		{
			ConfigEntry<bool> crosshairColorEnabled = CrosshairColorEnabled;
			if (crosshairColorEnabled == null || !crosshairColorEnabled.Value)
			{
				ConfigEntry<bool> hpColorEnabled = HpColorEnabled;
				if (hpColorEnabled == null || !hpColorEnabled.Value)
				{
					ConfigEntry<bool> waterColorEnabled = WaterColorEnabled;
					if (waterColorEnabled == null || !waterColorEnabled.Value)
					{
						ConfigEntry<bool> foodColorEnabled = FoodColorEnabled;
						if (foodColorEnabled == null || !foodColorEnabled.Value)
						{
							ConfigEntry<bool> teamNameColorEnabled = TeamNameColorEnabled;
							if (teamNameColorEnabled == null || !teamNameColorEnabled.Value)
							{
								ConfigEntry<bool> playerNameColorEnabled = PlayerNameColorEnabled;
								if (playerNameColorEnabled == null || !playerNameColorEnabled.Value)
								{
									ConfigEntry<bool> allyNameColorEnabled = AllyNameColorEnabled;
									if (allyNameColorEnabled == null || !allyNameColorEnabled.Value)
									{
										return EnemyNameColorEnabled?.Value ?? false;
									}
								}
							}
						}
					}
				}
			}
		}
		return true;
	}

	public static void ReloadFromSharedConfig()
	{
	}

	public static void SyncFromSharedConfig()
	{
	}

	public static bool IsReticleCustomizationActive()
	{
		return IsCustomActive();
	}

	public static Color DarkenTreeCrosshairColor(Color color)
	{
		float num = 0.62f;
		return new Color(Mathf.Clamp01(color.r * num), Mathf.Clamp01(color.g * num), Mathf.Clamp01(color.b * num), color.a);
	}

	public static bool TryResolveReticle(out Color color)
	{
		return TryResolve(ReticleColor, ReticleColorEnabled, out color);
	}

	public static bool TryResolveCrosshair(out Color color)
	{
		return TryResolve(CrosshairColor, CrosshairColorEnabled, out color);
	}

	public static bool TryResolveTreeCrosshair(out Color color)
	{
		if (!TryResolveCrosshair(out color))
		{
			return false;
		}
		color = DarkenTreeCrosshairColor(color);
		return true;
	}

	public static bool TryResolveHpBar(out Color color)
	{
		return TryResolve(HpBarColor, HpColorEnabled, out color);
	}

	public static bool TryResolveWaterBar(out Color color)
	{
		return TryResolve(WaterBarColor, WaterColorEnabled, out color);
	}

	public static bool TryResolveFoodBar(out Color color)
	{
		return TryResolve(FoodBarColor, FoodColorEnabled, out color);
	}

	public static bool TryResolveTeamName(out Color color)
	{
		return TryResolve(TeamNameColor, TeamNameColorEnabled, out color);
	}

	public static bool TryResolvePlayerName(out Color color)
	{
		return TryResolve(PlayerNameColor, PlayerNameColorEnabled, out color);
	}

	public static bool TryResolveAllyName(out Color color)
	{
		return TryResolve(AllyNameColor, AllyNameColorEnabled, out color);
	}

	public static bool TryResolveEnemyName(out Color color)
	{
		return TryResolve(EnemyNameColor, EnemyNameColorEnabled, out color);
	}

	public static bool TryResolveBloodColor(out Color color)
	{
		return TryResolve(BloodColor, BloodColorEnabled, out color);
	}

	public static bool TryResolveGatherCircleColor(out Color color)
	{
		return TryResolve(GatherCircleColor, GatherCircleColorEnabled, out color);
	}

	private static bool TryResolve(ConfigEntry<string> entry, ConfigEntry<bool> enabled, out Color color)
	{
		color = Color.white;
		if (entry == null || enabled == null || !enabled.Value)
		{
			return false;
		}
		string value = entry.Value;
		if (string.IsNullOrEmpty(value))
		{
			return false;
		}
		if (_colorCache.TryGetValue(entry, out var value2) && value2.Item1 == value)
		{
			color = value2.Item2;
			return true;
		}
		string[] array = value.Split(',');
		if (array.Length >= 3 && byte.TryParse(array[0], out var result) && byte.TryParse(array[1], out var result2) && byte.TryParse(array[2], out var result3))
		{
			byte result4 = byte.MaxValue;
			if (array.Length >= 4)
			{
				byte.TryParse(array[3], out result4);
			}
			color = new Color((float)(int)result / 255f, (float)(int)result2 / 255f, (float)(int)result3 / 255f, (float)(int)result4 / 255f);
			_colorCache[entry] = (value, color);
			return true;
		}
		return false;
	}
}
