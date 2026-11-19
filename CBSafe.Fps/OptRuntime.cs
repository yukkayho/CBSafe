using System;
using ConVar;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CBSafe.Fps;

public sealed class OptRuntime : MonoBehaviour
{
	public static bool ForceApply;

	private bool _snapReady;

	private ShadowQuality _shadows;

	private int _cascades;

	private float _shadowDistance;

	private AnisotropicFiltering _aniso;

	private int _mipLimit;

	private int _pixelLights;

	private int _particleRaycasts;

	private SkinWeights _skinWeights;

	private int _aa;

	private float _lodBias;

	private int _maxLod;

	private ShadowResolution _shadowRes;

	private float _terrainError = 5f;

	private float _treeDist = 2000f;

	private float _treeBillboard = 250f;

	private float _treeFade = 20f;

	private int _treeFullLod = 50;

	private float _detailDist = 80f;

	private float _baseMapDist = 200f;

	private float _farClip = 800f;

	private float[] _layerCull;

	private bool _camSnap;

	private bool _terrainSnap;

	private bool _decorOff;

	private int _instancedSnap = -1;

	private bool _toggleAllSnap = true;

	private bool _cliffsSnap = true;

	private bool _buildingsSnap = true;

	private bool _otherSnap = true;

	private bool _multidrawSnap;

	private bool? _instancingOn;

	private int _vsync;

	private int _convarVsync;

	private int _targetFps = -1;

	private bool? _pauseWant;

	private bool? _grassHidden;

	private bool _skyReady;

	private bool _gcApplied;

	private bool? _wasNight;

	private bool _probes;

	private bool _allowMsaa;

	private bool _allowMsaaSnap;

	private int _waterReflections;

	private bool _planar = true;

	private float _nextForceAt;

	private bool _convarSnap;

	private float _cullMax;

	private float _cullMin;

	private float _cullAnim;

	private float _cullShadow;

	private int _maxGibs;

	private bool _flares;

	private bool _grassShadows;

	private bool _contactShadows;

	private bool _grassDisp;

	private float _wetRain;

	private float _wetSnow;

	private int _waterQuality;

	private bool _dof;

	private bool _sssEnabled;

	private int _gcBuffer;

	private bool _gcSnapReady;

	private bool _effectsAo;

	private bool _effectsBloom;

	private bool _effectsLensDirt;

	private bool _effectsMotionBlur;

	private bool _effectsSharpen;

	private bool _effectsShafts;

	private bool _effectsVignet;

	private int _effectsAa;

	private bool _effectsSnapReady;

	private int _shaderLod;

	private float _treeMeshDist;

	private float _lodRefresh;

	public OptRuntime(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		bool flag = false;
		try
		{
			flag = LocalPlayer.Entity == null;
		}
		catch
		{
			flag = true;
		}
		if (flag && !ForceApply)
		{
			if (!_snapReady)
			{
				Snapshot();
			}
			return;
		}
		if (!_snapReady)
		{
			Snapshot();
			ForceApply = true;
		}
		OptDark.Tick();
		if (_wasNight != OptDark.Night)
		{
			_wasNight = OptDark.Night;
			ForceApply = true;
		}
		if (!ForceApply)
		{
			if (!_gcApplied)
			{
				ApplyGc();
			}
			_ = UnityEngine.Time.frameCount;
			return;
		}
		float unscaledTime = UnityEngine.Time.unscaledTime;
		if (!(unscaledTime < _nextForceAt))
		{
			_nextForceAt = unscaledTime + 0.35f;
			ForceApply = false;
			ApplyQuality();
			ApplyFoliagePause();
			ApplySky();
			ApplyCameras();
			ApplyDecor();
			ApplyInstancing();
			ApplyConvars();
			ApplyGc();
		}
	}

