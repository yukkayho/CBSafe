using CBSafe.Colors;
using CBSafe.Colors.Patches;
using CBSafe.Core;
using CBSafe.Core.Patches;
using CBSafe.Crosshair.Infrastructure;
using CBSafe.Fps;
using ConVar;
using UnityEngine;

namespace CBSafe.Patches;

[HydraNoObfuscate]
internal class GraphicsDon : MonoBehaviour
{
	private const float RamCleanIntervalSeconds = 1800f;

	private static GraphicsDon _instance;

	private float _nextRamCleanAt;

	private float _nextVersionReporterAt;

	private float _nextConfigDrainAt;

	private float _holoPulseUntil;

	private float _nextHoloPulseAt;

	private float _nextStretchApplyAt;

	private float _nextCacheUpdateAt;

	private float _nextGloveCheckAt;

	private bool _graphicCutsApplied;

	private bool _graphicSnapshotReady;

	private float _graphicTerrainQuality;

	private float _graphicTreeQuality;

	private int _graphicWaterQuality;

	private int _graphicWaterReflections;

	private float _graphicParticleQuality;

	private float _graphicDrawDistance;

	private int _graphicShowTexelDensity;

	private int _graphicQuality;

	private float _graphicLodBias;

	private int _graphicMipLimit;

	private int _graphicAniso;

	private int _graphicAf;

	private int _graphicPixelLights;

	private int _graphicParticleRaycasts;

	private int _graphicShaderLod;

	private int _graphicTreeMeshes;

	private float _graphicMeshQuality;

	private float _graphicDecorQuality;

	private bool _graphicSustainPedal;

	private bool _graphicContactShadows;

	private int _graphicParallax;

	private bool _graphicDof;

	private bool _graphicSss;

	private bool _grassSnapshotReady;

	private float _grassQuality;

	private float _grassQuality01;

	private bool _grassShadows;

	private bool _shadowSnapshotReady;

	private int _shadowCascades;

	private float _shadowDistance;

	private int _shadowMode;

	private int _shadowQuality;

	private int _shadowLights;

	internal static void ArmHoloBootstrapPulse()
	{
		if (!(_instance == null))
		{
			float unscaledTime = UnityEngine.Time.unscaledTime;
			_instance._holoPulseUntil = unscaledTime + 180f;
			_instance._nextHoloPulseAt = 0f;
		}
	}

	private void OnEnable()
	{
		_instance = this;
		_holoPulseUntil = UnityEngine.Time.unscaledTime + 180f;
		_nextHoloPulseAt = 0f;
		ResetRamSchedule();
		Defines.LoadSettings();
		try
		{
			UpdateFastAlt();
		}
		catch
		{
		}
		OptimizeSetting();
		ApplyStretch();
	}

	private void OnApplicationQuit()
	{
		try
		{
			CrosshairConfigStore.PrepareForPersist();
		}
		catch
		{
		}
		try
		{
			CrosshairConfigStore.PersistNow();
		}
		catch
		{
		}
		try
		{
			SharedConfig.FlushNow();
		}
		catch
		{
		}
	}

	private void OnDestroy()
	{
		try
		{
			CrosshairConfigStore.PrepareForPersist();
		}
		catch
		{
		}
		try
		{
			CrosshairConfigStore.PersistNow();
		}
		catch
		{
		}
		try
		{
			SharedConfig.FlushNow();
		}
		catch
		{
		}
	}

