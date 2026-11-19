using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
internal static class BarricadeDualGuidePreview
{
	internal const bool Enabled = true;

	private const string TargetShortname = "barricade.wood.cover";

	private const string BarricadePrefabToken = "barricade.cover.wood";

	private const float SideOffset = 0.65f;

	private static GameObject? _mirrorGuide;

	private static GameObject? _trackedPrimary;

	private static readonly Type PatchType = typeof(BarricadeDualGuidePreview);

	internal static void Install(Harmony harmony)
	{
		PatchPostfix(harmony, typeof(Planner), "UpdateGuide", Type.EmptyTypes, "AfterUpdateGuide");
		PatchPostfix(harmony, typeof(Planner), "OnHolstered", Type.EmptyTypes, "OnHolstered");
		PatchPostfix(harmony, typeof(Planner), "CloseGuide", Type.EmptyTypes, "OnCloseGuide");
		PatchPostfix(harmony, typeof(Planner), "SwitchConstruction", new Type[1] { typeof(Construction) }, "OnSwitchConstruction");
		Type type = AccessTools.Inner(typeof(Planner), "Guide");
		if (!(type == null))
		{
			PatchPostfix(harmony, type, "UpdateComponent", new Type[1] { typeof(Construction) }, "OnGuideUpdateComponent");
			PatchPostfix(harmony, type, "BecomeValid", Type.EmptyTypes, "AfterGuideState");
			PatchPostfix(harmony, type, "BecomeInvalid", new Type[1] { typeof(bool) }, "AfterGuideStateInvalid");
			PatchPostfix(harmony, type, "BecomeNeutral", Type.EmptyTypes, "AfterGuideState");
			PatchPostfix(harmony, type, "Shutdown", Type.EmptyTypes, "OnGuideShutdown");
		}
	}

	private static void PatchPostfix(Harmony harmony, Type type, string methodName, Type[] args, string postfixName)
	{
		MethodInfo methodInfo = ((args.Length == 0) ? AccessTools.Method(type, methodName) : AccessTools.Method(type, methodName, args));
		MethodInfo methodInfo2 = AccessTools.Method(PatchType, postfixName);
		if (!(methodInfo == null) && !(methodInfo2 == null))
		{
			harmony.Patch(methodInfo, null, new HarmonyMethod(methodInfo2));
		}
	}

	private static bool IsTarget(Planner? planner)
	{
		if (planner == null)
		{
			return false;
		}
		ItemDefinition def = null;
		try
		{
			def = planner!.GetOwnerItemDefinition();
		}
		catch
		{
		}
		if (IsTargetItem(def))
		{
			return true;
		}
		Construction construction = null;
		try
		{
			construction = planner!.currentConstruction;
		}
		catch
		{
		}
		return IsBarricadeConstruction(construction);
	}

