using System;
using System.Reflection;
using BepInEx.Logging;
using CBSafe.Colors.Infrastructure;
using CBSafe.Colors.Patches;
using CBSafe.Core;
using CBSafe.Core.UI;
using CBSafe.Infrastructure;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace CBSafe.Colors;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public sealed class CBSafeColorsPlugin
{
	public const string PluginId = "net.rust.CBSafe.Colors";

	private GameObject _appliersHost;

	public static CBSafeColorsPlugin Instance { get; private set; }

	public ManualLogSource Log { get; private set; }

	public HudBarColorApplier HudBarApplier { get; private set; }

	public HoloReticleColorApplier HoloReticleApplier { get; private set; }

	public TreeCrosshairColorApplier TreeCrosshairApplier { get; private set; }

	public TeamPlayerNameColorApplier TeamPlayerNameApplier { get; private set; }

	public BloodColorApplier BloodApplier { get; private set; }

	public static void Init(ManualLogSource log)
	{
		if (Instance == null)
		{
			Instance = new CBSafeColorsPlugin();
			Instance.Log = log;
			Instance.Load();
		}
	}

	private void Load()
	{
		try
		{
			PluginConfig.Initialize(SharedConfig.File);
		}
		catch (Exception ex)
		{
			Log.LogError("CBSafe: config load failed: " + ex);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<PanelOpenWatcher>();
		}
		catch (Exception ex2)
		{
			Log.LogError("CBSafe: register PanelOpenWatcher failed: " + ex2);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<DropdownLabelEnforcer>();
		}
		catch (Exception ex3)
		{
			Log.LogError("CBSafe: register DropdownLabelEnforcer failed: " + ex3);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ToggleLabelEnforcer>();
		}
		catch (Exception ex4)
		{
			Log.LogError("CBSafe: register ToggleLabelEnforcer failed: " + ex4);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ToggleVisualEnforcer>();
		}
		catch (Exception ex5)
		{
			Log.LogError("CBSafe: register ToggleVisualEnforcer failed: " + ex5);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<HudBarColorApplier>();
		}
		catch (Exception ex6)
		{
			Log.LogError("CBSafe: register HudBarColorApplier failed: " + ex6);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<HoloReticleColorApplier>();
		}
		catch (Exception ex7)
		{
			Log.LogError("CBSafe: register HoloReticleColorApplier failed: " + ex7);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<TreeCrosshairColorApplier>();
		}
		catch (Exception ex8)
		{
			Log.LogError("CBSafe: register TreeCrosshairColorApplier failed: " + ex8);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<TeamPlayerNameColorApplier>();
			ClassInjector.RegisterTypeInIl2Cpp<BloodColorApplier>();
		}
		catch (Exception ex9)
		{
			Log.LogError("CBSafe: register TeamPlayerNameColorApplier failed: " + ex9);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<CBSafeColorSessionBootstrap>();
		}
		catch (Exception ex10)
		{
			Log.LogError("CBSafe: register CBSafeColorSessionBootstrap failed: " + ex10);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ColorPickerOverlay>();
		}
		catch (Exception ex11)
		{
			Log.LogError("CBSafe: register ColorPickerOverlay failed: " + ex11);
		}
		try
		{
			_appliersHost = new GameObject(LocStrings.Name_Color_AppliersHost);
			UnityEngine.Object.DontDestroyOnLoad(_appliersHost);
			HudBarApplier = _appliersHost.AddComponent<HudBarColorApplier>();
			HoloReticleApplier = _appliersHost.AddComponent<HoloReticleColorApplier>();
			TreeCrosshairApplier = _appliersHost.AddComponent<TreeCrosshairColorApplier>();
			TeamPlayerNameApplier = _appliersHost.AddComponent<TeamPlayerNameColorApplier>();
			BloodApplier = _appliersHost.AddComponent<BloodColorApplier>();
			_appliersHost.AddComponent<CBSafeColorSessionBootstrap>();
		}
		catch (Exception ex12)
		{
			Log.LogError("CBSafe: spawn appliers host failed: " + ex12);
		}
		try
		{
			try
			{
				PluginConfig.ReloadFromSharedConfig();
			}
			catch
			{
			}
			HudBarColorApplier.RefreshCachedColors();
			HoloReticleColorApplier.ApplySavedConfigNow();
			try
			{
				CBSafeColorSessionBootstrap.ArmColdStart();
			}
			catch
			{
			}
			HudBarApplier?.ForceReapplyNow();
			TeamPlayerNameApplier?.ForceReapplyNow();
		}
		catch (Exception ex13)
		{
			Log.LogWarning("CBSafe: cold-start colour bootstrap: " + ex13.Message);
		}
	}
}
