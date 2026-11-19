using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.UI;
using CBSafe.Core;
using CBSafe.Core.UI;
using CBSafe.Crosshair.Infrastructure;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Crosshair.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
[HydraNoObfuscate]
internal static class CrosshairTabManager
{
	private sealed class CC__DisplayClass12_0
	{
		public ManualLogSource log;

		public Action CC_cached_2;
	}

	private static bool _built;

	private static bool _entered;

	private static Transform _originalPanel;

	private static GameObject _customOverlay;

	private static GameObject _pendingTabButton;

	private static CrosshairSectionBuilder.SectionHandles _handles;

	private static bool _contentBuilt;

	private static int _contentBuildStep;

	private static Transform _pendingTabBar;

	private static Transform _pendingContentParent;

	private static TMP_Text _pendingSliderFont;

	private static TMP_Text _pendingHeaderFont;

	private static readonly string[] TabKeywords = new string[16]
	{
		"НАСТРОЙКИ", "ИНТЕРФЕЙС", "АУДИО", "УПРАВЛЕНИЕ", "ЭКРАН", "ГРАФИКА", "ЭФФЕКТЫ", "ИГРОВОЙ ПРОЦЕСС", "GAMEPLAY", "GRAPHICS",
		"AUDIO", "CONTROLS", "DISPLAY", "EFFECTS", "INTERFACE", "SCREEN"
	};

	internal static bool IsContentBuilt => _contentBuilt;