	private void Snapshot()
	{
		_shadows = QualitySettings.shadows;
		_cascades = QualitySettings.shadowCascades;
		_shadowDistance = QualitySettings.shadowDistance;
		_aniso = QualitySettings.anisotropicFiltering;
		_mipLimit = QualitySettings.masterTextureLimit;
		_pixelLights = QualitySettings.pixelLightCount;
		_particleRaycasts = QualitySettings.particleRaycastBudget;
		_skinWeights = QualitySettings.skinWeights;
		_aa = QualitySettings.antiAliasing;
		_lodBias = QualitySettings.lodBias;
		_maxLod = QualitySettings.maximumLODLevel;
		_shadowRes = QualitySettings.shadowResolution;
		_vsync = QualitySettings.vSyncCount;
		_targetFps = Application.targetFrameRate;
		try
		{
			_convarVsync = ConVar.Graphics.vsync;
		}
		catch
		{
			_convarVsync = 0;
		}
		_probes = QualitySettings.realtimeReflectionProbes;
		SnapTerrain();
		_snapReady = true;
	}

	private void SnapTerrain()
	{
		UnityEngine.Terrain activeTerrain = UnityEngine.Terrain.activeTerrain;
		if (!(activeTerrain == null))
		{
			_terrainError = activeTerrain.heightmapPixelError;
			_treeDist = activeTerrain.treeDistance;
			_treeBillboard = activeTerrain.treeBillboardDistance;
			_treeFade = activeTerrain.treeCrossFadeLength;
			_treeFullLod = activeTerrain.treeMaximumFullLODCount;
			_detailDist = activeTerrain.detailObjectDistance;
			_baseMapDist = activeTerrain.basemapDistance;
			_terrainSnap = true;
		}
	}

	private void ApplyQuality()
	{
		int num = _cascades;
		float num2 = _shadowDistance;
		ShadowResolution shadowResolution = _shadowRes;
		ShadowQuality shadowQuality;
		if (OptDark.Night)
		{
			shadowQuality = _shadows;
		}
		else if (OptState.DisableShadows)
		{
			shadowQuality = ShadowQuality.Disable;
		}
		else if (OptState.SmartShadows)
		{
			shadowQuality = ((_shadows == ShadowQuality.Disable) ? ShadowQuality.HardOnly : _shadows);
			num = Math.Min(_cascades, 2);
			num2 = Math.Min(_shadowDistance, 80f);
			shadowResolution = ShadowResolution.Low;
		}
		else
		{
			shadowQuality = _shadows;
		}
		if (QualitySettings.shadows != shadowQuality)
		{
			QualitySettings.shadows = shadowQuality;
		}
		if (QualitySettings.shadowCascades != num)
		{
			QualitySettings.shadowCascades = num;
		}
		if (QualitySettings.shadowDistance != num2)
		{
			QualitySettings.shadowDistance = num2;
		}
		if (QualitySettings.shadowResolution != shadowResolution)
		{
			QualitySettings.shadowResolution = shadowResolution;
		}
		bool flag = Defines.OptimizeGraphicCached && !OptDark.Night;
		int num3 = (OptDark.Night ? _mipLimit : Math.Clamp(_mipLimit + Math.Max(0, OptState.TextureDownsample) + (flag ? 2 : 0), 0, 3));
		if (QualitySettings.masterTextureLimit != num3)
		{
			QualitySettings.masterTextureLimit = num3;
		}
		AnisotropicFiltering anisotropicFiltering = ((!OptState.PointFilterTextures && !flag) ? _aniso : AnisotropicFiltering.Disable);
		if (QualitySettings.anisotropicFiltering != anisotropicFiltering)
		{
			QualitySettings.anisotropicFiltering = anisotropicFiltering;
		}
		int num4 = (OptDark.Night ? _pixelLights : ((!flag) ? ((OptState.MaxDynamicLights > 0) ? Math.Min(_pixelLights, Math.Max(1, OptState.MaxDynamicLights)) : 0) : 0));
		if (QualitySettings.pixelLightCount != num4)
		{
			QualitySettings.pixelLightCount = num4;
		}
		int num5 = ((!flag && !OptDark.On(OptState.ReduceParticleCollision)) ? _particleRaycasts : 0);
		if (QualitySettings.particleRaycastBudget != num5)
		{
			QualitySettings.particleRaycastBudget = num5;
		}
		if (QualitySettings.skinWeights != _skinWeights)
		{
			QualitySettings.skinWeights = _skinWeights;
		}
		int num6 = ((!OptDark.On(OptState.CheapCamera) && !OptDark.On(OptState.DisableAa) && !flag) ? _aa : 0);
		if (QualitySettings.antiAliasing != num6)
		{
			QualitySettings.antiAliasing = num6;
		}
		float num7 = (OptState.AggressiveWorldLod ? Math.Min(_lodBias, 0.72f) : _lodBias);
		if (QualitySettings.lodBias != num7)
		{
			QualitySettings.lodBias = num7;
		}
		if (QualitySettings.maximumLODLevel != _maxLod)
		{
			QualitySettings.maximumLODLevel = _maxLod;
		}
		bool flag2 = !OptDark.On(OptState.DisableReflectionProbes) && _probes;
		if (QualitySettings.realtimeReflectionProbes != flag2)
		{
			QualitySettings.realtimeReflectionProbes = flag2;
		}
		bool flag3 = !OptState.DisableGrass;
		if (QualitySettings.softVegetation != flag3)
		{
			QualitySettings.softVegetation = flag3;
		}
		ApplyTerrains();
	}