	private void Update()
	{
		float unscaledTime = UnityEngine.Time.unscaledTime;
		if (unscaledTime >= _nextVersionReporterAt)
		{
			_nextVersionReporterAt = unscaledTime + 5f;
			try
			{
				CBSafeVersionReporter.Tick();
			}
			catch
			{
			}
		}
		if (unscaledTime >= _nextCacheUpdateAt)
		{
			_nextCacheUpdateAt = unscaledTime + 1f;
			Defines.UpdateCache();
			if (Defines.OptimizeGraphicCached != _graphicCutsApplied)
			{
				OptimizeSetting();
			}
		}
		if (SharedConfig.IsDirty && unscaledTime >= _nextConfigDrainAt)
		{
			_nextConfigDrainAt = unscaledTime + 0.5f;
			try
			{
				SharedConfig.DrainPendingSave();
			}
			catch
			{
			}
		}
		if (CrosshairConfigStore.HasInputBlockPending)
		{
			try
			{
				CrosshairConfigStore.TickInputBlock();
			}
			catch
			{
			}
		}
		if (unscaledTime < _holoPulseUntil && unscaledTime >= _nextHoloPulseAt && HoloReticleColorApplier.NeedsBootstrapPulse())
		{
			_nextHoloPulseAt = unscaledTime + 1.5f;
			try
			{
				PulseHoloReticleFromConfig();
			}
			catch
			{
			}
		}
		if (unscaledTime >= _nextStretchApplyAt)
		{
			_nextStretchApplyAt = unscaledTime + 5f;
			try
			{
				if (AspectStretch.IsEnabled())
				{
					ApplyStretch();
				}
			}
			catch
			{
			}
		}
		if (unscaledTime >= _nextGloveCheckAt)
		{
			_nextGloveCheckAt = unscaledTime + 0.25f;
			try
			{
				GlovesSkinApplier.Tick();
			}
			catch
			{
			}
		}
		TickRamCleanup();
	}

	internal static void PulseHoloReticleFromConfig()
	{
		try
		{
			HoloReticleColorApplier.ApplySavedConfigNow();
		}
		catch
		{
		}
		CBSafeColorsPlugin.Instance?.HoloReticleApplier?.QuickApplyFromConfig(skipConfigReload: true);
	}

	private void TickRamCleanup()
	{
		if (Defines.OptimizeRamCleanupCached && !(UnityEngine.Time.unscaledTime < _nextRamCleanAt))
		{
			ResetRamSchedule();
			RunRamCleanup();
		}
	}

	internal static void ResetRamSchedule()
	{
		try
		{
			if (_instance != null)
			{
				_instance._nextRamCleanAt = UnityEngine.Time.unscaledTime + 1800f;
			}
		}
		catch
		{
		}
	}

	internal static void RunRamCleanup()
	{
		try
		{
			GC.collect();
		}
		catch
		{
		}
		try
		{
			ConsoleSystem.Run(ConsoleSystem.Option.Client.Quiet(), LocStrings.Cmd_PoolClearMemory);
		}
		catch
		{
		}
	}

	internal static void OptimizeSetting()
	{
		try
		{
			UpdateShadows();
		}
		catch
		{
		}
		try
		{
			UpdateGrass();
		}
		catch
		{
		}
		try
		{
			UpdateLongHand();
		}
		catch
		{
		}
		try
		{
			UpdateLeftRightHand();
		}
		catch
		{
		}
		try
		{
			UpdateFastAlt();
		}
		catch
		{
		}
		try
		{
			if (Defines.OptimizeGraphicCached)
			{
				ApplyGraphicCuts();
			}
			else
			{
				RestoreGraphicCuts();
			}
			OptRuntime.ForceApply = true;
		}
		catch
		{
		}
	}

	internal static void ApplyGraphicCuts()
	{
		CaptureGraphicSnapshot();
		ConVar.Terrain.quality = 1f;
		ConVar.Tree.quality = 0f;
		Water.quality = 0;
		Water.reflections = 0;
		Particle.quality = 0f;
		ConVar.Graphics.drawdistance = 400f;
		ConVar.Graphics.showtexeldensity = 0;
		ConVar.Graphics.quality = 0;
		ConVar.Graphics.lodbias = 0.25f;
		ConVar.Graphics.af = 1;
		ConVar.Graphics.shaderlod = 100;
		ConVar.Tree.meshes = 0;
		ConVar.Mesh.quality = 0f;
		Decor.quality = 0f;
		instruments.processSustainPedal = false;
		ConVar.Graphics.contactshadows = false;
		ConVar.Graphics.parallax = 0;
		ConVar.Graphics.dof = false;
		QualitySettings.masterTextureLimit = 2;
		QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
		QualitySettings.pixelLightCount = 0;
		QualitySettings.particleRaycastBudget = 0;
		SSS.enabled = false;
		_instance._graphicCutsApplied = true;
	}

