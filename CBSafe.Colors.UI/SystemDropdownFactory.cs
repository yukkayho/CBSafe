using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.Infrastructure;
using CBSafe.Core;
using CBSafe.Core.UI;
using Facepunch.Flexbox;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Reflection;
using Rust.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CBSafe.Colors.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
[HydraNoObfuscate]
internal static class SystemDropdownFactory
{
	private struct PaintContext
	{
		public RectTransform ItemsContainer;

		public string PrefabName;

		public Color[] Colors;
	}

	private sealed class AnimState
	{
		public bool TargetOpen;

		public float Progress;
	}

	private struct ScrollRectCacheEntry
	{
		public ScrollRect Scroll;

		public FlexElement Flex;

		public float LastMaxH;

		public bool IsStale()
		{
			try
			{
				return Scroll == null && Flex == null;
			}
			catch
			{
				return true;
			}
		}

		public static ScrollRectCacheEntry Build(RectTransform panel)
		{
			ScrollRectCacheEntry scrollRectCacheEntry = default(ScrollRectCacheEntry);
			scrollRectCacheEntry.LastMaxH = -1f;
			ScrollRectCacheEntry result = scrollRectCacheEntry;
			try
			{
				Il2CppArrayBase<ScrollRect> componentsInChildren = panel.GetComponentsInChildren<ScrollRect>(includeInactive: true);
				if (componentsInChildren != null)
				{
					for (int i = 0; i < componentsInChildren.Length; i++)
					{
						ScrollRect scrollRect = componentsInChildren[i];
						if (!(scrollRect == null))
						{
							FlexElement component = scrollRect.GetComponent<FlexElement>();
							if (component != null)
							{
								result.Scroll = scrollRect;
								result.Flex = component;
								return result;
							}
						}
					}
					return result;
				}
				return result;
			}
			catch
			{
				return result;
			}
		}
	}

	private static GameObject _cachedRowTemplate;

	private static readonly List<(RustButton opener, GameObject panel)> _registered = new List<(RustButton, GameObject)>();

	private static readonly List<DropdownHandle> _handles = new List<DropdownHandle>();

	private static readonly Dictionary<TweakUIDropdown, Image> _rowSwatchByDropdown = new Dictionary<TweakUIDropdown, Image>();

	private static readonly Dictionary<TweakUIDropdown, PaintContext> _paintCtxByDropdown = new Dictionary<TweakUIDropdown, PaintContext>();

	private static readonly Dictionary<RustButton, bool[]> _ownStateByOpener = new Dictionary<RustButton, bool[]>();

	private static readonly Dictionary<RustButton, int[]> _repaintBudgetByOpener = new Dictionary<RustButton, int[]>();

	private static readonly Dictionary<RustButton, AnimState> _animByOpener = new Dictionary<RustButton, AnimState>();

	private const float AnimDurationSec = 0.18f;

	private static readonly Dictionary<int, ScrollRectCacheEntry> _panelScrollCache = new Dictionary<int, ScrollRectCacheEntry>();

	private const int PanelStableSkipAfter = 6;

	private const int MaxVisibleRows = 24;

	private static readonly Dictionary<RustButton, List<Component>> _tooltipsByOpener = new Dictionary<RustButton, List<Component>>();

	private static readonly Dictionary<RustButton, GameObject> _customDismisserByOpener = new Dictionary<RustButton, GameObject>();

	private static readonly List<Transform> _modalRoots = new List<Transform>();

	private static readonly Dictionary<Graphic, bool> _modalRestoreRaycast = new Dictionary<Graphic, bool>();

	private static bool _modalActive;

	private static RustButton _modalActiveOpener;

	private static RectTransform _modalActivePanel;

	private static readonly Dictionary<RustButton, GameObject> _activeTooltipPopupByOpener = new Dictionary<RustButton, GameObject>();

	private static readonly string[] SkipNameKeywords = new string[23]
	{
		"async", "loading", "preset", "оптимизация", "загруз", "antialias", "сглаж", "resolution", "разреш", "quality",
		"качеств", "vignette", "виньет", "bloom", "свечение", "motion", "движение", "blur", "размыт", "cbsafe_crosshair_dropdown_",
		"cbsafe_color_dropdown_", "systemdropdown_", "cbsafe_dropdown_"
	};

	private static readonly Color PanelBgGrey = new Color(13f / 85f, 13f / 85f, 0.12156863f, 1f);

	private static readonly HashSet<RectTransform> _panelDumpDone = new HashSet<RectTransform>();

	private static GameObject _tooltipObjectTemplate;

	private static bool DebugOn => PluginConfig.IsDebugEnabled;

	public static IReadOnlyList<DropdownHandle> Handles => _handles;

	public static bool IsModalActive => _modalActive;

	private static void ApplyAnimProgress(RectTransform panel, Transform arrow, float p)
	{
		if (p < 0f)
		{
			p = 0f;
		}
		else if (p > 1f)
		{
			p = 1f;
		}
		float num = p * p * (3f - 2f * p);
		if (panel != null)
		{
			try
			{
				Vector3 localScale = panel.localScale;
				if (localScale.x != 1f || localScale.y != num || localScale.z != 1f)
				{
					panel.localScale = new Vector3(1f, num, 1f);
				}
				CanvasGroup component = panel.GetComponent<CanvasGroup>();
				if (component != null)
				{
					component.alpha = ((p < 0.05f) ? 0f : num);
					component.blocksRaycasts = p > 0.05f;
					component.interactable = p >= 0.99f;
				}
				for (int i = 0; i < panel.childCount; i++)
				{
					Transform child = panel.GetChild(i);
					if (!(child == null) && !(child.name != LocStrings.Name_Color_Dismisser))
					{
						float num2 = ((num > 0.02f) ? (1f / num) : 0f);
						Vector3 localScale2 = child.localScale;
						if (localScale2.x != 1f || localScale2.y != num2 || localScale2.z != 1f)
						{
							child.localScale = new Vector3(1f, num2, 1f);
						}
						break;
					}
				}
			}
			catch
			{
			}
		}
		if (arrow != null)
		{
			try
			{
				arrow.localEulerAngles = new Vector3(0f, 0f, num * 180f);
			}
			catch
			{
			}
		}
	}

	public static void ClearHandles()
	{
		_handles.Clear();
		ClearModalRoots();
		_cachedRowTemplate = null;
	}

	public static void InvalidateRowTemplateCache()
	{
		_cachedRowTemplate = null;
	}

	public static void RegisterModalRoot(Transform root)
	{
		if (!(root == null) && !_modalRoots.Contains(root))
		{
			_modalRoots.Add(root);
		}
	}

	public static void ClearModalRoots()
	{
		CloseModal();
		_modalRoots.Clear();
	}

