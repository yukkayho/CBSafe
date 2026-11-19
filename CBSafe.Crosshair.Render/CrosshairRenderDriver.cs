using System;
using System.Collections.Generic;
using System.Reflection;
using CBSafe.Crosshair.Infrastructure;
using Il2CppSystem;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Crosshair.Render;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public static class CrosshairRenderDriver
{
	private const float PixelScale = 2.5f;

	private const float OutlineThickness = 2f;

	private const int SortingOrderTop = 9999;

	private const int SortingOrderBehindUI = -32000;

	private static Texture2D _whiteTex;

	private static GameObject _canvasGO;

	private static Canvas _canvas;

	private static int _currentSortingOrder = int.MinValue;

	private static readonly List<RawImage> _parts = new List<RawImage>();

	private static readonly List<CrosshairRect> _rects = new List<CrosshairRect>();

	private static float _spread;

	private static bool _errorLogged;

	private static bool _dynVisDiagLogged;

	private static int _dynVisDiagFrames;

	private static string _dynVisLastDiag = "";

	private static Color _cachedMainColor;

	private static Color _cachedOutlineColor;

	private static string _lastMainColorIdx = "";

	private static string _lastOutlineColorIdx = "";

	private static string _appliedMainColorIdx;

	private static string _appliedOutlineColorIdx;

	private static float _dynVisCacheUntil;

	private static bool _dynVisCachedResult;

	private static int _layoutFingerprint = int.MinValue;

	private static Texture2D WhiteTex
	{
		get
		{
			if (_whiteTex != null)
			{
				return _whiteTex;
			}
			_whiteTex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					_whiteTex.SetPixel(i, j, Color.white);
				}
			}
			_whiteTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
			_whiteTex.filterMode = FilterMode.Point;
			_whiteTex.hideFlags = HideFlags.HideAndDontSave;
			return _whiteTex;
		}
	}

	public static void InvalidateColorCache()
	{
		_lastMainColorIdx = "";
		_lastOutlineColorIdx = "";
		_appliedMainColorIdx = "";
		_appliedOutlineColorIdx = "";
		_layoutFingerprint = int.MinValue;
	}

	public static void InvalidateLayout()
	{
		_layoutFingerprint = int.MinValue;
		_appliedMainColorIdx = "";
		_appliedOutlineColorIdx = "";
		_dynVisCacheUntil = 0f;
		_spread = 0f;
	}

	public static void Tick()
	{
		CrosshairConfig current = CrosshairConfigStore.Current;
		if (current == null || !current.Enabled)
		{
			if (_canvasGO != null)
			{
				HideAll();
			}
			return;
		}
		EnsureCanvas();
		if (_canvasGO == null)
		{
			return;
		}
		bool flag = false;
		try
		{
			flag = Cursor.visible;
		}
		catch
		{
		}
		ApplySortingOrder(flag ? (-32000) : 9999);
		if (current.DynamicVisibility && HeldItemHasNativeCrosshair())
		{
			HideAll();
			return;
		}
		float target = 0f;
		int num;
		if (current.DynamicGap)
		{
			num = (IsPlayerMovingOrShooting() ? 1 : 0);
			if (num != 0)
			{
				target = Mathf.Max(2f, current.Gap * 1.5f);
			}
		}
		else
		{
			num = 0;
		}
		_spread = Mathf.MoveTowards(_spread, target, 30f * Time.deltaTime);
		float scale = 2.5f;
		CrosshairRenderMath.BuildPixelRects(current, _spread, scale, _rects, out var hasDot, out var dotRect);
		if (num == 0 && Mathf.Abs(_spread) < 0.05f)
		{
			int num2 = BuildLayoutFingerprint(current, hasDot, _rects.Count);
			bool flag2 = current.MainColorString != _appliedMainColorIdx || current.OutlineColorString != _appliedOutlineColorIdx;
			if (num2 == _layoutFingerprint && !flag2)
			{
				return;
			}
			_layoutFingerprint = num2;
		}
		else
		{
			_layoutFingerprint = int.MinValue;
		}
		Color cachedMainColor = GetCachedMainColor(current);
		Color cachedOutlineColor = GetCachedOutlineColor(current);
		int num3 = 0;
		float num4 = CrosshairRenderMath.OutlinePx(scale);
		if (current.OutlineEnabled)
		{
			foreach (CrosshairRect rect in _rects)
			{
				CrosshairRect crosshairRect = CrosshairRenderMath.ExpandOutlineOutward(rect, num4);
				SetPart(num3++, crosshairRect.X, crosshairRect.Y, crosshairRect.Width, crosshairRect.Height, cachedOutlineColor);
			}
			if (hasDot)
			{
				int diameterPx = CrosshairRenderMath.EvenSize(Mathf.RoundToInt(dotRect.Width) + Mathf.RoundToInt(num4 * 2f));
				SetDotPart(num3++, diameterPx, cachedOutlineColor);
			}
		}
		foreach (CrosshairRect rect2 in _rects)
		{
			SetPart(num3++, rect2.X, rect2.Y, rect2.Width, rect2.Height, cachedMainColor);
		}
		if (hasDot)
		{
			SetDotPart(num3++, Mathf.RoundToInt(dotRect.Width), cachedMainColor);
		}
		for (int i = num3; i < _parts.Count; i++)
		{
			if (_parts[i] != null && _parts[i].gameObject != null)
			{
				_parts[i].gameObject.SetActive(value: false);
			}
		}
		_appliedMainColorIdx = current.MainColorString;
		_appliedOutlineColorIdx = current.OutlineColorString;
	}

	private static void EnsureCanvas()
	{
		if (_canvasGO != null)
		{
			return;
		}
		try
		{
			_canvasGO = new GameObject("CBSafe_Crosshair_OverlayCanvas");
			UnityEngine.Object.DontDestroyOnLoad(_canvasGO);
			_canvasGO.layer = 5;
			_canvas = _canvasGO.AddComponent<Canvas>();
			_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			_canvas.sortingOrder = 9999;
			_currentSortingOrder = 9999;
			CanvasScaler canvasScaler = _canvasGO.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
			canvasScaler.scaleFactor = 1f;
			Log("[RenderDriver] Canvas created OK (layer=5, sort=" + 9999 + ")");
		}
		catch (System.Exception ex)
		{
			LogError("[RenderDriver] Canvas create failed: " + ex);
			if (_canvasGO != null)
			{
				UnityEngine.Object.Destroy(_canvasGO);
			}
			_canvasGO = null;
		}
	}

	private static RawImage GetOrCreatePart(int index)
	{
		while (_parts.Count <= index)
		{
			GameObject gameObject = new GameObject("CrosshairPart_" + _parts.Count);
			gameObject.layer = 5;
			gameObject.transform.SetParent(_canvasGO.transform, worldPositionStays: false);
			RawImage rawImage = gameObject.AddComponent<RawImage>();
			rawImage.texture = WhiteTex;
			rawImage.raycastTarget = false;
			RectTransform rectTransform = rawImage.rectTransform;
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			_parts.Add(rawImage);
		}
		return _parts[index];
	}

	private static void SetPart(int index, float x, float y, float w, float h, Color c)
	{
		CrosshairRenderMath.SnapSymmetricRect(x, y, w, h, out var cx, out var cy, out var wi, out var hi);
		RawImage orCreatePart = GetOrCreatePart(index);
		orCreatePart.gameObject.SetActive(value: true);
		orCreatePart.texture = WhiteTex;
		orCreatePart.color = c;
		RectTransform rectTransform = orCreatePart.rectTransform;
		rectTransform.anchoredPosition = new Vector2(cx, cy);
		rectTransform.sizeDelta = new Vector2(wi, hi);
	}

	private static void SetDotPart(int index, int diameterPx, Color c)
	{
		int num = CrosshairRenderMath.EvenSize(diameterPx);
		RawImage orCreatePart = GetOrCreatePart(index);
		orCreatePart.gameObject.SetActive(value: true);
		orCreatePart.texture = CrosshairTextures.Circle;
		orCreatePart.color = c;
		RectTransform rectTransform = orCreatePart.rectTransform;
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.sizeDelta = new Vector2(num, num);
	}

	private static void HideAll()
	{
		_layoutFingerprint = int.MinValue;
		_appliedMainColorIdx = "";
		_appliedOutlineColorIdx = "";
		for (int i = 0; i < _parts.Count; i++)
		{
			if (_parts[i] != null && _parts[i].gameObject != null)
			{
				_parts[i].gameObject.SetActive(value: false);
			}
		}
	}

	private static void ApplySortingOrder(int order)
	{
		if (_canvas == null || order == _currentSortingOrder)
		{
			return;
		}
		try
		{
			_canvas.sortingOrder = order;
			_currentSortingOrder = order;
		}
		catch
		{
		}
	}

	private static bool HeldItemHasNativeCrosshair()
	{
		if (Time.unscaledTime < _dynVisCacheUntil)
		{
			return _dynVisCachedResult;
		}
		_dynVisCacheUntil = Time.unscaledTime + 0.1f;
		string text = "ok";
		string text2 = "-";
		bool flag = false;
		bool flag2 = false;
		try
		{
			BasePlayer entity = LocalPlayer.Entity;
			if (entity == null)
			{
				text = "player-null";
				return _dynVisCachedResult = false;
			}
			HeldEntity heldEntity = entity.GetHeldEntity();
			if (heldEntity == null)
			{
				text = "held-null";
				return _dynVisCachedResult = false;
			}
			BaseProjectile baseProjectile = heldEntity.TryCast<BaseProjectile>();
			flag = baseProjectile != null;
			if (!flag)
			{
				text2 = GetRuntimeTypeName((UnityEngine.Object)heldEntity, heldEntity.GetType().Name);
				text = "not-ranged";
				return _dynVisCachedResult = false;
			}
			text2 = GetRuntimeTypeName((UnityEngine.Object)heldEntity, baseProjectile.GetType().Name);
			if (IsBowOrCrossbow(heldEntity, text2))
			{
				text = "bow-or-crossbow";
				return _dynVisCachedResult = false;
			}
			try
			{
				flag2 = baseProjectile.aiming;
			}
			catch
			{
				flag2 = false;
			}
			if (!flag2)
			{
				try
				{
					flag2 = entity.IsAiming;
				}
				catch
				{
					flag2 = false;
				}
			}
			if (!flag2)
			{
				text = "not-aiming";
				return _dynVisCachedResult = false;
			}
			if (!_dynVisDiagLogged)
			{
				_dynVisDiagLogged = true;
				Log("[RenderDriver] DynVis triggered: held=" + text2 + " aiming=true");
			}
			return _dynVisCachedResult = true;
		}
		catch (System.Exception ex)
		{
			text = "exception:" + ex.GetType().Name + ":" + ex.Message;
			return _dynVisCachedResult = false;
		}
		finally
		{
			if (++_dynVisDiagFrames >= 120)
			{
				_dynVisDiagFrames = 0;
				string text3 = "held=" + text2 + " ranged=" + flag + " aiming=" + flag2 + " reason=" + text;
				if (text3 != _dynVisLastDiag)
				{
					_dynVisLastDiag = text3;
					Log("[DynVis Diag] " + text3);
				}
			}
		}
	}

	private static string GetRuntimeTypeName(Il2CppSystem.Object obj, string fallback)
	{
		try
		{
			Il2CppSystem.Type type = obj?.GetIl2CppType();
			if (type != null)
			{
				string name = type.Name;
				if (!string.IsNullOrEmpty(name))
				{
					return name;
				}
				return fallback;
			}
			return fallback;
		}
		catch
		{
			return fallback;
		}
	}

	private static bool IsBowOrCrossbow(HeldEntity held, string heldName)
	{
		try
		{
			Il2CppSystem.Type type = held.GetIl2CppType();
			while (type != null)
			{
				switch (type.Name)
				{
				case "CompoundBowWeapon":
				case "CrossbowWeapon":
				case "BowWeapon":
				case "Crossbow":
				case "BaseBow":
				case "BowGun":
				case "HuntingBow":
					return true;
				case "BaseProjectile":
					goto end_IL_029b;
				case "BaseMelee":
					goto end_IL_029b;
				case "AttackEntity":
					goto end_IL_029b;
				case "HeldEntity":
					goto end_IL_029b;
				case "BaseCombatEntity":
					goto end_IL_029b;
				case "BaseEntity":
					goto end_IL_029b;
				case "BaseNetworkable":
					goto end_IL_029b;
				case "Object":
					goto end_IL_029b;
				case "MonoBehaviour":
					goto end_IL_029b;
				case "Behaviour":
					goto end_IL_029b;
				case "Component":
					goto end_IL_029b;
				}
				type = type.BaseType;
				continue;
				end_IL_029b:
				break;
			}
		}
		catch
		{
		}
		try
		{
			string shortPrefabName = held.ShortPrefabName;
			if (!string.IsNullOrEmpty(shortPrefabName))
			{
				string text = shortPrefabName.ToLowerInvariant();
				if (text.Contains("bow") || text.Contains("crossbow"))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		if (!string.IsNullOrEmpty(heldName))
		{
			string text2 = heldName.ToLowerInvariant();
			if (text2.Contains("bow") || text2.Contains("crossbow"))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsPlayerMovingOrShooting()
	{
		try
		{
			return Input.GetMouseButton(0) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
		}
		catch
		{
			return false;
		}
	}

	private static Color GetCachedMainColor(CrosshairConfig cfg)
	{
		if (cfg.MainColorString != _lastMainColorIdx)
		{
			_lastMainColorIdx = cfg.MainColorString;
			_cachedMainColor = cfg.MainColor;
		}
		return _cachedMainColor;
	}

	private static Color GetCachedOutlineColor(CrosshairConfig cfg)
	{
		if (cfg.OutlineColorString != _lastOutlineColorIdx)
		{
			_lastOutlineColorIdx = cfg.OutlineColorString;
			_cachedOutlineColor = cfg.OutlineColor;
		}
		return _cachedOutlineColor;
	}

	public static void LogOnce(string msg)
	{
		if (!_errorLogged)
		{
			_errorLogged = true;
			LogError(msg);
		}
	}

	private static void Log(string msg)
	{
		try
		{
			CBSafeCrosshairPlugin.Instance?.Log.LogInfo(msg);
		}
		catch
		{
		}
	}

	private static void LogError(string msg)
	{
		try
		{
			CBSafeCrosshairPlugin.Instance?.Log.LogError(msg);
		}
		catch
		{
		}
	}

	private static int BuildLayoutFingerprint(CrosshairConfig cfg, bool hasDot, int rectCount)
	{
		return ((((((((527 + (cfg.MainColorString?.GetHashCode() ?? 0)) * 31 + (cfg.OutlineColorString?.GetHashCode() ?? 0)) * 31 + (cfg.OutlineEnabled ? 1 : 0)) * 31 + cfg.StyleIndex) * 31 + (int)(cfg.Gap * 100f)) * 31 + (int)(cfg.Length * 100f)) * 31 + (int)(cfg.Thickness * 100f)) * 31 + (hasDot ? 1 : 0)) * 31 + rectCount;
	}
}
