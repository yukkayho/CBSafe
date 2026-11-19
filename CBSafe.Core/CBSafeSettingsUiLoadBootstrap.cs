using System.Reflection;
using CBSafe.Colors.UI;
using CBSafe.Core.UI;
using CBSafe.Crosshair.UI;
using CBSafe.Patches;
using UnityEngine;

namespace CBSafe.Core;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public static class CBSafeSettingsUiLoadBootstrap
{
	private static bool _loadBurstActive;

	private static float _loadBurstUntil;

	public static bool IsFullyBuilt()
	{
		try
		{
			return TabSystemManager.IsContentBuilt && CrosshairTabManager.IsContentBuilt;
		}
		catch
		{
			return false;
		}
	}

	public static void OnServerLoadStarted()
	{
		_loadBurstActive = true;
		try
		{
			_loadBurstUntil = Time.unscaledTime + 180f;
		}
		catch
		{
			_loadBurstUntil = 0f;
		}
		try
		{
			CBSafeUiWarmup.WarmTemplatesNow();
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

	public static void OnSettingsOpened()
	{
		try
		{
			CBSafeUiWarmup.WarmTemplatesNow();
		}
		catch
		{
		}
		try
		{
			BoostUI.CreateMiscTab();
		}
		catch
		{
		}
		try
		{
			TabSystemManager.TryBuildFromScene();
		}
		catch
		{
		}
		try
		{
			CrosshairTabManager.TryBuildFromScene();
		}
		catch
		{
		}
		try
		{
			SettingsTabPrefetcher.ArmSettingsBurst();
		}
		catch
		{
		}
		if (!IsFullyBuilt())
		{
			try
			{
				SettingsTabPrefetcher.CompleteAllPending();
			}
			catch
			{
			}
		}
	}

	public static void TickDuringLoadScreen()
	{
		if (_loadBurstActive || !(Time.unscaledTime > _loadBurstUntil))
		{
			try
			{
				BoostUI.CreateMiscTab();
			}
			catch
			{
			}
			try
			{
				TabSystemManager.TryBuildFromScene();
			}
			catch
			{
			}
			try
			{
				CrosshairTabManager.TryBuildFromScene();
			}
			catch
			{
			}
			try
			{
				SettingsTabPrefetcher.TickLoadScreenBurst();
			}
			catch
			{
			}
			if (IsFullyBuilt())
			{
				_loadBurstActive = false;
			}
		}
	}

	public static void CompleteBeforeTabOpen()
	{
		try
		{
			BoostUI.CreateMiscTab();
		}
		catch
		{
		}
		try
		{
			TabSystemManager.TryBuildFromScene();
		}
		catch
		{
		}
		try
		{
			CrosshairTabManager.TryBuildFromScene();
		}
		catch
		{
		}
		try
		{
			SettingsTabPrefetcher.CompleteAllPending();
		}
		catch
		{
		}
	}
}
