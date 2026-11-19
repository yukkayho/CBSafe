using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.Infrastructure;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CBSafe.Colors.Patches;

[HydraNoObfuscate]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class HoloReticleColorApplier : MonoBehaviour
{
	private struct GraphicOrig
	{
		public Graphic g;

		public Color color;
	}

	private struct SpriteOrig
	{
		public SpriteRenderer s;

		public Color color;
	}

	private struct MaterialOrig
	{
		public Material m;

		public string prop;

		public Color color;
	}

	private static readonly string[] ColorPropertyNames = new string[4] { "_Color", "_BaseColor", "_TintColor", "_EmissionColor" };

	private static readonly List<HolosightReticlePositioning> ReticleCache = new List<HolosightReticlePositioning>(8);

	internal static bool _customActive;

	private static float _nextFullScanTime;

	internal static Color _cachedColor;

	internal static bool _hasColor;

	private static readonly Dictionary<int, GraphicOrig> _originalGraphics = new Dictionary<int, GraphicOrig>();

	private static readonly Dictionary<int, SpriteOrig> _originalSprites = new Dictionary<int, SpriteOrig>();

	private static readonly List<MaterialOrig> _originalMaterials = new List<MaterialOrig>();

	private float _activeWindowEndTime;

	private const float ActiveWindowDuration = 2.5f;

	private const float SessionBoostDuration = 120f;

	private bool _bootstrapped;

	private HolosightReticlePositioning _trackedReticle;

	private float _nextRefreshTime;

	private float _nextApplyTime;

	private float _nextCleanupTime;

	private Color _lastAppliedColor;

	private bool _hasLastApplied;

	private readonly List<Graphic> _graphics = new List<Graphic>(8);

	private readonly List<SpriteRenderer> _sprites = new List<SpriteRenderer>(8);

	private readonly List<Renderer> _renderers = new List<Renderer>(8);

	private static int _lastSceneHandle;

	private static bool _sceneTrackInitialised;

	private static float _nextDiskConfigRefreshAt;

	private const string PrefabSceneName = "PrefabPreProcess - Client";

	public HoloReticleColorApplier(IntPtr ptr)
		: base(ptr)
	{
	}

	internal static bool NeedsBootstrapPulse()
	{
		RefreshCachedColor(forceDiskRead: true);
		if (!_customActive || !_hasColor)
		{
			return false;
		}
		HoloReticleColorApplier holoReticleColorApplier = CBSafeColorsPlugin.Instance?.HoloReticleApplier;
		if (holoReticleColorApplier != null && holoReticleColorApplier._trackedReticle != null && IsRuntimeReticle(holoReticleColorApplier._trackedReticle) && holoReticleColorApplier._hasLastApplied && ColorsApproximatelyEqual(holoReticleColorApplier._lastAppliedColor, _cachedColor))
		{
			return false;
		}
		return true;
	}

	internal static void PrepareForGameplaySession()
	{
		ReticleCache.Clear();
		HoloReticleColorApplier holoReticleColorApplier = CBSafeColorsPlugin.Instance?.HoloReticleApplier;
		if (holoReticleColorApplier != null)
		{
			holoReticleColorApplier._trackedReticle = null;
			holoReticleColorApplier._graphics.Clear();
			holoReticleColorApplier._sprites.Clear();
			holoReticleColorApplier._renderers.Clear();
			holoReticleColorApplier._hasLastApplied = false;
			holoReticleColorApplier._nextRefreshTime = 0f;
			holoReticleColorApplier._nextApplyTime = 0f;
			holoReticleColorApplier._activeWindowEndTime = Time.unscaledTime + 120f;
		}
		try
		{
			PluginConfig.SyncFromSharedConfig();
		}
		catch
		{
		}
		_nextDiskConfigRefreshAt = 0f;
		RefreshCachedColor(forceDiskRead: true);
		holoReticleColorApplier?.ForceReapplyNow();
	}

	internal static void ApplySavedConfigNow(bool armBootstrapOnly = false)
	{
		try
		{
			PluginConfig.SyncFromSharedConfig();
		}
		catch
		{
		}
		_nextDiskConfigRefreshAt = 0f;
		RefreshCachedColor(forceDiskRead: true);
		if (!armBootstrapOnly && _customActive && _hasColor)
		{
			CBSafeColorsPlugin.Instance?.HoloReticleApplier?.ForceReapplyNow();
		}
	}

	internal static void NotifyReticleConfigChanged(bool reloadFromDisk = false)
	{
		if (reloadFromDisk)
		{
			try
			{
				PluginConfig.SyncFromSharedConfig();
			}
			catch
			{
			}
			_nextDiskConfigRefreshAt = 0f;
		}
		RefreshCachedColor(reloadFromDisk);
		CBSafeColorsPlugin.Instance?.HoloReticleApplier?.ForceReapplyNow();
	}

	internal static void OnExternalSessionChanged()
	{
		PrepareForGameplaySession();
	}

	internal static void ResetWorldBootstrapState()
	{
	}

	internal static bool IsSessionColorStable()
	{
		if (_customActive)
		{
			return _hasColor;
		}
		return true;
	}

	internal static void RepaintAllGameplayReticles(bool allowRescan = true)
	{
		CBSafeColorsPlugin.Instance?.HoloReticleApplier?.ForceReapplyNow();
	}

	private void Update()
	{
		try
		{
			if (!_bootstrapped)
			{
				_bootstrapped = true;
				RefreshCachedColor(forceDiskRead: true);
				if (_customActive && _hasColor)
				{
					_activeWindowEndTime = Time.unscaledTime + 120f;
					ForceReapplyNow();
				}
			}
			int handle = SceneManager.GetActiveScene().handle;
			if (!_sceneTrackInitialised)
			{
				_sceneTrackInitialised = true;
				_lastSceneHandle = handle;
			}
			else if (handle != _lastSceneHandle)
			{
				_lastSceneHandle = handle;
				OnSceneChanged();
			}
			if (SceneManager.GetActiveScene().name != null && SceneManager.GetActiveScene().name.Contains("Menu"))
			{
				return;
			}
			RefreshCachedColor();
			if (!_customActive || !_hasColor)
			{
				_hasLastApplied = false;
				return;
			}
			bool flag = !_hasLastApplied || !ColorsApproximatelyEqual(_lastAppliedColor, _cachedColor);
			if (flag)
			{
				_lastAppliedColor = _cachedColor;
				_hasLastApplied = true;
				_activeWindowEndTime = Time.unscaledTime + 2.5f;
			}
			bool flag2 = Time.unscaledTime <= _activeWindowEndTime;
			if (Time.unscaledTime >= _nextCleanupTime)
			{
				CleanupReticleCache();
				_nextCleanupTime = Time.unscaledTime + 3f;
			}
			bool flag3 = _customActive && _hasColor && (_trackedReticle == null || !IsRuntimeReticle(_trackedReticle));
			if (_trackedReticle == null || (_trackedReticle != null && _trackedReticle.transform == null) || TargetsMissing() || flag3 || (flag2 && Time.unscaledTime >= _nextRefreshTime) || (flag3 && Time.unscaledTime >= _nextRefreshTime))
			{
				ReacquireTrackedReticleFromCache();
				_nextRefreshTime = Time.unscaledTime + (flag3 ? 0.5f : ((_trackedReticle == null) ? 1.2f : 0.8f));
			}
			if (!(_trackedReticle != null) || !(Time.unscaledTime >= _nextApplyTime))
			{
				return;
			}
			bool flag4 = flag || !_hasLastApplied || !ColorsApproximatelyEqual(_lastAppliedColor, _cachedColor) || flag2;
			if (flag4)
			{
				ApplyColor(_cachedColor);
				if (_trackedReticle != null)
				{
					PaintReticleAndParents(_trackedReticle, _cachedColor);
				}
				PaintAllCachedReticles(_cachedColor, rescan: false);
				_hasLastApplied = true;
				_lastAppliedColor = _cachedColor;
			}
			_nextApplyTime = Time.unscaledTime + (flag4 ? 0.1f : 0.25f);
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HoloReticleColorApplier] Update: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	internal static void RefreshCachedColor(bool forceDiskRead = false)
	{
		if (forceDiskRead || Time.unscaledTime >= _nextDiskConfigRefreshAt)
		{
			_nextDiskConfigRefreshAt = Time.unscaledTime + 2f;
			try
			{
				PluginConfig.SyncFromSharedConfig();
			}
			catch
			{
			}
		}
		_customActive = PluginConfig.IsReticleCustomizationActive();
		Color color;
		bool active;
		if (!_customActive)
		{
			_hasColor = false;
		}
		else if (ReticleColorSession.TryGetResolved(out color, out active) && active)
		{
			_cachedColor = color;
			_hasColor = true;
		}
		else
		{
			_hasColor = PluginConfig.TryResolveReticle(out _cachedColor);
		}
	}

	internal static void ApplySelectionFromUi()
	{
		_customActive = true;
		_hasColor = PluginConfig.TryResolveReticle(out _cachedColor);
		if (_hasColor)
		{
			_ = _cachedColor;
		}
		HoloReticleColorApplier holoReticleColorApplier = CBSafeColorsPlugin.Instance?.HoloReticleApplier;
		if (holoReticleColorApplier != null)
		{
			holoReticleColorApplier._activeWindowEndTime = Time.unscaledTime + 2.5f;
			holoReticleColorApplier._hasLastApplied = false;
			holoReticleColorApplier._nextRefreshTime = 0f;
			holoReticleColorApplier._nextApplyTime = 0f;
			holoReticleColorApplier.ForceReapplyNow();
		}
	}

	internal static void OnReticleUpdate(HolosightReticlePositioning reticle)
	{
		if (reticle == null)
		{
			return;
		}
		RefreshCachedColor();
		if (!_customActive || !_hasColor)
		{
			return;
		}
		try
		{
			if (!ReticleCache.Contains(reticle))
			{
				ReticleCache.Add(reticle);
			}
			HoloReticleColorApplier holoReticleColorApplier = CBSafeColorsPlugin.Instance?.HoloReticleApplier;
			if (holoReticleColorApplier != null)
			{
				holoReticleColorApplier._trackedReticle = reticle;
				holoReticleColorApplier._activeWindowEndTime = Time.unscaledTime + 2.5f;
			}
			if (!(reticle.transform == null))
			{
				PaintReticleAndParents(reticle, _cachedColor);
				if (holoReticleColorApplier != null)
				{
					holoReticleColorApplier._hasLastApplied = true;
					holoReticleColorApplier._lastAppliedColor = _cachedColor;
				}
			}
		}
		catch
		{
		}
	}

	private static bool TryPaintReticleRootGraphic(HolosightReticlePositioning reticle, Color color)
	{
		if (reticle == null)
		{
			return false;
		}
		try
		{
			Graphic component = reticle.GetComponent<Graphic>();
			if (component == null)
			{
				return false;
			}
			int instanceID = component.GetInstanceID();
			if (!_originalGraphics.ContainsKey(instanceID))
			{
				_originalGraphics[instanceID] = new GraphicOrig
				{
					g = component,
					color = component.color
				};
			}
			if (component.color != color)
			{
				component.color = color;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void PaintReticleAndParents(HolosightReticlePositioning reticle, Color color)
	{
		if (reticle == null)
		{
			return;
		}
		Transform transform;
		try
		{
			transform = reticle.transform;
		}
		catch
		{
			return;
		}
		if (!(transform == null))
		{
			TryPaintReticleRootGraphic(reticle, color);
			PaintBranch(transform, color);
			Transform parent = transform.parent;
			int num = 0;
			while (parent != null && num < 3)
			{
				PaintBranch(parent, color);
				parent = parent.parent;
				num++;
			}
		}
	}

	private static int PaintAllCachedReticles(Color color, bool rescan = true)
	{
		int num = 0;
		for (int num2 = ReticleCache.Count - 1; num2 >= 0; num2--)
		{
			HolosightReticlePositioning holosightReticlePositioning = ReticleCache[num2];
			if (holosightReticlePositioning == null || holosightReticlePositioning.transform == null)
			{
				ReticleCache.RemoveAt(num2);
			}
			else
			{
				PaintReticleAndParents(holosightReticlePositioning, color);
				num++;
			}
		}
		return num;
	}

	private static void PaintBranch(Transform root, Color color)
	{
		if (root == null)
		{
			return;
		}
		try
		{
			Il2CppArrayBase<Renderer> componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
			int length = componentsInChildren.Length;
			for (int i = 0; i < length; i++)
			{
				Renderer renderer = componentsInChildren[i];
				if (!(renderer == null) && !(renderer.TryCast<SpriteRenderer>() != null))
				{
					string text = SafeLower(renderer.name);
					if (text == "georeticle" || text.Contains("reticle") || text.Contains("dot") || text.Contains("center"))
					{
						ApplyRendererColor(renderer, color);
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			Il2CppArrayBase<SpriteRenderer> componentsInChildren2 = root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
			int length2 = componentsInChildren2.Length;
			for (int j = 0; j < length2; j++)
			{
				SpriteRenderer spriteRenderer = componentsInChildren2[j];
				if (spriteRenderer == null)
				{
					continue;
				}
				string text2 = SafeLower(spriteRenderer.name);
				if (!text2.Contains("dot") && !text2.Contains("reticle") && !text2.Contains("center") && !text2.Contains("point") && !text2.Contains("geo"))
				{
					continue;
				}
				try
				{
					int instanceID = spriteRenderer.GetInstanceID();
					if (!_originalSprites.ContainsKey(instanceID))
					{
						_originalSprites[instanceID] = new SpriteOrig
						{
							s = spriteRenderer,
							color = spriteRenderer.color
						};
					}
					if (spriteRenderer.color != color)
					{
						spriteRenderer.color = color;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		try
		{
			Il2CppArrayBase<Graphic> componentsInChildren3 = root.GetComponentsInChildren<Graphic>(includeInactive: true);
			int length3 = componentsInChildren3.Length;
			for (int k = 0; k < length3; k++)
			{
				Graphic graphic = componentsInChildren3[k];
				if (graphic == null)
				{
					continue;
				}
				string text3 = SafeLower(graphic.name);
				if (text3.Contains("darken") || text3.Contains("background") || text3.Contains("frame") || text3.Contains("border") || text3.Contains("panel") || text3 == "reticle" || (!text3.Contains("dot") && !text3.Contains("center") && !text3.Contains("cross") && !text3.Contains("point") && !text3.Contains("geo")) || !LooksLikeCenterGraphic(graphic))
				{
					continue;
				}
				try
				{
					int instanceID2 = graphic.GetInstanceID();
					if (!_originalGraphics.ContainsKey(instanceID2))
					{
						_originalGraphics[instanceID2] = new GraphicOrig
						{
							g = graphic,
							color = graphic.color
						};
					}
					if (graphic.color != color)
					{
						graphic.color = color;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private void OnSceneChanged()
	{
		try
		{
			_trackedReticle = null;
			_graphics.Clear();
			_sprites.Clear();
			_renderers.Clear();
			_hasLastApplied = false;
			_nextRefreshTime = 0f;
			_nextApplyTime = 0f;
			_activeWindowEndTime = Time.unscaledTime + 120f;
			ReticleCache.Clear();
			ForceReapplyNow();
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
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HoloReticleColorApplier] OnSceneChanged: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	public void QuickApplyFromConfig(bool skipConfigReload = false)
	{
		try
		{
			if (!skipConfigReload)
			{
				RefreshCachedColor(forceDiskRead: true);
			}
			if (!_customActive || !_hasColor)
			{
				return;
			}
			_activeWindowEndTime = Time.unscaledTime + 2.5f;
			if (_trackedReticle != null && !TargetsMissing())
			{
				ApplyColor(_cachedColor);
				PaintReticleAndParents(_trackedReticle, _cachedColor);
				_hasLastApplied = true;
				_lastAppliedColor = _cachedColor;
				return;
			}
			if (ReticleCache.Count > 0)
			{
				ReacquireTrackedReticleFromCache();
				if (_trackedReticle != null)
				{
					ApplyColor(_cachedColor);
					PaintReticleAndParents(_trackedReticle, _cachedColor);
					_hasLastApplied = true;
					_lastAppliedColor = _cachedColor;
				}
			}
			if (PaintAllCachedReticles(_cachedColor) > 0)
			{
				_hasLastApplied = true;
				_lastAppliedColor = _cachedColor;
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(48, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HoloReticleColorApplier] QuickApplyFromConfig: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	public void ForceReapplyNow()
	{
		bool isEnabled = false;
		try
		{
			RefreshCachedColor(forceDiskRead: true);
			if (!_customActive || !_hasColor)
			{
				return;
			}
			_activeWindowEndTime = Time.unscaledTime + 2.5f;
			_nextRefreshTime = 0f;
			_nextApplyTime = 0f;
			if (Time.unscaledTime >= _nextFullScanTime)
			{
				ScanReticlesIntoCache();
				_nextFullScanTime = Time.unscaledTime + 30f;
			}
			ReacquireTrackedReticleFromCache();
			bool flag = false;
			if (_trackedReticle != null && !TargetsMissing())
			{
				ApplyColor(_cachedColor);
				PaintReticleAndParents(_trackedReticle, _cachedColor);
				flag = true;
			}
			flag = (_hasLastApplied = PaintAllCachedReticles(_cachedColor) > 0 || flag);
			_lastAppliedColor = _cachedColor;
			if (!PluginConfig.IsDebugEnabled)
			{
				return;
			}
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(67, 3, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[HoloReticleColorApplier] ForceReapplyNow: cache=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(ReticleCache.Count);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" tracked=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_trackedReticle != null);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" targets=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_graphics.Count + _sprites.Count + _renderers.Count);
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance2 = CBSafeColorsPlugin.Instance;
			if (instance2 == null)
			{
				return;
			}
			ManualLogSource log2 = instance2.Log;
			if (log2 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(43, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HoloReticleColorApplier] ForceReapplyNow: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static bool ColorsApproximatelyEqual(Color a, Color b)
	{
		if (Mathf.Abs(a.r - b.r) < 0.001f && Mathf.Abs(a.g - b.g) < 0.001f && Mathf.Abs(a.b - b.b) < 0.001f)
		{
			return Mathf.Abs(a.a - b.a) < 0.001f;
		}
		return false;
	}

	private static void CleanupReticleCache()
	{
		for (int num = ReticleCache.Count - 1; num >= 0; num--)
		{
			HolosightReticlePositioning holosightReticlePositioning = ReticleCache[num];
			if (holosightReticlePositioning == null || holosightReticlePositioning.transform == null)
			{
				ReticleCache.RemoveAt(num);
			}
		}
	}

	private void ReacquireTrackedReticleFromCache()
	{
		_trackedReticle = null;
		if (TryPickTrackedReticleFromCache(out var reticle))
		{
			_trackedReticle = reticle;
			RebuildTargets(reticle);
		}
	}

	private static bool TryPickTrackedReticleFromCache(out HolosightReticlePositioning reticle)
	{
		if (TryPickRuntimeReticleFromCache(out reticle))
		{
			return true;
		}
		for (int i = 0; i < ReticleCache.Count; i++)
		{
			HolosightReticlePositioning holosightReticlePositioning = ReticleCache[i];
			if (!(holosightReticlePositioning == null) && !(holosightReticlePositioning.transform == null))
			{
				reticle = holosightReticlePositioning;
				return true;
			}
		}
		reticle = null;
		return false;
	}

	private static bool TryPickRuntimeReticleFromCache(out HolosightReticlePositioning reticle)
	{
		reticle = null;
		for (int i = 0; i < ReticleCache.Count; i++)
		{
			HolosightReticlePositioning holosightReticlePositioning = ReticleCache[i];
			if (IsRuntimeReticle(holosightReticlePositioning))
			{
				reticle = holosightReticlePositioning;
				return true;
			}
		}
		return false;
	}

	private static bool IsRuntimeReticle(HolosightReticlePositioning reticle)
	{
		if (reticle == null)
		{
			return false;
		}
		Transform transform;
		try
		{
			transform = reticle.transform;
		}
		catch
		{
			return false;
		}
		if (transform == null)
		{
			return false;
		}
		GameObject gameObject;
		try
		{
			gameObject = transform.gameObject;
		}
		catch
		{
			return false;
		}
		if (gameObject == null)
		{
			return false;
		}
		Scene scene;
		try
		{
			scene = gameObject.scene;
		}
		catch
		{
			return false;
		}
		if (!scene.IsValid())
		{
			return false;
		}
		return (scene.name ?? string.Empty) != "PrefabPreProcess - Client";
	}

	private static void ScanReticlesIntoCache()
	{
		try
		{
			Il2CppArrayBase<HolosightReticlePositioning> il2CppArrayBase = Resources.FindObjectsOfTypeAll<HolosightReticlePositioning>();
			int length = il2CppArrayBase.Length;
			for (int i = 0; i < length; i++)
			{
				HolosightReticlePositioning holosightReticlePositioning = il2CppArrayBase[i];
				if (!(holosightReticlePositioning == null) && !(holosightReticlePositioning.transform == null) && !ReticleCache.Contains(holosightReticlePositioning))
				{
					ReticleCache.Add(holosightReticlePositioning);
				}
			}
		}
		catch
		{
		}
	}

	private bool TargetsMissing()
	{
		CleanupNulls(_graphics);
		CleanupNulls(_sprites);
		CleanupNulls(_renderers);
		if (_graphics.Count == 0 && _sprites.Count == 0)
		{
			return _renderers.Count == 0;
		}
		return false;
	}

	private void RebuildTargets(HolosightReticlePositioning reticle)
	{
		_trackedReticle = reticle;
		_graphics.Clear();
		_sprites.Clear();
		_renderers.Clear();
		Transform transform = reticle.transform;
		if (transform == null)
		{
			return;
		}
		TryCollectReticleRootGraphic(transform, _graphics);
		CollectExactBranch(transform);
		Transform parent = transform.parent;
		if (!(parent != null))
		{
			return;
		}
		CollectExactBranch(parent);
		if (parent.parent != null)
		{
			CollectExactBranch(parent.parent);
			if (parent.parent.parent != null)
			{
				CollectExactBranch(parent.parent.parent);
			}
		}
	}

	private static void TryCollectReticleRootGraphic(Transform self, List<Graphic> graphics)
	{
		if (self == null || graphics == null)
		{
			return;
		}
		try
		{
			Graphic component = self.GetComponent<Graphic>();
			if (component != null)
			{
				AddUnique(graphics, component);
			}
		}
		catch
		{
		}
	}

	private void CollectExactBranch(Transform root)
	{
		if (root == null)
		{
			return;
		}
		Il2CppArrayBase<Renderer> componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
		int length = componentsInChildren.Length;
		for (int i = 0; i < length; i++)
		{
			Renderer renderer = componentsInChildren[i];
			if (!(renderer == null) && !(renderer.TryCast<SpriteRenderer>() != null))
			{
				string text = SafeLower(renderer.name);
				if (text == "georeticle" || text.Contains("reticle") || text.Contains("dot") || text.Contains("center"))
				{
					AddUnique(_renderers, renderer);
				}
			}
		}
		Il2CppArrayBase<SpriteRenderer> componentsInChildren2 = root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
		int length2 = componentsInChildren2.Length;
		for (int j = 0; j < length2; j++)
		{
			SpriteRenderer spriteRenderer = componentsInChildren2[j];
			if (!(spriteRenderer == null))
			{
				string text2 = SafeLower(spriteRenderer.name);
				if (text2.Contains("dot") || text2.Contains("reticle") || text2.Contains("center") || text2.Contains("point") || text2.Contains("geo"))
				{
					AddUnique(_sprites, spriteRenderer);
				}
			}
		}
		Il2CppArrayBase<Graphic> componentsInChildren3 = root.GetComponentsInChildren<Graphic>(includeInactive: true);
		int length3 = componentsInChildren3.Length;
		for (int k = 0; k < length3; k++)
		{
			Graphic graphic = componentsInChildren3[k];
			if (!(graphic == null))
			{
				string text3 = SafeLower(graphic.name);
				if (!text3.Contains("darken") && !text3.Contains("background") && !text3.Contains("frame") && !text3.Contains("border") && !text3.Contains("panel") && !(text3 == "reticle") && (text3.Contains("dot") || text3.Contains("center") || text3.Contains("cross") || text3.Contains("point") || text3.Contains("geo")) && LooksLikeCenterGraphic(graphic))
				{
					AddUnique(_graphics, graphic);
				}
			}
		}
	}

	private void ApplyColor(Color color)
	{
		for (int num = _graphics.Count - 1; num >= 0; num--)
		{
			Graphic graphic = _graphics[num];
			if (graphic == null)
			{
				_graphics.RemoveAt(num);
			}
			else
			{
				int instanceID = graphic.GetInstanceID();
				if (!_originalGraphics.ContainsKey(instanceID))
				{
					_originalGraphics[instanceID] = new GraphicOrig
					{
						g = graphic,
						color = graphic.color
					};
				}
				if (!(graphic.color == color))
				{
					graphic.color = color;
				}
			}
		}
		for (int num2 = _sprites.Count - 1; num2 >= 0; num2--)
		{
			SpriteRenderer spriteRenderer = _sprites[num2];
			if (spriteRenderer == null)
			{
				_sprites.RemoveAt(num2);
			}
			else
			{
				int instanceID2 = spriteRenderer.GetInstanceID();
				if (!_originalSprites.ContainsKey(instanceID2))
				{
					_originalSprites[instanceID2] = new SpriteOrig
					{
						s = spriteRenderer,
						color = spriteRenderer.color
					};
				}
				if (!(spriteRenderer.color == color))
				{
					spriteRenderer.color = color;
				}
			}
		}
		for (int num3 = _renderers.Count - 1; num3 >= 0; num3--)
		{
			Renderer renderer = _renderers[num3];
			if (renderer == null)
			{
				_renderers.RemoveAt(num3);
			}
			else
			{
				ApplyRendererColor(renderer, color);
			}
		}
	}

	private static void ApplyRendererColor(Renderer renderer, Color color)
	{
		try
		{
			Il2CppReferenceArray<Material> sharedMaterials = renderer.sharedMaterials;
			int length = ((Il2CppArrayBase<Material>)sharedMaterials).Length;
			for (int i = 0; i < length; i++)
			{
				Material material = ((Il2CppArrayBase<Material>)sharedMaterials)[i];
				if (material != null)
				{
					ApplyMaterialColor(material, color);
				}
			}
		}
		catch
		{
		}
		try
		{
			LineRenderer lineRenderer = renderer.TryCast<LineRenderer>();
			if (lineRenderer != null)
			{
				lineRenderer.startColor = color;
				lineRenderer.endColor = color;
			}
		}
		catch
		{
		}
	}

	private static void ApplyMaterialColor(Material material, Color color)
	{
		try
		{
			if (material.color == color)
			{
				return;
			}
		}
		catch
		{
		}
		for (int i = 0; i < ColorPropertyNames.Length; i++)
		{
			string prop = ColorPropertyNames[i];
			try
			{
				if (material.HasProperty(prop))
				{
					_originalMaterials.Add(new MaterialOrig
					{
						m = material,
						prop = prop,
						color = material.GetColor(prop)
					});
					material.SetColor(prop, color);
				}
			}
			catch
			{
			}
		}
		try
		{
			material.color = color;
		}
		catch
		{
		}
	}

	public static void RestoreOriginalsNow()
	{
		bool isEnabled = false;
		try
		{
			foreach (KeyValuePair<int, GraphicOrig> originalGraphic in _originalGraphics)
			{
				GraphicOrig value = originalGraphic.Value;
				if (!(value.g == null))
				{
					try
					{
						value.g.color = value.color;
					}
					catch
					{
					}
				}
			}
			foreach (KeyValuePair<int, SpriteOrig> originalSprite in _originalSprites)
			{
				SpriteOrig value2 = originalSprite.Value;
				if (!(value2.s == null))
				{
					try
					{
						value2.s.color = value2.color;
					}
					catch
					{
					}
				}
			}
			for (int num = _originalMaterials.Count - 1; num >= 0; num--)
			{
				MaterialOrig materialOrig = _originalMaterials[num];
				if (!(materialOrig.m == null))
				{
					try
					{
						if (materialOrig.m.HasProperty(materialOrig.prop))
						{
							materialOrig.m.SetColor(materialOrig.prop, materialOrig.color);
						}
					}
					catch
					{
					}
				}
			}
			if (!PluginConfig.IsDebugEnabled)
			{
				return;
			}
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(55, 3, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[HoloReticleColorApplier] RestoreOriginalsNow: g=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_originalGraphics.Count);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" s=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_originalSprites.Count);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" m=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_originalMaterials.Count);
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance2 = CBSafeColorsPlugin.Instance;
			if (instance2 == null)
			{
				return;
			}
			ManualLogSource log2 = instance2.Log;
			if (log2 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(47, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HoloReticleColorApplier] RestoreOriginalsNow: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static bool LooksLikeCenterGraphic(Graphic graphic)
	{
		string text = SafeLower(graphic.name);
		if (!text.Contains("dot") && !text.Contains("reticle") && !text.Contains("center") && !text.Contains("cross") && !text.Contains("point"))
		{
			return false;
		}
		try
		{
			Rect rect = graphic.rectTransform.rect;
			float num = Mathf.Abs(rect.width);
			float num2 = Mathf.Abs(rect.height);
			return num > 0.5f && num2 > 0.5f && num <= 32f && num2 <= 32f && Mathf.Abs(num - num2) < 10f;
		}
		catch
		{
			return false;
		}
	}

	private static void AddUnique<T>(List<T> list, T item) where T : class
	{
		if (item != null && !list.Contains(item))
		{
			list.Add(item);
		}
	}

	private static void CleanupNulls<T>(List<T> list) where T : class
	{
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (list[num] == null)
			{
				list.RemoveAt(num);
			}
		}
	}

	private static string SafeLower(string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			return value.ToLowerInvariant();
		}
		return string.Empty;
	}
}
