using System;
using System.Reflection;
using BepInEx.Core.Logging.Interpolation;
using BepInEx.Logging;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class TeamRelationshipsPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(RelationshipManager), LocStrings.Meth_CLIENT_RecieveLocalRelationships);
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix()
	{
		try
		{
			RelationshipManager.PlayerRelationships rels = null;
			try
			{
				rels = RelationshipManager.localRelationships;
			}
			catch
			{
			}
			TeamPlayerNameColorApplier.OnEnemyRelationshipsChanged(rels);
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
				BepInExWarningLogInterpolatedStringHandler bepInExWarningLogInterpolatedStringHandler = new BepInExWarningLogInterpolatedStringHandler(25, 1, out isEnabled);
				if (isEnabled)
				{
					bepInExWarningLogInterpolatedStringHandler.AppendLiteral("[TeamRelationshipsPatch] ");
					bepInExWarningLogInterpolatedStringHandler.AppendFormatted(ex.Message);
				}
				log.LogWarning(bepInExWarningLogInterpolatedStringHandler);
			}
		}
	}
}
