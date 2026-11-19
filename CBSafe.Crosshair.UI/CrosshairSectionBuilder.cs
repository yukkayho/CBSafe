using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.UI;
using CBSafe.Core;
using CBSafe.Crosshair.Infrastructure;
using Rust.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Crosshair.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
[HydraNoObfuscate]
internal static class CrosshairSectionBuilder
{
	private sealed class CC__DisplayClass1_0
	{
		public SectionHandles handles;

		public ManualLogSource log;
	}

	public sealed class SectionHandles
	{
		public GameObject Root;

		public RectTransform PreviewHolder;

		public PreviewRenderer Preview;

		public SystemControlFactory.ToggleHandle EnabledToggle;

		public SystemControlFactory.ToggleHandle DynVisibilityToggle;

		public SystemControlFactory.ToggleHandle DynGapToggle;

		public SystemControlFactory.ToggleHandle OutlineToggle;

		public DropdownHandle StyleDropdown;

		public SystemControlFactory.SliderHandle DotSizeSlider;

		public SystemControlFactory.SliderHandle GapSlider;

		public SystemControlFactory.SliderHandle LengthSlider;

		public SystemControlFactory.SliderHandle ThicknessSlider;
	}

	private static TMP_Text _incrSliderFont;

	public static SectionHandles Build(Transform parent, TMP_Text fontSource, TMP_Text headerFontSource = null)
	{
		CC__DisplayClass1_0 __locals1 = new CC__DisplayClass1_0();
		__locals1.log = CBSafeCrosshairPlugin.Instance?.Log;
		CrosshairConfig current = CrosshairConfigStore.Current;
		GameObject gameObject = new GameObject("CrosshairSection");
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0f, 0.5f);
		rectTransform.anchorMax = new Vector2(1f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		VerticalLayoutGroup verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
		verticalLayoutGroup.padding = new RectOffset
		{
			left = 0,
			right = 0,
			top = 6,
			bottom = 12
		};
		verticalLayoutGroup.spacing = 6f;
		verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
		verticalLayoutGroup.childControlWidth = true;
		verticalLayoutGroup.childControlHeight = true;
		verticalLayoutGroup.childForceExpandWidth = true;
		verticalLayoutGroup.childForceExpandHeight = false;
		ContentSizeFitter contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
		contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		__locals1.handles = new SectionHandles
		{
			Root = gameObject
		};
		bool isEnabled = false;
		try
		{
			SystemControlFactory.CreateSectionHeader(gameObject.transform, LocStrings.Label_Cross_Enabled, headerFontSource ?? fontSource);
		}
		catch (Exception t)
		{
			ManualLogSource log = __locals1.log;
			if (log != null)
			{
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(18, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[Section] header: ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				log.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
		}
		try
		{
			RectTransform item = SystemControlFactory.CreatePreviewBlock(gameObject.transform, 170f).Item2;
			__locals1.handles.PreviewHolder = item;
			__locals1.handles.Preview = item.gameObject.AddComponent<PreviewRenderer>();
			__locals1.handles.Preview.Bind(item);
		}
		catch (Exception t2)
		{
			ManualLogSource log2 = __locals1.log;
			if (log2 != null)
			{
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler2 = new BepInExErrorLogInterpolatedStringHandler(25, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler2.AppendLiteral("[Section] preview build: ");
					bepInExErrorLogInterpolatedStringHandler2.AppendFormatted(t2);
				}
				log2.LogError(bepInExErrorLogInterpolatedStringHandler2);
			}
		}
		try
		{
			BuildResetButton(__locals1.handles, fontSource, __locals1.log);
		}
		catch (Exception t3)
		{
			ManualLogSource log3 = __locals1.log;
			if (log3 != null)
			{
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler3 = new BepInExErrorLogInterpolatedStringHandler(24, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler3.AppendLiteral("[Section] reset button: ");
					bepInExErrorLogInterpolatedStringHandler3.AppendFormatted(t3);
				}
				log3.LogError(bepInExErrorLogInterpolatedStringHandler3);
			}
		}
		try
		{
			__locals1.handles.EnabledToggle = SystemControlFactory.CreateToggle(gameObject.transform, LocStrings.Label_Cross_Enabled, "Включает или выключает отображение прицела на экране", current.Enabled, delegate(bool v)
			{
				CrosshairConfigStore.TryApplyUserEdit(delegate
				{
					CrosshairConfigStore.Current.Enabled = v;
				});
				PreviewRenderer preview5 = __locals1.handles.Preview;
				if (!(preview5 == null))
				{
					preview5.MarkDirty();
				}
			}, fontSource);
			__locals1.handles.DynVisibilityToggle = SystemControlFactory.CreateToggle(gameObject.transform, LocStrings.Label_DynVisibility, "Прицел плавно исчезает в прицеливании и появляется в обычном режиме стрельбы", current.DynamicVisibility, delegate(bool v)
			{
				CrosshairConfigStore.TryApplyUserEdit(delegate
				{
					CrosshairConfigStore.Current.DynamicVisibility = v;
				});
			}, fontSource);
			__locals1.handles.DynGapToggle = SystemControlFactory.CreateToggle(gameObject.transform, LocStrings.Label_DynGap, "Расстояние между линиями прицела растёт при стрельбе и движении", current.DynamicGap, delegate(bool v)
			{
				CrosshairConfigStore.TryApplyUserEdit(delegate
				{
					CrosshairConfigStore.Current.DynamicGap = v;
				});
			}, fontSource);
			__locals1.handles.StyleDropdown = SystemDropdownFactory.Create(gameObject.transform, LocStrings.Title_Style, CrosshairConfig.StyleLabels, null, delegate(int idx, string txt)
			{
				CrosshairConfigStore.TryApplyUserEdit(delegate
				{
					CrosshairConfigStore.Current.StyleIndex = idx;
				});
				PreviewRenderer preview4 = __locals1.handles.Preview;
				if (!(preview4 == null))
				{
					preview4.MarkDirty();
				}
			}, null, LocStrings.Title_Style);
			ColorPickerButtonFactory.Create(gameObject.transform, LocStrings.Title_MainColor, CrosshairConfigStore.MainColorEntry, null, delegate
			{
				CrosshairConfigStore.Load();
				PreviewRenderer preview3 = __locals1.handles.Preview;
				if (preview3 != null)
				{
					preview3.MarkDirty();
				}
			});
			TMP_Text tMP_Text = null;
			try
			{
				tMP_Text = __locals1.handles.EnabledToggle?.Text;
			}
			catch
			{
			}
			if (tMP_Text == null)
			{
				try
				{
					tMP_Text = __locals1.handles.DynVisibilityToggle?.Text;
				}
				catch
				{
				}
			}
			if (tMP_Text == null)
			{
				try
				{
					tMP_Text = __locals1.handles.DynGapToggle?.Text;
				}
				catch
				{
				}
			}
			if (tMP_Text == null)
			{
				tMP_Text = fontSource;
			}
			__locals1.handles.DotSizeSlider = SystemControlFactory.CreateSlider(gameObject.transform, LocStrings.Label_DotSize, 0f, 5f, current.DotSize, "0.00", delegate(float v)
			{
				CrosshairConfigStore.TryApplyUserEdit(delegate
				{
					CrosshairConfigStore.Current.DotSize = v;
				});
			}, tMP_Text);
			__locals1.handles.GapSlider = SystemControlFactory.CreateSlider(gameObject.transform, LocStrings.Label_Gap, 0f, 5f, current.Gap, "0.00", delegate(float v)
			{
				CrosshairConfigStore.TryApplyUserEdit(delegate
				{
					CrosshairConfigStore.Current.Gap = v;
				});
			}, tMP_Text);
			__locals1.handles.LengthSlider = SystemControlFactory.CreateSlider(gameObject.transform, LocStrings.Label_Length, 0f, 20f, current.Length, "0.0", delegate(float v)
			{
				CrosshairConfigStore.TryApplyUserEdit(delegate
				{
					CrosshairConfigStore.Current.Length = v;
				});
			}, tMP_Text);
			__locals1.handles.ThicknessSlider = SystemControlFactory.CreateSlider(gameObject.transform, LocStrings.Label_Thickness, 0f, 10f, current.Thickness, "0.0", delegate(float v)
			{
				CrosshairConfigStore.TryApplyUserEdit(delegate
				{
					CrosshairConfigStore.Current.Thickness = v;
				});
			}, tMP_Text);
			__locals1.handles.OutlineToggle = SystemControlFactory.CreateToggle(gameObject.transform, LocStrings.Label_Outline, LocStrings.Tip_Outline, current.OutlineEnabled, delegate(bool v)
			{
				CrosshairConfigStore.TryApplyUserEdit(delegate
				{
					CrosshairConfigStore.Current.OutlineEnabled = v;
				});
				PreviewRenderer preview2 = __locals1.handles.Preview;
				if (!(preview2 == null))
				{
					preview2.MarkDirty();
				}
			}, fontSource);
			ColorPickerButtonFactory.Create(gameObject.transform, LocStrings.Title_OutlineColor, CrosshairConfigStore.OutlineColorEntry, null, delegate
			{
				CrosshairConfigStore.Load();
				PreviewRenderer preview = __locals1.handles.Preview;
				if (preview != null)
				{
					preview.MarkDirty();
				}
			});
			ApplyDropdownDefaults(__locals1.handles, current);
		}
		catch (Exception t4)
		{
			ManualLogSource log4 = __locals1.log;
			if (log4 != null)
			{
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler4 = new BepInExErrorLogInterpolatedStringHandler(18, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler4.AppendLiteral("[Section] params: ");
					bepInExErrorLogInterpolatedStringHandler4.AppendFormatted(t4);
				}
				log4.LogError(bepInExErrorLogInterpolatedStringHandler4);
			}
		}
		EnsureResetButton(__locals1.handles, fontSource, __locals1.log);
		return __locals1.handles;
	}

	internal static bool IsContentComplete(SectionHandles handles)
	{
		if (handles == null)
		{
			return false;
		}
		if (handles.EnabledToggle != null && handles.PreviewHolder != null)
		{
			return HasResetButton(handles);
		}
		return false;
	}

	internal static bool HasResetButton(SectionHandles handles)
	{
		if (handles?.Root == null)
		{
			return false;
		}
		Transform transform = handles.Root.transform.Find("ResetButtonRow");
		if (transform != null)
		{
			return transform.Find("ResetButton") != null;
		}
		return false;
	}

	internal static void EnsureResetButton(SectionHandles handles, TMP_Text fontSource, ManualLogSource log)
	{
		if (handles?.Root == null || HasResetButton(handles))
		{
			return;
		}
		try
		{
			BuildResetButton(handles, fontSource, log);
		}
		catch (Exception ex)
		{
			log?.LogWarning("[Section] ensure reset: " + ex.Message);
		}
	}

	internal static bool BuildIncrementalStep(SectionHandles handles, ref int step, Transform parent, TMP_Text fontSource, TMP_Text headerFontSource)
	{
		if (handles == null || parent == null)
		{
			return false;
		}
		CrosshairConfig current = CrosshairConfigStore.Current;
		ManualLogSource manualLogSource = CBSafeCrosshairPlugin.Instance?.Log;
		switch (step)
		{
		case 0:
		{
			GameObject gameObject = new GameObject("CrosshairSection");
			gameObject.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0.5f);
			rectTransform.anchorMax = new Vector2(1f, 0.5f);
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			VerticalLayoutGroup verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
			verticalLayoutGroup.padding = new RectOffset
			{
				left = 0,
				right = 0,
				top = 6,
				bottom = 12
			};
			verticalLayoutGroup.spacing = 6f;
			verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
			verticalLayoutGroup.childControlWidth = true;
			verticalLayoutGroup.childControlHeight = true;
			verticalLayoutGroup.childForceExpandWidth = true;
			verticalLayoutGroup.childForceExpandHeight = false;
			ContentSizeFitter contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
			contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
			contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			handles.Root = gameObject;
			step = 1;
			return true;
		}
		case 1:
			try
			{
				SystemControlFactory.CreateSectionHeader(handles.Root.transform, LocStrings.Label_Cross_Enabled, headerFontSource ?? fontSource);
			}
			catch
			{
			}
			try
			{
				RectTransform item = SystemControlFactory.CreatePreviewBlock(handles.Root.transform, 170f).Item2;
				handles.PreviewHolder = item;
				handles.Preview = item.gameObject.AddComponent<PreviewRenderer>();
				handles.Preview.Bind(item);
			}
			catch (Exception ex5)
			{
				manualLogSource?.LogWarning("[Section] incremental preview: " + ex5.Message);
			}
			BuildResetButton(handles, fontSource, manualLogSource);
			step = 2;
			return true;
		case 2:
			step = 3;
			return true;
		case 3:
			try
			{
				handles.EnabledToggle = SystemControlFactory.CreateToggle(handles.Root.transform, LocStrings.Label_Cross_Enabled, "Включает или выключает отображение прицела на экране", current.Enabled, delegate(bool v)
				{
					CrosshairConfigStore.TryApplyUserEdit(delegate
					{
						CrosshairConfigStore.Current.Enabled = v;
					});
					handles.Preview?.MarkDirty();
				}, fontSource);
				handles.DynVisibilityToggle = SystemControlFactory.CreateToggle(handles.Root.transform, LocStrings.Label_DynVisibility, "Прицел плавно исчезает в прицеливании и появляется в обычном режиме стрельбы", current.DynamicVisibility, delegate(bool v)
				{
					CrosshairConfigStore.TryApplyUserEdit(delegate
					{
						CrosshairConfigStore.Current.DynamicVisibility = v;
					});
				}, fontSource);
				handles.DynGapToggle = SystemControlFactory.CreateToggle(handles.Root.transform, LocStrings.Label_DynGap, "Расстояние между линиями прицела растёт при стрельбе и движении", current.DynamicGap, delegate(bool v)
				{
					CrosshairConfigStore.TryApplyUserEdit(delegate
					{
						CrosshairConfigStore.Current.DynamicGap = v;
					});
				}, fontSource);
			}
			catch (Exception ex2)
			{
				manualLogSource?.LogWarning("[Section] incremental toggles: " + ex2.Message);
			}
			step = 4;
			return true;
		case 4:
			try
			{
				handles.StyleDropdown = SystemDropdownFactory.Create(handles.Root.transform, LocStrings.Title_Style, CrosshairConfig.StyleLabels, null, delegate(int idx, string txt)
				{
					CrosshairConfigStore.TryApplyUserEdit(delegate
					{
						CrosshairConfigStore.Current.StyleIndex = idx;
					});
					handles.Preview?.MarkDirty();
				}, null, LocStrings.Title_Style);
				ColorPickerButtonFactory.Create(handles.Root.transform, LocStrings.Title_MainColor, CrosshairConfigStore.MainColorEntry, null, delegate
				{
					CrosshairConfigStore.Load();
					handles.Preview?.MarkDirty();
				});
				_incrSliderFont = handles.EnabledToggle?.Text ?? handles.DynVisibilityToggle?.Text ?? handles.DynGapToggle?.Text ?? fontSource;
			}
			catch (Exception ex4)
			{
				manualLogSource?.LogWarning("[Section] incremental dropdowns: " + ex4.Message);
			}
			step = 5;
			return true;
		case 5:
		{
			TMP_Text fontSource3 = _incrSliderFont ?? fontSource;
			try
			{
				handles.DotSizeSlider = SystemControlFactory.CreateSlider(handles.Root.transform, LocStrings.Label_DotSize, 0f, 5f, current.DotSize, "0.00", delegate(float v)
				{
					CrosshairConfigStore.TryApplyUserEdit(delegate
					{
						CrosshairConfigStore.Current.DotSize = v;
					});
				}, fontSource3);
				handles.GapSlider = SystemControlFactory.CreateSlider(handles.Root.transform, LocStrings.Label_Gap, 0f, 5f, current.Gap, "0.00", delegate(float v)
				{
					CrosshairConfigStore.TryApplyUserEdit(delegate
					{
						CrosshairConfigStore.Current.Gap = v;
					});
				}, fontSource3);
			}
			catch (Exception ex6)
			{
				manualLogSource?.LogWarning("[Section] incremental sliders a: " + ex6.Message);
			}
			step = 6;
			return true;
		}
		case 6:
		{
			TMP_Text fontSource2 = _incrSliderFont ?? fontSource;
			try
			{
				handles.LengthSlider = SystemControlFactory.CreateSlider(handles.Root.transform, LocStrings.Label_Length, 0f, 20f, current.Length, "0.0", delegate(float v)
				{
					CrosshairConfigStore.TryApplyUserEdit(delegate
					{
						CrosshairConfigStore.Current.Length = v;
					});
				}, fontSource2);
				handles.ThicknessSlider = SystemControlFactory.CreateSlider(handles.Root.transform, LocStrings.Label_Thickness, 0f, 10f, current.Thickness, "0.0", delegate(float v)
				{
					CrosshairConfigStore.TryApplyUserEdit(delegate
					{
						CrosshairConfigStore.Current.Thickness = v;
					});
				}, fontSource2);
			}
			catch (Exception ex3)
			{
				manualLogSource?.LogWarning("[Section] incremental sliders b: " + ex3.Message);
			}
			step = 7;
			return true;
		}
		case 7:
			try
			{
				handles.OutlineToggle = SystemControlFactory.CreateToggle(handles.Root.transform, LocStrings.Label_Outline, LocStrings.Tip_Outline, current.OutlineEnabled, delegate(bool v)
				{
					CrosshairConfigStore.TryApplyUserEdit(delegate
					{
						CrosshairConfigStore.Current.OutlineEnabled = v;
					});
					handles.Preview?.MarkDirty();
				}, fontSource);
				ColorPickerButtonFactory.Create(handles.Root.transform, LocStrings.Title_OutlineColor, CrosshairConfigStore.OutlineColorEntry, null, delegate
				{
					CrosshairConfigStore.Load();
					handles.Preview?.MarkDirty();
				});
				ApplyDropdownDefaults(handles, current);
			}
			catch (Exception ex)
			{
				manualLogSource?.LogWarning("[Section] incremental outline: " + ex.Message);
			}
			EnsureResetButton(handles, fontSource, manualLogSource);
			step = 8;
			return false;
		default:
			return false;
		}
	}

	private static void BuildResetButton(SectionHandles handles, TMP_Text fontSource, ManualLogSource log)
	{
		if (handles?.Root == null)
		{
			return;
		}
		Transform transform = handles.Root.transform.Find("ResetButtonRow");
		if (transform != null)
		{
			UnityEngine.Object.Destroy(transform.gameObject);
		}
		GameObject gameObject = new GameObject("ResetButtonRow");
		gameObject.transform.SetParent(handles.Root.transform, worldPositionStays: false);
		gameObject.AddComponent<RectTransform>();
		LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
		layoutElement.preferredHeight = 24f;
		layoutElement.minHeight = 24f;
		GameObject gameObject2 = new GameObject("ResetButton");
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform rectTransform = gameObject2.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0f, 0f);
		rectTransform.anchorMax = new Vector2(1f, 1f);
		rectTransform.offsetMin = new Vector2(0f, 0f);
		rectTransform.offsetMax = new Vector2(0f, -4f);
		Image image = gameObject2.AddComponent<Image>();
		image.color = new Color(0.78f, 0.2f, 0.2f, 1f);
		image.raycastTarget = true;
		Button button = gameObject2.AddComponent<Button>();
		ColorBlock colors = button.colors;
		colors.normalColor = new Color(1f, 1f, 1f, 1f);
		colors.highlightedColor = new Color(1.1f, 1.05f, 1.05f, 1f);
		colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
		colors.selectedColor = new Color(1f, 1f, 1f, 1f);
		colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
		button.colors = colors;
		try
		{
			GameObject gameObject3 = new GameObject("ButtonText");
			gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
			RectTransform rectTransform2 = gameObject3.AddComponent<RectTransform>();
			rectTransform2.anchorMin = Vector2.zero;
			rectTransform2.anchorMax = Vector2.one;
			rectTransform2.offsetMin = Vector2.zero;
			rectTransform2.offsetMax = Vector2.zero;
			TextMeshProUGUI textMeshProUGUI = gameObject3.AddComponent<TextMeshProUGUI>();
			if (fontSource != null && fontSource.font != null)
			{
				textMeshProUGUI.font = fontSource.font;
			}
			if (fontSource != null && fontSource.fontSharedMaterial != null)
			{
				textMeshProUGUI.fontSharedMaterial = fontSource.fontSharedMaterial;
			}
			textMeshProUGUI.text = LocStrings.Label_Reset;
			textMeshProUGUI.alignment = TextAlignmentOptions.Center;
			textMeshProUGUI.color = new Color(1f, 0.92f, 0.9f, 1f);
			textMeshProUGUI.fontStyle = FontStyles.Bold;
			textMeshProUGUI.fontSize = 12f;
			textMeshProUGUI.enableAutoSizing = false;
			textMeshProUGUI.ForceMeshUpdate();
		}
		catch
		{
		}
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener((Action)delegate
		{
			try
			{
				CrosshairConfigStore.ResetToDefaults();
				SyncControlsFromConfig(handles);
				CrosshairConfigStore.Save();
				handles.Preview?.MarkDirty();
			}
			catch (Exception ex)
			{
				log?.LogWarning("[Reset] " + ex.Message);
			}
		});
		if (handles.PreviewHolder != null && handles.EnabledToggle != null)
		{
			gameObject.transform.SetSiblingIndex(handles.PreviewHolder.GetSiblingIndex() + 1);
		}
	}