	public static void OpenModal(RustButton activeOpener, RectTransform activePanel)
	{
		if (_modalActive)
		{
			CloseModal();
		}
		_modalActive = true;
		_modalActiveOpener = activeOpener;
		_modalActivePanel = activePanel;
		bool isEnabled = false;
		try
		{
			Transform transform = ((activeOpener != null) ? activeOpener.transform : null);
			Transform transform2 = ((activePanel != null) ? activePanel.transform : null);
			for (int i = 0; i < _modalRoots.Count; i++)
			{
				Transform transform3 = _modalRoots[i];
				if (transform3 == null)
				{
					continue;
				}
				Il2CppArrayBase<Graphic> componentsInChildren = transform3.GetComponentsInChildren<Graphic>(includeInactive: true);
				if (componentsInChildren == null)
				{
					continue;
				}
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					Graphic graphic = componentsInChildren[j];
					if (!(graphic == null) && (!(transform != null) || !IsDescendantOf(graphic.transform, transform)) && (!(transform2 != null) || !IsDescendantOf(graphic.transform, transform2)) && graphic.raycastTarget)
					{
						_modalRestoreRaycast[graphic] = true;
						try
						{
							graphic.raycastTarget = false;
						}
						catch
						{
						}
					}
				}
			}
			if (!DebugOn)
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
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(65, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] Modal opened: blocked ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(_modalRestoreRaycast.Count);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" Graphic raycast target(s)");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (System.Exception ex)
		{
			CBSafeColorsPlugin instance2 = CBSafeColorsPlugin.Instance;
			if (instance2 == null)
			{
				return;
			}
			ManualLogSource log2 = instance2.Log;
			if (log2 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(28, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] OpenModal: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	public static void CloseModal()
	{
		if (!_modalActive && _modalRestoreRaycast.Count == 0)
		{
			return;
		}
		try
		{
			foreach (KeyValuePair<Graphic, bool> item in _modalRestoreRaycast)
			{
				if (!(item.Key == null))
				{
					try
					{
						item.Key.raycastTarget = item.Value;
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
		_modalRestoreRaycast.Clear();
		_modalActive = false;
		_modalActiveOpener = null;
		_modalActivePanel = null;
	}

	private static bool IsDescendantOf(Transform t, Transform ancestor)
	{
		if (t == null || ancestor == null)
		{
			return false;
		}
		Transform transform = t;
		while (transform != null)
		{
			if (transform == ancestor)
			{
				return true;
			}
			transform = transform.parent;
		}
		return false;
	}

	public static void CloseAllOpenPanels()
	{
		foreach (var item in _registered)
		{
			try
			{
				if (item.opener != null)
				{
					item.opener.SetToggleFalse();
				}
			}
			catch
			{
			}
			if (item.panel != null)
			{
				item.panel.SetActive(value: false);
			}
			if (item.opener != null)
			{
				if (_ownStateByOpener.TryGetValue(item.opener, out var value))
				{
					value[0] = false;
				}
				if (_animByOpener.TryGetValue(item.opener, out var value2))
				{
					value2.TargetOpen = false;
					value2.Progress = 0f;
				}
				HideDropdownTooltip(item.opener);
			}
		}
		CloseModal();
	}

	public static void RequestAnimatedCloseExcept(RustButton exceptOpener)
	{
		foreach (var item in _registered)
		{
			if (!(item.opener == null) && !(item.opener == exceptOpener))
			{
				if (_animByOpener.TryGetValue(item.opener, out var value))
				{
					value.TargetOpen = false;
				}
				if (_ownStateByOpener.TryGetValue(item.opener, out var value2))
				{
					value2[0] = false;
				}
				HideDropdownTooltip(item.opener);
			}
		}
		if (exceptOpener == null)
		{
			CloseModal();
		}
	}

	public static void ResetAllDropdowns()
	{
		foreach (var item in _registered)
		{
			if (item.opener != null)
			{
				try
				{
					item.opener.SetToggleFalse();
				}
				catch
				{
				}
				try
				{
					item.opener.Value = false;
				}
				catch
				{
				}
				if (_ownStateByOpener.TryGetValue(item.opener, out var value))
				{
					value[0] = false;
				}
				if (_animByOpener.TryGetValue(item.opener, out var value2))
				{
					value2.TargetOpen = false;
					value2.Progress = 0f;
				}
				HideDropdownTooltip(item.opener);
			}
			if (item.panel != null)
			{
				item.panel.SetActive(value: false);
			}
		}
		CloseModal();
	}

	public static bool IsAnyDropdownOpen()
	{
		foreach (var item in _registered)
		{
			if (!(item.opener == null))
			{
				if (_ownStateByOpener.TryGetValue(item.opener, out var value) && value[0])
				{
					return true;
				}
				if (_animByOpener.TryGetValue(item.opener, out var value2) && (value2.TargetOpen || value2.Progress > 0.01f))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void RequestAnimatedClose()
	{
		CloseModal();
		foreach (var item in _registered)
		{
			if (!(item.opener == null))
			{
				if (_animByOpener.TryGetValue(item.opener, out var value))
				{
					value.TargetOpen = false;
				}
				if (_ownStateByOpener.TryGetValue(item.opener, out var value2))
				{
					value2[0] = false;
				}
				HideDropdownTooltip(item.opener);
			}
		}
	}

	private static void DumpTooltipApi(Component comp, ManualLogSource log)
	{
		if (comp == null || log == null)
		{
			return;
		}
		bool isEnabled = false;
		try
		{
			Il2CppSystem.Type il2CppType = comp.GetIl2CppType();
			if (DebugOn)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(45, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown][diag] Tooltip type: ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(il2CppType.FullName);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" (base=");
					Il2CppSystem.Type baseType = il2CppType.BaseType;
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(((baseType != null) ? baseType.FullName : null) ?? "null");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(")");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
			HashSet<string> hashSet = new HashSet<string>();
			Il2CppSystem.Type type = il2CppType;
			int num = 0;
			while (type != null && num < 4)
			{
				try
				{
					Il2CppReferenceArray<Il2CppSystem.Reflection.MethodInfo> methods = type.GetMethods();
					if (methods != null)
					{
						for (int i = 0; i < ((Il2CppArrayBase<Il2CppSystem.Reflection.MethodInfo>)methods).Length; i++)
						{
							Il2CppSystem.Reflection.MethodInfo methodInfo = ((Il2CppArrayBase<Il2CppSystem.Reflection.MethodInfo>)methods)[i];
							if (methodInfo == null)
							{
								continue;
							}
							string text;
							try
							{
								Il2CppReferenceArray<Il2CppSystem.Reflection.ParameterInfo> parameters = methodInfo.GetParameters();
								string[] array = new string[((Il2CppArrayBase<Il2CppSystem.Reflection.ParameterInfo>)parameters)?.Length ?? 0];
								for (int j = 0; j < array.Length; j++)
								{
									int num2 = j;
									Il2CppSystem.Type parameterType = ((Il2CppArrayBase<Il2CppSystem.Reflection.ParameterInfo>)parameters)[j].ParameterType;
									array[num2] = ((parameterType != null) ? parameterType.Name : null) ?? "?";
								}
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 3);
								Il2CppSystem.Type returnType = methodInfo.ReturnType;
								defaultInterpolatedStringHandler.AppendFormatted(((returnType != null) ? returnType.Name : null) ?? "?");
								defaultInterpolatedStringHandler.AppendLiteral(" ");
								defaultInterpolatedStringHandler.AppendFormatted(methodInfo.Name);
								defaultInterpolatedStringHandler.AppendLiteral("(");
								defaultInterpolatedStringHandler.AppendFormatted(string.Join(",", array));
								defaultInterpolatedStringHandler.AppendLiteral(")");
								text = defaultInterpolatedStringHandler.ToStringAndClear();
							}
							catch
							{
								text = methodInfo.Name;
							}
							if (hashSet.Add(text) && DebugOn)
							{
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(31, 2, out isEnabled);
								if (isEnabled)
								{
									bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown][diag]   M @ ");
									bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(type.Name);
									bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(": ");
									bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(text);
								}
								log.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
							}
						}
					}
				}
				catch (System.Exception ex)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(39, 2, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown][diag] method dump @ ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted((type != null) ? type.Name : null);
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral(": ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
				type = type.BaseType;
				num++;
			}
			try
			{
				Il2CppReferenceArray<Il2CppSystem.Reflection.FieldInfo> fields = il2CppType.GetFields();
				if (fields != null)
				{
					for (int k = 0; k < ((Il2CppArrayBase<Il2CppSystem.Reflection.FieldInfo>)fields).Length; k++)
					{
						Il2CppSystem.Reflection.FieldInfo fieldInfo = ((Il2CppArrayBase<Il2CppSystem.Reflection.FieldInfo>)fields)[k];
						if (!(fieldInfo == null) && DebugOn)
						{
							BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler3 = new BepInExInfoLogInterpolatedStringHandler(29, 2, out isEnabled);
							if (isEnabled)
							{
								bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown][diag]   F: ");
								Il2CppSystem.Type fieldType = fieldInfo.FieldType;
								bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(((fieldType != null) ? fieldType.Name : null) ?? "?");
								bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" ");
								bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(fieldInfo.Name);
							}
							log.LogInfo(bepInExInfoLogInterpolatedStringHandler3);
						}
					}
				}
			}
			catch (System.Exception ex2)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(35, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown][diag] field dump: ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
			}
			try
			{
				Il2CppSystem.Type type2 = Il2CppSystem.Type.GetType("Rust.UI.TooltipController, Rust.UI") ?? Il2CppSystem.Type.GetType("TooltipController, Rust.UI") ?? Il2CppSystem.Type.GetType("Rust.UI.TooltipController");
				if (type2 != null)
				{
					if (DebugOn)
					{
						BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler4 = new BepInExInfoLogInterpolatedStringHandler(47, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExInfoLogInterpolatedStringHandler4.AppendLiteral("[SystemDropdown][diag] TooltipController type: ");
							bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(type2.FullName);
						}
						log.LogInfo(bepInExInfoLogInterpolatedStringHandler4);
					}
					Il2CppReferenceArray<Il2CppSystem.Reflection.MethodInfo> methods2 = type2.GetMethods();
					HashSet<string> hashSet2 = new HashSet<string>();
					if (methods2 != null)
					{
						for (int l = 0; l < ((Il2CppArrayBase<Il2CppSystem.Reflection.MethodInfo>)methods2).Length; l++)
						{
							Il2CppSystem.Reflection.MethodInfo methodInfo2 = ((Il2CppArrayBase<Il2CppSystem.Reflection.MethodInfo>)methods2)[l];
							if (methodInfo2 == null)
							{
								continue;
							}
							string text2;
							try
							{
								Il2CppReferenceArray<Il2CppSystem.Reflection.ParameterInfo> parameters2 = methodInfo2.GetParameters();
								string[] array2 = new string[((Il2CppArrayBase<Il2CppSystem.Reflection.ParameterInfo>)parameters2)?.Length ?? 0];
								for (int m = 0; m < array2.Length; m++)
								{
									int num3 = m;
									Il2CppSystem.Type parameterType2 = ((Il2CppArrayBase<Il2CppSystem.Reflection.ParameterInfo>)parameters2)[m].ParameterType;
									array2[num3] = ((parameterType2 != null) ? parameterType2.Name : null) ?? "?";
								}
								DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(3, 3);
								Il2CppSystem.Type returnType2 = methodInfo2.ReturnType;
								defaultInterpolatedStringHandler2.AppendFormatted(((returnType2 != null) ? returnType2.Name : null) ?? "?");
								defaultInterpolatedStringHandler2.AppendLiteral(" ");
								defaultInterpolatedStringHandler2.AppendFormatted(methodInfo2.Name);
								defaultInterpolatedStringHandler2.AppendLiteral("(");
								defaultInterpolatedStringHandler2.AppendFormatted(string.Join(",", array2));
								defaultInterpolatedStringHandler2.AppendLiteral(")");
								text2 = defaultInterpolatedStringHandler2.ToStringAndClear();
							}
							catch
							{
								text2 = methodInfo2.Name;
							}
							if (hashSet2.Add(text2) && DebugOn)
							{
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler5 = new BepInExInfoLogInterpolatedStringHandler(31, 1, out isEnabled);
								if (isEnabled)
								{
									bepInExInfoLogInterpolatedStringHandler5.AppendLiteral("[SystemDropdown][diag]   TC M: ");
									bepInExInfoLogInterpolatedStringHandler5.AppendFormatted(text2);
								}
								log.LogInfo(bepInExInfoLogInterpolatedStringHandler5);
							}
						}
					}
					Il2CppReferenceArray<Il2CppSystem.Reflection.FieldInfo> fields2 = type2.GetFields();
					if (fields2 == null)
					{
						return;
					}
					for (int n = 0; n < ((Il2CppArrayBase<Il2CppSystem.Reflection.FieldInfo>)fields2).Length; n++)
					{
						Il2CppSystem.Reflection.FieldInfo fieldInfo2 = ((Il2CppArrayBase<Il2CppSystem.Reflection.FieldInfo>)fields2)[n];
						if (!(fieldInfo2 == null) && DebugOn)
						{
							BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler6 = new BepInExInfoLogInterpolatedStringHandler(32, 2, out isEnabled);
							if (isEnabled)
							{
								bepInExInfoLogInterpolatedStringHandler6.AppendLiteral("[SystemDropdown][diag]   TC F: ");
								Il2CppSystem.Type fieldType2 = fieldInfo2.FieldType;
								bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(((fieldType2 != null) ? fieldType2.Name : null) ?? "?");
								bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(" ");
								bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(fieldInfo2.Name);
							}
							log.LogInfo(bepInExInfoLogInterpolatedStringHandler6);
						}
					}
				}
				else if (DebugOn)
				{
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler7 = new BepInExInfoLogInterpolatedStringHandler(70, 0, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler7.AppendLiteral("[SystemDropdown][diag] TooltipController type not found by name lookup");
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler7);
				}
			}
			catch (System.Exception ex3)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler3 = new BepInExWarningLogInterpolatedStringHandler(47, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown][diag] TooltipController dump: ");
					bepInExWarningLogInterpolatedStringHandler3.AppendFormatted(ex3.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler3);
			}
		}
		catch (System.Exception ex4)
		{
			BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler4 = new BepInExWarningLogInterpolatedStringHandler(39, 1, out isEnabled);
			if (isEnabled)
			{
				bepInExWarningLogInterpolatedStringHandler4.AppendLiteral("[SystemDropdown][diag] DumpTooltipApi: ");
				bepInExWarningLogInterpolatedStringHandler4.AppendFormatted(ex4.Message);
			}
			log.LogWarning(bepInExWarningLogInterpolatedStringHandler4);
		}
	}

	private static void InvokeTooltipMethodNoArg(Component comp, string methodName)
	{
		if (comp == null)
		{
			return;
		}
		try
		{
			Il2CppSystem.Type il2CppType = comp.GetIl2CppType();
			if (!(il2CppType == null))
			{
				Il2CppSystem.Reflection.MethodInfo method = il2CppType.GetMethod(methodName);
				if (!(method == null))
				{
					method.Invoke((UnityEngine.Object)comp, new Il2CppReferenceArray<Il2CppSystem.Object>(0L));
				}
			}
		}
		catch (System.Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance != null)
			{
				ManualLogSource log = instance.Log;
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(27, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] tooltip.");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(methodName);
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral(": ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static bool IsTooltipOpen(Component comp)
	{
		if (comp == null)
		{
			return false;
		}
		try
		{
			return Tooltip.Current != null;
		}
		catch
		{
			return false;
		}
	}

	private static void ShowDropdownTooltip_Direct(RustButton opener)
	{
		if (opener == null || !_tooltipsByOpener.TryGetValue(opener, out var value) || value == null || value.Count == 0)
		{
			return;
		}
		try
		{
			GameObject current = Tooltip.Current;
			if ((_activeTooltipPopupByOpener.TryGetValue(opener, out var value2) && value2 != null && current != null && current == value2) || current != null)
			{
				return;
			}
		}
		catch
		{
		}
		bool isEnabled = false;
		for (int i = 0; i < value.Count; i++)
		{
			if (value[i] == null)
			{
				continue;
			}
			try
			{
				Tooltip tooltip = value[i].TryCast<Tooltip>();
				if (tooltip == null)
				{
					continue;
				}
				tooltip.OpenTooltip();
				try
				{
					_activeTooltipPopupByOpener[opener] = Tooltip.Current;
					break;
				}
				catch
				{
					break;
				}
			}
			catch (System.Exception ex)
			{
				CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
				if (instance == null)
				{
					continue;
				}
				ManualLogSource log = instance.Log;
				if (log != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(37, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] OpenTooltip failed: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
		}
	}

	private static void HideDropdownTooltip_Direct(RustButton opener)
	{
		if (opener == null)
		{
			return;
		}
		try
		{
			_activeTooltipPopupByOpener.Remove(opener);
		}
		catch
		{
		}
		try
		{
			Tooltip.KillTooltip();
		}
		catch
		{
		}
	}

	private static void ShowDropdownTooltip(RustButton opener)
	{
		if (!(opener == null))
		{
			DropdownTooltipArbiter.RequestShow(opener);
		}
	}

	private static void HideDropdownTooltip(RustButton opener)
	{
		if (!(opener == null))
		{
			DropdownTooltipArbiter.NotifyHide(opener);
		}
	}

	public static void OnDropdownOptionsRebuilt(TweakUIDropdown tud)
	{
		if (tud == null || !_paintCtxByDropdown.TryGetValue(tud, out var value))
		{
			return;
		}
		try
		{
			ForceEnableItemFlex(value.ItemsContainer, CBSafeColorsPlugin.Instance?.Log);
			ForceEnableItemInteractables(value.ItemsContainer, CBSafeColorsPlugin.Instance?.Log);
			RepaintOptionSwatches(value.ItemsContainer, value.PrefabName, value.Colors, CBSafeColorsPlugin.Instance?.Log);
		}
		catch (System.Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance != null)
			{
				ManualLogSource log = instance.Log;
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(43, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] OnDropdownOptionsRebuilt: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	public static DropdownHandle Create(Transform container, string labelText, string[] options, Color[] colors, System.Action<int, string> onValueChanged, string tooltipText = null, string key = null)
	{
		ManualLogSource log = CBSafeColorsPlugin.Instance?.Log;
		if (string.IsNullOrEmpty(tooltipText))
		{
			tooltipText = labelText;
		}
		if (options == null || options.Length == 0)
		{
			log?.LogWarning("[SystemDropdown] no options provided");
			return null;
		}
		bool colourSwatchMode = colors != null && colors.Length == options.Length;
		GameObject gameObject = FindRowTemplate(log);
		if (gameObject == null)
		{
			log?.LogError("[SystemDropdown] no template row found (no TweakUIDropdown in scene)");
			return null;
		}
		GameObject gameObject2 = new GameObject("CBSafe_Color_TempParent");
		gameObject2.SetActive(value: false);
		bool isEnabled = false;
		GameObject gameObject3;
		try
		{
			gameObject3 = UnityEngine.Object.Instantiate(gameObject, gameObject2.transform, worldPositionStays: false);
			gameObject3.name = "CBSafe_Color_Dropdown_" + labelText;
		}
		catch (System.Exception t)
		{
			ManualLogSource manualLogSource = log;
			if (manualLogSource != null)
			{
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(37, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] instantiate failed: ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				manualLogSource.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
			UnityEngine.Object.Destroy(gameObject2);
			return null;
		}
		TweakUIDropdown tud = gameObject3.GetComponent<TweakUIDropdown>();
		if (tud == null)
		{
			tud = gameObject3.GetComponentInChildren<TweakUIDropdown>(includeInactive: true);
		}
		RectTransform dropdownPanel = ((tud != null) ? tud.Dropdown : null);
		RectTransform rectTransform = ((tud != null) ? tud.DropdownContainer : null);
		GameObject gameObject4 = ((tud != null) ? tud.DropdownItemPrefab : null);
		RustButton opener = ((tud != null) ? tud.Opener : null);
		RustText currentText = ((tud != null) ? tud.Current : null);
		if (DebugOn)
		{
			ManualLogSource manualLogSource2 = log;
			if (manualLogSource2 != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(76, 6, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] template wired: tud=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(tud != null);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" panel=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(dropdownPanel != null);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("items=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(rectTransform != null);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" prefab=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(gameObject4 != null);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" opener=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(opener != null);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" current=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(currentText != null);
				}
				manualLogSource2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		if (tud != null)
		{
			try
			{
				tud.convarName = string.Empty;
			}
			catch (System.Exception ex)
			{
				ManualLogSource manualLogSource3 = log;
				if (manualLogSource3 != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(35, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] clear convarName: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					manualLogSource3.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
			try
			{
				DisablePersistent(tud.onValueChanged);
			}
			catch (System.Exception ex2)
			{
				ManualLogSource manualLogSource4 = log;
				if (manualLogSource4 != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(56, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] disable persistent tud.onValueChanged: ");
						bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
					}
					manualLogSource4.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
				}
			}
			try
			{
				Il2CppReferenceArray<TweakUIDropdown.NameValue> il2CppReferenceArray = new Il2CppReferenceArray<TweakUIDropdown.NameValue>(options.Length);
				for (int i = 0; i < options.Length; i++)
				{
					TweakUIDropdown.NameValue nameValue = new TweakUIDropdown.NameValue();
					nameValue.value = options[i];
					string eng = (colourSwatchMode ? string.Empty : options[i]);
					try
					{
						nameValue.label = new Translate.Phrase(string.Empty, eng);
					}
					catch
					{
					}
					if (colourSwatchMode)
					{
						try
						{
							nameValue.imageColor = colors[i];
						}
						catch
						{
						}
					}
					((Il2CppArrayBase<TweakUIDropdown.NameValue>)il2CppReferenceArray)[i] = nameValue;
				}
				tud.nameValues = il2CppReferenceArray;
			}
			catch (System.Exception ex3)
			{
				ManualLogSource manualLogSource5 = log;
				if (manualLogSource5 != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler3 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown] set nameValues: ");
						bepInExWarningLogInterpolatedStringHandler3.AppendFormatted(ex3.Message);
					}
					manualLogSource5.LogWarning(bepInExWarningLogInterpolatedStringHandler3);
				}
			}
			if (colourSwatchMode)
			{
				try
				{
					tud.assignImageColor = false;
				}
				catch (System.Exception ex4)
				{
					ManualLogSource manualLogSource6 = log;
					if (manualLogSource6 != null)
					{
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler4 = new BepInExWarningLogInterpolatedStringHandler(45, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler4.AppendLiteral("[SystemDropdown] set assignImageColor=false: ");
							bepInExWarningLogInterpolatedStringHandler4.AppendFormatted(ex4.Message);
						}
						manualLogSource6.LogWarning(bepInExWarningLogInterpolatedStringHandler4);
					}
				}
			}
		}
		gameObject3.transform.SetParent(container, worldPositionStays: false);
		gameObject3.transform.localScale = Vector3.one;
		gameObject3.transform.localRotation = Quaternion.identity;
		UnityEngine.Object.Destroy(gameObject2);
		try
		{
			CanvasGroup component = gameObject3.GetComponent<CanvasGroup>();
			if (component != null)
			{
				component.alpha = 1f;
				component.blocksRaycasts = true;
				component.interactable = true;
			}
		}
		catch (System.Exception ex5)
		{
			ManualLogSource manualLogSource7 = log;
			if (manualLogSource7 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler5 = new BepInExWarningLogInterpolatedStringHandler(31, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler5.AppendLiteral("[SystemDropdown] reset row CG: ");
					bepInExWarningLogInterpolatedStringHandler5.AppendFormatted(ex5.Message);
				}
				manualLogSource7.LogWarning(bepInExWarningLogInterpolatedStringHandler5);
			}
		}
		if (gameObject3.GetComponent<LayoutElement>() == null)
		{
			RectTransform component2 = gameObject3.GetComponent<RectTransform>();
			if (component2 != null && component2.sizeDelta.y > 1f)
			{
				LayoutElement layoutElement = gameObject3.AddComponent<LayoutElement>();
				layoutElement.preferredHeight = component2.sizeDelta.y;
				layoutElement.minHeight = component2.sizeDelta.y;
				layoutElement.flexibleHeight = 0f;
			}
		}
		UpdateLabel(gameObject3, labelText, log);
		DefuseTooltip(gameObject3, tooltipText, log);
		if (opener != null)
		{
			try
			{
				List<Component> list = new List<Component>();
				foreach (Tooltip componentsInChild in gameObject3.GetComponentsInChildren<Tooltip>(includeInactive: true))
				{
					if (!(componentsInChild == null))
					{
						list.Add(componentsInChild);
					}
				}
				if (list.Count == 0)
				{
					Tooltip tooltip = EnsureTooltipOnRow(gameObject3, tooltipText);
					if (tooltip != null)
					{
						list.Add(tooltip);
					}
				}
				_tooltipsByOpener[opener] = list;
				ManualLogSource manualLogSource8 = log;
				if (manualLogSource8 != null)
				{
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(46, 2, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[Tooltip][DBG] opener=");
						GameObject gameObject5 = opener.gameObject;
						bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(((gameObject5 != null) ? gameObject5.name : null) ?? "<?>");
						bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(" captured Tooltip count=");
						bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(list.Count);
					}
					manualLogSource8.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
				}
				RustButton capturedOpener_TT = opener;
				DropdownTooltipArbiter.Register(capturedOpener_TT, delegate
				{
					ShowDropdownTooltip_Direct(capturedOpener_TT);
				}, delegate
				{
					HideDropdownTooltip_Direct(capturedOpener_TT);
				});
			}
			catch (System.Exception ex6)
			{
				ManualLogSource manualLogSource9 = log;
				if (manualLogSource9 != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler6 = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler6.AppendLiteral("[SystemDropdown] tooltip capture: ");
						bepInExWarningLogInterpolatedStringHandler6.AppendFormatted(ex6.Message);
					}
					manualLogSource9.LogWarning(bepInExWarningLogInterpolatedStringHandler6);
				}
			}
		}
		if (tud != null)
		{
			if (tud.DropdownItemPrefab == null)
			{
				EnsureDropdownPrefabFromAnySource(tud, log);
			}
			if (tud.DropdownItemPrefab != null)
			{
				try
				{
					tud.UpdateDropdownOptions();
				}
				catch (System.Exception ex7)
				{
					ManualLogSource manualLogSource10 = log;
					if (manualLogSource10 != null)
					{
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler7 = new BepInExWarningLogInterpolatedStringHandler(40, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler7.AppendLiteral("[SystemDropdown] UpdateDropdownOptions: ");
							bepInExWarningLogInterpolatedStringHandler7.AppendFormatted(ex7.Message);
						}
						manualLogSource10.LogWarning(bepInExWarningLogInterpolatedStringHandler7);
					}
				}
			}
		}
		if (tud != null && tud.onValueChanged != null)
		{
			try
			{
				tud.onValueChanged.AddListener((System.Action)delegate
				{
					bool isEnabled5 = false;
					try
					{
						int currentValue = tud.currentValue;
						if (currentValue < 0 || currentValue >= options.Length)
						{
							ManualLogSource manualLogSource37 = log;
							if (manualLogSource37 != null)
							{
								BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler29 = new BepInExWarningLogInterpolatedStringHandler(50, 1, out isEnabled5);
								if (isEnabled5)
								{
									bepInExWarningLogInterpolatedStringHandler29.AppendLiteral("[SystemDropdown] onValueChanged idx out of range: ");
									bepInExWarningLogInterpolatedStringHandler29.AppendFormatted(currentValue);
								}
								manualLogSource37.LogWarning(bepInExWarningLogInterpolatedStringHandler29);
							}
						}
						else
						{
							string text2 = options[currentValue];
							if (colourSwatchMode)
							{
								ApplyCurrentText(currentText, string.Empty);
								if (_rowSwatchByDropdown.TryGetValue(tud, out var value6) && value6 != null)
								{
									value6.color = colors[currentValue];
								}
							}
							else
							{
								ApplyCurrentText(currentText, text2);
							}
							try
							{
								if (opener != null)
								{
									try
									{
										opener.Value = false;
									}
									catch
									{
									}
								}
								if (opener != null && _ownStateByOpener.TryGetValue(opener, out var value7))
								{
									value7[0] = false;
								}
								if (opener != null && _animByOpener.TryGetValue(opener, out var value8))
								{
									value8.TargetOpen = false;
								}
								else if (dropdownPanel != null && dropdownPanel.gameObject.activeSelf)
								{
									dropdownPanel.gameObject.SetActive(value: false);
								}
								HideDropdownTooltip(opener);
								CloseModal();
							}
							catch
							{
							}
							onValueChanged?.Invoke(currentValue, text2);
							if (DebugOn)
							{
								ManualLogSource manualLogSource38 = log;
								if (manualLogSource38 != null)
								{
									BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler9 = new BepInExInfoLogInterpolatedStringHandler(29, 2, out isEnabled5);
									if (isEnabled5)
									{
										bepInExInfoLogInterpolatedStringHandler9.AppendLiteral("[SystemDropdown] selected ");
										bepInExInfoLogInterpolatedStringHandler9.AppendFormatted(currentValue);
										bepInExInfoLogInterpolatedStringHandler9.AppendLiteral(":'");
										bepInExInfoLogInterpolatedStringHandler9.AppendFormatted(text2);
										bepInExInfoLogInterpolatedStringHandler9.AppendLiteral("'");
									}
									manualLogSource38.LogInfo(bepInExInfoLogInterpolatedStringHandler9);
								}
							}
						}
					}
					catch (System.Exception t9)
					{
						ManualLogSource manualLogSource39 = log;
						if (manualLogSource39 != null)
						{
							BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler2 = new BepInExErrorLogInterpolatedStringHandler(41, 1, out isEnabled5);
							if (isEnabled5)
							{
								bepInExErrorLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] onValueChanged handler: ");
								bepInExErrorLogInterpolatedStringHandler2.AppendFormatted(t9);
							}
							manualLogSource39.LogError(bepInExErrorLogInterpolatedStringHandler2);
						}
					}
				});
			}
			catch (System.Exception ex8)
			{
				ManualLogSource manualLogSource11 = log;
				if (manualLogSource11 != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler8 = new BepInExWarningLogInterpolatedStringHandler(43, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler8.AppendLiteral("[SystemDropdown] subscribe onValueChanged: ");
						bepInExWarningLogInterpolatedStringHandler8.AppendFormatted(ex8.Message);
					}
					manualLogSource11.LogWarning(bepInExWarningLogInterpolatedStringHandler8);
				}
			}
		}
		if (opener != null && dropdownPanel != null)
		{
			_registered.Add((opener, dropdownPanel.gameObject));
		}
		Image image = null;
		if (colourSwatchMode)
		{
			ApplyCurrentText(currentText, string.Empty);
			image = CreateRowSwatch(opener, currentText, colors[0], log);
			if (image != null)
			{
				_rowSwatchByDropdown[tud] = image;
			}
		}
		else
		{
			ApplyCurrentText(currentText, options[0]);
		}
		string text = ((gameObject4 != null) ? gameObject4.name : null);
		if (colourSwatchMode && rectTransform != null)
		{
			_paintCtxByDropdown[tud] = new PaintContext
			{
				ItemsContainer = rectTransform,
				PrefabName = text,
				Colors = colors
			};
		}
		if (opener != null && dropdownPanel != null)
		{
			try
			{
				DisablePrefabListeners(opener);
				DestroyPanelDismisser(dropdownPanel, log);
				ClearAnimationEvents(gameObject3, log);
				DisableComponentByName(dropdownPanel.gameObject, "FlexTransition", log);
				try
				{
					Transform skipSubtree = ((rectTransform != null) ? rectTransform.transform : null);
					int t2 = DisableComponentByNameRecursiveExcept(gameObject3.gameObject, "FlexTransition", skipSubtree, log);
					int t3 = KillLeanTweensExcept(gameObject3.gameObject, skipSubtree, log);
					ManualLogSource manualLogSource12 = log;
					if (manualLogSource12 != null)
					{
						BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler3 = new BepInExInfoLogInterpolatedStringHandler(102, 2, out isEnabled);
						if (isEnabled)
						{
							bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown] flex/tween wipe (items-safe): disabled ");
							bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(t2);
							bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" FlexTransition(s), cancelled tweens on ");
							bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(t3);
							bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" GO(s)");
						}
						manualLogSource12.LogInfo(bepInExInfoLogInterpolatedStringHandler3);
					}
				}
				catch (System.Exception ex9)
				{
					ManualLogSource manualLogSource13 = log;
					if (manualLogSource13 != null)
					{
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler9 = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler9.AppendLiteral("[SystemDropdown] flex/tween wipe: ");
							bepInExWarningLogInterpolatedStringHandler9.AppendFormatted(ex9.Message);
						}
						manualLogSource13.LogWarning(bepInExWarningLogInterpolatedStringHandler9);
					}
				}
				try
				{
					if (gameObject3 != null)
					{
						Transform transform = ((opener != null) ? opener.transform : null);
						Transform transform2 = ((rectTransform != null) ? rectTransform.transform : null);
						int num = 0;
						int num2 = 0;
						foreach (Animator componentsInChild2 in gameObject3.GetComponentsInChildren<Animator>(includeInactive: true))
						{
							if (componentsInChild2 == null)
							{
								continue;
							}
							if (transform != null && componentsInChild2.transform != null && (componentsInChild2.transform == transform || componentsInChild2.transform.IsChildOf(transform)))
							{
								num2++;
								continue;
							}
							if (transform2 != null && componentsInChild2.transform != null && (componentsInChild2.transform == transform2 || componentsInChild2.transform.IsChildOf(transform2)))
							{
								num2++;
								continue;
							}
							try
							{
								componentsInChild2.enabled = false;
							}
							catch
							{
							}
							try
							{
								componentsInChild2.runtimeAnimatorController = null;
							}
							catch
							{
							}
							try
							{
								UnityEngine.Object.DestroyImmediate(componentsInChild2);
								num++;
							}
							catch
							{
								try
								{
									UnityEngine.Object.Destroy(componentsInChild2);
									num++;
								}
								catch
								{
								}
							}
						}
						ManualLogSource manualLogSource14 = log;
						if (manualLogSource14 != null)
						{
							BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler4 = new BepInExInfoLogInterpolatedStringHandler(70, 2, out isEnabled);
							if (isEnabled)
							{
								bepInExInfoLogInterpolatedStringHandler4.AppendLiteral("[SystemDropdown] killed ");
								bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(num);
								bepInExInfoLogInterpolatedStringHandler4.AppendLiteral(" Animator(s) on row (skipped ");
								bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(num2);
								bepInExInfoLogInterpolatedStringHandler4.AppendLiteral(" on opener/items)");
							}
							manualLogSource14.LogInfo(bepInExInfoLogInterpolatedStringHandler4);
						}
						if (dropdownPanel != null)
						{
							dropdownPanel.localScale = new Vector3(1f, 0f, 1f);
							CanvasGroup component3 = dropdownPanel.GetComponent<CanvasGroup>();
							if (component3 != null)
							{
								component3.alpha = 0f;
								component3.blocksRaycasts = false;
								component3.interactable = false;
							}
						}
					}
				}
				catch (System.Exception ex10)
				{
					ManualLogSource manualLogSource15 = log;
					if (manualLogSource15 != null)
					{
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler10 = new BepInExWarningLogInterpolatedStringHandler(37, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler10.AppendLiteral("[SystemDropdown] kill row animators: ");
							bepInExWarningLogInterpolatedStringHandler10.AppendFormatted(ex10.Message);
						}
						manualLogSource15.LogWarning(bepInExWarningLogInterpolatedStringHandler10);
					}
				}
				try
				{
					opener.OnPressed.RemoveAllListeners();
				}
				catch
				{
				}
				try
				{
					opener.OnReleased.RemoveAllListeners();
				}
				catch
				{
				}
				try
				{
					opener.TabPanelTarget = null;
				}
				catch
				{
				}
				try
				{
					opener.IsToggle = false;
				}
				catch
				{
				}
				try
				{
					opener.PreventToggleOff = false;
				}
				catch
				{
				}
				bool[] ownState = new bool[1];
				_ownStateByOpener[opener] = ownState;
				Transform transform3 = FindArrowChild(gameObject3.transform, log);
				RectTransform component4 = gameObject3.GetComponent<RectTransform>();
				int[] diagBudget = new int[1] { 12 };
				int[] watchDiagBudget = new int[1] { 12 };
				int[] stableFrames = new int[1];
				bool[] i0Dumped = new bool[1];
				RectTransform capturedPanel = dropdownPanel;
				RustButton capturedOpener = opener;
				RectTransform capturedItems = rectTransform;
				Color[] capturedColors = colors;
				string capturedPrefabName = text;
				Transform capturedArrow = transform3;
				RectTransform capturedRowRect = component4;
				int capturedOptionsCount = ((options != null) ? options.Length : 0);
				bool capturedSwatchMode = colourSwatchMode;
				opener.OnPressed.AddListener((System.Action)delegate
				{
					bool isEnabled4 = false;
					try
					{
						if (!(capturedPanel == null))
						{
							RequestAnimatedCloseExcept(capturedOpener);
							int frameCount = Time.frameCount;
							bool flag2 = ownState[0];
							bool flag3 = !flag2;
							ownState[0] = flag3;
							if (!_animByOpener.TryGetValue(capturedOpener, out var value4))
							{
								value4 = new AnimState();
								_animByOpener[capturedOpener] = value4;
							}
							value4.TargetOpen = flag3;
							if (flag3)
							{
								try
								{
									KillLeanTweensExcept(capturedPanel.gameObject, (capturedItems != null) ? capturedItems.transform : null, log);
								}
								catch (System.Exception ex18)
								{
									ManualLogSource manualLogSource25 = log;
									if (manualLogSource25 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler18 = new BepInExWarningLogInterpolatedStringHandler(43, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler18.AppendLiteral("[SystemDropdown] OnPressed KillLeanTweens: ");
											bepInExWarningLogInterpolatedStringHandler18.AppendFormatted(ex18.Message);
										}
										manualLogSource25.LogWarning(bepInExWarningLogInterpolatedStringHandler18);
									}
								}
								try
								{
									ApplyAnimProgress(capturedPanel, capturedArrow, value4.Progress);
								}
								catch (System.Exception ex19)
								{
									ManualLogSource manualLogSource26 = log;
									if (manualLogSource26 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler19 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler19.AppendLiteral("[SystemDropdown] apply progress: ");
											bepInExWarningLogInterpolatedStringHandler19.AppendFormatted(ex19.Message);
										}
										manualLogSource26.LogWarning(bepInExWarningLogInterpolatedStringHandler19);
									}
								}
								capturedPanel.gameObject.SetActive(value: true);
								try
								{
									KillLeanTweensExcept(capturedPanel.gameObject, (capturedItems != null) ? capturedItems.transform : null, log);
								}
								catch (System.Exception ex20)
								{
									ManualLogSource manualLogSource27 = log;
									if (manualLogSource27 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler20 = new BepInExWarningLogInterpolatedStringHandler(58, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler20.AppendLiteral("[SystemDropdown] OnPressed post-SetActive KillLeanTweens: ");
											bepInExWarningLogInterpolatedStringHandler20.AppendFormatted(ex20.Message);
										}
										manualLogSource27.LogWarning(bepInExWarningLogInterpolatedStringHandler20);
									}
								}
								try
								{
									EnforcePanelLayout(capturedPanel, capturedItems, capturedOptionsCount, log);
								}
								catch (System.Exception ex21)
								{
									ManualLogSource manualLogSource28 = log;
									if (manualLogSource28 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler21 = new BepInExWarningLogInterpolatedStringHandler(47, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler21.AppendLiteral("[SystemDropdown] OnPressed EnforcePanelLayout: ");
											bepInExWarningLogInterpolatedStringHandler21.AppendFormatted(ex21.Message);
										}
										manualLogSource28.LogWarning(bepInExWarningLogInterpolatedStringHandler21);
									}
								}
								try
								{
									if (capturedItems != null)
									{
										ForceEnableItemFlex(capturedItems, log);
									}
								}
								catch (System.Exception ex22)
								{
									ManualLogSource manualLogSource29 = log;
									if (manualLogSource29 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler22 = new BepInExWarningLogInterpolatedStringHandler(48, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler22.AppendLiteral("[SystemDropdown] OnPressed ForceEnableItemFlex: ");
											bepInExWarningLogInterpolatedStringHandler22.AppendFormatted(ex22.Message);
										}
										manualLogSource29.LogWarning(bepInExWarningLogInterpolatedStringHandler22);
									}
								}
								try
								{
									if (capturedItems != null)
									{
										ForceEnableItemInteractables(capturedItems, log);
									}
								}
								catch (System.Exception ex23)
								{
									ManualLogSource manualLogSource30 = log;
									if (manualLogSource30 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler23 = new BepInExWarningLogInterpolatedStringHandler(57, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler23.AppendLiteral("[SystemDropdown] OnPressed ForceEnableItemInteractables: ");
											bepInExWarningLogInterpolatedStringHandler23.AppendFormatted(ex23.Message);
										}
										manualLogSource30.LogWarning(bepInExWarningLogInterpolatedStringHandler23);
									}
								}
								try
								{
									EnsureScrollWired(capturedPanel, capturedItems, log);
								}
								catch (System.Exception ex24)
								{
									ManualLogSource manualLogSource31 = log;
									if (manualLogSource31 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler24 = new BepInExWarningLogInterpolatedStringHandler(46, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler24.AppendLiteral("[SystemDropdown] OnPressed EnsureScrollWired: ");
											bepInExWarningLogInterpolatedStringHandler24.AppendFormatted(ex24.Message);
										}
										manualLogSource31.LogWarning(bepInExWarningLogInterpolatedStringHandler24);
									}
								}
								try
								{
									EnsurePanelBackground(capturedPanel, log);
								}
								catch (System.Exception ex25)
								{
									ManualLogSource manualLogSource32 = log;
									if (manualLogSource32 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler25 = new BepInExWarningLogInterpolatedStringHandler(50, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler25.AppendLiteral("[SystemDropdown] OnPressed EnsurePanelBackground: ");
											bepInExWarningLogInterpolatedStringHandler25.AppendFormatted(ex25.Message);
										}
										manualLogSource32.LogWarning(bepInExWarningLogInterpolatedStringHandler25);
									}
								}
								try
								{
									DumpDropdownPanelOnce(capturedPanel, log);
								}
								catch
								{
								}
								try
								{
									LayoutRebuilder.ForceRebuildLayoutImmediate(capturedPanel);
									if (capturedItems != null)
									{
										LayoutRebuilder.ForceRebuildLayoutImmediate(capturedItems);
									}
									Canvas.ForceUpdateCanvases();
								}
								catch (System.Exception ex26)
								{
									ManualLogSource manualLogSource33 = log;
									if (manualLogSource33 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler26 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler26.AppendLiteral("[SystemDropdown] layout rebuild: ");
											bepInExWarningLogInterpolatedStringHandler26.AppendFormatted(ex26.Message);
										}
										manualLogSource33.LogWarning(bepInExWarningLogInterpolatedStringHandler26);
									}
								}
							}
							if (flag3)
							{
								watchDiagBudget[0] = 12;
							}
							if (DebugOn)
							{
								ManualLogSource manualLogSource34 = log;
								if (manualLogSource34 != null)
								{
									BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler8 = new BepInExInfoLogInterpolatedStringHandler(63, 6, out isEnabled4);
									if (isEnabled4)
									{
										bepInExInfoLogInterpolatedStringHandler8.AppendLiteral("[SystemDropdown] OnPressed: own ");
										bepInExInfoLogInterpolatedStringHandler8.AppendFormatted(flag2);
										bepInExInfoLogInterpolatedStringHandler8.AppendLiteral("->");
										bepInExInfoLogInterpolatedStringHandler8.AppendFormatted(flag3);
										bepInExInfoLogInterpolatedStringHandler8.AppendLiteral(" target=");
										bepInExInfoLogInterpolatedStringHandler8.AppendFormatted(value4.TargetOpen);
										bepInExInfoLogInterpolatedStringHandler8.AppendLiteral(" prog=");
										bepInExInfoLogInterpolatedStringHandler8.AppendFormatted(value4.Progress, "F2");
										bepInExInfoLogInterpolatedStringHandler8.AppendLiteral(" active=");
										bepInExInfoLogInterpolatedStringHandler8.AppendFormatted(capturedPanel.gameObject.activeSelf);
										bepInExInfoLogInterpolatedStringHandler8.AppendLiteral(" frame=");
										bepInExInfoLogInterpolatedStringHandler8.AppendFormatted(frameCount);
									}
									manualLogSource34.LogInfo(bepInExInfoLogInterpolatedStringHandler8);
								}
							}
							if (flag3 && diagBudget[0] > 0)
							{
								diagBudget[0]--;
								LogPanelDiagnostics(capturedPanel, capturedOpener, log, frameCount);
							}
							if (capturedSwatchMode && flag3 && capturedItems != null)
							{
								try
								{
									RepaintOptionSwatches(capturedItems, capturedPrefabName, capturedColors, log);
								}
								catch (System.Exception ex27)
								{
									ManualLogSource manualLogSource35 = log;
									if (manualLogSource35 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler27 = new BepInExWarningLogInterpolatedStringHandler(36, 1, out isEnabled4);
										if (isEnabled4)
										{
											bepInExWarningLogInterpolatedStringHandler27.AppendLiteral("[SystemDropdown] OnPressed repaint: ");
											bepInExWarningLogInterpolatedStringHandler27.AppendFormatted(ex27.Message);
										}
										manualLogSource35.LogWarning(bepInExWarningLogInterpolatedStringHandler27);
									}
								}
								if (_repaintBudgetByOpener.TryGetValue(capturedOpener, out var value5))
								{
									value5[0] = 4;
								}
							}
							if (flag3)
							{
								ShowDropdownTooltip(capturedOpener);
							}
							else
							{
								HideDropdownTooltip(capturedOpener);
							}
							if (flag3)
							{
								OpenModal(capturedOpener, capturedPanel);
							}
							else
							{
								CloseModal();
							}
						}
					}
					catch (System.Exception ex28)
					{
						ManualLogSource manualLogSource36 = log;
						if (manualLogSource36 != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler28 = new BepInExWarningLogInterpolatedStringHandler(35, 1, out isEnabled4);
							if (isEnabled4)
							{
								bepInExWarningLogInterpolatedStringHandler28.AppendLiteral("[SystemDropdown] OnPressed toggle: ");
								bepInExWarningLogInterpolatedStringHandler28.AppendFormatted(ex28.Message);
							}
							manualLogSource36.LogWarning(bepInExWarningLogInterpolatedStringHandler28);
						}
					}
				});
				opener.OnReleased.AddListener((System.Action)delegate
				{
					try
					{
						if (DebugOn)
						{
							ManualLogSource manualLogSource24 = log;
							if (manualLogSource24 != null)
							{
								bool isEnabled3 = false;
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler7 = new BepInExInfoLogInterpolatedStringHandler(61, 3, out isEnabled3);
								if (isEnabled3)
								{
									bepInExInfoLogInterpolatedStringHandler7.AppendLiteral("[SystemDropdown] OnReleased (noop): panel.activeSelf=");
									RectTransform rectTransform3 = capturedPanel;
									bepInExInfoLogInterpolatedStringHandler7.AppendFormatted((rectTransform3 != null) ? new bool?(rectTransform3.gameObject.activeSelf) : null);
									bepInExInfoLogInterpolatedStringHandler7.AppendLiteral(" own=");
									bepInExInfoLogInterpolatedStringHandler7.AppendFormatted(ownState[0]);
									bepInExInfoLogInterpolatedStringHandler7.AppendLiteral(" V=");
									bepInExInfoLogInterpolatedStringHandler7.AppendFormatted(capturedOpener.Value);
								}
								manualLogSource24.LogInfo(bepInExInfoLogInterpolatedStringHandler7);
							}
						}
					}
					catch
					{
					}
				});
				int[] watchRepaintBudget = new int[1] { 4 };
				if (capturedSwatchMode)
				{
					_repaintBudgetByOpener[opener] = watchRepaintBudget;
				}
				bool[] watchPrevActive = new bool[1];
				PanelOpenWatcher.Attach(dropdownPanel.gameObject, delegate
				{
					AnimState animState = null;
					bool isEnabled2 = false;
					try
					{
						if (capturedOpener != null && _animByOpener.TryGetValue(capturedOpener, out var value))
						{
							animState = value;
							float num3 = Time.unscaledDeltaTime;
							if (num3 > 0.05f)
							{
								num3 = 0.05f;
							}
							float num4 = num3 / 0.18f;
							if (value.TargetOpen)
							{
								value.Progress += num4;
							}
							else
							{
								value.Progress -= num4;
							}
							if (value.Progress < 0f)
							{
								value.Progress = 0f;
							}
							if (value.Progress > 1f)
							{
								value.Progress = 1f;
							}
							ApplyAnimProgress(capturedPanel, capturedArrow, value.Progress);
							if (!value.TargetOpen && value.Progress <= 0f)
							{
								if (capturedPanel.gameObject.activeSelf)
								{
									capturedPanel.gameObject.SetActive(value: false);
								}
								if (!IsAnyDropdownOpen())
								{
									CloseModal();
								}
							}
						}
					}
					catch (System.Exception ex12)
					{
						ManualLogSource manualLogSource18 = log;
						if (manualLogSource18 != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler12 = new BepInExWarningLogInterpolatedStringHandler(31, 1, out isEnabled2);
							if (isEnabled2)
							{
								bepInExWarningLogInterpolatedStringHandler12.AppendLiteral("[SystemDropdown] watcher anim: ");
								bepInExWarningLogInterpolatedStringHandler12.AppendFormatted(ex12.Message);
							}
							manualLogSource18.LogWarning(bepInExWarningLogInterpolatedStringHandler12);
						}
					}
					try
					{
						if (animState != null && animState.TargetOpen && capturedPanel != null && capturedPanel.gameObject.activeInHierarchy)
						{
							bool num5 = animState.Progress < 0.999f;
							if (num5)
							{
								stableFrames[0] = 0;
							}
							else if (stableFrames[0] < 7)
							{
								stableFrames[0]++;
							}
							if (num5 || stableFrames[0] <= 6)
							{
								EnforcePanelLayout(capturedPanel, capturedItems, capturedOptionsCount, null);
								try
								{
									if (capturedItems != null)
									{
										ForceEnableItemFlex(capturedItems, null);
									}
								}
								catch
								{
								}
								try
								{
									if (capturedItems != null)
									{
										ForceEnableItemInteractables(capturedItems, null);
									}
								}
								catch
								{
								}
								try
								{
									EnsureScrollWired(capturedPanel, capturedItems, null);
								}
								catch
								{
								}
								try
								{
									EnsurePanelBackground(capturedPanel, null);
								}
								catch
								{
								}
								try
								{
									LayoutRebuilder.ForceRebuildLayoutImmediate(capturedPanel);
								}
								catch
								{
								}
								if (capturedItems != null)
								{
									try
									{
										LayoutRebuilder.ForceRebuildLayoutImmediate(capturedItems);
									}
									catch
									{
									}
								}
							}
						}
						else if (animState != null && !animState.TargetOpen)
						{
							stableFrames[0] = 0;
						}
					}
					catch (System.Exception ex13)
					{
						ManualLogSource manualLogSource19 = log;
						if (manualLogSource19 != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler13 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled2);
							if (isEnabled2)
							{
								bepInExWarningLogInterpolatedStringHandler13.AppendLiteral("[SystemDropdown] watcher layout: ");
								bepInExWarningLogInterpolatedStringHandler13.AppendFormatted(ex13.Message);
							}
							manualLogSource19.LogWarning(bepInExWarningLogInterpolatedStringHandler13);
						}
					}
					try
					{
						if (DebugOn && watchDiagBudget[0] > 0 && capturedPanel != null)
						{
							watchDiagBudget[0]--;
							RectTransform rectTransform2 = capturedPanel;
							Vector3 localScale = rectTransform2.localScale;
							Vector2 sizeDelta = rectTransform2.sizeDelta;
							Rect rect = rectTransform2.rect;
							CanvasGroup component5 = rectTransform2.GetComponent<CanvasGroup>();
							float num6 = animState?.Progress ?? (-1f);
							bool t4 = animState?.TargetOpen ?? false;
							int num7 = ((capturedItems != null) ? capturedItems.childCount : (-1));
							Vector2 vector = ((capturedItems != null) ? capturedItems.sizeDelta : Vector2.zero);
							float num8 = ((capturedItems != null) ? capturedItems.rect.height : (-1f));
							float t5 = num8;
							float t6 = -1f;
							float t7 = -1f;
							float t8 = -1f;
							if (capturedItems != null && capturedItems.childCount > 0)
							{
								Transform child = capturedItems.GetChild(0);
								if (child != null)
								{
									t6 = child.localScale.x;
									t7 = child.localScale.y;
									CanvasGroup component6 = child.GetComponent<CanvasGroup>();
									if (component6 != null)
									{
										t8 = component6.alpha;
									}
								}
							}
							ManualLogSource manualLogSource20 = log;
							if (manualLogSource20 != null)
							{
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler6 = new BepInExInfoLogInterpolatedStringHandler(128, 18, out isEnabled2);
								if (isEnabled2)
								{
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral("[SystemDropdown] WATCH-DIAG f=");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(Time.frameCount);
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(" tgt=");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(t4);
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(" prog=");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(num6, "F3");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(" scale=(");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(localScale.x, "F2");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(",");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(localScale.y, "F2");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(",");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(localScale.z, "F2");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(") sizeDelta=(");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(sizeDelta.x, "F1");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(",");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(sizeDelta.y, "F1");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(") rect=");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(rect.width, "F1");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral("x");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(rect.height, "F1");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(" cgAlpha=");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted((component5 != null) ? component5.alpha : (-1f), "F2");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(" items=");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(num7);
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(" itemsSd=(");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(vector.x, "F1");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(",");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(vector.y, "F1");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(") itemsH=");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(t5, "F1");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(" i0scale=(");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(t6, "F2");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(",");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(t7, "F2");
									bepInExInfoLogInterpolatedStringHandler6.AppendLiteral(") i0cgA=");
									bepInExInfoLogInterpolatedStringHandler6.AppendFormatted(t8, "F2");
								}
								manualLogSource20.LogInfo(bepInExInfoLogInterpolatedStringHandler6);
							}
							if (!i0Dumped[0] && num6 >= 0.99f && num7 > 0 && capturedItems != null)
							{
								i0Dumped[0] = true;
								try
								{
									log?.LogInfo("[SystemDropdown] DUMP === BEGIN dropdownItemsContainer subtree ===");
									DumpItemTree(capturedItems, log);
									log?.LogInfo("[SystemDropdown] DUMP === END subtree ===");
								}
								catch (System.Exception ex14)
								{
									manualLogSource20 = log;
									if (manualLogSource20 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler14 = new BepInExWarningLogInterpolatedStringHandler(31, 1, out isEnabled2);
										if (isEnabled2)
										{
											bepInExWarningLogInterpolatedStringHandler14.AppendLiteral("[SystemDropdown] DumpItemTree: ");
											bepInExWarningLogInterpolatedStringHandler14.AppendFormatted(ex14.Message);
										}
										manualLogSource20.LogWarning(bepInExWarningLogInterpolatedStringHandler14);
									}
								}
							}
						}
					}
					catch (System.Exception ex15)
					{
						ManualLogSource manualLogSource21 = log;
						if (manualLogSource21 != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler15 = new BepInExWarningLogInterpolatedStringHandler(29, 1, out isEnabled2);
							if (isEnabled2)
							{
								bepInExWarningLogInterpolatedStringHandler15.AppendLiteral("[SystemDropdown] WATCH-DIAG: ");
								bepInExWarningLogInterpolatedStringHandler15.AppendFormatted(ex15.Message);
							}
							manualLogSource21.LogWarning(bepInExWarningLogInterpolatedStringHandler15);
						}
					}
					try
					{
						bool activeSelf = capturedPanel.gameObject.activeSelf;
						if (activeSelf && !watchPrevActive[0])
						{
							watchRepaintBudget[0] = 4;
						}
						watchPrevActive[0] = activeSelf;
					}
					catch
					{
					}
					if (capturedSwatchMode && watchRepaintBudget[0] > 0 && capturedItems != null && capturedItems.gameObject.activeInHierarchy)
					{
						try
						{
							RepaintOptionSwatches(capturedItems, capturedPrefabName, capturedColors, log);
						}
						catch (System.Exception ex16)
						{
							ManualLogSource manualLogSource22 = log;
							if (manualLogSource22 != null)
							{
								BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler16 = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled2);
								if (isEnabled2)
								{
									bepInExWarningLogInterpolatedStringHandler16.AppendLiteral("[SystemDropdown] watcher repaint: ");
									bepInExWarningLogInterpolatedStringHandler16.AppendFormatted(ex16.Message);
								}
								manualLogSource22.LogWarning(bepInExWarningLogInterpolatedStringHandler16);
							}
						}
						watchRepaintBudget[0]--;
					}
					try
					{
						if (capturedOpener != null && _animByOpener.TryGetValue(capturedOpener, out var value2) && value2.TargetOpen && value2.Progress > 0.001f)
						{
							ShowDropdownTooltip(capturedOpener);
						}
					}
					catch
					{
					}
					try
					{
						if (capturedOpener != null && _animByOpener.TryGetValue(capturedOpener, out var value3) && value3.TargetOpen && Input.GetMouseButtonDown(0))
						{
							Vector2 screenPoint = Input.mousePosition;
							bool num9 = capturedPanel != null && RectTransformUtility.RectangleContainsScreenPoint(capturedPanel, screenPoint, null);
							bool flag = capturedRowRect != null && RectTransformUtility.RectangleContainsScreenPoint(capturedRowRect, screenPoint, null);
							if (!num9 && !flag)
							{
								value3.TargetOpen = false;
								ownState[0] = false;
								HideDropdownTooltip(capturedOpener);
								if (DebugOn)
								{
									log?.LogInfo("[SystemDropdown] outside click -> animate close (modal stays on until anim finishes)");
								}
							}
						}
					}
					catch (System.Exception ex17)
					{
						ManualLogSource manualLogSource23 = log;
						if (manualLogSource23 != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler17 = new BepInExWarningLogInterpolatedStringHandler(32, 1, out isEnabled2);
							if (isEnabled2)
							{
								bepInExWarningLogInterpolatedStringHandler17.AppendLiteral("[SystemDropdown] outside click: ");
								bepInExWarningLogInterpolatedStringHandler17.AppendFormatted(ex17.Message);
							}
							manualLogSource23.LogWarning(bepInExWarningLogInterpolatedStringHandler17);
						}
					}
				});
			}
			catch (System.Exception ex11)
			{
				ManualLogSource manualLogSource16 = log;
				if (manualLogSource16 != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler11 = new BepInExWarningLogInterpolatedStringHandler(32, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler11.AppendLiteral("[SystemDropdown] opener wiring: ");
						bepInExWarningLogInterpolatedStringHandler11.AppendFormatted(ex11.Message);
					}
					manualLogSource16.LogWarning(bepInExWarningLogInterpolatedStringHandler11);
				}
			}
		}
		try
		{
			if (dropdownPanel != null)
			{
				try
				{
					ApplyAnimProgress(dropdownPanel, FindArrowChild(gameObject3.transform, log), 0f);
				}
				catch
				{
				}
				dropdownPanel.gameObject.SetActive(value: false);
			}
			if (opener != null)
			{
				try
				{
					opener.Value = false;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		DropdownHandle dropdownHandle = new DropdownHandle
		{
			Key = (key ?? labelText),
			Options = options,
			Colors = colors,
			Tud = tud,
			Opener = opener,
			Panel = dropdownPanel,
			RowRect = gameObject3.GetComponent<RectTransform>(),
			RowSwatch = image,
			CurrentText = currentText,
			CurrentIndex = 0
		};
		_handles.Add(dropdownHandle);
		if (DebugOn)
		{
			ManualLogSource manualLogSource17 = log;
			if (manualLogSource17 != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler5 = new BepInExInfoLogInterpolatedStringHandler(39, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler5.AppendLiteral("[SystemDropdown] '");
					bepInExInfoLogInterpolatedStringHandler5.AppendFormatted(labelText);
					bepInExInfoLogInterpolatedStringHandler5.AppendLiteral("' built ok (");
					bepInExInfoLogInterpolatedStringHandler5.AppendFormatted(options.Length);
					bepInExInfoLogInterpolatedStringHandler5.AppendLiteral(" options)");
				}
				manualLogSource17.LogInfo(bepInExInfoLogInterpolatedStringHandler5);
			}
		}
		return dropdownHandle;
	}

	private static void EnsureDropdownPrefabFromAnySource(TweakUIDropdown target, ManualLogSource log)
	{
		try
		{
			Il2CppReferenceArray<UnityEngine.Object> il2CppReferenceArray = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TweakUIDropdown>());
			if (il2CppReferenceArray == null)
			{
				return;
			}
			foreach (UnityEngine.Object item in (Il2CppArrayBase<UnityEngine.Object>)il2CppReferenceArray)
			{
				if (!(item == null))
				{
					TweakUIDropdown tweakUIDropdown = item.TryCast<TweakUIDropdown>();
					if (!(tweakUIDropdown == null) && !(tweakUIDropdown == target) && !(tweakUIDropdown.DropdownItemPrefab == null))
					{
						target.DropdownItemPrefab = tweakUIDropdown.DropdownItemPrefab;
						log?.LogWarning("[SystemDropdown] borrowed DropdownItemPrefab from another dropdown");
						break;
					}
				}
			}
		}
		catch
		{
		}
	}

	public static void PreWarmRowTemplate()
	{
		FindRowTemplate(null);
	}

	private static GameObject FindRowTemplate(ManualLogSource log)
	{
		if (_cachedRowTemplate != null)
		{
			TweakUIDropdown tweakUIDropdown = _cachedRowTemplate.GetComponent<TweakUIDropdown>() ?? _cachedRowTemplate.GetComponentInChildren<TweakUIDropdown>(includeInactive: true);
			if (tweakUIDropdown != null && tweakUIDropdown.DropdownItemPrefab != null && tweakUIDropdown.DropdownContainer != null && tweakUIDropdown.Dropdown != null && tweakUIDropdown.Opener != null && tweakUIDropdown.Current != null)
			{
				return _cachedRowTemplate;
			}
			_cachedRowTemplate = null;
		}
		Il2CppReferenceArray<UnityEngine.Object> il2CppReferenceArray = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TweakUIDropdown>());
		if (il2CppReferenceArray == null || ((Il2CppArrayBase<UnityEngine.Object>)il2CppReferenceArray).Length == 0)
		{
			log?.LogWarning("[SystemDropdown] Resources.FindObjectsOfTypeAll<TweakUIDropdown> returned nothing");
			return null;
		}
		GameObject gameObject = null;
		GameObject gameObject2 = null;
		GameObject gameObject3 = null;
		bool isEnabled = false;
		foreach (UnityEngine.Object item in (Il2CppArrayBase<UnityEngine.Object>)il2CppReferenceArray)
		{
			TweakUIDropdown tweakUIDropdown2 = item.TryCast<TweakUIDropdown>();
			if (tweakUIDropdown2 == null)
			{
				continue;
			}
			GameObject gameObject4 = tweakUIDropdown2.gameObject;
			if (gameObject4 == null)
			{
				continue;
			}
			int num = ((gameObject4.hideFlags == HideFlags.None && gameObject4.scene.name != null) ? 1 : 0);
			bool flag = tweakUIDropdown2.DropdownItemPrefab != null && tweakUIDropdown2.DropdownContainer != null && tweakUIDropdown2.Dropdown != null && tweakUIDropdown2.Opener != null && tweakUIDropdown2.Current != null;
			Transform transform = tweakUIDropdown2.transform;
			GameObject gameObject5 = (HasLabelOutsideContainer(transform) ? transform.gameObject : ((transform.parent != null && HasLabelOutsideContainer(transform.parent)) ? transform.parent.gameObject : transform.gameObject));
			bool flag2 = IsSafeTemplateName(gameObject5) && IsSafeTemplateName(gameObject4);
			if (gameObject3 == null)
			{
				gameObject3 = gameObject5;
			}
			if (num != 0 && gameObject == null)
			{
				gameObject = gameObject5;
			}
			if (((uint)num & ((flag2 ? true : false) ? 1u : 0u)) != 0 && gameObject2 == null)
			{
				gameObject2 = gameObject5;
			}
			if ((num & (flag2 ? 1 : 0) & (flag ? 1 : 0)) == 0)
			{
				continue;
			}
			_cachedRowTemplate = gameObject5;
			if (log != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(72, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] template selected (scene+safe+fully-wired): '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(gameObject5.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' (tud='");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(gameObject4.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("')");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
			return _cachedRowTemplate;
		}
		if (gameObject2 != null)
		{
			_cachedRowTemplate = gameObject2;
			if (log != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(51, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] template selected (scene+safe): '");
					bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(gameObject2.name);
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("'");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
			}
			return _cachedRowTemplate;
		}
		if (gameObject != null)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(89, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] no safe scene-based TweakUIDropdown found, falling back to scene-any: '");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(gameObject.name);
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("'");
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
			_cachedRowTemplate = gameObject;
			return _cachedRowTemplate;
		}
		if (gameObject3 != null)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(90, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] no scene-based TweakUIDropdown found, falling back to first candidate: '");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(gameObject3.name);
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("'");
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
			}
			_cachedRowTemplate = gameObject3;
			return _cachedRowTemplate;
		}
		return null;
	}

