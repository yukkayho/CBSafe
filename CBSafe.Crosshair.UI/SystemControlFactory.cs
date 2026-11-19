using System;
using System.Globalization;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using Rust.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CBSafe.Crosshair.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HydraNoObfuscate]
internal static class SystemControlFactory
{
	public sealed class ToggleHandle
	{
		public RectTransform Row;

		public Button Button;

		public Image Background;

		public TMP_Text Text;

		public bool Value;

		public Action<bool> OnChanged;

		public SystemToggleFactory.Handle SystemToggle;
	}

	public sealed class SliderHandle
	{
		public RectTransform Row;

		public Slider Slider;

		public TMP_Text ValueText;

		public string Format;

		public Action<float> OnChanged;

		public SystemSliderFactory.Handle SystemSlider;
	}

	public const float RowHeight = 36f;

	public const float SliderRowHeight = 36f;

	private const float LabelWidthPct = 0.35f;

	private const float ControlPaddingX = 6f;

	private static readonly Color RowBackground = new Color(0f, 0f, 0f, 0f);

	public static readonly Color LabelColor = new Color(0.85f, 0.85f, 0.85f, 1f);

	private static readonly Color ButtonOff = new Color(0.2f, 0.2f, 0.2f, 0.95f);

	private static readonly Color ButtonOn = new Color(0.18f, 0.55f, 0.85f, 0.95f);

	private static readonly Color ToggleIndicatorOff = new Color(0.22f, 0.22f, 0.22f, 0.95f);

	private static readonly Color ToggleIndicatorOn = new Color(0.32f, 0.74f, 0.34f, 1f);

	private static readonly Color ToggleBorder = new Color(0f, 0f, 0f, 0.45f);

	private static readonly Color ToggleTextColor = new Color(0.92f, 0.92f, 0.92f, 1f);

	private static readonly Color SliderTrack = new Color(0.32f, 0.32f, 0.34f, 0.95f);

	private static readonly Color SliderTrackBorder = new Color(0f, 0f, 0f, 0.55f);

	private static readonly Color SliderFill = new Color(0.8f, 0.3f, 0.2f, 1f);

	private static readonly Color SliderHandleCol = new Color(0f, 0f, 0f, 0f);

	private static readonly Color PreviewBg = new Color(0.05f, 0.05f, 0.05f, 0.95f);

	private static readonly Color SectionHeader = new Color(0.95f, 0.95f, 0.95f, 1f);

	private static Sprite _verticalDarkGradient;

	private static Sprite _whitePixel;

	private static int _gradientHeight = -1;

	private static Sprite GetVerticalDarkGradientSprite()
	{
		if (_verticalDarkGradient != null && _gradientHeight == 64)
		{
			return _verticalDarkGradient;
		}
		try
		{
			Texture2D texture2D = new Texture2D(1, 64, TextureFormat.RGBA32, mipChain: false, linear: false);
			texture2D.wrapMode = TextureWrapMode.Clamp;
			texture2D.filterMode = FilterMode.Bilinear;
			texture2D.hideFlags = HideFlags.HideAndDontSave;
			for (int i = 0; i < 64; i++)
			{
				float num = (float)i / 63f;
				float a = Mathf.Pow(1f - num, 1.4f) * 0.95f;
				float num2 = Mathf.Lerp(0f, 0.1f, num);
				texture2D.SetPixel(0, i, new Color(num2, num2, num2, a));
			}
			texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: false);
			_verticalDarkGradient = Sprite.Create(texture2D, new Rect(0f, 0f, 1f, 64f), new Vector2(0.5f, 0.5f), 100f);
			_verticalDarkGradient.hideFlags = HideFlags.HideAndDontSave;
			_gradientHeight = 64;
		}
		catch (Exception t)
		{
			CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
			if (instance != null)
			{
				ManualLogSource log = instance.Log;
				if (log != null)
				{
					bool isEnabled = false;
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(33, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemControlFactory] gradient: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(t);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
			_verticalDarkGradient = null;
		}
		return _verticalDarkGradient;
	}

	private static Sprite GetWhiteSprite()
	{
		if (_whitePixel != null)
		{
			return _whitePixel;
		}
		try
		{
			Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false, linear: false);
			texture2D.hideFlags = HideFlags.HideAndDontSave;
			texture2D.SetPixel(0, 0, Color.white);
			texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: false);
			_whitePixel = Sprite.Create(texture2D, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
			_whitePixel.hideFlags = HideFlags.HideAndDontSave;
		}
		catch (Exception t)
		{
			CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
			if (instance != null)
			{
				ManualLogSource log = instance.Log;
				if (log != null)
				{
					bool isEnabled = false;
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(35, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemControlFactory] whitePixel: ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(t);
					}
					log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
		}
		return _whitePixel;
	}

	public static RectTransform MakeRow(Transform parent, string name)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0f, 0.5f);
		rectTransform.anchorMax = new Vector2(1f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = new Vector2(0f, 36f);
		LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
		layoutElement.preferredHeight = 36f;
		layoutElement.minHeight = 36f;
		layoutElement.flexibleHeight = 0f;
		Image image = gameObject.AddComponent<Image>();
		image.color = RowBackground;
		image.raycastTarget = false;
		return rectTransform;
	}

