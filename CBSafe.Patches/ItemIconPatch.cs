using System.IO;
using System.Reflection;
using UnityEngine;

namespace CBSafe.Patches;

public static class ItemIconPatch
{
	private static bool _initialized;

	public static Sprite GlassAkSprite { get; private set; }

	public static Sprite OriginalAkSprite { get; private set; }

	public static void Init()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		try
		{
			byte[] array = null;
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CBSafe.Res.rifle.ak.glass.png"))
			{
				if (stream != null)
				{
					array = new byte[stream.Length];
					int num;
					for (int i = 0; i < array.Length; i += num)
					{
						num = stream.Read(array, i, array.Length - i);
						if (num <= 0)
						{
							break;
						}
					}
				}
			}
			if (array == null || array.Length == 0)
			{
				string path = "D:\\SteamLibrary\\steamapps\\common\\Rust\\Bundles\\items\\rifle.ak.glass.png";
				if (File.Exists(path))
				{
					array = File.ReadAllBytes(path);
				}
			}
			if (array != null && array.Length != 0)
			{
				Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
				if (texture2D.LoadImage(array))
				{
					texture2D.name = "rifle.ak.glass";
					texture2D.filterMode = FilterMode.Bilinear;
					GlassAkSprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
					GlassAkSprite.name = "rifle.ak.glass.sprite";
				}
			}
		}
		catch
		{
		}
	}

	public static void SyncIcon()
	{
		try
		{
			ItemDefinition itemDefinition = ItemManager.FindItemDefinition("rifle.ak");
			if (!(itemDefinition != null))
			{
				return;
			}
			if (OriginalAkSprite == null && itemDefinition.iconSprite != null && itemDefinition.iconSprite != GlassAkSprite)
			{
				OriginalAkSprite = itemDefinition.iconSprite;
			}
			if (ModConfig.GlassAkEnabled != null && ModConfig.GlassAkEnabled.Value)
			{
				if (GlassAkSprite != null)
				{
					itemDefinition.iconSprite = GlassAkSprite;
				}
			}
			else if (OriginalAkSprite != null)
			{
				itemDefinition.iconSprite = OriginalAkSprite;
			}
		}
		catch
		{
		}
	}
}
