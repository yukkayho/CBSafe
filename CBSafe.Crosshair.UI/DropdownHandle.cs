using Rust.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Crosshair.UI;

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
		if (Colors != null && RowSwatch != null && idx < Colors.Length)
		{
			try
			{
				RowSwatch.color = Colors[idx];
				return;
			}
			catch
			{
				return;
			}
		}
		if (Colors != null || !(CurrentText != null))
		{
			return;
		}
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
		EnsureDropdownValueTextVisible(CurrentText);
		try
		{
			CurrentText.ForceMeshUpdate();
		}
		catch
		{
		}
	}

	private static void EnsureDropdownValueTextVisible(RustText currentText)
	{
		if (currentText == null)
		{
			return;
		}
		try
		{
			if (currentText.color.a < 0.05f)
			{
				currentText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
			}
		}
		catch
		{
		}
	}
}
