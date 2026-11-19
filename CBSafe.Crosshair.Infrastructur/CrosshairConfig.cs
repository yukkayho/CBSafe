using System.Reflection;
using CBSafe.Core;
using UnityEngine;

namespace CBSafe.Crosshair.Infrastructure;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
public sealed class CrosshairConfig
{
	public bool Enabled = true;

	public bool DynamicVisibility = true;

	public bool DynamicGap = true;

	public int StyleIndex;

	public string MainColorString = "255,255,255,255";

	public string OutlineColorString = "0,0,0,255";

	public float DotSize;

	public float Gap = 2.06f;

	public float Length = 3.9f;

	public float Thickness = 0.6f;

	public bool OutlineEnabled;

	public const int StyleDefault = 0;

	public const int StyleDotCross = 1;

	public const int StyleTShape = 2;

	public static readonly string[] StyleLabels = new string[3]
	{
		LocStrings.CrossStyle_Default,
		LocStrings.CrossStyle_DotCross,
		LocStrings.CrossStyle_TShape
	};

	public Color MainColor
	{
		get
		{
			if (string.IsNullOrEmpty(MainColorString))
			{
				return Color.white;
			}
			string[] array = MainColorString.Split(',');
			if (array.Length >= 3 && byte.TryParse(array[0], out var result) && byte.TryParse(array[1], out var result2) && byte.TryParse(array[2], out var result3))
			{
				byte result4 = byte.MaxValue;
				if (array.Length >= 4)
				{
					byte.TryParse(array[3], out result4);
				}
				return new Color((float)(int)result / 255f, (float)(int)result2 / 255f, (float)(int)result3 / 255f, (float)(int)result4 / 255f);
			}
			return Color.white;
		}
	}

	public Color OutlineColor
	{
		get
		{
			if (string.IsNullOrEmpty(OutlineColorString))
			{
				return Color.black;
			}
			string[] array = OutlineColorString.Split(',');
			if (array.Length >= 3 && byte.TryParse(array[0], out var result) && byte.TryParse(array[1], out var result2) && byte.TryParse(array[2], out var result3))
			{
				byte result4 = byte.MaxValue;
				if (array.Length >= 4)
				{
					byte.TryParse(array[3], out result4);
				}
				return new Color((float)(int)result / 255f, (float)(int)result2 / 255f, (float)(int)result3 / 255f, (float)(int)result4 / 255f);
			}
			return Color.black;
		}
	}

	public static CrosshairConfig CreateDefaults()
	{
		return new CrosshairConfig();
	}
}
