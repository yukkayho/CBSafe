using System.Reflection;
using UnityEngine;

namespace CBSafe.Patches;

[HydraNoObfuscate]
[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
internal static class CBSafeVersionReporter
{
	internal const string InfoKey = "cbsafe.version";

	internal const string BoosterVersion = "1.9.18";

	private static bool _wasConnected;

	private static float _nextSendAt;

	private static int _sendAttempts;

	internal static void Tick()
	{
		BasePlayer localPlayer = GetLocalPlayer();
		if (!(localPlayer != null) || !localPlayer.IsConnected)
		{
			if (_wasConnected)
			{
				_sendAttempts = 0;
				_nextSendAt = 0f;
			}
			_wasConnected = false;
			return;
		}
		bool flag = !_wasConnected;
		_wasConnected = true;
		if (_sendAttempts >= 15)
		{
			return;
		}
		if (flag)
		{
			_nextSendAt = 0f;
		}
		if (Time.unscaledTime < _nextSendAt)
		{
			return;
		}
		_nextSendAt = Time.unscaledTime + 2f;
		try
		{
			ConsoleSystem.Run(ConsoleSystem.Option.Client, "global.setinfo cbsafe.version 1.9.18");
			_sendAttempts++;
		}
		catch
		{
		}
	}

	private static BasePlayer GetLocalPlayer()
	{
		try
		{
			return LocalPlayer.Entity;
		}
		catch
		{
			return null;
		}
	}
}
