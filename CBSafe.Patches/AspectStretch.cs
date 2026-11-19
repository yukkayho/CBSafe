using UnityEngine;

namespace CBSafe.Patches;

internal static class AspectStretch
{
	private static int _cachedValue = -1;

	private static float _lastCheckTime = 0f;

	internal static int GetStretchIndexFast()
	{
		return Defines.StretchIndex.Value;
	}

	internal static bool IsEnabled()
	{
		return GetStretchIndexFast() > 1;
	}

	internal static float GetAspect()
	{
		int stretchIndexFast = GetStretchIndexFast();
		if (stretchIndexFast <= 1)
		{
			return 0f;
		}
		float num = (float)Screen.width / (float)Screen.height;
		float b = num * 0.75f;
		float t = ((float)stretchIndexFast - 1f) / 59f;
		return Mathf.LerpUnclamped(num, b, t);
	}

	internal static string GetLabel(int index)
	{
		if (index <= 1)
		{
			return "Выкл";
		}
		if (index == 60)
		{
			return "4:3";
		}
		return index.ToString();
	}
}
