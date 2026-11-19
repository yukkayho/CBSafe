using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using CBSafe.Colors.UI;
using CBSafe.Core;
using CBSafe.Core.Patches;
using CBSafe.Core.UI;
using CBSafe.Crosshair.UI;
using CBSafe.Fps;
using CBSafe.Holo;
using ConVar;
using Il2CppInterop.Runtime;
using Il2CppSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CBSafe.Patches;

internal class BoostUI
{
	private static bool _built;

	private static bool _optBuilt;

	private static bool _building;

	private static GameObject _panelRef;

	private static GameObject _optPanelRef;

	private static GameObject _tooltipObjectTemplate;

	private static readonly List<System.Action> _optSync = new List<System.Action>();

	private static CBSafe.Crosshair.UI.DropdownHandle _optShablon;

	private static CBSafe.Crosshair.UI.DropdownHandle _optTextures;

	private static readonly PresetId[] _optShablonPresets = new PresetId[3]
	{
		PresetId.Combat,
		PresetId.Potato,
		PresetId.SuperPotato
	};

	private static bool _optSyncing;

	private static CBSafe.Crosshair.UI.DropdownHandle _optGlovesSkin;

	internal static void CreateMiscTab()
	{
		if (_building)
		{
			return;
		}
		_building = true;
		try
		{
			try
			{
				EnsureOptimizationTab();
			}
			catch (System.Exception ex)
			{
				Main.Instance?.Log.LogError("[BoostUI] optimization build: " + ex);
			}
			try
			{
				EnsureProcheeTab();
			}
			catch (System.Exception ex2)
			{
				Main.Instance?.Log.LogError("[BoostUI] prochee build: " + ex2);
			}
			try
			{
				HideNativeTutorial();
			}
			catch (System.Exception ex3)
			{
				Main.Instance?.Log.LogError("[BoostUI] native tutorial cleanup: " + ex3);
			}
		}
		catch
		{
		}
		finally
		{
			_building = false;
		}
	}

