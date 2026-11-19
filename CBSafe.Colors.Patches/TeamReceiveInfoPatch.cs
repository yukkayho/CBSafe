using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class TeamReceiveInfoPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(BasePlayer), LocStrings.Meth_CLIENT_ReceiveTeamInfo);
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix()
	{
		try
		{
			TeamPlayerNameColorApplier.OnClientTeamPacketReceived();
		}
		catch (Exception ex)
		{
			CBSafeColorsPlugin instance = CBSafeColorsPlugin.Instance;
			if (instance == null)
			{
				return;
			}
			ManualLogSource log = instance.Log;
			if (log != null)
			{
				bool isEnabled = false;
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(23, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TeamReceiveInfoPatch] ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}
}
