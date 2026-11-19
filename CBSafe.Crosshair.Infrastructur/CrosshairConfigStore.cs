using System;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Core;
using CBSafe.Crosshair.Render;
using UnityEngine;

namespace CBSafe.Crosshair.Infrastructure;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
internal static class CrosshairConfigStore
{
	private static ConfigEntry<bool> _enabled;

	private static ConfigEntry<bool> _dynVis;

	private static ConfigEntry<bool> _dynGap;

	private static ConfigEntry<int> _styleIdx;

	private static ConfigEntry<string> _mainColor;

	private static ConfigEntry<string> _outlineColor;

	private static ConfigEntry<float> _dotSize;

	private static ConfigEntry<float> _gap;

	private static ConfigEntry<float> _length;

	private static ConfigEntry<float> _thickness;

	private static ConfigEntry<bool> _outline;

	private static bool _bound;

	private static int _uiInitDepth;

	private static int _inputBlockDepth;

	private static CrosshairConfig _uiInitSnapshot;

	private static CrosshairConfig _inputBlockSnapshot;

	private static int _inputUnblockFrames;

	internal static ConfigEntry<string> MainColorEntry => _mainColor;

	internal static ConfigEntry<string> OutlineColorEntry => _outlineColor;

	public static CrosshairConfig Current { get; private set; } = CrosshairConfig.CreateDefaults();


	public static bool IsUiInputGuarded
	{
		get
		{
			if (_uiInitDepth <= 0)
			{
				return _inputBlockDepth > 0;
			}
			return true;
		}
	}

	private static bool IsGuarded => IsUiInputGuarded;

	public static bool HasInputBlockPending => _inputUnblockFrames > 0;

	private static event Action _onChanged;

	private static event Action _onInputUnblocked;

	public static void BeginUiInit()
	{
		if (_uiInitDepth++ == 0)
		{
			_uiInitSnapshot = CloneConfig(Current);
		}
	}

	public static void EndUiInitRestore()
	{
		if (_uiInitDepth == 1 && _uiInitSnapshot != null)
		{
			Current = _uiInitSnapshot;
		}
	}

	public static void EndUiInit()
	{
		if (_uiInitDepth > 0 && --_uiInitDepth == 0)
		{
			_uiInitSnapshot = null;
		}
	}

	public static void EndUiInitAndBlock(int frames = 10)
	{
		if (_uiInitDepth == 1 && _uiInitSnapshot != null)
		{
			Current = _uiInitSnapshot;
		}
		if (_inputBlockDepth == 0)
		{
			_inputBlockSnapshot = CloneConfig(Current);
		}
		_inputBlockDepth++;
		if (frames > _inputUnblockFrames)
		{
			_inputUnblockFrames = frames;
		}
		if (_uiInitDepth > 0 && --_uiInitDepth == 0)
		{
			_uiInitSnapshot = null;
		}
	}

	public static void BeginInputBlockForFrames(int frames)
	{
		if (_inputBlockDepth == 0)
		{
			_inputBlockSnapshot = CloneConfig(Current);
		}
		_inputBlockDepth++;
		if (frames > _inputUnblockFrames)
		{
			_inputUnblockFrames = frames;
		}
	}

	public static void TickInputBlock()
	{
		if (_inputUnblockFrames > 0 && --_inputUnblockFrames == 0)
		{
			EndInputBlock();
		}
	}

	public static void EndInputBlock()
	{
		_inputUnblockFrames = 0;
		if (_inputBlockDepth <= 0 || --_inputBlockDepth != 0)
		{
			return;
		}
		if (_inputBlockSnapshot != null)
		{
			Current = _inputBlockSnapshot;
			_inputBlockSnapshot = null;
			try
			{
				NotifyPreview();
			}
			catch
			{
			}
		}
		try
		{
			CrosshairConfigStore._onInputUnblocked?.Invoke();
		}
		catch
		{
		}
	}

