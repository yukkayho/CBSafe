using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CBSafe.Crosshair;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CBSafe.Core.Patches;

public static class GlovesSkinApplier
{
	private const string StealthPrefix = "Gloves_Stealth";

	internal static readonly string[] SkinPrefixes = new string[8] { "Gloves_Stealth", "Gloves_3772935132", "Gloves_3732758382", "Gloves_3607105045", "Gloves_3709631672", "Gloves_3620171303", "Gloves_3752934749", "Gloves_3662995121" };

	internal static readonly string[] SkinLabels = new string[8] { "Стелс (Локальный)", "Stealth Tactical Gloves", "Blackout Tactical Gloves V2", "Whiteout Tactical Gloves", "Oilrats Tactical Gloves", "Alucardo's Gloves", "AURELIAN", "Lunar Red Horse" };

	private static Texture2D? _mainTex;

	private static Texture2D? _bumpTex;

	private static Texture2D? _specTex;

	private static Texture2D? _occTex;

	private static bool _texturesLoaded;

	private static int _loadedIndex = -1;

	private static readonly HashSet<int> _appliedRenderers = new HashSet<int>();

	private static int _lastActiveModelId;

	private static float _mfIntensity;

	private static float _mfScatter = 0.704f;

	private static float _mfOcclusion = 1f;

	private static Color _mfColor = Color.white;

	private static float _glossiness = 1f;

	private static float _occStrength = 0.7f;

	private static bool _hasManifest;

	private static bool _missingLogged;

	internal static bool SkinEnabled
	{
		get
		{
			try
			{
				if (Defines.GlovesSkinEnabled == null)
				{
					return true;
				}
				return Defines.GlovesSkinEnabled.Value;
			}
			catch
			{
				return false;
			}
		}
	}

	internal static int CurrentSkinIndex
	{
		get
		{
			try
			{
				if (Defines.GlovesSkinIndex != null)
				{
					int value = Defines.GlovesSkinIndex.Value;
					if (value >= 0 && value < SkinPrefixes.Length)
					{
						return value;
					}
				}
			}
			catch
			{
			}
			return 0;
		}
	}

	private static void LogInfo(string msg)
	{
		try
		{
			CBSafeCrosshairPlugin.Instance?.Log?.LogInfo("[Gloves] " + msg);
		}
		catch
		{
		}
	}

	private static void LogWarn(string msg)
	{
		try
		{
			CBSafeCrosshairPlugin.Instance?.Log?.LogWarning("[Gloves] " + msg);
		}
		catch
		{
		}
	}

	private static void LogError(string msg)
	{
		try
		{
			CBSafeCrosshairPlugin.Instance?.Log?.LogError("[Gloves] " + msg);
		}
		catch
		{
		}
	}

	private static string[] SkinDirectories()
	{
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		return new string[2]
		{
			Path.Combine(baseDirectory, "BepInEx", "plugins", "Skins"),
			Path.Combine(baseDirectory, "Skins")
		};
	}

