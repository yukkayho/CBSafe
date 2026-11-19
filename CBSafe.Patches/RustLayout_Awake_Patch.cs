using System.Reflection;
using HarmonyLib;
using Rust.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch(typeof(RustLayout), "Awake")]
public class RustLayout_Awake_Patch
{
	private static void Postfix(RustLayout __instance)
	{
		try
		{
			if (!(__instance != null) || !(__instance.name == "Bottom"))
			{
				return;
			}
			foreach (Image componentsInChild in __instance.GetComponentsInChildren<Image>())
			{
				if (componentsInChild != null)
				{
					Object.Destroy(componentsInChild);
				}
			}
		}
		catch
		{
		}
	}
}
