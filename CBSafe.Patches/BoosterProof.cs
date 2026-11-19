using System;
using System.Security.Cryptography;
using System.Text;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;

namespace CBSafe.Patches;

internal static class BoosterProof
{
	private const string FullName = "booster.challenge";

	private const string Secret = "CBSafe-BoosterGate-1-9f3c7a1e";

	private const string Protocol = "1";

	private const string Prefix = "CBSafeGate|1|";

	private const string ReplyPrefix = "booster.proof 1 ";

	private static ConsoleSystem.Command _cmd;

	private static bool _answered;

	internal static bool Answered => _answered;

	internal static void Register()
	{
		try
		{
			Dictionary<string, ConsoleSystem.Command> dict = ConsoleSystem.Index.Client.Dict;
			if (dict != null && !dict.ContainsKey("booster.challenge"))
			{
				if (_cmd == null)
				{
					_cmd = Build();
				}
				dict.Add("booster.challenge", _cmd);
			}
		}
		catch
		{
		}
	}

	private static ConsoleSystem.Command Build()
	{
		return new ConsoleSystem.Command
		{
			Name = "challenge",
			Parent = "booster",
			FullName = "booster.challenge",
			AllowRunFromServer = true,
			Client = true,
			ClientInfo = true,
			Variable = false,
			Description = "CBSafe server gate challenge",
			Arguments = "<nonce>",
			Call = (Action<ConsoleSystem.Arg>)Handle
		};
	}

	private static void Handle(ConsoleSystem.Arg arg)
	{
		try
		{
			Il2CppStringArray args = arg.Args;
			if (args == null || args.Length == 0)
			{
				return;
			}
			string text = args[0];
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = Sign(text);
				if (!string.IsNullOrEmpty(text2) && ConsoleSystem.SendToServer("booster.proof 1 " + text2))
				{
					_answered = true;
				}
			}
		}
		catch
		{
		}
	}

	private static string Sign(string nonce)
	{
		try
		{
			using HMACSHA256 hMACSHA = new HMACSHA256(Encoding.UTF8.GetBytes("CBSafe-BoosterGate-1-9f3c7a1e"));
			byte[] array = hMACSHA.ComputeHash(Encoding.UTF8.GetBytes("CBSafeGate|1|" + nonce));
			StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
			for (int i = 0; i < array.Length; i++)
			{
				stringBuilder.Append(array[i].ToString("x2"));
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return null;
		}
	}
}
