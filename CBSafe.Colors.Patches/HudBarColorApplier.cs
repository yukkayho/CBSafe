using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.Infrastructure;
using CBSafe.Colors.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CBSafe.Colors.Patches;

[HydraNoObfuscate]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class HudBarColorApplier : MonoBehaviour
{
	private struct ImageOrig
	{
		public Image img;

		public Color color;
	}

	private struct TextOrig
	{
		public Text txt;

		public Color color;
	}

	public enum BarKind
	{
		Unknown,
		Hp,
		Water,
		Food,
		HpRecovery
	}

	internal const float MainBarAlpha = 0.72f;

	internal const float ValueTextTintStrength = 0.5f;

	internal const float RecoveryValueScale = 0.47f;

	internal const float RecoverySatScale = 0.8f;

	internal static Image _hpBar;

	internal static Image _waterBar;

	internal static Image _foodBar;

	internal static Image _hpRecovery;

	private static bool _customActive;

	private static float _nextFullScanTime;

	internal static Color _cachedHpColor;

	internal static Color _cachedWaterColor;

	internal static Color _cachedFoodColor;

	internal static bool _hasHpColor;

	internal static bool _hasWaterColor;

	internal static bool _hasFoodColor;

	private static readonly Dictionary<int, ImageOrig> _originalImageColors = new Dictionary<int, ImageOrig>();

	private static readonly Dictionary<int, TextOrig> _originalTextColors = new Dictionary<int, TextOrig>();

	private static float _nextRescanTime;

	private static float _nextHudConfigRefreshAt;

	private static int _lastSceneHandle;

	private static bool _sceneTrackInitialised;

	private static float _boostUntil;

	private static float _nextBoostApply;

	private const float BoostWindowDuration = 4f;

	private const float BoostApplyInterval = 0.25f;

	private static float _nextHudConfigPollAt;

	private static bool _loggedHp;

	private static bool _loggedWater;

	private static bool _loggedFood;

	private static bool _loggedHpRecovery;

	private const string PrefabSceneName = "PrefabPreProcess - Client";

	public HudBarColorApplier(IntPtr ptr)
		: base(ptr)
	{
	}

	internal static bool IsSessionColorStable()
	{
		if (!_customActive)
		{
			return true;
		}
		if (!(_hpBar != null) && !(_waterBar != null))
		{
			return _foodBar != null;
		}
		return true;
	}

	private void LateUpdate()
	{
		try
		{
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
			if (Time.unscaledTime >= _nextHudConfigPollAt)
			{
				RefreshCachedColors();
				_nextHudConfigPollAt = Time.unscaledTime + (_customActive ? 0.5f : 2.5f);
			}
			if (_customActive)
			{
				bool flag = Time.unscaledTime < _boostUntil;
				if (flag)
				{
					ApplyCachedColors();
				}
				else if (Time.unscaledTime >= _nextHudConfigRefreshAt)
				{
					_nextHudConfigRefreshAt = Time.unscaledTime + 0.5f;
					ApplyCachedColors();
				}
				if (((_hasHpColor && (_hpBar == null || !_hpBar.gameObject.activeInHierarchy)) || (_hasWaterColor && (_waterBar == null || !_waterBar.gameObject.activeInHierarchy)) || (_hasFoodColor && (_foodBar == null || !_foodBar.gameObject.activeInHierarchy))) && (flag || Time.unscaledTime >= _nextRescanTime))
				{
					Rescan();
					_nextRescanTime = Time.unscaledTime + (flag ? 0f : 2.5f);
					ApplyCachedColors();
				}
				if (flag && Time.unscaledTime >= _nextBoostApply)
				{
					ApplyToAllHudElements();
					_nextBoostApply = Time.unscaledTime + 0.25f;
				}
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HudBarColorApplier] LateUpdate: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void OnSceneChanged()
	{
		_hpBar = null;
		_waterBar = null;
		_foodBar = null;
		_hpRecovery = null;
		_loggedHp = (_loggedWater = (_loggedFood = (_loggedHpRecovery = false)));
		_nextRescanTime = 0f;
		_boostUntil = Time.unscaledTime + 4f;
		try
		{
			SystemDropdownFactory.InvalidateRowTemplateCache();
		}
		catch
		{
		}
	}

	internal static void RefreshCachedColors()
	{
		_customActive = PluginConfig.IsCustomActive();
		if (!_customActive)
		{
			_hasHpColor = (_hasWaterColor = (_hasFoodColor = false));
			return;
		}
		_hasHpColor = PluginConfig.TryResolveHpBar(out _cachedHpColor);
		_hasWaterColor = PluginConfig.TryResolveWaterBar(out _cachedWaterColor);
		_hasFoodColor = PluginConfig.TryResolveFoodBar(out _cachedFoodColor);
	}

	internal static void ApplyCachedColors()
	{
		ApplyCachedColors(BarKind.Unknown);
	}

	internal static void ApplyCachedColors(BarKind only)
	{
		if ((only == BarKind.Unknown || only == BarKind.Hp) && _hasHpColor && _hpBar != null)
		{
			ApplyToMainImage(_hpBar, _cachedHpColor);
			if (_hpRecovery != null)
			{
				ApplyToRecoveryImage(_hpRecovery, _cachedHpColor);
			}
		}
		if ((only == BarKind.Unknown || only == BarKind.Water) && _hasWaterColor && _waterBar != null)
		{
			ApplyToMainImage(_waterBar, _cachedWaterColor);
		}
		if ((only == BarKind.Unknown || only == BarKind.Food) && _hasFoodColor && _foodBar != null)
		{
			ApplyToMainImage(_foodBar, _cachedFoodColor);
		}
	}

	internal static void OnHudElementSetValue(HudElement element)
	{
		if (element == null)
		{
			return;
		}
		if (!_customActive)
		{
			RefreshCachedColors();
			if (!_customActive)
			{
				return;
			}
		}
		try
		{
			Il2CppReferenceArray<Image> filledImage = element.FilledImage;
			if (filledImage == null || ((Il2CppArrayBase<Image>)filledImage).Length == 0)
			{
				return;
			}
			BarKind barKind = ClassifyHudElement(element);
			if (barKind == BarKind.Unknown)
			{
				return;
			}
			LogHudHierarchyOnce(barKind, element, filledImage);
			Color color;
			switch (barKind)
			{
			default:
				return;
			case BarKind.HpRecovery:
			{
				if (!_hasHpColor)
				{
					return;
				}
				for (int i = 0; i < ((Il2CppArrayBase<Image>)filledImage).Length; i++)
				{
					Image image = ((Il2CppArrayBase<Image>)filledImage)[i];
					if (!(image == null))
					{
						ApplyToRecoveryImage(image, _cachedHpColor);
						if (_hpRecovery == null)
						{
							_hpRecovery = image;
						}
					}
				}
				return;
			}
			case BarKind.Hp:
				if (!_hasHpColor)
				{
					return;
				}
				color = _cachedHpColor;
				break;
			case BarKind.Water:
				if (!_hasWaterColor)
				{
					return;
				}
				color = _cachedWaterColor;
				break;
			case BarKind.Food:
				if (!_hasFoodColor)
				{
					return;
				}
				color = _cachedFoodColor;
				break;
			}
			int length = ((Il2CppArrayBase<Image>)filledImage).Length;
			int num = length - 1;
			for (int j = 0; j < length; j++)
			{
				Image image2 = ((Il2CppArrayBase<Image>)filledImage)[j];
				if (image2 == null)
				{
					continue;
				}
				if (j == num)
				{
					ApplyToMainImage(image2, color);
					bool flag = IsRuntimeSceneImage(image2);
					if (flag)
					{
						if (barKind == BarKind.Hp && _hpBar == null)
						{
							_hpBar = image2;
						}
						if (barKind == BarKind.Water && _waterBar == null)
						{
							_waterBar = image2;
						}
						if (barKind == BarKind.Food && _foodBar == null)
						{
							_foodBar = image2;
						}
					}
					if (barKind == BarKind.Hp && _hpRecovery == null && flag)
					{
						_hpRecovery = FindPendingHealthImage(image2);
					}
				}
				else
				{
					ApplyToRecoveryImage(image2, color);
				}
			}
			if (barKind == BarKind.Hp && _hpRecovery != null)
			{
				ApplyToRecoveryImage(_hpRecovery, color);
			}
			EnsureValueTextVisible(element, color);
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(43, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HudBarColorApplier] OnHudElementSetValue: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static BarKind ClassifyHudElement(HudElement element)
	{
		return ClassifyTransform((element != null) ? element.transform : null);
	}

	private static BarKind ClassifyTransform(Transform root)
	{
		Transform transform = root;
		int num = 0;
		while (num < 6 && transform != null)
		{
			string text = (transform.name ?? string.Empty).ToLowerInvariant();
			if (text.Contains("pending"))
			{
				return BarKind.HpRecovery;
			}
			if (MatchesWaterSegment(text))
			{
				return BarKind.Water;
			}
			if (MatchesFoodSegment(text))
			{
				return BarKind.Food;
			}
			if (MatchesHealthSegment(text))
			{
				return BarKind.Hp;
			}
			num++;
			transform = transform.parent;
		}
		return BarKind.Unknown;
	}

	private static bool MatchesWaterSegment(string n)
	{
		if (!n.Contains("hudhydration") && !n.Contains("hydration") && !n.Contains("thirst") && !(n == "water"))
		{
			return n.EndsWith(".water", StringComparison.Ordinal);
		}
		return true;
	}

	private static bool MatchesFoodSegment(string n)
	{
		if (!n.Contains("hudcalories") && !n.Contains("calories") && !n.Contains("hunger") && !(n == "food") && !n.EndsWith(".food", StringComparison.Ordinal))
		{
			return n.Contains("nutrition");
		}
		return true;
	}

	private static bool MatchesHealthSegment(string n)
	{
		if (!n.Contains("hudhealth") && !n.Contains("hudhp") && !(n == "hp") && !(n == "health"))
		{
			return n.EndsWith(".health", StringComparison.Ordinal);
		}
		return true;
	}

	internal static void OnExternalSessionChanged()
	{
		_hpBar = null;
		_waterBar = null;
		_foodBar = null;
		_hpRecovery = null;
		_nextRescanTime = 0f;
		_boostUntil = Time.unscaledTime + 4f;
	}

	private static void ApplyToMainImage(Image img, Color baseColor)
	{
		if (img == null)
		{
			return;
		}
		int instanceID = img.GetInstanceID();
		try
		{
			if (!_originalImageColors.ContainsKey(instanceID))
			{
				_originalImageColors[instanceID] = new ImageOrig
				{
					img = img,
					color = img.color
				};
			}
		}
		catch
		{
		}
		Color color = default(Color);
		color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.72f);
		try
		{
			if (img.color != color)
			{
				img.color = color;
			}
		}
		catch
		{
		}
	}

	private static void ApplyToRecoveryImage(Image img, Color mainColor)
	{
		if (img == null)
		{
			return;
		}
		int instanceID = img.GetInstanceID();
		try
		{
			if (!_originalImageColors.ContainsKey(instanceID))
			{
				_originalImageColors[instanceID] = new ImageOrig
				{
					img = img,
					color = img.color
				};
			}
		}
		catch
		{
		}
		Color color = MakeRecoveryColor(mainColor);
		try
		{
			if (img.color != color)
			{
				img.color = color;
			}
		}
		catch
		{
		}
	}

	private static Color MakeRecoveryColor(Color mainColor)
	{
		float H = 0f;
		float S = 0f;
		float V = 0f;
		Color.RGBToHSV(mainColor, out H, out S, out V);
		Color result = Color.HSVToRGB(H, S * 0.8f, V * 0.47f);
		result.a = 0.72f;
		return result;
	}

	private static Image FindPendingHealthImage(Image hpBar)
	{
		if (hpBar == null)
		{
			return null;
		}
		try
		{
			Transform parent = hpBar.transform.parent;
			if (parent == null)
			{
				return null;
			}
			Transform parent2 = parent.parent;
			if (parent2 == null)
			{
				return null;
			}
			int childCount = parent2.childCount;
			for (int i = 0; i < childCount; i++)
			{
				Transform child = parent2.GetChild(i);
				if (child == null || !(child.name ?? string.Empty).ToLowerInvariant().Contains("pending"))
				{
					continue;
				}
				Il2CppArrayBase<Image> componentsInChildren = child.GetComponentsInChildren<Image>(includeInactive: true);
				if (componentsInChildren == null)
				{
					continue;
				}
				int length = componentsInChildren.Length;
				for (int j = 0; j < length; j++)
				{
					Image image = componentsInChildren[j];
					if (!(image == null) && image.type == Image.Type.Filled)
					{
						return image;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static void EnsureValueTextVisible(HudElement element, Color barColor)
	{
		try
		{
			Il2CppReferenceArray<Text> valueText = element.ValueText;
			if (valueText == null)
			{
				return;
			}
			int length = ((Il2CppArrayBase<Text>)valueText).Length;
			if (length == 0)
			{
				return;
			}
			for (int i = 0; i < length; i++)
			{
				Text text = ((Il2CppArrayBase<Text>)valueText)[i];
				if (!(text == null))
				{
					int instanceID = text.GetInstanceID();
					if (!_originalTextColors.ContainsKey(instanceID))
					{
						_originalTextColors[instanceID] = new TextOrig
						{
							txt = text,
							color = text.color
						};
					}
				}
			}
			Color color = default(Color);
			color = new Color(Mathf.Lerp(1f, barColor.r, 0.5f), Mathf.Lerp(1f, barColor.g, 0.5f), Mathf.Lerp(1f, barColor.b, 0.5f), 1f);
			int num = length - 1;
			Transform hudRoot = element.transform;
			for (int j = 0; j < length; j++)
			{
				Text text2 = ((Il2CppArrayBase<Text>)valueText)[j];
				if (text2 == null)
				{
					continue;
				}
				TextOrig value;
				if (j == num)
				{
					if (!ColorsApproximatelyEqual(text2.color, color))
					{
						text2.color = color;
					}
					BringBranchToFront(hudRoot, text2.transform);
				}
				else if (_originalTextColors.TryGetValue(text2.GetInstanceID(), out value) && !ColorsApproximatelyEqual(text2.color, value.color))
				{
					text2.color = value.color;
				}
			}
		}
		catch
		{
		}
	}

	private static void BringBranchToFront(Transform hudRoot, Transform leaf)
	{
		if (leaf == null)
		{
			return;
		}
		int num = 16;
		Transform transform = leaf;
		while (transform != null && num-- > 0)
		{
			try
			{
				transform.SetAsLastSibling();
			}
			catch
			{
			}
			if (!(transform.parent == null) && !(transform.parent == hudRoot))
			{
				transform = transform.parent;
				continue;
			}
			break;
		}
	}

	private static void LogHudHierarchyOnce(BarKind kind, HudElement element, Il2CppReferenceArray<Image> filled)
	{
		if (!PluginConfig.IsDebugEnabled || (kind == BarKind.Hp && _loggedHp) || (kind == BarKind.Water && _loggedWater) || (kind == BarKind.Food && _loggedFood) || (kind == BarKind.HpRecovery && _loggedHpRecovery))
		{
			return;
		}
		if (kind == BarKind.Hp)
		{
			_loggedHp = true;
		}
		if (kind == BarKind.Water)
		{
			_loggedWater = true;
		}
		if (kind == BarKind.Food)
		{
			_loggedFood = true;
		}
		if (kind == BarKind.HpRecovery)
		{
			_loggedHpRecovery = true;
		}
		bool isEnabled = false;
		try
		{
			ManualLogSource manualLogSource = CBSafeColorsPlugin.Instance?.Log;
			if (manualLogSource == null)
			{
				return;
			}
			int num = ((Il2CppArrayBase<Image>)filled)?.Length ?? 0;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(33, 2, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[HudBarDiag] ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(kind);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" FilledImage.Length=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num);
			}
			manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			for (int i = 0; i < num; i++)
			{
				Image image = ((Il2CppArrayBase<Image>)filled)[i];
				if (image == null)
				{
					bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(22, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("  FilledImage[");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(i);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("] = null");
					}
					manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
					continue;
				}
				bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(46, 6, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("  FilledImage[");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(i);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("] name='");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(image.gameObject.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' type=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(image.type);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" fill=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(image.fillAmount, "F2");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" color=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(image.color);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" id=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(image.GetInstanceID());
				}
				manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
			Transform parent = element.transform;
			string text = "";
			int num2 = 0;
			while (num2 < 8 && parent != null)
			{
				text = text + ((num2 > 0) ? " <- " : "") + (parent.name ?? "(null)");
				num2++;
				parent = parent.parent;
			}
			bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(15, 1, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("  ParentChain: ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(text);
			}
			manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			Transform transform = element.transform;
			Transform transform2 = ((transform != null) ? transform.parent : null);
			if (!(transform2 != null))
			{
				return;
			}
			int childCount = transform2.childCount;
			bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(26, 2, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("  Siblings under '");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(transform2.name);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' count=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(childCount);
			}
			manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			for (int j = 0; j < childCount; j++)
			{
				Transform child = transform2.GetChild(j);
				if (!(child == null))
				{
					bool t = child.GetComponent<HudElement>() != null;
					int t2 = child.GetComponentsInChildren<Image>(includeInactive: true)?.Length ?? 0;
					bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(52, 5, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("    sibling[");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(j);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("] name='");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(child.name);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' active=");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(child.gameObject.activeInHierarchy);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" hasHudElement=");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" images=");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t2);
					}
					manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(15, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HudBarDiag] ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(kind);
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral(": ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static bool ColorsApproximatelyEqual(Color a, Color b)
	{
		if (Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f)
		{
			return Mathf.Abs(a.a - b.a) < 0.002f;
		}
		return false;
	}

	private static bool IsUnderGameplayHud(Transform t)
	{
		int num = 0;
		while (num < 10 && t != null)
		{
			string text = (t.name ?? string.Empty).ToLowerInvariant();
			if (text.Contains("menuui") || text.Contains("cbsafe"))
			{
				return false;
			}
			if (text.Contains("vitals") || text.Contains("gameui.hud"))
			{
				return true;
			}
			num++;
			t = t.parent;
		}
		return false;
	}

	private static void Rescan()
	{
		bool isEnabled = false;
		try
		{
			UIHUD instance = SingletonComponent<UIHUD>.Instance;
			if (instance == null)
			{
				return;
			}
			Il2CppArrayBase<Image> componentsInChildren = instance.GetComponentsInChildren<Image>(includeInactive: true);
			if (componentsInChildren == null)
			{
				return;
			}
			Image image = null;
			Image image2 = null;
			Image image3 = null;
			int length = componentsInChildren.Length;
			for (int i = 0; i < length; i++)
			{
				Image image4 = componentsInChildren[i];
				if (image4 == null || !image4.gameObject.activeInHierarchy || !IsRuntimeSceneImage(image4) || !IsUnderGameplayHud(image4.transform))
				{
					continue;
				}
				string text = (image4.gameObject.name ?? string.Empty).ToLowerInvariant();
				if (text != "bar" && text != "fill")
				{
					continue;
				}
				BarKind barKind = ClassifyTransform(image4.transform);
				if (barKind != BarKind.HpRecovery)
				{
					if (image == null && barKind == BarKind.Hp)
					{
						image = image4;
					}
					else if (image2 == null && barKind == BarKind.Water)
					{
						image2 = image4;
					}
					else if (image3 == null && barKind == BarKind.Food)
					{
						image3 = image4;
					}
				}
			}
			if (image != null)
			{
				_hpBar = image;
				if (_hpRecovery == null)
				{
					_hpRecovery = FindPendingHealthImage(image);
				}
			}
			if (image2 != null)
			{
				_waterBar = image2;
			}
			if (image3 != null)
			{
				_foodBar = image3;
			}
			if (!PluginConfig.IsDebugEnabled)
			{
				return;
			}
			CBSafeColorsPlugin instance2 = CBSafeColorsPlugin.Instance;
			if (instance2 == null)
			{
				return;
			}
			ManualLogSource log = instance2.Log;
			if (log != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(45, 3, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[HudBarColorApplier] Rescan: hp=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_hpBar != null);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" water=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_waterBar != null);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" food=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_foodBar != null);
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance3 = CBSafeColorsPlugin.Instance;
			if (instance3 == null)
			{
				return;
			}
			ManualLogSource log2 = instance3.Log;
			if (log2 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(29, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HudBarColorApplier] Rescan: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static bool IsParentNamed(Transform t, params string[] keywords)
	{
		int num = 0;
		while (num < 5 && t != null)
		{
			string text = (t.name ?? string.Empty).ToLowerInvariant();
			for (int i = 0; i < keywords.Length; i++)
			{
				if (text.Contains(keywords[i]))
				{
					return true;
				}
			}
			num++;
			t = t.parent;
		}
		return false;
	}

	private static bool IsRuntimeSceneImage(Image img)
	{
		if (img == null)
		{
			return false;
		}
		GameObject gameObject = img.gameObject;
		if (gameObject == null)
		{
			return false;
		}
		Scene scene = gameObject.scene;
		if (!scene.IsValid())
		{
			return false;
		}
		return (scene.name ?? string.Empty) != "PrefabPreProcess - Client";
	}

	public static void QuickApplyFromConfig(BarKind only = BarKind.Unknown)
	{
		try
		{
			RefreshCachedColors();
			if (_customActive)
			{
				bool num = only == BarKind.Unknown || only == BarKind.Hp;
				bool flag = only == BarKind.Unknown || only == BarKind.Water;
				bool flag2 = only == BarKind.Unknown || only == BarKind.Food;
				if ((num && _hasHpColor && (_hpBar == null || !_hpBar.gameObject.activeInHierarchy)) || (flag && _hasWaterColor && (_waterBar == null || !_waterBar.gameObject.activeInHierarchy)) || (flag2 && _hasFoodColor && (_foodBar == null || !_foodBar.gameObject.activeInHierarchy)))
				{
					Rescan();
				}
				ApplyCachedColors(only);
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(43, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HudBarColorApplier] QuickApplyFromConfig: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	public void ForceReapplyNow()
	{
		try
		{
			QuickApplyFromConfig();
			if (_hpBar == null && _waterBar == null && _foodBar == null)
			{
				if (Time.unscaledTime >= _nextFullScanTime)
				{
					ApplyToAllHudElements();
					_nextFullScanTime = Time.unscaledTime + 30f;
				}
			}
			else
			{
				ApplyCachedColors();
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(38, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HudBarColorApplier] ForceReapplyNow: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void ApplyToAllHudElements()
	{
		if (!_customActive)
		{
			return;
		}
		try
		{
			Il2CppArrayBase<HudElement> il2CppArrayBase = Resources.FindObjectsOfTypeAll<HudElement>();
			if (il2CppArrayBase == null)
			{
				return;
			}
			int length = il2CppArrayBase.Length;
			for (int i = 0; i < length; i++)
			{
				HudElement hudElement = il2CppArrayBase[i];
				if (!(hudElement == null))
				{
					OnHudElementSetValue(hudElement);
				}
			}
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
			int num = 0;
			foreach (KeyValuePair<int, ImageOrig> originalImageColor in _originalImageColors)
			{
				ImageOrig value = originalImageColor.Value;
				if (!(value.img == null))
				{
					try
					{
						value.img.color = value.color;
						num++;
					}
					catch
					{
					}
				}
			}
			int num2 = 0;
			foreach (KeyValuePair<int, TextOrig> originalTextColor in _originalTextColors)
			{
				TextOrig value2 = originalTextColor.Value;
				if (!(value2.txt == null))
				{
					try
					{
						value2.txt.color = value2.color;
						num2++;
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
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(65, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[HudBarColorApplier] RestoreOriginalsNow: restored images=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" texts=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num2);
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(42, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[HudBarColorApplier] RestoreOriginalsNow: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}
}
