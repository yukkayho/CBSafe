using System;
using UnityEngine;

namespace CBSafe.Patches;

internal sealed class MenuFpsForceRunner : MonoBehaviour
{
	public MenuFpsForceRunner(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		MenuFpsForce.TickUnityUncap();
	}

	private void LateUpdate()
	{
		MenuFpsForce.TickUnityUncap();
	}
}
