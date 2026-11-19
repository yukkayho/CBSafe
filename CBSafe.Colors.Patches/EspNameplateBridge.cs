using System;
using System.Reflection;
using Facepunch.UI;
using TMPro;
using UnityEngine;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
internal static class EspNameplateBridge
{
	private static ESPCanvas _cachedCanvas;

	public static bool IsAvailable => true;

	public static Type CanvasType => typeof(ESPCanvas);

	internal static void InvalidateCanvasCache()
	{
		_cachedCanvas = null;
	}

	internal static ESPCanvas TryGetCanvas()
	{
		if (_cachedCanvas != null)
		{
			try
			{
				if ((bool)_cachedCanvas)
				{
					return _cachedCanvas;
				}
			}
			catch
			{
			}
			_cachedCanvas = null;
		}
		try
		{
			_cachedCanvas = UnityEngine.Object.FindObjectOfType<ESPCanvas>();
		}
		catch
		{
			_cachedCanvas = null;
		}
		return _cachedCanvas;
	}

	public static BasePlayer GetEntity(ESPPlayerInfo info)
	{
		if (info == null)
		{
			return null;
		}
		try
		{
			return info.Entity;
		}
		catch
		{
			return null;
		}
	}

	public static ESPPlayerInfo FindOnPlayer(BasePlayer bp)
	{
		if (bp == null)
		{
			return null;
		}
		ESPCanvas eSPCanvas = TryGetCanvas();
		if (eSPCanvas == null)
		{
			return null;
		}
		try
		{
			ESPPlayerInfo[] array = eSPCanvas.Elements;
			if (array == null)
			{
				return null;
			}
			ESPPlayerInfo[] array2 = array;
			foreach (ESPPlayerInfo eSPPlayerInfo in array2)
			{
				if (!(eSPPlayerInfo == null))
				{
					BasePlayer basePlayer = null;
					try
					{
						basePlayer = eSPPlayerInfo.Entity;
					}
					catch
					{
					}
					if (basePlayer == bp)
					{
						return eSPPlayerInfo;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	public static void Paint(ESPPlayerInfo info, Color color)
	{
		if (info == null || !TeamPlayerNameColorApplier.IsEspNameplateVisible(info))
		{
			return;
		}
		try
		{
			TextMeshProUGUI text = info.Text;
			if (text != null)
			{
				Color color2 = color;
				color2.a = text.color.a;
				text.color = color2;
			}
			TextMeshProUGUI image = info.Image;
			if (image != null)
			{
				Color color3 = color;
				color3.a = image.color.a;
				image.color = color3;
			}
		}
		catch
		{
		}
	}
}