	private static void EnsureOptimizationTab()
	{
		GameObject gameObject = FindUiObject(LocStrings.Path_OptionsPanel);
		if (gameObject == null)
		{
			return;
		}
		Transform transform = gameObject.transform.Find(LocStrings.Path_OptimizationChild);
		if (transform != null && transform.Find(LocStrings.Name_OptLayoutV17) != null)
		{
			_optPanelRef = transform.gameObject;
			PrepareCustomPanelVisibility(_optPanelRef);
			_optBuilt = true;
			return;
		}
		if (transform != null)
		{
			try
			{
				UnityEngine.Object.Destroy(transform.gameObject);
			}
			catch
			{
			}
		}
		GameObject gameObject2 = FindUiObject(LocStrings.Path_Categories);
		if (gameObject2 == null)
		{
			return;
		}
		Transform transform2 = gameObject2.transform.Find(LocStrings.Path_OptimizationChild);
		GameObject gameObject3 = FindUiObject(LocStrings.Path_ExperimentalTab);
		GameObject gameObject4 = FindUiObject(LocStrings.Path_ImageEffects);
		if (gameObject3 == null || gameObject4 == null)
		{
			return;
		}
		if (transform2 == null)
		{
			CreateTab(gameObject3, gameObject2.transform, LocStrings.Name_Optimization, LocStrings.Optimization_Hdr);
		}
		GameObject gameObject5 = UnityEngine.Object.Instantiate(gameObject4);
		gameObject5.name = LocStrings.Name_Optimization;
		StripPanelChrome(gameObject5);
		Transform transform3 = gameObject5.transform.Find(LocStrings.Path_PanelOptHeader);
		if (transform3 == null)
		{
			transform3 = FindLargestTitle(gameObject5.transform);
		}
		if (transform3 != null)
		{
			StripLocalizers(transform3);
			TextMeshProUGUI component = transform3.GetComponent<TextMeshProUGUI>();
			if (component != null)
			{
				component.text = LocStrings.Optimization_Hdr;
				component.m_text = LocStrings.Optimization_Hdr;
			}
		}
		Transform transform4 = FindOptToggleTemplate(gameObject5.transform);
		if (transform4 == null)
		{
			return;
		}
		foreach (string item in new List<string>
		{
			LocStrings.Path_ToggleAntialias,
			LocStrings.Path_ToggleShafts,
			LocStrings.Path_ToggleSharpen,
			LocStrings.Path_ToggleVignet,
			LocStrings.Path_ToggleMotion,
			LocStrings.Path_ToggleAo,
			LocStrings.Path_ToggleBloom,
			LocStrings.Path_ToggleLensdirt
		})
		{
			Transform transform5 = gameObject5.transform.Find(item);
			if (transform5 != null && transform5 != transform4)
			{
				transform5.gameObject.SetActive(value: false);
			}
		}
		PrepareOptPanelLayout(gameObject5);
		Transform transform6 = gameObject5.transform;
		_optSync.Clear();
		_optShablon = null;
		_optTextures = null;
		AddOptShablonDropdown(transform6, transform3);
		AddOptHeader(transform6, transform3, LocStrings.Hdr_Light);
		AddOptTextureDropdown(transform6);
		AddOptRow(transform6, transform4, A("OptPointFilter"), LocStrings.Opt_PointFilter, OptConfig.PointFilterTextures, LocStrings.Tip_Opt_PointFilter);
		AddOptRow(transform6, transform4, A("OptProbes"), LocStrings.Opt_Probes, OptConfig.DisableReflectionProbes, LocStrings.Tip_Opt_Probes);
		AddOptRow(transform6, transform4, A("OptPlanar"), LocStrings.Opt_Planar, OptConfig.DisablePlanarReflections, LocStrings.Tip_Opt_Planar);
		AddOptRow(transform6, transform4, A("OptBloom"), LocStrings.Opt_Bloom, OptConfig.DisableBloom, LocStrings.Tip_Opt_Bloom);
		AddOptRow(transform6, transform4, A("OptMotionBlur"), LocStrings.Opt_MotionBlur, OptConfig.DisableMotionBlur, LocStrings.Tip_Opt_MotionBlur);
		AddOptRow(transform6, transform4, A("OptAa"), LocStrings.Opt_Aa, OptConfig.DisableAa, LocStrings.Tip_Opt_Aa);
		AddOptRow(transform6, transform4, A("OptSharpen"), LocStrings.Opt_Sharpen, OptConfig.DisableSharpen, LocStrings.Tip_Opt_Sharpen);
		AddOptRow(transform6, transform4, A("OptSsao"), LocStrings.Opt_Ssao, OptConfig.DisableSsao, LocStrings.Tip_Opt_Ssao);
		AddOptRow(transform6, transform4, A("OptLinearFog"), LocStrings.Opt_LinearFog, OptConfig.DisableLinearFog, LocStrings.Tip_Opt_LinearFog);
		AddOptRow(transform6, transform4, A("OptExplosionBloom"), LocStrings.Opt_ExplosionBloom, OptConfig.DisableExplosionBloom, LocStrings.Tip_Opt_ExplosionBloom);
		AddOptRow(transform6, transform4, A("OptContactShadows"), LocStrings.Opt_ContactShadows, OptConfig.DisableContactShadows, LocStrings.Tip_Opt_ContactShadows);
		AddOptRow(transform6, transform4, A("OptSss"), LocStrings.Opt_Sss, OptConfig.DisableSss, LocStrings.Tip_Opt_Sss);
		AddOptRow(transform6, transform4, A("OptSunRays"), LocStrings.Opt_SunRays, OptConfig.DisableSunRays, LocStrings.Tip_Opt_SunRays);
		AddOptRow(transform6, transform4, A("OptDof"), LocStrings.Opt_Dof, OptConfig.DisableDof, LocStrings.Tip_Opt_Dof);
		AddOptRow(transform6, transform4, A("OptHeatHaze"), LocStrings.Opt_HeatHaze, OptConfig.DisableHeatHaze, LocStrings.Tip_Opt_HeatHaze);
		AddOptRow(transform6, transform4, A("OptDmgOverlay"), LocStrings.Opt_DmgOverlay, OptConfig.DisableDamageOverlay, LocStrings.Tip_Opt_DmgOverlay);
		AddOptRow(transform6, transform4, A("OptVmShadows"), LocStrings.Opt_VmShadows, OptConfig.DisableVmShadows, LocStrings.Tip_Opt_VmShadows);
		AddOptRow(transform6, transform4, A("OptCheapCam"), LocStrings.Opt_CheapCam, OptConfig.CheapCamera, LocStrings.Tip_Opt_CheapCam);
		AddOptRow(transform6, transform4, A("OptShaders"), LocStrings.Opt_Shaders, OptConfig.CheapShaders, LocStrings.Tip_Opt_Shaders);
		AddOptRow(transform6, transform4, A("OptShake"), LocStrings.Opt_Shake, OptConfig.DisableShake, LocStrings.Tip_Opt_Shake);
		AddOptRow(transform6, transform4, A("OptHeavyPost"), LocStrings.Opt_Effects, OptConfig.DisableHeavyPostProcess, LocStrings.Tip_Opt_Effects);
		AddOptHeader(transform6, transform3, LocStrings.Hdr_Medium);
		AddOptRow(transform6, transform4, A("OptGrass"), LocStrings.Opt_Grass, OptConfig.DisableGrass, LocStrings.Tip_Opt_Grass);
		AddOptRow(transform6, transform4, A("OptGrassLod"), LocStrings.Opt_GrassLod, OptConfig.CoarseGrassLod, LocStrings.Tip_Opt_GrassLod);
		AddOptRow(transform6, transform4, A("OptGrassDisp"), LocStrings.Opt_GrassDisp, OptConfig.DisableGrassDisp, LocStrings.Tip_Opt_GrassDisp);
		AddOptRow(transform6, transform4, A("OptShadows"), LocStrings.Opt_Shadows, OptConfig.DisableShadows, LocStrings.Tip_Opt_Shadows);
		AddOptRow(transform6, transform4, A("OptSmartShadows"), LocStrings.Opt_SmartShadows, OptConfig.SmartShadows, LocStrings.Tip_Opt_SmartShadows);
		AddOptRow(transform6, transform4, A("OptImpostorSh"), LocStrings.Opt_ImpostorSh, OptConfig.DisableImpostorShadows, LocStrings.Tip_Opt_ImpostorSh);
		AddOptRow(transform6, transform4, A("OptWaves"), LocStrings.Opt_Waves, OptConfig.DisableWaterWaves, LocStrings.Tip_Opt_Waves);
		AddOptRow(transform6, transform4, A("OptFoam"), LocStrings.Opt_WaterFoam, OptConfig.DisableWaterFoam, LocStrings.Tip_Opt_WaterFoam);
		AddOptRow(transform6, transform4, A("OptWeather"), LocStrings.Opt_Weather, OptConfig.DisableWeather, LocStrings.Tip_Opt_Weather);
		AddOptRow(transform6, transform4, A("OptWetness"), LocStrings.Opt_Wetness, OptConfig.DisableWetness, LocStrings.Tip_Opt_Wetness);
		AddOptRow(transform6, transform4, A("OptDecor"), LocStrings.Opt_Decor, OptConfig.DisableDecor, LocStrings.Tip_Opt_Decor);
		AddOptRow(transform6, transform4, A("OptWorldLod"), LocStrings.Opt_WorldLod, OptConfig.AggressiveWorldLod, LocStrings.Tip_Opt_WorldLod);
		AddOptRow(transform6, transform4, A("OptCheapTrees"), LocStrings.Opt_CheapTrees, OptConfig.CheapTrees, LocStrings.Tip_Opt_CheapTrees);
		AddOptRow(transform6, transform4, A("OptTreeWind"), LocStrings.Opt_TreeWind, OptConfig.DisableTreeWind, LocStrings.Tip_Opt_TreeWind);
		AddOptRow(transform6, transform4, A("OptCheapTerrain"), LocStrings.Opt_CheapTerrain, OptConfig.CheapTerrain, LocStrings.Tip_Opt_CheapTerrain);
		AddOptRow(transform6, transform4, A("OptSky"), LocStrings.Opt_Sky, OptConfig.DisableSky, LocStrings.Tip_Opt_Sky);
		AddOptRow(transform6, transform4, A("OptAtmosphere"), LocStrings.Opt_Atmosphere, OptConfig.DisableAtmosphere, LocStrings.Tip_Opt_Atmosphere);
		AddOptHeader(transform6, transform3, LocStrings.Hdr_Hard);
		AddOptRow(transform6, transform4, A("OptParticles"), LocStrings.Opt_Particles, OptConfig.DisableSecondaryParticles, LocStrings.Tip_Opt_Particles);
		AddOptRow(transform6, transform4, A("OptParticleColl"), LocStrings.Opt_ParticleColl, OptConfig.ReduceParticleCollision, LocStrings.Tip_Opt_ParticleColl);
		AddOptRow(transform6, transform4, A("OptParticleIk"), LocStrings.Opt_ParticleIk, OptConfig.DisableParticleIk, LocStrings.Tip_Opt_ParticleIk);
		AddOptRow(transform6, transform4, A("OptDecals"), LocStrings.Opt_Decals, OptConfig.DisableDecals, LocStrings.Tip_Opt_Decals);
		AddOptRow(transform6, transform4, A("OptGibs"), LocStrings.Opt_Gibs, OptConfig.DisableGibs, LocStrings.Tip_Opt_Gibs);
		AddOptRow(transform6, transform4, A("OptVolBeams"), LocStrings.Opt_VolBeams, OptConfig.DisableVolumetricBeams, LocStrings.Tip_Opt_VolBeams);
		AddOptRow(transform6, transform4, A("OptLights"), LocStrings.Opt_Lights, null, LocStrings.Tip_Opt_Lights, () => OptState.MaxDynamicLights < 8, delegate(bool v)
		{
			OptConfig.MaxDynamicLights.Value = (v ? 2 : 8);
			if (!OptConfig.MuteApply)
			{
				OptState.ActivePreset = PresetId.Custom;
			}
			SharedConfig.MarkDirty();
			SyncOptUi();
		});
		AddOptRow(transform6, transform4, A("OptCloth"), LocStrings.Opt_Cloth, OptConfig.DisableCloth, LocStrings.Tip_Opt_Cloth);
		AddOptRow(transform6, transform4, A("OptDistantAnims"), LocStrings.Opt_DistantAnims, OptConfig.SimplifyDistantAnims, LocStrings.Tip_Opt_DistantAnims);
		AddOptRow(transform6, transform4, A("OptCorpse"), LocStrings.Opt_Corpse, OptConfig.DisableCorpsePhysics, LocStrings.Tip_Opt_Corpse);
		AddOptRow(transform6, transform4, A("OptAnimalPhys"), LocStrings.Opt_AnimalPhys, OptConfig.DisableAnimalPhysics, LocStrings.Tip_Opt_AnimalPhys);
		AddOptRow(transform6, transform4, A("OptLimitCorpse"), LocStrings.Opt_LimitCorpses, OptConfig.LimitCorpses, LocStrings.Tip_Opt_LimitCorpses);
		AddOptRow(transform6, transform4, A("OptFootprints"), LocStrings.Opt_Footprints, OptConfig.DisableFootprints, LocStrings.Tip_Opt_Footprints);
		AddOptHeader(transform6, transform3, LocStrings.Hdr_Other);
		AddOptRow(transform6, transform4, A("OptUnlockFps"), LocStrings.Opt_UnlockFps, OptConfig.UnlockFps, LocStrings.Tip_Opt_UnlockFps);
		AddOptRow(transform6, transform4, A("OptCulling"), LocStrings.Opt_Culling, OptConfig.AggressiveCulling, LocStrings.Tip_Opt_Culling);
		AddOptRow(transform6, transform4, A("OptInstancing"), LocStrings.Opt_Instancing, OptConfig.EnableInstancing, LocStrings.Tip_Opt_Instancing);
		AddOptRow(transform6, transform4, A("OptAutoMemory"), LocStrings.Opt_AutoMemory, OptConfig.AutoMemory, LocStrings.Tip_Opt_AutoMemory);
		AddOptRow(transform6, transform4, A("OptWorkshop"), LocStrings.Opt_Workshop, OptConfig.BlockWorkshopSkins, LocStrings.Tip_Opt_Workshop);
		AddOptRow(transform6, transform4, A("OptSkinUnload"), LocStrings.Opt_Skins, OptConfig.UnloadUnusedSkins, LocStrings.Tip_Opt_Skins);
		AddOptRow(transform6, transform4, A("OptSkinDeform"), LocStrings.Opt_SkinDeform, OptConfig.SimplifySkinDeform, LocStrings.Tip_Opt_SkinDeform);
		transform4.gameObject.SetActive(value: false);
		try
		{
			GameObject gameObject6 = new GameObject(LocStrings.Name_OptLayoutV17);
			gameObject6.transform.SetParent(gameObject5.transform, worldPositionStays: false);
			gameObject6.SetActive(value: false);
		}
		catch
		{
		}
		PlaceOptShablonAfterHeader(transform3);
		SyncOptUi();
		gameObject5.transform.SetParent(gameObject.transform, worldPositionStays: false);
		PrepareCustomPanelVisibility(gameObject5);
		_optPanelRef = gameObject5;
		_optBuilt = true;
	}