	public static void SyncControlsFromConfig(SectionHandles h)
	{
		if (h == null)
		{
			return;
		}
		CrosshairConfig current = CrosshairConfigStore.Current;
		try
		{
			if (h.EnabledToggle != null)
			{
				SystemControlFactory.SetToggleSilently(h.EnabledToggle, current.Enabled);
			}
			if (h.DynVisibilityToggle != null)
			{
				SystemControlFactory.SetToggleSilently(h.DynVisibilityToggle, current.DynamicVisibility);
			}
			if (h.DynGapToggle != null)
			{
				SystemControlFactory.SetToggleSilently(h.DynGapToggle, current.DynamicGap);
			}
			if (h.OutlineToggle != null)
			{
				SystemControlFactory.SetToggleSilently(h.OutlineToggle, current.OutlineEnabled);
			}
			if (h.StyleDropdown != null)
			{
				h.StyleDropdown.SetIndexSilently(current.StyleIndex);
			}
			if (h.DotSizeSlider != null)
			{
				SystemControlFactory.SetSliderSilently(h.DotSizeSlider, current.DotSize);
			}
			if (h.GapSlider != null)
			{
				SystemControlFactory.SetSliderSilently(h.GapSlider, current.Gap);
			}
			if (h.LengthSlider != null)
			{
				SystemControlFactory.SetSliderSilently(h.LengthSlider, current.Length);
			}
			if (h.ThicknessSlider != null)
			{
				SystemControlFactory.SetSliderSilently(h.ThicknessSlider, current.Thickness);
			}
			PreviewRenderer preview = h.Preview;
			if (preview != null)
			{
				preview.MarkDirty();
			}
		}
		catch (Exception t)
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
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(34, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[Section] SyncControlsFromConfig: ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				log.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
		}
	}

