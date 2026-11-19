using System;
using System.Collections.Generic;
using System.Reflection;
using CBSafe.Colors;
using CBSafe.Crosshair;
using UnityEngine;

namespace CBSafe.Core.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class PanelOpenWatcher : MonoBehaviour
{
	private static readonly Dictionary<int, Action> _callbacks = new Dictionary<int, Action>();

	public PanelOpenWatcher(IntPtr ptr)
		: base(ptr)
	{
	}

	public static void Attach(GameObject panel, Action onOpen)
	{
		if (!(panel == null))
		{
			int instanceID = panel.GetInstanceID();
			_callbacks[instanceID] = onOpen;
			if (panel.GetComponent<PanelOpenWatcher>() == null)
			{
				panel.AddComponent<PanelOpenWatcher>();
			}
		}
	}

	public static void Detach(GameObject panel)
	{
		if (!(panel == null))
		{
			_callbacks.Remove(panel.GetInstanceID());
		}
	}

	private void OnEnable()
	{
		TryFire();
	}

	private void LateUpdate()
	{
		TryFire();
	}

	private void TryFire()
	{
		try
		{
			if (_callbacks.TryGetValue(base.gameObject.GetInstanceID(), out var value))
			{
				value?.Invoke();
			}
		}
		catch (Exception ex)
		{
			try
			{
				(CBSafeColorsPlugin.Instance?.Log ?? CBSafeCrosshairPlugin.Instance?.Log)?.LogWarning("[PanelOpenWatcher] " + ex.Message);
			}
			catch
			{
			}
		}
	}
}
