using System;
using BepInEx.Configuration;
using CBSafe.Patches;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Rust.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CBSafe.UI;

public static class OptionsMenu
{
	[HarmonyPatch(typeof(MenuBackgroundVideo), "ReadyVideo")]
	internal static class MenuReadyPatch
	{
		[HarmonyPrefix]
		internal static void Prefix()
		{
			if (_tabsCreated)
			{
				return;
			}
			try
			{
				CreateOptimizationTab();
				CreateGlassAkTab();
				ItemIconPatch.SyncIcon();
				_tabsCreated = true;
			}
			catch
			{
			}
		}
	}

	private static bool _tabsCreated;

	private static void CreateTab(string tabName, string tabNewName, string tabRusName)
	{
		GameObject gameObject = GameObject.Find(tabName);
		if (gameObject == null)
		{
			return;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
		gameObject2.name = tabNewName;
		Transform transform = gameObject2.transform.Find("Text (TMP)");
		if (transform != null)
		{
			LocalizeText component = transform.GetComponent<LocalizeText>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
			TextMeshProUGUI component2 = transform.GetComponent<TextMeshProUGUI>();
			if (component2 != null)
			{
				component2.text = tabRusName;
			}
		}
		GameObject gameObject3 = GameObject.Find("MenuUI(Clone)/Body/Panel/Options/Categories/");
		if (gameObject3 != null)
		{
			gameObject2.transform.SetParent(gameObject3.transform, worldPositionStays: false);
		}
	}

	private static void HideNamed(Transform root, params string[] names)
	{
		if (root == null)
		{
			return;
		}
		foreach (string text in names)
		{
			Transform transform = root.Find(text);
			if (transform != null)
			{
				transform.gameObject.SetActive(value: false);
				continue;
			}
			for (int j = 0; j < root.childCount; j++)
			{
				Transform child = root.GetChild(j);
				if (child.name.Equals(text, StringComparison.OrdinalIgnoreCase) || child.name.Contains(text))
				{
					child.gameObject.SetActive(value: false);
				}
			}
		}
	}

	private static void CleanTooltipsAndLocalization(Transform root)
	{
		if (root == null)
		{
			return;
		}
		try
		{
			Tooltip component = root.GetComponent<Tooltip>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
		}
		catch
		{
		}
		try
		{
			LocalizeText component2 = root.GetComponent<LocalizeText>();
			if (component2 != null)
			{
				UnityEngine.Object.Destroy(component2);
			}
		}
		catch
		{
		}
	}

	private static void SyncToggleVisual(Toggle toggle, bool on)
	{
		if (toggle == null)
		{
			return;
		}
		try
		{
			Transform transform = toggle.transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				Transform child = transform.GetChild(i);
				string name = child.name;
				if (string.Equals(name, "ON", StringComparison.OrdinalIgnoreCase))
				{
					child.gameObject.SetActive(on);
				}
				else if (string.Equals(name, "OFF", StringComparison.OrdinalIgnoreCase))
				{
					child.gameObject.SetActive(!on);
				}
			}
			if (toggle.graphic != null)
			{
				toggle.graphic.canvasRenderer.SetAlpha(on ? 1f : 0f);
			}
		}
		catch
		{
		}
	}