	private static void ApplyDropdownDefaults(SectionHandles h, CrosshairConfig cfg)
	{
		try
		{
			h.StyleDropdown?.SetIndexSilently(cfg.StyleIndex);
		}
		catch (Exception ex)
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[Section] ApplyDropdownDefaults: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static TMP_Text AddCenteredLabel(Transform parent, string text, TMP_Text fontSource)
	{
		TMP_Text tMP_Text = null;
		if (fontSource != null)
		{
			try
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(fontSource.gameObject, parent, worldPositionStays: false);
				gameObject.name = "ButtonText";
				foreach (TweakUIBase componentsInChild in gameObject.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
				{
					if (componentsInChild != null)
					{
						UnityEngine.Object.DestroyImmediate(componentsInChild);
					}
				}
				RectTransform obj = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
				obj.anchorMin = Vector2.zero;
				obj.anchorMax = Vector2.one;
				obj.offsetMin = Vector2.zero;
				obj.offsetMax = Vector2.zero;
				obj.localScale = Vector3.one;
				obj.localRotation = Quaternion.identity;
				tMP_Text = gameObject.GetComponent<TMP_Text>();
			}
			catch
			{
				tMP_Text = null;
			}
		}
		if (tMP_Text == null)
		{
			GameObject gameObject2 = new GameObject("ButtonText");
			gameObject2.transform.SetParent(parent, worldPositionStays: false);
			RectTransform rectTransform = gameObject2.AddComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			tMP_Text = gameObject2.AddComponent<TextMeshProUGUI>();
			if (fontSource != null)
			{
				try
				{
					if (fontSource.font != null)
					{
						tMP_Text.font = fontSource.font;
					}
				}
				catch
				{
				}
				try
				{
					if (fontSource.fontSharedMaterial != null)
					{
						tMP_Text.fontSharedMaterial = fontSource.fontSharedMaterial;
					}
				}
				catch
				{
				}
			}
		}
		if (tMP_Text != null)
		{
			try
			{
				RustText rustText = tMP_Text.TryCast<RustText>();
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
					try
					{
						rustText.IsLocalized = false;
					}
					catch
					{
					}
					try
					{
						rustText.Phrase = new Translate.Phrase(string.Empty, text);
					}
					catch
					{
					}
					try
					{
						rustText.SetText(text);
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			tMP_Text.text = text;
			tMP_Text.alignment = TextAlignmentOptions.Center;
			tMP_Text.raycastTarget = false;
			try
			{
				tMP_Text.enableWordWrapping = false;
			}
			catch
			{
			}
			try
			{
				tMP_Text.overflowMode = TextOverflowModes.Overflow;
			}
			catch
			{
			}
			try
			{
				tMP_Text.margin = Vector4.zero;
			}
			catch
			{
			}
			try
			{
				tMP_Text.ForceMeshUpdate();
				return tMP_Text;
			}
			catch
			{
				return tMP_Text;
			}
		}
		return tMP_Text;
	}
}
