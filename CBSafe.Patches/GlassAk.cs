using System;
using System.Collections.Generic;
using ConVar;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Audio;

namespace CBSafe.Patches;

public static class GlassAk
{
	[HarmonyPatch(typeof(ViewModel), "Deploy")]
	internal static class ViewModelDeployPatch
	{
		[HarmonyPostfix]
		public static void Postfix(ViewModel __instance, BaseEntity parent)
		{
			if (!(__instance == null) && !(__instance.instance == null) && !(parent == null) && parent.ShortPrefabName != null && parent.ShortPrefabName.Contains("ak47"))
			{
				ItemIconPatch.SyncIcon();
				if (ModConfig.GlassAkEnabled.Value)
				{
					ApplyViewModel(__instance.instance);
				}
			}
		}
	}

	[HarmonyPatch(typeof(AnimationEvents), "DoEffect")]
	internal static class AnimationEventsDoEffectPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(AnimationEvents __instance, AnimationEvent evt)
		{
			if (!ModConfig.GlassAkEnabled.Value || evt == null)
			{
				return true;
			}
			string stringParameter = evt.stringParameter;
			if (string.IsNullOrEmpty(stringParameter) || !stringParameter.StartsWith("glass-ak"))
			{
				return true;
			}
			if (Audio.master <= 0.001f || Audio.game <= 0.001f)
			{
				AudioSource component = __instance.GetComponent<AudioSource>();
				if (component != null && component.isPlaying)
				{
					component.Stop();
				}
				return false;
			}
			AudioClip asset = AssetFactory.GetAsset<AudioClip>(stringParameter);
			if (asset == null)
			{
				return true;
			}
			AudioSource audioSource = __instance.GetComponent<AudioSource>() ?? __instance.gameObject.AddComponent<AudioSource>();
			audioSource.spatialBlend = 0f;
			audioSource.volume = Audio.master * Audio.game;
			SoundManager instance = SingletonComponent<SoundManager>.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				SoundClass defaultSoundClass = instance.defaultSoundClass;
				obj = ((defaultSoundClass != null) ? defaultSoundClass.firstPersonOutput : null);
			}
			if (obj == null)
			{
				SoundManager instance2 = SingletonComponent<SoundManager>.Instance;
				if (instance2 == null)
				{
					obj = null;
				}
				else
				{
					SoundClass defaultSoundClass2 = instance2.defaultSoundClass;
					obj = ((defaultSoundClass2 != null) ? defaultSoundClass2.output : null);
				}
			}
			audioSource.outputAudioMixerGroup = (AudioMixerGroup)obj;
			audioSource.PlayOneShot(asset, audioSource.volume);
			return false;
		}
	}

	private static Texture2D _frostedNormalTex;

	private static Texture2D _flatNormalTex;

	private static Texture2D _whiteAlbedoTex;

	private static Shader _rustStandardShader;

	private static float _curR = 0.85f;

	private static float _curG = 0.95f;

	private static float _curB = 1f;

	private static float _curA = 0.35f;

	private static float _curGloss = 0.45f;

	private static float _curSpecMul = 1.3f;

	private static float _curSpecTint = 0.25f;

	private static float _curEm = 0.04f;

	private static float _curTr = 0.35f;

	private static float _curBump = 0.8f;

	private static float _curBoost = 1f;

	private static readonly List<Material> _liveGlassMats = new List<Material>();

	public static Texture2D GetFrostedNormalTexture()
	{
		if (_frostedNormalTex != null)
		{
			return _frostedNormalTex;
		}
		int num = 64;
		_frostedNormalTex = new Texture2D(num, num, TextureFormat.RGBA32, mipChain: true, linear: true);
		_frostedNormalTex.name = "mad_frosted_normal";
		_frostedNormalTex.filterMode = FilterMode.Trilinear;
		Color32[] array = new Color32[num * num];
		System.Random random = new System.Random(42);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				float num2 = (float)(random.NextDouble() * 2.0 - 1.0) * 0.25f;
				float num3 = (float)(random.NextDouble() * 2.0 - 1.0) * 0.25f;
				num2 += Mathf.Sin((float)j * 0.5f + (float)i * 0.25f) * 0.12f;
				num3 += Mathf.Cos((float)i * 0.5f - (float)j * 0.25f) * 0.12f;
				byte b = (byte)Mathf.Clamp(128 + (int)(num2 * 127f), 0, 255);
				byte g = (byte)Mathf.Clamp(128 + (int)(num3 * 127f), 0, 255);
				array[i * num + j] = new Color32(b, g, byte.MaxValue, b);
			}
		}
		_frostedNormalTex.SetPixels32(array);
		_frostedNormalTex.Apply(updateMipmaps: true, makeNoLongerReadable: false);
		return _frostedNormalTex;
	}

	public static Texture2D GetFlatNormalTexture()
	{
		if (_flatNormalTex != null)
		{
			return _flatNormalTex;
		}
		_flatNormalTex = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false, linear: true);
		_flatNormalTex.name = "mad_flat_normal";
		Color32 color = new Color32(128, 128, byte.MaxValue, 128);
		Color32[] array = new Color32[16];
		for (int i = 0; i < 16; i++)
		{
			array[i] = color;
		}
		_flatNormalTex.SetPixels32(array);
		_flatNormalTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
		return _flatNormalTex;
	}

	public static Texture2D GetWhiteAlbedoTexture()
	{
		if (_whiteAlbedoTex != null)
		{
			return _whiteAlbedoTex;
		}
		_whiteAlbedoTex = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false, linear: false);
		_whiteAlbedoTex.name = "mad_white_albedo";
		Color32 color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		Color32[] array = new Color32[16];
		for (int i = 0; i < 16; i++)
		{
			array[i] = color;
		}
		_whiteAlbedoTex.SetPixels32(array);
		_whiteAlbedoTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
		return _whiteAlbedoTex;
	}

	public static Shader GetConfiguredShader()
	{
		if (_rustStandardShader != null)
		{
			return _rustStandardShader;
		}
		_rustStandardShader = Shader.Find("Rust/Standard");
		if (_rustStandardShader == null)
		{
			_rustStandardShader = Shader.Find("Standard");
		}
		if (_rustStandardShader == null)
		{
			_rustStandardShader = Shader.Find("Standard (Specular setup)");
		}
		return _rustStandardShader;
	}

	public static void SetColor(string colorKey)
	{
		if (!string.IsNullOrEmpty(colorKey))
		{
			string text = colorKey.ToLower();
			ModConfig.GlassColor.Value = text;
			int num = Array.IndexOf(ModConfig.ColorKeys, text);
			if (num >= 0)
			{
				ModConfig.GlassColorIndex.Value = num;
			}
			ApplyColorKey(text);
			ModConfig.Save();
			ApplyActive();
		}
	}

	public static void ApplyColorKey(string text)
	{
		switch (text)
		{
		case "red":
			SetColorParameters(1f, 0.05f, 0.05f, 0.65f, 0.5f, 1.4f, 0.6f, 0.06f, 0.4f, 0.6f, 1.2f);
			break;
		case "blue":
			SetColorParameters(0.08f, 0.5f, 1f, 0.6f, 0.5f, 1.5f, 0.55f, 0.06f, 0.45f, 0.6f, 1.2f);
			break;
		case "green":
			SetColorParameters(0.1f, 1f, 0.25f, 0.6f, 0.5f, 1.5f, 0.55f, 0.06f, 0.45f, 0.6f, 1.2f);
			break;
		case "purple":
			SetColorParameters(0.7f, 0.1f, 1f, 0.6f, 0.5f, 1.5f, 0.55f, 0.06f, 0.45f, 0.6f, 1.2f);
			break;
		case "gold":
			SetColorParameters(1f, 0.8f, 0.1f, 0.65f, 0.55f, 1.8f, 0.6f, 0.06f, 0.45f, 0.5f, 1.2f);
			break;
		case "pink":
			SetColorParameters(1f, 0.25f, 0.75f, 0.6f, 0.5f, 1.6f, 0.6f, 0.08f, 0.45f, 0.6f, 1.2f);
			break;
		case "diamond":
			SetColorParameters(1f, 1f, 1f, 0.2f, 0.6f, 1.8f, 0.3f, 0.05f, 0.5f, 0.4f, 1f);
			break;
		case "smoked":
			SetColorParameters(0.15f, 0.15f, 0.15f, 0.75f, 0.5f, 1.2f, 0.2f, 0.02f, 0.2f, 0.3f, 1f);
			break;
		case "amber":
			SetColorParameters(1f, 0.45f, 0.05f, 0.6f, 0.5f, 1.5f, 0.5f, 0.06f, 0.4f, 0.6f, 1.2f);
			break;
		case "cyan":
			SetColorParameters(0.05f, 0.9f, 0.95f, 0.55f, 0.5f, 1.5f, 0.4f, 0.06f, 0.45f, 0.6f, 1.2f);
			break;
		case "lime":
			SetColorParameters(0.55f, 1f, 0.1f, 0.55f, 0.5f, 1.5f, 0.5f, 0.06f, 0.4f, 0.6f, 1.2f);
			break;
		case "blood":
			SetColorParameters(0.7f, 0.02f, 0.02f, 0.75f, 0.55f, 1.3f, 0.4f, 0.04f, 0.3f, 0.5f, 1f);
			break;
		case "violet":
			SetColorParameters(0.4f, 0.05f, 0.85f, 0.65f, 0.5f, 1.5f, 0.55f, 0.06f, 0.45f, 0.6f, 1.2f);
			break;
		case "rainbow":
			SetColorParameters(1f, 0.2f, 0.5f, 0.6f, 0.5f, 1.5f, 0.5f, 0.06f, 0.45f, 0.6f, 1.2f);
			break;
		case "rgb":
			SetColorParameters(1f, 0.2f, 0.5f, 0.5f, 0.5f, 1.5f, 0.5f, 0.06f, 0.45f, 0.6f, 1.2f);
			break;
		case "clear":
			SetColorParameters(1f, 1f, 1f, 0.5f, 0.55f, 1.6f, 0.45f, 0.05f, 0.7f, 0.55f, 1.1f);
			break;
		default:
			SetColorParameters(0.85f, 0.95f, 1f, 0.35f, 0.45f, 1.3f, 0.25f, 0.04f, 0.35f, 0.8f, 1f);
			break;
		}
	}

	public static void ApplyCurrentColor()
	{
		string currentColorKey = ModConfig.CurrentColorKey;
		if (!string.IsNullOrEmpty(currentColorKey))
		{
			ApplyColorKey(currentColorKey.ToLower());
		}
	}

	private static void SetColorParameters(float r, float g, float b, float a, float gloss, float specMul, float specTint, float em, float tr, float bump, float boost)
	{
		_curR = r;
		_curG = g;
		_curB = b;
		_curA = a;
		_curGloss = gloss;
		_curSpecMul = specMul;
		_curSpecTint = specTint;
		_curEm = em;
		_curTr = tr;
		_curBump = bump;
		_curBoost = boost;
	}

	public static Material GetGlassMaterial()
	{
		Shader configuredShader = GetConfiguredShader();
		Material obj = new Material(configuredShader)
		{
			name = "mad_glass_ak_mat"
		};
		RememberGlassMat(obj);
		Color color = new Color(Mathf.Clamp01(_curR * _curBoost), Mathf.Clamp01(_curG * _curBoost), Mathf.Clamp01(_curB * _curBoost), _curA);
		Color specColor = Color.Lerp(Color.white * _curSpecMul, color * _curSpecMul, _curSpecTint);
		specColor.a = _curGloss;
		Color emColor = new Color(_curR * _curEm, _curG * _curEm, _curB * _curEm, 1f);
		Color trColor = new Color(_curR * _curTr, _curG * _curTr, _curB * _curTr, 1f);
		UnityEngine.Texture normalTex = ((_curBump > 0.02f) ? GetFrostedNormalTexture() : GetFlatNormalTexture());
		UnityEngine.Texture albedoTex = GetWhiteAlbedoTexture();
		TuneMaterial(obj, configuredShader, normalTex, albedoTex, color, specColor, emColor, trColor, _curGloss, 0f, _curBump, _curEm, _curTr);
		return obj;
	}

	private static void TuneMaterial(Material mat, Shader shader, UnityEngine.Texture normalTex, UnityEngine.Texture albedoTex, Color baseColor, Color specColor, Color emColor, Color trColor, float gloss, float metallic, float bump, float em, float tr)
	{
		if (mat == null)
		{
			return;
		}
		if (shader != null && mat.shader != shader)
		{
			mat.shader = shader;
		}
		mat.color = baseColor;
		if (mat.HasProperty("_Color"))
		{
			mat.SetColor("_Color", baseColor);
		}
		if (mat.HasProperty("_BaseColor"))
		{
			mat.SetColor("_BaseColor", baseColor);
		}
		if (mat.HasProperty("_SpecColor"))
		{
			mat.SetColor("_SpecColor", specColor);
		}
		if (mat.HasProperty("_Glossiness"))
		{
			mat.SetFloat("_Glossiness", gloss);
		}
		if (mat.HasProperty("_Smoothness"))
		{
			mat.SetFloat("_Smoothness", gloss);
		}
		if (mat.HasProperty("_Metallic"))
		{
			mat.SetFloat("_Metallic", metallic);
		}
		if (mat.HasProperty("_EmissionColor"))
		{
			mat.SetColor("_EmissionColor", emColor);
		}
		if (mat.HasProperty("_TransmissionColor"))
		{
			mat.SetColor("_TransmissionColor", trColor);
		}
		if (mat.HasProperty("_Transmission"))
		{
			mat.SetFloat("_Transmission", tr);
		}
		if (normalTex != null)
		{
			if (mat.HasProperty("_BumpMap"))
			{
				mat.SetTexture("_BumpMap", normalTex);
			}
			if (mat.HasProperty("_BumpScale"))
			{
				mat.SetFloat("_BumpScale", bump);
			}
			mat.EnableKeyword("_NORMALMAP");
		}
		if (albedoTex != null && mat.HasProperty("_MainTex"))
		{
			mat.SetTexture("_MainTex", albedoTex);
		}
		mat.SetFloat("_Mode", 3f);
		mat.SetFloat("_SrcBlend", 1f);
		mat.SetFloat("_DstBlend", 10f);
		mat.SetFloat("_ZWrite", 0f);
		mat.DisableKeyword("_ALPHABLEND_ON");
		mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
		mat.EnableKeyword("_DOUBLESIDED_ON");
		mat.renderQueue = 3100;
		if (em > 0.005f)
		{
			mat.EnableKeyword("_EMISSION");
		}
		else
		{
			mat.DisableKeyword("_EMISSION");
		}
	}

	public static void ApplyActive()
	{
		try
		{
			BasePlayer entity = LocalPlayer.Entity;
			if (entity == null)
			{
				return;
			}
			HeldEntity heldEntity = entity.GetHeldEntity();
			if (heldEntity == null)
			{
				return;
			}
			ViewModel component = heldEntity.GetComponent<ViewModel>();
			if (component == null || component.instance == null)
			{
				return;
			}
			Shader configuredShader = GetConfiguredShader();
			Color color = new Color(Mathf.Clamp01(_curR * _curBoost), Mathf.Clamp01(_curG * _curBoost), Mathf.Clamp01(_curB * _curBoost), _curA);
			Color specColor = Color.Lerp(Color.white * _curSpecMul, color * _curSpecMul, _curSpecTint);
			specColor.a = _curGloss;
			Color emColor = new Color(_curR * _curEm, _curG * _curEm, _curB * _curEm, 1f);
			Color trColor = new Color(_curR * _curTr, _curG * _curTr, _curB * _curTr, 1f);
			UnityEngine.Texture normalTex = ((_curBump > 0.02f) ? GetFrostedNormalTexture() : GetFlatNormalTexture());
			UnityEngine.Texture albedoTex = GetWhiteAlbedoTexture();
			Il2CppArrayBase<Renderer> componentsInChildren = component.instance.GetComponentsInChildren<Renderer>(includeInactive: true);
			if (componentsInChildren == null)
			{
				return;
			}
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Renderer renderer = componentsInChildren[i];
				if (renderer == null || !renderer.name.StartsWith("ak47"))
				{
					continue;
				}
				Il2CppReferenceArray<Material> materials = renderer.materials;
				if (materials == null)
				{
					continue;
				}
				bool flag = false;
				for (int j = 0; j < ((Il2CppArrayBase<Material>)materials).Length; j++)
				{
					Material material = ((Il2CppArrayBase<Material>)materials)[j];
					if (material != null && (material.name.Contains("transp") || j == 1) && !material.name.Contains("ammo"))
					{
						TuneMaterial(material, configuredShader, normalTex, albedoTex, color, specColor, emColor, trColor, _curGloss, 0f, _curBump, _curEm, _curTr);
						RememberGlassMat(material);
						flag = true;
					}
				}
				if (!flag)
				{
					continue;
				}
				renderer.materials = materials;
				ViewModelRenderer component2 = renderer.GetComponent<ViewModelRenderer>();
				if (component2 != null)
				{
					try
					{
						component2.RemoveDrawEvents();
					}
					catch
					{
					}
					try
					{
						component2.CacheReferences();
					}
					catch
					{
					}
					try
					{
						component2.AddDrawEvents();
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void SetupViewModelRenderer(Component comp, bool enableEvents)
	{
		if (comp == null)
		{
			return;
		}
		ViewModelRenderer component = comp.GetComponent<ViewModelRenderer>();
		if (!(component != null))
		{
			return;
		}
		try
		{
			component.RemoveDrawEvents();
		}
		catch
		{
		}
		if (!enableEvents)
		{
			return;
		}
		try
		{
			component.CacheReferences();
		}
		catch
		{
		}
		try
		{
			component.AddDrawEvents();
		}
		catch
		{
		}
	}

	private static void EnsureBonesExist(SkinnedMeshRenderer sourceSmr, Dictionary<string, Transform> targetBones)
	{
		if (sourceSmr == null || targetBones == null)
		{
			return;
		}
		Il2CppReferenceArray<Transform> bones = sourceSmr.bones;
		if (bones == null)
		{
			return;
		}
		Transform value = null;
		targetBones.TryGetValue("joint8", out value);
		if (value == null)
		{
			using Dictionary<string, Transform>.Enumerator enumerator = targetBones.GetEnumerator();
			if (enumerator.MoveNext())
			{
				value = enumerator.Current.Value;
			}
		}
		for (int i = 0; i < ((Il2CppArrayBase<Transform>)bones).Length; i++)
		{
			Transform transform = ((Il2CppArrayBase<Transform>)bones)[i];
			if (!(transform == null) && !targetBones.ContainsKey(transform.name))
			{
				GameObject gameObject = new GameObject(transform.name);
				Transform parent = value;
				if (transform.parent != null && targetBones.TryGetValue(transform.parent.name, out var value2))
				{
					parent = value2;
				}
				gameObject.transform.SetParent(parent, worldPositionStays: false);
				gameObject.transform.localPosition = transform.localPosition;
				gameObject.transform.localRotation = transform.localRotation;
				gameObject.transform.localScale = transform.localScale;
				targetBones[transform.name] = gameObject.transform;
			}
		}
	}

	public static void ApplyViewModel(BaseViewModel vm)
	{
		if (vm == null || !ModConfig.GlassAkEnabled.Value)
		{
			return;
		}
		Animator animator = vm.animator ?? vm.GetComponentInChildren<Animator>();
		if (animator != null)
		{
			RuntimeAnimatorController asset = AssetFactory.GetAsset<RuntimeAnimatorController>("glass_ak_viewmodel");
			if (asset != null)
			{
				animator.runtimeAnimatorController = UnityEngine.Object.Instantiate(asset);
			}
			Avatar asset2 = AssetFactory.GetAsset<Avatar>("v_ar_glass_rigAvatar");
			if (asset2 != null)
			{
				animator.avatar = asset2;
			}
			animator.SetFloat(Animator.StringToHash("ironsightsEnabled"), 0f);
			animator.SetFloat(Animator.StringToHash("ironsightstrength"), 1f);
			if (animator.HasState(0, Animator.StringToHash("deploy")))
			{
				animator.Play("deploy", 0, 0f);
			}
		}
		Transform transform = vm.transform.Find("v_ak47u") ?? vm.transform;
		GameObject gameObject = AssetFactory.GetAsset<GameObject>("v_ar_glass_rig") ?? AssetFactory.GetAsset<GameObject>("v_ar_glass");
		if (gameObject == null)
		{
			return;
		}
		Dictionary<string, Transform> dictionary = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);
		Il2CppArrayBase<Transform> componentsInChildren = transform.GetComponentsInChildren<Transform>(includeInactive: true);
		if (componentsInChildren != null)
		{
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Transform transform2 = componentsInChildren[i];
				if (transform2 != null && !dictionary.ContainsKey(transform2.name))
				{
					dictionary[transform2.name] = transform2;
				}
			}
		}
		SkinnedMeshRenderer skinnedMeshRenderer = null;
		SkinnedMeshRenderer skinnedMeshRenderer2 = null;
		Il2CppArrayBase<SkinnedMeshRenderer> componentsInChildren2 = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
		if (componentsInChildren2 != null)
		{
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				SkinnedMeshRenderer skinnedMeshRenderer3 = componentsInChildren2[j];
				if (!(skinnedMeshRenderer3 == null))
				{
					if (skinnedMeshRenderer3.name.IndexOf("body", StringComparison.OrdinalIgnoreCase) >= 0 || skinnedMeshRenderer3.name == "v_ar_glass")
					{
						skinnedMeshRenderer = skinnedMeshRenderer3;
					}
					else if (skinnedMeshRenderer3.name.IndexOf("mag", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						skinnedMeshRenderer2 = skinnedMeshRenderer3;
					}
				}
			}
		}
		if (skinnedMeshRenderer == null)
		{
			return;
		}
		EnsureBonesExist(skinnedMeshRenderer, dictionary);
		EnsureBonesExist(skinnedMeshRenderer2, dictionary);
		Il2CppArrayBase<Transform> componentsInChildren3 = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);
		if (componentsInChildren3 != null)
		{
			for (int k = 0; k < componentsInChildren3.Length; k++)
			{
				Transform transform3 = componentsInChildren3[k];
				if (!(transform3 == null) && dictionary.TryGetValue(transform3.name, out var value) && value != null && value != transform3)
				{
					value.localPosition = transform3.localPosition;
					value.localRotation = transform3.localRotation;
					value.localScale = transform3.localScale;
				}
			}
		}
		Shader configuredShader = GetConfiguredShader();
		Material glassMaterial = GetGlassMaterial();
		Il2CppArrayBase<Renderer> componentsInChildren4 = vm.GetComponentsInChildren<Renderer>(includeInactive: true);
		if (componentsInChildren4 == null)
		{
			return;
		}
		for (int l = 0; l < componentsInChildren4.Length; l++)
		{
			Renderer renderer = componentsInChildren4[l];
			if (renderer == null || !renderer.name.StartsWith("ak47"))
			{
				continue;
			}
			SkinnedMeshRenderer component = renderer.GetComponent<SkinnedMeshRenderer>();
			string text = renderer.name.ToLower();
			if (text.Contains("body"))
			{
				if (!(component != null))
				{
					continue;
				}
				component.sharedMesh = skinnedMeshRenderer.sharedMesh;
				component.updateWhenOffscreen = true;
				Il2CppReferenceArray<Transform> bones = skinnedMeshRenderer.bones;
				Transform[] array = new Transform[((Il2CppArrayBase<Transform>)bones).Length];
				for (int m = 0; m < ((Il2CppArrayBase<Transform>)bones).Length; m++)
				{
					string text2 = ((((Il2CppArrayBase<Transform>)bones)[m] != null) ? ((Il2CppArrayBase<Transform>)bones)[m].name : "");
					if (!string.IsNullOrEmpty(text2) && dictionary.TryGetValue(text2, out var value2))
					{
						array[m] = value2;
					}
					else
					{
						dictionary.TryGetValue("joint8", out array[m]);
					}
				}
				component.bones = array;
				string key = ((skinnedMeshRenderer.rootBone != null) ? skinnedMeshRenderer.rootBone.name : "joint8");
				if (dictionary.TryGetValue(key, out var value3))
				{
					component.rootBone = value3;
				}
				Il2CppReferenceArray<Material> sharedMaterials = skinnedMeshRenderer.sharedMaterials;
				Material[] array2 = new Material[((Il2CppArrayBase<Material>)sharedMaterials).Length];
				for (int n = 0; n < array2.Length; n++)
				{
					Material material = ((Il2CppArrayBase<Material>)sharedMaterials)[n];
					if (material != null && material.name.Contains("ammo"))
					{
						Material material2 = new Material(material);
						if (configuredShader != null)
						{
							material2.shader = configuredShader;
						}
						array2[n] = material2;
					}
					else if (material != null && (material.name.Contains("transp") || n == 1))
					{
						array2[n] = glassMaterial;
					}
					else if (material != null)
					{
						Material material3 = new Material(material);
						if (configuredShader != null)
						{
							material3.shader = configuredShader;
						}
						array2[n] = material3;
					}
				}
				renderer.materials = array2;
				renderer.enabled = true;
				SetupViewModelRenderer(component, enableEvents: true);
			}
			else if (text.Contains("mag"))
			{
				if (!(skinnedMeshRenderer2 != null) || !(component != null))
				{
					continue;
				}
				component.sharedMesh = skinnedMeshRenderer2.sharedMesh;
				component.updateWhenOffscreen = true;
				Il2CppReferenceArray<Transform> bones2 = skinnedMeshRenderer2.bones;
				Transform[] array3 = new Transform[((Il2CppArrayBase<Transform>)bones2).Length];
				for (int num = 0; num < ((Il2CppArrayBase<Transform>)bones2).Length; num++)
				{
					string text3 = ((((Il2CppArrayBase<Transform>)bones2)[num] != null) ? ((Il2CppArrayBase<Transform>)bones2)[num].name : "");
					if (!string.IsNullOrEmpty(text3) && dictionary.TryGetValue(text3, out var value4))
					{
						array3[num] = value4;
					}
					else
					{
						dictionary.TryGetValue("magazine", out array3[num]);
					}
				}
				component.bones = array3;
				string key2 = ((skinnedMeshRenderer2.rootBone != null) ? skinnedMeshRenderer2.rootBone.name : "magazine");
				if (dictionary.TryGetValue(key2, out var value5))
				{
					component.rootBone = value5;
				}
				Il2CppReferenceArray<Material> sharedMaterials2 = skinnedMeshRenderer2.sharedMaterials;
				Material[] array4 = new Material[((Il2CppArrayBase<Material>)sharedMaterials2).Length];
				for (int num2 = 0; num2 < array4.Length; num2++)
				{
					Material material4 = ((Il2CppArrayBase<Material>)sharedMaterials2)[num2];
					if (material4 != null && material4.name.Contains("ammo"))
					{
						Material material5 = new Material(material4);
						if (configuredShader != null)
						{
							material5.shader = configuredShader;
						}
						array4[num2] = material5;
					}
					else if (material4 != null && (material4.name.Contains("transp") || num2 == 1))
					{
						array4[num2] = glassMaterial;
					}
					else if (material4 != null)
					{
						Material material6 = new Material(material4);
						if (configuredShader != null)
						{
							material6.shader = configuredShader;
						}
						array4[num2] = material6;
					}
				}
				renderer.materials = array4;
				renderer.enabled = true;
				SetupViewModelRenderer(component, enableEvents: true);
			}
			else if (text.Contains("barrel") || text.Contains("stock") || text.Contains("handle"))
			{
				renderer.enabled = false;
				SetupViewModelRenderer(renderer, enableEvents: false);
			}
		}
	}

	private static void RememberGlassMat(Material mat)
	{
		if (!(mat == null) && !_liveGlassMats.Contains(mat))
		{
			_liveGlassMats.Add(mat);
		}
	}

	internal static void TickRgb()
	{
		try
		{
			if (ModConfig.GlassAkEnabled == null || !ModConfig.GlassAkEnabled.Value || !string.Equals(ModConfig.CurrentColorKey, "rgb", StringComparison.OrdinalIgnoreCase) || _liveGlassMats.Count == 0)
			{
				return;
			}
			Color color = Color.HSVToRGB(UnityEngine.Time.unscaledTime * 0.15f % 1f, 0.85f, 1f);
			_curR = color.r;
			_curG = color.g;
			_curB = color.b;
			Color color2 = new Color(Mathf.Clamp01(_curR * _curBoost), Mathf.Clamp01(_curG * _curBoost), Mathf.Clamp01(_curB * _curBoost), _curA);
			Color value = Color.Lerp(Color.white * _curSpecMul, color2 * _curSpecMul, _curSpecTint);
			value.a = _curGloss;
			Color value2 = new Color(_curR * _curEm, _curG * _curEm, _curB * _curEm, 1f);
			Color value3 = new Color(_curR * _curTr, _curG * _curTr, _curB * _curTr, 1f);
			for (int num = _liveGlassMats.Count - 1; num >= 0; num--)
			{
				Material material = _liveGlassMats[num];
				if (material == null)
				{
					_liveGlassMats.RemoveAt(num);
				}
				else
				{
					if (material.HasProperty("_Color"))
					{
						material.SetColor("_Color", color2);
					}
					if (material.HasProperty("_BaseColor"))
					{
						material.SetColor("_BaseColor", color2);
					}
					if (material.HasProperty("_SpecColor"))
					{
						material.SetColor("_SpecColor", value);
					}
					if (material.HasProperty("_EmissionColor"))
					{
						material.SetColor("_EmissionColor", value2);
					}
					if (material.HasProperty("_TransmissionColor"))
					{
						material.SetColor("_TransmissionColor", value3);
					}
				}
			}
		}
		catch
		{
		}
	}
}