	private static bool IsSafeTemplateName(GameObject go)
	{
		if (go == null)
		{
			return true;
		}
		string text = null;
		try
		{
			text = go.name;
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text))
		{
			return true;
		}
		string text2 = text.ToLowerInvariant();
		for (int i = 0; i < SkipNameKeywords.Length; i++)
		{
			if (text2.IndexOf(SkipNameKeywords[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return false;
			}
		}
		return true;
	}

	private static bool HasLabelOutsideContainer(Transform rowT)
	{
		for (int i = 0; i < rowT.childCount; i++)
		{
			Transform child = rowT.GetChild(i);
			if (!(child.name == "Container") && child.GetComponentInChildren<TMP_Text>(includeInactive: true) != null)
			{
				return true;
			}
		}
		return false;
	}

	private static void DestroyPanelDismisser(RectTransform panel, ManualLogSource log)
	{
	}

	private static void EnsureCustomDismisser(RectTransform dropdownPanel, RustButton opener, ManualLogSource log)
	{
		if (dropdownPanel == null || opener == null || (_customDismisserByOpener.TryGetValue(opener, out var value) && value != null))
		{
			return;
		}
		for (int i = 0; i < dropdownPanel.childCount; i++)
		{
			Transform child = dropdownPanel.GetChild(i);
			if (child != null && child.name == LocStrings.Name_Color_Dismisser)
			{
				_customDismisserByOpener[opener] = child.gameObject;
				return;
			}
		}
		bool isEnabled = false;
		try
		{
			GameObject gameObject = new GameObject(LocStrings.Name_Color_Dismisser);
			gameObject.transform.SetParent(dropdownPanel, worldPositionStays: false);
			RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.sizeDelta = new Vector2(20000f, 20000f);
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0f);
			image.raycastTarget = true;
			Button button = gameObject.AddComponent<Button>();
			button.transition = Selectable.Transition.None;
			button.targetGraphic = image;
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener((System.Action)delegate
			{
				try
				{
					if (_animByOpener.TryGetValue(opener, out var value2))
					{
						value2.TargetOpen = false;
					}
					else if (dropdownPanel.gameObject.activeSelf)
					{
						dropdownPanel.gameObject.SetActive(value: false);
					}
					if (_ownStateByOpener.TryGetValue(opener, out var value3))
					{
						value3[0] = false;
					}
					try
					{
						opener.Value = false;
					}
					catch
					{
					}
					HideDropdownTooltip(opener);
					if (DebugOn)
					{
						CBSafeColorsPlugin.Instance?.Log.LogInfo("[SystemDropdown] custom Dismisser: closing dropdown");
					}
				}
				catch (System.Exception ex2)
				{
					CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
					if (instance != null)
					{
						ManualLogSource log2 = instance.Log;
						bool isEnabled2 = false;
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled2);
						if (isEnabled2)
						{
							bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] dismisser click: ");
							bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
						}
						log2.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
					}
				}
			});
			gameObject.transform.SetAsFirstSibling();
			_customDismisserByOpener[opener] = gameObject;
			if (!DebugOn)
			{
				return;
			}
			if (log != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(48, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] custom Dismisser attached to '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(dropdownPanel.gameObject.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("'");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(40, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] EnsureCustomDismisser: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static Transform FindArrowChild(Transform root, ManualLogSource log)
	{
		if (root == null)
		{
			return null;
		}
		bool isEnabled = false;
		try
		{
			try
			{
				Transform transform = root.Find("Container/Right/Icon");
				if (transform != null)
				{
					if (DebugOn)
					{
						log?.LogInfo("[SystemDropdown] FindArrowChild: 'Container/Right/Icon' selected (exact path)");
					}
					return transform;
				}
			}
			catch
			{
			}
			Il2CppArrayBase<Transform> componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			Transform transform2 = null;
			foreach (Transform item in componentsInChildren)
			{
				if (item == null)
				{
					continue;
				}
				string name = item.gameObject.name;
				if (name == null)
				{
					continue;
				}
				string text = name.ToLowerInvariant();
				if (text.Contains("arrow") || text.Contains("chevron") || text.Contains("caret"))
				{
					if (DebugOn)
					{
						if (log != null)
						{
							BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(58, 1, out isEnabled);
							if (isEnabled)
							{
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] FindArrowChild: '");
								bepInExInfoLogInterpolatedStringHandler.AppendFormatted(name);
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' selected (arrow-named)");
							}
							log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
						}
					}
					return item;
				}
				if (transform2 == null && text == "icon")
				{
					transform2 = item;
				}
			}
			if (transform2 != null)
			{
				if (DebugOn)
				{
					if (log != null)
					{
						BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(60, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] FindArrowChild: '");
							bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(transform2.gameObject.name);
							bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("' selected (icon fallback)");
						}
						log.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
					}
				}
				return transform2;
			}
			log?.LogWarning("[SystemDropdown] FindArrowChild: no arrow-named or Icon child found, arrow rotation disabled");
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] FindArrowChild: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
		return null;
	}

	private static void DisableComponentByName(GameObject go, string typeNameSubstring, ManualLogSource log)
	{
		if (go == null || string.IsNullOrEmpty(typeNameSubstring))
		{
			return;
		}
		bool isEnabled = false;
		try
		{
			Il2CppArrayBase<Component> components = go.GetComponents<Component>();
			if (components == null)
			{
				return;
			}
			int num = 0;
			ManualLogSource manualLogSource;
			for (int i = 0; i < components.Length; i++)
			{
				Component component = components[i];
				if (component == null)
				{
					continue;
				}
				string text = null;
				try
				{
					Il2CppSystem.Type il2CppType = component.GetIl2CppType();
					text = ((il2CppType != null) ? il2CppType.Name : null);
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(text))
				{
					try
					{
						text = component.GetType().Name;
					}
					catch
					{
					}
				}
				if (string.IsNullOrEmpty(text) || text.IndexOf(typeNameSubstring, System.StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}
				try
				{
					Behaviour behaviour = component.TryCast<Behaviour>();
					if (behaviour != null)
					{
						behaviour.enabled = false;
						num++;
						if (!DebugOn)
						{
							continue;
						}
						manualLogSource = log;
						if (manualLogSource != null)
						{
							BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(58, 2, out isEnabled);
							if (isEnabled)
							{
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] DisableComponentByName: disabled '");
								bepInExInfoLogInterpolatedStringHandler.AppendFormatted(text);
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' on '");
								bepInExInfoLogInterpolatedStringHandler.AppendFormatted(go.name);
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("'");
							}
							manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
						}
						continue;
					}
				}
				catch
				{
				}
				try
				{
					UnityEngine.Object.Destroy(component);
					num++;
					if (!DebugOn)
					{
						continue;
					}
					manualLogSource = log;
					if (manualLogSource != null)
					{
						BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(59, 2, out isEnabled);
						if (isEnabled)
						{
							bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] DisableComponentByName: destroyed '");
							bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(text);
							bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("' on '");
							bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(go.name);
							bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("'");
						}
						manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
					}
				}
				catch (System.Exception ex)
				{
					manualLogSource = log;
					if (manualLogSource != null)
					{
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(71, 2, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] DisableComponentByName: could not disable/destroy '");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(text);
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("': ");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
						}
						manualLogSource.LogWarning(bepInExWarningLogInterpolatedStringHandler);
					}
				}
			}
			if (num != 0 || !DebugOn)
			{
				return;
			}
			manualLogSource = log;
			if (manualLogSource != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler3 = new BepInExInfoLogInterpolatedStringHandler(63, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown] DisableComponentByName: no '");
					bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(typeNameSubstring);
					bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("' components on '");
					bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(go.name);
					bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("'");
				}
				manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler3);
			}
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(41, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] DisableComponentByName: ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
			}
		}
	}

	private static int DisableComponentByNameRecursive(GameObject root, string typeNameSubstring, ManualLogSource log)
	{
		if (root == null || string.IsNullOrEmpty(typeNameSubstring))
		{
			return 0;
		}
		int num = 0;
		bool isEnabled = false;
		try
		{
			Il2CppArrayBase<Transform> componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Transform transform = componentsInChildren[i];
				if (transform == null)
				{
					continue;
				}
				GameObject gameObject = transform.gameObject;
				if (gameObject == null)
				{
					continue;
				}
				foreach (Component component in gameObject.GetComponents<Component>())
				{
					if (component == null)
					{
						continue;
					}
					string text = null;
					try
					{
						Il2CppSystem.Type il2CppType = component.GetIl2CppType();
						text = ((il2CppType != null) ? il2CppType.Name : null);
					}
					catch
					{
					}
					if (string.IsNullOrEmpty(text))
					{
						try
						{
							text = component.GetType().Name;
						}
						catch
						{
						}
					}
					if (string.IsNullOrEmpty(text) || text.IndexOf(typeNameSubstring, System.StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
					try
					{
						Behaviour behaviour = component.TryCast<Behaviour>();
						if (behaviour != null)
						{
							behaviour.enabled = false;
							num++;
							continue;
						}
					}
					catch
					{
					}
					try
					{
						UnityEngine.Object.Destroy(component);
						num++;
					}
					catch (System.Exception ex)
					{
						if (log != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(43, 3, out isEnabled);
							if (isEnabled)
							{
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] disable/destroy '");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(text);
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("' on '");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(gameObject.name);
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("': ");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
							}
							log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
						}
					}
				}
			}
			return num;
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(54, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] DisableComponentByNameRecursive('");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(typeNameSubstring);
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("'): ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
				return num;
			}
			return num;
		}
	}

	private static void DumpItemTree(Transform root, ManualLogSource log)
	{
		if (root == null)
		{
			return;
		}
		try
		{
			DumpItemNode(root, 0, log);
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(31, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] DumpItemTree: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void DumpItemNode(Transform t, int depth, ManualLogSource log)
	{
		if (t == null || depth > 8)
		{
			return;
		}
		GameObject gameObject = t.gameObject;
		RectTransform rectTransform = null;
		try
		{
			rectTransform = t.TryCast<RectTransform>();
		}
		catch
		{
		}
		if (rectTransform == null)
		{
			try
			{
				rectTransform = gameObject.GetComponent<RectTransform>();
			}
			catch
			{
			}
		}
		string t2 = "-";
		string t3 = "-";
		string t4 = "-";
		string t5 = "-";
		string t6 = "-";
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
		defaultInterpolatedStringHandler.AppendFormatted(t.localScale.x, "F2");
		defaultInterpolatedStringHandler.AppendLiteral(",");
		defaultInterpolatedStringHandler.AppendFormatted(t.localScale.y, "F2");
		string t7 = defaultInterpolatedStringHandler.ToStringAndClear();
		if (rectTransform != null)
		{
			Vector2 sizeDelta = rectTransform.sizeDelta;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(sizeDelta.x, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(sizeDelta.y, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(")");
			t2 = defaultInterpolatedStringHandler.ToStringAndClear();
			Vector2 anchoredPosition = rectTransform.anchoredPosition;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(anchoredPosition.x, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(anchoredPosition.y, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(")");
			t3 = defaultInterpolatedStringHandler.ToStringAndClear();
			Rect rect = rectTransform.rect;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
			defaultInterpolatedStringHandler.AppendFormatted(rect.width, "F1");
			defaultInterpolatedStringHandler.AppendLiteral("x");
			defaultInterpolatedStringHandler.AppendFormatted(rect.height, "F1");
			t4 = defaultInterpolatedStringHandler.ToStringAndClear();
			Vector2 anchorMin = rectTransform.anchorMin;
			Vector2 anchorMax = rectTransform.anchorMax;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 4);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMin.x, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMin.y, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(")-(");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMax.x, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMax.y, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(")");
			t5 = defaultInterpolatedStringHandler.ToStringAndClear();
			Vector2 pivot = rectTransform.pivot;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 2);
			defaultInterpolatedStringHandler.AppendLiteral("(");
			defaultInterpolatedStringHandler.AppendFormatted(pivot.x, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(pivot.y, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(")");
			t6 = defaultInterpolatedStringHandler.ToStringAndClear();
		}
		string text = "";
		float t8 = -1f;
		float t9 = -1f;
		float t10 = -1f;
		bool t11 = false;
		bool t12 = false;
		string t13 = "-";
		string text2 = "-";
		bool t14 = false;
		bool t15 = false;
		try
		{
			Il2CppArrayBase<Component> components = gameObject.GetComponents<Component>();
			List<string> list = new List<string>();
			foreach (Component item in components)
			{
				if (item == null)
				{
					continue;
				}
				string text3 = null;
				try
				{
					Il2CppSystem.Type il2CppType = item.GetIl2CppType();
					text3 = ((il2CppType != null) ? il2CppType.Name : null);
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(text3))
				{
					text3 = item.GetType().Name;
				}
				if (string.IsNullOrEmpty(text3))
				{
					continue;
				}
				list.Add(text3);
				switch (text3)
				{
				case "CanvasGroup":
					try
					{
						CanvasGroup canvasGroup = item.TryCast<CanvasGroup>();
						if (canvasGroup != null)
						{
							t8 = canvasGroup.alpha;
							_ = canvasGroup.interactable;
						}
					}
					catch
					{
					}
					continue;
				case "Image":
				case "RawImage":
				case "RustImage":
				case "ProceduralImage":
					try
					{
						Image image = item.TryCast<Image>();
						if (image != null)
						{
							t9 = image.color.a;
							t11 = image.enabled;
							t13 = ((image.sprite != null) ? image.sprite.name : "<null>");
						}
					}
					catch
					{
					}
					continue;
				}
				if (!text3.StartsWith("TMP_Text"))
				{
					switch (text3)
					{
					case "Mask":
						t14 = true;
						continue;
					case "RectMask2D":
						t15 = true;
						continue;
					case "TextMeshProUGUI":
					case "TMP_Text":
					case "RustText":
						break;
					default:
						continue;
					}
				}
				try
				{
					TMP_Text tMP_Text = item.TryCast<TMP_Text>();
					if (tMP_Text != null)
					{
						t10 = tMP_Text.color.a;
						t12 = tMP_Text.enabled;
						text2 = tMP_Text.text ?? "<null>";
						if (text2.Length > 32)
						{
							text2 = text2.Substring(0, 32) + "…";
						}
					}
				}
				catch
				{
				}
			}
			text = string.Join(",", list);
		}
		catch (System.Exception ex)
		{
			text = "<err:" + ex.Message + ">";
		}
		string t16 = "";
		try
		{
			Il2CppArrayBase<Component> components2 = gameObject.GetComponents<Component>();
			List<string> list2 = new List<string>();
			foreach (Component item2 in components2)
			{
				if (item2 == null)
				{
					continue;
				}
				string text4 = null;
				try
				{
					Il2CppSystem.Type il2CppType2 = item2.GetIl2CppType();
					text4 = ((il2CppType2 != null) ? il2CppType2.Name : null);
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(text4))
				{
					text4 = item2.GetType().Name;
				}
				if (string.IsNullOrEmpty(text4) || (text4.IndexOf("Layout", System.StringComparison.OrdinalIgnoreCase) < 0 && text4.IndexOf("Flex", System.StringComparison.OrdinalIgnoreCase) < 0 && text4.IndexOf("ContentSizeFitter", System.StringComparison.OrdinalIgnoreCase) < 0))
				{
					continue;
				}
				bool flag = true;
				try
				{
					Behaviour behaviour = item2.TryCast<Behaviour>();
					if (behaviour != null)
					{
						flag = behaviour.enabled;
					}
				}
				catch
				{
				}
				list2.Add(text4 + "(" + (flag ? "on" : "OFF") + ")");
			}
			if (list2.Count > 0)
			{
				t16 = " layouts=[" + string.Join(",", list2) + "]";
			}
		}
		catch
		{
		}
		string t17 = new string(' ', depth * 2);
		if (log != null)
		{
			bool isEnabled = false;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(102, 21, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] DUMP");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t17);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" '");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(gameObject.name);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' act=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(gameObject.activeSelf);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("/");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(gameObject.activeInHierarchy);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" sd=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t2);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ap=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t3);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" rect=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t4);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" anch=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t5);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" pivot=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t6);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" sc=(");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t7);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(") cg=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t8, "F2");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" img=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t9, "F2");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("/");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t11);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("/sp=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t13);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" txt=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t10, "F2");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("/");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t12);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("/'");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(text2);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' mask=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t14);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("/");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t15);
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t16);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" comps=[");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(text);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("]");
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
		for (int i = 0; i < t.childCount && i < 16; i++)
		{
			Transform child = t.GetChild(i);
			if (child != null)
			{
				DumpItemNode(child, depth + 1, log);
			}
		}
	}

	private static int KillLeanTweensExcept(GameObject root, Transform skipSubtree, ManualLogSource log)
	{
		if (root == null)
		{
			return 0;
		}
		int num = 0;
		bool isEnabled = false;
		try
		{
			Il2CppArrayBase<Transform> componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Transform transform = componentsInChildren[i];
				if (transform == null || (skipSubtree != null && (transform == skipSubtree || transform.IsChildOf(skipSubtree))))
				{
					continue;
				}
				try
				{
					LeanTween.cancel(transform.gameObject);
					num++;
				}
				catch (System.Exception ex)
				{
					if (log != null)
					{
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(41, 2, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] LeanTween.cancel on '");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(transform.name);
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("': ");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
						}
						log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
					}
				}
			}
			return num;
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(39, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] KillLeanTweensExcept: ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
				return num;
			}
			return num;
		}
	}

	private static int DisableComponentByNameRecursiveExcept(GameObject root, string typeNameSubstring, Transform skipSubtree, ManualLogSource log)
	{
		if (root == null || string.IsNullOrEmpty(typeNameSubstring))
		{
			return 0;
		}
		int num = 0;
		bool isEnabled = false;
		try
		{
			Il2CppArrayBase<Transform> componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Transform transform = componentsInChildren[i];
				if (transform == null || (skipSubtree != null && (transform == skipSubtree || transform.IsChildOf(skipSubtree))))
				{
					continue;
				}
				foreach (Component component in transform.gameObject.GetComponents<Component>())
				{
					if (component == null)
					{
						continue;
					}
					Il2CppSystem.Type il2CppType = component.GetIl2CppType();
					string text = ((il2CppType != null) ? il2CppType.Name : null) ?? component.GetType().Name;
					if (string.IsNullOrEmpty(text) || text.IndexOf(typeNameSubstring, System.StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
					try
					{
						Behaviour behaviour = component.TryCast<Behaviour>();
						if (behaviour != null)
						{
							behaviour.enabled = false;
							num++;
						}
						else
						{
							UnityEngine.Object.Destroy(component);
							num++;
						}
					}
					catch (System.Exception ex)
					{
						if (log != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(66, 3, out isEnabled);
							if (isEnabled)
							{
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] DisableComponentByNameRecursiveExcept('");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(typeNameSubstring);
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("') on '");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(transform.name);
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("': ");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
							}
							log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
						}
					}
				}
			}
			return num;
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(60, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] DisableComponentByNameRecursiveExcept('");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(typeNameSubstring);
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("'): ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
				return num;
			}
			return num;
		}
	}

	private static int KillLeanTweens(GameObject root, ManualLogSource log)
	{
		if (root == null)
		{
			return 0;
		}
		int num = 0;
		bool isEnabled = false;
		try
		{
			Il2CppArrayBase<Transform> componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Transform transform = componentsInChildren[i];
				if (transform == null)
				{
					continue;
				}
				try
				{
					LeanTween.cancel(transform.gameObject);
					num++;
				}
				catch (System.Exception ex)
				{
					if (log != null)
					{
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(41, 2, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] LeanTween.cancel on '");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(transform.name);
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("': ");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
						}
						log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
					}
				}
			}
			return num;
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] KillLeanTweens: ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
				return num;
			}
			return num;
		}
	}

	private static void EnforcePanelLayout(RectTransform panel, RectTransform itemsContainer, int optionsCount, ManualLogSource log)
	{
		if (panel == null)
		{
			return;
		}
		try
		{
			float num = 38f;
			if (itemsContainer != null && itemsContainer.childCount > 0)
			{
				Transform child = itemsContainer.GetChild(0);
				RectTransform rectTransform = (RectTransform)((child is RectTransform) ? child : null);
				if (rectTransform != null)
				{
					float num2 = ((rectTransform.rect.height > 1f) ? rectTransform.rect.height : ((rectTransform.sizeDelta.y > 1f) ? rectTransform.sizeDelta.y : 0f));
					float num3 = num2;
					if (num3 > 1f)
					{
						num = num3;
					}
				}
			}
			int num4 = ((optionsCount > 0) ? optionsCount : ((itemsContainer != null) ? itemsContainer.childCount : 0));
			if (num4 <= 0)
			{
				return;
			}
			float num5 = num4;
			float num6 = num5 * num + 16f;
			Vector2 sizeDelta = panel.sizeDelta;
			if (System.Math.Abs(sizeDelta.y - num6) > 0.5f)
			{
				panel.sizeDelta = new Vector2(sizeDelta.x, num6);
			}
			if (itemsContainer != null && num4 > 0)
			{
				float num7 = (float)num4 * num;
				Vector2 sizeDelta2 = itemsContainer.sizeDelta;
				if (System.Math.Abs(sizeDelta2.y - num7) > 0.5f)
				{
					itemsContainer.sizeDelta = new Vector2(sizeDelta2.x, num7);
				}
			}
			try
			{
				int instanceID = panel.GetInstanceID();
				if (!_panelScrollCache.TryGetValue(instanceID, out var value) || value.IsStale())
				{
					value = ScrollRectCacheEntry.Build(panel);
					_panelScrollCache[instanceID] = value;
				}
				float num8 = num5 * num;
				if (value.Flex != null && System.Math.Abs(value.LastMaxH - num8) > 0.5f)
				{
					FlexLength flexLength = default(FlexLength);
					flexLength.Value = num8;
					flexLength.Unit = FlexUnit.Pixels;
					FlexLength maxHeight = flexLength;
					value.Flex.MaxHeight = maxHeight;
					value.LastMaxH = num8;
					_panelScrollCache[instanceID] = value;
				}
			}
			catch
			{
			}
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(37, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] EnforcePanelLayout: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void ForceEnableItemFlex(RectTransform itemsContainer, ManualLogSource log)
	{
		if (itemsContainer == null)
		{
			return;
		}
		int num = 0;
		bool isEnabled = false;
		ManualLogSource manualLogSource;
		try
		{
			Il2CppArrayBase<Behaviour> componentsInChildren = itemsContainer.GetComponentsInChildren<Behaviour>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Behaviour behaviour = componentsInChildren[i];
				if (behaviour == null)
				{
					continue;
				}
				string text = null;
				try
				{
					Il2CppSystem.Type il2CppType = behaviour.GetIl2CppType();
					text = ((il2CppType != null) ? il2CppType.Name : null);
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(text))
				{
					try
					{
						text = behaviour.GetType().Name;
					}
					catch
					{
						text = null;
					}
				}
				if (string.IsNullOrEmpty(text) || text.IndexOf("Flex", System.StringComparison.OrdinalIgnoreCase) < 0 || text.Equals("FlexTransition", System.StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				try
				{
					if (!behaviour.enabled)
					{
						behaviour.enabled = true;
						num++;
					}
				}
				catch
				{
				}
			}
		}
		catch (System.Exception ex)
		{
			manualLogSource = log;
			if (manualLogSource != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(38, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] ForceEnableItemFlex: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				manualLogSource.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
		if (!DebugOn || num <= 0)
		{
			return;
		}
		manualLogSource = log;
		if (manualLogSource != null)
		{
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(76, 1, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] ForceEnableItemFlex: re-enabled ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" Flex behaviour(s) on items");
			}
			manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
	}

	private static void ForceEnableItemInteractables(RectTransform itemsContainer, ManualLogSource log)
	{
		if (itemsContainer == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		bool isEnabled = false;
		ManualLogSource manualLogSource;
		try
		{
			int childCount = itemsContainer.childCount;
			Color color = default(Color);
			for (int i = 0; i < childCount; i++)
			{
				Transform child = itemsContainer.GetChild(i);
				if (child == null || !child.gameObject.activeSelf)
				{
					continue;
				}
				Image component = child.GetComponent<Image>();
				if (component != null)
				{
					bool flag = false;
					try
					{
						if (!component.enabled)
						{
							component.enabled = true;
							flag = true;
						}
					}
					catch
					{
					}
					try
					{
						if (!component.raycastTarget)
						{
							component.raycastTarget = true;
							flag = true;
						}
					}
					catch
					{
					}
					try
					{
						color = new Color(0.2f, 0.2f, 0.16862746f, 1f);
						Color color2 = component.color;
						if (System.Math.Abs(color2.r - color.r) > 0.003921569f || System.Math.Abs(color2.g - color.g) > 0.003921569f || System.Math.Abs(color2.b - color.b) > 0.003921569f || System.Math.Abs(color2.a - color.a) > 0.003921569f)
						{
							component.color = color;
							flag = true;
						}
					}
					catch
					{
					}
					try
					{
						if (component.sprite != null)
						{
							component.sprite = null;
							flag = true;
						}
					}
					catch
					{
					}
					if (flag)
					{
						num++;
					}
				}
				RustButton component2 = child.GetComponent<RustButton>();
				if (!(component2 != null))
				{
					continue;
				}
				try
				{
					if (!component2.enabled)
					{
						component2.enabled = true;
						num2++;
					}
				}
				catch
				{
				}
			}
		}
		catch (System.Exception ex)
		{
			manualLogSource = log;
			if (manualLogSource != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(47, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] ForceEnableItemInteractables: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				manualLogSource.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
		if (!DebugOn || (num <= 0 && num2 <= 0))
		{
			return;
		}
		manualLogSource = log;
		if (manualLogSource != null)
		{
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(86, 2, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] ForceEnableItemInteractables: fixed ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" Image(s), enabled ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num2);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" RustButton(s)");
			}
			manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
	}

	private static bool ApplyOpaqueGreyTint(Image img)
	{
		if (img == null)
		{
			return false;
		}
		bool result = false;
		try
		{
			if (!img.enabled)
			{
				img.enabled = true;
				result = true;
			}
		}
		catch
		{
		}
		try
		{
			Color color = img.color;
			if (System.Math.Abs(color.r - PanelBgGrey.r) > 0.003921569f || System.Math.Abs(color.g - PanelBgGrey.g) > 0.003921569f || System.Math.Abs(color.b - PanelBgGrey.b) > 0.003921569f || System.Math.Abs(color.a - PanelBgGrey.a) > 0.003921569f)
			{
				img.color = PanelBgGrey;
				result = true;
			}
		}
		catch
		{
		}
		try
		{
			if (img.sprite != null)
			{
				img.sprite = null;
				result = true;
			}
		}
		catch
		{
		}
		try
		{
			if (!img.raycastTarget)
			{
				img.raycastTarget = true;
				result = true;
				return result;
			}
			return result;
		}
		catch
		{
			return result;
		}
	}

	private static void EnsurePanelBackground(RectTransform dropdownPanel, ManualLogSource log)
	{
		if (dropdownPanel == null)
		{
			return;
		}
		bool isEnabled = false;
		try
		{
			bool flag = false;
			Image component = dropdownPanel.GetComponent<Image>();
			if (component != null)
			{
				bool num = ApplyOpaqueGreyTint(component);
				flag = true;
				if (num && DebugOn)
				{
					log?.LogInfo("[SystemDropdown] EnsurePanelBackground: tinted panel root Image");
				}
			}
			int childCount = dropdownPanel.childCount;
			for (int i = 0; i < childCount; i++)
			{
				Transform child = dropdownPanel.GetChild(i);
				if (child == null)
				{
					continue;
				}
				string text = child.name ?? "";
				if (text.IndexOf("Background", System.StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Bg", System.StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Frame", System.StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("Panel", System.StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}
				Image component2 = child.GetComponent<Image>();
				if (component2 == null)
				{
					continue;
				}
				try
				{
					if (!child.gameObject.activeSelf)
					{
						child.gameObject.SetActive(value: true);
					}
				}
				catch
				{
				}
				bool num2 = ApplyOpaqueGreyTint(component2);
				flag = true;
				if (!num2 || !DebugOn)
				{
					continue;
				}
				if (log != null)
				{
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(55, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] EnsurePanelBackground: tinted child '");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(text);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("'");
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
			}
			if (flag)
			{
				return;
			}
			for (int j = 0; j < childCount; j++)
			{
				Transform child2 = dropdownPanel.GetChild(j);
				if (!(child2 != null) || !(child2.name == LocStrings.Name_Color_PanelBackground))
				{
					continue;
				}
				Image component3 = child2.GetComponent<Image>();
				if (component3 != null)
				{
					try
					{
						component3.color = PanelBgGrey;
					}
					catch
					{
					}
					try
					{
						component3.enabled = true;
					}
					catch
					{
					}
					try
					{
						component3.raycastTarget = true;
						return;
					}
					catch
					{
						return;
					}
				}
				return;
			}
			try
			{
				GameObject gameObject = new GameObject(LocStrings.Name_Color_PanelBackground);
				gameObject.transform.SetParent(dropdownPanel, worldPositionStays: false);
				RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.offsetMin = Vector2.zero;
				rectTransform.offsetMax = Vector2.zero;
				Image image = gameObject.AddComponent<Image>();
				image.color = PanelBgGrey;
				image.raycastTarget = true;
				gameObject.transform.SetAsFirstSibling();
				if (DebugOn)
				{
					log?.LogInfo("[SystemDropdown] EnsurePanelBackground: created " + LocStrings.Name_Color_PanelBackground);
				}
			}
			catch (System.Exception ex)
			{
				if (log != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(47, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] EnsurePanelBackground create: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(40, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] EnsurePanelBackground: ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
			}
		}
	}

	private static void DumpDropdownPanelOnce(RectTransform dropdownPanel, ManualLogSource log)
	{
		if (dropdownPanel == null || log == null || _panelDumpDone.Contains(dropdownPanel))
		{
			return;
		}
		_panelDumpDone.Add(dropdownPanel);
		bool isEnabled = false;
		try
		{
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(56, 3, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] PANELDUMP === BEGIN panel='");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(dropdownPanel.gameObject.name);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' rect=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(dropdownPanel.rect.width, "F1");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("x");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(dropdownPanel.rect.height, "F1");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ===");
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			DumpRectOneLine(dropdownPanel.transform, log, 0);
			int childCount = dropdownPanel.childCount;
			for (int i = 0; i < childCount; i++)
			{
				Transform child = dropdownPanel.GetChild(i);
				if (child == null)
				{
					continue;
				}
				DumpRectOneLine(child, log, 1);
				int childCount2 = child.childCount;
				for (int j = 0; j < childCount2; j++)
				{
					Transform child2 = child.GetChild(j);
					if (!(child2 == null))
					{
						DumpRectOneLine(child2, log, 2);
					}
				}
			}
			log.LogInfo("[SystemDropdown] PANELDUMP === END ===");
		}
		catch (System.Exception ex)
		{
			BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(40, 1, out isEnabled);
			if (isEnabled)
			{
				bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] DumpDropdownPanelOnce: ");
				bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
			}
			log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
		}
	}

	private static void DumpRectOneLine(Transform t, ManualLogSource log, int depth)
	{
		if (t == null)
		{
			return;
		}
		bool isEnabled = false;
		try
		{
			RectTransform component = t.GetComponent<RectTransform>();
			Image component2 = t.GetComponent<Image>();
			string t2 = "img=-";
			if (component2 != null)
			{
				string value = "<null>";
				try
				{
					value = ((component2.sprite != null) ? component2.sprite.name : "<null>");
				}
				catch
				{
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 4);
				defaultInterpolatedStringHandler.AppendLiteral("img(a=");
				defaultInterpolatedStringHandler.AppendFormatted(component2.color.a, "F2");
				defaultInterpolatedStringHandler.AppendLiteral("/en=");
				defaultInterpolatedStringHandler.AppendFormatted(component2.enabled);
				defaultInterpolatedStringHandler.AppendLiteral("/rt=");
				defaultInterpolatedStringHandler.AppendFormatted(component2.raycastTarget);
				defaultInterpolatedStringHandler.AppendLiteral("/sp=");
				defaultInterpolatedStringHandler.AppendFormatted(value);
				defaultInterpolatedStringHandler.AppendLiteral(")");
				t2 = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			string t3 = "";
			ScrollRect component3 = t.GetComponent<ScrollRect>();
			if (component3 != null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 3);
				defaultInterpolatedStringHandler.AppendLiteral(" ScrollRect(en=");
				defaultInterpolatedStringHandler.AppendFormatted(component3.enabled);
				defaultInterpolatedStringHandler.AppendLiteral(",content=");
				defaultInterpolatedStringHandler.AppendFormatted((component3.content != null) ? component3.content.name : "<null>");
				defaultInterpolatedStringHandler.AppendLiteral(",viewport=");
				defaultInterpolatedStringHandler.AppendFormatted((component3.viewport != null) ? component3.viewport.name : "<null>");
				defaultInterpolatedStringHandler.AppendLiteral(")");
				t3 = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			string t4 = "";
			Scrollbar component4 = t.GetComponent<Scrollbar>();
			if (component4 != null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 2);
				defaultInterpolatedStringHandler.AppendLiteral(" Scrollbar(en=");
				defaultInterpolatedStringHandler.AppendFormatted(component4.enabled);
				defaultInterpolatedStringHandler.AppendLiteral(",val=");
				defaultInterpolatedStringHandler.AppendFormatted(component4.value, "F2");
				defaultInterpolatedStringHandler.AppendLiteral(")");
				t4 = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			Il2CppArrayBase<Component> components = t.GetComponents<Component>();
			List<string> list = new List<string>();
			if (components != null)
			{
				for (int i = 0; i < components.Length; i++)
				{
					Component component5 = components[i];
					if (component5 == null)
					{
						continue;
					}
					string text = null;
					try
					{
						Il2CppSystem.Type il2CppType = component5.GetIl2CppType();
						text = ((il2CppType != null) ? il2CppType.Name : null);
					}
					catch
					{
					}
					if (string.IsNullOrEmpty(text))
					{
						try
						{
							text = component5.GetType().Name;
						}
						catch
						{
						}
					}
					if (!string.IsNullOrEmpty(text))
					{
						list.Add(text);
					}
				}
			}
			string t5 = new string(' ', depth * 2);
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(50, 10, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] PANELDUMP ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t5);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("'");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t.name);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' act=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t.gameObject.activeSelf);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("/");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t.gameObject.activeInHierarchy);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" sd=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted((component != null) ? component.sizeDelta.x : 0f, "F0");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted((component != null) ? component.sizeDelta.y : 0f, "F0");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t2);
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t3);
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t4);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" comps=[");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(string.Join(",", list));
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("]");
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
		catch (System.Exception ex)
		{
			BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled);
			if (isEnabled)
			{
				bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] DumpRectOneLine: ");
				bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
			}
			log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
		}
	}

	private static void EnsureScrollWired(RectTransform dropdownPanel, RectTransform itemsContainer, ManualLogSource log)
	{
		if (dropdownPanel == null)
		{
			return;
		}
		bool isEnabled = false;
		try
		{
			try
			{
				EnsureViewportClipping(dropdownPanel, log);
			}
			catch
			{
			}
			Il2CppArrayBase<ScrollRect> componentsInChildren = dropdownPanel.GetComponentsInChildren<ScrollRect>(includeInactive: true);
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ScrollRect scrollRect = componentsInChildren[i];
				if (scrollRect == null)
				{
					continue;
				}
				try
				{
					if (scrollRect.content == null && itemsContainer != null)
					{
						scrollRect.content = itemsContainer;
						num2++;
					}
					if (scrollRect.viewport == null)
					{
						RectTransform content = scrollRect.content;
						if (content != null)
						{
							Transform parent = content.parent;
							RectTransform rectTransform = (RectTransform)((parent is RectTransform) ? parent : null);
							if (rectTransform != null)
							{
								scrollRect.viewport = rectTransform;
							}
						}
					}
				}
				catch
				{
				}
				try
				{
					if (scrollRect.enabled)
					{
						scrollRect.enabled = false;
						num++;
					}
				}
				catch
				{
				}
			}
			Il2CppArrayBase<Scrollbar> componentsInChildren2 = dropdownPanel.GetComponentsInChildren<Scrollbar>(includeInactive: true);
			int num3 = 0;
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				Scrollbar scrollbar = componentsInChildren2[j];
				if (scrollbar == null)
				{
					continue;
				}
				try
				{
					if (scrollbar.enabled)
					{
						scrollbar.enabled = false;
						num3++;
					}
					scrollbar.gameObject.SetActive(value: false);
				}
				catch
				{
				}
			}
			if (!DebugOn || num + num2 + num3 <= 0)
			{
				return;
			}
			if (log != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(100, 3, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] EnsureScrollWired: enabled ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ScrollRect(s), wired ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num2);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" content(s), enabled ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num3);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" Scrollbar(s)");
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(36, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] EnsureScrollWired: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void EnsureViewportClipping(RectTransform viewport, ManualLogSource log)
	{
		if (viewport == null)
		{
			return;
		}
		GameObject gameObject = viewport.gameObject;
		if (gameObject == null)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		try
		{
			RectMask2D rectMask2D = gameObject.GetComponent<RectMask2D>();
			if (rectMask2D == null)
			{
				rectMask2D = gameObject.AddComponent<RectMask2D>();
				flag = rectMask2D != null;
			}
			if (rectMask2D != null && !rectMask2D.enabled)
			{
				rectMask2D.enabled = true;
				flag2 = true;
			}
		}
		catch
		{
		}
		bool flag3 = false;
		try
		{
			Mask component = gameObject.GetComponent<Mask>();
			if (component != null && component.enabled)
			{
				component.enabled = false;
				flag3 = true;
			}
		}
		catch
		{
		}
		bool flag4 = false;
		try
		{
			Image component2 = gameObject.GetComponent<Image>();
			if (component2 != null)
			{
				Color color = component2.color;
				if (color.a > 0.001f)
				{
					component2.color = new Color(color.r, color.g, color.b, 0f);
					flag4 = true;
				}
				if (component2.raycastTarget)
				{
					component2.raycastTarget = false;
					flag4 = true;
				}
			}
		}
		catch
		{
		}
		if (!DebugOn || !(flag || flag2 || flag3 || flag4))
		{
			return;
		}
		string t = "?";
		try
		{
			Rect rect = viewport.rect;
			Vector2 anchorMin = viewport.anchorMin;
			Vector2 anchorMax = viewport.anchorMax;
			Vector2 pivot = viewport.pivot;
			Vector2 sizeDelta = viewport.sizeDelta;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 12);
			defaultInterpolatedStringHandler.AppendLiteral("rect=(");
			defaultInterpolatedStringHandler.AppendFormatted(rect.x, "F0");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(rect.y, "F0");
			defaultInterpolatedStringHandler.AppendLiteral(")/");
			defaultInterpolatedStringHandler.AppendFormatted(rect.width, "F0");
			defaultInterpolatedStringHandler.AppendLiteral("x");
			defaultInterpolatedStringHandler.AppendFormatted(rect.height, "F0");
			defaultInterpolatedStringHandler.AppendLiteral(" aMin=(");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMin.x, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMin.y, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(") aMax=(");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMax.x, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMax.y, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(") piv=(");
			defaultInterpolatedStringHandler.AppendFormatted(pivot.x, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(pivot.y, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(") sd=(");
			defaultInterpolatedStringHandler.AppendFormatted(sizeDelta.x, "F0");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(sizeDelta.y, "F0");
			defaultInterpolatedStringHandler.AppendLiteral(")");
			t = defaultInterpolatedStringHandler.ToStringAndClear();
		}
		catch
		{
		}
		if (log != null)
		{
			bool isEnabled = false;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(109, 6, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] EnsureViewportClipping '");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(gameObject.name);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("': RectMask2D added=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(flag);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" enabled=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(flag2);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", legacyMask disabled=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(flag3);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", image fixed=");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(flag4);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" | ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t);
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
	}

	private static void ForceItemsVisible(RectTransform itemsContainer)
	{
		if (itemsContainer == null)
		{
			return;
		}
		try
		{
			int childCount = itemsContainer.childCount;
			for (int i = 0; i < childCount; i++)
			{
				Transform child = itemsContainer.GetChild(i);
				if (child == null)
				{
					continue;
				}
				Vector3 localScale = child.localScale;
				if (localScale.x < 0.95f || localScale.y < 0.95f || localScale.z < 0.95f)
				{
					child.localScale = Vector3.one;
				}
				CanvasGroup component = child.GetComponent<CanvasGroup>();
				if (component != null)
				{
					if (component.alpha < 0.99f)
					{
						component.alpha = 1f;
					}
					if (!component.interactable)
					{
						component.interactable = true;
					}
					if (!component.blocksRaycasts)
					{
						component.blocksRaycasts = true;
					}
				}
				int childCount2 = child.childCount;
				for (int j = 0; j < childCount2; j++)
				{
					Transform child2 = child.GetChild(j);
					if (!(child2 == null))
					{
						Vector3 localScale2 = child2.localScale;
						if (localScale2.x < 0.95f || localScale2.y < 0.95f || localScale2.z < 0.95f)
						{
							child2.localScale = Vector3.one;
						}
						CanvasGroup component2 = child2.GetComponent<CanvasGroup>();
						if (component2 != null && component2.alpha < 0.99f)
						{
							component2.alpha = 1f;
						}
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void ForcePanelVisible(RectTransform panel)
	{
		if (panel == null)
		{
			return;
		}
		try
		{
			Vector3 localScale = panel.localScale;
			if (localScale.x != 1f || localScale.y != 1f || localScale.z != 1f)
			{
				panel.localScale = Vector3.one;
			}
			CanvasGroup component = panel.GetComponent<CanvasGroup>();
			if (component != null)
			{
				if (component.alpha < 1f)
				{
					component.alpha = 1f;
				}
				if (!component.interactable)
				{
					component.interactable = true;
				}
				if (!component.blocksRaycasts)
				{
					component.blocksRaycasts = true;
				}
			}
		}
		catch
		{
		}
	}

	private static void LogPanelDiagnostics(RectTransform panel, RustButton opener, ManualLogSource log, int frame)
	{
		bool isEnabled = false;
		try
		{
			if (panel == null)
			{
				return;
			}
			GameObject gameObject = panel.gameObject;
			Rect rect = panel.rect;
			Vector2 anchoredPosition = panel.anchoredPosition;
			Vector3 localPosition = panel.localPosition;
			Vector3 localScale = panel.localScale;
			bool activeInHierarchy = gameObject.activeInHierarchy;
			bool activeSelf = gameObject.activeSelf;
			Canvas componentInParent = panel.GetComponentInParent<Canvas>();
			CanvasGroup component = panel.GetComponent<CanvasGroup>();
			float t = ((component != null) ? component.alpha : (-1f));
			bool t2 = !(component != null) || component.interactable;
			bool t3 = !(component != null) || component.blocksRaycasts;
			Image component2 = panel.GetComponent<Image>();
			float t4 = ((component2 != null) ? component2.color.a : (-1f));
			int childCount = panel.childCount;
			int num = 0;
			for (int i = 0; i < childCount; i++)
			{
				if (panel.GetChild(i).gameObject.activeSelf)
				{
					num++;
				}
			}
			if (!DebugOn)
			{
				return;
			}
			if (log != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(173, 21, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] DIAG f=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(frame);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" panel='");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(gameObject.name);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' activeSelf=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(activeSelf);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" activeInHierarchy=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(activeInHierarchy);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" rect=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(rect.width, "F1");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("x");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(rect.height, "F1");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" pos=(");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(anchoredPosition.x, "F1");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(anchoredPosition.y, "F1");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(") lp=(");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(localPosition.x, "F1");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(localPosition.y, "F1");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(localPosition.z, "F1");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(") scale=(");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(localScale.x, "F2");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(localScale.y, "F2");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(localScale.z, "F2");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(") canvas=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted((componentInParent != null) ? componentInParent.name : "<none>");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" cgAlpha=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t, "F2");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" cgInteract=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t2);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" cgBlocks=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t3);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" imgAlpha=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t4, "F2");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" children=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(childCount);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" activeChildren=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num);
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(38, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] LogPanelDiagnostics: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void ClearAnimationEvents(GameObject root, ManualLogSource log)
	{
		if (root == null)
		{
			return;
		}
		bool isEnabled = false;
		try
		{
			Il2CppArrayBase<Animator> componentsInChildren = root.GetComponentsInChildren<Animator>(includeInactive: true);
			int num = 0;
			ManualLogSource manualLogSource;
			foreach (Animator item in componentsInChildren)
			{
				if (item == null)
				{
					continue;
				}
				RuntimeAnimatorController runtimeAnimatorController = item.runtimeAnimatorController;
				if (runtimeAnimatorController == null)
				{
					continue;
				}
				Il2CppReferenceArray<AnimationClip> animationClips = runtimeAnimatorController.animationClips;
				if (animationClips == null)
				{
					continue;
				}
				for (int i = 0; i < ((Il2CppArrayBase<AnimationClip>)animationClips).Length; i++)
				{
					AnimationClip animationClip = ((Il2CppArrayBase<AnimationClip>)animationClips)[i];
					if (animationClip == null)
					{
						continue;
					}
					int num2 = 0;
					try
					{
						num2 = ((animationClip.events != null) ? ((Il2CppArrayBase<AnimationEvent>)animationClip.events).Length : 0);
					}
					catch
					{
					}
					if (num2 == 0)
					{
						continue;
					}
					try
					{
						animationClip.events = new Il2CppReferenceArray<AnimationEvent>(0L);
						num += num2;
						if (!DebugOn)
						{
							continue;
						}
						manualLogSource = log;
						if (manualLogSource != null)
						{
							BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(72, 3, out isEnabled);
							if (isEnabled)
							{
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] cleared ");
								bepInExInfoLogInterpolatedStringHandler.AppendFormatted(num2);
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" animation event(s) on clip '");
								bepInExInfoLogInterpolatedStringHandler.AppendFormatted(animationClip.name);
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' (animator on '");
								bepInExInfoLogInterpolatedStringHandler.AppendFormatted(item.gameObject.name);
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("')");
							}
							manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
						}
					}
					catch (System.Exception ex)
					{
						manualLogSource = log;
						if (manualLogSource != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(42, 2, out isEnabled);
							if (isEnabled)
							{
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] clear events on clip '");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(animationClip.name);
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("': ");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
							}
							manualLogSource.LogWarning(bepInExWarningLogInterpolatedStringHandler);
						}
					}
				}
			}
			if (!DebugOn)
			{
				return;
			}
			manualLogSource = log;
			if (manualLogSource != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(85, 3, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] ClearAnimationEvents: ");
					bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(num);
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(" event(s) cleared across ");
					bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(componentsInChildren?.Length ?? 0);
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(" animator(s) under '");
					bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(root.name);
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("'");
				}
				manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
			}
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(39, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] ClearAnimationEvents: ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
			}
		}
	}

	private static void DisablePrefabListeners(RustButton opener)
	{
		if (!(opener == null))
		{
			DisablePersistent(opener.OnPressed);
			DisablePersistent(opener.OnReleased);
		}
	}

	private static void DisablePersistent(UnityEvent ev)
	{
		if (ev == null)
		{
			return;
		}
		try
		{
			int persistentEventCount = ev.GetPersistentEventCount();
			for (int i = 0; i < persistentEventCount; i++)
			{
				try
				{
					ev.SetPersistentListenerState(i, UnityEventCallState.Off);
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

	private static void DestroyTweakComponents(GameObject root, ManualLogSource log)
	{
		List<Component> list = new List<Component>();
		foreach (Component componentsInChild in root.GetComponentsInChildren<Component>(includeInactive: true))
		{
			if (!(componentsInChild == null))
			{
				string name;
				try
				{
					name = componentsInChild.GetIl2CppType().Name;
				}
				catch
				{
					continue;
				}
				switch (name)
				{
				case "TweakUIDropdown":
				case "TweakUIBase":
				case "UI_SettingsTweakDropdown":
					list.Add(componentsInChild);
					break;
				}
			}
		}
		bool isEnabled = false;
		ManualLogSource manualLogSource;
		foreach (Component item in list)
		{
			try
			{
				UnityEngine.Object.DestroyImmediate(item);
			}
			catch (System.Exception ex)
			{
				manualLogSource = log;
				if (manualLogSource != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(34, 2, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] destroy ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted((item != null) ? item.GetIl2CppType().Name : null);
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral(" failed: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					manualLogSource.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
		}
		if (!DebugOn)
		{
			return;
		}
		manualLogSource = log;
		if (manualLogSource != null)
		{
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(44, 1, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] removed ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(list.Count);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" tweak component(s)");
			}
			manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
	}

	private static void UpdateLabel(GameObject row, string labelText, ManualLogSource log)
	{
		Transform transform = row.transform;
		TMP_Text tMP_Text = null;
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			if (!(child.name == "Container"))
			{
				TMP_Text componentInChildren = child.GetComponentInChildren<TMP_Text>(includeInactive: true);
				if (componentInChildren != null)
				{
					tMP_Text = componentInChildren;
					break;
				}
			}
		}
		if (tMP_Text == null)
		{
			log?.LogWarning("[SystemDropdown] could not find label TMP_Text");
			return;
		}
		bool isEnabled = false;
		try
		{
			bool flag = LocStrings.Marker_Locali.Length > 0 && LocStrings.Marker_Phrase.Length > 0 && LocStrings.Marker_Token.Length > 0;
			foreach (Component componentsInChild in row.GetComponentsInChildren<Component>(includeInactive: true))
			{
				if (componentsInChild == null)
				{
					continue;
				}
				string text = null;
				try
				{
					Il2CppSystem.Type il2CppType = componentsInChild.GetIl2CppType();
					text = ((il2CppType != null) ? il2CppType.Name : null);
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(text))
				{
					try
					{
						text = componentsInChild.GetType().Name;
					}
					catch
					{
					}
				}
				if (!string.IsNullOrEmpty(text) && flag && (text.Contains(LocStrings.Marker_Locali) || text.Contains(LocStrings.Marker_Phrase) || text.Contains(LocStrings.Marker_Token)))
				{
					Behaviour behaviour = componentsInChild.TryCast<Behaviour>();
					if (behaviour != null)
					{
						behaviour.enabled = false;
					}
				}
			}
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(52, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] UpdateLabel: defuse locali failed: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
		try
		{
			foreach (TMP_Text componentsInChild2 in row.GetComponentsInChildren<TMP_Text>(includeInactive: true))
			{
				if (componentsInChild2 == null)
				{
					continue;
				}
				RustText rustText = componentsInChild2.TryCast<RustText>();
				if (!(rustText == null))
				{
					try
					{
						rustText.Token = string.Empty;
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
		RustText rustText2 = tMP_Text.TryCast<RustText>();
		if (rustText2 != null)
		{
			try
			{
				rustText2.IsLocalized = false;
			}
			catch
			{
			}
			try
			{
				rustText2.Token = string.Empty;
			}
			catch
			{
			}
			try
			{
				rustText2.English = labelText;
			}
			catch
			{
			}
		}
		tMP_Text.text = labelText;
		tMP_Text.raycastTarget = false;
		tMP_Text.ForceMeshUpdate();
		try
		{
			DropdownLabelEnforcer component = tMP_Text.gameObject.GetComponent<DropdownLabelEnforcer>();
			if (component != null)
			{
				try
				{
					UnityEngine.Object.Destroy(component);
				}
				catch
				{
				}
			}
			tMP_Text.gameObject.AddComponent<DropdownLabelEnforcer>().Bind(tMP_Text, labelText);
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(54, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] UpdateLabel: enforcer attach failed: ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
			}
		}
	}

	private static GameObject FindTooltipObjectTemplate()
	{
		if (_tooltipObjectTemplate != null)
		{
			try
			{
				if (_tooltipObjectTemplate.GetInstanceID() != 0)
				{
					return _tooltipObjectTemplate;
				}
			}
			catch
			{
				_tooltipObjectTemplate = null;
			}
		}
		try
		{
			foreach (Tooltip item in Resources.FindObjectsOfTypeAll<Tooltip>())
			{
				if (item == null)
				{
					continue;
				}
				try
				{
					GameObject tooltipObject = item.TooltipObject;
					if (tooltipObject != null)
					{
						_tooltipObjectTemplate = tooltipObject;
						return tooltipObject;
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
		return null;
	}

	private static Tooltip EnsureTooltipOnRow(GameObject row, string text)
	{
		if (row == null)
		{
			return null;
		}
		Tooltip tooltip = null;
		try
		{
			tooltip = row.GetComponent<Tooltip>();
		}
		catch
		{
		}
		if (tooltip == null)
		{
			try
			{
				tooltip = row.GetComponentInChildren<Tooltip>(includeInactive: true);
			}
			catch
			{
			}
		}
		if (tooltip == null)
		{
			try
			{
				tooltip = row.AddComponent<Tooltip>();
			}
			catch
			{
				return null;
			}
		}
		try
		{
			tooltip.enabled = true;
		}
		catch
		{
		}
		try
		{
			tooltip.token = string.Empty;
		}
		catch
		{
		}
		try
		{
			tooltip.Text = text ?? string.Empty;
		}
		catch
		{
		}
		try
		{
			if (tooltip.TooltipObject == null)
			{
				GameObject gameObject = FindTooltipObjectTemplate();
				if (gameObject != null)
				{
					try
					{
						tooltip.TooltipObject = gameObject;
						return tooltip;
					}
					catch
					{
						return tooltip;
					}
				}
				return tooltip;
			}
			return tooltip;
		}
		catch
		{
			return tooltip;
		}
	}

	private static void DefuseTooltip(GameObject row, string labelText, ManualLogSource log)
	{
		bool isEnabled = false;
		try
		{
			int num = 0;
			foreach (Tooltip componentsInChild in row.GetComponentsInChildren<Tooltip>(includeInactive: true))
			{
				if (componentsInChild == null)
				{
					continue;
				}
				num++;
				try
				{
					componentsInChild.token = string.Empty;
				}
				catch
				{
				}
				try
				{
					componentsInChild.Text = labelText ?? string.Empty;
				}
				catch
				{
				}
				try
				{
					componentsInChild.enabled = true;
				}
				catch
				{
				}
				try
				{
					if (!(componentsInChild.TooltipObject == null))
					{
						continue;
					}
					GameObject gameObject = FindTooltipObjectTemplate();
					if (gameObject != null)
					{
						try
						{
							componentsInChild.TooltipObject = gameObject;
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
			ManualLogSource manualLogSource;
			if (num == 0)
			{
				Tooltip tooltip = EnsureTooltipOnRow(row, labelText);
				manualLogSource = log;
				if (manualLogSource != null)
				{
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(65, 2, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[Tooltip][DBG] DefuseTooltip: row='");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(row.name);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' had 0 Tooltip(s), added new=");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(tooltip != null);
					}
					manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
				return;
			}
			manualLogSource = log;
			if (manualLogSource != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(52, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[Tooltip][DBG] DefuseTooltip: row='");
					bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(row.name);
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("' had ");
					bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(num);
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(" Tooltip(s)");
				}
				manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
			}
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] tooltip defuse: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void RebuildOptions(RectTransform dropdownPanel, RectTransform itemsContainer, GameObject itemPrefab, RustButton opener, RustText currentText, string[] options, System.Action<int, string> onValueChanged, ManualLogSource log)
	{
		if (dropdownPanel == null || itemsContainer == null || itemPrefab == null)
		{
			log?.LogError("[SystemDropdown] dropdownPanel/itemsContainer/itemPrefab missing - cannot rebuild options");
			return;
		}
		for (int num = itemsContainer.childCount - 1; num >= 0; num--)
		{
			Transform child = itemsContainer.GetChild(num);
			if (!(child == null) && !(child.gameObject == itemPrefab))
			{
				UnityEngine.Object.DestroyImmediate(child.gameObject);
			}
		}
		for (int i = 0; i < options.Length; i++)
		{
			string text = options[i];
			int num2 = i;
			GameObject gameObject = UnityEngine.Object.Instantiate(itemPrefab, itemsContainer, worldPositionStays: false);
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Option_");
			defaultInterpolatedStringHandler.AppendFormatted(num2);
			defaultInterpolatedStringHandler.AppendLiteral("_");
			defaultInterpolatedStringHandler.AppendFormatted(text);
			gameObject.name = defaultInterpolatedStringHandler.ToStringAndClear();
			gameObject.SetActive(value: true);
			foreach (TMP_Text componentsInChild in gameObject.GetComponentsInChildren<TMP_Text>(includeInactive: true))
			{
				RustText rustText = componentsInChild.TryCast<RustText>();
				if (rustText != null)
				{
					try
					{
						rustText.Token = string.Empty;
					}
					catch
					{
					}
					try
					{
						rustText.English = text;
					}
					catch
					{
					}
				}
				componentsInChild.text = text;
				componentsInChild.ForceMeshUpdate();
			}
			DestroyTweakComponents(gameObject, log);
			WireOptionClick(gameObject, num2, text, opener, currentText, dropdownPanel, onValueChanged, log);
		}
	}

	private static void WireOptionClick(GameObject item, int idx, string optText, RustButton opener, RustText currentText, RectTransform dropdownPanel, System.Action<int, string> onValueChanged, ManualLogSource log)
	{
		RustButton component = item.GetComponent<RustButton>();
		if (component != null)
		{
			try
			{
				component.OnReleased.RemoveAllListeners();
			}
			catch
			{
			}
			component.OnReleased.AddListener((System.Action)delegate
			{
				HandleSelection(idx, optText, opener, currentText, dropdownPanel, onValueChanged, log);
			});
			return;
		}
		Button button = item.GetComponent<Button>();
		if (button == null)
		{
			button = item.AddComponent<Button>();
		}
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener((System.Action)delegate
		{
			HandleSelection(idx, optText, opener, currentText, dropdownPanel, onValueChanged, log);
		});
	}

	private static void HandleSelection(int idx, string optText, RustButton opener, RustText currentText, RectTransform dropdownPanel, System.Action<int, string> onValueChanged, ManualLogSource log)
	{
		bool isEnabled = false;
		try
		{
			ApplyCurrentText(currentText, optText);
			ManualLogSource manualLogSource;
			try
			{
				if (opener != null)
				{
					opener.SetToggleFalse();
				}
			}
			catch (System.Exception ex)
			{
				manualLogSource = log;
				if (manualLogSource != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] SetToggleFalse: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					manualLogSource.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
			if (dropdownPanel != null)
			{
				dropdownPanel.gameObject.SetActive(value: false);
			}
			if (opener != null && _ownStateByOpener.TryGetValue(opener, out var value))
			{
				value[0] = false;
			}
			try
			{
				EventSystem current = EventSystem.current;
				if (current != null && opener != null && current.currentSelectedGameObject == opener.gameObject)
				{
					current.SetSelectedGameObject(null);
				}
			}
			catch
			{
			}
			onValueChanged?.Invoke(idx, optText);
			if (!DebugOn)
			{
				return;
			}
			manualLogSource = log;
			if (manualLogSource != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(29, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] selected ");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(idx);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(":'");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(optText);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("'");
				}
				manualLogSource.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (System.Exception t)
		{
			if (log != null)
			{
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(36, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] selection handler: ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				log.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
		}
	}

	private static void ApplyCurrentText(RustText currentText, string text)
	{
		if (currentText == null)
		{
			return;
		}
		try
		{
			currentText.IsLocalized = false;
		}
		catch
		{
		}
		try
		{
			currentText.Token = string.Empty;
		}
		catch
		{
		}
		try
		{
			currentText.English = text;
		}
		catch
		{
		}
		try
		{
			currentText.text = text;
		}
		catch
		{
		}
		try
		{
			currentText.raycastTarget = false;
		}
		catch
		{
		}
		EnsureDropdownValueTextVisible(currentText);
		try
		{
			currentText.ForceMeshUpdate();
		}
		catch
		{
		}
	}

	internal static void EnsureDropdownValueTextVisible(RustText currentText)
	{
		if (currentText == null)
		{
			return;
		}
		try
		{
			if (currentText.color.a < 0.05f)
			{
				currentText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
			}
		}
		catch
		{
		}
	}

	private static void HideOptionLabels(RectTransform itemsContainer, ManualLogSource log)
	{
		if (itemsContainer == null)
		{
			return;
		}
		try
		{
			for (int i = 0; i < itemsContainer.childCount; i++)
			{
				Transform child = itemsContainer.GetChild(i);
				if (child == null)
				{
					continue;
				}
				Il2CppArrayBase<TMP_Text> componentsInChildren = child.GetComponentsInChildren<TMP_Text>(includeInactive: true);
				if (componentsInChildren == null)
				{
					continue;
				}
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					try
					{
						if (componentsInChildren[j] != null)
						{
							componentsInChildren[j].enabled = false;
						}
					}
					catch
					{
					}
				}
			}
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(37, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] hide option labels: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static Image CreateRowSwatch(RustButton opener, RustText currentText, Color initialColor, ManualLogSource log)
	{
		if (currentText == null)
		{
			log?.LogWarning("[SystemDropdown] no currentText -> cannot place row swatch");
			return null;
		}
		try
		{
			Transform parent = ((currentText.transform.parent != null) ? currentText.transform.parent : currentText.transform);
			GameObject gameObject = new GameObject("CBSafe_Color_RowSwatch");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rectTransform = currentText.rectTransform;
			RectTransform rectTransform2 = gameObject.AddComponent<RectTransform>();
			if (rectTransform != null)
			{
				rectTransform2.anchorMin = rectTransform.anchorMin;
				rectTransform2.anchorMax = rectTransform.anchorMax;
				rectTransform2.pivot = rectTransform.pivot;
				rectTransform2.anchoredPosition = rectTransform.anchoredPosition;
				rectTransform2.sizeDelta = rectTransform.sizeDelta;
				rectTransform2.offsetMin = rectTransform.offsetMin;
				rectTransform2.offsetMax = rectTransform.offsetMax;
			}
			else
			{
				rectTransform2.anchorMin = new Vector2(0f, 0.18f);
				rectTransform2.anchorMax = new Vector2(1f, 0.82f);
				rectTransform2.offsetMin = new Vector2(8f, 0f);
				rectTransform2.offsetMax = new Vector2(-32f, 0f);
			}
			rectTransform2.offsetMin = new Vector2(rectTransform2.offsetMin.x + 4f, rectTransform2.offsetMin.y + 6f);
			rectTransform2.offsetMax = new Vector2(rectTransform2.offsetMax.x - 4f, rectTransform2.offsetMax.y - 6f);
			Image image = gameObject.AddComponent<Image>();
			image.color = initialColor;
			image.raycastTarget = false;
			gameObject.transform.SetAsLastSibling();
			return image;
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(36, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] create row swatch: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
			return null;
		}
	}

	private static void RepaintOptionSwatches(RectTransform itemsContainer, string prefabName, Color[] colors, ManualLogSource log)
	{
		if (itemsContainer == null || colors == null || colors.Length == 0)
		{
			return;
		}
		bool isEnabled = false;
		try
		{
			ForceEnableItemFlex(itemsContainer, log);
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(61, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] RepaintOptionSwatches: ForceEnableItemFlex: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
		try
		{
			ForceEnableItemInteractables(itemsContainer, log);
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(70, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] RepaintOptionSwatches: ForceEnableItemInteractables: ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
			}
		}
		try
		{
			Il2CppArrayBase<RustButton> componentsInChildren = itemsContainer.GetComponentsInChildren<RustButton>(includeInactive: true);
			if (componentsInChildren == null || componentsInChildren.Length == 0)
			{
				return;
			}
			List<RustButton> list = new List<RustButton>();
			foreach (RustButton item in componentsInChildren)
			{
				if (!(item == null) && !(item.gameObject == null))
				{
					string name = item.gameObject.name;
					if ((string.IsNullOrEmpty(prefabName) || (name != null && name.Contains(prefabName))) && item.gameObject.activeSelf)
					{
						list.Add(item);
					}
				}
			}
			list.Sort(delegate(RustButton a, RustButton b)
			{
				int num = ((((a != null) ? a.transform : null) != null) ? a.transform.GetSiblingIndex() : int.MaxValue);
				int value = ((((b != null) ? b.transform : null) != null) ? b.transform.GetSiblingIndex() : int.MaxValue);
				return num.CompareTo(value);
			});
			if (DebugOn)
			{
				if (log != null)
				{
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(62, 2, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] RepaintOptionSwatches: ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(list.Count);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" active items, ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(colors.Length);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" colors");
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
			}
			for (int i = 0; i < list.Count && i < colors.Length; i++)
			{
				Transform transform = list[i].transform;
				TMP_Text componentInChildren = transform.GetComponentInChildren<TMP_Text>(includeInactive: true);
				if (componentInChildren != null)
				{
					try
					{
						componentInChildren.enabled = false;
					}
					catch
					{
					}
				}
				Image image = null;
				for (int j = 0; j < transform.childCount; j++)
				{
					Transform child = transform.GetChild(j);
					if (child != null && child.name == LocStrings.Name_Color_OptionSwatch)
					{
						image = child.GetComponent<Image>();
						break;
					}
				}
				if (image == null)
				{
					GameObject gameObject = new GameObject(LocStrings.Name_Color_OptionSwatch);
					gameObject.transform.SetParent(transform, worldPositionStays: false);
					RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
					rectTransform.anchorMin = new Vector2(0f, 0f);
					rectTransform.anchorMax = new Vector2(1f, 1f);
					rectTransform.pivot = new Vector2(0.5f, 0.5f);
					rectTransform.offsetMin = new Vector2(8f, 6f);
					rectTransform.offsetMax = new Vector2(-8f, -6f);
					image = gameObject.AddComponent<Image>();
					image.raycastTarget = false;
					gameObject.transform.SetAsLastSibling();
				}
				image.color = colors[i];
				if (!DebugOn)
				{
					continue;
				}
				if (log != null)
				{
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(38, 2, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] repaint item[");
						bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(i);
						bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("] color=");
						bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(colors[i]);
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
				}
			}
		}
		catch (System.Exception ex3)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler3 = new BepInExWarningLogInterpolatedStringHandler(40, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown] RepaintOptionSwatches: ");
					bepInExWarningLogInterpolatedStringHandler3.AppendFormatted(ex3.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler3);
			}
		}
	}

	private static int AddSwatchesToOptions(RectTransform itemsContainer, string prefabName, Color[] colors, ManualLogSource log)
	{
		if (itemsContainer == null || colors == null)
		{
			return 0;
		}
		int num = 0;
		bool isEnabled = false;
		try
		{
			Il2CppArrayBase<RustButton> componentsInChildren = itemsContainer.GetComponentsInChildren<RustButton>(includeInactive: true);
			if (componentsInChildren == null || componentsInChildren.Length == 0)
			{
				if (DebugOn)
				{
					if (log != null)
					{
						BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(79, 2, out isEnabled);
						if (isEnabled)
						{
							bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] options: no RustButton(s) under itemsContainer (");
							bepInExInfoLogInterpolatedStringHandler.AppendFormatted(itemsContainer.name);
							bepInExInfoLogInterpolatedStringHandler.AppendLiteral(", childCount=");
							bepInExInfoLogInterpolatedStringHandler.AppendFormatted(itemsContainer.childCount);
							bepInExInfoLogInterpolatedStringHandler.AppendLiteral(")");
						}
						log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
					}
				}
				return 0;
			}
			List<RustButton> list = new List<RustButton>();
			foreach (RustButton item in componentsInChildren)
			{
				if (!(item == null) && !(item.gameObject == null))
				{
					string name = item.gameObject.name;
					if ((string.IsNullOrEmpty(prefabName) || (name != null && name.Contains(prefabName))) && item.gameObject.activeSelf)
					{
						list.Add(item);
					}
				}
			}
			list.Sort(delegate(RustButton a, RustButton b)
			{
				int num4 = ((a != null && a.transform != null) ? a.transform.GetSiblingIndex() : int.MaxValue);
				int value = ((b != null && b.transform != null) ? b.transform.GetSiblingIndex() : int.MaxValue);
				return num4.CompareTo(value);
			});
			if (DebugOn)
			{
				if (log != null)
				{
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(85, 3, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] options: ");
						bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(componentsInChildren.Length);
						bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(" total RustButton(s), ");
						bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(list.Count);
						bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(" active candidate(s), painting up to ");
						bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(colors.Length);
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
				}
			}
			if (list.Count == 0)
			{
				if (DebugOn)
				{
					if (log != null)
					{
						BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler3 = new BepInExInfoLogInterpolatedStringHandler(75, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown] options: no active candidates yet; listing all ");
							bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(componentsInChildren.Length);
							bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" button(s):");
						}
						log.LogInfo(bepInExInfoLogInterpolatedStringHandler3);
					}
				}
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					RustButton rustButton = componentsInChildren[i];
					if (rustButton == null || rustButton.gameObject == null)
					{
						continue;
					}
					string t = BuildPath(rustButton.transform, itemsContainer);
					if (!DebugOn)
					{
						continue;
					}
					if (log != null)
					{
						BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler4 = new BepInExInfoLogInterpolatedStringHandler(48, 4, out isEnabled);
						if (isEnabled)
						{
							bepInExInfoLogInterpolatedStringHandler4.AppendLiteral("[SystemDropdown]   btn[");
							bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(i);
							bepInExInfoLogInterpolatedStringHandler4.AppendLiteral("] name='");
							bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(rustButton.gameObject.name);
							bepInExInfoLogInterpolatedStringHandler4.AppendLiteral("' active=");
							bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(rustButton.gameObject.activeSelf);
							bepInExInfoLogInterpolatedStringHandler4.AppendLiteral(" path='");
							bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(t);
							bepInExInfoLogInterpolatedStringHandler4.AppendLiteral("'");
						}
						log.LogInfo(bepInExInfoLogInterpolatedStringHandler4);
					}
				}
			}
			int num2 = 0;
			int num3 = 0;
			while (true)
			{
				if (num3 < list.Count)
				{
					if (num2 >= colors.Length)
					{
						break;
					}
					Transform transform = ((list[num3] != null) ? list[num3].transform : null);
					if (!(transform == null))
					{
						bool flag = false;
						for (int j = 0; j < transform.childCount; j++)
						{
							if (transform.GetChild(j) != null && transform.GetChild(j).name == LocStrings.Name_Color_OptionSwatch)
							{
								flag = true;
								break;
							}
						}
						if (flag)
						{
							num2++;
							num++;
						}
						else
						{
							TMP_Text componentInChildren = transform.GetComponentInChildren<TMP_Text>(includeInactive: true);
							if (componentInChildren != null)
							{
								try
								{
									componentInChildren.enabled = false;
								}
								catch
								{
								}
							}
							GameObject gameObject = new GameObject(LocStrings.Name_Color_OptionSwatch);
							gameObject.transform.SetParent(transform, worldPositionStays: false);
							RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
							rectTransform.anchorMin = new Vector2(0f, 0f);
							rectTransform.anchorMax = new Vector2(1f, 1f);
							rectTransform.pivot = new Vector2(0.5f, 0.5f);
							rectTransform.offsetMin = new Vector2(8f, 6f);
							rectTransform.offsetMax = new Vector2(-8f, -6f);
							Image image = gameObject.AddComponent<Image>();
							image.color = colors[num2];
							image.raycastTarget = false;
							gameObject.transform.SetAsLastSibling();
							if (DebugOn)
							{
								if (log != null)
								{
									BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler5 = new BepInExInfoLogInterpolatedStringHandler(49, 3, out isEnabled);
									if (isEnabled)
									{
										bepInExInfoLogInterpolatedStringHandler5.AppendLiteral("[SystemDropdown] painted option[");
										bepInExInfoLogInterpolatedStringHandler5.AppendFormatted(num2);
										bepInExInfoLogInterpolatedStringHandler5.AppendLiteral("] on '");
										bepInExInfoLogInterpolatedStringHandler5.AppendFormatted(transform.name);
										bepInExInfoLogInterpolatedStringHandler5.AppendLiteral("' (path '");
										bepInExInfoLogInterpolatedStringHandler5.AppendFormatted(BuildPath(transform, itemsContainer));
										bepInExInfoLogInterpolatedStringHandler5.AppendLiteral("')");
									}
									log.LogInfo(bepInExInfoLogInterpolatedStringHandler5);
								}
							}
							num2++;
							num++;
						}
					}
					num3++;
					continue;
				}
				return num;
			}
			return num;
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] option swatches: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				return num;
			}
			return num;
		}
	}

	private static string BuildPath(Transform leaf, Transform stopAt)
	{
		if (leaf == null)
		{
			return "?";
		}
		List<string> list = new List<string>();
		Transform transform = leaf;
		int num = 0;
		while (transform != null && transform != stopAt && num++ < 64)
		{
			list.Add(transform.name);
			transform = transform.parent;
		}
		list.Reverse();
		return string.Join("/", list);
	}

	private static void DumpRowHierarchy(GameObject root, ManualLogSource log)
	{
		if (root == null || log == null)
		{
			return;
		}
		try
		{
			if (DebugOn)
			{
				log.LogInfo("[SystemDropdown] ---- row hierarchy dump (begin) ----");
			}
			DumpTransform(root.transform, 0, log);
			if (DebugOn)
			{
				log.LogInfo("[SystemDropdown] ---- row hierarchy dump (end) ----");
			}
		}
		catch (System.Exception ex)
		{
			bool isEnabled = false;
			BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
			if (isEnabled)
			{
				bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] dump hierarchy: ");
				bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
			}
			log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
		}
	}

	private static void DumpTransform(Transform t, int depth, ManualLogSource log)
	{
		if (t == null)
		{
			return;
		}
		string t2 = new string(' ', depth * 2);
		RectTransform rectTransform = (RectTransform)((t is RectTransform) ? t : null);
		string t3;
		if (rectTransform != null)
		{
			Vector2 anchorMin = rectTransform.anchorMin;
			Vector2 anchorMax = rectTransform.anchorMax;
			Vector2 offsetMin = rectTransform.offsetMin;
			Vector2 offsetMax = rectTransform.offsetMax;
			Vector2 sizeDelta = rectTransform.sizeDelta;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 10);
			defaultInterpolatedStringHandler.AppendLiteral("aMin=(");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMin.x, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMin.y, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(") aMax=(");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMax.x, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(anchorMax.y, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(") oMin=(");
			defaultInterpolatedStringHandler.AppendFormatted(offsetMin.x, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(offsetMin.y, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(") oMax=(");
			defaultInterpolatedStringHandler.AppendFormatted(offsetMax.x, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(offsetMax.y, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(") sd=(");
			defaultInterpolatedStringHandler.AppendFormatted(sizeDelta.x, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(sizeDelta.y, "F1");
			defaultInterpolatedStringHandler.AppendLiteral(")");
			t3 = defaultInterpolatedStringHandler.ToStringAndClear();
		}
		else
		{
			t3 = "<no RectTransform>";
		}
		Il2CppArrayBase<Component> components = t.GetComponents<Component>();
		List<string> list = new List<string>();
		if (components != null)
		{
			for (int i = 0; i < components.Length; i++)
			{
				if (!(components[i] == null))
				{
					Il2CppSystem.Type il2CppType = components[i].GetIl2CppType();
					string item = ((il2CppType != null) ? il2CppType.Name : null) ?? "?";
					list.Add(item);
				}
			}
		}
		if (DebugOn)
		{
			bool isEnabled = false;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(21, 4, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t2);
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t.name);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" [");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(string.Join(",", list));
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("] ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t3);
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
		}
		for (int j = 0; j < t.childCount; j++)
		{
			DumpTransform(t.GetChild(j), depth + 1, log);
		}
	}

	private static void ApplyInitialSwatchColour(TweakUIDropdown tud, Color color, ManualLogSource log)
	{
		if (tud == null)
		{
			return;
		}
		try
		{
			if (tud.BackgroundImage != null)
			{
				tud.BackgroundImage.color = color;
			}
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(40, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] initial swatch colour: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void WireDismisser(RectTransform panel, RustButton opener, ManualLogSource log)
	{
		if (panel == null)
		{
			return;
		}
		try
		{
			Transform transform = null;
			for (int i = 0; i < panel.childCount; i++)
			{
				Transform child = panel.GetChild(i);
				if (string.Equals(child.name, "Dismisser", System.StringComparison.OrdinalIgnoreCase))
				{
					transform = child;
					break;
				}
			}
			if (transform == null)
			{
				return;
			}
			DestroyTweakComponents(transform.gameObject, log);
			RustButton component = transform.GetComponent<RustButton>();
			if (component != null)
			{
				try
				{
					component.OnReleased.RemoveAllListeners();
				}
				catch
				{
				}
				component.OnReleased.AddListener((System.Action)delegate
				{
					Close();
				});
				return;
			}
			Button button = transform.GetComponent<Button>() ?? transform.gameObject.AddComponent<Button>();
			try
			{
				button.onClick.RemoveAllListeners();
			}
			catch
			{
			}
			button.onClick.AddListener((System.Action)delegate
			{
				Close();
			});
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] dismisser wire: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
		void Close()
		{
			try
			{
				if (opener != null)
				{
					opener.SetToggleFalse();
				}
			}
			catch
			{
			}
			panel.gameObject.SetActive(value: false);
		}
	}

	private static void WireOpener(RustButton opener, RectTransform dropdownPanel, ManualLogSource log)
	{
		if (opener == null)
		{
			log?.LogWarning("[SystemDropdown] opener is null - dropdown cannot be opened");
			return;
		}
		if (dropdownPanel == null)
		{
			log?.LogWarning("[SystemDropdown] dropdownPanel is null - cannot toggle");
			return;
		}
		try
		{
			string t = ((opener.TabPanelTarget != null) ? opener.TabPanelTarget.name : "null");
			if (DebugOn && log != null)
			{
				bool isEnabled = false;
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(79, 4, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] opener pre: IsToggle=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(opener.IsToggle);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("TabPanelTarget=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ");
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("PreventToggleOff=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(opener.PreventToggleOff);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" Value=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(opener.Value);
				}
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch
		{
		}
		try
		{
			opener.IsToggle = true;
		}
		catch
		{
		}
		try
		{
			opener.TabPanelTarget = dropdownPanel.transform;
		}
		catch
		{
		}
		try
		{
			opener.PreventToggleOff = false;
		}
		catch
		{
		}
		try
		{
			opener.OnPressed.RemoveAllListeners();
		}
		catch
		{
		}
		try
		{
			opener.OnReleased.RemoveAllListeners();
		}
		catch
		{
		}
		_registered.Add((opener, dropdownPanel.gameObject));
	}
}
