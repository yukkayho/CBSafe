using System.IO;
using System.Reflection;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
internal static class ItemIconOverride
{
	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(ItemManager), "Initialize")]
	internal static class ItemManagerInitializePatch
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			Apply();
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(LoadingScreen), "UpdateFromServer")]
	internal static class LoadingScreenPatch
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			if (!_applied)
			{
				Apply();
			}
		}
	}

	private const string TargetShortname = "barricade.wood.cover";

	private const ulong WorkshopId = 4123512uL;

	private static bool _applied;

	private static Sprite _cachedSprite;

	public static void Apply()
	{
		if (_applied)
		{
			return;
		}
		try
		{
			List<ItemDefinition> itemList = ItemManager.itemList;
			if (itemList == null || itemList.Count == 0)
			{
				return;
			}
			EnsureWorkshopIconFile(GetWorkshopRoot());
			string text = ResolveIconPath();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			Sprite sprite = LoadSprite(text);
			if (sprite == null)
			{
				return;
			}
			_cachedSprite = sprite;
			int num = 0;
			for (int i = 0; i < itemList.Count; i++)
			{
				ItemDefinition itemDefinition = itemList[i];
				if (!(itemDefinition == null) && itemDefinition.shortname == "barricade.wood.cover")
				{
					itemDefinition.iconSprite = sprite;
					num++;
				}
			}
			if (num > 0)
			{
				_applied = true;
			}
		}
		catch
		{
		}
	}

	private static string GetWorkshopRoot()
	{
		string text = Application.dataPath;
		if (text.Length > 16)
		{
			text = text.Remove(text.Length - 16);
		}
		return text + "\\workshop\\" + 4123512uL;
	}

	private static string ResolveIconPath()
	{
		string workshopRoot = GetWorkshopRoot();
		if (!Directory.Exists(workshopRoot))
		{
			return null;
		}
		string[] array = new string[3]
		{
			Path.Combine(workshopRoot, "barricade.wood.cover.png"),
			Path.Combine(workshopRoot, "icon.png"),
			Path.Combine(workshopRoot, "icon_background.png")
		};
		for (int i = 0; i < array.Length; i++)
		{
			if (File.Exists(array[i]))
			{
				return array[i];
			}
		}
		return null;
	}

	private static Sprite LoadSprite(string path)
	{
		if (string.IsNullOrEmpty(path) || !File.Exists(path))
		{
			return null;
		}
		byte[] array = File.ReadAllBytes(path);
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
		{
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp
		};
		if (!texture2D.LoadImage(array, markNonReadable: false))
		{
			return null;
		}
		float width = texture2D.width;
		float height = texture2D.height;
		return Sprite.Create(texture2D, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
	}

	internal static void EnsureWorkshopIconFile(string workshopDir)
	{
		if (string.IsNullOrEmpty(workshopDir) || !Directory.Exists(workshopDir))
		{
			return;
		}
		string text = Path.Combine(workshopDir, "barricade.wood.cover.png");
		string text2 = Path.Combine(workshopDir, "icon.png");
		if (!File.Exists(text))
		{
			return;
		}
		try
		{
			if (!File.Exists(text2) || File.GetLastWriteTimeUtc(text) > File.GetLastWriteTimeUtc(text2))
			{
				File.Copy(text, text2, overwrite: true);
			}
		}
		catch
		{
		}
	}
}
