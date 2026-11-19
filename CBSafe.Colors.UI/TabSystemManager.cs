using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.Infrastructure;
using CBSafe.Colors.Patches;
using CBSafe.Core;
using CBSafe.Core.UI;
using CBSafe.Crosshair.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Rust.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Colors.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
[HydraNoObfuscate]
internal static class TabSystemManager
{
	private static bool _built;

	private static Transform _originalPanel;

	private static GameObject _customOverlay;

	private static bool _contentBuilt;

	private static int _contentBuildStep;

	private static Transform _pendingOverlay;

	private static Transform _pendingTabBar;

	private static GameObject _pendingTabButton;

	private static readonly string[] TabKeywords = new string[14]
	{
		"РќРђРЎРўР\u00a0РћР™РљР\u0098", "Р\u0098РќРўР•Р\u00a0Р¤Р•Р™РЎ", "РђРЈР”Р\u0098Рћ", "РЈРџР\u00a0РђР’Р›Р•РќР\u0098Р•", "Р\u00adРљР\u00a0РђРќ", "Р“Р\u00a0РђР¤Р\u0098РљРђ", "Р\u00adР¤Р¤Р•РљРўР«", "GRAPHICS", "AUDIO", "CONTROLS",
		"DISPLAY", "EFFECTS", "INTERFACE", "SCREEN"
	};

	internal static bool IsContentBuilt => _contentBuilt;

	internal static bool IsTabShellReady
	{
		get
		{
			if (_built)
			{
				return _pendingOverlay != null;
			}
			return false;
		}
	}

