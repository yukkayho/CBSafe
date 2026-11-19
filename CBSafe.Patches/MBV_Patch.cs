using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Rust.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "string encryption")]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "stringsHider")]
[HarmonyPatch(typeof(MenuBackgroundVideo), "NextVideo")]
public class MBV_Patch
{
	private static List<string> disallowedItems = new List<string> { "NEWS", "Inventory", "Item Store", "Workshop", "Rust+", "НОВОСТИ", "Инвентарь", "Магазин скинов", "Мастерская" };

	private static void Prefix()
	{
		if (SingletonComponent<MainMenuSystem>.Instance == null)
		{
			return;
		}
		foreach (MaskableGraphic item in Object.FindObjectsOfType<MaskableGraphic>())
		{
			if (item != null && item.name == "Updated News")
			{
				Object.Destroy(item);
			}
		}
		foreach (TMP_Text item2 in Object.FindObjectsOfType<TMP_Text>())
		{
			if (item2 != null && item2.text == "NEW")
			{
				Object.Destroy(item2);
			}
		}
		GameObject gameObject = null;
		GameObject gameObject2 = null;
		GameObject gameObject3 = null;
		GameObject gameObject4 = null;
		foreach (RustText item3 in Object.FindObjectsOfType<RustText>())
		{
			if (!(item3 == null) && item3.name != null)
			{
				if (disallowedItems.Contains(item3.text))
				{
					((item3.transform.parent != null) ? item3.transform.parent.gameObject : item3.gameObject).SetActive(value: false);
				}
				else if (item3.text == "PLAY GAME")
				{
					item3.text = "Играть";
					gameObject = ((item3.transform.parent != null) ? item3.transform.parent.gameObject : item3.gameObject);
				}
				else if (item3.text == "Options")
				{
					item3.text = "Настройки";
					gameObject2 = ((item3.transform.parent != null) ? item3.transform.parent.gameObject : item3.gameObject);
				}
				else if (item3.text == "Quit")
				{
					item3.text = "Выйти";
					gameObject3 = ((item3.transform.parent != null) ? item3.transform.parent.gameObject : item3.gameObject);
				}
				else if (item3.text == "Resume" || item3.text == "RESUME")
				{
					item3.text = "Сессия";
					gameObject4 = ((item3.transform.parent != null) ? item3.transform.parent.gameObject : item3.gameObject);
				}
			}
		}
		try
		{
			if (gameObject != null && gameObject2 != null && gameObject3 != null)
			{
				float num = gameObject2.transform.position.y - gameObject3.transform.position.y;
				Vector3 position = gameObject2.transform.position;
				gameObject.transform.position = new Vector3(position.x, position.y + num, position.z);
				if (gameObject4 != null)
				{
					gameObject4.transform.position = new Vector3(position.x, position.y + num * 2f, position.z);
				}
			}
		}
		catch
		{
		}
	}
}