	private void ApplyTerrains()
	{
		if (!_terrainSnap)
		{
			SnapTerrain();
		}
		Il2CppReferenceArray<UnityEngine.Terrain> activeTerrains = UnityEngine.Terrain.activeTerrains;
		if (activeTerrains == null)
		{
			return;
		}
		bool flag = OptState.CheapTerrain || OptState.AggressiveWorldLod;
		float heightmapPixelError = (flag ? Math.Max(_terrainError, 28f) : _terrainError);
		for (int i = 0; i < ((Il2CppArrayBase<UnityEngine.Terrain>)activeTerrains).Length; i++)
		{
			UnityEngine.Terrain terrain = ((Il2CppArrayBase<UnityEngine.Terrain>)activeTerrains)[i];
			if (!(terrain == null))
			{
				terrain.heightmapPixelError = heightmapPixelError;
				terrain.basemapDistance = (flag ? Math.Min(_baseMapDist, 80f) : _baseMapDist);
				terrain.detailObjectDistance = (OptState.DisableGrass ? 0f : _detailDist);
				if (OptState.CheapTrees || OptState.AggressiveWorldLod)
				{
					bool cheapTrees = OptState.CheapTrees;
					terrain.treeDistance = Math.Min(_treeDist, cheapTrees ? 120f : 180f);
					terrain.treeBillboardDistance = Math.Min(_treeBillboard, cheapTrees ? 22f : 45f);
					terrain.treeCrossFadeLength = Math.Min(_treeFade, cheapTrees ? 2f : 5f);
					terrain.treeMaximumFullLODCount = Math.Min(_treeFullLod, (!cheapTrees) ? 8 : 0);
				}
				else
				{
					terrain.treeDistance = _treeDist;
					terrain.treeBillboardDistance = _treeBillboard;
					terrain.treeCrossFadeLength = _treeFade;
					terrain.treeMaximumFullLODCount = _treeFullLod;
				}
			}
		}
	}

	private void ApplyCameras()
	{
		Camera main = Camera.main;
		if (main == null)
		{
			return;
		}
		if (!_camSnap)
		{
			_farClip = main.farClipPlane;
			_layerCull = new float[32];
			try
			{
				Il2CppStructArray<float> layerCullDistances = main.layerCullDistances;
				if (layerCullDistances != null)
				{
					int num = Math.Min(32, ((Il2CppArrayBase<float>)layerCullDistances).Length);
					for (int i = 0; i < num; i++)
					{
						_layerCull[i] = ((Il2CppArrayBase<float>)layerCullDistances)[i];
					}
				}
			}
			catch
			{
			}
			_camSnap = true;
		}
		if (!_allowMsaaSnap)
		{
			_allowMsaa = main.allowMSAA;
			_allowMsaaSnap = true;
		}
		main.allowMSAA = (OptDark.Night ? _allowMsaa : (!OptState.CheapCamera && !OptState.DisableAa && !Defines.OptimizeGraphicCached));
		main.farClipPlane = _farClip;
		if (_layerCull != null)
		{
			main.layerCullDistances = _layerCull;
		}
	}

