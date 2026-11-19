using System;
using System.Reflection;
using BepInEx.Configuration;
using CBSafe.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Colors.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
internal static class ColorPickerButtonFactory
{
	public static void Create(Transform parent, string label, ConfigEntry<string> colorConfig, ConfigEntry<bool> enabledConfig = null, Action onChanged = null)
	{
		try
		{
			GameObject gameObject = GameObject.Find("MenuUI(Clone)/Body/Panel/Options/Panels/Panel/Graphics/TweakGraphics/Graphics/Resolution");
			if (gameObject == null)
			{
				TweakUIDropdown componentInChildren = parent.root.GetComponentInChildren<TweakUIDropdown>(includeInactive: true);
				if (componentInChildren != null)
				{
					gameObject = componentInChildren.gameObject;
				}
			}
			if (gameObject == null)
			{
				return;
			}
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, parent);
			gameObject2.name = "ColorPicker_" + label;
			gameObject2.SetActive(value: true);
			foreach (MonoBehaviour componentsInChild in gameObject2.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
			{
				if (componentsInChild == null)
				{
					continue;
				}
				string name = componentsInChild.GetIl2CppType().Name;
				if (name.Contains("TweakUI") || name.Contains("Tooltip") || name.Contains("Dropdown") || name.Contains("Localize"))
				{
					try
					{
						_ = (UnityEngine.Object)componentsInChild;
						_ = 1;
					}
					catch
					{
					}
				}
			}
			foreach (Selectable componentsInChild2 in gameObject2.GetComponentsInChildren<Selectable>(includeInactive: true))
			{
				try
				{
					_ = (UnityEngine.Object)componentsInChild2;
					_ = 1;
				}
				catch
				{
				}
			}
			for (int i = 0; i < gameObject2.transform.childCount; i++)
			{
				Transform child = gameObject2.transform.GetChild(i);
				if (child.name != "Text")
				{
					child.gameObject.SetActive(value: false);
				}
			}
			Transform transform = gameObject2.transform.Find("Text");
			if (transform != null)
			{
				TextMeshProUGUI component = transform.GetComponent<TextMeshProUGUI>();
				if (component != null)
				{
					_ = component.font;
					_ = component.fontSharedMaterial;
					component.text = label;
					component.m_text = label;
				}
			}
			GameObject gameObject3 = new GameObject("ColorButton");
			gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
			RectTransform rectTransform = gameObject3.AddComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(1f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			rectTransform.pivot = new Vector2(1f, 0.5f);
			rectTransform.sizeDelta = new Vector2(250f, -10f);
			rectTransform.anchoredPosition = new Vector2(-10f, 0f);
			Image img = gameObject3.AddComponent<Image>();
			img.color = ParseColor(colorConfig.Value);
			Button button = gameObject3.AddComponent<Button>();
			ColorBlock colors = button.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
			colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
			button.colors = colors;
			GameObject gameObject4 = null;
			if (transform != null)
			{
				gameObject4 = UnityEngine.Object.Instantiate(transform.gameObject, gameObject3.transform);
				gameObject4.name = "ButtonText";
				RectTransform component2 = gameObject4.GetComponent<RectTransform>();
				if (component2 != null)
				{
					component2.anchorMin = new Vector2(0f, 0f);
					component2.anchorMax = new Vector2(1f, 1f);
					component2.sizeDelta = Vector2.zero;
					component2.anchoredPosition = Vector2.zero;
				}
				TextMeshProUGUI component3 = gameObject4.GetComponent<TextMeshProUGUI>();
				if (component3 != null)
				{
					component3.text = "ТАПАЙ";
					component3.alignment = TextAlignmentOptions.Center;
					component3.color = Color.white;
				}
			}
			if (gameObject4 == null)
			{
				gameObject4 = new GameObject("ButtonText");
				gameObject4.transform.SetParent(gameObject3.transform, worldPositionStays: false);
			}
			button.onClick.AddListener((Action)delegate
			{
				Color current3 = ParseColor(colorConfig.Value);
				ColorPickerOverlay.Open(colorConfig, current3, label, delegate
				{
					try
					{
						img.color = ParseColor(colorConfig.Value);
					}
					catch
					{
					}
					onChanged?.Invoke();
				});
			});
			if (enabledConfig != null && !enabledConfig.Value)
			{
				img.color = new Color(img.color.r, img.color.g, img.color.b, 0.2f);
			}
		}
		catch (Exception)
		{
		}
	}

	private static Color ParseColor(string val)
	{
		if (string.IsNullOrEmpty(val))
		{
			return Color.white;
		}
		string[] array = val.Split(',');
		if (array.Length >= 3 && byte.TryParse(array[0], out var result) && byte.TryParse(array[1], out var result2) && byte.TryParse(array[2], out var result3))
		{
			byte result4 = byte.MaxValue;
			if (array.Length >= 4)
			{
				byte.TryParse(array[3], out result4);
			}
			return new Color((float)(int)result / 255f, (float)(int)result2 / 255f, (float)(int)result3 / 255f, (float)(int)result4 / 255f);
		}
		return Color.white;
	}
}
