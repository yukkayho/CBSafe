using System;
using System.Linq;
using System.Reflection;
using CBSafe.Core.Patches;
using ConVar;
using HarmonyLib;
using Kino;
using Smaa;
using VLB;

namespace CBSafe.Fps;

public static class OptPatchInstaller
{
	public static void Install(HarmonyLib.Harmony harmony)
	{
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(AmbienceManager), "UpdateCullingGroups");
			if (methodInfo != null)
			{
				harmony.Patch(methodInfo, new HarmonyMethod(typeof(LodSafety), "AmbienceCull"));
			}
		}
		catch (Exception ex)
		{
			OptLog.LogWarning("AmbienceManager.UpdateCullingGroups safety: " + ex.Message);
		}
		try
		{
			MethodInfo methodInfo2 = AccessTools.Method(typeof(PlayerEyes), "DoFirstPersonCamera");
			if (methodInfo2 != null)
			{
				harmony.Patch(methodInfo2, null, new HarmonyMethod(typeof(EntityPatches), "FirstPersonCameraPost"));
			}
			MethodInfo methodInfo3 = AccessTools.Method(typeof(PlayerEyes), "DoThirdPersonCamera");
			if (methodInfo3 != null)
			{
				harmony.Patch(methodInfo3, null, new HarmonyMethod(typeof(EntityPatches), "ThirdPersonCameraPost"));
			}
			MethodInfo methodInfo4 = AccessTools.Method(typeof(PlayerEyes), "DoDeathCamera");
			if (methodInfo4 != null)
			{
				harmony.Patch(methodInfo4, null, new HarmonyMethod(typeof(EntityPatches), "ThirdPersonCameraPost"));
			}
		}
		catch (Exception ex2)
		{
			OptLog.LogWarning("PlayerEyes camera safety: " + ex2.Message);
		}
		Pref(harmony, FeatureId.Grass, typeof(FoliageRenderer), "OnEnable", typeof(EnvironmentPatches), "FoliageRendererOnEnable");
		Pref(harmony, FeatureId.Waves, typeof(WaterSystem), "UpdateOceanSimulation", typeof(EnvironmentPatches), "WaterOceanSim");
		Pref(harmony, FeatureId.Waves, typeof(WaterDynamics), "UpdateWaterDynamics", typeof(EnvironmentPatches), "WaterDynamicsUpdate");
		Pref(harmony, FeatureId.Probes, typeof(WaterSystem), "UpdateReflectionProbe", typeof(EnvironmentPatches), "WaterProbe");
		Pref(harmony, FeatureId.Probes, typeof(WaterSystem), "CheckReflectionProbe", typeof(EnvironmentPatches), "WaterCheckProbe");
		Pref(harmony, FeatureId.Probes, typeof(WaterSystem), "get_Reflections", typeof(EnvironmentPatches), "WaterReflectionsOff");
		Pref(harmony, FeatureId.Probes, typeof(TOD_Sky), "RenderToProbe", typeof(EnvironmentPatches), "TodProbe");
		Pref(harmony, FeatureId.Probes, typeof(TOD_Sky), "UpdateReflection", typeof(EnvironmentPatches), "TodReflection");
		Pref(harmony, FeatureId.Probes, typeof(ReflectionProbeEx), "RenderProbe", typeof(RenderPatches), "ProbeRender");
		Pref(harmony, FeatureId.Probes, typeof(ReflectionProbeEx), "OnCameraPreRender", typeof(RenderPatches), "ProbePreRender");
		Pref(harmony, FeatureId.Weather, typeof(RainEffect), "GetCurrentIntensity", typeof(EnvironmentPatches), "WeatherIntensity");
		Pref(harmony, FeatureId.Weather, typeof(SnowEffect), "GetCurrentIntensity", typeof(EnvironmentPatches), "WeatherIntensity");
		Pref(harmony, FeatureId.Weather, typeof(RainEffectSting), "GetCurrentIntensity", typeof(EnvironmentPatches), "WeatherIntensity");
		Pref(harmony, FeatureId.Weather, typeof(TOD_WeatherManager), "GetRainEmission", typeof(EnvironmentPatches), "WeatherIntensity");
		Pref(harmony, FeatureId.Weather, typeof(Weather), "get_wind", typeof(RenderPatches), "WeatherWind");
		Post(harmony, FeatureId.Weather, typeof(WeatherEffect), "InvokeUpdate", typeof(EnvironmentPatches), "WeatherAfterUpdate");
		Post(harmony, FeatureId.Weather, typeof(TOD_WeatherManager), "Update", typeof(EnvironmentPatches), "TodWeatherPost");
		Pref(harmony, FeatureId.SunRays, typeof(TOD_Rays), "OnRenderImage", typeof(EnvironmentPatches), "TodRays");
		Pref(harmony, FeatureId.Decals, typeof(DeferredDecalSystem), "AddDecal", typeof(RenderPatches), "AddDecal");
		Pref(harmony, FeatureId.Decals, typeof(DeferredDecalSystem), "RefreshDecal", typeof(RenderPatches), "RefreshDecal");
		Pref(harmony, FeatureId.Decals, typeof(DeferredMeshDecalRenderer), "FillCommandBuffer", typeof(RenderPatches), "DecalCmd");
		Pref(harmony, FeatureId.Decals, typeof(DeferredMeshDecalRenderer), "OnPreRender", typeof(RenderPatches), "DecalCmd");
		Pref(harmony, FeatureId.Lights, typeof(LightLOD), "GetMaxLightCount", typeof(RenderPatches), "MaxLights");
		Pref(harmony, FeatureId.PostProcess, typeof(WaterOverlay), "Update", typeof(RenderPatches), "WaterOverlayUpdate");
		Pref(harmony, FeatureId.Particles, typeof(LODComponentParticleSystem), "Show", typeof(RenderPatches), "ParticleShow");
		Pref(harmony, FeatureId.Particles, typeof(ParticleSystemLOD), "SetLOD", typeof(LodSafety), "ParticleSet");
		Pref(harmony, FeatureId.ParticleColl, typeof(ParticleCollisionLOD), "SetLOD", typeof(LodSafety), "ParticleCollisionSet");
		FeatureWatch.MarkRuntimeOnly(FeatureId.SkinUnload);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Workshop);
		FeatureWatch.MarkRuntimeOnly(FeatureId.SkinDeform);
		Pref(harmony, FeatureId.DistantAnims, typeof(PlayerModel), "OnAnimatorIK", typeof(EntityPatches), "AnimatorIk");
		Pref(harmony, FeatureId.DistantAnims, typeof(PlayerModel), "FootIK", typeof(EntityPatches), "FootIk");
		Pref(harmony, FeatureId.DistantAnims, typeof(PlayerModel), "HeadIK", typeof(EntityPatches), "HeadIk");
		Pref(harmony, FeatureId.DistantAnims, typeof(PlayerModel), "SpineIk", typeof(EntityPatches), "SpineIk");
		Pref(harmony, FeatureId.Cloth, typeof(ClothLOD), "EnableCloth", typeof(EntityPatches), "EnableCloth");
		Pref(harmony, FeatureId.Cloth, typeof(ClothLOD), "LODCheck", typeof(EntityPatches), "ClothLodCheck");
		Pref(harmony, FeatureId.Cloth, typeof(ClothWindModify), "DoWind", typeof(EntityPatches), "ClothWind");
		Pref(harmony, FeatureId.Corpse, typeof(Ragdoll), "SetUpPhysics", typeof(EntityPatches), "CorpsePhysics");
		Post(harmony, FeatureId.Corpse, typeof(Ragdoll), "OnDestroy", typeof(EntityPatches), "RagdollGone");
		Pref(harmony, FeatureId.Corpse, typeof(BaseCorpse), "PushRagdoll", typeof(EntityPatches), "PushRagdoll");
		Pref(harmony, FeatureId.Grass, typeof(FoliageGridBatch), "ApplyMesh", typeof(GpuPatches), "FoliageApplyMesh");
		Pref(harmony, FeatureId.Grass, typeof(FoliageGridBatch), "RefreshMesh", typeof(GpuPatches), "FoliageRefreshMesh");
		Pref(harmony, FeatureId.Grass, typeof(FoliageGridBatch), "ToggleMesh", typeof(GpuPatches), "FoliageToggleMesh");
		PrefAll(harmony, FeatureId.Gibs, typeof(Gibbable), "CreateGibs", typeof(GpuPatches), "CreateGibs");
		Pref(harmony, FeatureId.ImpostorSh, typeof(ImpostorShadows), "IssueDrawBatch", typeof(GpuPatches), "ImpostorShadowBatch");
		Post(harmony, FeatureId.Terrain, typeof(TerrainQuality), "Setup", typeof(GpuPatches), "TerrainSetup");
		Pref(harmony, FeatureId.Bloom, typeof(NaturalBloomAndDirtyLens), "OnRenderImage", typeof(CameraFxPatches), "BloomBlit");
		Pref(harmony, FeatureId.MotionBlur, typeof(Motion), "OnRenderImage", typeof(CameraFxPatches), "MotionBlit");
		Pref(harmony, FeatureId.Aa, typeof(FXAA), "OnRenderImage", typeof(CameraFxPatches), "AaBlit");
		Pref(harmony, FeatureId.Aa, typeof(SMAA), "OnRenderImage", typeof(CameraFxPatches), "AaBlit");
		Pref(harmony, FeatureId.ExplosionBloom, typeof(Explosion_Bloom), "OnRenderImage", typeof(CameraFxPatches), "ExplosionBlit");
		PrefByName(harmony, FeatureId.Planar, "PlanarReflectionCamera", "RenderReflectionPlane", typeof(CameraFxPatches), "PlanarRender");
		Pref(harmony, FeatureId.ContactShadows, typeof(SEScreenSpaceShadows), "OnPreRender", typeof(RenderPatches), "ContactPreRender");
		Pref(harmony, FeatureId.ContactShadows, typeof(SEScreenSpaceShadows), "OnPostRender", typeof(RenderPatches), "ContactPostRender");
		Pref(harmony, FeatureId.ContactShadows, typeof(SEScreenSpaceShadows), "ToggleScreenSpaceShadows", typeof(RenderPatches), "ContactToggle");
		Pref(harmony, FeatureId.Ssao, typeof(AmplifyOcclusionBase), "Update", typeof(RenderPatches), "AoUpdate");
		Pref(harmony, FeatureId.Ssao, typeof(AmplifyOcclusionBase), "OnPreRender", typeof(RenderPatches), "AoPreRender");
		Pref(harmony, FeatureId.Ssao, typeof(AmplifyOcclusionBase), "OnPostRender", typeof(RenderPatches), "AoPostRender");
		Pref(harmony, FeatureId.ParticleIk, typeof(ParticleSystemIK), "Update", typeof(CameraFxPatches), "ParticleIkUpdate");
		Pref(harmony, FeatureId.VolBeams, typeof(BeamGeometry), "OnWillRenderObject", typeof(CameraFxPatches), "BeamRender");
		Pref(harmony, FeatureId.VolBeams, typeof(VolumetricDustParticles), "Update", typeof(CameraFxPatches), "BeamDust");
		Pref(harmony, FeatureId.Decor, typeof(DecorSpawn), "OnEnable", typeof(RenderPatches), "DecorEnable");
		Pref(harmony, FeatureId.Decor, typeof(Decor), "get_quality", typeof(RenderPatches), "DecorQuality");
		Pref(harmony, FeatureId.Particles, typeof(Particle), "get_quality", typeof(RenderPatches), "ParticleQuality");
		Pref(harmony, FeatureId.Shadows, typeof(LightCloneShadow), "UpdateShadowClone", typeof(GpuPatches), "LightCloneShadow");
		Pref(harmony, FeatureId.Weather, typeof(WindZoneExManager), "Update", typeof(GpuPatches), "WindZoneUpdate");
		Pref(harmony, FeatureId.TreeWind, typeof(WindZoneExManager), "ComputeWindForceAtLocation", typeof(GpuPatches), "WindForce");
		Post(harmony, FeatureId.CheapTrees, typeof(TreeLOD), "GetLOD", typeof(GpuPatches), "TreeBillboard");
		Post(harmony, FeatureId.VmShadows, typeof(BaseViewModel), "OnEnable", typeof(EntityPatches), "VmNoShadows");
		Post(harmony, FeatureId.VmShadows, typeof(BaseViewModel), "UpdateRenderers", typeof(EntityPatches), "VmNoShadows");
		Post(harmony, FeatureId.VmShadows, typeof(BaseViewModel), "UpdateClothingModels", typeof(GlovesSkinApplier), "OnClothingUpdated");
		Post(harmony, FeatureId.VmShadows, typeof(BaseViewModel), "UpdateRenderersAndSkin", typeof(GlovesSkinApplier), "OnUpdateRenderers");
		Post(harmony, FeatureId.VmShadows, typeof(BaseViewModel), "OnClothingChanged", typeof(GlovesSkinApplier), "OnClothingChanged");
		Pref(harmony, FeatureId.DamageOverlay, typeof(HurtOverlay), "GetIntensity", typeof(RenderPatches), "HurtIntensity");
		Pref(harmony, FeatureId.PostProcess, typeof(OutlineManager), "OnRenderImage", typeof(CameraFxPatches), "OutlineBlit");
		Pref(harmony, FeatureId.WorldLod, typeof(Graphics), "get_Hlod", typeof(RenderPatches), "HlodOn");
		Pref(harmony, FeatureId.WorldLod, typeof(Tree), "get_quality", typeof(RenderPatches), "TreeQuality");
		Pref(harmony, FeatureId.WorldLod, typeof(Tree), "get_meshes", typeof(RenderPatches), "TreeMeshes");
		Pref(harmony, FeatureId.WorldLod, typeof(Mesh), "get_quality", typeof(RenderPatches), "MeshQuality");
		Pref(harmony, FeatureId.Grass, typeof(Grass), "get_quality", typeof(RenderPatches), "GrassQuality");
		PrefByName(harmony, FeatureId.HeatHaze, "HeatHaze", "OnRenderImage", typeof(CameraFxPatches), "HeatBlit");
		PrefByName(harmony, FeatureId.HeatHaze, "HeatDistort", "OnRenderImage", typeof(CameraFxPatches), "HeatBlit");
		PrefByName(harmony, FeatureId.LinearFog, "LinearFog", "OnRenderImage", typeof(CameraFxPatches), "LinearFogBlit");
		Pref(harmony, FeatureId.ImpostorSh, typeof(Graphics), "get_impostorshadows", typeof(RenderPatches), "ImpostorShadowsOff");
		Pref(harmony, FeatureId.Waves, typeof(Graphics), "get_waves", typeof(RenderPatches), "WavesOff");
		Pref(harmony, FeatureId.PointFilter, typeof(Graphics), "get_parallax", typeof(RenderPatches), "ParallaxOff");
		Pref(harmony, FeatureId.PointFilter, typeof(Graphics), "get_af", typeof(RenderPatches), "AnisoOff");
		Pref(harmony, FeatureId.PostProcess, typeof(HurtOverlay), "Update", typeof(RenderPatches), "OverlaySkip");
		Pref(harmony, FeatureId.PostProcess, typeof(ColdOverlay), "Update", typeof(RenderPatches), "ColdOverlayUpdate");
		Pref(harmony, FeatureId.PostProcess, typeof(HotOverlay), "Update", typeof(RenderPatches), "OverlaySkip");
		Pref(harmony, FeatureId.PostProcess, typeof(RadiationOverlay), "Update", typeof(RenderPatches), "OverlaySkip");
		Pref(harmony, FeatureId.Shake, typeof(ViewShake), "AddShake", typeof(EntityPatches), "ShakeAdd");
		PrefAll(harmony, FeatureId.Shake, typeof(BaseScreenShake), "Run", typeof(EntityPatches), "ShakeRun");
		PrefByName(harmony, FeatureId.Shake, "AnimatedScreenShake", "Run", typeof(EntityPatches), "ShakeRun");
		Pref(harmony, FeatureId.GrassDisp, typeof(FoliageDisplacement), "OnEnable", typeof(GpuPatches), "FoliageDispOff");
		Pref(harmony, FeatureId.GrassDisp, typeof(FoliageDisplacement), "ChangeLOD", typeof(GpuPatches), "FoliageDispOff");
		Pref(harmony, FeatureId.AutoMemory, typeof(System.GC), "collect", typeof(RenderPatches), "GcCollectOff");
		Pref(harmony, FeatureId.AutoMemory, typeof(System.GC), "Collect", typeof(RenderPatches), "GcCollectOff");
		FeatureWatch.MarkRuntimeOnly(FeatureId.SmartShadows);
		FeatureWatch.MarkRuntimeOnly(FeatureId.AutoMemory);
		FeatureWatch.MarkRuntimeOnly(FeatureId.SkinDeform);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Sky);
		FeatureWatch.MarkRuntimeOnly(FeatureId.PointFilter);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Textures);
		FeatureWatch.MarkRuntimeOnly(FeatureId.CheapCam);
		FeatureWatch.MarkRuntimeOnly(FeatureId.WorldLod);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Instancing);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Culling);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Sss);
		FeatureWatch.MarkRuntimeOnly(FeatureId.ContactShadows);
		FeatureWatch.MarkRuntimeOnly(FeatureId.GrassLod);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Atmosphere);
		FeatureWatch.MarkRuntimeOnly(FeatureId.LinearFog);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Dof);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Sharpen);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Ssao);
		FeatureWatch.MarkRuntimeOnly(FeatureId.VmShadows);
		FeatureWatch.MarkRuntimeOnly(FeatureId.DamageOverlay);
		FeatureWatch.MarkRuntimeOnly(FeatureId.CheapTrees);
		FeatureWatch.MarkRuntimeOnly(FeatureId.GrassDisp);
		FeatureWatch.MarkRuntimeOnly(FeatureId.Wetness);
		FeatureWatch.MarkRuntimeOnly(FeatureId.WaterFoam);
		FeatureWatch.MarkRuntimeOnly(FeatureId.HeatHaze);
		FeatureWatch.MarkRuntimeOnly(FeatureId.AnimalPhysics);
		FeatureWatch.MarkRuntimeOnly(FeatureId.LimitCorpses);
		FeatureWatch.MarkRuntimeOnly(FeatureId.CheapShaders);
	}

	private static void PrefByName(HarmonyLib.Harmony harmony, FeatureId id, string typeName, string method, Type patch, string patchMethod)
	{
		Type type = AccessTools.TypeByName(typeName);
		if (!(type == null))
		{
			Pref(harmony, id, type, method, patch, patchMethod);
		}
	}

	private static void PrefAll(HarmonyLib.Harmony harmony, FeatureId id, Type game, string method, Type patch, string patchMethod)
	{
		MethodInfo[] array = (from m in AccessTools.GetDeclaredMethods(game)
			where m.Name == method
			select m).ToArray();
		if (array.Length == 0)
		{
			FeatureWatch.MarkHook(id, ok: false, "no method " + game.Name + "." + method);
			return;
		}
		int num = 0;
		string error = null;
		MethodInfo[] array2 = array;
		foreach (MethodInfo original in array2)
		{
			try
			{
				harmony.Patch(original, new HarmonyMethod(patch, patchMethod));
				num++;
			}
			catch (Exception ex)
			{
				error = ex.GetType().Name + ": " + ex.Message;
			}
		}
		FeatureWatch.MarkHook(id, num > 0, error);
	}

	private static void Pref(HarmonyLib.Harmony harmony, FeatureId id, Type game, string method, Type patch, string patchMethod)
	{
		Hook(harmony, id, game, method, new HarmonyMethod(patch, patchMethod));
	}

	private static void Post(HarmonyLib.Harmony harmony, FeatureId id, Type game, string method, Type patch, string patchMethod)
	{
		Hook(harmony, id, game, method, null, new HarmonyMethod(patch, patchMethod));
	}

	private static void Hook(HarmonyLib.Harmony harmony, FeatureId id, Type type, string method, HarmonyMethod prefix = null, HarmonyMethod postfix = null)
	{
		try
		{
			MethodInfo methodInfo = AccessTools.Method(type, method);
			if (methodInfo == null && type.BaseType != null)
			{
				methodInfo = AccessTools.Method(type.BaseType, method);
			}
			if (methodInfo == null)
			{
				FeatureWatch.MarkHook(id, ok: false, "no method " + type.Name + "." + method);
				return;
			}
			harmony.Patch(methodInfo, prefix, postfix);
			FeatureWatch.MarkHook(id, ok: true);
		}
		catch (Exception ex)
		{
			FeatureWatch.MarkHook(id, ok: false, ex.GetType().Name + ": " + ex.Message);
		}
	}
}
