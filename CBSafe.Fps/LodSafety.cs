using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace CBSafe.Fps;

internal static class LodSafety
{
	private static FieldInfo _ambienceCullField;

	private static bool _ambienceCullResolved;

	public static bool AmbienceCull(AmbienceManager __instance)
	{
		if (__instance == null)
		{
			return false;
		}
		try
		{
			if (!_ambienceCullResolved)
			{
				_ambienceCullField = AccessTools.Field(typeof(AmbienceManager), "cullingGroup") ?? AccessTools.Field(typeof(AmbienceManager), "cullingGroups");
				_ambienceCullResolved = true;
			}
			if (_ambienceCullField == null)
			{
				return true;
			}
			return _ambienceCullField.GetValue(__instance) != null;
		}
		catch
		{
			return false;
		}
	}

	public static bool ParticleSet(ParticleSystemLOD __instance, ref int newlod)
	{
		newlod = ClampLen(Len(__instance), newlod);
		return true;
	}

	public static bool ParticleCollisionSet(ParticleCollisionLOD __instance, ref int newlod)
	{
		newlod = ClampLen(Len(__instance), newlod);
		return true;
	}

	private static int Len(ParticleSystemLOD i)
	{
		try
		{
			return (!(i == null) && i.States != null) ? ((Il2CppArrayBase<ParticleSystemLOD.State>)i.States).Length : 0;
		}
		catch
		{
			return 0;
		}
	}

	private static int Len(ParticleCollisionLOD i)
	{
		try
		{
			return (!(i == null) && i.States != null) ? ((Il2CppArrayBase<ParticleCollisionLOD.State>)i.States).Length : 0;
		}
		catch
		{
			return 0;
		}
	}

	private static int ClampLen(int length, int lod)
	{
		if (length <= 0)
		{
			return 0;
		}
		if (lod < 0)
		{
			return 0;
		}
		int num = length - 1;
		if (lod <= num)
		{
			return lod;
		}
		return num;
	}
}
