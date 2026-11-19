using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
[HydraNoObfuscate]
internal static class BloodInstantiatePatch3
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(UnityEngine.Object), "Instantiate", new Type[3]
		{
			typeof(UnityEngine.Object),
			typeof(Transform),
			typeof(bool)
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
