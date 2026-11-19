using ConVar;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace CBSafe.Patches;

internal static class MenuFpsForce
{
	private static bool _started;

	private static bool _logged;

	internal static void Install(HarmonyLib.Harmony harmony)
	{
		if (!_logged)
		{
			_logged = true;
		}
	}

	internal static void EnsureRunner(GameObject host)
	{
		if (_started || host == null)
		{
			return;
		}
		try
		{
			try
			{
				ClassInjector.RegisterTypeInIl2Cpp<MenuFpsForceRunner>();
			}
			catch
			{
			}
			MenuFpsForceRunner menuFpsForceRunner = host.GetComponent<MenuFpsForceRunner>();
			if (menuFpsForceRunner == null)
			{
				menuFpsForceRunner = host.AddComponent<MenuFpsForceRunner>();
			}
			_started = menuFpsForceRunner != null;
		}
		catch
		{
		}
	}

	internal static void TickUnityUncap()
	{
		try
		{
			QualitySettings.vSyncCount = 0;
		}
		catch
		{
		}
		try
		{
			Application.targetFrameRate = -1;
		}
		catch
		{
		}
		try
		{
			FPS.limitInMenu = false;
		}
		catch
		{
		}
		try
		{
			ConVar.Graphics.vsync = 0;
		}
		catch
		{
		}
	}
}
