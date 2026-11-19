using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
[HarmonyPatch(typeof(MainMenuSystem), "Awake")]
public class MainMenuSystem_Awake_Patch
{
	private static void Postfix(MainMenuSystem __instance)
	{
		try
		{
			foreach (Image item in Object.FindObjectsOfType<Image>())
			{
				if (item != null)
				{
					int num = (int)(item.color.r * 255f);
					int num2 = (int)(item.color.g * 255f);
					int num3 = (int)(item.color.b * 255f);
					if (num == 76 && num2 == 72 && num3 == 59)
					{
						item.color = new Color(0f, 0f, 0f, 0f);
					}
				}
			}
		}
		catch
		{
		}
	}
}