	private void ApplyDecor()
	{
		bool disableDecor = OptState.DisableDecor;
		if (disableDecor == _decorOff)
		{
			return;
		}
		try
		{
			if (disableDecor)
			{
				DecorSpawn.SetEnabled(b: false);
				_decorOff = true;
				FeatureWatch.Skip(FeatureId.Decor);
				return;
			}
			DecorSpawn.SetEnabled(b: true);
			try
			{
				DecorSpawn.RefreshAll(force: true);
			}
			catch
			{
			}
			_decorOff = false;
		}
		catch (Exception ex)
		{
			FeatureWatch.All[FeatureId.Decor].Runtime = "WAIT " + ex.GetType().Name;
		}
	}

	private void ApplyConvars()
	{
		if (Camera.main == null)
		{
			return;
		}
		try
		{
			if (!_convarSnap)
			{
				_cullMax = Culling.entityMaxDist;
				_cullMin = Culling.entityMinCullDist;
				_cullAnim = Culling.entityMinAnimatorCullDist;
				_cullShadow = Culling.entityMinShadowCullDist;
				_maxGibs = Effects.maxgibs;
				_flares = Effects.otherplayerslightflares;
				_grassShadows = ConVar.Graphics.grassshadows;
				_contactShadows = ConVar.Graphics.contactshadows;
				_grassDisp = Grass.displacement;
				_wetRain = Weather.wetness_rain;
				_wetSnow = Weather.wetness_snow;
				_waterQuality = Water.quality;
				_waterReflections = Water.reflections;
				_dof = ConVar.Graphics.dof;
				_sssEnabled = SSS.enabled;
				_gcBuffer = ConVar.GC.buffer;
				_gcSnapReady = true;
				_effectsAo = Effects.ao;
				_effectsBloom = Effects.bloom;
				_effectsLensDirt = Effects.lensdirt;
				_effectsMotionBlur = Effects.motionblur;
				_effectsSharpen = Effects.sharpen;
				_effectsShafts = Effects.shafts;
				_effectsVignet = Effects.vignet;
				_effectsAa = Effects.antialiasing;
				_effectsSnapReady = true;
				try
				{
					_planar = OptConvarBridge.GetBool("Reflection", "planarReflections", fallback: true);
				}
				catch
				{
					_planar = true;
				}
				_shaderLod = ConVar.Graphics.shaderlod;
				_treeMeshDist = LODGrid.TreeMeshDistance;
				_lodRefresh = LODGrid.MaxRefreshDistance;
				_convarSnap = true;
			}
			if (OptState.AggressiveCulling)
			{
				Culling.entityMaxDist = 700f;
				Culling.entityMinCullDist = 28f;
				Culling.entityMinAnimatorCullDist = 22f;
				Culling.entityMinShadowCullDist = 45f;
				FeatureWatch.Skip(FeatureId.Culling);
				FeatureWatch.All[FeatureId.Culling].Runtime = "OK entityMax=700";
			}
			else
			{
				Culling.entityMaxDist = _cullMax;
				Culling.entityMinCullDist = _cullMin;
				Culling.entityMinAnimatorCullDist = _cullAnim;
				Culling.entityMinShadowCullDist = _cullShadow;
				FeatureWatch.All[FeatureId.Culling].Runtime = "OFF";
			}
			Effects.maxgibs = ((!OptState.DisableGibs) ? _maxGibs : 0);
			Effects.otherplayerslightflares = !OptState.DisableSecondaryParticles && _flares;
			ConVar.Graphics.grassshadows = !OptState.DisableGrass && !OptDark.On(OptState.DisableShadows || OptState.SmartShadows) && _grassShadows;
			ConVar.Graphics.contactshadows = !OptDark.On(OptState.DisableShadows) && _contactShadows;
			Grass.displacement = !OptState.DisableGrassDisp && !OptState.DisableGrass && _grassDisp;
			Weather.wetness_rain = (OptDark.On(OptState.DisableWetness) ? 0f : _wetRain);
			Weather.wetness_snow = (OptDark.On(OptState.DisableWetness) ? 0f : _wetSnow);
			bool flag = Defines.OptimizeGraphicCached && !OptDark.Night;
			Water.quality = ((!OptDark.On(OptState.DisableWaterFoam) && !flag) ? _waterQuality : 0);
			Water.reflections = ((!(OptDark.On(OptState.DisableReflectionProbes) || flag)) ? _waterReflections : 0);
			try
			{
				OptConvarBridge.SetBool("Reflection", "planarReflections", !OptDark.On(OptState.DisablePlanarReflections) && _planar);
			}
			catch
			{
			}
			ConVar.Graphics.shaderlod = ((OptDark.On(OptState.CheapShaders) || flag) ? Math.Min(_shaderLod, flag ? 100 : 200) : _shaderLod);
			ConVar.Graphics.dof = !(OptDark.On(OptState.DisableDof) || flag) && _dof;
			SSS.enabled = !(OptDark.On(OptState.DisableSss) || flag) && _sssEnabled;
			if (_effectsSnapReady)
			{
				Effects.ao = !(OptDark.On(OptState.DisableSsao) || flag) && _effectsAo;
				Effects.bloom = !(OptDark.On(OptState.DisableBloom) || flag) && _effectsBloom;
				Effects.lensdirt = !(OptDark.On(OptState.DisableHeavyPostProcess) || flag) && _effectsLensDirt;
				Effects.motionblur = !(OptDark.On(OptState.DisableMotionBlur) || flag) && _effectsMotionBlur;
				Effects.sharpen = !(OptDark.On(OptState.DisableSharpen) || flag) && _effectsSharpen;
				Effects.shafts = !(OptDark.On(OptState.DisableSunRays) || flag) && _effectsShafts;
				Effects.vignet = !(OptDark.On(OptState.DisableHeavyPostProcess) || flag) && _effectsVignet;
				Effects.antialiasing = ((!(OptDark.On(OptState.DisableAa) || OptDark.On(OptState.CheapCamera) || flag)) ? _effectsAa : 0);
			}
			if (OptState.DisableGrassDisp || OptState.DisableGrass)
			{
				FeatureWatch.Skip(FeatureId.GrassDisp);
			}
			if (OptDark.On(OptState.DisableWetness))
			{
				FeatureWatch.Skip(FeatureId.Wetness);
			}
			if (OptDark.On(OptState.DisableWaterFoam))
			{
				FeatureWatch.Skip(FeatureId.WaterFoam);
			}
			if (OptState.AggressiveWorldLod)
			{
				LODGrid.TreeMeshDistance = Math.Min(_treeMeshDist, 70f);
				LODGrid.MaxRefreshDistance = Math.Min(_lodRefresh, 220f);
			}
			else
			{
				LODGrid.TreeMeshDistance = _treeMeshDist;
				LODGrid.MaxRefreshDistance = _lodRefresh;
			}
		}
		catch (Exception ex)
		{
			FeatureWatch.All[FeatureId.Culling].Runtime = "WAIT " + ex.GetType().Name;
		}
	}

