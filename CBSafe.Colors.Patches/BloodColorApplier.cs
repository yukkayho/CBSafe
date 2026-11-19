using System;
using System.Reflection;
using CBSafe.Colors.Infrastructure;
using UnityEngine;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HydraNoObfuscate]
public sealed class BloodColorApplier : MonoBehaviour
{
	public BloodColorApplier(IntPtr ptr)
		: base(ptr)
	{
	}

	public void ForceReapplyNow()
	{
		try
		{
			UpdateAllBlood();
		}
		catch
		{
		}
	}

	public static void UpdateAllBlood()
	{
		if (!PluginConfig.TryResolveBloodColor(out var color))
		{
			return;
		}
		ParticleSystem[] array = null;
		try
		{
			array = Resources.FindObjectsOfTypeAll<ParticleSystem>();
		}
		catch
		{
			return;
		}
		if (array == null)
		{
			return;
		}
		ParticleSystem[] array2 = array;
		foreach (ParticleSystem particleSystem in array2)
		{
			if (particleSystem == null)
			{
				continue;
			}
			GameObject gameObject = null;
			try
			{
				gameObject = particleSystem.gameObject;
			}
			catch
			{
				continue;
			}
			if (gameObject == null)
			{
				continue;
			}
			string text = null;
			try
			{
				text = gameObject.name;
			}
			catch
			{
				continue;
			}
			if (text == null)
			{
				continue;
			}
			text = text.ToLowerInvariant();
			string text2 = "";
			try
			{
				Transform parent = particleSystem.transform.parent;
				if (parent != null)
				{
					text2 = parent.name.ToLowerInvariant();
				}
			}
			catch
			{
			}
			if (IsBloodName(text) || IsBloodName(text2))
			{
				ApplyColorToParticle(particleSystem, color);
			}
		}
	}

	public static void ApplyColorToParticle(ParticleSystem ps, Color targetColor)
	{
		if (ps == null)
		{
			return;
		}
		try
		{
			ps.main.startColor = new ParticleSystem.MinMaxGradient(targetColor);
		}
		catch
		{
		}
		try
		{
			ParticleSystemRenderer component = ps.GetComponent<ParticleSystemRenderer>();
			if (component != null)
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				component.GetPropertyBlock(materialPropertyBlock);
				materialPropertyBlock.SetColor("_Color", targetColor);
				materialPropertyBlock.SetColor("_TintColor", targetColor);
				materialPropertyBlock.SetColor("_BaseColor", targetColor);
				materialPropertyBlock.SetColor("_EmissionColor", targetColor);
				component.SetPropertyBlock(materialPropertyBlock);
			}
		}
		catch
		{
		}
	}

	public static void ProcessClone(UnityEngine.Object cloneObj)
	{
		if (cloneObj == null)
		{
			return;
		}
		bool flag = false;
		try
		{
			flag = PluginConfig.BloodColorEnabled != null && PluginConfig.BloodColorEnabled.Value;
		}
		catch
		{
			return;
		}
		if (!flag)
		{
			return;
		}
		GameObject gameObject = null;
		try
		{
			gameObject = cloneObj.TryCast<GameObject>();
		}
		catch
		{
			return;
		}
		if (gameObject == null)
		{
			return;
		}
		string text = null;
		try
		{
			text = gameObject.name;
		}
		catch
		{
			return;
		}
		if (text == null)
		{
			return;
		}
		text = text.ToLowerInvariant();
		if (!IsBloodName(text) || !PluginConfig.TryResolveBloodColor(out var color))
		{
			return;
		}
		ParticleSystem[] array = null;
		try
		{
			array = gameObject.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		}
		catch
		{
			return;
		}
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				ApplyColorToParticle(array[i], color);
			}
		}
	}

	private static bool IsBloodName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (!name.Contains("blood") && !name.Contains("flesh"))
		{
			return name.Contains("gore");
		}
		return true;
	}
}