	private static Transform CreateButton(Transform transform, string transformName, string buttonNameText, bool currentState, ConfigEntry<bool> toChange, Action<bool> onStateChanged = null)
	{
		if (transform == null)
		{
			return null;
		}
		transform.name = transformName;
		CleanTooltipsAndLocalization(transform);
		TweakUIToggle component = transform.GetComponent<TweakUIToggle>();
		if (component != null)
		{
			UnityEngine.Object.Destroy(component);
		}
		Transform transform2 = transform.Find("Text");
		if (transform2 != null)
		{
			CleanTooltipsAndLocalization(transform2);
			TextMeshProUGUI component2 = transform2.GetComponent<TextMeshProUGUI>();
			if (component2 != null)
			{
				component2.text = buttonNameText;
			}
		}
		Transform transform3 = transform.Find("Toggle");
		if (transform3 != null)
		{
			CleanTooltipsAndLocalization(transform3);
			Toggle toggle = transform3.GetComponent<Toggle>();
			if (toggle != null)
			{
				toggle.onValueChanged.RemoveAllListeners();
				try
				{
					toggle.toggleTransition = Toggle.ToggleTransition.None;
				}
				catch
				{
				}
				toggle.isOn = currentState;
				SyncToggleVisual(toggle, currentState);
				((UnityEvent<bool>)toggle.onValueChanged).AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>((Action<bool>)delegate(bool p0)
				{
					SyncToggleVisual(toggle, p0);
					if (toChange != null)
					{
						toChange.Value = p0;
						ModConfig.Save();
					}
					onStateChanged?.Invoke(p0);
				}));
			}
		}
		return transform;
	}

	private static Transform CreateDropdown(Transform transform, string transformName, string labelText, string[] values, ConfigEntry<int> toChange, Action<int> onSelect)
	{
		if (transform == null)
		{
			return null;
		}
		transform.name = transformName;
		CleanTooltipsAndLocalization(transform);
		TweakUIDropdown component = transform.GetComponent<TweakUIDropdown>();
		if (component != null)
		{
			UnityEngine.Object.Destroy(component);
		}
		Transform transform2 = transform.Find("Text");
		if (transform2 != null)
		{
			CleanTooltipsAndLocalization(transform2);
			TextMeshProUGUI component2 = transform2.GetComponent<TextMeshProUGUI>();
			if (component2 != null)
			{
				component2.text = labelText;
			}
		}
		Transform transform3 = transform.Find("Container");
		TextMeshProUGUI current = transform3.Find("Current").GetComponent<TextMeshProUGUI>();
		Transform transform4 = transform3.Find("Dropdown/Scroll View/Content");
		RustButton dismisserBtn = transform3.Find("Dropdown/Dismisser").GetComponent<RustButton>();
		Transform transform5 = transform4.Find("Item");
		Transform transform6 = transform3.Find("Dropdown/Scroll View/Scrollbar Vertical/Sliding Area");
		if (transform6 != null)
		{
			transform6.gameObject.SetActive(value: false);
		}
		int index = ((toChange != null) ? toChange.Value : 0);
		if (index < 0)
		{
			index = 0;
		}
		if (values != null && index >= values.Length)
		{
			index = values.Length - 1;
		}
		if (values != null && values.Length != 0)
		{
			current.text = values[index];
		}
		for (int num = transform4.childCount - 1; num >= 0; num--)
		{
			Transform child = transform4.GetChild(num);
			if (child != transform5)
			{
				UnityEngine.Object.Destroy(child.gameObject);
			}
		}
		transform5.gameObject.SetActive(value: false);
		if (values != null)
		{
			for (int i = 0; i < values.Length; i++)
			{
				int capturedIndex = i;
				GameObject gameObject = UnityEngine.Object.Instantiate(transform5.gameObject, transform4);
				gameObject.SetActive(value: true);
				gameObject.name = "Item";
				CleanTooltipsAndLocalization(gameObject.transform);
				Transform transform7 = gameObject.transform.Find("Label");
				CleanTooltipsAndLocalization(transform7);
				transform7.GetComponent<TextMeshProUGUI>().text = values[capturedIndex];
				RustButton component3 = gameObject.GetComponent<RustButton>();
				component3.OnPressed.RemoveAllListeners();
				component3.OnPressed.AddListener(DelegateSupport.ConvertDelegate<UnityAction>((Action)delegate
				{
					index = capturedIndex;
					current.text = values[index];
					dismisserBtn.OnPressed.Invoke();
					if (toChange != null)
					{
						toChange.Value = index;
						ModConfig.Save();
					}
					onSelect?.Invoke(index);
				}));
			}
		}
		return transform;
	}

	private static void CreateOptimizationTab()
	{
		CreateTab("MenuUI(Clone)/Body/Panel/Options/Categories/Experimental", "OptimizationTab", "ОПТИМИЗАЦИЯ");
		GameObject gameObject = GameObject.Find("MenuUI(Clone)/Body/Panel/Options/Panels/Panel/Image Effects");
		if (gameObject == null)
		{
			return;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
		gameObject2.name = "OptimizationTab";
		Transform transform = gameObject2.transform.Find("IMAGE EFFECTS");
		if (transform != null)
		{
			LocalizeText component = transform.GetComponent<LocalizeText>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
			TextMeshProUGUI component2 = transform.GetComponent<TextMeshProUGUI>();
			if (component2 != null)
			{
				component2.text = "ОПТИМИЗАЦИЯ КЛИЕНТА";
			}
		}
		HideNamed(gameObject2.transform, "Toggle - effects.bloom", "Toggle - effects.dof", "Toggle - effects.ao", "Toggle - effects.lensdirt", "Toggle - effects.motion", "Toggle - effects.shafts", "Toggle - effects.sharpen", "Dropdown - effects.antialiasing", "Toggle - effects.vignet");
		Transform toggleTemplate = gameObject.transform.Find("Toggle - effects.bloom");
		Transform transform2 = gameObject2.transform.Find("Dropdown - effects.antialiasing");
		Transform parent = ((transform2 != null) ? transform2.parent : null) ?? gameObject2.transform;
		CloneToggle("OptimizeShadowsToggle", "ОТКЛЮЧЕНИЕ ТЕНЕЙ", ModConfig.OptimizeShadows, delegate
		{
			Optimizations.UpdateShadows();
		});
		CloneToggle("OptimizeGrassToggle", "ОТКЛЮЧЕНИЕ ТРАВЫ", ModConfig.OptimizeGrass, null);
		CloneToggle("OptimizeWeatherToggle", "ОТКЛЮЧЕНИЕ ПОГОДЫ", ModConfig.OptimizeWeather, null);
		CloneToggle("SuperPotatoToggle", "SUPER POTATO MODE", ModConfig.SuperPotatoMode, delegate
		{
			Optimizations.UpdatePotatoMode();
		});
		GameObject gameObject3 = GameObject.Find("MenuUI(Clone)/Body/Panel/Options/Panels/Panel/");
		if (gameObject3 != null)
		{
			gameObject2.transform.SetParent(gameObject3.transform, worldPositionStays: false);
		}
		GameObject CloneToggle(string name, string label, ConfigEntry<bool> entry, Action<bool> onStateChanged)
		{
			if (!(toggleTemplate == null))
			{
				GameObject gameObject4 = UnityEngine.Object.Instantiate(toggleTemplate.gameObject);
				gameObject4.name = name;
				gameObject4.transform.SetParent(parent, worldPositionStays: false);
				gameObject4.SetActive(value: true);
				CreateButton(gameObject4.transform, name, label, entry?.Value ?? false, entry, onStateChanged);
				return gameObject4;
			}
			return null;
		}
	}

	private static void CreateGlassAkTab()
	{
		CreateTab("MenuUI(Clone)/Body/Panel/Options/Categories/Experimental", "GlassAkTab", "СТЕКЛЯННЫЙ КАЛАШ");
		GameObject gameObject = GameObject.Find("MenuUI(Clone)/Body/Panel/Options/Panels/Panel/Image Effects");
		if (gameObject == null)
		{
			return;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
		gameObject2.name = "GlassAkTab";
		Transform transform = gameObject2.transform.Find("IMAGE EFFECTS");
		if (transform != null)
		{
			LocalizeText component = transform.GetComponent<LocalizeText>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
			TextMeshProUGUI component2 = transform.GetComponent<TextMeshProUGUI>();
			if (component2 != null)
			{
				component2.text = "НАСТРОЙКИ СТЕКЛЯННОГО КАЛАША";
			}
		}
		HideNamed(gameObject2.transform, "Toggle - effects.bloom", "Toggle - effects.dof", "Toggle - effects.ao", "Toggle - effects.lensdirt", "Toggle - effects.motion", "Toggle - effects.shafts", "Toggle - effects.sharpen", "Dropdown - effects.antialiasing", "Toggle - effects.vignet");
		Transform transform2 = gameObject.transform.Find("Toggle - effects.bloom");
		Transform transform3 = gameObject.transform.Find("Dropdown - effects.antialiasing");
		Transform transform4 = gameObject2.transform.Find("Dropdown - effects.antialiasing");
		Transform parent = ((transform4 != null) ? transform4.parent : null) ?? gameObject2.transform;
		if (transform2 != null)
		{
			GameObject gameObject3 = UnityEngine.Object.Instantiate(transform2.gameObject);
			gameObject3.name = "GlassAkMainToggle";
			gameObject3.transform.SetParent(parent, worldPositionStays: false);
			gameObject3.SetActive(value: true);
			CreateButton(gameObject3.transform, "GlassAkMainToggle", "ВКЛЮЧИТЬ СТЕКЛЯННЫЙ КАЛАШ", ModConfig.GlassAkEnabled.Value, ModConfig.GlassAkEnabled, delegate
			{
				ItemIconPatch.SyncIcon();
				GlassAk.ApplyActive();
			});
		}
		if (transform2 != null)
		{
			GameObject gameObject4 = UnityEngine.Object.Instantiate(transform2.gameObject);
			gameObject4.name = "RgbToggle";
			gameObject4.transform.SetParent(parent, worldPositionStays: false);
			gameObject4.SetActive(value: true);
			CreateButton(gameObject4.transform, "RgbToggle", "RGB АНИМАЦИЯ КАЛАША", ModConfig.RgbEnabled.Value, ModConfig.RgbEnabled, delegate
			{
				GlassAk.ApplyCurrentColor();
			});
		}
		if (transform3 != null)
		{
			GameObject gameObject5 = UnityEngine.Object.Instantiate(transform3.gameObject);
			gameObject5.name = "GlassAkColorDropdown";
			gameObject5.transform.SetParent(parent, worldPositionStays: false);
			gameObject5.SetActive(value: true);
			CreateDropdown(gameObject5.transform, "GlassAkColorDropdown", "ЦВЕТ СТЕКЛЯННОГО КАЛАША", ModConfig.ColorNames, ModConfig.GlassColorIndex, delegate(int index)
			{
				if (index >= 0 && index < ModConfig.ColorKeys.Length)
				{
					GlassAk.SetColor(ModConfig.ColorKeys[index]);
				}
			});
		}
		GameObject gameObject6 = new GameObject("padding");
		gameObject6.transform.SetParent(parent, worldPositionStays: false);
		gameObject6.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 250f);
		LayoutElement layoutElement = gameObject6.AddComponent<LayoutElement>();
		layoutElement.minHeight = 250f;
		layoutElement.preferredHeight = 250f;
		gameObject6.SetActive(value: true);
		GameObject gameObject7 = GameObject.Find("MenuUI(Clone)/Body/Panel/Options/Panels/Panel/");
		if (gameObject7 != null)
		{
			gameObject2.transform.SetParent(gameObject7.transform, worldPositionStays: false);
		}
	}
}