	private void ApplyInstancing()
	{
		if (_instancingOn == OptState.EnableInstancing)
		{
			return;
		}
		try
		{
			if (_instancedSnap < 0)
			{
				_instancedSnap = OptConvarBridge.GetInt("Render", "instanced_rendering", -1);
				_toggleAllSnap = OptConvarBridge.GetBool("Render", "instanced_toggle_all", fallback: true);
				_cliffsSnap = OptConvarBridge.GetBool("Render", "instanced_toggle_cliffs", fallback: true);
				_buildingsSnap = OptConvarBridge.GetBool("Render", "instanced_toggle_buildings", fallback: true);
				_otherSnap = OptConvarBridge.GetBool("Render", "instanced_toggle_other", fallback: true);
				_multidrawSnap = OptConvarBridge.GetBool("Render", "multidraw", fallback: false);
			}
			if (OptState.EnableInstancing)
			{
				FeatureWatch.All[FeatureId.Instancing].Runtime = "SKIP no force (FPS safe)";
			}
			else
			{
				if (_instancedSnap >= 0)
				{
					OptConvarBridge.SetInt("Render", "instanced_rendering", _instancedSnap);
					OptConvarBridge.SetBool("Render", "instanced_toggle_all", _toggleAllSnap);
					OptConvarBridge.SetBool("Render", "instanced_toggle_cliffs", _cliffsSnap);
					OptConvarBridge.SetBool("Render", "instanced_toggle_buildings", _buildingsSnap);
					OptConvarBridge.SetBool("Render", "instanced_toggle_other", _otherSnap);
					OptConvarBridge.SetBool("Render", "multidraw", _multidrawSnap);
				}
				FeatureWatch.All[FeatureId.Instancing].Runtime = "OFF";
			}
			_instancingOn = OptState.EnableInstancing;
		}
		catch (Exception ex)
		{
			FeatureWatch.All[FeatureId.Instancing].Runtime = "WAIT " + ex.GetType().Name;
		}
	}

