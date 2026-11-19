using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;

namespace CBSafe.Patches;

internal static class DlcUnlockCommand
{
	private const string FullName = "dlccheck.receive";

	private static ConsoleSystem.Command _cmd;

	internal static void Register()
	{
		try
		{
			Dictionary<string, ConsoleSystem.Command> dict = ConsoleSystem.Index.Client.Dict;
			if (dict != null && !dict.ContainsKey("dlccheck.receive"))
			{
				if (_cmd == null)
				{
					_cmd = Build();
				}
				dict.Add("dlccheck.receive", _cmd);
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
			Name = "receive",
			Parent = "dlccheck",
			FullName = "dlccheck.receive",
			AllowRunFromServer = true,
			Client = true,
			ClientInfo = true,
			Variable = false,
			Description = "DLC unlock toggle",
			Arguments = "status:True|False[,type:...]",
			Call = (Action<ConsoleSystem.Arg>)Handle
		};
	}

	private static void Handle(ConsoleSystem.Arg arg)
	{
		try
		{
			string text = null;
			Il2CppStringArray args = arg.Args;
			if (args != null && args.Length > 0)
			{
				text = args[0];
				for (int i = 1; i < args.Length; i++)
				{
					text = text + "," + args[i];
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				text = arg.FullString;
			}
			DlcUnlock.ProcessDlcCommand(text);
		}
		catch
		{
		}
	}
}