	public static TMP_Text AddLabel(RectTransform row, string text, TMP_Text fontSource)
	{
		GameObject gameObject = new GameObject("Label");
		gameObject.transform.SetParent(row, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0f, 0f);
		rectTransform.anchorMax = new Vector2(0.35f, 1f);
		rectTransform.offsetMin = new Vector2(6f, 0f);
		rectTransform.offsetMax = new Vector2(-6f, 0f);
		return BuildText(gameObject.transform, text, fontSource, 16f, TextAlignmentOptions.MidlineLeft, LabelColor, bold: false);
	}

	private static TMP_Text BuildText(Transform parent, string text, TMP_Text fontSource, float fontSize, TextAlignmentOptions align, Color color, bool bold, bool inheritStyle = false)
	{
		if (fontSource != null && fontSource.gameObject != null)
		{
			try
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(fontSource.gameObject, parent, worldPositionStays: false);
				gameObject.name = "Text";
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
				TMP_Text component = gameObject.GetComponent<TMP_Text>();
				if (component != null)
				{
					RustText rustText = component.TryCast<RustText>();
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
					try
					{
						component.text = text;
					}
					catch
					{
					}
					if (!inheritStyle)
					{
						try
						{
							component.color = color;
						}
						catch
						{
						}
						try
						{
							component.fontStyle = (bold ? FontStyles.Bold : FontStyles.Normal);
						}
						catch
						{
						}
					}
					try
					{
						component.alignment = align;
					}
					catch
					{
					}
					if (fontSize > 0f)
					{
						try
						{
							component.fontSize = fontSize;
						}
						catch
						{
						}
					}
					try
					{
						component.raycastTarget = false;
					}
					catch
					{
					}
					try
					{
						component.enableWordWrapping = false;
					}
					catch
					{
					}
					try
					{
						component.overflowMode = TextOverflowModes.Overflow;
					}
					catch
					{
					}
					try
					{
						component.margin = Vector4.zero;
					}
					catch
					{
					}
					try
					{
						component.ForceMeshUpdate();
					}
					catch
					{
					}
					return component;
				}
				UnityEngine.Object.Destroy(gameObject);
			}
			catch (Exception ex)
			{
				CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
				if (instance != null)
				{
					ManualLogSource log = instance.Log;
					if (log != null)
					{
						bool isEnabled = false;
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(50, 2, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[BuildText clone '");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(text);
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("'] ");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral(" -- falling back to plain TMP");
						}
						log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
					}
				}
			}
		}
		GameObject gameObject2 = new GameObject("Text");
		gameObject2.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rectTransform = gameObject2.AddComponent<RectTransform>();
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		TextMeshProUGUI textMeshProUGUI = gameObject2.AddComponent<TextMeshProUGUI>();
		if (fontSource != null)
		{
			try
			{
				if (fontSource.font != null)
				{
					textMeshProUGUI.font = fontSource.font;
				}
			}
			catch
			{
			}
			try
			{
				if (fontSource.fontSharedMaterial != null)
				{
					textMeshProUGUI.fontSharedMaterial = fontSource.fontSharedMaterial;
				}
			}
			catch
			{
			}
		}
		textMeshProUGUI.text = text;
		textMeshProUGUI.color = color;
		textMeshProUGUI.alignment = align;
		textMeshProUGUI.fontSize = fontSize;
		textMeshProUGUI.fontStyle = (bold ? FontStyles.Bold : FontStyles.Normal);
		textMeshProUGUI.raycastTarget = false;
		textMeshProUGUI.enableWordWrapping = false;
		textMeshProUGUI.overflowMode = TextOverflowModes.Overflow;
		return textMeshProUGUI;
	}

