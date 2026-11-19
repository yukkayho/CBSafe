using System;
using System.Collections.Generic;
using System.Reflection;
using CBSafe.Colors.Infrastructure;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
public static class DlcUnlock
{
	private static bool? _remote;

	public static bool Active
	{
		get
		{
			bool? remote = _remote;
			if (remote.HasValue)
			{
				return remote.Value;
			}
			return PluginConfig.DlcUnlockEnabled?.Value ?? true;
		}
	}

	public static void ProcessDlcCommand(string commandData)
	{
		try
		{
			if (string.IsNullOrEmpty(commandData))
			{
				return;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			string[] array = commandData.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Split(new char[1] { ':' }, 2);
				if (array2.Length == 2)
				{
					dictionary[array2[0].Trim()] = array2[1].Trim();
				}
			}
			if (dictionary.TryGetValue("status", out var value))
			{
				_remote = value.Equals("True", StringComparison.OrdinalIgnoreCase) || value.Equals("enable", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase) || value == "1";
			}
		}
		catch
		{
		}
	}

	public static void ClearRemoteOverride()
	{
		_remote = null;
	}
}
