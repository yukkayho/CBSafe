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

namespace CBSafe.Crosshair.UI;

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

	private sealed class CC__DisplayClass39_0
	{
		public string[] options;

		public Color[] colors;

		public System.Action<int, string> onValueChanged;

		public ManualLogSource log;

		public bool colourSwatchMode;

		public TweakUIDropdown tud;

		public RectTransform dropdownPanel;

		public RustButton opener;

		public RustText currentText;
	}

	private const int PanelStableSkipAfter = 6;

	private static readonly List<(RustButton, GameObject)> _registered = new List<(RustButton, GameObject)>();

	private static readonly List<DropdownHandle> _handles = new List<DropdownHandle>();

	private static readonly Dictionary<TweakUIDropdown, Image> _rowSwatchByDropdown = new Dictionary<TweakUIDropdown, Image>();

	private static readonly Dictionary<TweakUIDropdown, PaintContext> _paintCtxByDropdown = new Dictionary<TweakUIDropdown, PaintContext>();

	private static readonly Dictionary<RustButton, bool[]> _ownStateByOpener = new Dictionary<RustButton, bool[]>();

	private static readonly Dictionary<RustButton, int[]> _repaintBudgetByOpener = new Dictionary<RustButton, int[]>();

	private static readonly Dictionary<RustButton, AnimState> _animByOpener = new Dictionary<RustButton, AnimState>();

	private const float AnimDurationSec = 0.18f;

	private static GameObject _cachedRowTemplate;

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

	private static GameObject _tooltipObjectTemplate;

	private static readonly Color PanelBgGrey = new Color(13f / 85f, 13f / 85f, 0.12156863f, 1f);

	private static readonly HashSet<RectTransform> _panelDumpDone = new HashSet<RectTransform>();

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
					if (!(child == null) && !(child.name != LocStrings.Name_Crosshair_Dismisser))
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
			CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
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
			CBSafeCrosshairPlugin instance2 = CBSafeCrosshairPlugin.Instance;
			if (instance2 == null)
			{
				return;
			}
			ManualLogSource log2 = instance2.Log;
			if (log2 != null)
			{
				bool isEnabled2 = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(28, 1, out isEnabled2);
				if (isEnabled2)
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
				if (item.Item1 != null)
				{
					item.Item1.SetToggleFalse();
				}
			}
			catch
			{
			}
			if (item.Item2 != null)
			{
				item.Item2.SetActive(value: false);
			}
			if (item.Item1 != null)
			{
				if (_ownStateByOpener.TryGetValue(item.Item1, out var value))
				{
					value[0] = false;
				}
				if (_animByOpener.TryGetValue(item.Item1, out var value2))
				{
					value2.TargetOpen = false;
					value2.Progress = 0f;
				}
				HideDropdownTooltip(item.Item1);
			}
		}
		CloseModal();
	}

	public static void RequestAnimatedCloseExcept(RustButton exceptOpener)
	{
		foreach (var item in _registered)
		{
			if (!(item.Item1 == null) && !(item.Item1 == exceptOpener))
			{
				if (_animByOpener.TryGetValue(item.Item1, out var value))
				{
					value.TargetOpen = false;
				}
				if (_ownStateByOpener.TryGetValue(item.Item1, out var value2))
				{
					value2[0] = false;
				}
				HideDropdownTooltip(item.Item1);
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
			if (item.Item1 != null)
			{
				try
				{
					item.Item1.SetToggleFalse();
				}
				catch
				{
				}
				try
				{
					item.Item1.Value = false;
				}
				catch
				{
				}
				if (_ownStateByOpener.TryGetValue(item.Item1, out var value))
				{
					value[0] = false;
				}
				if (_animByOpener.TryGetValue(item.Item1, out var value2))
				{
					value2.TargetOpen = false;
					value2.Progress = 0f;
				}
				HideDropdownTooltip(item.Item1);
			}
			if (item.Item2 != null)
			{
				item.Item2.SetActive(value: false);
			}
		}
		CloseModal();
	}

	public static bool IsAnyDropdownOpen()
	{
		foreach (var item in _registered)
		{
			if (!(item.Item1 == null))
			{
				if (_ownStateByOpener.TryGetValue(item.Item1, out var value) && value[0])
				{
					return true;
				}
				if (_animByOpener.TryGetValue(item.Item1, out var value2) && (value2.TargetOpen || value2.Progress > 0.01f))
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
			if (!(item.Item1 == null))
			{
				if (_animByOpener.TryGetValue(item.Item1, out var value))
				{
					value.TargetOpen = false;
				}
				if (_ownStateByOpener.TryGetValue(item.Item1, out var value2))
				{
					value2[0] = false;
				}
				HideDropdownTooltip(item.Item1);
			}
		}
	}

	private static void DumpTooltipApi(Component comp, ManualLogSource log)
	{
		if (comp == null || log == null)
		{
			return;
		}
		try
		{
			Il2CppSystem.Type il2CppType = comp.GetIl2CppType();
			bool isEnabled = false;
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(45, 2, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown][diag] Tooltip type: ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(il2CppType.FullName);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" (base=");
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = bepInExInfoLogInterpolatedStringHandler;
				Il2CppSystem.Type baseType = il2CppType.BaseType;
				bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(((baseType != null) ? baseType.FullName : null) ?? "null");
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(")");
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
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
								string[] array2 = new string[6];
								int num3 = 0;
								Il2CppSystem.Type returnType = methodInfo.ReturnType;
								array2[num3] = ((returnType != null) ? returnType.Name : null) ?? "?";
								array2[1] = " ";
								array2[2] = methodInfo.Name;
								array2[3] = "(";
								array2[4] = string.Join(",", array);
								array2[5] = ")";
								text = string.Concat(array2);
							}
							catch
							{
								text = methodInfo.Name;
							}
							if (hashSet.Add(text))
							{
								bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(31, 2, out isEnabled);
								if (isEnabled)
								{
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown][diag]   M @ ");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(type.Name);
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(": ");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(text);
								}
								log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
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
						if (!(fieldInfo == null))
						{
							bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(29, 2, out isEnabled);
							if (isEnabled)
							{
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown][diag]   F: ");
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler3 = bepInExInfoLogInterpolatedStringHandler;
								Il2CppSystem.Type fieldType = fieldInfo.FieldType;
								bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(((fieldType != null) ? fieldType.Name : null) ?? "?");
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ");
								bepInExInfoLogInterpolatedStringHandler.AppendFormatted(fieldInfo.Name);
							}
							log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
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
				Il2CppSystem.Type type2;
				if ((type2 = Il2CppSystem.Type.GetType("Rust.UI.TooltipController, Rust.UI")) == null)
				{
					type2 = Il2CppSystem.Type.GetType("TooltipController, Rust.UI") ?? Il2CppSystem.Type.GetType("Rust.UI.TooltipController");
				}
				Il2CppSystem.Type type3 = type2;
				if (type3 != null)
				{
					bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(47, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown][diag] TooltipController type: ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(type3.FullName);
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
					Il2CppReferenceArray<Il2CppSystem.Reflection.MethodInfo> methods2 = type3.GetMethods();
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
								string[] array3 = new string[((Il2CppArrayBase<Il2CppSystem.Reflection.ParameterInfo>)parameters2)?.Length ?? 0];
								for (int m = 0; m < array3.Length; m++)
								{
									int num4 = m;
									Il2CppSystem.Type parameterType2 = ((Il2CppArrayBase<Il2CppSystem.Reflection.ParameterInfo>)parameters2)[m].ParameterType;
									array3[num4] = ((parameterType2 != null) ? parameterType2.Name : null) ?? "?";
								}
								string[] array4 = new string[6];
								int num5 = 0;
								Il2CppSystem.Type returnType2 = methodInfo2.ReturnType;
								array4[num5] = ((returnType2 != null) ? returnType2.Name : null) ?? "?";
								array4[1] = " ";
								array4[2] = methodInfo2.Name;
								array4[3] = "(";
								array4[4] = string.Join(",", array3);
								array4[5] = ")";
								text2 = string.Concat(array4);
							}
							catch
							{
								text2 = methodInfo2.Name;
							}
							if (hashSet2.Add(text2))
							{
								bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(31, 1, out isEnabled);
								if (isEnabled)
								{
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown][diag]   TC M: ");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(text2);
								}
								log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
							}
						}
					}
					Il2CppReferenceArray<Il2CppSystem.Reflection.FieldInfo> fields2 = type3.GetFields();
					if (fields2 == null)
					{
						return;
					}
					for (int n = 0; n < ((Il2CppArrayBase<Il2CppSystem.Reflection.FieldInfo>)fields2).Length; n++)
					{
						Il2CppSystem.Reflection.FieldInfo fieldInfo2 = ((Il2CppArrayBase<Il2CppSystem.Reflection.FieldInfo>)fields2)[n];
						if (!(fieldInfo2 == null))
						{
							bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(32, 2, out isEnabled);
							if (isEnabled)
							{
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown][diag]   TC F: ");
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler4 = bepInExInfoLogInterpolatedStringHandler;
								Il2CppSystem.Type fieldType2 = fieldInfo2.FieldType;
								bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(((fieldType2 != null) ? fieldType2.Name : null) ?? "?");
								bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" ");
								bepInExInfoLogInterpolatedStringHandler.AppendFormatted(fieldInfo2.Name);
							}
							log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
						}
					}
				}
				else
				{
					bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(70, 0, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown][diag] TooltipController type not found by name lookup");
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
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
			bool isEnabled2 = false;
			BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler4 = new BepInExWarningLogInterpolatedStringHandler(39, 1, out isEnabled2);
			if (isEnabled2)
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
			CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
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
				Main.Instance?.Log?.LogWarning("[SystemDropdown][CH] OpenTooltip failed: " + ex.Message);
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
			RectTransform itemsContainer = value.ItemsContainer;
			string prefabName = value.PrefabName;
			Color[] colors = value.Colors;
			ForceEnableItemFlex(itemsContainer, CBSafeCrosshairPlugin.Instance?.Log);
			ForceEnableItemInteractables(itemsContainer, CBSafeCrosshairPlugin.Instance?.Log);
			RepaintOptionSwatches(itemsContainer, prefabName, colors, CBSafeCrosshairPlugin.Instance?.Log);
		}
		catch (System.Exception ex)
		{
			CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
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
		CC__DisplayClass39_0 __locals1 = new CC__DisplayClass39_0();
		__locals1.options = options;
		__locals1.colors = colors;
		__locals1.onValueChanged = onValueChanged;
		__locals1.log = CBSafeCrosshairPlugin.Instance?.Log;
		bool flag = !string.IsNullOrEmpty(tooltipText);
		if (__locals1.options == null || __locals1.options.Length == 0)
		{
			__locals1.log?.LogWarning("[SystemDropdown] no options provided");
			return null;
		}
		__locals1.colourSwatchMode = __locals1.colors != null && __locals1.colors.Length == __locals1.options.Length;
		GameObject gameObject = FindRowTemplate(__locals1.log);
		if (gameObject == null)
		{
			__locals1.log?.LogError("[SystemDropdown] no template row found (no TweakUIDropdown in scene)");
			return null;
		}
		GameObject gameObject2 = new GameObject("CBSafe_Crosshair_TempParent");
		gameObject2.SetActive(value: false);
		bool isEnabled = false;
		GameObject gameObject3;
		ManualLogSource log;
		try
		{
			gameObject3 = UnityEngine.Object.Instantiate(gameObject, gameObject2.transform, worldPositionStays: false);
			gameObject3.name = "CBSafe_Crosshair_Dropdown_" + labelText;
		}
		catch (System.Exception t)
		{
			log = __locals1.log;
			if (log != null)
			{
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(37, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] instantiate failed: ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				log.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
			UnityEngine.Object.Destroy(gameObject2);
			return null;
		}
		__locals1.tud = gameObject3.GetComponent<TweakUIDropdown>();
		if (__locals1.tud == null)
		{
			__locals1.tud = gameObject3.GetComponentInChildren<TweakUIDropdown>(includeInactive: true);
		}
		__locals1.dropdownPanel = ((__locals1.tud != null) ? __locals1.tud.Dropdown : null);
		RectTransform rectTransform = ((__locals1.tud != null) ? __locals1.tud.DropdownContainer : null);
		GameObject gameObject4 = ((__locals1.tud != null) ? __locals1.tud.DropdownItemPrefab : null);
		__locals1.opener = ((__locals1.tud != null) ? __locals1.tud.Opener : null);
		__locals1.currentText = ((__locals1.tud != null) ? __locals1.tud.Current : null);
		log = __locals1.log;
		if (__locals1.tud != null)
		{
			try
			{
				__locals1.tud.convarName = string.Empty;
			}
			catch (System.Exception ex)
			{
				log = __locals1.log;
				if (log != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(35, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] clear convarName: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
			try
			{
				DisablePersistent(__locals1.tud.onValueChanged);
			}
			catch (System.Exception ex2)
			{
				__locals1.log?.LogWarning("[SystemDropdown] disable persistent tud.onValueChanged: " + ex2.Message);
			}
			try
			{
				Il2CppReferenceArray<TweakUIDropdown.NameValue> il2CppReferenceArray = new Il2CppReferenceArray<TweakUIDropdown.NameValue>(__locals1.options.Length);
				for (int i = 0; i < __locals1.options.Length; i++)
				{
					TweakUIDropdown.NameValue nameValue = new TweakUIDropdown.NameValue();
					nameValue.value = __locals1.options[i];
					string eng = (__locals1.colourSwatchMode ? string.Empty : __locals1.options[i]);
					try
					{
						nameValue.label = new Translate.Phrase(string.Empty, eng);
					}
					catch
					{
					}
					if (__locals1.colourSwatchMode)
					{
						try
						{
							nameValue.imageColor = __locals1.colors[i];
						}
						catch
						{
						}
					}
					((Il2CppArrayBase<TweakUIDropdown.NameValue>)il2CppReferenceArray)[i] = nameValue;
				}
				__locals1.tud.nameValues = il2CppReferenceArray;
			}
			catch (System.Exception ex3)
			{
				log = __locals1.log;
				if (log != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] set nameValues: ");
						bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex3.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
				}
			}
			if (__locals1.colourSwatchMode)
			{
				try
				{
					__locals1.tud.assignImageColor = false;
				}
				catch (System.Exception ex4)
				{
					log = __locals1.log;
					if (log != null)
					{
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler3 = new BepInExWarningLogInterpolatedStringHandler(45, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown] set assignImageColor=false: ");
							bepInExWarningLogInterpolatedStringHandler3.AppendFormatted(ex4.Message);
						}
						log.LogWarning(bepInExWarningLogInterpolatedStringHandler3);
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
			__locals1.log?.LogWarning("[SystemDropdown] reset row CG: " + ex5.Message);
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
		UpdateLabel(gameObject3, labelText, __locals1.log);
		DefuseTooltip(gameObject3, flag ? tooltipText : null, __locals1.log);
		if (__locals1.opener != null && flag)
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
				_tooltipsByOpener[__locals1.opener] = list;
				RustButton capturedOpener_TT = __locals1.opener;
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
				log = __locals1.log;
				if (log != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler4 = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler4.AppendLiteral("[SystemDropdown] tooltip capture: ");
						bepInExWarningLogInterpolatedStringHandler4.AppendFormatted(ex6.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler4);
				}
			}
		}
		if (__locals1.tud != null)
		{
			if (__locals1.tud.DropdownItemPrefab == null)
			{
				EnsureDropdownPrefabFromAnySource(__locals1.tud, __locals1.log);
			}
			if (__locals1.tud.DropdownItemPrefab != null)
			{
				try
				{
					__locals1.tud.UpdateDropdownOptions();
				}
				catch (System.Exception ex7)
				{
					__locals1.log?.LogWarning("[SystemDropdown] UpdateDropdownOptions: " + ex7.Message);
				}
			}
		}
		if (__locals1.tud != null && __locals1.tud.onValueChanged != null)
		{
			try
			{
				__locals1.tud.onValueChanged.AddListener((System.Action)delegate
				{
					try
					{
						int currentValue = __locals1.tud.currentValue;
						if (currentValue < 0 || currentValue >= __locals1.options.Length)
						{
							ManualLogSource log9 = __locals1.log;
							if (log9 != null)
							{
								bool isEnabled7 = false;
								BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler14 = new BepInExWarningLogInterpolatedStringHandler(50, 1, out isEnabled7);
								if (isEnabled7)
								{
									bepInExWarningLogInterpolatedStringHandler14.AppendLiteral("[SystemDropdown] onValueChanged idx out of range: ");
									bepInExWarningLogInterpolatedStringHandler14.AppendFormatted(currentValue);
								}
								log9.LogWarning(bepInExWarningLogInterpolatedStringHandler14);
							}
						}
						else
						{
							string text2 = __locals1.options[currentValue];
							if (__locals1.colourSwatchMode)
							{
								ApplyCurrentText(__locals1.currentText, string.Empty);
								if (_rowSwatchByDropdown.TryGetValue(__locals1.tud, out var value6) && value6 != null)
								{
									value6.color = __locals1.colors[currentValue];
								}
							}
							else
							{
								ApplyCurrentText(__locals1.currentText, text2);
							}
							try
							{
								if (__locals1.opener != null)
								{
									try
									{
										__locals1.opener.Value = false;
									}
									catch
									{
									}
								}
								if (__locals1.opener != null && _ownStateByOpener.TryGetValue(__locals1.opener, out var value7))
								{
									value7[0] = false;
								}
								if (__locals1.opener != null && _animByOpener.TryGetValue(__locals1.opener, out var value8))
								{
									value8.TargetOpen = false;
								}
								else if (__locals1.dropdownPanel != null && __locals1.dropdownPanel.gameObject.activeSelf)
								{
									__locals1.dropdownPanel.gameObject.SetActive(value: false);
								}
								HideDropdownTooltip(__locals1.opener);
								CloseModal();
							}
							catch
							{
							}
							__locals1.onValueChanged?.Invoke(currentValue, text2);
							ManualLogSource log10 = __locals1.log;
							if (log10 != null)
							{
								bool isEnabled8 = false;
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler4 = new BepInExInfoLogInterpolatedStringHandler(29, 2, out isEnabled8);
								if (isEnabled8)
								{
									bepInExInfoLogInterpolatedStringHandler4.AppendLiteral("[SystemDropdown] selected ");
									bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(currentValue);
									bepInExInfoLogInterpolatedStringHandler4.AppendLiteral(":'");
									bepInExInfoLogInterpolatedStringHandler4.AppendFormatted(text2);
									bepInExInfoLogInterpolatedStringHandler4.AppendLiteral("'");
								}
								log10.LogInfo(bepInExInfoLogInterpolatedStringHandler4);
							}
						}
					}
					catch (System.Exception t10)
					{
						ManualLogSource log11 = __locals1.log;
						if (log11 != null)
						{
							bool isEnabled9 = false;
							BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler2 = new BepInExErrorLogInterpolatedStringHandler(41, 1, out isEnabled9);
							if (isEnabled9)
							{
								bepInExErrorLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] onValueChanged handler: ");
								bepInExErrorLogInterpolatedStringHandler2.AppendFormatted(t10);
							}
							log11.LogError(bepInExErrorLogInterpolatedStringHandler2);
						}
					}
				});
			}
			catch (System.Exception ex8)
			{
				log = __locals1.log;
				if (log != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler5 = new BepInExWarningLogInterpolatedStringHandler(43, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler5.AppendLiteral("[SystemDropdown] subscribe onValueChanged: ");
						bepInExWarningLogInterpolatedStringHandler5.AppendFormatted(ex8.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler5);
				}
			}
		}
		if (__locals1.opener != null && __locals1.dropdownPanel != null)
		{
			_registered.Add((__locals1.opener, __locals1.dropdownPanel.gameObject));
		}
		Image image = null;
		if (__locals1.colourSwatchMode)
		{
			ApplyCurrentText(__locals1.currentText, string.Empty);
			image = CreateRowSwatch(__locals1.opener, __locals1.currentText, __locals1.colors[0], __locals1.log);
			if (image != null)
			{
				_rowSwatchByDropdown[__locals1.tud] = image;
			}
		}
		else
		{
			ApplyCurrentText(__locals1.currentText, __locals1.options[0]);
		}
		string text = ((gameObject4 != null) ? gameObject4.name : null);
		if (__locals1.colourSwatchMode && rectTransform != null)
		{
			_paintCtxByDropdown[__locals1.tud] = new PaintContext
			{
				ItemsContainer = rectTransform,
				PrefabName = text,
				Colors = __locals1.colors
			};
		}
		if (__locals1.opener != null && __locals1.dropdownPanel != null)
		{
			try
			{
				DisablePrefabListeners(__locals1.opener);
				DestroyPanelDismisser(__locals1.dropdownPanel, __locals1.log);
				ClearAnimationEvents(gameObject3, __locals1.log);
				DisableComponentByName(__locals1.dropdownPanel.gameObject, "FlexTransition", __locals1.log);
				try
				{
					Transform skipSubtree = ((rectTransform != null) ? rectTransform.transform : null);
					int num = DisableComponentByNameRecursiveExcept(gameObject3, "FlexTransition", skipSubtree, __locals1.log);
					int num2 = KillLeanTweensExcept(gameObject3, skipSubtree, __locals1.log);
					__locals1.log?.LogInfo("[SystemDropdown] flex/tween wipe (items-safe): disabled " + num + " FlexTransition(s), cancelled tweens on " + num2 + " GO(s)");
				}
				catch (System.Exception ex9)
				{
					__locals1.log?.LogWarning("[SystemDropdown] flex/tween wipe: " + ex9.Message);
				}
				try
				{
					if (gameObject3 != null)
					{
						Transform transform = ((__locals1.opener != null) ? __locals1.opener.transform : null);
						Transform transform2 = ((rectTransform != null) ? rectTransform.transform : null);
						int num3 = 0;
						int num4 = 0;
						foreach (Animator componentsInChild2 in gameObject3.GetComponentsInChildren<Animator>(includeInactive: true))
						{
							if (componentsInChild2 == null)
							{
								continue;
							}
							if (transform != null && componentsInChild2.transform != null && (componentsInChild2.transform == transform || componentsInChild2.transform.IsChildOf(transform)))
							{
								num4++;
								continue;
							}
							if (transform2 != null && componentsInChild2.transform != null && (componentsInChild2.transform == transform2 || componentsInChild2.transform.IsChildOf(transform2)))
							{
								num4++;
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
								num3++;
							}
							catch
							{
								try
								{
									UnityEngine.Object.Destroy(componentsInChild2);
									num3++;
								}
								catch
								{
								}
							}
						}
						__locals1.log?.LogInfo("[SystemDropdown] killed " + num3 + " Animator(s) on row (skipped " + num4 + " on opener/items)");
						if (__locals1.dropdownPanel != null)
						{
							__locals1.dropdownPanel.localScale = new Vector3(1f, 0f, 1f);
							CanvasGroup component3 = __locals1.dropdownPanel.GetComponent<CanvasGroup>();
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
					__locals1.log?.LogWarning("[SystemDropdown] kill row animators: " + ex10.Message);
				}
				try
				{
					__locals1.opener.OnPressed.RemoveAllListeners();
				}
				catch
				{
				}
				try
				{
					__locals1.opener.OnReleased.RemoveAllListeners();
				}
				catch
				{
				}
				try
				{
					__locals1.opener.TabPanelTarget = null;
				}
				catch
				{
				}
				try
				{
					__locals1.opener.IsToggle = false;
				}
				catch
				{
				}
				try
				{
					__locals1.opener.PreventToggleOff = false;
				}
				catch
				{
				}
				bool[] ownState = new bool[1];
				_ownStateByOpener[__locals1.opener] = ownState;
				Transform transform3 = FindArrowChild(gameObject3.transform, __locals1.log);
				RectTransform component4 = gameObject3.GetComponent<RectTransform>();
				int[] diagBudget = new int[1] { 12 };
				int[] watchDiagBudget = new int[1] { 12 };
				int[] stableFrames = new int[1];
				RectTransform capturedPanel = __locals1.dropdownPanel;
				RustButton capturedOpener = __locals1.opener;
				RectTransform capturedItems = rectTransform;
				Color[] capturedColors = __locals1.colors;
				string capturedPrefabName = text;
				Transform capturedArrow = transform3;
				RectTransform capturedRowRect = component4;
				int capturedOptionsCount = ((__locals1.options != null) ? __locals1.options.Length : 0);
				bool capturedSwatchMode = __locals1.colourSwatchMode;
				__locals1.opener.OnPressed.AddListener((System.Action)delegate
				{
					try
					{
						if (!(capturedPanel == null))
						{
							RequestAnimatedCloseExcept(capturedOpener);
							int frameCount = Time.frameCount;
							bool flag3 = ownState[0];
							bool flag4 = !flag3;
							ownState[0] = flag4;
							if (!_animByOpener.TryGetValue(capturedOpener, out var value4))
							{
								value4 = new AnimState();
								_animByOpener[capturedOpener] = value4;
							}
							value4.TargetOpen = flag4;
							if (flag4)
							{
								watchDiagBudget[0] = 12;
							}
							bool isEnabled5 = false;
							ManualLogSource log7;
							if (flag4)
							{
								try
								{
									KillLeanTweensExcept(capturedPanel.gameObject, (capturedItems != null) ? capturedItems.transform : null, __locals1.log);
								}
								catch (System.Exception ex17)
								{
									__locals1.log?.LogWarning("[SystemDropdown] OnPressed KillLeanTweens: " + ex17.Message);
								}
								try
								{
									ApplyAnimProgress(capturedPanel, capturedArrow, value4.Progress);
								}
								catch (System.Exception ex18)
								{
									log7 = __locals1.log;
									if (log7 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler10 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled5);
										if (isEnabled5)
										{
											bepInExWarningLogInterpolatedStringHandler10.AppendLiteral("[SystemDropdown] apply progress: ");
											bepInExWarningLogInterpolatedStringHandler10.AppendFormatted(ex18.Message);
										}
										log7.LogWarning(bepInExWarningLogInterpolatedStringHandler10);
									}
								}
								capturedPanel.gameObject.SetActive(value: true);
								try
								{
									KillLeanTweensExcept(capturedPanel.gameObject, (capturedItems != null) ? capturedItems.transform : null, __locals1.log);
								}
								catch (System.Exception ex19)
								{
									__locals1.log?.LogWarning("[SystemDropdown] OnPressed post-SetActive KillLeanTweens: " + ex19.Message);
								}
								try
								{
									EnforcePanelLayout(capturedPanel, capturedItems, capturedOptionsCount, __locals1.log);
								}
								catch (System.Exception ex20)
								{
									__locals1.log?.LogWarning("[SystemDropdown] OnPressed EnforcePanelLayout: " + ex20.Message);
								}
								try
								{
									if (capturedItems != null)
									{
										ForceEnableItemFlex(capturedItems, __locals1.log);
									}
								}
								catch (System.Exception ex21)
								{
									__locals1.log?.LogWarning("[SystemDropdown] OnPressed ForceEnableItemFlex: " + ex21.Message);
								}
								try
								{
									if (capturedItems != null)
									{
										ForceEnableItemInteractables(capturedItems, __locals1.log);
									}
								}
								catch (System.Exception ex22)
								{
									__locals1.log?.LogWarning("[SystemDropdown] OnPressed ForceEnableItemInteractables: " + ex22.Message);
								}
								try
								{
									EnsureScrollWired(capturedPanel, capturedItems, __locals1.log);
								}
								catch (System.Exception ex23)
								{
									__locals1.log?.LogWarning("[SystemDropdown] OnPressed EnsureScrollWired: " + ex23.Message);
								}
								try
								{
									EnsurePanelBackground(capturedPanel, __locals1.log);
								}
								catch (System.Exception ex24)
								{
									__locals1.log?.LogWarning("[SystemDropdown] OnPressed EnsurePanelBackground: " + ex24.Message);
								}
								try
								{
									DumpDropdownPanelOnce(capturedPanel, __locals1.log);
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
								catch (System.Exception ex25)
								{
									log7 = __locals1.log;
									if (log7 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler11 = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled5);
										if (isEnabled5)
										{
											bepInExWarningLogInterpolatedStringHandler11.AppendLiteral("[SystemDropdown] layout rebuild: ");
											bepInExWarningLogInterpolatedStringHandler11.AppendFormatted(ex25.Message);
										}
										log7.LogWarning(bepInExWarningLogInterpolatedStringHandler11);
									}
								}
							}
							log7 = __locals1.log;
							if (log7 != null)
							{
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler3 = new BepInExInfoLogInterpolatedStringHandler(63, 6, out isEnabled5);
								if (isEnabled5)
								{
									bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown] OnPressed: own ");
									bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(flag3);
									bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("->");
									bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(flag4);
									bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" target=");
									bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(value4.TargetOpen);
									bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" prog=");
									bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(value4.Progress, "F2");
									bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" active=");
									bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(capturedPanel.gameObject.activeSelf);
									bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" frame=");
									bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(frameCount);
								}
								log7.LogInfo(bepInExInfoLogInterpolatedStringHandler3);
							}
							if (flag4 && diagBudget[0] > 0)
							{
								diagBudget[0]--;
								LogPanelDiagnostics(capturedPanel, capturedOpener, __locals1.log, frameCount);
							}
							if (capturedSwatchMode && flag4 && capturedItems != null)
							{
								try
								{
									RepaintOptionSwatches(capturedItems, capturedPrefabName, capturedColors, __locals1.log);
								}
								catch (System.Exception ex26)
								{
									log7 = __locals1.log;
									if (log7 != null)
									{
										BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler12 = new BepInExWarningLogInterpolatedStringHandler(36, 1, out isEnabled5);
										if (isEnabled5)
										{
											bepInExWarningLogInterpolatedStringHandler12.AppendLiteral("[SystemDropdown] OnPressed repaint: ");
											bepInExWarningLogInterpolatedStringHandler12.AppendFormatted(ex26.Message);
										}
										log7.LogWarning(bepInExWarningLogInterpolatedStringHandler12);
									}
								}
								if (_repaintBudgetByOpener.TryGetValue(capturedOpener, out var value5))
								{
									value5[0] = 4;
								}
							}
							if (flag4)
							{
								ShowDropdownTooltip(capturedOpener);
							}
							else
							{
								HideDropdownTooltip(capturedOpener);
							}
							if (flag4)
							{
								OpenModal(capturedOpener, capturedPanel);
							}
							else
							{
								CloseModal();
							}
						}
					}
					catch (System.Exception ex27)
					{
						ManualLogSource log8 = __locals1.log;
						if (log8 != null)
						{
							bool isEnabled6 = false;
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler13 = new BepInExWarningLogInterpolatedStringHandler(35, 1, out isEnabled6);
							if (isEnabled6)
							{
								bepInExWarningLogInterpolatedStringHandler13.AppendLiteral("[SystemDropdown] OnPressed toggle: ");
								bepInExWarningLogInterpolatedStringHandler13.AppendFormatted(ex27.Message);
							}
							log8.LogWarning(bepInExWarningLogInterpolatedStringHandler13);
						}
					}
				});
				__locals1.opener.OnReleased.AddListener((System.Action)delegate
				{
					try
					{
						ManualLogSource log6 = __locals1.log;
						if (log6 != null)
						{
							bool isEnabled4 = false;
							BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(61, 3, out isEnabled4);
							if (isEnabled4)
							{
								bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] OnReleased (noop): panel.activeSelf=");
								bepInExInfoLogInterpolatedStringHandler2.AppendFormatted((capturedPanel != null) ? new bool?(capturedPanel.gameObject.activeSelf) : null);
								bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(" own=");
								bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(ownState[0]);
								bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(" V=");
								bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(capturedOpener.Value);
							}
							log6.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
						}
					}
					catch
					{
					}
				});
				int[] watchRepaintBudget = new int[1] { 4 };
				if (capturedSwatchMode)
				{
					_repaintBudgetByOpener[__locals1.opener] = watchRepaintBudget;
				}
				bool[] watchPrevActive = new bool[1];
				PanelOpenWatcher.Attach(__locals1.dropdownPanel.gameObject, delegate
				{
					AnimState animState = null;
					bool isEnabled2 = false;
					try
					{
						if (capturedOpener != null && _animByOpener.TryGetValue(capturedOpener, out var value))
						{
							animState = value;
							float num5 = Time.unscaledDeltaTime;
							if (num5 > 0.05f)
							{
								num5 = 0.05f;
							}
							float num6 = num5 / 0.18f;
							if (value.TargetOpen)
							{
								value.Progress += num6;
							}
							else
							{
								value.Progress -= num6;
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
						ManualLogSource log2 = __locals1.log;
						if (log2 != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler7 = new BepInExWarningLogInterpolatedStringHandler(31, 1, out isEnabled2);
							if (isEnabled2)
							{
								bepInExWarningLogInterpolatedStringHandler7.AppendLiteral("[SystemDropdown] watcher anim: ");
								bepInExWarningLogInterpolatedStringHandler7.AppendFormatted(ex12.Message);
							}
							log2.LogWarning(bepInExWarningLogInterpolatedStringHandler7);
						}
					}
					try
					{
						if (animState != null && animState.TargetOpen && capturedPanel != null && capturedPanel.gameObject.activeInHierarchy)
						{
							EnforcePanelLayout(capturedPanel, capturedItems, capturedOptionsCount, null);
							bool num7 = animState.Progress < 0.999f;
							if (num7)
							{
								stableFrames[0] = 0;
							}
							else if (stableFrames[0] < 7)
							{
								stableFrames[0]++;
							}
							if (num7 || stableFrames[0] <= 6)
							{
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
						__locals1.log?.LogWarning("[SystemDropdown] watcher layout: " + ex13.Message);
					}
					try
					{
						if (DebugOn && watchDiagBudget[0] > 0 && capturedPanel != null)
						{
							watchDiagBudget[0]--;
							Vector3 localScale = capturedPanel.localScale;
							Vector2 sizeDelta = capturedPanel.sizeDelta;
							Rect rect = capturedPanel.rect;
							CanvasGroup component5 = capturedPanel.GetComponent<CanvasGroup>();
							float t2 = animState?.Progress ?? (-1f);
							bool t3 = animState?.TargetOpen ?? false;
							int t4 = ((capturedItems != null) ? capturedItems.childCount : (-1));
							float num8 = ((capturedItems != null) ? capturedItems.rect.height : (-1f));
							float t5 = num8;
							float t6 = ((component5 != null) ? component5.alpha : (-1f));
							float t7 = -1f;
							float t8 = -1f;
							float t9 = -1f;
							if (capturedItems != null && capturedItems.childCount > 0)
							{
								Transform child = capturedItems.GetChild(0);
								if (child != null)
								{
									t7 = child.localScale.x;
									t8 = child.localScale.y;
									CanvasGroup component6 = child.GetComponent<CanvasGroup>();
									if (component6 != null)
									{
										t9 = component6.alpha;
									}
								}
							}
							ManualLogSource log3 = __locals1.log;
							if (log3 != null)
							{
								bool isEnabled3 = false;
								BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(116, 16, out isEnabled3);
								if (isEnabled3)
								{
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] WATCH-DIAG f=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(Time.frameCount);
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" tgt=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t3);
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" prog=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t2, "F3");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" scale=(");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(localScale.x, "F2");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(localScale.y, "F2");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(localScale.z, "F2");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(") sizeDelta=(");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(sizeDelta.x, "F1");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(sizeDelta.y, "F1");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(") rect=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(rect.width, "F1");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral("x");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(rect.height, "F1");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" cgAlpha=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t6, "F2");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" items=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t4);
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" itemsH=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t5, "F1");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" i0scale=(");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t7, "F2");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(",");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t8, "F2");
									bepInExInfoLogInterpolatedStringHandler.AppendLiteral(") i0cgA=");
									bepInExInfoLogInterpolatedStringHandler.AppendFormatted(t9, "F2");
								}
								log3.LogInfo(bepInExInfoLogInterpolatedStringHandler);
							}
						}
					}
					catch (System.Exception ex14)
					{
						__locals1.log?.LogWarning("[SystemDropdown] WATCH-DIAG: " + ex14.Message);
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
							RepaintOptionSwatches(capturedItems, capturedPrefabName, capturedColors, __locals1.log);
						}
						catch (System.Exception ex15)
						{
							ManualLogSource log4 = __locals1.log;
							if (log4 != null)
							{
								BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler8 = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled2);
								if (isEnabled2)
								{
									bepInExWarningLogInterpolatedStringHandler8.AppendLiteral("[SystemDropdown] watcher repaint: ");
									bepInExWarningLogInterpolatedStringHandler8.AppendFormatted(ex15.Message);
								}
								log4.LogWarning(bepInExWarningLogInterpolatedStringHandler8);
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
							bool flag2 = capturedRowRect != null && RectTransformUtility.RectangleContainsScreenPoint(capturedRowRect, screenPoint, null);
							if (!num9 && !flag2)
							{
								value3.TargetOpen = false;
								ownState[0] = false;
								HideDropdownTooltip(capturedOpener);
								__locals1.log?.LogInfo("[SystemDropdown] outside click -> animate close (modal stays on until anim finishes)");
							}
						}
					}
					catch (System.Exception ex16)
					{
						ManualLogSource log5 = __locals1.log;
						if (log5 != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler9 = new BepInExWarningLogInterpolatedStringHandler(32, 1, out isEnabled2);
							if (isEnabled2)
							{
								bepInExWarningLogInterpolatedStringHandler9.AppendLiteral("[SystemDropdown] outside click: ");
								bepInExWarningLogInterpolatedStringHandler9.AppendFormatted(ex16.Message);
							}
							log5.LogWarning(bepInExWarningLogInterpolatedStringHandler9);
						}
					}
				});
			}
			catch (System.Exception ex11)
			{
				log = __locals1.log;
				if (log != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler6 = new BepInExWarningLogInterpolatedStringHandler(32, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler6.AppendLiteral("[SystemDropdown] opener wiring: ");
						bepInExWarningLogInterpolatedStringHandler6.AppendFormatted(ex11.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler6);
				}
			}
		}
		try
		{
			if (__locals1.dropdownPanel != null)
			{
				try
				{
					ApplyAnimProgress(__locals1.dropdownPanel, FindArrowChild(gameObject3.transform, __locals1.log), 0f);
				}
				catch
				{
				}
				__locals1.dropdownPanel.gameObject.SetActive(value: false);
			}
			if (__locals1.opener != null)
			{
				try
				{
					__locals1.opener.Value = false;
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
			Options = __locals1.options,
			Colors = __locals1.colors,
			Tud = __locals1.tud,
			Opener = __locals1.opener,
			Panel = __locals1.dropdownPanel,
			RowRect = gameObject3.GetComponent<RectTransform>(),
			RowSwatch = image,
			CurrentText = __locals1.currentText,
			CurrentIndex = 0
		};
		_handles.Add(dropdownHandle);
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
			try
			{
				TweakUIDropdown tweakUIDropdown = _cachedRowTemplate.GetComponent<TweakUIDropdown>() ?? _cachedRowTemplate.GetComponentInChildren<TweakUIDropdown>(includeInactive: true);
				if (tweakUIDropdown != null && tweakUIDropdown.DropdownItemPrefab != null && tweakUIDropdown.DropdownContainer != null && tweakUIDropdown.Dropdown != null && tweakUIDropdown.Opener != null && tweakUIDropdown.Current != null)
				{
					return _cachedRowTemplate;
				}
			}
			catch
			{
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
		foreach (UnityEngine.Object item in (Il2CppArrayBase<UnityEngine.Object>)il2CppReferenceArray)
		{
			if (item == null)
			{
				continue;
			}
			TweakUIDropdown tweakUIDropdown2 = item.TryCast<TweakUIDropdown>();
			if (!(tweakUIDropdown2 == null) && !(tweakUIDropdown2.gameObject == null))
			{
				GameObject gameObject4 = tweakUIDropdown2.gameObject;
				int num = ((gameObject4.hideFlags == HideFlags.None && gameObject4.scene.name != null) ? 1 : 0);
				bool flag = tweakUIDropdown2.DropdownItemPrefab != null && tweakUIDropdown2.DropdownContainer != null && tweakUIDropdown2.Dropdown != null && tweakUIDropdown2.Opener != null && tweakUIDropdown2.Current != null;
				bool flag2 = IsSafeTemplateName(gameObject4);
				if (gameObject3 == null)
				{
					gameObject3 = gameObject4;
				}
				if (num != 0 && gameObject == null)
				{
					gameObject = gameObject4;
				}
				if (((uint)num & ((flag2 ? true : false) ? 1u : 0u)) != 0 && gameObject2 == null)
				{
					gameObject2 = gameObject4;
				}
				if (((uint)num & ((flag2 ? true : false) ? 1u : 0u) & ((flag ? true : false) ? 1u : 0u)) != 0)
				{
					_cachedRowTemplate = gameObject4;
					log?.LogInfo("[SystemDropdown] template selected (scene+safe+fully-wired): '" + gameObject4.name + "'");
					return gameObject4;
				}
			}
		}
		if (gameObject2 != null)
		{
			_cachedRowTemplate = gameObject2;
			log?.LogInfo("[SystemDropdown] template selected (scene+safe): '" + gameObject2.name + "'");
			return gameObject2;
		}
		if (gameObject != null)
		{
			_cachedRowTemplate = gameObject;
			log?.LogWarning("[SystemDropdown] no safe scene-based TweakUIDropdown found, falling back to scene-any: '" + gameObject.name + "'");
			return gameObject;
		}
		if (gameObject3 != null)
		{
			_cachedRowTemplate = gameObject3;
			log?.LogWarning("[SystemDropdown] no scene-based TweakUIDropdown found, falling back to first candidate: '" + gameObject3.name + "'");
			return gameObject3;
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
			if (child != null && child.name == LocStrings.Name_Crosshair_Dismisser)
			{
				_customDismisserByOpener[opener] = child.gameObject;
				return;
			}
		}
		try
		{
			GameObject gameObject = new GameObject(LocStrings.Name_Crosshair_Dismisser);
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
					CBSafeCrosshairPlugin.Instance?.Log.LogInfo("[SystemDropdown] custom Dismisser: closing dropdown");
				}
				catch (System.Exception ex2)
				{
					CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
					if (instance != null)
					{
						ManualLogSource log2 = instance.Log;
						bool isEnabled3 = false;
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled3);
						if (isEnabled3)
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
			if (log != null)
			{
				bool isEnabled = false;
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
				bool isEnabled2 = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(40, 1, out isEnabled2);
				if (isEnabled2)
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
		try
		{
			try
			{
				Transform transform = root.Find("Container/Right/Icon");
				if (transform != null)
				{
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
				if (name != null)
				{
					string text = name.ToLowerInvariant();
					if (text.Contains("arrow") || text.Contains("chevron") || text.Contains("caret"))
					{
						return item;
					}
					if (transform2 == null && text == "icon")
					{
						transform2 = item;
					}
				}
			}
			if (transform2 != null)
			{
				return transform2;
			}
			log?.LogWarning("[SystemDropdown] FindArrowChild: no arrow-named or Icon child found, arrow rotation disabled");
		}
		catch (System.Exception ex)
		{
			log?.LogWarning("[SystemDropdown] FindArrowChild: " + ex.Message);
		}
		return null;
	}

	private static void DisableComponentByName(GameObject go, string typeNameSubstring, ManualLogSource log)
	{
		if (go == null || string.IsNullOrEmpty(typeNameSubstring))
		{
			return;
		}
		try
		{
			Il2CppArrayBase<Component> components = go.GetComponents<Component>();
			if (components == null)
			{
				return;
			}
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
						continue;
					}
				}
				catch
				{
				}
				try
				{
					UnityEngine.Object.Destroy(component);
				}
				catch (System.Exception ex)
				{
					log?.LogWarning("[SystemDropdown] DisableComponentByName: could not disable/destroy '" + text + "': " + ex.Message);
				}
			}
		}
		catch (System.Exception ex2)
		{
			log?.LogWarning("[SystemDropdown] DisableComponentByName: " + ex2.Message);
		}
	}

	private static int DisableComponentByNameRecursiveExcept(GameObject root, string typeNameSubstring, Transform skipSubtree, ManualLogSource log)
	{
		if (root == null || string.IsNullOrEmpty(typeNameSubstring))
		{
			return 0;
		}
		int num = 0;
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
				GameObject gameObject = transform.gameObject;
				if (gameObject == null)
				{
					continue;
				}
				Il2CppArrayBase<Component> components = gameObject.GetComponents<Component>();
				if (components == null)
				{
					continue;
				}
				for (int j = 0; j < components.Length; j++)
				{
					Component component = components[j];
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
						log?.LogWarning("[SystemDropdown] disable/destroy '" + text + "' on '" + gameObject.name + "': " + ex.Message);
					}
				}
			}
			return num;
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				log.LogWarning("[SystemDropdown] DisableComponentByNameRecursiveExcept('" + typeNameSubstring + "'): " + ex2.Message);
				return num;
			}
			return num;
		}
	}

	private static int KillLeanTweensExcept(GameObject root, Transform skipSubtree, ManualLogSource log)
	{
		if (root == null)
		{
			return 0;
		}
		int num = 0;
		try
		{
			Il2CppArrayBase<Transform> componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Transform transform = componentsInChildren[i];
				if (!(transform == null) && (!(skipSubtree != null) || (!(transform == skipSubtree) && !transform.IsChildOf(skipSubtree))))
				{
					try
					{
						LeanTween.cancel(transform.gameObject);
						num++;
					}
					catch (System.Exception ex)
					{
						log?.LogWarning("[SystemDropdown] LeanTween.cancel on '" + transform.name + "': " + ex.Message);
					}
				}
			}
			return num;
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				log.LogWarning("[SystemDropdown] KillLeanTweensExcept: " + ex2.Message);
				return num;
			}
			return num;
		}
	}

	private static int DisableComponentByNameRecursive(GameObject root, string typeNameSubstring, ManualLogSource log)
	{
		if (root == null || string.IsNullOrEmpty(typeNameSubstring))
		{
			return 0;
		}
		int num = 0;
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
				Il2CppArrayBase<Component> components = gameObject.GetComponents<Component>();
				if (components == null)
				{
					continue;
				}
				for (int j = 0; j < components.Length; j++)
				{
					Component component = components[j];
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
						log?.LogWarning("[SystemDropdown] disable/destroy '" + text + "' on '" + gameObject.name + "': " + ex.Message);
					}
				}
			}
			return num;
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				log.LogWarning("[SystemDropdown] DisableComponentByNameRecursive('" + typeNameSubstring + "'): " + ex2.Message);
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
		try
		{
			Il2CppArrayBase<Transform> componentsInChildren = root.GetComponentsInChildren<Transform>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Transform transform = componentsInChildren[i];
				if (!(transform == null))
				{
					try
					{
						LeanTween.cancel(transform.gameObject);
						num++;
					}
					catch (System.Exception ex)
					{
						log?.LogWarning("[SystemDropdown] LeanTween.cancel on '" + transform.name + "': " + ex.Message);
					}
				}
			}
			return num;
		}
		catch (System.Exception ex2)
		{
			if (log != null)
			{
				log.LogWarning("[SystemDropdown] KillLeanTweens: " + ex2.Message);
				return num;
			}
			return num;
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
				Il2CppArrayBase<ScrollRect> componentsInChildren = panel.GetComponentsInChildren<ScrollRect>(includeInactive: true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					ScrollRect scrollRect = componentsInChildren[i];
					if (!(scrollRect == null))
					{
						FlexElement component = scrollRect.GetComponent<FlexElement>();
						if (component != null)
						{
							float value = num5 * num;
							component.MaxHeight = new FlexLength
							{
								Value = value,
								Unit = FlexUnit.Pixels
							};
						}
					}
				}
			}
			catch
			{
			}
		}
		catch (System.Exception ex)
		{
			log?.LogWarning("[SystemDropdown] EnforcePanelLayout: " + ex.Message);
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

	private static void LogPanelDiagnostics(RectTransform panel, RustButton opener, ManualLogSource log, int frame)
	{
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
			if (log != null)
			{
				bool isEnabled = false;
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
				bool isEnabled2 = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(38, 1, out isEnabled2);
				if (isEnabled2)
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
		try
		{
			Il2CppArrayBase<Animator> componentsInChildren = root.GetComponentsInChildren<Animator>(includeInactive: true);
			int num = 0;
			bool isEnabled = false;
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
						if (log != null)
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
							log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
						}
					}
					catch (System.Exception ex)
					{
						if (log != null)
						{
							BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(42, 2, out isEnabled);
							if (isEnabled)
							{
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] clear events on clip '");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(animationClip.name);
								bepInExWarningLogInterpolatedStringHandler.AppendLiteral("': ");
								bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
							}
							log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
						}
					}
				}
			}
		}
		catch (System.Exception ex2)
		{
			log?.LogWarning("[SystemDropdown] ClearAnimationEvents: " + ex2.Message);
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
		foreach (Component item in list)
		{
			try
			{
				UnityEngine.Object.DestroyImmediate(item);
			}
			catch (System.Exception ex)
			{
				if (log != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(34, 2, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] destroy ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted((item != null) ? item.GetIl2CppType().Name : null);
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral(" failed: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
		}
		if (log != null)
		{
			BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(44, 1, out isEnabled);
			if (isEnabled)
			{
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] removed ");
				bepInExInfoLogInterpolatedStringHandler.AppendFormatted(list.Count);
				bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" tweak component(s)");
			}
			log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
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
			log?.LogWarning("[SystemDropdown] UpdateLabel: defuse locali failed: " + ex.Message);
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
			log?.LogWarning("[SystemDropdown] UpdateLabel: enforcer attach failed: " + ex2.Message);
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
		try
		{
			bool flag = !string.IsNullOrEmpty(labelText);
			foreach (Tooltip componentsInChild in row.GetComponentsInChildren<Tooltip>(includeInactive: true))
			{
				if (componentsInChild == null)
				{
					continue;
				}
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
					componentsInChild.enabled = flag;
				}
				catch
				{
				}
				if (!flag)
				{
					continue;
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
		}
		catch
		{
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
		try
		{
			ApplyCurrentText(currentText, optText);
			bool isEnabled = false;
			try
			{
				if (opener != null)
				{
					opener.SetToggleFalse();
				}
			}
			catch (System.Exception ex)
			{
				if (log != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] SetToggleFalse: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
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
			if (log != null)
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
				log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		catch (System.Exception t)
		{
			if (log != null)
			{
				bool isEnabled2 = false;
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(36, 1, out isEnabled2);
				if (isEnabled2)
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
		try
		{
			currentText.ForceMeshUpdate();
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
			GameObject gameObject = new GameObject("CBSafe_Crosshair_RowSwatch");
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

	private static void ForceEnableItemFlex(RectTransform itemsContainer, ManualLogSource log)
	{
		if (itemsContainer == null)
		{
			return;
		}
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
					}
				}
				catch
				{
				}
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
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] ForceEnableItemFlex: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
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
			log?.LogWarning("[SystemDropdown] ForceEnableItemInteractables: " + ex.Message);
		}
		if (num > 0 || num2 > 0)
		{
			log?.LogInfo("[SystemDropdown] ForceEnableItemInteractables: fixed " + num + " Image(s), enabled " + num2 + " RustButton(s)");
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
		try
		{
			bool flag = false;
			Image component = dropdownPanel.GetComponent<Image>();
			if (component != null)
			{
				bool num = ApplyOpaqueGreyTint(component);
				flag = true;
				if (num)
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
				if (num2)
				{
					log?.LogInfo("[SystemDropdown] EnsurePanelBackground: tinted child '" + text + "'");
				}
			}
			if (flag)
			{
				return;
			}
			for (int j = 0; j < childCount; j++)
			{
				Transform child2 = dropdownPanel.GetChild(j);
				if (!(child2 != null) || !(child2.name == LocStrings.Name_Crosshair_PanelBackground))
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
				GameObject gameObject = new GameObject(LocStrings.Name_Crosshair_PanelBackground);
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
				log?.LogInfo("[SystemDropdown] EnsurePanelBackground: created " + LocStrings.Name_Crosshair_PanelBackground);
			}
			catch (System.Exception ex)
			{
				log?.LogWarning("[SystemDropdown] EnsurePanelBackground create: " + ex.Message);
			}
		}
		catch (System.Exception ex2)
		{
			log?.LogWarning("[SystemDropdown] EnsurePanelBackground: " + ex2.Message);
		}
	}

	private static void DumpDropdownPanelOnce(RectTransform dropdownPanel, ManualLogSource log)
	{
		if (dropdownPanel == null || log == null || _panelDumpDone.Contains(dropdownPanel))
		{
			return;
		}
		_panelDumpDone.Add(dropdownPanel);
		try
		{
			log.LogInfo("[SystemDropdown] PANELDUMP === BEGIN panel='" + dropdownPanel.gameObject.name + "' ===");
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
			log.LogWarning("[SystemDropdown] DumpDropdownPanelOnce: " + ex.Message);
		}
	}

	private static void DumpRectOneLine(Transform t, ManualLogSource log, int depth)
	{
		if (t == null)
		{
			return;
		}
		try
		{
			RectTransform component = t.GetComponent<RectTransform>();
			Image component2 = t.GetComponent<Image>();
			string text = "img=-";
			if (component2 != null)
			{
				string text2 = "<null>";
				try
				{
					text2 = ((component2.sprite != null) ? component2.sprite.name : "<null>");
				}
				catch
				{
				}
				text = "img(a=" + component2.color.a.ToString("F2") + "/en=" + component2.enabled + "/rt=" + component2.raycastTarget + "/sp=" + text2 + ")";
			}
			string text3 = "";
			ScrollRect component3 = t.GetComponent<ScrollRect>();
			if (component3 != null)
			{
				text3 = " ScrollRect(en=" + component3.enabled + ",content=" + ((component3.content != null) ? component3.content.name : "<null>") + ",viewport=" + ((component3.viewport != null) ? component3.viewport.name : "<null>") + ")";
			}
			string text4 = "";
			Scrollbar component4 = t.GetComponent<Scrollbar>();
			if (component4 != null)
			{
				text4 = " Scrollbar(en=" + component4.enabled + ",val=" + component4.value.ToString("F2") + ")";
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
					string text5 = null;
					try
					{
						Il2CppSystem.Type il2CppType = component5.GetIl2CppType();
						text5 = ((il2CppType != null) ? il2CppType.Name : null);
					}
					catch
					{
					}
					if (string.IsNullOrEmpty(text5))
					{
						try
						{
							text5 = component5.GetType().Name;
						}
						catch
						{
						}
					}
					if (!string.IsNullOrEmpty(text5))
					{
						list.Add(text5);
					}
				}
			}
			string text6 = new string(' ', depth * 2);
			float num = ((component != null) ? component.sizeDelta.x : 0f);
			float num2 = ((component != null) ? component.sizeDelta.y : 0f);
			log.LogInfo("[SystemDropdown] PANELDUMP " + text6 + "'" + t.name + "' act=" + t.gameObject.activeSelf + "/" + t.gameObject.activeInHierarchy + " sd=" + num.ToString("F0") + "," + num2.ToString("F0") + " " + text + text3 + text4 + " comps=[" + string.Join(",", list) + "]");
		}
		catch (System.Exception ex)
		{
			log.LogWarning("[SystemDropdown] DumpRectOneLine: " + ex.Message);
		}
	}

	private static void EnsureScrollWired(RectTransform dropdownPanel, RectTransform itemsContainer, ManualLogSource log)
	{
		if (dropdownPanel == null)
		{
			return;
		}
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
					if (scrollRect.viewport == null && scrollRect.content != null)
					{
						Transform parent = scrollRect.content.parent;
						RectTransform rectTransform = ((parent != null) ? parent.GetComponent<RectTransform>() : null);
						if (rectTransform != null)
						{
							scrollRect.viewport = rectTransform;
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
			if (num + num2 + num3 > 0)
			{
				log?.LogInfo("[SystemDropdown] EnsureScrollWired: enabled " + num + " ScrollRect(s), wired " + num2 + " content(s), enabled " + num3 + " Scrollbar(s)");
			}
		}
		catch (System.Exception ex)
		{
			log?.LogWarning("[SystemDropdown] EnsureScrollWired: " + ex.Message);
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
		if ((flag || flag2 || flag3 || flag4) && log != null)
		{
			string text = "?";
			try
			{
				Rect rect = viewport.rect;
				Vector2 anchorMin = viewport.anchorMin;
				Vector2 anchorMax = viewport.anchorMax;
				Vector2 pivot = viewport.pivot;
				Vector2 sizeDelta = viewport.sizeDelta;
				text = "rect=(" + rect.x.ToString("F0") + "," + rect.y.ToString("F0") + ")/" + rect.width.ToString("F0") + "x" + rect.height.ToString("F0") + " aMin=(" + anchorMin.x.ToString("F2") + "," + anchorMin.y.ToString("F2") + ") aMax=(" + anchorMax.x.ToString("F2") + "," + anchorMax.y.ToString("F2") + ") piv=(" + pivot.x.ToString("F2") + "," + pivot.y.ToString("F2") + ") sd=(" + sizeDelta.x.ToString("F0") + "," + sizeDelta.y.ToString("F0") + ")";
			}
			catch
			{
			}
			log.LogInfo("[SystemDropdown] EnsureViewportClipping '" + gameObject.name + "': RectMask2D added=" + flag + " enabled=" + flag2 + ", legacyMask disabled=" + flag3 + ", image fixed=" + flag4 + " | " + text);
		}
	}

	private static void RepaintOptionSwatches(RectTransform itemsContainer, string prefabName, Color[] colors, ManualLogSource log)
	{
		if (itemsContainer == null || colors == null || colors.Length == 0)
		{
			return;
		}
		try
		{
			ForceEnableItemFlex(itemsContainer, log);
		}
		catch
		{
		}
		try
		{
			ForceEnableItemInteractables(itemsContainer, log);
		}
		catch
		{
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
			bool isEnabled = false;
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
					if (child != null && child.name == LocStrings.Name_Crosshair_OptionSwatch)
					{
						image = child.GetComponent<Image>();
						break;
					}
				}
				if (image == null)
				{
					GameObject gameObject = new GameObject(LocStrings.Name_Crosshair_OptionSwatch);
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
				if (DebugOn && log != null)
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
		catch (System.Exception ex)
		{
			if (log != null)
			{
				bool isEnabled2 = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(40, 1, out isEnabled2);
				if (isEnabled2)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] RepaintOptionSwatches: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
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
		try
		{
			Il2CppArrayBase<RustButton> componentsInChildren = itemsContainer.GetComponentsInChildren<RustButton>(includeInactive: true);
			bool isEnabled = false;
			if (componentsInChildren == null || componentsInChildren.Length == 0)
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
			if (list.Count == 0)
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
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					RustButton rustButton = componentsInChildren[i];
					if (rustButton == null || rustButton.gameObject == null)
					{
						continue;
					}
					string t = BuildPath(rustButton.transform, itemsContainer);
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
							if (transform.GetChild(j) != null && transform.GetChild(j).name == LocStrings.Name_Crosshair_OptionSwatch)
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
							GameObject gameObject = new GameObject(LocStrings.Name_Crosshair_OptionSwatch);
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
				bool isEnabled2 = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled2);
				if (isEnabled2)
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
			log.LogInfo("[SystemDropdown] ---- row hierarchy dump (begin) ----");
			DumpTransform(root.transform, 0, log);
			log.LogInfo("[SystemDropdown] ---- row hierarchy dump (end) ----");
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
					RequestAnimatedClose();
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
				RequestAnimatedClose();
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
			if (log != null)
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
