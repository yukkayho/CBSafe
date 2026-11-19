using System;
using System.Reflection;
using UnityEngine;

namespace CBSafe.Crosshair.Render;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class ScreenCrosshairRenderer : MonoBehaviour
{
	public ScreenCrosshairRenderer(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		CrosshairRenderDriver.Tick();
	}
}