	public static void HideCustomOverlay()
	{
		try
		{
			if (_customOverlay != null && _customOverlay.activeSelf)
			{
				try
				{
					SystemDropdownFactory.CloseAllOpenPanels();
				}
				catch
				{
				}
				_customOverlay.SetActive(value: false);
				SetCustomTabSelected(selected: false);
				CustomTabSelection.Apply(_pendingTabBar, null);
				try
				{
					SharedConfig.FlushNow();
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

	internal static void PrepareNativeTabSwitch()
	{
		try
		{
			try
			{
				SystemDropdownFactory.CloseAllOpenPanels();
			}
			catch
			{
			}
			if (_customOverlay != null)
			{
				_customOverlay.SetActive(value: false);
			}
			SetCustomTabSelected(selected: false);
			CustomTabSelection.Apply(_pendingTabBar, null);
			if (_originalPanel != null)
			{
				_originalPanel.gameObject.SetActive(value: true);
			}
		}
		catch
		{
		}
	}

	public static void TryBuild(TweakUIToggle toggle)
	{
		if (toggle == null)
		{
			return;
		}
		if (_built && (_customOverlay == null || !_customOverlay))
		{
			_built = false;
			_contentBuilt = false;
			_contentBuildStep = 0;
			_customOverlay = null;
			_originalPanel = null;
			_pendingOverlay = null;
			_pendingTabBar = null;
		}
		if (_customOverlay != null && _customOverlay.activeSelf)
		{
			_customOverlay.SetActive(value: false);
		}
		if (_built)
		{
			return;
		}
		ManualLogSource manualLogSource = CBSafeColorsPlugin.Instance?.Log;
		if (PluginConfig.IsDebugEnabled)
		{
			manualLogSource?.LogInfo("[TabSystemManager] TryBuild start");
		}
		Transform transform = toggle.transform;
		int num = 0;
		while (num < 10 && transform != null)
		{
			Transform transform2 = FindTabBar(transform);
			if (!(transform2 == null))
			{
				Transform transform3 = FindContentRoot(transform, transform2);
				if (!(transform3 == null))
				{
					Transform transform4 = FindPanel(transform3);
					if (!(transform4 == null))
					{
						GameObject gameObject = FindTemplateTab(transform2);
						if (!(gameObject == null))
						{
							_built = true;
							Build(gameObject, transform2, transform4, transform3);
							return;
						}
					}
				}
			}
			num++;
			transform = transform.parent;
		}
		manualLogSource?.LogWarning("[TabSystemManager] required UI elements not found this pass");
	}

	private static Transform FindTabBar(Transform root)
	{
		for (int i = 0; i < root.childCount; i++)
		{
			Transform child = root.GetChild(i);
			if (CountTabMatches(child) >= 3)
			{
				return child;
			}
		}
		return null;
	}

	private static int CountTabMatches(Transform container)
	{
		int num = 0;
		for (int i = 0; i < container.childCount; i++)
		{
			TMP_Text componentInChildren = container.GetChild(i).GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (componentInChildren == null)
			{
				continue;
			}
			string text = componentInChildren.text.ToUpperInvariant().Trim();
			string[] tabKeywords = TabKeywords;
			foreach (string text2 in tabKeywords)
			{
				if (text == text2)
				{
					num++;
					break;
				}
			}
		}
		return num;
	}

	private static Transform FindContentRoot(Transform root, Transform tabBar)
	{
		Transform parent = tabBar.parent;
		if (parent == null)
		{
			return null;
		}
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			if (!(child == tabBar) && child.GetComponentInChildren<TweakUIBase>(includeInactive: true) != null)
			{
				return child;
			}
		}
		return null;
	}

	private static Transform FindPanel(Transform contentRoot)
	{
		for (int i = 0; i < contentRoot.childCount; i++)
		{
			Transform child = contentRoot.GetChild(i);
			if (child.GetComponent<VerticalLayoutGroup>() != null)
			{
				return child;
			}
		}
		return null;
	}

	private static GameObject FindTemplateTab(Transform tabBar)
	{
		for (int i = 0; i < tabBar.childCount; i++)
		{
			Transform child = tabBar.GetChild(i);
			if (!child.gameObject.activeSelf)
			{
				continue;
			}
			TMP_Text componentInChildren = child.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (componentInChildren == null)
			{
				continue;
			}
			string text = componentInChildren.text.ToUpperInvariant().Trim();
			string[] tabKeywords = TabKeywords;
			foreach (string text2 in tabKeywords)
			{
				if (text == text2)
				{
					return child.gameObject;
				}
			}
		}
		return null;
	}

	private static void Build(GameObject templateTab, Transform tabBar, Transform panel, Transform panelsRoot)
	{
		ManualLogSource manualLogSource = CBSafeColorsPlugin.Instance?.Log;
		_originalPanel = panel;
		try
		{
			BuildTab(templateTab, tabBar, panel, panelsRoot);
			if (PluginConfig.IsDebugEnabled)
			{
				manualLogSource?.LogInfo("[TabSystemManager] tab built ok");
			}
		}
		catch (Exception t)
		{
			ManualLogSource manualLogSource2 = manualLogSource;
			if (manualLogSource2 != null)
			{
				bool isEnabled = false;
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(32, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[TabSystemManager] Build error: ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				manualLogSource2.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
			_built = false;
		}
	}

	private static void BuildTab(GameObject templateTab, Transform tabBar, Transform panel, Transform panelsRoot)
	{
		ManualLogSource log = CBSafeColorsPlugin.Instance?.Log;
		GameObject gameObject = UnityEngine.Object.Instantiate(templateTab, tabBar);
		gameObject.name = LocStrings.Name_Color_TabButton;
		_pendingTabButton = gameObject;
		TabLabelHelper.ApplyFixedTabLabel(gameObject, LocStrings.ColorTab_Label);
		foreach (TweakUIBase componentsInChild in gameObject.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
		{
			if (componentsInChild != null)
			{
				UnityEngine.Object.Destroy(componentsInChild);
			}
		}
		Button obj = gameObject.GetComponent<Button>() ?? gameObject.GetComponentInChildren<Button>(includeInactive: true) ?? gameObject.AddComponent<Button>();
		obj.onClick.RemoveAllListeners();
		obj.interactable = true;
		GameObject gameObject2 = new GameObject(LocStrings.Name_Color_Overlay);
		gameObject2.transform.SetParent(panelsRoot, worldPositionStays: false);
		RectTransform rectTransform = gameObject2.AddComponent<RectTransform>();
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		rectTransform.localScale = Vector3.one;
		CustomOverlayPresenter.Configure(gameObject2, panel);
		Image component = panel.GetComponent<Image>();
		if (component != null)
		{
			Image image = gameObject2.AddComponent<Image>();
			image.color = component.color;
			image.sprite = component.sprite;
			image.type = component.type;
		}
		VerticalLayoutGroup component2 = panel.GetComponent<VerticalLayoutGroup>();
		VerticalLayoutGroup verticalLayoutGroup = gameObject2.AddComponent<VerticalLayoutGroup>();
		RectOffset rectOffset = new RectOffset();
		if (component2 != null)
		{
			rectOffset.left = component2.padding.left;
			rectOffset.right = component2.padding.right;
			rectOffset.top = component2.padding.top;
			rectOffset.bottom = component2.padding.bottom;
			verticalLayoutGroup.spacing = component2.spacing;
			verticalLayoutGroup.childAlignment = component2.childAlignment;
			verticalLayoutGroup.childControlWidth = component2.childControlWidth;
			verticalLayoutGroup.childControlHeight = component2.childControlHeight;
			verticalLayoutGroup.childForceExpandWidth = component2.childForceExpandWidth;
			verticalLayoutGroup.childForceExpandHeight = false;
			verticalLayoutGroup.childScaleWidth = component2.childScaleWidth;
			verticalLayoutGroup.childScaleHeight = component2.childScaleHeight;
		}
		else
		{
			verticalLayoutGroup.childAlignment = TextAnchor.UpperLeft;
			verticalLayoutGroup.childForceExpandWidth = true;
			verticalLayoutGroup.childForceExpandHeight = false;
			verticalLayoutGroup.childControlWidth = true;
			verticalLayoutGroup.childControlHeight = true;
		}
		verticalLayoutGroup.spacing = 6f;
		rectOffset.top += 6;
		verticalLayoutGroup.padding = rectOffset;
		CreateHeader(gameObject, gameObject2.transform);
		gameObject2.SetActive(value: false);
		_customOverlay = gameObject2;
		_pendingOverlay = gameObject2.transform;
		_pendingTabBar = tabBar;
		_contentBuilt = false;
		obj.onClick.AddListener((Action)delegate
		{
			try
			{
				try
				{
					CrosshairTabManager.HideCustomOverlay();
				}
				catch
				{
				}
				if (!(_originalPanel == null) && !(_customOverlay == null))
				{
					_originalPanel.gameObject.SetActive(value: false);
					CustomOverlayPresenter.Show(_customOverlay);
					SetCustomTabSelected(selected: true);
					CustomTabSelection.Apply(_pendingTabBar, LocStrings.Name_Color_TabButton);
					EnsureContentBuilt();
					CustomOverlayPresenter.Show(_customOverlay);
				}
			}
			catch (Exception t)
			{
				ManualLogSource manualLogSource = log;
				if (manualLogSource != null)
				{
					bool isEnabled = false;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(30, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[TabSystemManager] tab click: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
					}
					manualLogSource.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
			}
		});
		Transform transform = tabBar.Find(LocStrings.Name_Prochee);
		if (transform != null)
		{
			gameObject.transform.SetSiblingIndex(transform.GetSiblingIndex());
		}
		else
		{
			gameObject.transform.SetAsLastSibling();
		}
		if (PluginConfig.IsDebugEnabled)
		{
			log?.LogInfo("[TabSystemManager] custom tab installed");
		}
		try
		{
			CBSafeUiWarmup.OnSettingsPanelReady();
		}
		catch
		{
		}
		try
		{
			SettingsTabPrefetcher.ArmLoadScreenBurst();
		}
		catch
		{
		}
	}

	internal static void SetCustomTabSelected(bool selected)
	{
		try
		{
			if (!(_pendingTabButton == null))
			{
				Button button = _pendingTabButton.GetComponent<Button>() ?? _pendingTabButton.GetComponentInChildren<Button>(includeInactive: true);
				if (button != null)
				{
					button.interactable = !selected;
				}
			}
		}
		catch
		{
		}
	}

	internal static void TryBuildFromScene()
	{
		if (IsTabShellReady)
		{
			return;
		}
		try
		{
			GameObject gameObject = GameObject.Find(LocStrings.Path_MenuUiRoot);
			if (gameObject == null)
			{
				gameObject = GameObject.Find(LocStrings.Path_OptionsRoot);
			}
			if (gameObject == null)
			{
				return;
			}
			Il2CppArrayBase<TweakUIToggle> componentsInChildren = gameObject.GetComponentsInChildren<TweakUIToggle>(includeInactive: true);
			int num = componentsInChildren?.Length ?? 0;
			for (int i = 0; i < num; i++)
			{
				TweakUIToggle tweakUIToggle = componentsInChildren[i];
				if (!(tweakUIToggle == null))
				{
					TryBuild(tweakUIToggle);
					if (IsTabShellReady)
					{
						break;
					}
				}
			}
		}
		catch
		{
		}
	}

	internal static bool TryBuildNextContentStep()
	{
		if (_contentBuilt)
		{
			return false;
		}
		if (_pendingOverlay == null)
		{
			return false;
		}
		ManualLogSource manualLogSource = CBSafeColorsPlugin.Instance?.Log;
		bool isEnabled = false;
		try
		{
			if (_contentBuildStep == 0)
			{
				SystemDropdownFactory.ClearHandles();
				_contentBuildStep = 1;
				return true;
			}
			switch (_contentBuildStep)
			{
			case 1:
				_contentBuildStep = 2;
				return true;
			case 2:
				CreateReticleColorDropdown(manualLogSource);
				_contentBuildStep = 3;
				return true;
			case 3:
				CreateCrosshairColorDropdown(manualLogSource);
				_contentBuildStep = 4;
				return true;
			case 4:
				CreateHpBarColorDropdown(manualLogSource);
				_contentBuildStep = 5;
				return true;
			case 5:
				CreateWaterBarColorDropdown(manualLogSource);
				_contentBuildStep = 6;
				return true;
			case 6:
				CreateFoodBarColorDropdown(manualLogSource);
				_contentBuildStep = 7;
				return true;
			case 7:
				CreatePlayerNameColorDropdown(manualLogSource);
				_contentBuildStep = 8;
				return true;
			case 8:
				CreateTeamNameColorDropdown(manualLogSource);
				_contentBuildStep = 9;
				return true;
			case 9:
				CreateAllyNameColorDropdown(manualLogSource);
				_contentBuildStep = 10;
				return true;
			case 10:
				CreateEnemyNameColorDropdown(manualLogSource);
				_contentBuildStep = 11;
				return true;
			case 11:
				CreateBloodColorDropdown(manualLogSource);
				_contentBuildStep = 12;
				return true;
			case 12:
				CreateGatherCircleColorDropdown(manualLogSource);
				_contentBuildStep = 13;
				return true;
			case 13:
				ApplySavedConfig();
				try
				{
					EnsureColorSectionHeader();
				}
				catch
				{
				}
				try
				{
					SystemDropdownFactory.RegisterModalRoot(_pendingTabBar);
					SystemDropdownFactory.RegisterModalRoot(_pendingOverlay);
				}
				catch (Exception ex)
				{
					ManualLogSource manualLogSource2 = manualLogSource;
					if (manualLogSource2 != null)
					{
						BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(26, 1, out isEnabled);
						if (isEnabled)
						{
							bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TabSystemManager] modal: ");
							bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
						}
						manualLogSource2.LogWarning(bepInExWarningLogInterpolatedStringHandler);
					}
				}
				_contentBuilt = true;
				_contentBuildStep = 0;
				return false;
			}
		}
		catch (Exception t)
		{
			ManualLogSource manualLogSource3 = manualLogSource;
			if (manualLogSource3 != null)
			{
				BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(46, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[TabSystemManager] TryBuildNextContentStep(");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(_contentBuildStep);
					bepInExErrorLogInterpolatedStringHandler.AppendLiteral("): ");
					bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
				}
				manualLogSource3.LogError(bepInExErrorLogInterpolatedStringHandler);
			}
		}
		return false;
	}

	private static void EnsureContentBuilt()
	{
		_ = _contentBuilt;
		try
		{
			CBSafeSettingsUiLoadBootstrap.CompleteBeforeTabOpen();
		}
		catch
		{
		}
		int num = 0;
		while (!_contentBuilt && num++ < 32 && TryBuildNextContentStep())
		{
		}
	}

	private static void CreateReticleColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_ReticleColor, PluginConfig.ReticleColor, PluginConfig.ReticleColorEnabled, delegate
		{
			CBSafeColorsPlugin.Instance?.HoloReticleApplier?.ForceReapplyNow();
		});
	}

	private static void CreateCrosshairColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_CrosshairColor, PluginConfig.CrosshairColor, PluginConfig.CrosshairColorEnabled, delegate
		{
			CBSafeColorsPlugin.Instance?.TreeCrosshairApplier?.ForceReapplyNow();
		});
	}

	private static void CreateHpBarColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_HpBarColor, PluginConfig.HpBarColor, PluginConfig.HpColorEnabled, delegate
		{
			CBSafeColorsPlugin.Instance?.HudBarApplier?.ForceReapplyNow();
		});
	}

	private static void CreateWaterBarColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_WaterBarColor, PluginConfig.WaterBarColor, PluginConfig.WaterColorEnabled, delegate
		{
			CBSafeColorsPlugin.Instance?.HudBarApplier?.ForceReapplyNow();
		});
	}

	private static void CreateFoodBarColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_FoodBarColor, PluginConfig.FoodBarColor, PluginConfig.FoodColorEnabled, delegate
		{
			CBSafeColorsPlugin.Instance?.HudBarApplier?.ForceReapplyNow();
		});
	}

	private static void CreatePlayerNameColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_PlayerNameColor, PluginConfig.PlayerNameColor, PluginConfig.PlayerNameColorEnabled, delegate
		{
			TeamPlayerNameColorApplier.QuickApplyFromConfig();
		});
	}

	private static void CreateTeamNameColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_TeamNameColor, PluginConfig.TeamNameColor, PluginConfig.TeamNameColorEnabled, delegate
		{
			TeamPlayerNameColorApplier.QuickApplyFromConfig();
		});
	}

	private static void CreateAllyNameColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_AllyNameColor, PluginConfig.AllyNameColor, PluginConfig.AllyNameColorEnabled, delegate
		{
			TeamPlayerNameColorApplier.QuickApplyFromConfig();
		});
	}

	private static void CreateEnemyNameColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_EnemyNameColor, PluginConfig.EnemyNameColor, PluginConfig.EnemyNameColorEnabled, delegate
		{
			TeamPlayerNameColorApplier.QuickApplyFromConfig();
		});
	}

	private static void CreateBloodColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_BloodColor, PluginConfig.BloodColor, PluginConfig.BloodColorEnabled, delegate
		{
			BloodColorApplier.UpdateAllBlood();
		});
	}

	private static void CreateGatherCircleColorDropdown(ManualLogSource log)
	{
		ColorPickerButtonFactory.Create(_pendingOverlay, LocStrings.Key_GatherCircleColor, PluginConfig.GatherCircleColor, PluginConfig.GatherCircleColorEnabled, delegate
		{
		});
	}

	private static void CreateHeader(GameObject tabButton, Transform overlay)
	{
		ManualLogSource manualLogSource = CBSafeColorsPlugin.Instance?.Log;
		TMP_Text tMP_Text = FindSourceHeaderText(overlay);
		if (tMP_Text == null)
		{
			try
			{
				tMP_Text = tabButton.GetComponentInChildren<TMP_Text>(includeInactive: true);
			}
			catch
			{
			}
		}
		if (tMP_Text == null)
		{
			manualLogSource?.LogWarning("[TabSystemManager] no source TMP_Text for header");
			return;
		}
		Transform transform = overlay.Find(LocStrings.Name_Color_SectionHeader);
		if (transform != null)
		{
			UnityEngine.Object.Destroy(transform.gameObject);
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(tMP_Text.gameObject, overlay);
		gameObject.name = LocStrings.Name_Color_SectionHeader;
		foreach (TweakUIBase componentsInChild in gameObject.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
		{
			if (componentsInChild != null)
			{
				UnityEngine.Object.Destroy(componentsInChild);
			}
		}
		TMP_Text component = gameObject.GetComponent<TMP_Text>();
		if (component == null)
		{
			manualLogSource?.LogWarning("[TabSystemManager] cloned header has no TMP_Text");
			return;
		}
		string colorSection_Header = LocStrings.ColorSection_Header;
		try
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
					rustText.English = colorSection_Header;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		component.text = colorSection_Header;
		component.raycastTarget = false;
		try
		{
			component.ForceMeshUpdate();
		}
		catch
		{
		}
		gameObject.transform.SetAsFirstSibling();
		TabLabelHelper.ApplyFixedTabLabel(component, colorSection_Header);
	}

	private static TMP_Text FindSourceHeaderText(Transform overlay)
	{
		if (_originalPanel == null)
		{
			return null;
		}
		TMP_Text result = null;
		float num = 0f;
		foreach (TMP_Text componentsInChild in _originalPanel.GetComponentsInChildren<TMP_Text>(includeInactive: true))
		{
			if (!(componentsInChild == null) && componentsInChild.fontSize > num)
			{
				num = componentsInChild.fontSize;
				result = componentsInChild;
			}
		}
		return result;
	}

	private static void EnsureColorSectionHeader()
	{
		if (_pendingOverlay == null)
		{
			return;
		}
		string colorSection_Header = LocStrings.ColorSection_Header;
		Transform transform = _pendingOverlay.Find(LocStrings.Name_Color_SectionHeader);
		if (transform != null)
		{
			transform.SetAsFirstSibling();
			TMP_Text componentInChildren = transform.GetComponentInChildren<TMP_Text>(includeInactive: true);
			if (componentInChildren != null)
			{
				TabLabelHelper.ApplyFixedTabLabel(componentInChildren, colorSection_Header);
			}
		}
		else if (_pendingTabButton != null)
		{
			CreateHeader(_pendingTabButton, _pendingOverlay);
		}
	}

	private static void ApplySavedConfig()
	{
		try
		{
			ApplyRuntimeColorsFromSavedConfig();
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(37, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TabSystemManager] ApplySavedConfig: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	private static void ApplyRuntimeColorsFromSavedConfig()
	{
		try
		{
			HoloReticleColorApplier.ApplySavedConfigNow();
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(54, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TabSystemManager] ApplyRuntimeColorsFromSavedConfig: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}
}
