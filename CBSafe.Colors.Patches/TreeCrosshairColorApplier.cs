using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Colors.Infrastructure;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CBSafe.Colors.Patches;

[HydraNoObfuscate]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class TreeCrosshairColorApplier : MonoBehaviour
{
	private struct MarkerTargets
	{
		public Renderer[] renderers;

		public Graphic[] graphics;

		public ParticleSystem[] particles;

		public Projector[] projectors;

		public DeferredDecal[] decals;
	}

	private struct ParticleOrig
	{
		public ParticleSystem ps;

		public Color color;
	}

	private struct ProjectorOrig
	{
		public Projector proj;

		public Color color;
	}

	private struct DecalColorOrig
	{
		public DeferredDecal decal;

		public string property;

		public Color color;
	}

	private struct DecalTexOrig
	{
		public DeferredDecal decal;

		public string property;

		public Texture original;
	}

	private struct GraphicOrig
	{
		public Graphic g;

		public Color color;
	}

	private struct SpriteOrig
	{
		public SpriteRenderer s;

		public Color color;
	}

	private struct RendererOrig
	{
		public Renderer r;

		public Color color;

		public string property;
	}

	private struct RendererTexOrig
	{
		public Renderer r;

		public Texture original;

		public string property;
	}

	private struct RendererShaderOrig
	{
		public Material mat;

		public Shader original;
	}

	private struct DeferredDump
	{
		public GameObject go;

		public float runAt;

		public Color color;
	}

	private struct PendingMarkerPaint
	{
		public GameObject Go;

		public Color Color;
	}

	private struct PendingDecalTextureWhitening
	{
		public Material Mat;

		public string Prop;

		public Texture Orig;
	}

	private static readonly string[] PrefabTokens = new string[9] { "treemarker", "tree_marker", "tree_marking", "treemarking", "tree marker", "tree marking", "tree_indicator", "tree_hit", "tree-marker" };

	private static readonly string[] ColorPropertyNames = new string[9] { "_Color", "_BaseColor", "_MainColor", "_TintColor", "_EmissionColor", "_CoreColor", "_DecalColor", "_OverlayColor", "_FrontColor" };

	private static readonly string[] TexturePropertyNames = new string[10] { "_MainTex", "_BaseMap", "_BaseColorMap", "_MainTexture", "_AlbedoMap", "_AlbedoTex", "_Diffuse", "_DiffuseMap", "_Texture", "_Tex" };

	private static readonly Dictionary<int, GameObject> MarkerCache = new Dictionary<int, GameObject>();

	private static readonly Dictionary<int, MarkerTargets> MarkerTargetsCache = new Dictionary<int, MarkerTargets>();

	private static readonly Dictionary<int, ParticleOrig> _originalParticles = new Dictionary<int, ParticleOrig>();

	private static readonly Dictionary<int, ProjectorOrig> _originalProjectors = new Dictionary<int, ProjectorOrig>();

	private static readonly List<DecalColorOrig> _originalDecalColors = new List<DecalColorOrig>();

	private static readonly HashSet<long> _decalColorCachedKeys = new HashSet<long>();

	private static readonly List<DecalTexOrig> _originalDecalTextures = new List<DecalTexOrig>();

	private static readonly HashSet<long> _decalTexCachedKeys = new HashSet<long>();

	private static readonly Dictionary<int, Texture2D> _whitenedTextureCache = new Dictionary<int, Texture2D>();

	private static readonly HashSet<int> _whitenedTextureIds = new HashSet<int>();

	private static readonly Dictionary<int, GraphicOrig> _originalGraphics = new Dictionary<int, GraphicOrig>();

	private static readonly Dictionary<int, SpriteOrig> _originalSprites = new Dictionary<int, SpriteOrig>();

	private static readonly List<RendererOrig> _originalRenderers = new List<RendererOrig>();

	private static readonly HashSet<long> _rendererOrigCachedKeys = new HashSet<long>();

	private static readonly List<RendererTexOrig> _originalRendererTextures = new List<RendererTexOrig>();

	private static readonly HashSet<long> _rendererTexOrigCachedKeys = new HashSet<long>();

	private static readonly HashSet<string> _reusableDynNames = new HashSet<string>();

	private static readonly HashSet<string> _reusableTexNames = new HashSet<string>();

	private static Shader _standardShader;

	private static readonly List<RendererShaderOrig> _originalRendererShaders = new List<RendererShaderOrig>();

	private static Texture2D _whiteTexture1x1;

	private static MaterialPropertyBlock _mpb;

	private static MaterialPropertyBlock _emptyMpb;

	private float _activeWindowEndTime;

	private const float ActiveWindowDuration = 2.5f;

	private const float SessionBoostDuration = 10f;

	private const float IdleReapplyInterval = 5f;

	private static float _nextScanTime;

	private static float _nextFullScanTime;

	private static float _nextApplyTime;

	private Color _lastAppliedColor;

	private bool _hasLastApplied;

	private int _lastScanTyped;

	private int _lastScanFallback;

	private int _lastScanRenderer;

	private static readonly HashSet<int> _treeMarkersDumped = new HashSet<int>();

	private static readonly List<DeferredDump> _deferredDumps = new List<DeferredDump>();

	private const float DeferredDumpDelay = 1f;

	private static readonly Queue<PendingMarkerPaint> _pendingMarkerPaints = new Queue<PendingMarkerPaint>();

	private const int MaxMarkerPaintsPerFrame = 1;

	private const int MaxWhitenedTextures = 12;

	private static readonly Queue<PendingDecalTextureWhitening> _pendingDecalTextures = new Queue<PendingDecalTextureWhitening>();

	private const int MaxDecalTexWhitensPerFrame = 1;

	private static int _lastSceneHandle;

	private static bool _sceneTrackInitialised;

	private static readonly List<int> _applyCacheScratch = new List<int>(8);

	private static readonly Dictionary<int, string[]> _shaderColorPropsCache = new Dictionary<int, string[]>();

	private static readonly Dictionary<int, string[]> _shaderTexturePropsCache = new Dictionary<int, string[]>();

	private static readonly string[] _emptyStringArray = Array.Empty<string>();

	public TreeCrosshairColorApplier(IntPtr ptr)
		: base(ptr)
	{
	}

	internal static bool IsSessionColorStable()
	{
		if (!PluginConfig.TryResolveTreeCrosshair(out var _))
		{
			return true;
		}
		return MarkerCache.Count > 0;
	}

	private static Shader GetStandardShader()
	{
		if (_standardShader != null)
		{
			return _standardShader;
		}
		try
		{
			_standardShader = Shader.Find("Standard");
		}
		catch
		{
			_standardShader = null;
		}
		return _standardShader;
	}

	private static Texture2D GetWhiteTexture()
	{
		if (_whiteTexture1x1 == null)
		{
			try
			{
				Texture2D whiteTexture = Texture2D.whiteTexture;
				if (whiteTexture != null)
				{
					_whiteTexture1x1 = whiteTexture;
				}
			}
			catch
			{
			}
		}
		if (_whiteTexture1x1 == null)
		{
			try
			{
				_whiteTexture1x1 = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
				_whiteTexture1x1.SetPixel(0, 0, Color.white);
				_whiteTexture1x1.Apply(updateMipmaps: false, makeNoLongerReadable: true);
				UnityEngine.Object.DontDestroyOnLoad(_whiteTexture1x1);
			}
			catch
			{
			}
		}
		return _whiteTexture1x1;
	}

	private static void ScheduleDeferredDump(GameObject go, Color color)
	{
		if (go == null)
		{
			return;
		}
		try
		{
			_deferredDumps.Add(new DeferredDump
			{
				go = go,
				runAt = Time.unscaledTime + 1f,
				color = color
			});
		}
		catch
		{
		}
	}

	private static void DrainDeferredDumps()
	{
		if (_deferredDumps.Count == 0)
		{
			return;
		}
		float num = 0f;
		try
		{
			num = Time.unscaledTime;
		}
		catch
		{
			return;
		}
		for (int num2 = _deferredDumps.Count - 1; num2 >= 0; num2--)
		{
			DeferredDump deferredDump = _deferredDumps[num2];
			if (!(num < deferredDump.runAt))
			{
				_deferredDumps.RemoveAt(num2);
				if (!(deferredDump.go == null))
				{
					try
					{
						try
						{
							int instanceID = deferredDump.go.GetInstanceID();
							MarkerTargetsCache.Remove(instanceID);
							ApplyToGameObject(deferredDump.go, deferredDump.color);
						}
						catch
						{
						}
					}
					catch
					{
					}
				}
			}
		}
	}

	private void LateUpdate()
	{
		try
		{
			int handle = SceneManager.GetActiveScene().handle;
			if (!_sceneTrackInitialised)
			{
				_sceneTrackInitialised = true;
				_lastSceneHandle = handle;
			}
			else if (handle != _lastSceneHandle)
			{
				_lastSceneHandle = handle;
				OnSceneChanged();
			}
			if (SceneManager.GetActiveScene().name != null && SceneManager.GetActiveScene().name.Contains("Menu"))
			{
				return;
			}
			if (!PluginConfig.TryResolveTreeCrosshair(out var color))
			{
				_hasLastApplied = false;
				return;
			}
			bool flag = !_hasLastApplied || !ColorsApproximatelyEqual(_lastAppliedColor, color);
			if (flag)
			{
				_lastAppliedColor = color;
				_hasLastApplied = true;
				_activeWindowEndTime = Time.unscaledTime + 2.5f;
			}
			bool flag2 = Time.unscaledTime <= _activeWindowEndTime;
			float num = (flag2 ? 0.25f : ((MarkerCache.Count == 0) ? 20f : 12f));
			if (Time.unscaledTime >= _nextScanTime)
			{
				ScanForTreeMarkers();
				_nextScanTime = Time.unscaledTime + num;
			}
			if (MarkerCache.Count > 0 && (flag || flag2 || Time.unscaledTime >= _nextApplyTime))
			{
				ApplyCacheNow(color);
				_nextApplyTime = Time.unscaledTime + (flag2 ? 0.2f : 5f);
			}
			try
			{
				DrainDeferredDumps();
			}
			catch
			{
			}
			try
			{
				DrainPendingMarkerPaints();
			}
			catch
			{
			}
			try
			{
				DrainPendingDecalTextures();
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private static void DrainPendingMarkerPaints()
	{
		if (_pendingMarkerPaints.Count == 0)
		{
			return;
		}
		int num = 0;
		while (_pendingMarkerPaints.Count > 0 && num < 1)
		{
			PendingMarkerPaint pendingMarkerPaint = _pendingMarkerPaints.Dequeue();
			if (!(pendingMarkerPaint.Go == null))
			{
				try
				{
					ApplyToGameObject(pendingMarkerPaint.Go, pendingMarkerPaint.Color);
				}
				catch
				{
				}
				num++;
			}
		}
	}

	private static void DrainPendingDecalTextures()
	{
		if (_pendingDecalTextures.Count == 0)
		{
			return;
		}
		int num = 0;
		while (_pendingDecalTextures.Count > 0 && num < 1)
		{
			PendingDecalTextureWhitening pendingDecalTextureWhitening = _pendingDecalTextures.Dequeue();
			if (pendingDecalTextureWhitening.Mat == null || pendingDecalTextureWhitening.Orig == null)
			{
				continue;
			}
			Texture2D whitenedTexture = GetWhitenedTexture(pendingDecalTextureWhitening.Orig);
			if (whitenedTexture == null)
			{
				continue;
			}
			try
			{
				if (!string.IsNullOrEmpty(pendingDecalTextureWhitening.Prop) && pendingDecalTextureWhitening.Mat.HasProperty(pendingDecalTextureWhitening.Prop))
				{
					pendingDecalTextureWhitening.Mat.SetTexture(pendingDecalTextureWhitening.Prop, whitenedTexture);
				}
			}
			catch
			{
			}
			num++;
		}
	}

	private void OnSceneChanged()
	{
		try
		{
			MarkerCache.Clear();
			MarkerTargetsCache.Clear();
			_pendingMarkerPaints.Clear();
			_hasLastApplied = false;
			_nextScanTime = 0f;
			_activeWindowEndTime = Time.unscaledTime + 10f;
			ForceReapplyNow();
		}
		catch
		{
		}
	}

	public void QuickApplyFromConfig()
	{
		try
		{
			if (!PluginConfig.TryResolveTreeCrosshair(out var color))
			{
				_hasLastApplied = false;
				return;
			}
			_activeWindowEndTime = Time.unscaledTime + 2.5f;
			_lastAppliedColor = color;
			_hasLastApplied = true;
			_nextApplyTime = 0f;
			if (MarkerCache.Count != 0)
			{
				ApplyCacheNow(color);
			}
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(50, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TreeCrosshairColorApplier] QuickApplyFromConfig: ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}

	public void ForceReapplyNow(Color? colorOverride = null)
	{
		try
		{
			Color color;
			if (colorOverride.HasValue)
			{
				color = PluginConfig.DarkenTreeCrosshairColor(colorOverride.Value);
			}
			else if (!PluginConfig.TryResolveTreeCrosshair(out color))
			{
				return;
			}
			_activeWindowEndTime = Time.unscaledTime + 2.5f;
			if (MarkerCache.Count == 0)
			{
				_nextScanTime = 0f;
				if (Time.unscaledTime >= _nextFullScanTime)
				{
					ScanForTreeMarkers();
					_nextFullScanTime = Time.unscaledTime + 30f;
				}
			}
			_nextScanTime = Time.unscaledTime + 0.25f;
			ApplyCacheNow(color);
			_nextApplyTime = 0f;
			_lastAppliedColor = color;
			_hasLastApplied = true;
		}
		catch
		{
		}
	}

	internal static void OnTreeMarkerClientInit(TreeMarker tm)
	{
		try
		{
			if (tm == null)
			{
				return;
			}
			GameObject gameObject = tm.gameObject;
			if (gameObject == null || !IsRuntimeSceneObject(gameObject))
			{
				return;
			}
			int instanceID = gameObject.GetInstanceID();
			if (!MarkerCache.ContainsKey(instanceID))
			{
				MarkerCache[instanceID] = gameObject;
			}
			if (PluginConfig.TryResolveTreeCrosshair(out var color))
			{
				bool num = _treeMarkersDumped.Add(instanceID);
				MarkerTargetsCache.Remove(instanceID);
				ApplyToGameObject(gameObject, color);
				if (num)
				{
					ScheduleDeferredDump(gameObject, color);
				}
			}
		}
		catch
		{
		}
	}

	private static bool IsRuntimeSceneObject(GameObject go)
	{
		if (go == null)
		{
			return false;
		}
		try
		{
			if (!go.scene.IsValid())
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		return true;
	}

	private static string GetTransformPath(Transform t)
	{
		if (t == null)
		{
			return "<null>";
		}
		try
		{
			string text = t.name;
			Transform parent = t.parent;
			int num = 16;
			while (parent != null && num-- > 0)
			{
				text = parent.name + "/" + text;
				parent = parent.parent;
			}
			return text;
		}
		catch
		{
			return t.name;
		}
	}

	private static string ColorToStr(Color c)
	{
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 4);
		defaultInterpolatedStringHandler.AppendLiteral("(");
		defaultInterpolatedStringHandler.AppendFormatted(c.r, "F2");
		defaultInterpolatedStringHandler.AppendLiteral(",");
		defaultInterpolatedStringHandler.AppendFormatted(c.g, "F2");
		defaultInterpolatedStringHandler.AppendLiteral(",");
		defaultInterpolatedStringHandler.AppendFormatted(c.b, "F2");
		defaultInterpolatedStringHandler.AppendLiteral(",");
		defaultInterpolatedStringHandler.AppendFormatted(c.a, "F2");
		defaultInterpolatedStringHandler.AppendLiteral(")");
		return defaultInterpolatedStringHandler.ToStringAndClear();
	}

	private static void ApplyCacheNow(Color target)
	{
		if (MarkerCache.Count == 0)
		{
			return;
		}
		_applyCacheScratch.Clear();
		foreach (KeyValuePair<int, GameObject> item in MarkerCache)
		{
			GameObject value = item.Value;
			if (value == null)
			{
				_applyCacheScratch.Add(item.Key);
			}
			else
			{
				ApplyToGameObject(value, target);
			}
		}
		for (int i = 0; i < _applyCacheScratch.Count; i++)
		{
			int key = _applyCacheScratch[i];
			MarkerCache.Remove(key);
			MarkerTargetsCache.Remove(key);
		}
	}

	private static bool ColorsApproximatelyEqual(Color a, Color b)
	{
		if (Mathf.Abs(a.r - b.r) < 0.001f && Mathf.Abs(a.g - b.g) < 0.001f && Mathf.Abs(a.b - b.b) < 0.001f)
		{
			return Mathf.Abs(a.a - b.a) < 0.001f;
		}
		return false;
	}

	private void ScanForTreeMarkers()
	{
		int num = 0;
		try
		{
			Il2CppArrayBase<TreeMarker> il2CppArrayBase = UnityEngine.Object.FindObjectsOfType<TreeMarker>();
			int num2 = il2CppArrayBase?.Length ?? 0;
			for (int i = 0; i < num2; i++)
			{
				TreeMarker treeMarker = il2CppArrayBase[i];
				if (treeMarker == null)
				{
					continue;
				}
				GameObject gameObject = treeMarker.gameObject;
				if (!(gameObject == null) && IsRuntimeSceneObject(gameObject))
				{
					int instanceID = gameObject.GetInstanceID();
					if (!MarkerCache.ContainsKey(instanceID))
					{
						MarkerCache[instanceID] = gameObject;
						num++;
					}
				}
			}
		}
		catch
		{
		}
		_lastScanTyped = 0;
		_lastScanFallback = num;
		_lastScanRenderer = 0;
	}

	private static void WalkSceneForMarkers(Transform t, Color targetForDiag, ref int found)
	{
		if (t == null)
		{
			return;
		}
		try
		{
			string text = t.name;
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = text.ToLower();
				bool flag = false;
				for (int i = 0; i < PrefabTokens.Length; i++)
				{
					if (text2.Contains(PrefabTokens[i]))
					{
						flag = true;
						break;
					}
				}
				if (flag && (text2.Contains("_spawn") || text2.Contains("smokepuff") || text2.Contains("tire_smoke") || text2.Contains("smoke_puff")))
				{
					flag = false;
				}
				if (flag)
				{
					GameObject gameObject = t.gameObject;
					if (gameObject != null && IsRuntimeSceneObject(gameObject))
					{
						int instanceID = gameObject.GetInstanceID();
						if (!MarkerCache.ContainsKey(instanceID))
						{
							MarkerCache[instanceID] = gameObject;
							found++;
						}
						_treeMarkersDumped.Add(instanceID);
					}
				}
			}
		}
		catch
		{
		}
		int num = 0;
		try
		{
			num = t.childCount;
		}
		catch
		{
		}
		for (int j = 0; j < num; j++)
		{
			Transform transform = null;
			try
			{
				transform = t.GetChild(j);
			}
			catch
			{
			}
			if (!(transform == null))
			{
				WalkSceneForMarkers(transform, targetForDiag, ref found);
			}
		}
	}

	private static string TryGetSceneName(GameObject go)
	{
		if (go == null)
		{
			return "(null)";
		}
		try
		{
			Scene scene = go.scene;
			return scene.IsValid() ? scene.name : "(invalid)";
		}
		catch
		{
			return "(err)";
		}
	}

	private static void ApplyToGameObject(GameObject root, Color color)
	{
		if (root == null)
		{
			return;
		}
		try
		{
			int instanceID = root.GetInstanceID();
			if (!MarkerTargetsCache.TryGetValue(instanceID, out var value))
			{
				Il2CppArrayBase<Renderer> componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
				Il2CppArrayBase<Graphic> componentsInChildren2 = root.GetComponentsInChildren<Graphic>(includeInactive: true);
				Il2CppArrayBase<ParticleSystem> componentsInChildren3 = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
				Il2CppArrayBase<Projector> componentsInChildren4 = root.GetComponentsInChildren<Projector>(includeInactive: true);
				Il2CppArrayBase<DeferredDecal> componentsInChildren5 = root.GetComponentsInChildren<DeferredDecal>(includeInactive: true);
				int num = componentsInChildren?.Length ?? 0;
				int num2 = componentsInChildren2?.Length ?? 0;
				int num3 = componentsInChildren3?.Length ?? 0;
				int num4 = componentsInChildren4?.Length ?? 0;
				int num5 = componentsInChildren5?.Length ?? 0;
				value.renderers = new Renderer[num];
				value.graphics = new Graphic[num2];
				value.particles = new ParticleSystem[num3];
				value.projectors = new Projector[num4];
				value.decals = new DeferredDecal[num5];
				for (int i = 0; i < num; i++)
				{
					value.renderers[i] = componentsInChildren[i];
				}
				for (int j = 0; j < num2; j++)
				{
					value.graphics[j] = componentsInChildren2[j];
				}
				for (int k = 0; k < num3; k++)
				{
					value.particles[k] = componentsInChildren3[k];
				}
				for (int l = 0; l < num4; l++)
				{
					value.projectors[l] = componentsInChildren4[l];
				}
				for (int m = 0; m < num5; m++)
				{
					value.decals[m] = componentsInChildren5[m];
				}
				MarkerTargetsCache[instanceID] = value;
			}
			int num6 = ((value.renderers != null) ? value.renderers.Length : 0);
			for (int n = 0; n < num6; n++)
			{
				Renderer renderer = value.renderers[n];
				if (!(renderer == null))
				{
					ApplyRendererColor(renderer, color);
				}
			}
			int num7 = ((value.graphics != null) ? value.graphics.Length : 0);
			for (int num8 = 0; num8 < num7; num8++)
			{
				Graphic graphic = value.graphics[num8];
				if (!(graphic == null))
				{
					ApplyToGraphic(graphic, color);
				}
			}
			int num9 = ((value.particles != null) ? value.particles.Length : 0);
			for (int num10 = 0; num10 < num9; num10++)
			{
				ParticleSystem particleSystem = value.particles[num10];
				if (!(particleSystem == null))
				{
					ApplyToParticleSystem(particleSystem, color);
				}
			}
			int num11 = ((value.projectors != null) ? value.projectors.Length : 0);
			for (int num12 = 0; num12 < num11; num12++)
			{
				Projector projector = value.projectors[num12];
				if (!(projector == null))
				{
					ApplyToProjector(projector, color);
				}
			}
			int num13 = ((value.decals != null) ? value.decals.Length : 0);
			for (int num14 = 0; num14 < num13; num14++)
			{
				DeferredDecal deferredDecal = value.decals[num14];
				if (!(deferredDecal == null))
				{
					ApplyToDeferredDecal(deferredDecal, color);
				}
			}
		}
		catch
		{
		}
	}

	private static void ApplyToDeferredDecal(DeferredDecal dd, Color color)
	{
		if (dd == null)
		{
			return;
		}
		Material material = null;
		try
		{
			material = dd.material;
		}
		catch
		{
			material = null;
		}
		if (material == null)
		{
			return;
		}
		int instanceID = dd.GetInstanceID();
		string[] array = GetShaderColorPropertyNames(material);
		if (array == null || array.Length == 0)
		{
			array = ColorPropertyNames;
		}
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			long item = ((long)instanceID << 32) ^ (uint)text.GetHashCode();
			if (_decalColorCachedKeys.Add(item))
			{
				Color color2 = Color.white;
				try
				{
					if (material.HasProperty(text))
					{
						color2 = material.GetColor(text);
					}
				}
				catch
				{
				}
				_originalDecalColors.Add(new DecalColorOrig
				{
					decal = dd,
					property = text,
					color = color2
				});
			}
			try
			{
				if (material.HasProperty(text))
				{
					material.SetColor(text, color);
				}
			}
			catch
			{
			}
		}
		try
		{
			material.color = color;
		}
		catch
		{
		}
		string[] shaderTexturePropertyNames = GetShaderTexturePropertyNames(material);
		int num = 0;
		if (shaderTexturePropertyNames == null || shaderTexturePropertyNames.Length == 0)
		{
			return;
		}
		array2 = shaderTexturePropertyNames;
		foreach (string text2 in array2)
		{
			if (string.IsNullOrEmpty(text2))
			{
				continue;
			}
			bool flag = false;
			try
			{
				flag = material.HasProperty(text2);
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				continue;
			}
			Texture texture = null;
			try
			{
				texture = material.GetTexture(text2);
			}
			catch
			{
				texture = null;
			}
			if (texture == null)
			{
				continue;
			}
			long item2 = ((long)instanceID << 32) ^ (uint)("TEX_" + text2).GetHashCode();
			if (_decalTexCachedKeys.Add(item2))
			{
				_originalDecalTextures.Add(new DecalTexOrig
				{
					decal = dd,
					property = text2,
					original = texture
				});
			}
			int instanceID2 = texture.GetInstanceID();
			if (_whitenedTextureIds.Contains(instanceID2))
			{
				continue;
			}
			Texture2D whitenedTexture = GetWhitenedTexture(texture);
			if (!(whitenedTexture == null))
			{
				try
				{
					material.SetTexture(text2, whitenedTexture);
					num++;
				}
				catch
				{
				}
			}
		}
	}

	private static Texture2D GetWhitenedTexture(Texture src)
	{
		if (src == null)
		{
			return null;
		}
		int instanceID = src.GetInstanceID();
		if (_whitenedTextureCache.TryGetValue(instanceID, out var value) && value != null)
		{
			return value;
		}
		int num = 0;
		int num2 = 0;
		try
		{
			num = src.width;
			num2 = src.height;
		}
		catch
		{
			num = 0;
			num2 = 0;
		}
		if (num <= 0 || num2 <= 0)
		{
			return null;
		}
		if (num > 256 || num2 > 256)
		{
			float num3 = 256f / (float)Mathf.Max(num, num2);
			num = Mathf.Max(1, Mathf.RoundToInt((float)num * num3));
			num2 = Mathf.Max(1, Mathf.RoundToInt((float)num2 * num3));
		}
		RenderTexture active = null;
		RenderTexture renderTexture = null;
		Texture2D texture2D = null;
		try
		{
			active = RenderTexture.active;
			renderTexture = RenderTexture.GetTemporary(num, num2, 0, RenderTextureFormat.ARGB32);
			Graphics.Blit(src, renderTexture);
			RenderTexture.active = renderTexture;
			texture2D = new Texture2D(num, num2, TextureFormat.RGBA32, mipChain: false);
			texture2D.ReadPixels(new Rect(0f, 0f, num, num2), 0, 0, recalculateMipMaps: false);
			Il2CppStructArray<Color32> pixels = texture2D.GetPixels32();
			int length = ((Il2CppArrayBase<Color32>)pixels).Length;
			for (int i = 0; i < length; i++)
			{
				Color32 value2 = ((Il2CppArrayBase<Color32>)pixels)[i];
				value2.r = byte.MaxValue;
				value2.g = byte.MaxValue;
				value2.b = byte.MaxValue;
				((Il2CppArrayBase<Color32>)pixels)[i] = value2;
			}
			texture2D.SetPixels32(pixels);
			texture2D.Apply(updateMipmaps: false, makeNoLongerReadable: false);
			UnityEngine.Object.DontDestroyOnLoad(texture2D);
			TrimWhitenedTextureCacheIfNeeded();
			_whitenedTextureCache[instanceID] = texture2D;
			_whitenedTextureIds.Add(texture2D.GetInstanceID());
			return texture2D;
		}
		catch
		{
			if (texture2D != null)
			{
				try
				{
					UnityEngine.Object.Destroy(texture2D);
				}
				catch
				{
				}
			}
			return null;
		}
		finally
		{
			try
			{
				RenderTexture.active = active;
			}
			catch
			{
			}
			if (renderTexture != null)
			{
				try
				{
					RenderTexture.ReleaseTemporary(renderTexture);
				}
				catch
				{
				}
			}
		}
	}

	private static void TrimWhitenedTextureCacheIfNeeded()
	{
		if (_whitenedTextureCache.Count < 12)
		{
			return;
		}
		int num = _whitenedTextureCache.Count - 12 + 1;
		foreach (int item in new List<int>(_whitenedTextureCache.Keys))
		{
			if (num <= 0)
			{
				break;
			}
			if (_whitenedTextureCache.TryGetValue(item, out var value) && value != null)
			{
				try
				{
					_whitenedTextureIds.Remove(value.GetInstanceID());
					UnityEngine.Object.Destroy(value);
				}
				catch
				{
				}
			}
			_whitenedTextureCache.Remove(item);
			num--;
		}
	}

	private static void ApplyToParticleSystem(ParticleSystem ps, Color color)
	{
		if (ps == null)
		{
			return;
		}
		try
		{
			int instanceID = ps.GetInstanceID();
			if (!_originalParticles.ContainsKey(instanceID))
			{
				Color color2 = Color.white;
				try
				{
					color2 = ps.main.startColor.color;
				}
				catch
				{
				}
				_originalParticles[instanceID] = new ParticleOrig
				{
					ps = ps,
					color = color2
				};
			}
		}
		catch
		{
		}
		try
		{
			ps.main.startColor = new ParticleSystem.MinMaxGradient(color);
		}
		catch
		{
		}
	}

	private static void ApplyToProjector(Projector proj, Color color)
	{
		if (proj == null)
		{
			return;
		}
		try
		{
			int instanceID = proj.GetInstanceID();
			if (!_originalProjectors.ContainsKey(instanceID))
			{
				Color color2 = Color.white;
				try
				{
					if (proj.material != null)
					{
						color2 = proj.material.color;
					}
				}
				catch
				{
				}
				_originalProjectors[instanceID] = new ProjectorOrig
				{
					proj = proj,
					color = color2
				};
			}
		}
		catch
		{
		}
		try
		{
			Material material = proj.material;
			if (!(material != null))
			{
				return;
			}
			try
			{
				material.color = color;
			}
			catch
			{
			}
			string[] colorPropertyNames = ColorPropertyNames;
			foreach (string text in colorPropertyNames)
			{
				try
				{
					if (material.HasProperty(text))
					{
						material.SetColor(text, color);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private static void ApplyToGraphic(Graphic g, Color color)
	{
		if (g == null)
		{
			return;
		}
		try
		{
			int instanceID = g.GetInstanceID();
			if (!_originalGraphics.ContainsKey(instanceID))
			{
				_originalGraphics[instanceID] = new GraphicOrig
				{
					g = g,
					color = g.color
				};
			}
		}
		catch
		{
		}
		try
		{
			if (g.color != color)
			{
				g.color = color;
			}
		}
		catch
		{
		}
	}

	private static string[] GetShaderColorPropertyNames(Material mat)
	{
		if (mat == null)
		{
			return _emptyStringArray;
		}
		Shader shader;
		try
		{
			shader = mat.shader;
		}
		catch
		{
			return _emptyStringArray;
		}
		if (shader == null)
		{
			return _emptyStringArray;
		}
		int instanceID;
		try
		{
			instanceID = shader.GetInstanceID();
		}
		catch
		{
			return _emptyStringArray;
		}
		if (_shaderColorPropsCache.TryGetValue(instanceID, out var value))
		{
			return value;
		}
		value = BuildShaderPropertyNames(shader, ShaderPropertyType.Color);
		_shaderColorPropsCache[instanceID] = value;
		return value;
	}

	private static string[] GetShaderTexturePropertyNames(Material mat)
	{
		if (mat == null)
		{
			return _emptyStringArray;
		}
		Shader shader;
		try
		{
			shader = mat.shader;
		}
		catch
		{
			return _emptyStringArray;
		}
		if (shader == null)
		{
			return _emptyStringArray;
		}
		int instanceID;
		try
		{
			instanceID = shader.GetInstanceID();
		}
		catch
		{
			return _emptyStringArray;
		}
		if (_shaderTexturePropsCache.TryGetValue(instanceID, out var value))
		{
			return value;
		}
		value = BuildShaderPropertyNames(shader, ShaderPropertyType.Texture);
		_shaderTexturePropsCache[instanceID] = value;
		return value;
	}

	private static string[] BuildShaderPropertyNames(Shader sh, ShaderPropertyType wantType)
	{
		try
		{
			int propertyCount = sh.GetPropertyCount();
			if (propertyCount <= 0)
			{
				return _emptyStringArray;
			}
			List<string> list = new List<string>(propertyCount);
			for (int i = 0; i < propertyCount; i++)
			{
				try
				{
					if (sh.GetPropertyType(i) == wantType)
					{
						string propertyName = sh.GetPropertyName(i);
						if (!string.IsNullOrEmpty(propertyName))
						{
							list.Add(propertyName);
						}
					}
				}
				catch
				{
				}
			}
			if (list.Count == 0)
			{
				return _emptyStringArray;
			}
			return list.ToArray();
		}
		catch
		{
			return _emptyStringArray;
		}
	}

	private static void SetColorIfPresent(Material mat, string prop, Color color)
	{
		if (mat == null || string.IsNullOrEmpty(prop))
		{
			return;
		}
		try
		{
			if (mat.HasProperty(prop))
			{
				mat.SetColor(prop, color);
			}
		}
		catch
		{
		}
	}

	private static void SetTextureIfPresent(Material mat, string prop, Texture tex)
	{
		if (mat == null || string.IsNullOrEmpty(prop))
		{
			return;
		}
		try
		{
			if (mat.HasProperty(prop))
			{
				mat.SetTexture(prop, tex);
			}
		}
		catch
		{
		}
	}

	private static Color GetColorIfPresent(Material mat, string prop)
	{
		if (mat == null || string.IsNullOrEmpty(prop))
		{
			return Color.clear;
		}
		try
		{
			if (mat.HasProperty(prop))
			{
				return mat.GetColor(prop);
			}
		}
		catch
		{
		}
		return Color.clear;
	}

	private static void ApplyRendererColor(Renderer r, Color color)
	{
		if (r == null)
		{
			return;
		}
		bool flag = false;
		try
		{
			Material sharedMaterial = r.sharedMaterial;
			if (sharedMaterial != null && sharedMaterial.shader != null)
			{
				string text = sharedMaterial.shader.name ?? string.Empty;
				if (text.StartsWith("Particles/") || text.IndexOf("Particles", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					flag = true;
				}
			}
		}
		catch
		{
		}
		_reusableDynNames.Clear();
		_reusableTexNames.Clear();
		HashSet<string> reusableDynNames = _reusableDynNames;
		HashSet<string> reusableTexNames = _reusableTexNames;
		Il2CppArrayBase<Material> il2CppArrayBase = null;
		int num = 0;
		try
		{
			il2CppArrayBase = r.sharedMaterials;
			num = il2CppArrayBase?.Length ?? 0;
			for (int i = 0; i < num; i++)
			{
				Material material = il2CppArrayBase[i];
				if (!(material == null))
				{
					string[] shaderColorPropertyNames = GetShaderColorPropertyNames(material);
					for (int j = 0; j < shaderColorPropertyNames.Length; j++)
					{
						reusableDynNames.Add(shaderColorPropertyNames[j]);
					}
					string[] shaderTexturePropertyNames = GetShaderTexturePropertyNames(material);
					for (int k = 0; k < shaderTexturePropertyNames.Length; k++)
					{
						reusableTexNames.Add(shaderTexturePropertyNames[k]);
					}
				}
			}
		}
		catch
		{
		}
		for (int l = 0; l < ColorPropertyNames.Length; l++)
		{
			reusableDynNames.Add(ColorPropertyNames[l]);
		}
		for (int m = 0; m < TexturePropertyNames.Length; m++)
		{
			reusableTexNames.Add(TexturePropertyNames[m]);
		}
		Texture texture = null;
		try
		{
			texture = GetWhiteTexture();
		}
		catch
		{
			texture = null;
		}
		try
		{
			if (_mpb == null)
			{
				_mpb = new MaterialPropertyBlock();
			}
			r.GetPropertyBlock(_mpb);
			foreach (string item3 in reusableDynNames)
			{
				try
				{
					_mpb.SetColor(item3, color);
				}
				catch
				{
				}
			}
			if (texture != null && !flag)
			{
				foreach (string item4 in reusableTexNames)
				{
					try
					{
						_mpb.SetTexture(item4, texture);
					}
					catch
					{
					}
				}
			}
			r.SetPropertyBlock(_mpb);
		}
		catch
		{
		}
		try
		{
			Il2CppArrayBase<Material> il2CppArrayBase2 = il2CppArrayBase;
			int num2 = num;
			if (il2CppArrayBase2 == null)
			{
				try
				{
					il2CppArrayBase2 = r.sharedMaterials;
					num2 = il2CppArrayBase2?.Length ?? 0;
				}
				catch
				{
					il2CppArrayBase2 = null;
					num2 = 0;
				}
			}
			int instanceID = r.GetInstanceID();
			for (int n = 0; n < num2; n++)
			{
				Material material2 = il2CppArrayBase2[n];
				if (material2 == null)
				{
					continue;
				}
				foreach (string item5 in reusableDynNames)
				{
					try
					{
						if (material2.HasProperty(item5))
						{
							long item = ((long)instanceID << 32) ^ (uint)item5.GetHashCode();
							if (_rendererOrigCachedKeys.Add(item))
							{
								_originalRenderers.Add(new RendererOrig
								{
									r = r,
									color = material2.GetColor(item5),
									property = item5
								});
							}
							material2.SetColor(item5, color);
						}
					}
					catch
					{
					}
				}
				try
				{
					material2.color = color;
				}
				catch
				{
				}
				if (texture != null && !flag)
				{
					foreach (string item6 in reusableTexNames)
					{
						try
						{
							if (!material2.HasProperty(item6))
							{
								continue;
							}
							long item2 = ((long)instanceID << 32) ^ (uint)("T_" + item6).GetHashCode();
							if (_rendererTexOrigCachedKeys.Add(item2))
							{
								Texture original = null;
								try
								{
									original = material2.GetTexture(item6);
								}
								catch
								{
								}
								_originalRendererTextures.Add(new RendererTexOrig
								{
									r = r,
									original = original,
									property = item6
								});
							}
							material2.SetTexture(item6, texture);
						}
						catch
						{
						}
					}
				}
				try
				{
					if (material2.HasProperty("_EmissionColor"))
					{
						material2.SetColor("_EmissionColor", color);
						try
						{
							material2.EnableKeyword("_EMISSION");
						}
						catch
						{
						}
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		try
		{
			Shader standardShader = GetStandardShader();
			Il2CppArrayBase<Material> il2CppArrayBase3 = null;
			try
			{
				il2CppArrayBase3 = r.materials;
			}
			catch
			{
				il2CppArrayBase3 = null;
			}
			int num3 = il2CppArrayBase3?.Length ?? 0;
			for (int num4 = 0; num4 < num3; num4++)
			{
				Material material3 = il2CppArrayBase3[num4];
				if (material3 == null)
				{
					continue;
				}
				if (standardShader != null)
				{
					Shader shader = null;
					try
					{
						shader = material3.shader;
					}
					catch
					{
						shader = null;
					}
					if (shader != standardShader)
					{
						bool flag2 = false;
						for (int num5 = 0; num5 < _originalRendererShaders.Count; num5++)
						{
							if (_originalRendererShaders[num5].mat == material3)
							{
								flag2 = true;
								break;
							}
						}
						if (!flag2)
						{
							_originalRendererShaders.Add(new RendererShaderOrig
							{
								mat = material3,
								original = shader
							});
						}
						try
						{
							material3.shader = standardShader;
						}
						catch
						{
						}
					}
				}
				SetColorIfPresent(material3, "_Color", color);
				try
				{
					material3.color = color;
				}
				catch
				{
				}
				SetColorIfPresent(material3, "_EmissionColor", color);
				try
				{
					material3.EnableKeyword("_EMISSION");
				}
				catch
				{
				}
				foreach (string item7 in reusableDynNames)
				{
					SetColorIfPresent(material3, item7, color);
				}
				if (!(texture != null) || flag)
				{
					continue;
				}
				SetTextureIfPresent(material3, "_MainTex", texture);
				foreach (string item8 in reusableTexNames)
				{
					SetTextureIfPresent(material3, item8, texture);
				}
			}
		}
		catch
		{
		}
		try
		{
			LineRenderer lineRenderer = r.TryCast<LineRenderer>();
			if (lineRenderer != null)
			{
				lineRenderer.startColor = color;
				lineRenderer.endColor = color;
			}
		}
		catch
		{
		}
		try
		{
			SpriteRenderer spriteRenderer = r.TryCast<SpriteRenderer>();
			if (spriteRenderer != null)
			{
				int instanceID2 = spriteRenderer.GetInstanceID();
				if (!_originalSprites.ContainsKey(instanceID2))
				{
					_originalSprites[instanceID2] = new SpriteOrig
					{
						s = spriteRenderer,
						color = spriteRenderer.color
					};
				}
				if (spriteRenderer.color != color)
				{
					spriteRenderer.color = color;
				}
			}
		}
		catch
		{
		}
	}

	public static void RestoreOriginalsNow()
	{
		int num = 0;
		foreach (KeyValuePair<int, GraphicOrig> originalGraphic in _originalGraphics)
		{
			GraphicOrig value = originalGraphic.Value;
			if (!(value.g == null))
			{
				try
				{
					value.g.color = value.color;
					num++;
				}
				catch
				{
				}
			}
		}
		foreach (KeyValuePair<int, SpriteOrig> originalSprite in _originalSprites)
		{
			SpriteOrig value2 = originalSprite.Value;
			if (!(value2.s == null))
			{
				try
				{
					value2.s.color = value2.color;
					num++;
				}
				catch
				{
				}
			}
		}
		foreach (KeyValuePair<int, ParticleOrig> originalParticle in _originalParticles)
		{
			ParticleOrig value3 = originalParticle.Value;
			if (!(value3.ps == null))
			{
				try
				{
					value3.ps.main.startColor = new ParticleSystem.MinMaxGradient(value3.color);
					num++;
				}
				catch
				{
				}
			}
		}
		foreach (KeyValuePair<int, ProjectorOrig> originalProjector in _originalProjectors)
		{
			ProjectorOrig value4 = originalProjector.Value;
			if (value4.proj == null)
			{
				continue;
			}
			try
			{
				Material material = value4.proj.material;
				if (material != null)
				{
					material.color = value4.color;
					num++;
				}
			}
			catch
			{
			}
		}
		int num2 = 0;
		try
		{
			for (int num3 = _originalDecalColors.Count - 1; num3 >= 0; num3--)
			{
				DecalColorOrig decalColorOrig = _originalDecalColors[num3];
				if (!(decalColorOrig.decal == null))
				{
					Material material2 = null;
					try
					{
						material2 = decalColorOrig.decal.material;
					}
					catch
					{
						material2 = null;
					}
					if (!(material2 == null))
					{
						try
						{
							if (material2.HasProperty(decalColorOrig.property))
							{
								material2.SetColor(decalColorOrig.property, decalColorOrig.color);
								num2++;
							}
						}
						catch
						{
						}
					}
				}
			}
		}
		catch
		{
		}
		int num4 = 0;
		try
		{
			for (int num5 = _originalDecalTextures.Count - 1; num5 >= 0; num5--)
			{
				DecalTexOrig decalTexOrig = _originalDecalTextures[num5];
				if (!(decalTexOrig.decal == null))
				{
					Material material3 = null;
					try
					{
						material3 = decalTexOrig.decal.material;
					}
					catch
					{
						material3 = null;
					}
					if (!(material3 == null))
					{
						try
						{
							if (material3.HasProperty(decalTexOrig.property))
							{
								material3.SetTexture(decalTexOrig.property, decalTexOrig.original);
								num4++;
							}
						}
						catch
						{
						}
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			if (_emptyMpb == null)
			{
				_emptyMpb = new MaterialPropertyBlock();
			}
			_emptyMpb.Clear();
			HashSet<int> hashSet = new HashSet<int>();
			for (int i = 0; i < _originalRenderers.Count; i++)
			{
				RendererOrig rendererOrig = _originalRenderers[i];
				if (rendererOrig.r == null)
				{
					continue;
				}
				int instanceID = rendererOrig.r.GetInstanceID();
				if (hashSet.Add(instanceID))
				{
					try
					{
						rendererOrig.r.SetPropertyBlock(_emptyMpb);
					}
					catch
					{
					}
				}
			}
			for (int num6 = _originalRenderers.Count - 1; num6 >= 0; num6--)
			{
				RendererOrig rendererOrig2 = _originalRenderers[num6];
				if (!(rendererOrig2.r == null))
				{
					try
					{
						Il2CppReferenceArray<Material> sharedMaterials = rendererOrig2.r.sharedMaterials;
						for (int j = 0; j < ((Il2CppArrayBase<Material>)sharedMaterials).Length; j++)
						{
							Material material4 = ((Il2CppArrayBase<Material>)sharedMaterials)[j];
							if (!(material4 == null))
							{
								if (material4.HasProperty(rendererOrig2.property))
								{
									material4.SetColor(rendererOrig2.property, rendererOrig2.color);
								}
								try
								{
									material4.DisableKeyword("_EMISSION");
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
			}
		}
		catch
		{
		}
		int num7 = 0;
		try
		{
			for (int num8 = _originalRendererTextures.Count - 1; num8 >= 0; num8--)
			{
				RendererTexOrig rendererTexOrig = _originalRendererTextures[num8];
				if (!(rendererTexOrig.r == null))
				{
					try
					{
						Il2CppReferenceArray<Material> sharedMaterials2 = rendererTexOrig.r.sharedMaterials;
						for (int k = 0; k < ((Il2CppArrayBase<Material>)sharedMaterials2).Length; k++)
						{
							Material material5 = ((Il2CppArrayBase<Material>)sharedMaterials2)[k];
							if (!(material5 == null) && material5.HasProperty(rendererTexOrig.property))
							{
								material5.SetTexture(rendererTexOrig.property, rendererTexOrig.original);
								num7++;
							}
						}
						Material material6 = rendererTexOrig.r.material;
						if (material6 != null && material6.HasProperty(rendererTexOrig.property))
						{
							material6.SetTexture(rendererTexOrig.property, rendererTexOrig.original);
						}
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
		int num9 = 0;
		try
		{
			for (int num10 = _originalRendererShaders.Count - 1; num10 >= 0; num10--)
			{
				RendererShaderOrig rendererShaderOrig = _originalRendererShaders[num10];
				if (!(rendererShaderOrig.mat == null) && !(rendererShaderOrig.original == null))
				{
					try
					{
						rendererShaderOrig.mat.shader = rendererShaderOrig.original;
						num9++;
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
		try
		{
			_originalRendererTextures.Clear();
		}
		catch
		{
		}
		try
		{
			_originalRendererShaders.Clear();
		}
		catch
		{
		}
		try
		{
			_rendererOrigCachedKeys.Clear();
		}
		catch
		{
		}
		try
		{
			_rendererTexOrigCachedKeys.Clear();
		}
		catch
		{
		}
		try
		{
			_originalRenderers.Clear();
		}
		catch
		{
		}
		try
		{
			_originalDecalColors.Clear();
		}
		catch
		{
		}
		try
		{
			_decalColorCachedKeys.Clear();
		}
		catch
		{
		}
		try
		{
			_originalDecalTextures.Clear();
		}
		catch
		{
		}
		try
		{
			_decalTexCachedKeys.Clear();
		}
		catch
		{
		}
	}
}