	public static void ForceReleaseInputGuard()
	{
		_inputUnblockFrames = 0;
		while (_inputBlockDepth > 0)
		{
			EndInputBlock();
		}
	}

	public static void ForceReleaseUiInitGuard()
	{
		_uiInitDepth = 0;
		_uiInitSnapshot = null;
	}

	public static void PrepareForPersist()
	{
		_inputUnblockFrames = 0;
		_inputBlockDepth = 0;
		_inputBlockSnapshot = null;
		ForceReleaseUiInitGuard();
	}

	private static CrosshairConfig CloneConfig(CrosshairConfig src)
	{
		if (src == null)
		{
			return CrosshairConfig.CreateDefaults();
		}
		return new CrosshairConfig
		{
			Enabled = src.Enabled,
			DynamicVisibility = src.DynamicVisibility,
			DynamicGap = src.DynamicGap,
			StyleIndex = src.StyleIndex,
			MainColorString = src.MainColorString,
			OutlineColorString = src.OutlineColorString,
			DotSize = src.DotSize,
			Gap = src.Gap,
			Length = src.Length,
			Thickness = src.Thickness,
			OutlineEnabled = src.OutlineEnabled
		};
	}

	public static void Subscribe(Action handler)
	{
		if (handler != null)
		{
			_onChanged += handler;
		}
	}

	public static void SubscribeInputUnblocked(Action handler)
	{
		if (handler != null)
		{
			_onInputUnblocked += handler;
		}
	}

	public static void TryApplyUserEdit(Action mutate)
	{
		if (mutate != null && !IsGuarded)
		{
			mutate();
			NotifyChanged();
		}
	}

