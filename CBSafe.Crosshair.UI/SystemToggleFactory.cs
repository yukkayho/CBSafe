using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Core;
using CBSafe.Core.UI;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Reflection;
using Rust.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CBSafe.Crosshair.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HydraNoObfuscate]
internal static class SystemToggleFactory
{
	public sealed class Handle
	{
		public GameObject Root;

		public RectTransform Row;

		public Toggle ToggleControl;

		public TMP_Text Label;

		public bool Value;

		public System.Action<bool> OnChanged;

		public GameObject OffGO;

		public GameObject OnGO;

		public bool Initialized;
	}

	private static GameObject _cachedTemplate;

	private static int _cachedTemplateSceneHandle = -1;

	public static Handle Create(Transform parent, string labelText, bool initial, System.Action<bool> onChanged, TMP_Text fontFallback)
	{
		return Create(parent, labelText, null, initial, onChanged, fontFallback);
	}

	public static Handle Create(Transform parent, string labelText, string tooltipText, bool initial, System.Action<bool> onChanged, TMP_Text fontFallback)
	{
		ManualLogSource manualLogSource = CBSafeCrosshairPlugin.Instance?.Log;
		GameObject gameObject = FindTemplate(manualLogSource);
		if (gameObject == null)
		{
			manualLogSource?.LogWarning("[SystemToggle] no TweakUIToggle template in memory -- falling back to custom toggle");
			return null;
		}
		GameObject gameObject2 = new GameObject("SystemToggle_TempHost_" + labelText);
		gameObject2.SetActive(value: false);
		bool isEnabled = false;
		GameObject gameObject3;
		try
		{
			gameObject3 = UnityEngine.Object.Instantiate(gameObject, gameObject2.transform, worldPositionStays: false);
			gameObject3.name = "SystemToggle_" + labelText;
		}
		catch (System.Exception t)
		{
			ManualLogSource manualLogSource2 = manualLogSource;
			if (manualLogSource2 != null)
			{
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(35, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[SystemToggle] instantiate failed: ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				manualLogSource2.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
			UnityEngine.Object.Destroy(gameObject2);
			return null;
		}
		try
		{
			foreach (TweakUIBase componentsInChild in gameObject3.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
			{
				if (componentsInChild != null)
				{
					UnityEngine.Object.Destroy(componentsInChild);
				}
			}
		}
		catch (System.Exception ex)
		{
			ManualLogSource manualLogSource3 = manualLogSource;
			if (manualLogSource3 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(34, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemToggle] strip TweakUIBase: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				manualLogSource3.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
		int num = 0;
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
				if (LocStrings.Marker_Locali.Length > 0 && LocStrings.Marker_Phrase.Length > 0 && LocStrings.Marker_Token.Length > 0 && (name.Contains(LocStrings.Marker_Locali) || name.Contains(LocStrings.Marker_Phrase) || name.Contains(LocStrings.Marker_Token)))
				{
					try
					{
						UnityEngine.Object.DestroyImmediate(componentsInChild2);
						num++;
					}
					catch
					{
					}
				}
			}
		}
		catch (System.Exception ex2)
		{
			ManualLogSource manualLogSource4 = manualLogSource;
			if (manualLogSource4 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler2 = new BepInExWarningLogInterpolatedStringHandler(40, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler2.AppendLiteral("[SystemToggle] strip locale components: ");
					bepInExWarningLogInterpolatedStringHandler2.AppendFormatted(ex2.Message);
				}
				manualLogSource4.LogWarning(bepInExWarningLogInterpolatedStringHandler2);
			}
		}
		Toggle toggle = null;
		try
		{
			toggle = gameObject3.GetComponent<Toggle>() ?? gameObject3.GetComponentInChildren<Toggle>(includeInactive: true);
		}
		catch
		{
		}
		if (toggle == null)
		{
			manualLogSource?.LogWarning("[SystemToggle] no inner UnityEngine.UI.Toggle on template -- using custom toggle");
			UnityEngine.Object.Destroy(gameObject2);
			return null;
		}
		try
		{
			foreach (Component componentsInChild3 in gameObject3.GetComponentsInChildren<Component>(includeInactive: true))
			{
				if (componentsInChild3 == null)
				{
					continue;
				}
				string name2;
				try
				{
					name2 = componentsInChild3.GetIl2CppType().Name;
				}
				catch
				{
					continue;
				}
				if (name2 != "Tooltip")
				{
					continue;
				}
				try
				{
					Il2CppSystem.Type il2CppType = componentsInChild3.GetIl2CppType();
					Il2CppSystem.Reflection.FieldInfo field = il2CppType.GetField("token");
					if (field != null)
					{
						field.SetValue((UnityEngine.Object)componentsInChild3, (Il2CppSystem.String)string.Empty);
					}
					Il2CppSystem.Reflection.FieldInfo field2 = il2CppType.GetField("Text");
					if (field2 != null)
					{
						field2.SetValue((UnityEngine.Object)componentsInChild3, (Il2CppSystem.String)(tooltipText ?? string.Empty));
					}
				}
				catch
				{
				}
			}
		}
		catch (System.Exception ex3)
		{
			ManualLogSource manualLogSource5 = manualLogSource;
			if (manualLogSource5 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler3 = new BepInExWarningLogInterpolatedStringHandler(31, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler3.AppendLiteral("[SystemToggle] tooltip defuse: ");
					bepInExWarningLogInterpolatedStringHandler3.AppendFormatted(ex3.Message);
				}
				manualLogSource5.LogWarning(bepInExWarningLogInterpolatedStringHandler3);
			}
		}
		TMP_Text tMP_Text = null;
		try
		{
			for (int i = 0; i < gameObject3.transform.childCount; i++)
			{
				Transform child = gameObject3.transform.GetChild(i);
				if (child == null)
				{
					continue;
				}
				switch (child.name ?? string.Empty)
				{
				case "Container":
				case "Toggle":
				case "Switch":
				case "Background":
				case "Checkmark":
				case "Indicator":
					continue;
				}
				TMP_Text componentInChildren = child.GetComponentInChildren<TMP_Text>(includeInactive: true);
				if (!(componentInChildren != null))
				{
					continue;
				}
				tMP_Text = componentInChildren;
				break;
			}
		}
		catch
		{
		}
		TMP_Text tMP_Text2 = null;
		int num2 = -1;
		try
		{
			foreach (TMP_Text componentsInChild4 in gameObject3.GetComponentsInChildren<TMP_Text>(includeInactive: true))
			{
				if (!(componentsInChild4 == null))
				{
					string text = null;
					try
					{
						text = componentsInChild4.text;
					}
					catch
					{
					}
					int num3 = text?.Length ?? 0;
					if (num3 > num2)
					{
						num2 = num3;
						tMP_Text2 = componentsInChild4;
					}
				}
			}
		}
		catch
		{
		}
		if (tMP_Text == null)
		{
			tMP_Text = tMP_Text2;
		}
		if (tMP_Text2 != null && num2 >= 5)
		{
			tMP_Text = tMP_Text2;
		}
		if (tMP_Text != null)
		{
			ResetRustText(tMP_Text, labelText);
			try
			{
				tMP_Text.text = labelText;
			}
			catch
			{
			}
			try
			{
				tMP_Text.raycastTarget = false;
			}
			catch
			{
			}
			try
			{
				tMP_Text.ForceMeshUpdate();
			}
			catch
			{
			}
		}
		else
		{
			ManualLogSource manualLogSource6 = manualLogSource;
			if (manualLogSource6 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler4 = new BepInExWarningLogInterpolatedStringHandler(60, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler4.AppendLiteral("[SystemToggle] no description TMP_Text found on clone for '");
					bepInExWarningLogInterpolatedStringHandler4.AppendFormatted(labelText);
					bepInExWarningLogInterpolatedStringHandler4.AppendLiteral("'");
				}
				manualLogSource6.LogWarning(bepInExWarningLogInterpolatedStringHandler4);
			}
		}
		gameObject3.transform.SetParent(parent, worldPositionStays: false);
		UnityEngine.Object.Destroy(gameObject2);
		if (tMP_Text != null)
		{
			ResetRustText(tMP_Text, labelText);
			try
			{
				tMP_Text.text = labelText;
			}
			catch
			{
			}
			try
			{
				tMP_Text.ForceMeshUpdate();
			}
			catch
			{
			}
			try
			{
				tMP_Text.gameObject.AddComponent<ToggleLabelEnforcer>().Bind(tMP_Text, labelText);
			}
			catch (System.Exception ex4)
			{
				ManualLogSource manualLogSource7 = manualLogSource;
				if (manualLogSource7 != null)
				{
					BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler5 = new BepInExWarningLogInterpolatedStringHandler(32, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExWarningLogInterpolatedStringHandler5.AppendLiteral("[SystemToggle] enforcer attach: ");
						bepInExWarningLogInterpolatedStringHandler5.AppendFormatted(ex4.Message);
					}
					manualLogSource7.LogWarning(bepInExWarningLogInterpolatedStringHandler5);
				}
			}
		}
		RectTransform rectTransform = gameObject3.GetComponent<RectTransform>();
		if (rectTransform == null)
		{
			rectTransform = gameObject3.AddComponent<RectTransform>();
		}
		rectTransform.anchorMin = new Vector2(0f, 0.5f);
		rectTransform.anchorMax = new Vector2(1f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.sizeDelta = new Vector2(0f, 36f);
		LayoutElement obj14 = gameObject3.GetComponent<LayoutElement>() ?? gameObject3.AddComponent<LayoutElement>();
		obj14.preferredHeight = 36f;
		obj14.minHeight = 36f;
		obj14.flexibleHeight = 0f;
		GameObject offGO = null;
		GameObject onGO = null;
		try
		{
			Transform transform = toggle.transform;
			for (int j = 0; j < transform.childCount; j++)
			{
				Transform child2 = transform.GetChild(j);
				if (!(child2 == null))
				{
					switch (child2.name)
					{
					case "off":
					case "Off":
						offGO = child2.gameObject;
						break;
					case "on":
					case "On":
						onGO = child2.gameObject;
						break;
					}
				}
			}
		}
		catch
		{
		}
		Handle handle = new Handle
		{
			Root = gameObject3,
			Row = rectTransform,
			ToggleControl = toggle,
			Label = tMP_Text,
			Value = initial,
			OnChanged = onChanged,
			OffGO = offGO,
			OnGO = onGO
		};
		try
		{
			toggle.onValueChanged.RemoveAllListeners();
		}
		catch
		{
		}
		try
		{
			toggle.SetIsOnWithoutNotify(initial);
		}
		catch
		{
			try
			{
				toggle.isOn = initial;
			}
			catch
			{
			}
		}
		ApplyVisualState(handle);
		try
		{
			((UnityEvent<bool>)toggle.onValueChanged).AddListener((System.Action<bool>)delegate(bool v)
			{
				if (!handle.Initialized)
				{
					return;
				}
				handle.Value = v;
				ApplyVisualState(handle);
				try
				{
					handle.OnChanged?.Invoke(v);
				}
				catch (System.Exception t2)
				{
					CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
					if (instance != null)
					{
						ManualLogSource log = instance.Log;
						if (log != null)
						{
							bool isEnabled2 = false;
							BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler2 = new BepInExErrorLogInterpolatedStringHandler(22, 1, out isEnabled2);
							if (isEnabled2)
							{
								bepInExErrorLogInterpolatedStringHandler2.AppendLiteral("[SystemToggle change] ");
								bepInExErrorLogInterpolatedStringHandler2.AppendFormatted(t2);
							}
							log.LogError(bepInExErrorLogInterpolatedStringHandler2);
						}
					}
				}
			});
		}
		catch (System.Exception ex5)
		{
			ManualLogSource manualLogSource8 = manualLogSource;
			if (manualLogSource8 != null)
			{
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler6 = new BepInExWarningLogInterpolatedStringHandler(36, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler6.AppendLiteral("[SystemToggle] hook onValueChanged: ");
					bepInExWarningLogInterpolatedStringHandler6.AppendFormatted(ex5.Message);
				}
				manualLogSource8.LogWarning(bepInExWarningLogInterpolatedStringHandler6);
			}
		}
		handle.Initialized = true;
		return handle;
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

	public static void SetSilently(Handle h, bool value)
	{
		if (h == null || h.ToggleControl == null)
		{
			return;
		}
		System.Action<bool> onChanged = h.OnChanged;
		h.OnChanged = null;
		try
		{
			h.ToggleControl.SetIsOnWithoutNotify(value);
		}
		catch
		{
			try
			{
				h.ToggleControl.isOn = value;
			}
			catch
			{
			}
		}
		h.Value = value;
		ApplyVisualState(h);
		h.OnChanged = onChanged;
	}

	private static void ApplyVisualState(Handle h)
	{
		if (h == null)
		{
			return;
		}
		try
		{
			if (h.OffGO != null)
			{
				h.OffGO.SetActive(!h.Value);
			}
		}
		catch
		{
		}
		try
		{
			if (h.OnGO != null)
			{
				h.OnGO.SetActive(h.Value);
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
			Il2CppReferenceArray<UnityEngine.Object> il2CppReferenceArray = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TweakUIToggle>());
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
				TweakUIToggle tweakUIToggle = @object.TryCast<TweakUIToggle>();
				if (!(tweakUIToggle == null) && !(tweakUIToggle.gameObject == null))
				{
					if (gameObject == null)
					{
						gameObject = tweakUIToggle.gameObject;
					}
					if (tweakUIToggle.transform.parent != null)
					{
						_cachedTemplate = tweakUIToggle.gameObject;
						_cachedTemplateSceneHandle = handle;
						return _cachedTemplate;
					}
				}
			}
			_cachedTemplate = gameObject;
			_cachedTemplateSceneHandle = handle;
			return gameObject;
		}
		catch (System.Exception ex)
		{
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(29, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[SystemToggle] FindTemplate: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
			return null;
		}
	}
}