	private void ApplyGc()
	{
		try
		{
			if (!_gcSnapReady)
			{
				_gcBuffer = ConVar.GC.buffer;
				_gcSnapReady = true;
			}
			int num = Math.Max(256, _gcBuffer);
			if (OptState.AutoMemory)
			{
				num = Math.Max(num, 4096);
			}
			if (ConVar.GC.buffer != num)
			{
				ConVar.GC.buffer = num;
			}
			FeatureWatch.All[FeatureId.AutoMemory].Runtime = "OK buffer=" + ConVar.GC.buffer;
			_gcApplied = true;
		}
		catch (Exception ex)
		{
			FeatureWatch.All[FeatureId.AutoMemory].Runtime = "WAIT " + ex.GetType().Name;
		}
	}

	private void ApplyFoliagePause()
	{
		try
		{
			bool disableGrass = OptState.DisableGrass;
			if (_grassHidden == disableGrass)
			{
				FoliageGrid.Paused = disableGrass;
				_pauseWant = disableGrass;
				return;
			}
			if (disableGrass)
			{
				FoliageGrid.Paused = true;
			}
			else
			{
				FoliageGrid.Paused = false;
				try
				{
					FoliageGrid.RefreshAll();
				}
				catch
				{
				}
			}
			_grassHidden = disableGrass;
			_pauseWant = disableGrass;
		}
		catch
		{
		}
	}

	private void ApplySky()
	{
		TOD_Sky tOD_Sky = null;
		try
		{
			tOD_Sky = TOD_Sky.Instance;
		}
		catch
		{
			_skyReady = false;
			return;
		}
		if (tOD_Sky == null || tOD_Sky.Components == null || !tOD_Sky.Initialized)
		{
			_skyReady = false;
			return;
		}
		TOD_Components components = tOD_Sky.Components;
		SetGo(components.Clouds, !OptDark.On(OptState.DisableSky));
		SetGo(components.Stars, !OptDark.On(OptState.DisableSky));
		SetGo(components.Billboards, !OptDark.On(OptState.DisableSky));
		SetGo(components.Space, on: true);
		SetGo(components.Atmosphere, on: true);
		SetGo(components.Clear, on: true);
		if (components.Rays != null)
		{
			components.Rays.enabled = !OptDark.On(OptState.DisableSunRays);
		}
		if (components.Scattering != null)
		{
			components.Scattering.enabled = true;
		}
		if (components.Shadows != null)
		{
			components.Shadows.enabled = true;
		}
		_skyReady = true;
	}

	private static void SetGo(GameObject go, bool on)
	{
		if (go != null && go.activeSelf != on)
		{
			go.SetActive(on);
		}
	}
}