	private static void EnsureBound()
	{
		if (!_bound)
		{
			_bound = true;
			ConfigFile file = SharedConfig.File;
			SharedConfig.EnsureDeferredSave();
			CrosshairConfig crosshairConfig = CrosshairConfig.CreateDefaults();
			_enabled = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_Enabled, crosshairConfig.Enabled, "Включить кастомный прицел.");
			_dynVis = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_DynamicVisibility, crosshairConfig.DynamicVisibility, "Динамическая видимость (скрывать прицел в неприцельных режимах).");
			_dynGap = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_DynamicGap, crosshairConfig.DynamicGap, "Динамический зазор (увеличивать gap при движении/стрельбе).");
			_styleIdx = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_StyleIndex, crosshairConfig.StyleIndex, "Индекс стиля прицела (0=ПО УМОЛЧАНИЮ, 1=ТОЧКА В ЦЕНТРЕ, 2=Т-ОБРАЗНЫЙ).");
			_mainColor = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_MainColorIndex, crosshairConfig.MainColorString, "Основной цвет (RGB).");
			_outlineColor = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_OutlineColorIndex, crosshairConfig.OutlineColorString, "Цвет обводки (RGB).");
			_dotSize = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_DotSize, crosshairConfig.DotSize, "Размер центральной точки (0..5).");
			_gap = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_Gap, crosshairConfig.Gap, "Зазор между чёрточками (0..5).");
			_length = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_Length, crosshairConfig.Length, "Длина чёрточек (0..20).");
			_thickness = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_Thickness, crosshairConfig.Thickness, "Толщина чёрточек (0..10).");
			_outline = file.Bind(LocStrings.Cfg_Crosshair, LocStrings.Key_Cross_OutlineEnabled, crosshairConfig.OutlineEnabled, "Включить обводку.");
		}
	}

	public static void ReloadFromDisk()
	{
		Load();
	}

	public static void Load()
	{
		try
		{
			EnsureBound();
		}
		catch (Exception t)
		{
			try
			{
				CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
				if (instance != null)
				{
					ManualLogSource log = instance.Log;
					if (log != null)
					{
						bool isEnabled = false;
						BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(43, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[CrosshairConfigStore] EnsureBound failed: ");
							bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
						}
						log.LogError(bepInExErrorLogInterpolatedStringHandler);
					}
				}
			}
			catch
			{
			}
			Current = CrosshairConfig.CreateDefaults();
			return;
		}
		try
		{
			if (_enabled != null)
			{
				Current.Enabled = _enabled.Value;
			}
		}
		catch (Exception ex)
		{
			LogFieldFail("Enabled", ex);
		}
		try
		{
			if (_dynVis != null)
			{
				Current.DynamicVisibility = _dynVis.Value;
			}
		}
		catch (Exception ex2)
		{
			LogFieldFail("DynVis", ex2);
		}
		try
		{
			if (_dynGap != null)
			{
				Current.DynamicGap = _dynGap.Value;
			}
		}
		catch (Exception ex3)
		{
			LogFieldFail("DynGap", ex3);
		}
		try
		{
			if (_styleIdx != null)
			{
				Current.StyleIndex = _styleIdx.Value;
			}
		}
		catch (Exception ex4)
		{
			LogFieldFail("StyleIndex", ex4);
		}
		try
		{
			if (_mainColor != null)
			{
				Current.MainColorString = _mainColor.Value;
			}
		}
		catch (Exception ex5)
		{
			LogFieldFail("MainColor", ex5);
		}
		try
		{
			if (_outlineColor != null)
			{
				Current.OutlineColorString = _outlineColor.Value;
			}
		}
		catch (Exception ex6)
		{
			LogFieldFail("OutlineColor", ex6);
		}
		try
		{
			if (_dotSize != null)
			{
				Current.DotSize = _dotSize.Value;
			}
		}
		catch (Exception ex7)
		{
			LogFieldFail("DotSize", ex7);
		}
		try
		{
			if (_gap != null)
			{
				Current.Gap = _gap.Value;
			}
		}
		catch (Exception ex8)
		{
			LogFieldFail("Gap", ex8);
		}
		try
		{
			if (_length != null)
			{
				Current.Length = _length.Value;
			}
		}
		catch (Exception ex9)
		{
			LogFieldFail("Length", ex9);
		}
		try
		{
			if (_thickness != null)
			{
				Current.Thickness = _thickness.Value;
			}
		}
		catch (Exception ex10)
		{
			LogFieldFail("Thickness", ex10);
		}
		try
		{
			if (_outline != null)
			{
				Current.OutlineEnabled = _outline.Value;
			}
		}
		catch (Exception ex11)
		{
			LogFieldFail("OutlineEnabled", ex11);
		}
		try
		{
			Clamp(Current);
		}
		catch
		{
		}
	}

	private static void LogFieldFail(string field, Exception ex)
	{
		try
		{
			CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(63, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[CrosshairConfigStore] Load field '");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(field);
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("' failed (keeping default): ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
		catch
		{
		}
	}

	public static void Save()
	{
		PersistNow();
		try
		{
			SharedConfig.File.Save();
		}
		catch
		{
		}
	}

	public static void PersistNow(bool mirrorBepInEx = true)
	{
		if (!mirrorBepInEx)
		{
			return;
		}
		try
		{
			ApplyToConfig();
			SharedConfig.MarkDirty();
			SharedConfig.FlushNow();
		}
		catch (Exception t)
		{
			try
			{
				CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
				if (instance == null)
				{
					return;
				}
				ManualLogSource log = instance.Log;
				if (log != null)
				{
					bool isEnabled = false;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(39, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[CrosshairConfigStore] persist failed: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
			}
			catch
			{
			}
		}
	}

	public static void NotifyPreview()
	{
		try
		{
			CrosshairRenderDriver.InvalidateColorCache();
		}
		catch
		{
		}
		try
		{
			CrosshairRenderDriver.InvalidateLayout();
		}
		catch
		{
		}
		try
		{
			CrosshairConfigStore._onChanged?.Invoke();
		}
		catch (Exception ex)
		{
			try
			{
				CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
				if (instance == null)
				{
					return;
				}
				ManualLogSource log = instance.Log;
				if (log != null)
				{
					bool isEnabled = false;
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(32, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[CrosshairConfigStore] preview: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
			catch
			{
			}
		}
	}

	public static void NotifyPersist()
	{
		PersistNow();
	}

	public static void NotifyChanged()
	{
		if (!IsGuarded)
		{
			NotifyPreview();
			NotifyPersist();
		}
	}

	private static void ApplyToConfig()
	{
		try
		{
			EnsureBound();
		}
		catch (Exception t)
		{
			try
			{
				CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
				if (instance == null)
				{
					return;
				}
				ManualLogSource log = instance.Log;
				if (log != null)
				{
					bool isEnabled = false;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(50, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[CrosshairConfigStore] ApplyToConfig EnsureBound: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
				return;
			}
			catch
			{
				return;
			}
		}
		try
		{
			Clamp(Current);
		}
		catch
		{
		}
		try
		{
			if (_enabled != null)
			{
				_enabled.Value = Current.Enabled;
			}
		}
		catch (Exception ex)
		{
			LogFieldFail("Enabled (apply)", ex);
		}
		try
		{
			if (_dynVis != null)
			{
				_dynVis.Value = Current.DynamicVisibility;
			}
		}
		catch (Exception ex2)
		{
			LogFieldFail("DynVis (apply)", ex2);
		}
		try
		{
			if (_dynGap != null)
			{
				_dynGap.Value = Current.DynamicGap;
			}
		}
		catch (Exception ex3)
		{
			LogFieldFail("DynGap (apply)", ex3);
		}
		try
		{
			if (_styleIdx != null)
			{
				_styleIdx.Value = Current.StyleIndex;
			}
		}
		catch (Exception ex4)
		{
			LogFieldFail("StyleIndex (apply)", ex4);
		}
		try
		{
			if (_mainColor != null)
			{
				_mainColor.Value = Current.MainColorString;
			}
		}
		catch (Exception ex5)
		{
			LogFieldFail("MainColor (apply)", ex5);
		}
		try
		{
			if (_outlineColor != null)
			{
				_outlineColor.Value = Current.OutlineColorString;
			}
		}
		catch (Exception ex6)
		{
			LogFieldFail("OutlineColor (apply)", ex6);
		}
		try
		{
			if (_dotSize != null)
			{
				_dotSize.Value = Current.DotSize;
			}
		}
		catch (Exception ex7)
		{
			LogFieldFail("DotSize (apply)", ex7);
		}
		try
		{
			if (_gap != null)
			{
				_gap.Value = Current.Gap;
			}
		}
		catch (Exception ex8)
		{
			LogFieldFail("Gap (apply)", ex8);
		}
		try
		{
			if (_length != null)
			{
				_length.Value = Current.Length;
			}
		}
		catch (Exception ex9)
		{
			LogFieldFail("Length (apply)", ex9);
		}
		try
		{
			if (_thickness != null)
			{
				_thickness.Value = Current.Thickness;
			}
		}
		catch (Exception ex10)
		{
			LogFieldFail("Thickness (apply)", ex10);
		}
		try
		{
			if (_outline != null)
			{
				_outline.Value = Current.OutlineEnabled;
			}
		}
		catch (Exception ex11)
		{
			LogFieldFail("OutlineEnabled (apply)", ex11);
		}
	}

	public static void ResetToDefaults()
	{
		Current = CrosshairConfig.CreateDefaults();
		NotifyChanged();
	}

	private static void Clamp(CrosshairConfig c)
	{
		if (c != null)
		{
			c.DotSize = Mathf.Clamp(c.DotSize, 0f, 5f);
			c.Gap = Mathf.Clamp(c.Gap, 0f, 5f);
			c.Length = Mathf.Clamp(c.Length, 0f, 20f);
			c.Thickness = Mathf.Clamp(c.Thickness, 0f, 10f);
			c.StyleIndex = Mathf.Clamp(c.StyleIndex, 0, CrosshairConfig.StyleLabels.Length - 1);
		}
	}
}
