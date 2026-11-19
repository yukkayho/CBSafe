using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.Infrastructure;
using Rust.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Colors.UI;

internal sealed class DropdownHandle
{
	public string Key;

	public string[] Options;

	public Color[] Colors;

	public TweakUIDropdown Tud;

	public RustButton Opener;

	public RectTransform Panel;

	public RectTransform RowRect;

	public Image RowSwatch;

	public RustText CurrentText;

	public int CurrentIndex;

	public void SetIndexSilently(int idx)
	{
		if (Options == null || idx < 0 || idx >= Options.Length)
		{
			return;
		}
		CurrentIndex = idx;
		ManualLogSource manualLogSource = CBSafeColorsPlugin.Instance?.Log;
		if (Tud != null)
		{
			try
			{
				Tud.currentValue = idx;
			}
			catch
			{
			}
		}
		bool isEnabled = false;
		if (Colors != null && RowSwatch != null && idx < Colors.Length)
		{
			try
			{
				RowSwatch.color = Colors[idx];
			}
			catch
			{
			}
			if (!PluginConfig.IsDebugEnabled)
			{
				return;
			}
			ManualLogSource manualLogSource2 = manualLogSource;
			if (manualLogSource2 != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler = new BepInExInfoLogInterpolatedStringHandler(53, 2, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("[SystemDropdown] SetIndexSilently '");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(Key);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral("' -> idx=");
					bepInExInfoLogInterpolatedStringHandler.AppendFormatted(idx);
					bepInExInfoLogInterpolatedStringHandler.AppendLiteral(" (swatch)");
				}
				manualLogSource2.LogInfo(bepInExInfoLogInterpolatedStringHandler);
			}
		}
		else if (Colors == null && CurrentText != null)
		{
			try
			{
				CurrentText.IsLocalized = false;
			}
			catch
			{
			}
			try
			{
				CurrentText.Token = string.Empty;
			}
			catch
			{
			}
			try
			{
				CurrentText.English = Options[idx];
			}
			catch
			{
			}
			try
			{
				CurrentText.text = Options[idx];
			}
			catch
			{
			}
			try
			{
				CurrentText.raycastTarget = false;
			}
			catch
			{
			}
			SystemDropdownFactory.EnsureDropdownValueTextVisible(CurrentText);
			try
			{
				CurrentText.ForceMeshUpdate();
			}
			catch
			{
			}
			if (!PluginConfig.IsDebugEnabled)
			{
				return;
			}
			ManualLogSource manualLogSource3 = manualLogSource;
			if (manualLogSource3 != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler2 = new BepInExInfoLogInterpolatedStringHandler(52, 3, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("[SystemDropdown] SetIndexSilently '");
					bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(Key);
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("' -> idx=");
					bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(idx);
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral(" text='");
					bepInExInfoLogInterpolatedStringHandler2.AppendFormatted(Options[idx]);
					bepInExInfoLogInterpolatedStringHandler2.AppendLiteral("'");
				}
				manualLogSource3.LogInfo(bepInExInfoLogInterpolatedStringHandler2);
			}
		}
		else
		{
			if (!PluginConfig.IsDebugEnabled)
			{
				return;
			}
			ManualLogSource manualLogSource4 = manualLogSource;
			if (manualLogSource4 != null)
			{
				BepInExInfoLogInterpolatedStringHandler bepInExInfoLogInterpolatedStringHandler3 = new BepInExInfoLogInterpolatedStringHandler(89, 5, out isEnabled);
				if (isEnabled)
				{
					bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("[SystemDropdown] SetIndexSilently '");
					bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(Key);
					bepInExInfoLogInterpolatedStringHandler3.AppendLiteral("' -> idx=");
					bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(idx);
					bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" (no visual: Colors=");
					bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(Colors != null);
					bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" RowSwatch=");
					bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(RowSwatch != null);
					bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(" CurrentText=");
					bepInExInfoLogInterpolatedStringHandler3.AppendFormatted(CurrentText != null);
					bepInExInfoLogInterpolatedStringHandler3.AppendLiteral(")");
				}
				manualLogSource4.LogInfo(bepInExInfoLogInterpolatedStringHandler3);
			}
		}
	}
}