	public static TMP_Text CreateSectionHeader(Transform parent, string text, TMP_Text fontSource)
	{
		GameObject gameObject = new GameObject("SectionHeader_" + text);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0f, 0.5f);
		rectTransform.anchorMax = new Vector2(1f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = new Vector2(0f, 48f);
		LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
		layoutElement.preferredHeight = 48f;
		layoutElement.minHeight = 48f;
		layoutElement.flexibleHeight = 0f;
		GameObject gameObject2 = new GameObject("HeaderInner");
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform rectTransform2 = gameObject2.AddComponent<RectTransform>();
		rectTransform2.anchorMin = Vector2.zero;
		rectTransform2.anchorMax = Vector2.one;
		rectTransform2.offsetMin = new Vector2(6f, 0f);
		rectTransform2.offsetMax = new Vector2(-6f, 0f);
		TMP_Text tMP_Text = null;
		if (fontSource != null)
		{
			try
			{
				GameObject gameObject3 = UnityEngine.Object.Instantiate(fontSource.gameObject, gameObject2.transform);
				gameObject3.name = "HeaderText";
				foreach (TweakUIBase componentsInChild in gameObject3.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
				{
					if (componentsInChild != null)
					{
						UnityEngine.Object.Destroy(componentsInChild);
					}
				}
				RectTransform obj = gameObject3.GetComponent<RectTransform>() ?? gameObject3.AddComponent<RectTransform>();
				obj.anchorMin = Vector2.zero;
				obj.anchorMax = Vector2.one;
				obj.offsetMin = Vector2.zero;
				obj.offsetMax = Vector2.zero;
				tMP_Text = gameObject3.GetComponent<TMP_Text>();
				if (tMP_Text != null)
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
					}
					tMP_Text.text = text;
					tMP_Text.raycastTarget = false;
					tMP_Text.alignment = TextAlignmentOptions.MidlineLeft;
					tMP_Text.color = SectionHeader;
					tMP_Text.fontStyle = FontStyles.Bold;
					try
					{
						tMP_Text.ForceMeshUpdate();
					}
					catch
					{
					}
				}
			}
			catch (Exception ex)
			{
				CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
				if (instance != null)
				{
					ManualLogSource log = instance.Log;
					if (log != null)
					{
						bool isEnabled = false;
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(30, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SectionHeader] clone failed: ");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
						}
						log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
					}
				}
				tMP_Text = null;
			}
		}
		if (tMP_Text == null)
		{
			tMP_Text = BuildText(gameObject2.transform, text, fontSource, 26f, TextAlignmentOptions.MidlineLeft, SectionHeader, bold: true);
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

	public static (RectTransform, RectTransform) CreatePreviewBlock(Transform parent, float height)
	{
		GameObject gameObject = new GameObject("CrosshairPreviewBlock");
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0f, 0.5f);
		rectTransform.anchorMax = new Vector2(1f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = new Vector2(0f, height);
		LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
		layoutElement.preferredHeight = height;
		layoutElement.minHeight = height;
		layoutElement.flexibleHeight = 0f;
		Image image = gameObject.AddComponent<Image>();
		image.color = Color.white;
		image.raycastTarget = false;
		try
		{
			Sprite verticalDarkGradientSprite = GetVerticalDarkGradientSprite();
			if (verticalDarkGradientSprite != null)
			{
				image.sprite = verticalDarkGradientSprite;
				image.type = Image.Type.Simple;
			}
			else
			{
				image.color = PreviewBg;
			}
		}
		catch
		{
			image.color = PreviewBg;
		}
		GameObject gameObject2 = new GameObject("PreviewHolder");
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform rectTransform2 = gameObject2.AddComponent<RectTransform>();
		rectTransform2.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform2.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform2.pivot = new Vector2(0.5f, 0.5f);
		rectTransform2.anchoredPosition = Vector2.zero;
		rectTransform2.sizeDelta = new Vector2(0f, 0f);
		return (rectTransform, rectTransform2);
	}

	public static Button CreateButton(Transform parent, string label, Action onClick, TMP_Text fontSource)
	{
		GameObject gameObject = new GameObject("Button_" + label);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0f, 0.5f);
		rectTransform.anchorMax = new Vector2(1f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = new Vector2(0f, 28f);
		LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
		layoutElement.preferredHeight = 28f;
		layoutElement.minHeight = 28f;
		layoutElement.flexibleHeight = 0f;
		Image image = gameObject.AddComponent<Image>();
		image.color = ButtonOff;
		image.raycastTarget = true;
		Button button = gameObject.AddComponent<Button>();
		ColorBlock colors = button.colors;
		colors.normalColor = ButtonOff;
		colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.95f);
		colors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 0.95f);
		colors.selectedColor = ButtonOff;
		colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
		button.colors = colors;
		GameObject gameObject2 = new GameObject("Label");
		gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
		RectTransform rectTransform2 = gameObject2.AddComponent<RectTransform>();
		rectTransform2.anchorMin = Vector2.zero;
		rectTransform2.anchorMax = Vector2.one;
		rectTransform2.offsetMin = Vector2.zero;
		rectTransform2.offsetMax = Vector2.zero;
		TMP_Text tMP_Text = null;
		if (fontSource != null)
		{
			try
			{
				GameObject gameObject3 = UnityEngine.Object.Instantiate(fontSource.gameObject, gameObject2.transform);
				gameObject3.name = "ButtonText";
				foreach (TweakUIBase componentsInChild in gameObject3.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
				{
					if (componentsInChild != null)
					{
						UnityEngine.Object.Destroy(componentsInChild);
					}
				}
				RectTransform obj = gameObject3.GetComponent<RectTransform>() ?? gameObject3.AddComponent<RectTransform>();
				obj.anchorMin = Vector2.zero;
				obj.anchorMax = Vector2.one;
				obj.offsetMin = Vector2.zero;
				obj.offsetMax = Vector2.zero;
				tMP_Text = gameObject3.GetComponent<TMP_Text>();
			}
			catch
			{
				tMP_Text = null;
			}
		}
		if (tMP_Text == null)
		{
			tMP_Text = gameObject2.AddComponent<TextMeshProUGUI>();
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
						rustText.English = label;
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			tMP_Text.text = label;
			tMP_Text.alignment = TextAlignmentOptions.Center;
			tMP_Text.fontStyle = FontStyles.Bold;
			tMP_Text.color = LabelColor;
			tMP_Text.raycastTarget = false;
		}
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener((Action)delegate
		{
			try
			{
				onClick?.Invoke();
			}
			catch (Exception t)
			{
				CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
				if (instance != null)
				{
					ManualLogSource log = instance.Log;
					if (log != null)
					{
						bool isEnabled = false;
						BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(12, 2, out isEnabled);
						if (isEnabled)
						{
							bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[Button '");
							bepInExErrorLogInterpolatedStringHandler.AppendFormatted(label);
							bepInExErrorLogInterpolatedStringHandler.AppendLiteral("'] ");
							bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
						}
						log.LogError(bepInExErrorLogInterpolatedStringHandler);
					}
				}
			}
		});
		return button;
	}

	public static ToggleHandle CreateToggle(Transform parent, string labelText, bool initial, Action<bool> onChanged, TMP_Text fontSource)
	{
		return CreateToggle(parent, labelText, null, initial, onChanged, fontSource);
	}

	public static ToggleHandle CreateToggle(Transform parent, string labelText, string tooltipText, bool initial, Action<bool> onChanged, TMP_Text fontSource)
	{
		SystemToggleFactory.Handle handle = SystemToggleFactory.Create(parent, labelText, tooltipText, initial, onChanged, fontSource);
		if (handle != null)
		{
			return new ToggleHandle
			{
				Row = handle.Row,
				Button = null,
				Background = null,
				Text = handle.Label,
				Value = initial,
				OnChanged = onChanged,
				SystemToggle = handle
			};
		}
		ManualLogSource manualLogSource = CBSafeCrosshairPlugin.Instance?.Log;
		if (manualLogSource != null)
		{
			bool isEnabled = false;
			BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(45, 1, out isEnabled);
			if (isEnabled)
			{
				bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[CreateToggle '");
				bepInExWarningLogInterpolatedStringHandler.AppendFormatted(labelText);
				bepInExWarningLogInterpolatedStringHandler.AppendLiteral("'] no system template -- skipped");
			}
			manualLogSource.LogWarning(bepInExWarningLogInterpolatedStringHandler);
		}
		return null;
	}

	public static void SetToggleSilently(ToggleHandle handle, bool value)
	{
		if (handle != null)
		{
			handle.Value = value;
			if (handle.SystemToggle != null)
			{
				SystemToggleFactory.SetSilently(handle.SystemToggle, value);
			}
		}
	}

	public static SliderHandle CreateSlider(Transform parent, string labelText, float min, float max, float initial, string format, Action<float> onChanged, TMP_Text fontSource)
	{
		try
		{
			SystemSliderFactory.Handle handle3 = SystemSliderFactory.Create(parent, labelText, min, max, initial, format, onChanged, fontSource);
			if (handle3 != null)
			{
				return new SliderHandle
				{
					Row = handle3.Row,
					Slider = handle3.SliderControl,
					ValueText = handle3.ValueText,
					Format = handle3.Format,
					OnChanged = onChanged,
					SystemSlider = handle3
				};
			}
		}
		catch (Exception ex)
		{
			CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
			if (instance != null && instance.Log != null)
			{
				instance.Log.LogWarning("[CreateSlider '" + labelText + "'] system clone failed -- using custom: " + ex.Message);
			}
		}
		RectTransform rectTransform = MakeRow(parent, "Slider_" + labelText);
		try
		{
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 36f);
			LayoutElement component = rectTransform.GetComponent<LayoutElement>();
			if (component != null)
			{
				component.preferredHeight = 36f;
				component.minHeight = 36f;
			}
		}
		catch
		{
		}
		GameObject gameObject = new GameObject("Label");
		gameObject.transform.SetParent(rectTransform, worldPositionStays: false);
		RectTransform rectTransform2 = gameObject.AddComponent<RectTransform>();
		rectTransform2.anchorMin = new Vector2(0f, 0f);
		rectTransform2.anchorMax = new Vector2(0.35f, 1f);
		rectTransform2.offsetMin = new Vector2(0f, 0f);
		rectTransform2.offsetMax = new Vector2(-6f, 0f);
		BuildText(gameObject.transform, labelText, fontSource, 0f, TextAlignmentOptions.MidlineLeft, LabelColor, bold: false, inheritStyle: true);
		GameObject gameObject2 = new GameObject("Control");
		gameObject2.transform.SetParent(rectTransform, worldPositionStays: false);
		RectTransform rectTransform3 = gameObject2.AddComponent<RectTransform>();
		rectTransform3.anchorMin = new Vector2(0.35f, 0f);
		rectTransform3.anchorMax = new Vector2(1f, 1f);
		rectTransform3.offsetMin = new Vector2(6f, 0f);
		rectTransform3.offsetMax = new Vector2(-6f, 0f);
		GameObject gameObject3 = new GameObject("Value");
		gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		RectTransform rectTransform4 = gameObject3.AddComponent<RectTransform>();
		rectTransform4.anchorMin = new Vector2(0f, 0f);
		rectTransform4.anchorMax = new Vector2(0f, 1f);
		rectTransform4.pivot = new Vector2(0f, 0.5f);
		rectTransform4.sizeDelta = new Vector2(64f, 0f);
		rectTransform4.anchoredPosition = new Vector2(0f, 0f);
		TMP_Text tMP_Text = BuildText(gameObject3.transform, "0", fontSource, 14f, TextAlignmentOptions.MidlineRight, LabelColor, bold: false, inheritStyle: true);
		try
		{
			tMP_Text.outlineColor = new Color(0f, 0f, 0f, 0f);
		}
		catch
		{
		}
		try
		{
			tMP_Text.outlineWidth = 0f;
		}
		catch
		{
		}
		try
		{
			Material fontMaterial = tMP_Text.fontMaterial;
			if (fontMaterial != null)
			{
				try
				{
					fontMaterial.SetFloat("_OutlineWidth", 0f);
				}
				catch
				{
				}
				try
				{
					fontMaterial.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 0f));
				}
				catch
				{
				}
				try
				{
					fontMaterial.SetFloat("_UnderlayOffsetX", 0f);
				}
				catch
				{
				}
				try
				{
					fontMaterial.SetFloat("_UnderlayOffsetY", 0f);
				}
				catch
				{
				}
				try
				{
					fontMaterial.SetFloat("_UnderlaySoftness", 0f);
				}
				catch
				{
				}
				try
				{
					fontMaterial.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0f));
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		GameObject gameObject4 = new GameObject("Slider");
		gameObject4.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		RectTransform obj11 = gameObject4.GetComponent<RectTransform>() ?? gameObject4.AddComponent<RectTransform>();
		obj11.anchorMin = new Vector2(0f, 0.5f);
		obj11.anchorMax = new Vector2(1f, 0.5f);
		obj11.pivot = new Vector2(0.5f, 0.5f);
		obj11.offsetMin = new Vector2(100f, -5f);
		obj11.offsetMax = new Vector2(-16f, 5f);
		bool isEnabled = false;
		try
		{
			CBSafeCrosshairPlugin instance2 = CBSafeCrosshairPlugin.Instance;
			if (instance2 != null)
			{
				ManualLogSource log = instance2.Log;
				if (log != null)
				{
					BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(64, 4, out isEnabled);
					if (isEnabled)
					{
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[Slider '");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(labelText);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("'] indent: left=");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(100f);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" right=");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(16f);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" (bar shorter than ctrlGo by ");
						bepInExInfoLogInterpolatedStringHandler.AppendFormatted(116f);
						bepInExInfoLogInterpolatedStringHandler.AppendLiteral("px)");
					}
					log.LogInfo(bepInExInfoLogInterpolatedStringHandler);
				}
			}
		}
		catch
		{
		}
		Image image = gameObject4.AddComponent<Image>();
		image.color = SliderTrack;
		image.raycastTarget = true;
		try
		{
			Sprite whiteSprite = GetWhiteSprite();
			if (whiteSprite != null)
			{
				image.sprite = whiteSprite;
			}
		}
		catch
		{
		}
		try
		{
			Outline outline = gameObject4.AddComponent<Outline>();
			outline.effectColor = SliderTrackBorder;
			outline.effectDistance = new Vector2(1f, -1f);
			outline.useGraphicAlpha = false;
		}
		catch (Exception ex2)
		{
			CBSafeCrosshairPlugin instance3 = CBSafeCrosshairPlugin.Instance;
			if (instance3 != null)
			{
				ManualLogSource log2 = instance3.Log;
				if (log2 != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(17, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[Slider outline] ");
						bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex2.Message);
					}
					log2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
				}
			}
		}
		GameObject gameObject5 = new GameObject("Fill Area");
		gameObject5.transform.SetParent(gameObject4.transform, worldPositionStays: false);
		RectTransform rectTransform5 = gameObject5.AddComponent<RectTransform>();
		rectTransform5.anchorMin = new Vector2(0f, 0f);
		rectTransform5.anchorMax = new Vector2(1f, 1f);
		rectTransform5.offsetMin = new Vector2(2f, 1f);
		rectTransform5.offsetMax = new Vector2(-2f, -1f);
		GameObject gameObject6 = new GameObject("Fill");
		gameObject6.transform.SetParent(gameObject5.transform, worldPositionStays: false);
		RectTransform rectTransform6 = gameObject6.AddComponent<RectTransform>();
		rectTransform6.anchorMin = Vector2.zero;
		rectTransform6.anchorMax = new Vector2(0f, 1f);
		rectTransform6.offsetMin = Vector2.zero;
		rectTransform6.offsetMax = Vector2.zero;
		Image image2 = gameObject6.AddComponent<Image>();
		image2.color = SliderFill;
		image2.raycastTarget = false;
		try
		{
			Sprite whiteSprite2 = GetWhiteSprite();
			if (whiteSprite2 != null)
			{
				image2.sprite = whiteSprite2;
			}
		}
		catch
		{
		}
		GameObject gameObject7 = new GameObject("Handle Slide Area");
		gameObject7.transform.SetParent(gameObject4.transform, worldPositionStays: false);
		RectTransform rectTransform7 = gameObject7.AddComponent<RectTransform>();
		rectTransform7.anchorMin = new Vector2(0f, 0.5f);
		rectTransform7.anchorMax = new Vector2(1f, 0.5f);
		rectTransform7.pivot = new Vector2(0.5f, 0.5f);
		rectTransform7.offsetMin = new Vector2(7f, -7f);
		rectTransform7.offsetMax = new Vector2(-7f, 7f);
		GameObject gameObject8 = new GameObject("Handle");
		gameObject8.transform.SetParent(gameObject7.transform, worldPositionStays: false);
		RectTransform rectTransform8 = gameObject8.AddComponent<RectTransform>();
		rectTransform8.anchorMin = new Vector2(0f, 0.5f);
		rectTransform8.anchorMax = new Vector2(0f, 0.5f);
		rectTransform8.pivot = new Vector2(0.5f, 0.5f);
		rectTransform8.sizeDelta = new Vector2(14f, 14f);
		Image image3 = gameObject8.AddComponent<Image>();
		image3.color = SliderHandleCol;
		image3.raycastTarget = false;
		Slider slider = gameObject4.AddComponent<Slider>();
		slider.fillRect = rectTransform6;
		slider.handleRect = rectTransform8;
		slider.targetGraphic = image3;
		slider.m_MinValue = min;
		slider.m_MaxValue = max;
		slider.m_WholeNumbers = false;
		slider.value = Mathf.Clamp(initial, min, max);
		slider.direction = Slider.Direction.LeftToRight;
		SliderHandle handle2 = new SliderHandle
		{
			Row = rectTransform,
			Slider = slider,
			ValueText = tMP_Text,
			Format = (format ?? "0.00"),
			OnChanged = onChanged
		};
		UpdateSliderText(handle2, slider.value);
		((UnityEvent<float>)slider.onValueChanged).AddListener((Action<float>)delegate(float v)
		{
			UpdateSliderText(handle2, v);
			try
			{
				handle2.OnChanged?.Invoke(v);
			}
			catch (Exception t)
			{
				CBSafeCrosshairPlugin instance4 = CBSafeCrosshairPlugin.Instance;
				if (instance4 != null)
				{
					ManualLogSource log3 = instance4.Log;
					if (log3 != null)
					{
						bool isEnabled2 = false;
						BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(15, 1, out isEnabled2);
						if (isEnabled2)
						{
							bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[SliderChange] ");
							bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
						}
						log3.LogError(bepInExErrorLogInterpolatedStringHandler);
					}
				}
			}
		});
		return handle2;
	}

	public static void SetSliderSilently(SliderHandle h, float value)
	{
		if (h == null)
		{
			return;
		}
		if (h.SystemSlider != null)
		{
			SystemSliderFactory.SetSilently(h.SystemSlider, value);
		}
		else
		{
			if (h.Slider == null)
			{
				return;
			}
			float num = Mathf.Clamp(value, h.Slider.m_MinValue, h.Slider.m_MaxValue);
			Action<float> onChanged = h.OnChanged;
			h.OnChanged = null;
			try
			{
				h.Slider.SetValueWithoutNotify(num);
			}
			catch
			{
				try
				{
					h.Slider.value = num;
				}
				catch
				{
				}
			}
			finally
			{
				h.OnChanged = onChanged;
			}
			UpdateSliderText(h, num);
		}
	}

	private static void UpdateSliderText(SliderHandle h, float v)
	{
		if (h == null || h.ValueText == null)
		{
			return;
		}
		try
		{
			string text = v.ToString(h.Format, CultureInfo.InvariantCulture);
			try
			{
				RustText rustText = h.ValueText.TryCast<RustText>();
				if (rustText != null)
				{
					try
					{
						rustText.English = text;
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			h.ValueText.text = text;
		}
		catch
		{
		}
	}
}
