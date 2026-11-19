using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class TeamMemberElementUpdateStatePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(TeamMemberElement), LocStrings.Meth_UpdateState);
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Prefix(TeamMemberElement __instance, bool isOnline, bool isAlive, bool isWounded)
	{
		try
		{
			TeamPlayerNameColorApplier.PrepareTeamMemberUpdateState(__instance, isOnline, isAlive, isWounded);
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(36, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TeamMemberElementUpdateStatePatch] ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}
}
