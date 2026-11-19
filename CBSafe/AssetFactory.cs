using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace CBSafe;

public static class AssetFactory
{
	private static AssetBundle _bundle;

	private static readonly Dictionary<string, UnityEngine.Object> _cache = new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);

	public static void Init()
	{
		if (_bundle != null)
		{
			return;
		}
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CBSafe.Res.glass_ak.bundle");
			if (stream != null)
			{
				byte[] array = new byte[stream.Length];
				int num;
				for (int i = 0; i < array.Length; i += num)
				{
					num = stream.Read(array, i, array.Length - i);
					if (num <= 0)
					{
						break;
					}
				}
				AssetBundle assetBundle = AssetBundle.LoadFromMemory(array);
				if (assetBundle != null)
				{
					_bundle = assetBundle;
					return;
				}
			}
		}
		catch
		{
		}
		string[] array2 = new string[4]
		{
			"D:\\GlassAK\\AssetBundles\\textures.5.bundle",
			"C:\\Users\\User\\Desktop\\CobaltRust\\CobaltRust\\Bundles\\shared\\glass_ak.bundle",
			Path.Combine(Paths.PluginPath, "glass_ak.bundle"),
			Path.Combine(Paths.PluginPath, "textures.5.bundle")
		};
		foreach (string path in array2)
		{
			if (!File.Exists(path))
			{
				continue;
			}
			try
			{
				AssetBundle assetBundle2 = AssetBundle.LoadFromFile(path);
				if (assetBundle2 != null)
				{
					_bundle = assetBundle2;
					break;
				}
			}
			catch
			{
			}
		}
	}

	public static T GetAsset<T>(string name) where T : UnityEngine.Object
	{
		if (_bundle == null)
		{
			Init();
		}
		if (_bundle == null)
		{
			return null;
		}
		if (_cache.TryGetValue(name, out var value) && value != null)
		{
			return value.TryCast<T>() ?? ((T)((value is T) ? value : null));
		}
		try
		{
			T val = _bundle.LoadAsset<T>(name);
			if ((UnityEngine.Object)val != (UnityEngine.Object)null)
			{
				_cache[name] = val;
				return val;
			}
		}
		catch
		{
		}
		return null;
	}
}