	private static bool IsTargetItem(ItemDefinition? def)
	{
		if (def == null)
		{
			return false;
		}
		if (def!.shortname == "barricade.wood.cover")
		{
			return true;
		}
		try
		{
			string english = def!.displayName.english;
			if (!string.IsNullOrEmpty(english) && english.IndexOf("Barricade Cover", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			string translated = def!.displayName.translated;
			if (!string.IsNullOrEmpty(translated) && translated.IndexOf("укрытие", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsBarricadeGuide(Planner.Guide? guide)
	{
		if (guide == null)
		{
			return false;
		}
		Construction construction = null;
		try
		{
			construction = guide!.component;
		}
		catch
		{
		}
		return IsBarricadeConstruction(construction);
	}

	private static bool IsBarricadeConstruction(Construction? construction)
	{
		if (construction == null)
		{
			return false;
		}
		try
		{
			Deployable deployable = construction!.deployable;
			if (deployable != null && (deployable.fullName ?? string.Empty).IndexOf("barricade.cover.wood", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if ((construction!.fullName ?? string.Empty).IndexOf("barricade.cover.wood", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsAlive(UnityEngine.Object? obj)
	{
		if (obj == null)
		{
			return false;
		}
		try
		{
			return obj;
		}
		catch
		{
			return false;
		}
	}

	private static void AfterUpdateGuide(Planner __instance)
	{
		try
		{
			if (!IsTarget(__instance))
			{
				CleanupMirror();
				return;
			}
			Planner.Guide guide = null;
			try
			{
				guide = __instance.guide;
			}
			catch
			{
				return;
			}
			if (guide == null || !IsBarricadeGuide(guide))
			{
				CleanupMirror();
				return;
			}
			GameObject gameObject = null;
			try
			{
				gameObject = guide.guideObject;
			}
			catch
			{
				return;
			}
			if (!IsAlive(gameObject))
			{
				CleanupMirror();
			}
			else
			{
				ApplyDualLayout(gameObject);
			}
		}
		catch
		{
		}
	}

	private static void AfterGuideState(object __instance)
	{
		try
		{
			SyncFromGuideInstance(__instance);
		}
		catch
		{
		}
	}

	private static void AfterGuideStateInvalid(object __instance, bool force)
	{
		try
		{
			SyncFromGuideInstance(__instance);
		}
		catch
		{
		}
	}

	private static void SyncFromGuideInstance(object guideInstance)
	{
		Planner.Guide guide = (Planner.Guide)((guideInstance is Planner.Guide) ? guideInstance : null);
		if (guide == null)
		{
			return;
		}
		if (!IsBarricadeGuide(guide))
		{
			CleanupMirror();
			return;
		}
		GameObject gameObject = null;
		try
		{
			gameObject = guide.guideObject;
		}
		catch
		{
			return;
		}
		if (!IsAlive(gameObject))
		{
			CleanupMirror();
			return;
		}
		EnsureMirror(gameObject);
		SyncMirrorAppearance(gameObject);
	}

	private static void ApplyDualLayout(GameObject primary)
	{
		EnsureMirror(primary);
		if (IsAlive(_mirrorGuide))
		{
			Vector3 position = primary.transform.position;
			Quaternion rotation = primary.transform.rotation;
			Vector3 lossyScale = primary.transform.lossyScale;
			primary.transform.position = position + rotation * new Vector3(-0.65f, 0f, 0f);
			_mirrorGuide!.transform.SetPositionAndRotation(position + rotation * new Vector3(0.65f, 0f, 0f), rotation);
			_mirrorGuide!.transform.localScale = lossyScale;
			_mirrorGuide!.SetActive(primary.activeSelf);
			SyncMirrorAppearance(primary);
		}
	}

	private static void EnsureMirror(GameObject primary)
	{
		if (!IsAlive(_mirrorGuide) || !(_trackedPrimary == primary))
		{
			CleanupMirror();
			if (IsAlive(primary))
			{
				_mirrorGuide = UnityEngine.Object.Instantiate(primary, primary.transform.parent);
				_mirrorGuide!.name = "Deployable Guide (dual)";
				_mirrorGuide!.SetActive(primary.activeSelf);
				_trackedPrimary = primary;
			}
		}
	}

	private static void SyncMirrorAppearance(GameObject primary)
	{
		if (IsAlive(primary) && IsAlive(_mirrorGuide))
		{
			MeshRenderer meshRenderer = null;
			MeshRenderer meshRenderer2 = null;
			try
			{
				meshRenderer = primary.GetComponent<MeshRenderer>();
			}
			catch
			{
			}
			try
			{
				meshRenderer2 = _mirrorGuide!.GetComponent<MeshRenderer>();
			}
			catch
			{
			}
			if (meshRenderer != null && meshRenderer2 != null && meshRenderer.sharedMaterial != null)
			{
				meshRenderer2.sharedMaterial = meshRenderer.sharedMaterial;
			}
			_mirrorGuide!.SetActive(primary.activeSelf);
		}
	}

	private static void OnHolstered(Planner __instance)
	{
		CleanupMirror();
	}

	private static void OnCloseGuide(Planner __instance)
	{
		CleanupMirror();
	}

	private static void OnSwitchConstruction(Planner __instance, Construction c)
	{
		if (!IsBarricadeConstruction(c))
		{
			CleanupMirror();
		}
	}

	private static void OnGuideUpdateComponent(object __instance, Construction c)
	{
		if (!IsBarricadeConstruction(c))
		{
			CleanupMirror();
		}
	}

	private static void OnGuideShutdown(object __instance)
	{
		CleanupMirror();
	}

	private static void CleanupMirror()
	{
		if (IsAlive(_mirrorGuide))
		{
			UnityEngine.Object.Destroy(_mirrorGuide);
		}
		_mirrorGuide = null;
		_trackedPrimary = null;
	}
}
