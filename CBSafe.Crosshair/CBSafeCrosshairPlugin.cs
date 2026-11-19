using System;
using System.Reflection;
using BepInEx.Logging;
using CBSafe.Core;
using CBSafe.Core.UI;
using CBSafe.Crosshair.Infrastructure;
using CBSafe.Crosshair.Render;
using CBSafe.Crosshair.UI;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace CBSafe.Crosshair;

public sealed class CBSafeCrosshairPlugin
{
	public const string PluginId = "net.rust.CBSafe.Crosshair";

	private GameObject _rendererHost;

	public static CBSafeCrosshairPlugin Instance { get; private set; }

	public ManualLogSource Log { get; private set; }

	public ScreenCrosshairRenderer ScreenRenderer { get; private set; }

	public static void Init(ManualLogSource log)
	{
		if (Instance == null)
		{
			Instance = new CBSafeCrosshairPlugin();
			Instance.Log = log;
			Instance.Load();
		}
	}

	private void Load()
	{
		try
		{
			CrosshairConfigStore.Load();
		}
		catch (Exception ex)
		{
			Log.LogError("CBSafe: config load failed: " + ex);
		}
		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<ScreenCrosshairRenderer>();
			ClassInjector.RegisterTypeInIl2Cpp<PreviewRenderer>();
			ClassInjector.RegisterTypeInIl2Cpp<CrosshairContentPrefetcher>();
			ClassInjector.RegisterTypeInIl2Cpp<SettingsTabPrefetcher>();
		}
		catch (Exception ex2)
		{
			Log.LogError("CBSafe: register MonoBehaviours failed: " + ex2);
		}
		try
		{
			_rendererHost = new GameObject("CBSafe_Crosshair_RendererHost");
			UnityEngine.Object.DontDestroyOnLoad(_rendererHost);
			ScreenRenderer = _rendererHost.AddComponent<ScreenCrosshairRenderer>();
			_rendererHost.AddComponent<CrosshairContentPrefetcher>();
			_rendererHost.AddComponent<SettingsTabPrefetcher>();
		}
		catch (Exception ex3)
		{
			Log.LogError("CBSafe: spawn renderer host failed: " + ex3);
		}
		try
		{
			string[] array = new string[3]
			{
				LocStrings.Asm_AssemblyCSharp,
				LocStrings.Asm_RustUI,
				LocStrings.Asm_RustLocalization
			};
			foreach (string text in array)
			{
				if (!string.IsNullOrEmpty(text))
				{
					try
					{
						Assembly.Load(text);
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
}
