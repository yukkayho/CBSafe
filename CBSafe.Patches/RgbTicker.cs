using UnityEngine;

namespace CBSafe.Patches;

internal class RgbTicker : MonoBehaviour
{
	private void Update()
	{
		try
		{
			GlassAk.TickRgb();
		}
		catch
		{
		}
	}
}