	private static void CaptureGraphicSnapshot()
	{
		if (!(_instance == null) && !_instance._graphicSnapshotReady)
		{
			_instance._graphicTerrainQuality = ConVar.Terrain.quality;
			_instance._graphicTreeQuality = ConVar.Tree.quality;
			_instance._graphicWaterQuality = Water.quality;
			_instance._graphicWaterReflections = Water.reflections;
			_instance._graphicParticleQuality = Particle.quality;
			_instance._graphicDrawDistance = ConVar.Graphics.drawdistance;
			_instance._graphicShowTexelDensity = ConVar.Graphics.showtexeldensity;
			_instance._graphicQuality = ConVar.Graphics.quality;
			_instance._graphicLodBias = ConVar.Graphics.lodbias;
			_instance._graphicMipLimit = QualitySettings.masterTextureLimit;
			_instance._graphicAniso = (int)QualitySettings.anisotropicFiltering;
			_instance._graphicAf = ConVar.Graphics.af;
			_instance._graphicPixelLights = QualitySettings.pixelLightCount;
			_instance._graphicParticleRaycasts = QualitySettings.particleRaycastBudget;
			_instance._graphicShaderLod = ConVar.Graphics.shaderlod;
			_instance._graphicTreeMeshes = ConVar.Tree.meshes;
			_instance._graphicMeshQuality = ConVar.Mesh.quality;
			_instance._graphicDecorQuality = Decor.quality;
			_instance._graphicSustainPedal = instruments.processSustainPedal;
			_instance._graphicContactShadows = ConVar.Graphics.contactshadows;
			_instance._graphicParallax = ConVar.Graphics.parallax;
			_instance._graphicDof = ConVar.Graphics.dof;
			_instance._graphicSss = SSS.enabled;
			_instance._graphicSnapshotReady = true;
		}
	}

	private static void RestoreGraphicCuts()
	{
		if (!(_instance == null) && _instance._graphicSnapshotReady)
		{
			ConVar.Terrain.quality = _instance._graphicTerrainQuality;
			ConVar.Tree.quality = _instance._graphicTreeQuality;
			Water.quality = _instance._graphicWaterQuality;
			Water.reflections = _instance._graphicWaterReflections;
			Particle.quality = _instance._graphicParticleQuality;
			ConVar.Graphics.drawdistance = _instance._graphicDrawDistance;
			ConVar.Graphics.showtexeldensity = _instance._graphicShowTexelDensity;
			ConVar.Graphics.quality = _instance._graphicQuality;
			ConVar.Graphics.lodbias = _instance._graphicLodBias;
			QualitySettings.masterTextureLimit = _instance._graphicMipLimit;
			QualitySettings.anisotropicFiltering = (AnisotropicFiltering)_instance._graphicAniso;
			ConVar.Graphics.af = _instance._graphicAf;
			QualitySettings.pixelLightCount = _instance._graphicPixelLights;
			QualitySettings.particleRaycastBudget = _instance._graphicParticleRaycasts;
			ConVar.Graphics.shaderlod = _instance._graphicShaderLod;
			ConVar.Tree.meshes = _instance._graphicTreeMeshes;
			ConVar.Mesh.quality = _instance._graphicMeshQuality;
			Decor.quality = _instance._graphicDecorQuality;
			instruments.processSustainPedal = _instance._graphicSustainPedal;
			ConVar.Graphics.contactshadows = _instance._graphicContactShadows;
			ConVar.Graphics.parallax = _instance._graphicParallax;
			ConVar.Graphics.dof = _instance._graphicDof;
			SSS.enabled = _instance._graphicSss;
			_instance._graphicCutsApplied = false;
		}
	}

