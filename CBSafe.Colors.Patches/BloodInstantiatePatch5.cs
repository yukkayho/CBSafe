using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
[HydraNoObfuscate]
internal static class BloodInstantiatePatch5
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(UnityEngine.Object), "Instantiate", new Type[4]
		{
			typeof(UnityEngine.Object),
			typeof(Vector3),
			typeof(Quaternion),
			typeof(Transform)
		});
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix(UnityEngine.Object __result)
	{
		try
		{
			BloodColorApplier.ProcessClone(__result);
		}
		catch
		{
		}
	}
}