	internal static bool IsTabShellReady
	{
		get
		{
			if (_built && _customOverlay != null)
			{
				return _pendingContentParent != null;
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
				try
				{
					CrosshairConfigStore.ForceReleaseInputGuard();
				}
				catch
				{
				}
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
				SetCustomTabSelected(selected: false);
				CustomTabSelection.Apply(_pendingTabBar, null);
			}
			if (_originalPanel != null)
			{
				_originalPanel.gameObject.SetActive(value: true);
			}
			try
			{
				CrosshairConfigStore.ForceReleaseInputGuard();
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	public static void TryBuild(TweakUIToggle toggle)
	{
		if (toggle == null || _entered)
		{
			return;
		}
		try
		{
			if (_customOverlay != null && toggle.transform != null && toggle.transform.IsChildOf(_customOverlay.transform))
			{
				return;
			}
		}
		catch
		{
		}
		if (_built && (_customOverlay == null || !_customOverlay))
		{
			_built = false;
			_contentBuilt = false;
			_contentBuildStep = 0;
			_customOverlay = null;
			_originalPanel = null;
			_handles = null;
			_pendingTabBar = null;
			_pendingContentParent = null;
			_pendingSliderFont = null;
			_pendingHeaderFont = null;
			try
			{
				CBSafeUiWarmup.ResetForSettingsSession();
			}
			catch
			{
			}
			try
			{
				CrosshairContentPrefetcher.Cancel();
			}
			catch
			{
			}
		}
		if (_customOverlay != null && _customOverlay.activeSelf)
		{
			_customOverlay.SetActive(value: false);
			SetCustomTabSelected(selected: false);
			CustomTabSelection.Apply(_pendingTabBar, null);
		}
		if (_built)
		{
			return;
		}
		ManualLogSource manualLogSource = CBSafeCrosshairPlugin.Instance?.Log;
		Transform transform = toggle.transform;
		int num = 0;
		bool isEnabled = false;
		while (num < 12 && transform != null)
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
							_entered = true;
							try
							{
								BuildTab(gameObject, transform2, transform4, transform3);
								return;
							}
							catch (Exception t)
							{
								ManualLogSource manualLogSource2 = manualLogSource;
								if (manualLogSource2 != null)
								{
									BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(32, 1, out isEnabled);
									if (isEnabled)
									{
										bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[CrosshairTabManager] BuildTab: ");
										bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
									}
									manualLogSource2.LogError(bepInExErrorLogInterpolatedStringHandler);
								}
								_built = false;
								return;
							}
							finally
							{
								_entered = false;
							}
						}
					}
				}
			}
			num++;
			transform = transform.parent;
		}
		manualLogSource?.LogWarning("[CrosshairTabManager] required UI elements not found this pass");
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
			string text = (componentInChildren.text ?? string.Empty).ToUpperInvariant().Trim();
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
			if (!(child == null) && child.GetComponent<VerticalLayoutGroup>() != null)
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
			string text = (componentInChildren.text ?? string.Empty).ToUpperInvariant().Trim();
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

	private static void BuildTab(GameObject templateTab, Transform tabBar, Transform panel, Transform panelsRoot)
	{
		CC__DisplayClass12_0 __locals1 = new CC__DisplayClass12_0();
		__locals1.log = CBSafeCrosshairPlugin.Instance?.Log;
		_originalPanel = panel;
		RemoveNativeCrosshairToggle(panel, __locals1.log);
		GameObject gameObject = UnityEngine.Object.Instantiate(templateTab, tabBar);
		gameObject.name = LocStrings.Name_Crosshair_TabButton;
		_pendingTabButton = gameObject;
		TabLabelHelper.ApplyFixedTabLabel(gameObject, LocStrings.Crosshair_Tab);
		foreach (TweakUIBase componentsInChild in gameObject.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
		{
			if (componentsInChild != null)
			{
				UnityEngine.Object.Destroy(componentsInChild);
			}
		}
		Button button;
		if ((button = gameObject.GetComponent<Button>()) == null)
		{
			button = gameObject.GetComponentInChildren<Button>(includeInactive: true) ?? gameObject.AddComponent<Button>();
		}
		Button button2 = button;
		button2.onClick.RemoveAllListeners();
		button2.interactable = true;
		GameObject gameObject2 = new GameObject("Crosshair_Overlay");
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
		RectOffset rectOffset = new RectOffset();
		if (component2 != null)
		{
			rectOffset.left = component2.padding.left;
			rectOffset.right = component2.padding.right;
			rectOffset.top = component2.padding.top + 6;
			rectOffset.bottom = component2.padding.bottom;
		}
		else
		{
			rectOffset.left = 12;
			rectOffset.right = 12;
			rectOffset.top = 12;
			rectOffset.bottom = 12;
		}
		GameObject gameObject3 = new GameObject("Viewport");
		gameObject3.transform.SetParent(gameObject2.transform, worldPositionStays: false);
		RectTransform rectTransform2 = gameObject3.AddComponent<RectTransform>();
		rectTransform2.anchorMin = Vector2.zero;
		rectTransform2.anchorMax = Vector2.one;
		rectTransform2.pivot = new Vector2(0f, 1f);
		rectTransform2.offsetMin = new Vector2(rectOffset.left, rectOffset.bottom);
		rectTransform2.offsetMax = new Vector2(0f - (float)rectOffset.right, 0f - (float)rectOffset.top);
		Image image2 = gameObject3.AddComponent<Image>();
		image2.color = new Color(1f, 1f, 1f, 0.005f);
		image2.raycastTarget = true;
		gameObject3.AddComponent<Mask>().showMaskGraphic = false;
		GameObject gameObject4 = new GameObject("Content");
		gameObject4.transform.SetParent(gameObject3.transform, worldPositionStays: false);
		RectTransform rectTransform3 = gameObject4.AddComponent<RectTransform>();
		rectTransform3.anchorMin = new Vector2(0f, 1f);
		rectTransform3.anchorMax = new Vector2(1f, 1f);
		rectTransform3.pivot = new Vector2(0.5f, 1f);
		rectTransform3.anchoredPosition = Vector2.zero;
		rectTransform3.sizeDelta = new Vector2(0f, 0f);
		VerticalLayoutGroup verticalLayoutGroup = gameObject4.AddComponent<VerticalLayoutGroup>();
		if (component2 != null)
		{
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
		verticalLayoutGroup.spacing = 8f;
		verticalLayoutGroup.padding = new RectOffset();
		ContentSizeFitter contentSizeFitter = gameObject4.AddComponent<ContentSizeFitter>();
		contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		ScrollRect scrollRect = gameObject2.AddComponent<ScrollRect>();
		scrollRect.content = rectTransform3;
		scrollRect.viewport = rectTransform2;
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.scrollSensitivity = 28f;
		scrollRect.inertia = false;
		SystemDropdownFactory.ClearHandles();
		TMP_Text tMP_Text = null;
		TMP_Text tMP_Text2 = null;
		foreach (TMP_Text componentsInChild2 in panel.GetComponentsInChildren<TMP_Text>(includeInactive: true))
		{
			if (!(componentsInChild2 == null))
			{
				if (tMP_Text2 == null)
				{
					tMP_Text2 = componentsInChild2;
				}
				bool flag = false;
				try
				{
					flag = componentsInChild2.font != null;
				}
				catch
				{
				}
				if (flag)
				{
					tMP_Text = componentsInChild2;
					break;
				}
			}
		}
		if (tMP_Text == null)
		{
			tMP_Text = tMP_Text2;
		}
		TMP_Text pendingHeaderFont = FindBiggestText(panel);
		_contentBuilt = false;
		_pendingTabBar = tabBar;
		_pendingContentParent = gameObject4.transform;
		_pendingSliderFont = tMP_Text;
		_pendingHeaderFont = pendingHeaderFont;
		button2.onClick.AddListener((Action)delegate
		{
			try
			{
				try
				{
					TabSystemManager.HideCustomOverlay();
				}
				catch
				{
				}
				EnsureContentBuilt();
				if (!(_originalPanel == null) && !(_customOverlay == null))
				{
					_originalPanel.gameObject.SetActive(value: false);
					CustomOverlayPresenter.Show(_customOverlay);
					SetCustomTabSelected(selected: true);
					CustomTabSelection.Apply(_pendingTabBar, LocStrings.Name_Crosshair_TabButton);
					if (_handles != null)
					{
						CrosshairSectionBuilder.SyncControlsFromConfig(_handles);
					}
					CustomOverlayPresenter.Show(_customOverlay);
				}
			}
			catch (Exception t)
			{
				ManualLogSource log = __locals1.log;
				if (log != null)
				{
					bool isEnabled = false;
					BepInExErrorLogInterpolatedStringHandler bepInExErrorLogInterpolatedStringHandler = new BepInExErrorLogInterpolatedStringHandler(33, 1, out isEnabled);
					if (isEnabled)
					{
						bepInExErrorLogInterpolatedStringHandler.AppendLiteral("[CrosshairTabManager] tab click: ");
						bepInExErrorLogInterpolatedStringHandler.AppendFormatted(t);
					}
					log.LogError(bepInExErrorLogInterpolatedStringHandler);
				}
			}
		});
		_customOverlay = gameObject2;
		gameObject2.SetActive(value: false);
		Transform transform = tabBar.Find(LocStrings.Name_Color_TabButton);
		if (transform == null)
		{
			transform = tabBar.Find(LocStrings.Name_Prochee);
		}
		if (transform != null)
		{
			gameObject.transform.SetSiblingIndex(transform.GetSiblingIndex());
		}
		else
		{
			gameObject.transform.SetAsLastSibling();
		}
		__locals1.log?.LogInfo("[CrosshairTabManager] custom CROSSHAIR tab installed");
		try
		{
			CrosshairContentPrefetcher.Request();
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
		if (_pendingContentParent == null || _pendingTabBar == null || _customOverlay == null)
		{
			return false;
		}
		if (_contentBuildStep == 0)
		{
			if (_handles == null)
			{
				_handles = new CrosshairSectionBuilder.SectionHandles();
			}
			try
			{
				CrosshairConfigStore.BeginUiInit();
			}
			catch
			{
			}
		}
		if (CrosshairSectionBuilder.BuildIncrementalStep(_handles, ref _contentBuildStep, _pendingContentParent, _pendingSliderFont, _pendingHeaderFont))
		{
			return true;
		}
		_contentBuilt = true;
		try
		{
			CrosshairConfigStore.EndUiInitAndBlock();
		}
		catch
		{
		}
		FinalizeContentBuilt();
		return false;
	}

	private static void EnsureContentBuilt()
	{
		if (_contentBuilt && CrosshairSectionBuilder.IsContentComplete(_handles))
		{
			try
			{
				CrosshairConfigStore.ForceReleaseUiInitGuard();
				return;
			}
			catch
			{
				return;
			}
		}
		if (!(_pendingContentParent == null) && !(_pendingTabBar == null) && !(_customOverlay == null))
		{
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
	}

	private static void FinalizeContentBuilt()
	{
		ManualLogSource manualLogSource = CBSafeCrosshairPlugin.Instance?.Log;
		try
		{
			if (_handles != null)
			{
				CrosshairSectionBuilder.EnsureResetButton(_handles, _pendingSliderFont, manualLogSource);
				CrosshairSectionBuilder.SyncControlsFromConfig(_handles);
			}
			CrosshairConfigStore.SubscribeInputUnblocked(delegate
			{
				if (_handles != null)
				{
					CrosshairSectionBuilder.SyncControlsFromConfig(_handles);
				}
			});
			if (_handles?.Root != null)
			{
				manualLogSource?.LogInfo("[CrosshairTabManager] section built in overlay");
			}
			CrosshairConfigStore.Subscribe(delegate
			{
				if (_handles != null)
				{
					PreviewRenderer preview = _handles.Preview;
					if (!(preview == null))
					{
						preview.MarkDirty();
					}
				}
			});
			try
			{
				SystemDropdownFactory.RegisterModalRoot(_pendingTabBar);
				SystemDropdownFactory.RegisterModalRoot(_customOverlay.transform);
			}
			catch (Exception ex)
			{
				manualLogSource?.LogWarning("[CrosshairTabManager] modal: " + ex.Message);
			}
		}
		catch (Exception ex2)
		{
			manualLogSource?.LogError("[CrosshairTabManager] FinalizeContentBuilt: " + ex2);
			_contentBuilt = false;
			_contentBuildStep = 0;
		}
	}

	private static TMP_Text FindBiggestText(Transform panel)
	{
		TMP_Text result = null;
		float num = 0f;
		foreach (TMP_Text componentsInChild in panel.GetComponentsInChildren<TMP_Text>(includeInactive: true))
		{
			if (!(componentsInChild == null) && componentsInChild.fontSize > num)
			{
				num = componentsInChild.fontSize;
				result = componentsInChild;
			}
		}
		return result;
	}

	private static void RemoveNativeCrosshairToggle(Transform settingsPanel, ManualLogSource log)
	{
	}

	private static string ReadLongestLabel(TweakUIToggle toggle)
	{
		string result = null;
		int num = -1;
		try
		{
			foreach (TMP_Text componentsInChild in toggle.GetComponentsInChildren<TMP_Text>(includeInactive: true))
			{
				if (!(componentsInChild == null))
				{
					string text = null;
					try
					{
						text = componentsInChild.text;
					}
					catch
					{
					}
					int num2 = text?.Length ?? 0;
					if (num2 > num)
					{
						num = num2;
						result = text;
					}
				}
			}
			return result;
		}
		catch
		{
			return result;
		}
	}
}
