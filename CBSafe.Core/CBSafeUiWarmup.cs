using System.Reflection;
using CBSafe.Colors.UI;
using CBSafe.Core.UI;
using CBSafe.Crosshair.UI;

namespace CBSafe.Core;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
public static class CBSafeUiWarmup
{
	private static bool _warmedThisMenu;

	private static bool _warmedThisSettingsSession;

	public static void OnMenuReady()
	{
		if (_warmedThisMenu)
		{
			return;
		}
		_warmedThisMenu = true;
		WarmTemplatesNow();
		try
		{
			CBSafeSettingsUiLoadBootstrap.OnServerLoadStarted();
		}
		catch
		{
		}
	}

	public static void OnSettingsPanelReady()
	{
		try
		{
			CBSafeSettingsUiLoadBootstrap.OnSettingsOpened();
		}
		catch
		{
		}
		if (!_warmedThisSettingsSession)
		{
			_warmedThisSettingsSession = true;
			WarmTemplatesNow();
		}
	}

	public static void WarmTemplatesNow()
	{
		WarmTemplates();
	}

	public static void ResetForNextMenu()
	{
		_warmedThisMenu = false;
	}

	public static void ResetForSettingsSession()
	{
		_warmedThisSettingsSession = false;
		try
		{
			SettingsTabPrefetcher.ResetForSettingsSession();
		}
		catch
		{
		}
	}

	private static void WarmTemplates()
	{
		try
		{
			SystemToggleFactory.PreWarmTemplate();
		}
		catch
		{
		}
		try
		{
			SystemSliderFactory.PreWarmTemplate();
		}
		catch
		{
		}
		try
		{
			CBSafe.Colors.UI.SystemDropdownFactory.PreWarmRowTemplate();
		}
		catch
		{
		}
		try
		{
			CBSafe.Crosshair.UI.SystemDropdownFactory.PreWarmRowTemplate();
		}
		catch
		{
		}
	}
}