	private static Texture2D? LoadFromDisk(string fileName)
	{
		string[] array = SkinDirectories();
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				string path = Path.Combine(array[i], fileName);
				if (File.Exists(path))
				{
					return LoadTextureFromFile(path);
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static Texture2D? LoadFromResource(string fileName)
	{
		try
		{
			using Stream stream = typeof(GlovesSkinApplier).Assembly.GetManifestResourceStream(fileName);
			if (stream == null)
			{
				return null;
			}
			byte[] array;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				stream.CopyTo(memoryStream);
				array = memoryStream.ToArray();
			}
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
			if (!texture2D.LoadImage(array))
			{
				UnityEngine.Object.Destroy(texture2D);
				return null;
			}
			texture2D.Apply(updateMipmaps: true, makeNoLongerReadable: false);
			texture2D.hideFlags = HideFlags.DontUnloadUnusedAsset;
			return texture2D;
		}
		catch
		{
			return null;
		}
	}

	private static string? ReadText(string fileName)
	{
		string[] array = SkinDirectories();
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				string path = Path.Combine(array[i], fileName);
				if (File.Exists(path))
				{
					return File.ReadAllText(path);
				}
			}
			catch
			{
			}
		}
		try
		{
			using Stream stream = typeof(GlovesSkinApplier).Assembly.GetManifestResourceStream(fileName);
			if (stream == null)
			{
				return null;
			}
			using StreamReader streamReader = new StreamReader(stream);
			return streamReader.ReadToEnd();
		}
		catch
		{
			return null;
		}
	}

	private static string? FallbackFile(string prefix, string suffix)
	{
		if (!(prefix == "Gloves_Stealth"))
		{
			return null;
		}
		return "Gloves" + suffix;
	}

	private static Texture2D? LoadTexture(string prefix, string suffix)
	{
		string fileName = prefix + suffix;
		Texture2D texture2D = LoadFromDisk(fileName);
		if (texture2D != null)
		{
			return texture2D;
		}
		string text = FallbackFile(prefix, suffix);
		if (text != null)
		{
			texture2D = LoadFromDisk(text);
			if (texture2D != null)
			{
				return texture2D;
			}
		}
		texture2D = LoadFromResource(fileName);
		if (texture2D != null)
		{
			return texture2D;
		}
		if (text == null)
		{
			return null;
		}
		return LoadFromResource(text);
	}

	private static Texture2D LoadTextureFromFile(string path)
	{
		try
		{
			byte[] array = File.ReadAllBytes(path);
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
			if (!texture2D.LoadImage(array))
			{
				UnityEngine.Object.Destroy(texture2D);
				return null;
			}
			texture2D.Apply(updateMipmaps: true, makeNoLongerReadable: false);
			texture2D.hideFlags = HideFlags.DontUnloadUnusedAsset;
			return texture2D;
		}
		catch (Exception ex)
		{
			LogWarn("[Gloves] LoadTex " + path + ": " + ex.Message);
			return null;
		}
	}

	private static void DestroyTextures()
	{
		try
		{
			if (_mainTex != null)
			{
				UnityEngine.Object.Destroy(_mainTex);
			}
			if (_bumpTex != null)
			{
				UnityEngine.Object.Destroy(_bumpTex);
			}
			if (_specTex != null)
			{
				UnityEngine.Object.Destroy(_specTex);
			}
			if (_occTex != null)
			{
				UnityEngine.Object.Destroy(_occTex);
			}
		}
		catch
		{
		}
		_mainTex = null;
		_bumpTex = null;
		_specTex = null;
		_occTex = null;
		_texturesLoaded = false;
		_loadedIndex = -1;
	}

	internal static void ReloadTextures(int index)
	{
		DestroyTextures();
		_appliedRenderers.Clear();
		_lastActiveModelId = 0;
		LoadTexturesByIndex(index);
	}

	internal static void EnsureTexturesLoaded()
	{
		int currentSkinIndex = CurrentSkinIndex;
		if (!_texturesLoaded || _loadedIndex != currentSkinIndex || _mainTex == null)
		{
			LoadTexturesByIndex(currentSkinIndex);
		}
	}

	private static void LoadTexturesByIndex(int index)
	{
		try
		{
			if (index < 0 || index >= SkinPrefixes.Length)
			{
				index = 0;
			}
			string text = SkinPrefixes[index];
			_mainTex = LoadTexture(text, ".png");
			_bumpTex = LoadTexture(text, "_Bump.png");
			_specTex = LoadTexture(text, "_Spec.png");
			_occTex = LoadTexture(text, "_Occ.png");
			LoadManifest(text, FallbackFile(text, "_manifest.txt"));
			_loadedIndex = index;
			_texturesLoaded = _mainTex != null;
			if (_texturesLoaded)
			{
				LogInfo("[Gloves] Текстуры загружены: " + text + " (m=" + (_bumpTex != null) + " s=" + (_specTex != null) + " o=" + (_occTex != null) + " manifest=" + _hasManifest + ")");
			}
			else if (!_missingLogged)
			{
				_missingLogged = true;
				LogWarn("[Gloves] Текстуры не найдены ни на диске, ни в DLL: " + text);
			}
		}
		catch (Exception ex)
		{
			LogError("[Gloves] LoadTexturesByIndex: " + ex);
		}
	}

	private static float ParseFloat(string json, string key, float def)
	{
		try
		{
			string text = "\"" + key + "\"";
			int num = json.IndexOf(text, StringComparison.Ordinal);
			if (num < 0)
			{
				return def;
			}
			int num2 = json.IndexOf(':', num + text.Length);
			if (num2 < 0)
			{
				return def;
			}
			int i;
			for (i = num2 + 1; i < json.Length && (json[i] == ' ' || json[i] == '\r' || json[i] == '\n'); i++)
			{
			}
			int j;
			for (j = i; j < json.Length && json[j] != ',' && json[j] != '}' && json[j] != '\r' && json[j] != '\n'; j++)
			{
			}
			if (float.TryParse(json.Substring(i, j - i).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return result;
			}
			return def;
		}
		catch
		{
			return def;
		}
	}

	private static void LoadManifest(string prefix, string? fallbackFile)
	{
		_hasManifest = false;
		_mfIntensity = 0f;
		_mfScatter = 0.704f;
		_mfOcclusion = 1f;
		_mfColor = Color.white;
		_glossiness = 1f;
		_occStrength = 0.7f;
		string text = ReadText(prefix + "_manifest.txt");
		if (string.IsNullOrEmpty(text))
		{
			text = ReadText(prefix + "_manifest.json");
		}
		if (text == null && fallbackFile != null)
		{
			text = ReadText(fallbackFile);
		}
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		try
		{
			string text2 = text;
			_glossiness = ParseFloat(text2, "_Glossiness", 1f);
			_occStrength = ParseFloat(text2, "_OcclusionStrength", 0.7f);
			_mfIntensity = ParseFloat(text2, "_MicrofiberFuzzIntensity", 0f);
			_mfScatter = ParseFloat(text2, "_MicrofiberFuzzScatter", 0.704f);
			_mfOcclusion = ParseFloat(text2, "_MicrofiberFuzzOcclusion", 1f);
			int num = text2.IndexOf("_MicrofiberFuzzColor", StringComparison.Ordinal);
			if (num >= 0)
			{
				string json = text2.Substring(num);
				_mfColor = new Color(ParseFloat(json, "r", 1f), ParseFloat(json, "g", 1f), ParseFloat(json, "b", 1f), 1f);
			}
			_hasManifest = true;
		}
		catch (Exception ex)
		{
			LogWarn("[Gloves] Manifest " + prefix + ": " + ex.Message);
		}
	}

	internal static void Tick()
	{
		if (!SkinEnabled)
		{
			return;
		}
		try
		{
			if (LocalPlayer.Entity == null)
			{
				return;
			}
		}
		catch
		{
			return;
		}
		EnsureTexturesLoaded();
		if (_mainTex == null)
		{
			return;
		}
		try
		{
			BaseViewModel activeModel = BaseViewModel.ActiveModel;
			if (activeModel != null)
			{
				int instanceID = activeModel.GetInstanceID();
				if (instanceID != _lastActiveModelId)
				{
					_lastActiveModelId = instanceID;
					_appliedRenderers.Clear();
				}
				ApplyToViewModel(activeModel);
			}
			BasePlayer entity = LocalPlayer.Entity;
			if (entity != null)
			{
				ApplyToLocalPlayer(entity);
			}
		}
		catch (Exception ex)
		{
			LogWarn("[Gloves] Tick: " + ex.Message);
		}
	}

	internal static void OnUpdateRenderers(BaseViewModel model)
	{
		ApplySafe(model, "UpdateRenderers");
	}

	internal static void OnClothingUpdated(BaseViewModel model)
	{
		try
		{
			_appliedRenderers.Clear();
		}
		catch
		{
		}
		ApplySafe(model, "ClothingUpdated");
	}

	internal static void OnClothingChanged(BaseViewModel model)
	{
		try
		{
			_appliedRenderers.Clear();
		}
		catch
		{
		}
		ApplySafe(model, "ClothingChanged");
	}

	private static void ApplySafe(BaseViewModel model, string source)
	{
		if (!SkinEnabled)
		{
			return;
		}
		try
		{
			ApplyToViewModel(model);
		}
		catch (Exception ex)
		{
			LogWarn("[Gloves] " + source + ": " + ex.Message);
		}
	}

	internal static void ApplyToViewModel(BaseViewModel model)
	{
		if (model == null)
		{
			return;
		}
		EnsureTexturesLoaded();
		if (_mainTex == null)
		{
			return;
		}
		GameObject gameObject = model.gameObject;
		if (gameObject == null)
		{
			return;
		}
		bool glovesEquipped = IsTacticalGlovesEquipped();
		Il2CppArrayBase<Renderer> componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
		if (componentsInChildren != null)
		{
			foreach (Renderer item in componentsInChildren)
			{
				ProcessViewModelRenderer(item, glovesEquipped);
			}
		}
		GameObject clothing = model.Clothing;
		if (clothing == null)
		{
			return;
		}
		Il2CppArrayBase<Renderer> componentsInChildren2 = clothing.GetComponentsInChildren<Renderer>(includeInactive: true);
		if (componentsInChildren2 == null)
		{
			return;
		}
		foreach (Renderer item2 in componentsInChildren2)
		{
			ProcessViewModelRenderer(item2, glovesEquipped: true);
		}
	}

	private static bool IsGloveName(string value)
	{
		if (!value.Contains("tactical") && !value.Contains("glove") && !value.Contains("gloves"))
		{
			return value.Contains("glov");
		}
		return true;
	}

	private static void ProcessViewModelRenderer(Renderer rend, bool glovesEquipped)
	{
		if (rend == null)
		{
			return;
		}
		rend.GetInstanceID();
		bool flag = IsGloveName(rend.name.ToLower());
		bool flag2 = false;
		bool flag3 = false;
		Il2CppReferenceArray<Material> sharedMaterials = rend.sharedMaterials;
		if (sharedMaterials != null)
		{
			foreach (Material item in sharedMaterials)
			{
				if (!(item == null))
				{
					string text = item.name.ToLower();
					if (IsGloveName(text))
					{
						flag2 = true;
						break;
					}
					Shader shader = item.shader;
					if (text.Contains("viewmodel_arm") || text.Contains("viewmodel_hand") || (shader != null && shader.name.Contains("Core/Skin")))
					{
						flag3 = true;
					}
				}
			}
		}
		if (!flag3 && (flag || flag2))
		{
			ApplySkinToRenderer(rend);
		}
	}

	internal static void ApplyToLocalPlayer(BasePlayer player)
	{
		if (player == null)
		{
			return;
		}
		EnsureTexturesLoaded();
		if (_mainTex == null)
		{
			return;
		}
		PlayerModel component = player.GetComponent<PlayerModel>();
		if (component == null)
		{
			return;
		}
		Il2CppArrayBase<Renderer> componentsInChildren = component.GetComponentsInChildren<Renderer>(includeInactive: true);
		if (componentsInChildren == null)
		{
			return;
		}
		foreach (Renderer item in componentsInChildren)
		{
			if (!(item == null))
			{
				string text = item.name.ToLower();
				if (text.Contains("tactical") || text.Contains("glove") || text.Contains("gloves"))
				{
					ApplySkinToRenderer(item);
				}
			}
		}
	}

	internal static void ApplySkinToRenderer(Renderer rend)
	{
		if (rend == null || _mainTex == null)
		{
			return;
		}
		int instanceID = rend.GetInstanceID();
		if (_appliedRenderers.Contains(instanceID))
		{
			return;
		}
		_appliedRenderers.Add(instanceID);
		try
		{
			Il2CppReferenceArray<Material> materials = rend.materials;
			if (materials != null)
			{
				foreach (Material item in materials)
				{
					if (item != null)
					{
						ApplySkinToMaterial(item, rend.name);
					}
				}
			}
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			rend.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetTexture("_MainTex", _mainTex);
			materialPropertyBlock.SetTexture("_BaseMap", _mainTex);
			if (_bumpTex != null)
			{
				materialPropertyBlock.SetTexture("_BumpMap", _bumpTex);
				materialPropertyBlock.SetTexture("_NormalMap", _bumpTex);
			}
			if (_specTex != null)
			{
				materialPropertyBlock.SetTexture("_SpecGlossMap", _specTex);
				materialPropertyBlock.SetTexture("_MetallicGlossMap", _specTex);
			}
			if (_occTex != null)
			{
				materialPropertyBlock.SetTexture("_OcclusionMap", _occTex);
			}
			if (_hasManifest)
			{
				materialPropertyBlock.SetFloat("_Glossiness", _glossiness);
				materialPropertyBlock.SetFloat("_OcclusionStrength", _occStrength);
				materialPropertyBlock.SetFloat("_MicrofiberFuzzIntensity", _mfIntensity);
				materialPropertyBlock.SetFloat("_MicrofiberFuzzScatter", _mfScatter);
				materialPropertyBlock.SetFloat("_MicrofiberFuzzOcclusion", _mfOcclusion);
				materialPropertyBlock.SetColor("_MicrofiberFuzzColor", _mfColor);
			}
			rend.SetPropertyBlock(materialPropertyBlock);
		}
		catch (Exception ex)
		{
			LogWarn("[Gloves] Apply '" + rend.name + "': " + ex.Message);
		}
	}

	private static void ApplySkinToMaterial(Material mat, string rendName)
	{
		if (mat == null || _mainTex == null)
		{
			return;
		}
		try
		{
			try
			{
				mat.mainTexture = _mainTex;
			}
			catch
			{
			}
			if (mat.HasProperty("_MainTex"))
			{
				mat.SetTexture("_MainTex", _mainTex);
			}
			if (mat.HasProperty("_BaseMap"))
			{
				mat.SetTexture("_BaseMap", _mainTex);
			}
			if (_bumpTex != null)
			{
				if (mat.HasProperty("_BumpMap"))
				{
					mat.SetTexture("_BumpMap", _bumpTex);
				}
				if (mat.HasProperty("_NormalMap"))
				{
					mat.SetTexture("_NormalMap", _bumpTex);
				}
				try
				{
					mat.EnableKeyword("_NORMALMAP");
				}
				catch
				{
				}
			}
			if (_specTex != null)
			{
				if (mat.HasProperty("_SpecGlossMap"))
				{
					mat.SetTexture("_SpecGlossMap", _specTex);
				}
				if (mat.HasProperty("_MetallicGlossMap"))
				{
					mat.SetTexture("_MetallicGlossMap", _specTex);
				}
				try
				{
					mat.EnableKeyword("_SPECGLOSSMAP");
				}
				catch
				{
				}
			}
			if (_occTex != null && mat.HasProperty("_OcclusionMap"))
			{
				mat.SetTexture("_OcclusionMap", _occTex);
			}
			if (_hasManifest)
			{
				if (mat.HasProperty("_Glossiness"))
				{
					mat.SetFloat("_Glossiness", _glossiness);
				}
				if (mat.HasProperty("_OcclusionStrength"))
				{
					mat.SetFloat("_OcclusionStrength", _occStrength);
				}
				if (mat.HasProperty("_MicrofiberFuzzIntensity"))
				{
					mat.SetFloat("_MicrofiberFuzzIntensity", _mfIntensity);
				}
				if (mat.HasProperty("_MicrofiberFuzzScatter"))
				{
					mat.SetFloat("_MicrofiberFuzzScatter", _mfScatter);
				}
				if (mat.HasProperty("_MicrofiberFuzzOcclusion"))
				{
					mat.SetFloat("_MicrofiberFuzzOcclusion", _mfOcclusion);
				}
				if (mat.HasProperty("_MicrofiberFuzzColor"))
				{
					mat.SetColor("_MicrofiberFuzzColor", _mfColor);
				}
			}
		}
		catch (Exception ex)
		{
			LogWarn("[Gloves] Material '" + mat.name + "' (" + rendName + "): " + ex.Message);
		}
	}

	internal static bool IsTacticalGlovesEquipped()
	{
		try
		{
			BasePlayer entity = LocalPlayer.Entity;
			if (entity == null)
			{
				return false;
			}
			PlayerInventory inventory = entity.inventory;
			if (inventory == null)
			{
				return false;
			}
			ItemContainer containerWear = inventory.containerWear;
			if (containerWear == null)
			{
				return false;
			}
			Il2CppSystem.Collections.Generic.List<Item> itemList = containerWear.itemList;
			if (itemList == null)
			{
				return false;
			}
			for (int i = 0; i < itemList.Count; i++)
			{
				Item item = itemList[i];
				if (item != null && !(item.info == null))
				{
					string shortname = item.info.shortname;
					if (!string.IsNullOrEmpty(shortname) && (shortname.IndexOf("tactical.gloves", StringComparison.OrdinalIgnoreCase) >= 0 || shortname.IndexOf("glove", StringComparison.OrdinalIgnoreCase) >= 0))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}
}
