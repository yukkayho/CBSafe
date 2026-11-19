using System.Reflection;
using UnityEngine;

namespace CBSafe.Colors.Infrastructure;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
internal static class ReticleColorSession
{
	private static bool _valid;

	private static int _idx;

	private static bool _customized;

	private static Color _rgb;

	public static bool IsValid => _valid;

	public static int Index => _idx;

	public static bool IsActive
	{
		get
		{
			if (_valid)
			{
				if (!_customized)
				{
					return _idx != 0;
				}
				return true;
			}
			return false;
		}
	}

	public static void Remember(int idx, bool customized, Color rgb)
	{
		_valid = true;
		_idx = idx;
		_customized = customized;
		_rgb = rgb;
	}

	public static void Clear()
	{
		_valid = false;
	}

	public static bool TryGetResolved(out Color color, out bool active)
	{
		color = default(Color);
		active = false;
		if (!_valid)
		{
			return false;
		}
		active = _customized || _idx != 0;
		if (!active)
		{
			return false;
		}
		color = _rgb;
		return true;
	}
}
