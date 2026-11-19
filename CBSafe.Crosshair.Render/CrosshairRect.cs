using System.Reflection;

namespace CBSafe.Crosshair.Render;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public struct CrosshairRect
{
	public float X;

	public float Y;

	public float Width;

	public float Height;
}
