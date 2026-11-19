using System;
using System.Globalization;
using System.Reflection;
using BepInEx.Logging;
using CBSafe.Core;
using CBSafe.Core.UI;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Rust.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CBSafe.Crosshair.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HydraNoObfuscate]
internal static class SystemSliderFactory
{
	public sealed class Handle
	{
		public GameObject Root;

		public RectTransform Row;

		public Slider SliderControl;

		public TMP_Text Label;

		public TMP_Text ValueText;

		public string Format;

		public Action<float> OnChanged;

		public bool Initialized;
	}

	private static GameObject _cachedTemplate;

	private static int _cachedTemplateSceneHandle = -1;

	public static Handle Create(Transform parent, string labelText, float min, float max, float initial, string format, Action<float> onChanged, TMP_Text fontFallback)
	{
		ManualLogSource manualLogSource = CBSafeCrosshairPlugin.Instance?.Log;
		GameObject gameObject = FindTemplate(manualLogSource);
		if (gameObject == null)
		{
			manualLogSource?.LogWarning("[SystemSlider] no TweakUISlider template in memory -- falling back to custom slider");
			return null;
		}
		GameObject gameObject2 = new GameObject("SystemSlider_TempHost_" + labelText);
		gameObject2.SetActive(value: false);
		GameObject gameObject3;
		try
		{
			gameObject3 = UnityEngine.Object.Instantiate(gameObject, gameObject2.transform, worldPositionStays: false);
			gameObject3.name = "SystemSlider_" + labelText;
		}
		catch (Exception ex)
		{
			manualLogSource?.LogError("[SystemSlider] instantiate failed: " + ex);
			UnityEngine.Object.Destroy(gameObject2);
			return null;
		}
		Slider slider = null;
		TMP_Text tMP_Text = null;
		try
		{
			TweakUISlider tweakUISlider = gameObject3.GetComponent<TweakUISlider>() ?? gameObject3.GetComponentInChildren<TweakUISlider>(includeInactive: true);
			if (tweakUISlider != null)
			{
				try
				{
					slider = tweakUISlider.sliderControl;
				}
				catch
				{
				}
				try
				{
					tMP_Text = tweakUISlider.textControl;
				}
				catch
				{
				}
			}
		}
		catch (Exception ex2)
		{
			manualLogSource?.LogWarning("[SystemSlider] read slider/text refs: " + ex2.Message);
		}
		try
		{
			foreach (TweakUIBase componentsInChild in gameObject3.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
			{
				if (componentsInChild != null)
				{
					UnityEngine.Object.DestroyImmediate(componentsInChild);
				}
			}
		}
		catch (Exception ex3)
		{
			manualLogSource?.LogWarning("[SystemSlider] strip TweakUIBase: " + ex3.Message);
		}
		try
		{
			foreach (Component componentsInChild2 in gameObject3.GetComponentsInChildren<Component>(includeInactive: true))
			{
				if (componentsInChild2 == null)
				{
					continue;
				}
				string name;
				try
				{
					name = componentsInChild2.GetIl2CppType().Name;
				}
				catch
				{
					continue;
				}
				if (string.IsNullOrEmpty(name))
				{
					continue;
				}
				switch (name)
				{
				case "RectTransform":
				case "Transform":
				case "CanvasRenderer":
					continue;
				}
				bool num = LocStrings.Marker_Locali.Length > 0 && name.Contains(LocStrings.Marker_Locali);
				bool flag = LocStrings.Marker_Phrase.Length > 0 && name.Contains(LocStrings.Marker_Phrase);
				bool flag2 = LocStrings.Marker_Token.Length > 0 && name.Contains(LocStrings.Marker_Token);
				bool flag3 = name == "Tooltip";
				if (num || flag || flag2 || flag3)
				{
					try
					{
						UnityEngine.Object.DestroyImmediate(componentsInChild2);
					}
					catch
					{
					}
				}
			}
		}
		catch (Exception ex4)
		{
			manualLogSource?.LogWarning("[SystemSlider] strip locale components: " + ex4.Message);
		}
		if (slider == null)
		{
			try
			{
				slider = gameObject3.GetComponentInChildren<Slider>(includeInactive: true);
			}
			catch
			{
			}
		}
		if (slider == null)
		{
			manualLogSource?.LogWarning("[SystemSlider] no UnityEngine.UI.Slider on template -- using custom slider");
			UnityEngine.Object.Destroy(gameObject2);
			return null;
		}
		TMP_Text tMP_Text2 = null;
		try
		{
			TMP_Text tMP_Text3 = null;
			int num2 = -1;
			foreach (TMP_Text componentsInChild3 in gameObject3.GetComponentsInChildren<TMP_Text>(includeInactive: true))
			{
				if (!(componentsInChild3 == null) && !(componentsInChild3 == tMP_Text))
				{
					string text = null;
					try
					{
						text = componentsInChild3.text;
					}
					catch
					{
					}
					int num3 = text?.Length ?? 0;
					if (num3 > num2)
					{
						num2 = num3;
						tMP_Text3 = componentsInChild3;
					}
				}
			}
			tMP_Text2 = tMP_Text3;
		}
		catch
		{
		}
		if (tMP_Text == null)
		{
			try
			{
				TMP_Text tMP_Text4 = null;
				int num4 = int.MaxValue;
				foreach (TMP_Text componentsInChild4 in gameObject3.GetComponentsInChildren<TMP_Text>(includeInactive: true))
				{
					if (!(componentsInChild4 == null) && !(componentsInChild4 == tMP_Text2))
					{
						string text2 = null;
						try
						{
							text2 = componentsInChild4.text;
						}
						catch
						{
						}
						int num5 = text2?.Length ?? 0;
						if (num5 < num4)
						{
							num4 = num5;
							tMP_Text4 = componentsInChild4;
						}
					}
				}
				tMP_Text = tMP_Text4;
			}
			catch
			{
			}
		}
		if (tMP_Text2 != null)
		{
			ResetRustText(tMP_Text2, labelText);
			try
			{
				tMP_Text2.text = labelText;
			}
			catch
			{
			}
			try
			{
				tMP_Text2.raycastTarget = false;
			}
			catch
			{
			}
			try
			{
				tMP_Text2.ForceMeshUpdate();
			}
			catch
			{
			}
			try
			{
				tMP_Text2.gameObject.AddComponent<ToggleLabelEnforcer>().Bind(tMP_Text2, labelText);
			}
			catch (Exception ex5)
			{
				manualLogSource?.LogWarning("[SystemSlider] label enforcer attach: " + ex5.Message);
			}
		}
		else
		{
			manualLogSource?.LogWarning("[SystemSlider] no label TMP_Text found for '" + labelText + "'");
		}
		try
		{
			slider.m_MinValue = min;
			slider.m_MaxValue = max;
			slider.m_WholeNumbers = false;
			slider.SetValueWithoutNotify(Mathf.Clamp(initial, min, max));
		}
		catch (Exception ex6)
		{
			manualLogSource?.LogWarning("[SystemSlider] configure: " + ex6.Message);
		}
		gameObject3.transform.SetParent(parent, worldPositionStays: false);
		UnityEngine.Object.Destroy(gameObject2);
		RectTransform rectTransform = gameObject3.GetComponent<RectTransform>() ?? gameObject3.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0f, 0.5f);
		rectTransform.anchorMax = new Vector2(1f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = new Vector2(0f, 36f);
		LayoutElement obj13 = gameObject3.GetComponent<LayoutElement>() ?? gameObject3.AddComponent<LayoutElement>();
		obj13.preferredHeight = 36f;
		obj13.minHeight = 36f;
		obj13.flexibleHeight = 0f;
		Handle handle = new Handle
		{
			Root = gameObject3,
			Row = rectTransform,
			SliderControl = slider,
			Label = tMP_Text2,
			ValueText = tMP_Text,
			Format = (format ?? "0.00"),
			OnChanged = onChanged
		};
		UpdateValueText(handle, slider.value);
		try
		{
			slider.onValueChanged.RemoveAllListeners();
		}
		catch
		{
		}
		try
		{
			((UnityEvent<float>)slider.onValueChanged).AddListener((Action<float>)delegate(float v)
			{
				if (!handle.Initialized)
				{
					return;
				}
				UpdateValueText(handle, v);
				try
				{
					handle.OnChanged?.Invoke(v);
				}
				catch (Exception ex8)
				{
					CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
					if (instance != null && instance.Log != null)
					{
						instance.Log.LogError("[SystemSlider change] " + ex8);
					}
				}
			});
		}
		catch (Exception ex7)
		{
			manualLogSource?.LogWarning("[SystemSlider] hook onValueChanged: " + ex7.Message);
		}
		handle.Initialized = true;
		return handle;
	}

	public static void SetSilently(Handle h, float value)
	{
		if (h == null || h.SliderControl == null)
		{
			return;
		}
		float num = Mathf.Clamp(value, h.SliderControl.m_MinValue, h.SliderControl.m_MaxValue);
		Action<float> onChanged = h.OnChanged;
		h.OnChanged = null;
		try
		{
			h.SliderControl.SetValueWithoutNotify(num);
		}
		catch
		{
			try
			{
				h.SliderControl.value = num;
			}
			catch
			{
			}
		}
		h.OnChanged = onChanged;
		UpdateValueText(h, num);
	}

	private static void UpdateValueText(Handle h, float v)
	{
		if (h == null || h.ValueText == null)
		{
			return;
		}
		try
		{
			string text = v.ToString(h.Format ?? "0.00", CultureInfo.InvariantCulture);
			try
			{
				RustText rustText = h.ValueText.TryCast<RustText>();
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

	private static void ResetRustText(TMP_Text tmp, string labelText)
	{
		if (tmp == null)
		{
			return;
		}
		try
		{
			RustText rustText = tmp.TryCast<RustText>();
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
					rustText.English = labelText;
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
					rustText.Phrase = new Translate.Phrase(string.Empty, labelText);
				}
				catch
				{
				}
				try
				{
					rustText.SetText(labelText);
					return;
				}
				catch
				{
					return;
				}
			}
		}
		catch
		{
		}
	}

	public static void PreWarmTemplate()
	{
		FindTemplate(null);
	}

	private static GameObject FindTemplate(ManualLogSource log)
	{
		try
		{
			int handle = SceneManager.GetActiveScene().handle;
			if (_cachedTemplate != null && _cachedTemplateSceneHandle == handle)
			{
				return _cachedTemplate;
			}
			Il2CppReferenceArray<UnityEngine.Object> il2CppReferenceArray = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TweakUISlider>());
			if (il2CppReferenceArray == null || ((Il2CppArrayBase<UnityEngine.Object>)il2CppReferenceArray).Length == 0)
			{
				return null;
			}
			GameObject gameObject = null;
			for (int i = 0; i < ((Il2CppArrayBase<UnityEngine.Object>)il2CppReferenceArray).Length; i++)
			{
				UnityEngine.Object @object = ((Il2CppArrayBase<UnityEngine.Object>)il2CppReferenceArray)[i];
				if (@object == null)
				{
					continue;
				}
				TweakUISlider tweakUISlider = @object.TryCast<TweakUISlider>();
				if (!(tweakUISlider == null) && !(tweakUISlider.gameObject == null) && !string.Equals(tweakUISlider.convarName, "graphics.uiscale", StringComparison.OrdinalIgnoreCase))
				{
					if (gameObject == null && !string.Equals(tweakUISlider.convarName, "graphics.uiscale", StringComparison.OrdinalIgnoreCase))
					{
						gameObject = tweakUISlider.gameObject;
					}
					if (tweakUISlider.transform.parent != null)
					{
						_cachedTemplate = tweakUISlider.gameObject;
						_cachedTemplateSceneHandle = handle;
						return _cachedTemplate;
					}
				}
			}
			_cachedTemplate = gameObject;
			_cachedTemplateSceneHandle = handle;
			return gameObject;
		}
		catch (Exception ex)
		{
			log?.LogWarning("[SystemSlider] FindTemplate: " + ex.Message);
			return null;
		}
	}
}
