using System.Runtime.CompilerServices;

namespace CBSafe.Fps;

internal static class OptDark
{
	internal static bool Night;

	private static bool _seen;

	private static int _hold;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool On(bool flag)
	{
		if (flag)
		{
			return !Night;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool Perf(bool flag)
	{
		return flag;
	}

	internal static void Tick()
	{
		try
		{
			TOD_Sky instance = TOD_Sky.Instance;
			if (!(instance == null) && instance.Initialized)
			{
				bool isNight = instance.IsNight;
				if (!_seen)
				{
					Night = isNight;
					_seen = true;
					_hold = 0;
				}
				else if (isNight == Night)
				{
					_hold = 0;
				}
				else if (++_hold >= 6)
				{
					Night = isNight;
					_hold = 0;
				}
			}
		}
		catch
		{
		}
	}
}