	private static string A(string s)
	{
		return new string(s.ToCharArray());
	}

	private static Transform FindOptToggleTemplate(Transform panel)
	{
		Transform transform = panel.Find(LocStrings.Path_ToggleDof) ?? panel.Find(LocStrings.Path_ToggleAo) ?? panel.Find(LocStrings.Path_ToggleBloom) ?? panel.Find(LocStrings.Path_ToggleLensdirt);
		if (transform != null)
		{
			return transform;
		}
		try
		{
			for (int i = 0; i < panel.childCount; i++)
			{
				Transform child = panel.GetChild(i);
				if (!(child == null) && (child.name ?? string.Empty).IndexOf(LocStrings.Marker_Toggle, System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return child;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static Transform FindLargestTitle(Transform panel)
	{
		if (panel == null)
		{
			return null;
		}
		Transform result = null;
		float num = 0f;
		try
		{
			TextMeshProUGUI[] array = panel.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
			if (array == null)
			{
				return null;
			}
			TextMeshProUGUI[] array2 = array;
			foreach (TextMeshProUGUI textMeshProUGUI in array2)
			{
				if (!(textMeshProUGUI == null) && textMeshProUGUI.fontSize > num)
				{
					num = textMeshProUGUI.fontSize;
					result = textMeshProUGUI.transform;
				}
			}
			return result;
		}
		catch
		{
			return result;
		}
	}

	private static void PrepareOptPanelLayout(GameObject panel)
	{
		if (panel == null)
		{
			return;
		}
		try
		{
			ScrollRect component = panel.GetComponent<ScrollRect>();
			if (component != null)
			{
				UnityEngine.Object.DestroyImmediate(component);
			}
		}
		catch
		{
		}
		try
		{
			Transform transform = panel.transform.Find("OptViewport");
			if (transform != null)
			{
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		catch
		{
		}
	}

	private static void AddOptHeader(Transform parent, Transform titleTemplate, string label)
	{
		if (parent == null || titleTemplate == null)
		{
			return;
		}
		Transform transform = UnityEngine.Object.Instantiate(titleTemplate, parent);
		transform.gameObject.SetActive(value: true);
		transform.name = A("OptHdr");
		StripAnimatorsAndTransitions(transform);
		try
		{
			foreach (TweakUIBase componentsInChild in transform.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
			{
				if (componentsInChild != null)
				{
					UnityEngine.Object.DestroyImmediate(componentsInChild);
				}
			}
		}
		catch
		{
		}
		StripLocalizers(transform);
		Transform transform2 = transform.Find(LocStrings.Marker_Toggle);
		if (transform2 != null)
		{
			transform2.gameObject.SetActive(value: false);
		}
		TextMeshProUGUI textMeshProUGUI = transform.GetComponent<TextMeshProUGUI>() ?? transform.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
		if (textMeshProUGUI != null)
		{
			StripLocalizers(textMeshProUGUI.transform);
			try
			{
				textMeshProUGUI.enableWordWrapping = false;
			}
			catch
			{
			}
			try
			{
				textMeshProUGUI.overflowMode = TextOverflowModes.Overflow;
			}
			catch
			{
			}
			try
			{
				textMeshProUGUI.raycastTarget = false;
			}
			catch
			{
			}
			TabLabelHelper.ApplyFixedTabLabel(textMeshProUGUI, label);
		}
		else
		{
			LayoutElement obj5 = transform.GetComponent<LayoutElement>() ?? transform.gameObject.AddComponent<LayoutElement>();
			obj5.minHeight = 44f;
			obj5.preferredHeight = 44f;
			obj5.flexibleHeight = 0f;
		}
		EnsureTooltip(transform, null);
	}

	private static void AddOptRow(Transform parent, Transform template, string name, string label, ConfigEntry<bool> config, string tip, System.Func<bool> get = null, System.Action<bool> set = null)
	{
		Transform transform = UnityEngine.Object.Instantiate(template, parent);
		transform.gameObject.SetActive(value: true);
		if (config != null)
		{
			get = () => config.Value;
			set = delegate(bool v)
			{
				config.Value = v;
				if (!OptConfig.MuteApply)
				{
					OptState.ActivePreset = PresetId.Custom;
				}
				SharedConfig.MarkDirty();
				SyncOptUi();
			};
		}
		CreateOptToggle(transform, name, label, get, set, tip);
	}

	private static void AddOptShablonDropdown(Transform parent, Transform header)
	{
		if (parent == null)
		{
			return;
		}
		string[] options = new string[4]
		{
			LocStrings.Preset_Own,
			LocStrings.Preset_Combat,
			LocStrings.Preset_Potato,
			LocStrings.Preset_SuperPotato
		};
		_optSyncing = true;
		try
		{
			_optShablon = CBSafe.Crosshair.UI.SystemDropdownFactory.Create(parent, LocStrings.Key_Shablon, options, null, delegate(int idx, string _)
			{
				if (!_optSyncing)
				{
					ApplyOptShablonIndex(idx);
					SyncOptUi();
				}
			}, LocStrings.Tip_OptShablon, LocStrings.Key_OptShablon);
			_optShablon?.SetIndexSilently(ReadOptShablonIndex());
		}
		catch
		{
			_optShablon = null;
		}
		finally
		{
			_optSyncing = false;
		}
		PlaceOptShablonAfterHeader(header);
	}

	private static void PlaceOptShablonAfterHeader(Transform header)
	{
		if (_optShablon == null || _optShablon.RowRect == null || header == null)
		{
			return;
		}
		try
		{
			_optShablon.RowRect.SetSiblingIndex(header.GetSiblingIndex() + 1);
		}
		catch
		{
		}
	}

	private static int ReadOptShablonIndex()
	{
		return OptState.ActivePreset switch
		{
			PresetId.Combat => 1, 
			PresetId.Potato => 2, 
			PresetId.SuperPotato => 3, 
			_ => 0, 
		};
	}

	private static void ApplyOptShablonIndex(int idx)
	{
		if (idx >= 1 && idx <= _optShablonPresets.Length)
		{
			OptPresets.Apply(_optShablonPresets[idx - 1]);
		}
		else
		{
			OptConfig.AllOff();
			OptState.ActivePreset = PresetId.Custom;
		}
		SharedConfig.MarkDirty();
	}

	private static void AddOptTextureDropdown(Transform parent)
	{
		_optTextures = CreateOptChoiceDropdown(parent, LocStrings.Opt_Textures, new string[3]
		{
			LocStrings.Opt_Tex_1,
			LocStrings.Opt_Tex_2,
			LocStrings.Opt_Tex_4
		}, LocStrings.Tip_Opt_Textures, LocStrings.Key_OptTextures, ReadOptTextureIndex, ApplyOptTextureIndex);
	}

	private static CBSafe.Crosshair.UI.DropdownHandle CreateOptChoiceDropdown(Transform parent, string label, string[] options, string tip, string key, System.Func<int> readIndex, System.Action<int> applyIndex)
	{
		if (parent == null)
		{
			return null;
		}
		CBSafe.Crosshair.UI.DropdownHandle dropdownHandle = null;
		_optSyncing = true;
		try
		{
			dropdownHandle = CBSafe.Crosshair.UI.SystemDropdownFactory.Create(parent, label, options, null, delegate(int idx, string _)
			{
				if (!_optSyncing)
				{
					applyIndex(idx);
					SyncOptUi();
				}
			}, tip, key);
			if (dropdownHandle != null)
			{
				dropdownHandle.SetIndexSilently(readIndex());
				return dropdownHandle;
			}
			return dropdownHandle;
		}
		catch
		{
			return null;
		}
		finally
		{
			_optSyncing = false;
		}
	}

	private static void AddGlovesSkinDropdown(Transform parent)
	{
		if (parent == null)
		{
			return;
		}
		string[] skinLabels = GlovesSkinApplier.SkinLabels;
		try
		{
			SystemToggleFactory.Create(parent, "Перчатки", "Применять скин к тактическим перчаткам", GlovesSkinApplier.SkinEnabled, delegate(bool v)
			{
				try
				{
					if (Defines.GlovesSkinEnabled != null)
					{
						Defines.GlovesSkinEnabled.Value = v;
					}
					Defines.SaveSettings();
				}
				catch
				{
				}
				try
				{
					GlovesSkinApplier.ReloadTextures(GlovesSkinApplier.CurrentSkinIndex);
				}
				catch
				{
				}
			}, null);
		}
		catch
		{
		}
		_optGlovesSkin = CreateOptChoiceDropdown(parent, "Скин на перчатки", skinLabels, "Выберите скин для тактических перчаток", "GlovesSkinIndex", delegate
		{
			try
			{
				return (Defines.GlovesSkinIndex != null) ? Defines.GlovesSkinIndex.Value : 0;
			}
			catch
			{
				return 0;
			}
		}, delegate(int idx)
		{
			if (Defines.GlovesSkinIndex != null)
			{
				try
				{
					Defines.GlovesSkinIndex.Value = idx;
				}
				catch
				{
				}
				try
				{
					Defines.SaveSettings();
				}
				catch
				{
				}
				try
				{
					GlovesSkinApplier.ReloadTextures(idx);
				}
				catch
				{
				}
			}
		});
	}

	private static int ReadOptTextureIndex()
	{
		int num = 0;
		try
		{
			num = OptConfig.TextureDownsample.Value;
		}
		catch
		{
		}
		if (num <= 0)
		{
			return 0;
		}
		if (num == 1)
		{
			return 1;
		}
		return 2;
	}

	private static void ApplyOptTextureIndex(int idx)
	{
		int value = ((idx > 0) ? ((idx == 1) ? 1 : 2) : 0);
		try
		{
			OptConfig.TextureDownsample.Value = value;
		}
		catch
		{
		}
		if (!OptConfig.MuteApply)
		{
			OptState.ActivePreset = PresetId.Custom;
		}
		SharedConfig.MarkDirty();
		OptRuntime.ForceApply = true;
	}

	private static void CreateOptToggle(Transform source, string newName, string label, System.Func<bool> get, System.Action<bool> set, string tip)
	{
		if (source == null || get == null || set == null)
		{
			return;
		}
		try
		{
			source.name = newName;
			StripAnimatorsAndTransitions(source);
			try
			{
				source.RemoveComponent<TweakUIToggle>();
			}
			catch
			{
			}
			try
			{
				foreach (TweakUIBase componentsInChild in source.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
				{
					if (componentsInChild != null)
					{
						UnityEngine.Object.DestroyImmediate(componentsInChild);
					}
				}
			}
			catch
			{
			}
			try
			{
				foreach (Component componentsInChild2 in source.GetComponentsInChildren<Component>(includeInactive: true))
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
					if (name != null && (name.Contains(LocStrings.Marker_Locali) || name.Contains(LocStrings.Marker_Phrase) || name.Contains(LocStrings.Marker_Token)))
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
			catch
			{
			}
			Transform transform = source.Find(LocStrings.Marker_Text);
			if (transform != null)
			{
				StripLocalizers(transform);
				TextMeshProUGUI component = transform.GetComponent<TextMeshProUGUI>();
				if (component != null)
				{
					component.text = label;
					try
					{
						component.m_text = label;
					}
					catch
					{
					}
					try
					{
						component.gameObject.AddComponent<ToggleLabelEnforcer>().Bind(component, label);
					}
					catch
					{
					}
				}
			}
			Transform transform2 = source.Find(LocStrings.Marker_Toggle);
			if (transform2 == null)
			{
				return;
			}
			StripAnimatorsAndTransitions(transform2);
			Toggle component2 = transform2.GetComponent<Toggle>();
			if (component2 == null)
			{
				return;
			}
			component2.onValueChanged.RemoveAllListeners();
			try
			{
				component2.transition = Selectable.Transition.None;
			}
			catch
			{
			}
			FindToggleChildren(transform2, out var offGo, out var onGo);
			bool flag = false;
			try
			{
				flag = get();
			}
			catch
			{
			}
			try
			{
				component2.SetIsOnWithoutNotify(flag);
			}
			catch
			{
				try
				{
					component2.isOn = flag;
				}
				catch
				{
				}
			}
			ApplyToggleVisual(offGo, onGo, flag);
			ToggleVisualEnforcer toggleVisualEnforcer = null;
			try
			{
				toggleVisualEnforcer = transform2.gameObject.GetComponent<ToggleVisualEnforcer>() ?? transform2.gameObject.AddComponent<ToggleVisualEnforcer>();
				toggleVisualEnforcer.Bind(offGo, onGo, flag);
			}
			catch
			{
			}
			try
			{
				component2.enabled = true;
				component2.interactable = true;
			}
			catch
			{
			}
			GameObject capturedOff = offGo;
			GameObject capturedOn = onGo;
			ToggleVisualEnforcer capturedEnforcer = toggleVisualEnforcer;
			((UnityEvent<bool>)component2.onValueChanged).AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>((System.Action<bool>)delegate(bool v)
			{
				if (_optSyncing)
				{
					return;
				}
				try
				{
					set(v);
					ApplyToggleVisual(capturedOff, capturedOn, v);
					capturedEnforcer?.SetValue(v);
				}
				catch
				{
				}
			}));
			NormalizeToggleRowLayout(source);
			LayoutElement obj14 = source.GetComponent<LayoutElement>() ?? source.gameObject.AddComponent<LayoutElement>();
			obj14.minHeight = 32f;
			obj14.preferredHeight = 32f;
			EnsureTooltip(source, tip);
			Toggle capturedToggle = component2;
			GameObject capOff = offGo;
			GameObject capOn = onGo;
			ToggleVisualEnforcer capEnf = toggleVisualEnforcer;
			_optSync.Add(delegate
			{
				bool flag2 = false;
				try
				{
					flag2 = get();
				}
				catch
				{
				}
				try
				{
					capturedToggle.SetIsOnWithoutNotify(flag2);
				}
				catch
				{
				}
				ApplyToggleVisual(capOff, capOn, flag2);
				capEnf?.SetValue(flag2);
			});
		}
		catch
		{
		}
	}

	private static void SyncOptUi()
	{
		_optSyncing = true;
		try
		{
			for (int i = 0; i < _optSync.Count; i++)
			{
				try
				{
					_optSync[i]?.Invoke();
				}
				catch
				{
				}
			}
			try
			{
				_optShablon?.SetIndexSilently(ReadOptShablonIndex());
			}
			catch
			{
			}
			try
			{
				_optTextures?.SetIndexSilently(ReadOptTextureIndex());
			}
			catch
			{
			}
			try
			{
				_optGlovesSkin?.SetIndexSilently((Defines.GlovesSkinIndex != null) ? Defines.GlovesSkinIndex.Value : 0);
			}
			catch
			{
			}
		}
		finally
		{
			_optSyncing = false;
		}
	}

	private static void EnsureProcheeTab()
	{
		GameObject gameObject = FindUiObject(LocStrings.Path_OptionsPanel);
		if (gameObject == null)
		{
			return;
		}
		Transform transform = gameObject.transform.Find(LocStrings.Path_ProcheeChild);
		if (transform != null)
		{
			if (transform.Find(LocStrings.Name_BindAutorun) != null && transform.Find(LocStrings.Name_ToggleLayoutV2) != null && transform.Find(LocStrings.Name_NewHoloPatch) != null && transform.Find(LocStrings.Name_GraphicsPatch) == null)
			{
				_panelRef = transform.gameObject;
				PrepareCustomPanelVisibility(_panelRef);
				_built = true;
				return;
			}
			try
			{
				UnityEngine.Object.Destroy(transform.gameObject);
			}
			catch
			{
			}
		}
		GameObject gameObject2 = FindUiObject(LocStrings.Path_Categories);
		if (gameObject2 == null)
		{
			return;
		}
		bool flag = gameObject2.transform.Find(LocStrings.Path_ProcheeChild) != null;
		GameObject gameObject3 = FindUiObject(LocStrings.Path_ExperimentalTab);
		if (gameObject3 == null)
		{
			return;
		}
		GameObject gameObject4 = FindUiObject(LocStrings.Path_ImageEffects);
		if (gameObject4 == null)
		{
			return;
		}
		GameObject gameObject5 = FindUiObject(LocStrings.Path_Input);
		if (gameObject5 == null)
		{
			return;
		}
		if (!flag)
		{
			CreateTab(gameObject3, gameObject2.transform, LocStrings.Name_Prochee, LocStrings.Prochee_Tab);
		}
		GameObject gameObject6 = UnityEngine.Object.Instantiate(gameObject4);
		gameObject6.name = LocStrings.Name_Prochee;
		StripPanelChrome(gameObject6);
		Transform transform2 = gameObject6.transform.Find(LocStrings.Path_PanelOptHeader);
		if (transform2 != null)
		{
			transform2.gameObject.SetActive(value: false);
		}
		foreach (string item in new List<string>
		{
			LocStrings.Path_ToggleAntialias,
			LocStrings.Path_ToggleShafts,
			LocStrings.Path_ToggleSharpen,
			LocStrings.Path_ToggleVignet,
			LocStrings.Path_ToggleMotion,
			LocStrings.Path_ToggleDof,
			LocStrings.Path_ToggleAo,
			LocStrings.Path_ToggleBloom,
			LocStrings.Path_ToggleLensdirt
		})
		{
			Transform transform3 = gameObject6.transform.Find(item);
			if (transform3 != null)
			{
				transform3.gameObject.SetActive(value: false);
			}
		}
		GameObject gameObject7 = UnityEngine.Object.Instantiate(gameObject5);
		Transform transform4 = gameObject7.transform.Find(LocStrings.Path_InputHandHeader);
		if (transform4 != null)
		{
			StripLocalizers(transform4);
			TextMeshProUGUI component = transform4.GetComponent<TextMeshProUGUI>();
			if (component != null)
			{
				component.text = LocStrings.HandSettings_Hdr;
				component.m_text = LocStrings.HandSettings_Hdr;
			}
			transform4.SetParent(gameObject6.transform, worldPositionStays: false);
		}
		Transform transform5 = gameObject7.transform.Find(LocStrings.Path_InputMidi);
		if (transform5 != null)
		{
			transform5.SetParent(gameObject6.transform, worldPositionStays: false);
			CreateButton(transform5, LocStrings.Name_HandRightLeft, LocStrings.Btn_LeftHand, !Defines.LeftRightHand.Value, Defines.LeftRightHand, LocStrings.Tip_LeftHand, invertedConfig: true);
		}
		Transform transform6 = gameObject7.transform.Find(LocStrings.Path_InputFlipy);
		if (transform6 != null)
		{
			Transform transform7 = UnityEngine.Object.Instantiate(transform6, gameObject6.transform);
			transform7.SetParent(gameObject6.transform, worldPositionStays: false);
			CreateButton(transform7, LocStrings.Name_HandLong, LocStrings.Btn_LongHand, Defines.LongHand.Value, Defines.LongHand, LocStrings.Tip_LongHand);
			Transform transform8 = UnityEngine.Object.Instantiate(transform6, gameObject6.transform);
			transform8.SetParent(gameObject6.transform, worldPositionStays: false);
			CreateButton(transform8, LocStrings.Name_FastAlt, LocStrings.Btn_FastAlt, Defines.FastAlt.Value, Defines.FastAlt, LocStrings.Tip_FastAlt);
			Transform transform9 = UnityEngine.Object.Instantiate(transform6, gameObject6.transform);
			transform9.SetParent(gameObject6.transform, worldPositionStays: false);
			CreateButton(transform9, LocStrings.Name_NewHoloPatch, LocStrings.Btn_NewHolo, Defines.NewHolo.Value, Defines.NewHolo, LocStrings.Tip_NewHolo, invertedConfig: false, delegate
			{
				try
				{
					HolosightSwap.SetEnabled(Defines.NewHolo.Value);
				}
				catch
				{
				}
			});
		}
		Transform transform10 = gameObject7.transform.Find(LocStrings.Path_InputMovement);
		if (transform10 != null)
		{
			StripLocalizers(transform10);
			TextMeshProUGUI component2 = transform10.GetComponent<TextMeshProUGUI>();
			if (component2 != null)
			{
				component2.text = LocStrings.Binds_Hdr;
				component2.m_text = LocStrings.Binds_Hdr;
			}
			transform10.SetParent(gameObject6.transform, worldPositionStays: false);
		}
		string[] array = new string[7]
		{
			LocStrings.Bind_Forward,
			LocStrings.Bind_Backward,
			LocStrings.Bind_Left,
			LocStrings.Bind_Right,
			LocStrings.Bind_Jump,
			LocStrings.Bind_Sprint,
			LocStrings.Bind_Duck
		};
		string[] array2 = new string[7]
		{
			LocStrings.Name_BindAutorun,
			LocStrings.Name_BindTpMenu,
			LocStrings.Name_BindRemove,
			LocStrings.Name_BindTpa,
			LocStrings.Name_BindUp,
			LocStrings.Name_BindTpc,
			LocStrings.Name_BindKit
		};
		string[] array3 = new string[7]
		{
			LocStrings.Bind_AutoRun,
			LocStrings.Bind_TpMenu,
			LocStrings.Bind_RemoveLbl,
			LocStrings.Bind_TpaLbl,
			LocStrings.Bind_MenuLbl,
			LocStrings.Bind_TpcLbl,
			LocStrings.Bind_KitLbl
		};
		for (int i = 0; i < array.Length; i++)
		{
			Transform transform11 = gameObject7.transform.Find(array[i]);
			if (transform11 != null)
			{
				transform11.SetParent(gameObject6.transform, worldPositionStays: false);
				CreateBind(transform11, array2[i], array3[i]);
			}
		}
		Transform transform12 = gameObject7.transform.Find("input.sensitivity");
		if (transform12 != null)
		{
			CreateStretchSliderRow(gameObject6.transform, transform12.gameObject);
		}
		UnityEngine.Object.Destroy(gameObject7);
		try
		{
			GameObject gameObject8 = new GameObject(LocStrings.Name_ToggleLayoutV2);
			gameObject8.transform.SetParent(gameObject6.transform, worldPositionStays: false);
			gameObject8.SetActive(value: false);
		}
		catch
		{
		}
		try
		{
			AddGlovesSkinDropdown(gameObject6.transform);
		}
		catch (System.Exception ex)
		{
			Main.Instance?.Log.LogError("[BoostUI] AddGlovesSkinDropdown failed: " + ex);
		}
		gameObject6.transform.SetParent(gameObject.transform, worldPositionStays: false);
		PrepareCustomPanelVisibility(gameObject6);
		ConVar.Input.flipy = false;
		_panelRef = gameObject6;
		_built = true;
	}

	private static void CreateTab(GameObject sourceTab, Transform tabBar, string name, string label)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(sourceTab);
		gameObject.name = name;
		gameObject.transform.SetParent(tabBar, worldPositionStays: false);
		gameObject.transform.SetAsLastSibling();
		gameObject.SetActive(value: true);
		GameObject gameObject2 = FindUiObject(LocStrings.Path_OptionsPanel);
		try
		{
			foreach (TabToggle componentsInChild in gameObject.GetComponentsInChildren<TabToggle>(includeInactive: true))
			{
				if (componentsInChild != null)
				{
					componentsInChild.TabHolder = tabBar;
					if (gameObject2 != null)
					{
						componentsInChild.ContentHolder = gameObject2.transform;
					}
					componentsInChild.FadeIn = false;
					componentsInChild.FadeOut = false;
				}
			}
		}
		catch
		{
		}
		try
		{
			TabLabelHelper.ApplyFixedTabLabel(gameObject, label);
		}
		catch
		{
		}
		Button button = gameObject.GetComponent<Button>() ?? gameObject.GetComponentInChildren<Button>(includeInactive: true);
		if (button != null)
		{
			button.onClick.RemoveAllListeners();
			button.interactable = true;
			Button sourceButton = button;
			string panelName = name;
			button.onClick.AddListener((System.Action)delegate
			{
				SwitchToCustomTab(panelName, tabBar, sourceButton);
			});
		}
	}

	private static void SwitchToCustomTab(string panelName, Transform tabBar, Button sourceButton)
	{
		try
		{
			try
			{
				TabSystemManager.PrepareNativeTabSwitch();
			}
			catch
			{
			}
			try
			{
				CrosshairTabManager.PrepareNativeTabSwitch();
			}
			catch
			{
			}
			GameObject gameObject = FindUiObject(LocStrings.Path_OptionsPanel);
			GameObject gameObject2 = ((panelName == LocStrings.Name_Optimization) ? _optPanelRef : _panelRef);
			if (gameObject == null || gameObject2 == null)
			{
				Main.Instance?.Log.LogError("[BoostUI] custom tab panel is unavailable: " + panelName);
				return;
			}
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				Transform child = gameObject.transform.GetChild(i);
				if (child != null)
				{
					child.gameObject.SetActive(child == gameObject2.transform);
				}
			}
			gameObject2.SetActive(value: true);
			PrepareCustomPanelVisibility(gameObject2);
			RectTransform component = gameObject2.GetComponent<RectTransform>();
			if (component != null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(component);
			}
			Canvas.ForceUpdateCanvases();
			if (tabBar != null && sourceButton != null)
			{
				for (int j = 0; j < tabBar.childCount; j++)
				{
					Button component2 = tabBar.GetChild(j).GetComponent<Button>();
					if (component2 != null)
					{
						component2.interactable = component2 != sourceButton;
					}
				}
			}
			CustomTabSelection.Apply(tabBar, panelName);
		}
		catch (System.Exception ex)
		{
			Main.Instance?.Log.LogError("[BoostUI] custom tab switch: " + ex);
		}
	}

	private static void PrepareCustomPanelVisibility(GameObject panel)
	{
		if (panel == null)
		{
			return;
		}
		try
		{
			panel.SetActive(value: true);
			panel.transform.localScale = Vector3.one;
			RectTransform component = panel.GetComponent<RectTransform>();
			if (component != null)
			{
				component.anchorMin = Vector2.zero;
				component.anchorMax = Vector2.one;
				component.offsetMin = Vector2.zero;
				component.offsetMax = Vector2.zero;
			}
			foreach (CanvasGroup componentsInChild in panel.GetComponentsInChildren<CanvasGroup>(includeInactive: true))
			{
				if (componentsInChild != null)
				{
					componentsInChild.alpha = 1f;
					componentsInChild.interactable = true;
					componentsInChild.blocksRaycasts = true;
				}
			}
		}
		catch
		{
		}
	}

	private static GameObject FindUiObject(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		GameObject gameObject = GameObject.Find(path);
		if (gameObject != null)
		{
			return gameObject;
		}
		GameObject gameObject2 = GameObject.Find(LocStrings.Path_MenuUiRoot);
		if (gameObject2 == null)
		{
			return null;
		}
		string text = LocStrings.Path_MenuUiRoot + "/";
		if (path.StartsWith(text, System.StringComparison.Ordinal))
		{
			Transform transform = gameObject2.transform.Find(path.Substring(text.Length));
			if (!(transform == null))
			{
				return transform.gameObject;
			}
			return null;
		}
		Transform transform2 = gameObject2.transform.Find(path);
		if (!(transform2 == null))
		{
			return transform2.gameObject;
		}
		return null;
	}

	private static void HideNativeTutorial()
	{
		GameObject gameObject = FindUiObject(LocStrings.Path_OptionsRoot);
		if (gameObject == null)
		{
			return;
		}
		try
		{
			foreach (Button componentsInChild in gameObject.GetComponentsInChildren<Button>(includeInactive: true))
			{
				if (componentsInChild == null)
				{
					continue;
				}
				TMP_Text tMP_Text = null;
				try
				{
					tMP_Text = componentsInChild.GetComponentInChildren<TMP_Text>(includeInactive: true);
				}
				catch
				{
				}
				if (tMP_Text == null)
				{
					continue;
				}
				string text = null;
				try
				{
					text = tMP_Text.text;
				}
				catch
				{
				}
				if (!string.IsNullOrEmpty(text) && !(text.Trim().ToUpperInvariant() != LocStrings.Txt_StartTutorial))
				{
					Transform transform = componentsInChild.transform.parent;
					if (transform != null && transform.parent != null && (transform.parent.name ?? string.Empty).IndexOf(LocStrings.Marker_Tutorial, System.StringComparison.OrdinalIgnoreCase) >= 0)
					{
						transform = transform.parent;
					}
					else if (transform == null)
					{
						transform = componentsInChild.transform;
					}
					transform.gameObject.SetActive(value: false);
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Transform componentsInChild2 in gameObject.GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (!(componentsInChild2 == null) && (componentsInChild2.name ?? string.Empty).IndexOf(LocStrings.Marker_Tutorial, System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					componentsInChild2.gameObject.SetActive(value: false);
				}
			}
		}
		catch
		{
		}
	}

	private static void StripPanelChrome(GameObject panel)
	{
		if (panel == null)
		{
			return;
		}
		try
		{
			foreach (TweakUIBase componentsInChild in panel.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
			{
				if (componentsInChild != null)
				{
					UnityEngine.Object.DestroyImmediate(componentsInChild);
				}
			}
		}
		catch
		{
		}
		try
		{
			TweakUIDropdown[] array = panel.GetComponentsInChildren<TweakUIDropdown>(includeInactive: true);
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != null)
					{
						UnityEngine.Object.DestroyImmediate(array[i]);
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Animator componentsInChild2 in panel.GetComponentsInChildren<Animator>(includeInactive: true))
			{
				if (componentsInChild2 != null)
				{
					UnityEngine.Object.DestroyImmediate(componentsInChild2);
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Component componentsInChild3 in panel.GetComponentsInChildren<Component>(includeInactive: true))
			{
				if (!(componentsInChild3 == null))
				{
					string text = null;
					try
					{
						Il2CppSystem.Type il2CppType = componentsInChild3.GetIl2CppType();
						text = ((il2CppType != null) ? il2CppType.Name : null);
					}
					catch
					{
						text = null;
					}
					if (text == "FlexTransition")
					{
						UnityEngine.Object.DestroyImmediate(componentsInChild3);
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void StripLocalizers(Transform t)
	{
		if (t == null)
		{
			return;
		}
		try
		{
			t.RemoveComponent<LocalizeText>();
		}
		catch
		{
		}
		try
		{
			foreach (Component component in t.GetComponents<Component>())
			{
				if (component == null)
				{
					continue;
				}
				string text;
				try
				{
					Il2CppSystem.Type il2CppType = component.GetIl2CppType();
					text = ((il2CppType != null) ? il2CppType.Name : null);
				}
				catch
				{
					continue;
				}
				if (!string.IsNullOrEmpty(text) && (text.Contains(LocStrings.Marker_Locali) || text.Contains(LocStrings.Marker_Phrase) || text.Contains(LocStrings.Marker_Token)))
				{
					try
					{
						UnityEngine.Object.DestroyImmediate(component);
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
	}

	private static void StripAnimatorsAndTransitions(Transform root)
	{
		if (root == null)
		{
			return;
		}
		try
		{
			foreach (Animator componentsInChild in root.GetComponentsInChildren<Animator>(includeInactive: true))
			{
				if (componentsInChild != null)
				{
					UnityEngine.Object.DestroyImmediate(componentsInChild);
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Component componentsInChild2 in root.GetComponentsInChildren<Component>(includeInactive: true))
			{
				if (!(componentsInChild2 == null))
				{
					string text = null;
					try
					{
						Il2CppSystem.Type il2CppType = componentsInChild2.GetIl2CppType();
						text = ((il2CppType != null) ? il2CppType.Name : null);
					}
					catch
					{
						continue;
					}
					if (text == "FlexTransition")
					{
						UnityEngine.Object.DestroyImmediate(componentsInChild2);
					}
				}
			}
		}
		catch
		{
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

	private static Tooltip EnsureTooltip(Transform source, string description)
	{
		if (source == null)
		{
			return null;
		}
		if (string.IsNullOrEmpty(description))
		{
			try
			{
				Tooltip tooltip = source.GetComponent<Tooltip>() ?? source.GetComponentInChildren<Tooltip>(includeInactive: true);
				if (tooltip != null)
				{
					tooltip.enabled = false;
				}
			}
			catch
			{
			}
			return null;
		}
		Tooltip tooltip2 = null;
		try
		{
			tooltip2 = source.GetComponent<Tooltip>();
		}
		catch
		{
		}
		if (tooltip2 == null)
		{
			try
			{
				tooltip2 = source.GetComponentInChildren<Tooltip>(includeInactive: true);
			}
			catch
			{
			}
		}
		if (tooltip2 == null)
		{
			try
			{
				tooltip2 = source.gameObject.AddComponent<Tooltip>();
			}
			catch
			{
				return null;
			}
		}
		try
		{
			tooltip2.enabled = true;
		}
		catch
		{
		}
		try
		{
			tooltip2.token = string.Empty;
		}
		catch
		{
		}
		try
		{
			tooltip2.Text = description;
		}
		catch
		{
		}
		try
		{
			if (tooltip2.TooltipObject == null)
			{
				GameObject gameObject = FindTooltipObjectTemplate();
				if (gameObject != null)
				{
					tooltip2.TooltipObject = gameObject;
					return tooltip2;
				}
				return tooltip2;
			}
			return tooltip2;
		}
		catch
		{
			return tooltip2;
		}
	}

	private static void FindToggleChildren(Transform toggleTransform, out GameObject offGo, out GameObject onGo)
	{
		offGo = null;
		onGo = null;
		if (toggleTransform == null)
		{
			return;
		}
		try
		{
			for (int i = 0; i < toggleTransform.childCount; i++)
			{
				Transform child = toggleTransform.GetChild(i);
				if (!(child == null))
				{
					string name = child.name;
					if (name == LocStrings.Marker_Off || name == LocStrings.Marker_OffCap)
					{
						offGo = child.gameObject;
					}
					else if (name == LocStrings.Marker_On || name == LocStrings.Marker_OnCap)
					{
						onGo = child.gameObject;
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void ApplyToggleVisual(GameObject offGo, GameObject onGo, bool value)
	{
		try
		{
			if (offGo != null)
			{
				offGo.SetActive(!value);
			}
		}
		catch
		{
		}
		try
		{
			if (onGo != null)
			{
				onGo.SetActive(value);
			}
		}
		catch
		{
		}
	}

	private static void NormalizeToggleRowLayout(Transform row)
	{
		if (row == null)
		{
			return;
		}
		try
		{
			if (row is RectTransform)
			{
				LayoutElement obj = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
				obj.minHeight = 32f;
				obj.preferredHeight = 32f;
			}
			Transform transform = row.Find(LocStrings.Marker_Text);
			RectTransform rectTransform = (RectTransform)((transform is RectTransform) ? transform : null);
			if (rectTransform != null)
			{
				rectTransform.anchorMin = new Vector2(0f, 0f);
				rectTransform.anchorMax = new Vector2(1f, 1f);
				rectTransform.pivot = new Vector2(0f, 0.5f);
				rectTransform.offsetMin = new Vector2(16f, 0f);
				rectTransform.offsetMax = new Vector2(-72f, 0f);
				TextMeshProUGUI component = transform.GetComponent<TextMeshProUGUI>();
				if (component != null)
				{
					component.alignment = TextAlignmentOptions.MidlineLeft;
					component.enableWordWrapping = false;
					try
					{
						component.overflowMode = TextOverflowModes.Ellipsis;
					}
					catch
					{
					}
				}
			}
			Transform transform2 = row.Find(LocStrings.Marker_Toggle);
			RectTransform rectTransform2 = (RectTransform)((transform2 is RectTransform) ? transform2 : null);
			if (rectTransform2 != null)
			{
				rectTransform2.anchorMin = new Vector2(1f, 0.5f);
				rectTransform2.anchorMax = new Vector2(1f, 0.5f);
				rectTransform2.pivot = new Vector2(1f, 0.5f);
				rectTransform2.anchoredPosition = new Vector2(-24f, 0f);
			}
		}
		catch
		{
		}
	}

	private static void CreateStretchSliderRow(Transform parent, GameObject sliderTemplate)
	{
		string text = "StretchRow";
		if (parent == null || sliderTemplate == null || parent.Find(text) != null)
		{
			return;
		}
		int num = 1;
		int num2 = 100;
		int num3 = Mathf.Clamp(Defines.StretchIndex.Value, num, num2);
		GameObject gameObject = UnityEngine.Object.Instantiate(sliderTemplate, parent);
		gameObject.name = text;
		gameObject.SetActive(value: true);
		try
		{
			foreach (MonoBehaviour componentsInChild in gameObject.GetComponentsInChildren<MonoBehaviour>(includeInactive: true))
			{
				if (componentsInChild == null || componentsInChild is Graphic || componentsInChild is LayoutGroup || componentsInChild is LayoutElement || componentsInChild is Slider || componentsInChild is ContentSizeFitter)
				{
					continue;
				}
				string text2 = null;
				try
				{
					Il2CppSystem.Type il2CppType = componentsInChild.GetIl2CppType();
					text2 = ((il2CppType != null) ? il2CppType.Name : null);
				}
				catch
				{
				}
				if (!string.IsNullOrEmpty(text2) && (text2.Contains("TweakUI") || text2.Contains("Tooltip") || text2.Contains("Localize") || text2.Contains("SoundPlayer") || text2.Contains("Translate")))
				{
					try
					{
						UnityEngine.Object.DestroyImmediate(componentsInChild);
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
		Transform transform = gameObject.transform.Find(LocStrings.Marker_Text);
		if (transform != null)
		{
			Text component = transform.GetComponent<Text>();
			if (component != null)
			{
				component.text = "РАСТЯГ";
			}
			TextMeshProUGUI component2 = transform.GetComponent<TextMeshProUGUI>();
			if (component2 != null)
			{
				component2.text = "РАСТЯГ";
			}
		}
		Transform transform2 = gameObject.transform.Find("Value");
		Text valueText = ((transform2 != null) ? transform2.GetComponent<Text>() : null);
		TextMeshProUGUI valueTextMesh = ((transform2 != null) ? transform2.GetComponent<TextMeshProUGUI>() : null);
		if (valueText == null && valueTextMesh == null)
		{
			try
			{
				foreach (Text componentsInChild2 in gameObject.GetComponentsInChildren<Text>(includeInactive: true))
				{
					if (componentsInChild2 != null && (transform == null || componentsInChild2.transform != transform))
					{
						valueText = componentsInChild2;
						break;
					}
				}
				if (valueText == null)
				{
					foreach (TextMeshProUGUI componentsInChild3 in gameObject.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true))
					{
						if (componentsInChild3 != null && (transform == null || componentsInChild3.transform != transform))
						{
							valueTextMesh = componentsInChild3;
							break;
						}
					}
				}
			}
			catch
			{
			}
		}
		if (valueText != null)
		{
			valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
			valueText.text = AspectStretch.GetLabel(num3);
			valueText.color = Color.white;
		}
		if (valueTextMesh != null)
		{
			valueTextMesh.enableWordWrapping = false;
			valueTextMesh.overflowMode = TextOverflowModes.Overflow;
			valueTextMesh.text = AspectStretch.GetLabel(num3);
			valueTextMesh.color = Color.white;
		}
		Transform transform3 = gameObject.transform.Find("Slider");
		Slider slider = ((transform3 != null) ? transform3.GetComponent<Slider>() : null);
		if (slider == null)
		{
			slider = gameObject.GetComponentInChildren<Slider>(includeInactive: true);
		}
		if (!(slider != null))
		{
			return;
		}
		slider.m_MinValue = num;
		slider.m_MaxValue = num2;
		slider.m_WholeNumbers = true;
		slider.value = num3;
		slider.onValueChanged.RemoveAllListeners();
		((UnityEvent<float>)slider.onValueChanged).AddListener(DelegateSupport.ConvertDelegate<UnityAction<float>>((System.Action<float>)delegate(float value)
		{
			try
			{
				int num4 = (int)value;
				Defines.StretchIndex.Value = num4;
				if (valueText != null)
				{
					valueText.text = AspectStretch.GetLabel(num4);
				}
				if (valueTextMesh != null)
				{
					valueTextMesh.text = AspectStretch.GetLabel(num4);
				}
				SharedConfig.MarkDirty();
				GraphicsDon.ApplyStretch();
			}
			catch
			{
			}
		}));
	}

	private static void CreateButton(Transform source, string newName, string label, bool currentState, ConfigEntry<bool> config, string description = null, bool invertedConfig = false, System.Action onChanged = null)
	{
		if (source == null)
		{
			return;
		}
		try
		{
			source.name = newName;
			StripAnimatorsAndTransitions(source);
			try
			{
				source.RemoveComponent<TweakUIToggle>();
			}
			catch
			{
			}
			try
			{
				foreach (TweakUIBase componentsInChild in source.GetComponentsInChildren<TweakUIBase>(includeInactive: true))
				{
					if (componentsInChild != null)
					{
						UnityEngine.Object.DestroyImmediate(componentsInChild);
					}
				}
			}
			catch
			{
			}
			try
			{
				foreach (Component componentsInChild2 in source.GetComponentsInChildren<Component>(includeInactive: true))
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
					if (name != null && (name.Contains(LocStrings.Marker_Locali) || name.Contains(LocStrings.Marker_Phrase) || name.Contains(LocStrings.Marker_Token)))
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
			catch
			{
			}
			EnsureTooltip(source, description);
			Transform transform = source.Find(LocStrings.Marker_Text);
			if (transform != null)
			{
				StripLocalizers(transform);
				TextMeshProUGUI component = transform.GetComponent<TextMeshProUGUI>();
				if (component != null)
				{
					component.text = label;
					try
					{
						component.m_text = label;
					}
					catch
					{
					}
					try
					{
						component.gameObject.AddComponent<ToggleLabelEnforcer>().Bind(component, label);
					}
					catch
					{
					}
				}
			}
			Transform transform2 = source.Find(LocStrings.Marker_Toggle);
			if (transform2 != null)
			{
				StripAnimatorsAndTransitions(transform2);
				Toggle component2 = transform2.GetComponent<Toggle>();
				if (component2 != null)
				{
					component2.onValueChanged.RemoveAllListeners();
					try
					{
						component2.transition = Selectable.Transition.None;
					}
					catch
					{
					}
					FindToggleChildren(transform2, out var offGo, out var onGo);
					try
					{
						component2.SetIsOnWithoutNotify(currentState);
					}
					catch
					{
						try
						{
							component2.isOn = currentState;
						}
						catch
						{
						}
					}
					ApplyToggleVisual(offGo, onGo, currentState);
					ToggleVisualEnforcer toggleVisualEnforcer = null;
					try
					{
						toggleVisualEnforcer = transform2.gameObject.GetComponent<ToggleVisualEnforcer>() ?? transform2.gameObject.AddComponent<ToggleVisualEnforcer>();
						toggleVisualEnforcer.Bind(offGo, onGo, currentState);
					}
					catch
					{
					}
					GameObject capturedOff = offGo;
					GameObject capturedOn = onGo;
					bool capturedInverted = invertedConfig;
					ToggleVisualEnforcer capturedEnforcer = toggleVisualEnforcer;
					System.Action capturedOnChanged = onChanged;
					try
					{
						component2.enabled = true;
						component2.interactable = true;
					}
					catch
					{
					}
					((UnityEvent<bool>)component2.onValueChanged).AddListener(DelegateSupport.ConvertDelegate<UnityAction<bool>>((System.Action<bool>)delegate(bool v)
					{
						try
						{
							config.Value = (capturedInverted ? (!v) : v);
							Defines.SaveSettings();
							GraphicsDon.OptimizeSetting();
							ApplyToggleVisual(capturedOff, capturedOn, v);
							capturedEnforcer?.SetValue(v);
							capturedOnChanged?.Invoke();
						}
						catch
						{
						}
					}));
				}
			}
			NormalizeToggleRowLayout(source);
		}
		catch
		{
		}
	}

	private static Transform CreateBind(Transform transform, string bindName, string buttonNameText)
	{
		if (transform == null)
		{
			return null;
		}
		try
		{
			transform.name = bindName;
			Transform transform2 = transform.Find(LocStrings.Marker_BindB);
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(value: false);
			}
			Transform transform3 = transform.Find(LocStrings.Marker_BindA);
			if (transform3 == null)
			{
				return transform;
			}
			RectTransform component = transform3.GetComponent<RectTransform>();
			if (component == null)
			{
				return transform;
			}
			component.offsetMin = new Vector2(component.offsetMin.x + 130f, component.offsetMin.y);
			component.offsetMax = new Vector2(component.offsetMax.x + 130f, component.offsetMax.y);
			Transform transform4 = transform.Find(LocStrings.Marker_Text);
			if (transform4 == null)
			{
				return transform;
			}
			StripLocalizers(transform4);
			Text component2 = transform4.GetComponent<Text>();
			if (component2 != null)
			{
				component2.text = buttonNameText;
			}
			KeyBindUI component3 = transform.GetComponent<KeyBindUI>();
			if (component3 != null)
			{
				component3.bindString = bindName;
			}
			string strCommand = string.Format(LocStrings.Cmd_BindNoneFmt, bindName);
			ConsoleSystem.Run(ConsoleSystem.Option.Client.Quiet(), strCommand);
			return transform;
		}
		catch
		{
			return transform;
		}
	}
}
