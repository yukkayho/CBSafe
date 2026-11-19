using System;

namespace CBSafe.Fps;

public sealed class FeatureInfo
{
	public FeatureId Id;

	public string Name = "";

	public int HooksOk;

	public int HooksTotal;

	public string Error = "";

	public int Calls;

	public int Skips;

	public string Runtime = "—";

	public Func<bool> IsOn = () => false;
}
