using System.Collections.Generic;
using CBSafe.Core.Patches;
using UnityEngine;
using UnityEngine.Rendering;

namespace CBSafe.Fps;

internal static class EntityPatches
{
	private static float _camTime;

	private static Vector3 _camPos;

	private static int _fpMode = -1;

	private static PlayerModel _fpModel;

	private static Renderer[] _fpHeadRenderers;

	private static readonly HashSet<int> _ragdollIds = new HashSet<int>();

	private static bool Distant(PlayerModel model)
	{
		if (model == null || model.isLocalPlayer)
		{
			return false;
		}
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime - _camTime > 0.15f)
		{
			Camera main = Camera.main;
			if (main == null)
			{
				return true;
			}
			_camPos = main.transform.position;
			_camTime = unscaledTime;
		}
		Transform transform = model.transform;
		if (transform == null)
		{
			return true;
		}
		float distantAnimRange = OptState.DistantAnimRange;
		return (transform.position - _camPos).sqrMagnitude > distantAnimRange * distantAnimRange;
	}

	public static bool AnimatorIk(PlayerModel __instance)
	{
		if (!OptDark.Perf(OptState.SimplifyDistantAnims))
		{
			return true;
		}
		if (!Distant(__instance))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.DistantAnims);
		return false;
	}

	public static bool FootIk(PlayerModel __instance)
	{
		if (!OptDark.Perf(OptState.SimplifyDistantAnims))
		{
			return true;
		}
		if (!Distant(__instance))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.DistantAnims);
		return false;
	}

	public static bool HeadIk(PlayerModel __instance)
	{
		if (!OptDark.Perf(OptState.SimplifyDistantAnims))
		{
			return true;
		}
		if (!Distant(__instance))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.DistantAnims);
		return false;
	}

	public static bool SpineIk(PlayerModel __instance)
	{
		if (!OptDark.Perf(OptState.SimplifyDistantAnims))
		{
			return true;
		}
		if (!Distant(__instance))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.DistantAnims);
		return false;
	}

	private static bool LocalCloth(ClothLOD lod)
	{
		try
		{
			if (lod == null)
			{
				return false;
			}
			PlayerModel componentInParent = lod.GetComponentInParent<PlayerModel>();
			return componentInParent != null && componentInParent.isLocalPlayer;
		}
		catch
		{
			return false;
		}
	}

	public static bool EnableCloth(ClothLOD __instance)
	{
		if (!OptState.DisableCloth || LocalCloth(__instance))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Cloth);
		return false;
	}

	public static bool ClothLodCheck(ClothLOD __instance)
	{
		if (!OptState.DisableCloth || LocalCloth(__instance))
		{
			return true;
		}
		FeatureWatch.Skip(FeatureId.Cloth);
		try
		{
			__instance.DisableCloth();
		}
		catch
		{
		}
		return false;
	}

	public static bool ClothWind(ClothWindModify __instance)
	{
		if (!OptState.DisableCloth)
		{
			return true;
		}
		try
		{
			PlayerModel playerModel = ((__instance != null) ? __instance.GetComponentInParent<PlayerModel>() : null);
			if (playerModel != null && playerModel.isLocalPlayer)
			{
				return true;
			}
		}
		catch
		{
		}
		FeatureWatch.Skip(FeatureId.Cloth);
		return false;
	}

	public static void FirstPersonCameraPost(PlayerEyes __instance)
	{
		ApplyLocalFpBody(firstPerson: true);
	}

	public static void ThirdPersonCameraPost(PlayerEyes __instance)
	{
		ApplyLocalFpBody(firstPerson: false);
	}

	private static void ApplyLocalFpBody(bool firstPerson)
	{
		int num = (firstPerson ? 1 : 0);
		try
		{
			BasePlayer entity = LocalPlayer.Entity;
			if (entity == null)
			{
				return;
			}
			PlayerModel component = entity.GetComponent<PlayerModel>();
			if (component == null || !component.isLocalPlayer || (_fpMode == num && _fpModel == component))
			{
				return;
			}
			_fpMode = num;
			_fpModel = component;
			try
			{
				if (component.ShouldDoLegs)
				{
					component.drawShadowOnly = firstPerson;
				}
			}
			catch
			{
			}
			ShadowCastingMode shadowCastingMode = ((!firstPerson) ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly);
			CacheHeadRenderers(component);
			if (_fpHeadRenderers == null)
			{
				return;
			}
			for (int i = 0; i < _fpHeadRenderers.Length; i++)
			{
				Renderer renderer = _fpHeadRenderers[i];
				if (renderer != null)
				{
					renderer.shadowCastingMode = shadowCastingMode;
				}
			}
		}
		catch
		{
		}
	}

	private static void CacheHeadRenderers(PlayerModel pm)
	{
		List<Renderer> list = new List<Renderer>(16);
		Collect(pm.headBone, list);
		Collect(pm.neckBone, list);
		Collect(pm.jawBone, list);
		_fpHeadRenderers = list.ToArray();
	}

	private static void Collect(GameObject go, List<Renderer> list)
	{
		if (go == null)
		{
			return;
		}
		Renderer[] array;
		try
		{
			array = go.GetComponentsInChildren<Renderer>(includeInactive: true);
		}
		catch
		{
			return;
		}
		if (array == null)
		{
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] != null)
			{
				list.Add(array[i]);
			}
		}
	}

	private static void SetCast(GameObject go, ShadowCastingMode mode)
	{
		if (go == null)
		{
			return;
		}
		Renderer[] array;
		try
		{
			array = go.GetComponentsInChildren<Renderer>(includeInactive: true);
		}
		catch
		{
			return;
		}
		if (array == null)
		{
			return;
		}
		Renderer[] array2 = array;
		foreach (Renderer renderer in array2)
		{
			if (!(renderer == null))
			{
				renderer.shadowCastingMode = mode;
			}
		}
	}

	public static void CorpsePhysics(Ragdoll __instance, ref bool __0)
	{
		bool flag = false;
		try
		{
			flag = __instance is AnimalRagdoll || __instance is HorseRagdoll;
		}
		catch
		{
		}
		if (flag && OptState.DisableAnimalPhysics)
		{
			FeatureWatch.Skip(FeatureId.Corpse);
			__0 = false;
		}
		else if (!flag && OptState.DisableCorpsePhysics)
		{
			FeatureWatch.Skip(FeatureId.Corpse);
			__0 = false;
		}
		else
		{
			if (!OptState.LimitCorpses || !(__instance != null))
			{
				return;
			}
			int instanceID = __instance.GetInstanceID();
			if (!_ragdollIds.Contains(instanceID))
			{
				if (_ragdollIds.Count >= 6)
				{
					FeatureWatch.Skip(FeatureId.Corpse);
					__0 = false;
				}
				else if (__0)
				{
					_ragdollIds.Add(instanceID);
				}
			}
		}
	}

	public static void RagdollGone(Ragdoll __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			_ragdollIds.Remove(__instance.GetInstanceID());
		}
		catch
		{
		}
	}

	public static bool PushRagdoll(BaseCorpse __instance)
	{
		bool flag = false;
		try
		{
			flag = __instance is HorseCorpse;
		}
		catch
		{
		}
		if (flag && OptState.DisableAnimalPhysics)
		{
			return FeatureWatch.PassOrSkipPerf(FeatureId.Corpse, disable: true);
		}
		if (!flag && OptState.DisableCorpsePhysics)
		{
			return FeatureWatch.PassOrSkipPerf(FeatureId.Corpse, disable: true);
		}
		return true;
	}

	public static void VmNoShadows(BaseViewModel __instance)
	{
		try
		{
			GlovesSkinApplier.OnUpdateRenderers(__instance);
		}
		catch
		{
		}
		if (!OptDark.Night && OptState.DisableVmShadows && !(__instance == null))
		{
			SetCast(__instance.gameObject, ShadowCastingMode.Off);
		}
	}

	public static bool ApplySkin()
	{
		return true;
	}

	public static bool ShakeAdd()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Shake, OptState.DisableShake);
	}

	public static bool ShakeRun()
	{
		return FeatureWatch.PassOrSkipPerf(FeatureId.Shake, OptState.DisableShake);
	}
}
