using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using CBSafe.Colors;
using CBSafe.Colors.Infrastructure;
using CBSafe.Colors.Patches;
using CBSafe.Colors.UI;
using CBSafe.Core;
using CBSafe.Crosshair;
using CBSafe.Crosshair.Render;
using CBSafe.Crosshair.UI;
using CBSafe.Fps;
using CBSafe.Holo;
using CBSafe.Patches;
using CBSafe.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace CBSafe;

[BepInPlugin("net.rust.cbsafe", "CBSafe", "1.8.0")]
public class Main : BasePlugin
{
	public static Main Instance { get; private set; }

	public override void Load()
	{
		SlotColorEmbed.Boot(base.Log);
		Instance = this;
		CBSafeStartupLogFilter.Install();
		Defines.LoadSettings();
		HolosightSwap.Init(SharedConfig.File, base.Log);
		HolosightSwap.SetEnabled(Defines.NewHolo.Value);
		CBSafeColorsPlugin.Init(base.Log);
		CBSafeCrosshairPlugin.Init(base.Log);
		CBSafeStartupLogFilter.MuteLogSources();
		Harmony harmony = new Harmony("net.rust.cbsafe");
		harmony.PatchAll();
		try
		{
			ModConfig.Init(SharedConfig.File);
			GlassAk.ApplyCurrentColor();
			AssetFactory.Init();
			ItemIconPatch.Init();
			Optimizations.ApplySettings();
		}
		catch (Exception ex)
		{
			base.Log.LogError("[CBSafe] не запустился: " + ex.Message);
		}
		try
		{
			OptPatchInstaller.Install(harmony);
		}
		catch
		{
		}
		BarricadeDualGuidePreview.Install(new Harmony("net.rust.cbsafe.barricade.dual"));
		GameObject gameObject = AddComponent<GraphicsDon>().gameObject;
		UnityEngine.Object.DontDestroyOnLoad(gameObject);
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<OptRuntime>();
			gameObject.AddComponent<OptRuntime>();
		}
		catch
		{
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<CommandTicker>();
			gameObject.AddComponent<CommandTicker>();
		}
		catch
		{
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<RgbTicker>();
			gameObject.AddComponent<RgbTicker>();
		}
		catch
		{
		}
		try
		{
			DlcUnlockCommand.Register();
		}
		catch
		{
		}
		try
		{
			BoosterProof.Register();
		}
		catch
		{
		}
		try
		{
			WarmUp.PreJit(typeof(TreeCrosshairColorApplier), typeof(TeamPlayerNameColorApplier), typeof(BloodColorApplier), typeof(BloodInstantiatePatch1), typeof(BloodInstantiatePatch2), typeof(BloodInstantiatePatch3), typeof(BloodInstantiatePatch4), typeof(BloodInstantiatePatch5), typeof(BlackGatherImageFillPatch), typeof(BlackGatherImageOnEnablePatch), typeof(BlackGatherGraphicColorPatch), typeof(HudBarColorApplier), typeof(HoloReticleColorApplier), typeof(CBSafeColorSessionBootstrap), typeof(PluginConfig), typeof(ScreenCrosshairRenderer), typeof(CrosshairRenderDriver), typeof(PreviewRenderer), typeof(SystemToggleFactory), typeof(SystemSliderFactory), typeof(CrosshairSectionBuilder), typeof(CrosshairTabManager), typeof(TabSystemManager), typeof(CBSafe.Colors.UI.SystemDropdownFactory), typeof(CBSafe.Crosshair.UI.SystemDropdownFactory));
		}
		catch
		{
		}
	}
}