	private static void RunClientQuiet(string cmd)
	{
		try
		{
			ConsoleSystem.Run(ConsoleSystem.Option.Client.Quiet(), cmd);
		}
		catch
		{
		}
	}

	internal static void UpdateGrass()
	{
		if (_instance != null && !_instance._grassSnapshotReady)
		{
			_instance._grassQuality = Grass.quality;
			_instance._grassQuality01 = Grass.quality01;
			_instance._grassShadows = ConVar.Graphics.grassshadows;
			_instance._grassSnapshotReady = true;
		}
		if (Defines.OptimizeGrassCached)
		{
			Grass.quality = 0f;
			Grass.quality01 = 0f;
			ConVar.Graphics.grassshadows = false;
		}
		else
		{
			Grass.quality = ((_instance == null) ? 100f : _instance._grassQuality);
			Grass.quality01 = ((_instance == null) ? 1f : _instance._grassQuality01);
			ConVar.Graphics.grassshadows = _instance == null || !_instance._grassSnapshotReady || _instance._grassShadows;
		}
	}

	internal static void UpdateLongHand()
	{
		ConVar.Graphics.vm_fov_scale = !Defines.LongHand.Value;
	}

	internal static void UpdateLeftRightHand()
	{
		ConVar.Graphics.vm_horizontal_flip = !Defines.LeftRightHand.Value;
	}

	internal static void ReapplyPersistedClientSettings()
	{
		try
		{
			UpdateFastAlt();
		}
		catch
		{
		}
		try
		{
			UpdateLongHand();
		}
		catch
		{
		}
		try
		{
			UpdateLeftRightHand();
		}
		catch
		{
		}
		try
		{
			UpdateGrass();
		}
		catch
		{
		}
		try
		{
			UpdateShadows();
		}
		catch
		{
		}
	}

	internal static void UpdateFastAlt()
	{
		RunClientQuiet(Defines.FastAlt.Value ? "client.headlerp 1000" : "client.headlerp 1");
	}

	internal static void UpdateShadows()
	{
		if (_instance != null && !_instance._shadowSnapshotReady)
		{
			_instance._shadowCascades = ConVar.Graphics.shadowcascades;
			_instance._shadowDistance = ConVar.Graphics.shadowdistance;
			_instance._shadowMode = ConVar.Graphics.shadowmode;
			_instance._shadowQuality = ConVar.Graphics.shadowquality;
			_instance._shadowLights = ConVar.Graphics.shadowlights;
			_instance._shadowSnapshotReady = true;
		}
		if (Defines.OptimizeShadowsCached)
		{
			ConVar.Graphics.shadowdistance = 0f;
			ConVar.Graphics.shadowcascades = 0;
			ConVar.Graphics.shadowmode = 0;
			ConVar.Graphics.shadowquality = 0;
			ConVar.Graphics.shadowlights = 0;
		}
		else
		{
			ConVar.Graphics.shadowcascades = ((_instance == null) ? 1 : _instance._shadowCascades);
			ConVar.Graphics.shadowdistance = ((_instance == null) ? 40f : _instance._shadowDistance);
			ConVar.Graphics.shadowmode = ((_instance == null) ? 1 : _instance._shadowMode);
			ConVar.Graphics.shadowquality = ((_instance == null) ? 1 : _instance._shadowQuality);
			ConVar.Graphics.shadowlights = ((_instance == null) ? 1 : _instance._shadowLights);
		}
	}

	internal static void ApplyStretch()
	{
		Camera main = Camera.main;
		if (!(main == null))
		{
			if (AspectStretch.IsEnabled())
			{
				main.aspect = AspectStretch.GetAspect();
			}
			else
			{
				main.ResetAspect();
			}
		}
	}
}
