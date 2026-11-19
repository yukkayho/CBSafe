using HarmonyLib;
using UnityEngine;

namespace CBSafe.Patches;

[HarmonyPatch(typeof(Camera), "ResetAspect")]
internal static class CameraResetAspectPatch
{
	private static Camera _cachedMain;

	private static float _cacheUntil;

	private static Camera GetMain()
	{
		float num;
		try
		{
			num = Time.unscaledTime;
		}
		catch
		{
			num = 0f;
		}
		if (_cachedMain == null || num >= _cacheUntil)
		{
			_cachedMain = Camera.main;
			_cacheUntil = num + 2f;
		}
		return _cachedMain;
	}

	[HarmonyPrefix]
	public static bool Prefix(Camera __instance)
	{
		if (AspectStretch.IsEnabled())
		{
			return __instance != GetMain();
		}
		return true;
	}
}
