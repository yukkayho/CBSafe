using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.Patches;
using CBSafe.Core;
using CBSafe.Core.UI;
using CBSafe.Crosshair.Infrastructure;
using CBSafe.Crosshair.Render;
using CBSafe.Crosshair.UI;
using CBSafe.Patches;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CBSafe.Colors.Infrastructure;

[HydraNoObfuscate]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class CBSafeColorSessionBootstrap : MonoBehaviour
{
	private static int _lastSceneHandle;

	private static bool _sceneInit;

	private float _reapplyUntil;

	private float _nextReapply;

	private static float _coldStartReapplyUntil;

	private const float ReapplyWindowDuration = 30f;

	private const float ReapplyInterval = 1f;

	private static bool _connectionTrackInit;

	private static bool _lastConnected;

	private static int _delayedHoloApplyIndex;

	private static float _nextDelayedHoloApplyAt;

	private static readonly float[] DelayedHoloApplySeconds = new float[7] { 1f, 3f, 8f, 15f, 30f, 60f, 120f };

	public CBSafeColorSessionBootstrap(IntPtr ptr)
		: base(ptr)
	{
	}

	internal static void ArmColdStart()
	{
		_coldStartReapplyUntil = Time.unscaledTime + 30f;
		ResetDelayedHoloApply();
		ReapplyAllColors();
	}

	private void LateUpdate()
	{
		try
		{
			int handle = SceneManager.GetActiveScene().handle;
			if (!_sceneInit)
			{
				_sceneInit = true;
				_lastSceneHandle = handle;
				_reapplyUntil = Time.unscaledTime + 30f;
				_coldStartReapplyUntil = _reapplyUntil;
				ReapplyAllColors();
			}
			if (handle != _lastSceneHandle)
			{
				_lastSceneHandle = handle;
				OnSessionSceneChanged();
			}
			TrackConnectionTransition();
			TickDelayedHoloApply();
			float num = ((_reapplyUntil > _coldStartReapplyUntil) ? _reapplyUntil : _coldStartReapplyUntil);
			if (!(Time.unscaledTime >= num) && !(Time.unscaledTime < _nextReapply))
			{
				_nextReapply = Time.unscaledTime + 1f;
				ReapplyAllColors();
			}
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(42, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[CBSafeColorSessionBootstrap] LateUpdate: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private void OnSessionSceneChanged()
	{
		try
		{
			try
			{
				SettingsTabPrefetcher.PauseForWorldLoad(3f);
			}
			catch
			{
			}
			try
			{
				CrosshairContentPrefetcher.PauseForWorldLoad(3f);
			}
			catch
			{
			}
			try
			{
				CBSafeSettingsUiLoadBootstrap.OnServerLoadStarted();
			}
			catch
			{
			}
			try
			{
				GraphicsDon.ReapplyPersistedClientSettings();
			}
			catch
			{
			}
			_reapplyUntil = Time.unscaledTime + 30f;
			_nextReapply = 0f;
			try
			{
				GraphicsDon.ArmHoloBootstrapPulse();
			}
			catch
			{
			}
			ResetDelayedHoloApply();
			HudBarColorApplier.OnExternalSessionChanged();
			HoloReticleColorApplier.PrepareForGameplaySession();
			CrosshairRenderDriver.InvalidateColorCache();
			TeamPlayerNameColorApplier.OnSessionSceneChanged();
			try
			{
				CrosshairConfigStore.ReloadFromDisk();
			}
			catch
			{
			}
			ReapplyAllColors(fullHoloScan: true);
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(53, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[CBSafeColorSessionBootstrap] OnSessionSceneChanged: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private void TrackConnectionTransition()
	{
		bool flag = false;
		try
		{
			BasePlayer entity = LocalPlayer.Entity;
			flag = entity != null && entity.IsConnected;
		}
		catch
		{
		}
		if (!_connectionTrackInit)
		{
			_connectionTrackInit = true;
			_lastConnected = flag;
			if (flag)
			{
				OnGameplayConnected();
			}
		}
		else if (flag != _lastConnected)
		{
			_lastConnected = flag;
			if (flag)
			{
				OnGameplayConnected();
			}
		}
	}

	private void OnGameplayConnected()
	{
		try
		{
			try
			{
				SettingsTabPrefetcher.PauseForWorldLoad(3f);
			}
			catch
			{
			}
			try
			{
				CrosshairContentPrefetcher.PauseForWorldLoad(3f);
			}
			catch
			{
			}
			try
			{
				CBSafeSettingsUiLoadBootstrap.OnServerLoadStarted();
			}
			catch
			{
			}
			try
			{
				GraphicsDon.ReapplyPersistedClientSettings();
			}
			catch
			{
			}
			_reapplyUntil = Time.unscaledTime + 30f;
			_coldStartReapplyUntil = _reapplyUntil;
			_nextReapply = 0f;
			try
			{
				GraphicsDon.ArmHoloBootstrapPulse();
			}
			catch
			{
			}
			ResetDelayedHoloApply();
			HoloReticleColorApplier.PrepareForGameplaySession();
			HoloReticleColorApplier.ApplySavedConfigNow();
			ReapplyAllColors(fullHoloScan: true);
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(51, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[CBSafeColorSessionBootstrap] OnGameplayConnected: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void ResetDelayedHoloApply()
	{
		_delayedHoloApplyIndex = 0;
		_nextDelayedHoloApplyAt = Time.unscaledTime + DelayedHoloApplySeconds[0];
	}

	private static void TickDelayedHoloApply()
	{
		if (_delayedHoloApplyIndex < DelayedHoloApplySeconds.Length && !(Time.unscaledTime < _nextDelayedHoloApplyAt))
		{
			try
			{
				HoloReticleColorApplier.ApplySavedConfigNow();
			}
			catch
			{
			}
			_delayedHoloApplyIndex++;
			if (_delayedHoloApplyIndex < DelayedHoloApplySeconds.Length)
			{
				_nextDelayedHoloApplyAt = Time.unscaledTime + DelayedHoloApplySeconds[_delayedHoloApplyIndex];
			}
		}
	}

	internal static void ReapplyAllColors(bool fullHoloScan = false)
	{
		CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
		if (instance == null)
		{
			return;
		}
		try
		{
			PluginConfig.SyncFromSharedConfig();
		}
		catch
		{
		}
		try
		{
			CrosshairRenderDriver.InvalidateColorCache();
		}
		catch
		{
		}
		try
		{
			HudBarColorApplier.RefreshCachedColors();
		}
		catch
		{
		}
		try
		{
			HoloReticleColorApplier.RefreshCachedColor(fullHoloScan);
		}
		catch
		{
		}
		try
		{
			instance.HudBarApplier?.ForceReapplyNow();
		}
		catch
		{
		}
		try
		{
			instance.HoloReticleApplier?.ForceReapplyNow();
		}
		catch
		{
		}
		try
		{
			instance.TreeCrosshairApplier?.ForceReapplyNow();
		}
		catch
		{
		}
		try
		{
			instance.TeamPlayerNameApplier?.ForceReapplyNow();
		}
		catch
		{
		}
	}
}
