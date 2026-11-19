using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Rendering;

namespace CBSafe.Holo;

internal static class HolosightSwap
{
	private struct NativeTransform
	{
		internal Vector3 LocalScale;

		internal Vector3 LocalPosition;

		internal Quaternion LocalRotation;

		internal static NativeTransform Capture(Transform root)
		{
			NativeTransform result;
			if (root == null)
			{
				result = default(NativeTransform);
				result.LocalScale = Vector3.one;
				result.LocalPosition = Vector3.zero;
				result.LocalRotation = Quaternion.identity;
				return result;
			}
			result = default(NativeTransform);
			result.LocalScale = root.localScale;
			result.LocalPosition = root.localPosition;
			result.LocalRotation = root.localRotation;
			return result;
		}

		internal void ApplyTo(Transform root, Vector3 positionOffset, Vector3 rotationOffsetEuler)
		{
			if (!(root == null))
			{
				root.localScale = LocalScale;
				root.localPosition = LocalPosition + positionOffset;
				root.localRotation = LocalRotation * Quaternion.Euler(rotationOffsetEuler);
			}
		}
	}

	private sealed class ObjParser
	{
		private readonly List<ObjGroup> _groups = new List<ObjGroup>();

		internal ObjParser(string objText)
		{
			List<Vector3> list = new List<Vector3>();
			List<Vector2> list2 = new List<Vector2>();
			List<Vector3> list3 = new List<Vector3>();
			ObjGroup objGroup = new ObjGroup
			{
				Name = "default"
			};
			_groups.Add(objGroup);
			string[] array = objText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length == 0 || text[0] == '#')
				{
					continue;
				}
				if (text.StartsWith("o ", StringComparison.Ordinal) || text.StartsWith("g ", StringComparison.Ordinal))
				{
					if (objGroup.Vertices.Count > 0)
					{
						objGroup = new ObjGroup
						{
							Name = text.Substring(2).Trim()
						};
						_groups.Add(objGroup);
					}
					else
					{
						objGroup.Name = text.Substring(2).Trim();
					}
				}
				else if (text.StartsWith("usemtl ", StringComparison.Ordinal))
				{
					objGroup.Material = text.Substring(7).Trim();
				}
				else if (text.StartsWith("v ", StringComparison.Ordinal))
				{
					string[] array2 = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
					if (array2.Length >= 4)
					{
						list.Add(new Vector3(ParseF(array2[1]), ParseF(array2[2]), ParseF(array2[3])));
					}
				}
				else if (text.StartsWith("vt ", StringComparison.Ordinal))
				{
					string[] array3 = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
					if (array3.Length >= 3)
					{
						list2.Add(new Vector2(ParseF(array3[1]), ParseF(array3[2])));
					}
				}
				else if (text.StartsWith("vn ", StringComparison.Ordinal))
				{
					string[] array4 = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
					if (array4.Length >= 4)
					{
						list3.Add(new Vector3(ParseF(array4[1]), ParseF(array4[2]), ParseF(array4[3])));
					}
				}
				else if (text.StartsWith("f ", StringComparison.Ordinal))
				{
					string[] array5 = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
					if (array5.Length >= 4)
					{
						objGroup.AddFace(array5, list, list2, list3);
					}
				}
			}
			for (int num = _groups.Count - 1; num >= 0; num--)
			{
				if (_groups[num].Vertices.Count == 0)
				{
					_groups.RemoveAt(num);
				}
			}
		}

		internal bool TryGetGroup(string name, out ObjGroup group)
		{
			for (int i = 0; i < _groups.Count; i++)
			{
				if (_groups[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					group = _groups[i];
					return true;
				}
			}
			group = null;
			return false;
		}

		private static float ParseF(string s)
		{
			float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result);
			return result;
		}
	}

	private sealed class ObjGroup
	{
		internal string Name = string.Empty;

		internal string Material = string.Empty;

		internal readonly List<Vector3> Vertices = new List<Vector3>();

		internal readonly List<Vector2> Uvs = new List<Vector2>();

		internal readonly List<Vector3> Normals = new List<Vector3>();

		internal readonly List<int> Triangles = new List<int>();

		internal void AddFace(string[] tokens, List<Vector3> pos, List<Vector2> uv, List<Vector3> norm)
		{
			int num = tokens.Length - 1;
			if (num >= 3)
			{
				int[] array = new int[num];
				for (int i = 0; i < num; i++)
				{
					array[i] = AddCorner(tokens[i + 1], pos, uv, norm);
				}
				for (int j = 1; j < num - 1; j++)
				{
					Triangles.Add(array[0]);
					Triangles.Add(array[j]);
					Triangles.Add(array[j + 1]);
				}
			}
		}

		private int AddCorner(string corner, List<Vector3> pos, List<Vector2> uv, List<Vector3> norm)
		{
			string[] array = corner.Split('/');
			int index = ResolveIndex(array[0], pos.Count);
			Vertices.Add(pos[index]);
			if (array.Length > 1 && array[1].Length > 0 && uv.Count > 0)
			{
				Uvs.Add(uv[ResolveIndex(array[1], uv.Count)]);
			}
			else
			{
				Uvs.Add(Vector2.zero);
			}
			if (array.Length > 2 && array[2].Length > 0 && norm.Count > 0)
			{
				Normals.Add(norm[ResolveIndex(array[2], norm.Count)]);
			}
			else
			{
				Normals.Add(Vector3.up);
			}
			return Vertices.Count - 1;
		}

		private static int ResolveIndex(string token, int count)
		{
			int num = int.Parse(token, CultureInfo.InvariantCulture);
			num = ((num >= 0) ? (num - 1) : (count + num));
			return Mathf.Clamp(num, 0, count - 1);
		}

		internal Mesh Build()
		{
			Mesh mesh = new Mesh();
			mesh.vertices = Vertices.ToArray();
			if (Uvs.Count == Vertices.Count)
			{
				mesh.uv = Uvs.ToArray();
			}
			if (Normals.Count == Vertices.Count)
			{
				mesh.normals = Normals.ToArray();
			}
			else
			{
				mesh.RecalculateNormals();
			}
			mesh.triangles = Triangles.ToArray();
			mesh.RecalculateBounds();
			mesh.hideFlags = HideFlags.HideAndDontSave;
			return mesh;
		}

		internal static void SplitByAlpha(ObjGroup source, Texture2D tex, float threshold, out ObjGroup solid, out ObjGroup glass)
		{
			solid = new ObjGroup
			{
				Name = source.Name,
				Material = source.Material
			};
			glass = new ObjGroup
			{
				Name = source.Name + "_glass",
				Material = source.Material
			};
			Color32[] pixels = null;
			int w = 0;
			int h = 0;
			try
			{
				w = tex.width;
				h = tex.height;
				pixels = tex.GetPixels32();
			}
			catch
			{
			}
			int count = source.Triangles.Count;
			for (int i = 0; i < count; i += 3)
			{
				int num = source.Triangles[i];
				int num2 = source.Triangles[i + 1];
				int num3 = source.Triangles[i + 2];
				ObjGroup obj2 = (IsTriTransparent(source, pixels, w, h, threshold, num, num2, num3) ? glass : solid);
				int item = obj2.AddVertex(source, num);
				int item2 = obj2.AddVertex(source, num2);
				int item3 = obj2.AddVertex(source, num3);
				obj2.Triangles.Add(item);
				obj2.Triangles.Add(item2);
				obj2.Triangles.Add(item3);
			}
		}

		private int AddVertex(ObjGroup src, int index)
		{
			Vertices.Add(src.Vertices[index]);
			Uvs.Add(src.Uvs[index]);
			Normals.Add(src.Normals[index]);
			return Vertices.Count - 1;
		}

		private static bool IsTriTransparent(ObjGroup src, Color32[] pixels, int w, int h, float threshold, int a, int b, int c)
		{
			if (SampleAlpha(pixels, w, h, src.Uvs[a]) < threshold && SampleAlpha(pixels, w, h, src.Uvs[b]) < threshold)
			{
				return SampleAlpha(pixels, w, h, src.Uvs[c]) < threshold;
			}
			return false;
		}

		private static float SampleAlpha(Color32[] pixels, int w, int h, Vector2 uv)
		{
			if (pixels == null || w <= 0 || h <= 0)
			{
				return 1f;
			}
			try
			{
				int num = Mathf.Clamp((int)(uv.x * (float)w), 0, w - 1);
				int num2 = Mathf.Clamp((int)(uv.y * (float)h), 0, h - 1);
				return (float)(int)pixels[num2 * w + num].a / 255f;
			}
			catch
			{
				return 1f;
			}
		}
	}

	private static readonly string[] TexProps = new string[4] { "_MainTex", "_BaseMap", "_BaseColorMap", "_AlbedoMap" };

	private static readonly byte[] _hxK = new byte[8] { 90, 60, 145, 7, 226, 68, 27, 136 };

	private static readonly byte[] _bSpawned = new byte[17]
	{
		41, 76, 240, 112, 140, 33, 127, 207, 59, 81,
		244, 72, 128, 46, 126, 235, 46
	};

	private static readonly byte[] _bWeapon = new byte[20]
	{
		45, 89, 240, 119, 141, 42, 53, 229, 53, 88,
		191, 111, 141, 40, 116, 251, 51, 91, 249, 115
	};

	private static readonly byte[] _bHolo = new byte[9] { 50, 83, 253, 104, 145, 45, 124, 224, 46 };

	private static readonly byte[] _bHoloU = new byte[10] { 50, 83, 253, 104, 189, 55, 114, 239, 50, 72 };

	private static readonly byte[] _bRet = new byte[7] { 40, 89, 229, 110, 129, 40, 126 };

	private static readonly byte[] _bGeo = new byte[10] { 61, 89, 254, 117, 135, 48, 114, 235, 54, 89 };

	private static readonly byte[] _bDot = new byte[3] { 62, 83, 229 };

	private static readonly byte[] _bVm = new byte[19]
	{
		50, 83, 253, 104, 145, 45, 124, 224, 46, 99,
		231, 110, 135, 51, 118, 231, 62, 89, 253
	};

	private static readonly byte[] _bLod = new byte[13]
	{
		50, 83, 253, 104, 145, 45, 124, 224, 46, 99,
		253, 104, 134
	};

	private static readonly byte[] _bMs = new byte[12]
	{
		55, 79, 206, 111, 141, 40, 116, 251, 51, 91,
		249, 115
	};

	private static readonly byte[] _bGlass = new byte[14]
	{
		12, 89, 248, 107, 170, 43, 119, 231, 5, 123,
		253, 102, 145, 55
	};

	private static readonly byte[] _bCross = new byte[9] { 57, 78, 254, 116, 145, 44, 122, 225, 40 };

	private static readonly byte[] _bGl = new byte[5] { 61, 80, 240, 116, 145 };

	private static readonly byte[] _bView = new byte[9] { 44, 85, 244, 112, 143, 43, 127, 237, 54 };

	private static readonly byte[] _bScope = new byte[15]
	{
		41, 95, 254, 119, 135, 106, 115, 231, 54, 83,
		226, 110, 133, 44, 111
	};

	private static readonly byte[] _bEmMap = new byte[12]
	{
		5, 121, 252, 110, 145, 55, 114, 231, 52, 113,
		240, 119
	};

	private static readonly byte[] _bEmKw = new byte[9] { 5, 121, 220, 78, 177, 23, 82, 199, 20 };

	private static readonly byte[] _bMode = new byte[5] { 5, 113, 254, 99, 135 };

	private static readonly byte[] _bSrc = new byte[9] { 5, 111, 227, 100, 160, 40, 126, 230, 62 };

	private static readonly byte[] _bDst = new byte[9] { 5, 120, 226, 115, 160, 40, 126, 230, 62 };

	private static readonly byte[] _bZw = new byte[7] { 5, 102, 198, 117, 139, 48, 126 };

	private static readonly byte[] _bAlpha = new byte[14]
	{
		5, 125, 221, 87, 170, 5, 89, 196, 31, 114,
		213, 88, 173, 10
	};

	private static readonly byte[] _bMat0 = new byte[13]
	{
		12, 89, 248, 107, 170, 43, 119, 231, 5, 81,
		240, 115, 210
	};

	private static readonly byte[] _bMat1 = new byte[13]
	{
		12, 89, 248, 107, 170, 43, 119, 231, 5, 81,
		240, 115, 211
	};

	private static readonly string TokSpawned = Hx(_bSpawned);

	private static readonly string TokWeapon = Hx(_bWeapon);

	private static readonly string TokHolo = Hx(_bHolo);

	private static readonly string TokHoloU = Hx(_bHoloU);

	private static readonly string TokRet = Hx(_bRet);

	private static readonly string TokGeo = Hx(_bGeo);

	private static readonly string TokDot = Hx(_bDot);

	private static readonly string TokVm = Hx(_bVm);

	private static readonly string TokLod = Hx(_bLod);

	private static readonly string TokMs = Hx(_bMs);

	private static readonly string TokCross = Hx(_bCross);

	private static readonly string TokGl = Hx(_bGl);

	private static readonly string TokView = Hx(_bView);

	private static readonly string TokScope = Hx(_bScope);

	private static readonly string TokEmMap = Hx(_bEmMap);

	private static readonly string TokEmKw = Hx(_bEmKw);

	private static readonly string TokMode = Hx(_bMode);

	private static readonly string TokSrc = Hx(_bSrc);

	private static readonly string TokDst = Hx(_bDst);

	private static readonly string TokZw = Hx(_bZw);

	private static readonly string TokAlpha = Hx(_bAlpha);

	private static readonly string TokMat0 = Hx(_bMat0);

	private static readonly string TokMat1 = Hx(_bMat1);

	private static readonly Vector3 VmPosition = new Vector3(-0.002f, -0.021f, -0.018f);

	private static readonly Vector3 VmRotation = new Vector3(90f, 0f, 0f);

	private static bool _enabledFlag;

	private static ConfigEntry<bool> _replaceMesh;

	private static Texture2D _bodyTex;

	private static Texture2D _glassTex;

	private static Mesh _viewMesh;

	private static Mesh _glassMesh;

	private static Material _bodyMat;

	private static Material _glassMat;

	private static readonly HashSet<int> AppliedRoots = new HashSet<int>();

	private static readonly HashSet<int> AppliedRenderers = new HashSet<int>();

	private static readonly HashSet<int> PendingAttachments = new HashSet<int>();

	private static readonly HashSet<int> TransformApplied = new HashSet<int>();

	private static readonly HashSet<int> PreHiddenRenderers = new HashSet<int>();

	private static MaterialPropertyBlock _mpb;

	private static float _nextActiveScanAt;

	private static int _idleHeldId = int.MinValue;

	private static int _idleRootId = int.MinValue;

	private static readonly List<int> PendingDone = new List<int>(8);

	private static string GlassChild => Hx(_bGlass);

	private static string ObjResource => "CBSafeHolo.Embedded.holosight_dump.obj";

	private static string BodyTexResource => "CBSafeHolo.Embedded.mat_0.png";

	private static string GlassTexResource => "CBSafeHolo.Embedded.mat_1.png";

	internal static bool NewHoloActive => _enabledFlag;

	private static string Hx(byte[] en)
	{
		byte[] array = new byte[en.Length];
		for (int i = 0; i < en.Length; i++)
		{
			array[i] = (byte)(en[i] ^ _hxK[i % _hxK.Length]);
		}
		return Encoding.UTF8.GetString(array);
	}

	internal static void Init(ConfigFile cfg, ManualLogSource log)
	{
		_enabledFlag = false;
		ConfigFile configFile = cfg;
		try
		{
			configFile = SharedConfig.File ?? cfg;
		}
		catch
		{
			configFile = cfg;
		}
		_replaceMesh = configFile.Bind("CBSafeHolo", "ReplaceHolosightMesh", defaultValue: true, "Подмена 3D-модели из embedded OBJ.");
		try
		{
			if (cfg != null)
			{
				ConfigEntry<bool> configEntry = cfg.Bind("General", "Enabled", defaultValue: false);
				if (configEntry.Value)
				{
					configEntry.Value = false;
				}
				try
				{
					cfg.Save();
					return;
				}
				catch
				{
					return;
				}
			}
		}
		catch
		{
		}
	}

	internal static void SetEnabled(bool on)
	{
		_enabledFlag = on;
		PendingAttachments.Clear();
		AppliedRoots.Clear();
		AppliedRenderers.Clear();
		TransformApplied.Clear();
		PreHiddenRenderers.Clear();
		_idleHeldId = int.MinValue;
		_idleRootId = int.MinValue;
		if (on)
		{
			EnsureReady();
			TickActive();
		}
		else
		{
			ResetHeldHolosights();
		}
	}

	private static void EnsureReady()
	{
		if (!_enabledFlag)
		{
			return;
		}
		LoadEmbeddedTextures();
		if (_glassMesh != null)
		{
			try
			{
				UnityEngine.Object.DestroyImmediate(_glassMesh);
			}
			catch
			{
			}
			_glassMesh = null;
			try
			{
				if (_viewMesh != null)
				{
					UnityEngine.Object.DestroyImmediate(_viewMesh);
				}
			}
			catch
			{
			}
			_viewMesh = null;
			AppliedRoots.Clear();
			_idleRootId = int.MinValue;
		}
		if (_viewMesh == null)
		{
			WarmupMesh();
		}
	}

	private static void ResetHeldHolosights()
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
			Il2CppArrayBase<ViewmodelAttachment> componentsInChildren = heldEntity.GetComponentsInChildren<ViewmodelAttachment>(includeInactive: true);
			if (componentsInChildren == null)
			{
				return;
			}
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ViewmodelAttachment viewmodelAttachment = componentsInChildren[i];
				if (viewmodelAttachment == null)
				{
					continue;
				}
				GameObject spawnedObject = GetSpawnedObject(viewmodelAttachment);
				if (spawnedObject == null || !IsHolosight(spawnedObject, viewmodelAttachment))
				{
					continue;
				}
				try
				{
					UnityEngine.Object.DestroyImmediate(spawnedObject);
				}
				catch
				{
				}
				try
				{
					viewmodelAttachment.spawnedGameObject = null;
				}
				catch
				{
					try
					{
						Traverse.Create(viewmodelAttachment).Field<GameObject>(TokSpawned).Value = null;
					}
					catch
					{
					}
				}
				try
				{
					viewmodelAttachment.CreateAttachment();
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

	internal static void OnAttachment(ViewmodelAttachment attachment)
	{
		if (_enabledFlag && !(attachment == null))
		{
			EnsureReady();
			_idleHeldId = int.MinValue;
			_idleRootId = int.MinValue;
			try
			{
				PendingAttachments.Add(attachment.GetInstanceID());
			}
			catch
			{
			}
			TryApplyAttachment(attachment);
		}
	}

	internal static void TickRetry()
	{
		bool flag = false;
		try
		{
			flag = Defines.NewHolo?.Value ?? false;
		}
		catch
		{
			flag = false;
		}
		if (_enabledFlag != flag)
		{
			SetEnabled(flag);
			if (!flag)
			{
				return;
			}
		}
		if (!_enabledFlag)
		{
			return;
		}
		if (PendingAttachments.Count > 0)
		{
			PendingDone.Clear();
			foreach (int pendingAttachment in PendingAttachments)
			{
				ViewmodelAttachment viewmodelAttachment = FindAttachmentById(pendingAttachment);
				if (viewmodelAttachment == null)
				{
					PendingDone.Add(pendingAttachment);
				}
				else if (TryApplyAttachment(viewmodelAttachment))
				{
					PendingDone.Add(pendingAttachment);
				}
			}
			for (int i = 0; i < PendingDone.Count; i++)
			{
				PendingAttachments.Remove(PendingDone[i]);
			}
		}
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime >= _nextActiveScanAt)
		{
			_nextActiveScanAt = unscaledTime + ((_idleRootId != int.MinValue) ? 2.5f : 0.75f);
			TickActive();
		}
	}

	internal static GameObject GetSpawnedObject(ViewmodelAttachment attachment)
	{
		try
		{
			GameObject spawnedGameObject = attachment.spawnedGameObject;
			if (spawnedGameObject != null)
			{
				return spawnedGameObject;
			}
		}
		catch
		{
		}
		try
		{
			return Traverse.Create(attachment).Field<GameObject>(TokSpawned).Value;
		}
		catch
		{
			return null;
		}
	}

	internal static bool IsHolosight(GameObject root, ViewmodelAttachment attachment)
	{
		try
		{
			if (attachment != null)
			{
				BaseEntity baseEntity = ((EntityComponent<BaseEntity>)attachment).baseEntity;
				if (baseEntity != null)
				{
					Item item = baseEntity.GetItem();
					if (item != null && item.info != null && string.Equals(item.info.shortname, TokWeapon, StringComparison.Ordinal))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		string text = root.name ?? string.Empty;
		if (text.IndexOf(TokHolo, StringComparison.OrdinalIgnoreCase) < 0)
		{
			return text.IndexOf(TokHoloU, StringComparison.OrdinalIgnoreCase) >= 0;
		}
		return true;
	}

	private static bool TryApplyAttachment(ViewmodelAttachment attachment)
	{
		GameObject spawnedObject = GetSpawnedObject(attachment);
		if (spawnedObject == null || !IsHolosight(spawnedObject, attachment))
		{
			return false;
		}
		PreHideBodyRenderers(spawnedObject);
		if (_replaceMesh != null && _replaceMesh.Value)
		{
			TryApplyMesh(spawnedObject);
		}
		TryApplyTextures(spawnedObject, force: true);
		return AppliedRoots.Contains(spawnedObject.GetInstanceID());
	}

	private static ViewmodelAttachment FindAttachmentById(int attachmentId)
	{
		try
		{
			BasePlayer entity = LocalPlayer.Entity;
			if (entity == null)
			{
				return null;
			}
			HeldEntity heldEntity = entity.GetHeldEntity();
			if (heldEntity == null)
			{
				return null;
			}
			Il2CppArrayBase<ViewmodelAttachment> componentsInChildren = heldEntity.GetComponentsInChildren<ViewmodelAttachment>(includeInactive: true);
			if (componentsInChildren == null)
			{
				return null;
			}
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ViewmodelAttachment viewmodelAttachment = componentsInChildren[i];
				if (viewmodelAttachment != null && viewmodelAttachment.GetInstanceID() == attachmentId)
				{
					return viewmodelAttachment;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static void PreHideBodyRenderers(GameObject root)
	{
		if (AppliedRoots.Contains(root.GetInstanceID()))
		{
			return;
		}
		Il2CppArrayBase<Renderer> componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
		if (componentsInChildren == null)
		{
			return;
		}
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Renderer renderer = componentsInChildren[i];
			if (renderer == null)
			{
				continue;
			}
			string text = (renderer.gameObject.name ?? string.Empty).ToLowerInvariant();
			if (text.Contains(TokRet) || text.Contains(TokGeo) || text.Contains(TokDot) || (!text.Contains(TokVm) && !text.Contains(TokLod) && !text.Contains(TokMs)))
			{
				continue;
			}
			int instanceID = renderer.GetInstanceID();
			if (PreHiddenRenderers.Add(instanceID))
			{
				try
				{
					renderer.enabled = false;
				}
				catch
				{
				}
			}
		}
	}

	internal static void TickActive()
	{
		if (!_enabledFlag || !MainCamera.isValid)
		{
			return;
		}
		try
		{
			BasePlayer entity = LocalPlayer.Entity;
			if (entity == null || !entity.IsConnected)
			{
				_idleHeldId = int.MinValue;
				_idleRootId = int.MinValue;
				return;
			}
			HeldEntity heldEntity = entity.GetHeldEntity();
			if (heldEntity == null)
			{
				_idleHeldId = int.MinValue;
				_idleRootId = int.MinValue;
				return;
			}
			int instanceID = heldEntity.GetInstanceID();
			if (instanceID == _idleHeldId && _idleRootId != int.MinValue && AppliedRoots.Contains(_idleRootId))
			{
				return;
			}
			EnsureReady();
			Il2CppArrayBase<ViewmodelAttachment> componentsInChildren = heldEntity.GetComponentsInChildren<ViewmodelAttachment>(includeInactive: true);
			if (componentsInChildren == null)
			{
				return;
			}
			int num = int.MinValue;
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ViewmodelAttachment viewmodelAttachment = componentsInChildren[i];
				if (viewmodelAttachment == null)
				{
					continue;
				}
				GameObject spawnedObject = GetSpawnedObject(viewmodelAttachment);
				if (!(spawnedObject == null) && IsHolosight(spawnedObject, viewmodelAttachment))
				{
					int instanceID2 = spawnedObject.GetInstanceID();
					num = instanceID2;
					if (_replaceMesh != null && _replaceMesh.Value && !AppliedRoots.Contains(instanceID2))
					{
						TryApplyMesh(spawnedObject);
					}
					TryApplyTextures(spawnedObject, force: false);
				}
			}
			if (num != int.MinValue && AppliedRoots.Contains(num))
			{
				_idleHeldId = instanceID;
				_idleRootId = num;
			}
			else
			{
				_idleHeldId = int.MinValue;
				_idleRootId = int.MinValue;
			}
		}
		catch
		{
		}
	}

	private static void WarmupMesh()
	{
		if (_replaceMesh != null && !_replaceMesh.Value)
		{
			return;
		}
		try
		{
			if (TryLoadObjGroup(TokVm, out var group))
			{
				Texture2D texture2D = LoadBodyTexture();
				if (texture2D != null)
				{
					ObjGroup.SplitByAlpha(group, texture2D, 0.95f, out var solid, out var _);
					_viewMesh = ((solid.Triangles.Count > 0) ? solid.Build() : group.Build());
				}
				else
				{
					_viewMesh = group.Build();
				}
				_glassMesh = null;
				_glassMat = null;
			}
		}
		catch
		{
		}
	}

	private static bool TryApplyMesh(GameObject root)
	{
		if (_viewMesh == null)
		{
			return false;
		}
		int instanceID = root.GetInstanceID();
		if (AppliedRoots.Contains(instanceID))
		{
			return true;
		}
		Il2CppArrayBase<Renderer> componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
		if (componentsInChildren == null || componentsInChildren.Length == 0)
		{
			return false;
		}
		Renderer renderer = null;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Renderer renderer2 = componentsInChildren[i];
			if (!(renderer2 == null) && (renderer2.gameObject.name ?? string.Empty).ToLowerInvariant().Contains(TokVm))
			{
				renderer = renderer2;
				break;
			}
		}
		if (renderer == null)
		{
			return false;
		}
		if (!ReplaceRendererMesh(renderer, _viewMesh))
		{
			return false;
		}
		ApplyViewModelTransformOnce(renderer.transform);
		EnsureMaterials(renderer.sharedMaterial);
		if (_bodyMat != null)
		{
			renderer.sharedMaterial = _bodyMat;
		}
		try
		{
			renderer.enabled = true;
		}
		catch
		{
		}
		try
		{
			Transform transform = renderer.transform.Find(GlassChild);
			if (transform != null)
			{
				UnityEngine.Object.DestroyImmediate(transform.gameObject);
			}
		}
		catch
		{
		}
		HideExtraRenderers(root, renderer.GetInstanceID());
		AppliedRoots.Add(instanceID);
		return true;
	}

	private static void ApplyViewModelTransformOnce(Transform renderer)
	{
		if (!(renderer == null))
		{
			int instanceID = renderer.GetInstanceID();
			if (!TransformApplied.Contains(instanceID))
			{
				NativeTransform.Capture(renderer).ApplyTo(renderer, VmPosition, VmRotation);
				TransformApplied.Add(instanceID);
			}
		}
	}

	private static bool ReplaceRendererMesh(Renderer renderer, Mesh mesh)
	{
		try
		{
			MeshFilter component = renderer.GetComponent<MeshFilter>();
			if (component != null)
			{
				component.sharedMesh = mesh;
				return true;
			}
		}
		catch
		{
		}
		try
		{
			SkinnedMeshRenderer skinnedMeshRenderer = renderer.TryCast<SkinnedMeshRenderer>();
			if (skinnedMeshRenderer != null)
			{
				skinnedMeshRenderer.sharedMesh = mesh;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void AttachGlassChild(Transform parent, int layer)
	{
		Transform transform = parent.Find(GlassChild);
		if (transform != null)
		{
			UnityEngine.Object.DestroyImmediate(transform.gameObject);
		}
		GameObject gameObject = new GameObject(GlassChild);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.layer = layer;
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		meshFilter.sharedMesh = _glassMesh;
		meshRenderer.sharedMaterial = _glassMat;
		meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
		meshRenderer.receiveShadows = false;
	}

	private static void HideExtraRenderers(GameObject root, int keepId)
	{
		Il2CppArrayBase<Renderer> componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
		if (componentsInChildren == null)
		{
			return;
		}
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Renderer renderer = componentsInChildren[i];
			if (renderer == null || renderer.GetInstanceID() == keepId)
			{
				continue;
			}
			string text = (renderer.gameObject.name ?? string.Empty).ToLowerInvariant();
			if (!text.Contains(TokRet) && !text.Contains(TokGeo) && !text.Contains(TokDot) && (text.Contains(TokHolo) || text.Contains(TokHoloU) || text.Contains(TokMs)))
			{
				try
				{
					renderer.enabled = false;
				}
				catch
				{
				}
			}
		}
	}

	private static void TryApplyTextures(GameObject root, bool force)
	{
		if (!EnsureTextures())
		{
			return;
		}
		Il2CppArrayBase<Renderer> componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
		if (componentsInChildren != null)
		{
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ApplyTextureToRenderer(componentsInChildren[i], force);
			}
		}
	}

	private static void ApplyTextureToRenderer(Renderer r, bool force)
	{
		if (r == null)
		{
			return;
		}
		string text = (r.gameObject.name ?? string.Empty).ToLowerInvariant();
		if (text.Contains(TokRet) || text.Contains(TokGeo) || text.Contains(TokDot) || text.Contains(TokCross))
		{
			return;
		}
		if ((text.Contains(TokVm) && text.Contains(TokGl)) || text.Contains(TokGl))
		{
			int instanceID = r.GetInstanceID();
			if (AppliedRenderers.Add(instanceID))
			{
				try
				{
					r.enabled = false;
				}
				catch
				{
				}
			}
		}
		else
		{
			if (!text.Contains(TokVm) && !text.Contains(TokLod) && !text.Contains(TokMs) && !text.Contains(TokScope) && ((!text.Contains(TokHolo) && !text.Contains(TokHoloU)) || text.Contains(TokView)))
			{
				return;
			}
			Texture2D bodyTex = _bodyTex;
			if (bodyTex == null)
			{
				return;
			}
			int instanceID2 = r.GetInstanceID();
			if (!force && AppliedRenderers.Contains(instanceID2))
			{
				return;
			}
			Material sharedMaterial;
			try
			{
				sharedMaterial = r.sharedMaterial;
			}
			catch
			{
				return;
			}
			if (sharedMaterial == null)
			{
				return;
			}
			Material material = _bodyMat;
			if (material == null)
			{
				material = (_bodyMat = CreateSwapMaterial(sharedMaterial, bodyTex, glass: false));
			}
			try
			{
				if (ApplyMaterialReplacement(r, sharedMaterial, material))
				{
					AppliedRenderers.Add(instanceID2);
				}
			}
			catch
			{
			}
			try
			{
				if (_mpb == null)
				{
					_mpb = new MaterialPropertyBlock();
				}
				r.GetPropertyBlock(_mpb);
				StampTex(_mpb, bodyTex);
				r.SetPropertyBlock(_mpb);
			}
			catch
			{
			}
		}
	}

	private static bool ApplyMaterialReplacement(Renderer r, Material source, Material swap)
	{
		MaterialReplacement materialReplacement = r.GetComponent<MaterialReplacement>();
		if (materialReplacement == null)
		{
			materialReplacement = r.gameObject.AddComponent<MaterialReplacement>();
		}
		materialReplacement.Init();
		if (materialReplacement.Renderer == null || materialReplacement.Default == null || materialReplacement.Override == null)
		{
			r.sharedMaterial = swap;
			return true;
		}
		bool flag = false;
		for (int i = 0; i < ((Il2CppArrayBase<Material>)materialReplacement.Default).Length; i++)
		{
			Material material = ((Il2CppArrayBase<Material>)materialReplacement.Default)[i];
			if (!(material == null) && (material == source || string.Equals(material.name, source.name, StringComparison.OrdinalIgnoreCase)))
			{
				((Il2CppArrayBase<Material>)materialReplacement.Override)[i] = swap;
				flag = true;
			}
		}
		if (!flag)
		{
			for (int j = 0; j < ((Il2CppArrayBase<Material>)materialReplacement.Override).Length; j++)
			{
				((Il2CppArrayBase<Material>)materialReplacement.Override)[j] = swap;
			}
		}
		materialReplacement.Renderer.sharedMaterials = materialReplacement.Override;
		return true;
	}

	private static void EnsureMaterials(Material sample)
	{
		if (!(sample == null) && _bodyMat == null)
		{
			Texture2D texture2D = LoadBodyTexture();
			if (texture2D != null)
			{
				_bodyMat = CreateSwapMaterial(sample, texture2D, glass: false);
			}
		}
	}

	private static Material CreateSwapMaterial(Material source, Texture2D tex, bool glass)
	{
		Material material = new Material(source.shader);
		material.hideFlags = HideFlags.HideAndDontSave;
		try
		{
			material.CopyPropertiesFromMaterial(source);
		}
		catch
		{
		}
		StampTex(material, tex);
		if (glass)
		{
			try
			{
				if (material.HasProperty(TokEmMap))
				{
					material.SetTexture(TokEmMap, tex);
				}
				material.EnableKeyword(TokEmKw);
				if (material.HasProperty(TokMode))
				{
					material.SetFloat(TokMode, 3f);
					material.SetInt(TokSrc, 5);
					material.SetInt(TokDst, 10);
					material.SetInt(TokZw, 0);
					material.EnableKeyword(TokAlpha);
				}
				if (material.renderQueue >= 3001)
				{
					return material;
				}
				material.renderQueue = 3001;
				return material;
			}
			catch
			{
				return material;
			}
		}
		return material;
	}

	private static void StampTex(Material mat, Texture2D tex)
	{
		if (mat == null || tex == null)
		{
			return;
		}
		for (int i = 0; i < TexProps.Length; i++)
		{
			try
			{
				if (mat.HasProperty(TexProps[i]))
				{
					mat.SetTexture(TexProps[i], tex);
				}
			}
			catch
			{
			}
		}
		try
		{
			mat.mainTexture = tex;
		}
		catch
		{
		}
	}

	private static void StampTex(MaterialPropertyBlock block, Texture2D tex)
	{
		for (int i = 0; i < TexProps.Length; i++)
		{
			try
			{
				block.SetTexture(TexProps[i], tex);
			}
			catch
			{
			}
		}
	}

	private static void LoadEmbeddedTextures()
	{
		if (_bodyTex == null)
		{
			TryLoadEmbeddedTexture(BodyTexResource, TokMat0, out _bodyTex);
		}
		if (_glassTex == null)
		{
			TryLoadEmbeddedTexture(GlassTexResource, TokMat1, out _glassTex);
		}
	}

	private static bool EnsureTextures()
	{
		if (_bodyTex == null || _glassTex == null)
		{
			LoadEmbeddedTextures();
		}
		if (_bodyTex != null)
		{
			return _glassTex != null;
		}
		return false;
	}

	private static Texture2D LoadBodyTexture()
	{
		if (_bodyTex != null)
		{
			return _bodyTex;
		}
		LoadEmbeddedTextures();
		return _bodyTex;
	}

	private static Texture2D LoadGlassTexture()
	{
		if (_glassTex != null)
		{
			return _glassTex;
		}
		LoadEmbeddedTextures();
		return _glassTex;
	}

	private static bool TryLoadEmbeddedTexture(string resource, string name, out Texture2D tex)
	{
		tex = null;
		try
		{
			using Stream stream = typeof(HolosightSwap).Assembly.GetManifestResourceStream(resource);
			if (stream == null)
			{
				return false;
			}
			using MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			byte[] array = memoryStream.ToArray();
			if (array.Length == 0)
			{
				return false;
			}
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
			if (!texture2D.LoadImage(array, markNonReadable: false))
			{
				return false;
			}
			texture2D.wrapMode = TextureWrapMode.Repeat;
			texture2D.filterMode = FilterMode.Bilinear;
			texture2D.name = name;
			texture2D.hideFlags = HideFlags.HideAndDontSave;
			tex = texture2D;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryLoadObjGroup(string groupName, out ObjGroup group)
	{
		group = null;
		if (!TryReadEmbeddedText(ObjResource, out var text))
		{
			return false;
		}
		return new ObjParser(text).TryGetGroup(groupName, out group);
	}

	private static bool TryReadEmbeddedText(string resource, out string text)
	{
		text = null;
		try
		{
			using Stream stream = typeof(HolosightSwap).Assembly.GetManifestResourceStream(resource);
			if (stream == null)
			{
				return false;
			}
			using MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			text = Encoding.UTF8.GetString(memoryStream.ToArray());
			return !string.IsNullOrWhiteSpace(text);
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsBodyRendererName(string lowerName)
	{
		if (string.IsNullOrEmpty(lowerName))
		{
			return false;
		}
		if (lowerName.Contains(TokRet) || lowerName.Contains(TokGeo) || lowerName.Contains(TokDot) || lowerName.Contains(TokCross))
		{
			return false;
		}
		if (lowerName.Contains(TokGl) || lowerName.Contains("center") || lowerName.Contains("point") || lowerName.Contains("geo"))
		{
			return false;
		}
		return true;
	}

	internal static GameObject FindCurrentHolosight()
	{
		try
		{
			BasePlayer entity = LocalPlayer.Entity;
			if (entity == null || !entity.IsConnected)
			{
				return null;
			}
			HeldEntity heldEntity = entity.GetHeldEntity();
			if (heldEntity == null)
			{
				return null;
			}
			Il2CppArrayBase<ViewmodelAttachment> componentsInChildren = heldEntity.GetComponentsInChildren<ViewmodelAttachment>(includeInactive: true);
			if (componentsInChildren == null)
			{
				return null;
			}
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ViewmodelAttachment viewmodelAttachment = componentsInChildren[i];
				if (!(viewmodelAttachment == null))
				{
					GameObject spawnedObject = GetSpawnedObject(viewmodelAttachment);
					if (spawnedObject != null && IsHolosight(spawnedObject, viewmodelAttachment))
					{
						return spawnedObject;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	internal static GameObject FindCurrentLaser()
	{
		try
		{
			BasePlayer entity = LocalPlayer.Entity;
			if (entity == null || !entity.IsConnected)
			{
				return null;
			}
			HeldEntity heldEntity = entity.GetHeldEntity();
			if (heldEntity == null)
			{
				return null;
			}
			Il2CppArrayBase<ViewmodelAttachment> componentsInChildren = heldEntity.GetComponentsInChildren<ViewmodelAttachment>(includeInactive: true);
			if (componentsInChildren == null)
			{
				return null;
			}
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ViewmodelAttachment viewmodelAttachment = componentsInChildren[i];
				if (viewmodelAttachment == null)
				{
					continue;
				}
				GameObject spawnedObject = GetSpawnedObject(viewmodelAttachment);
				if (spawnedObject != null)
				{
					string name = spawnedObject.name;
					if (name != null && name.IndexOf("laser", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return spawnedObject;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}
}
