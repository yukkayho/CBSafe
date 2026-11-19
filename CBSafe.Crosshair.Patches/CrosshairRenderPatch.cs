using System;
using System.Reflection;
using CBSafe.Core;
using CBSafe.Crosshair.Render;
using HarmonyLib;

namespace CBSafe.Crosshair.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HarmonyPatch]
internal static class CrosshairRenderPatch
{
	private static MethodBase TargetMethod()
	{
		try
		{
			Type typeFromHandle = typeof(BasePlayer);
			MethodBase methodBase = AccessTools.Method(typeFromHandle, LocStrings.Meth_ClientUpdate, new Type[0]);
			if (methodBase != null)
			{
				return methodBase;
			}
			methodBase = AccessTools.Method(typeFromHandle, LocStrings.Meth_ClientUpdate);
			if (methodBase != null)
			{
				return methodBase;
			}
		}
		catch (Exception ex)
		{
			LogWarn("typeof(BasePlayer) failed: " + ex.Message);
		}
		string[] array = new string[3]
		{
			LocStrings.Type_BasePlayer,
			LocStrings.Type_BaseCombatEntity,
			LocStrings.Type_BaseEntity
		};
		string[] array2 = new string[3]
		{
			LocStrings.Meth_ClientUpdate,
			LocStrings.Meth_ClientUpdate_Clientside,
			LocStrings.Meth_DoUpdate
		};
		string[] array3 = array;
		for (int i = 0; i < array3.Length; i++)
		{
			Type type = AccessTools.TypeByName(array3[i]);
			if (type == null)
			{
				continue;
			}
			string[] array4 = array2;
			foreach (string name in array4)
			{
				MethodBase methodBase2 = AccessTools.Method(type, name);
				if (methodBase2 != null)
				{
					return methodBase2;
				}
			}
		}
		LogWarn("No suitable target method found");
		return null;
	}

	private static bool Prepare()
	{
		return TargetMethod() != null;
	}

	private static void Postfix()
	{
		try
		{
			CrosshairRenderDriver.Tick();
		}
		catch (Exception ex)
		{
			CrosshairRenderDriver.LogOnce("Postfix error: " + ex.Message);
		}
	}

	private static void LogWarn(string msg)
	{
		try
		{
			CBSafeCrosshairPlugin.Instance?.Log.LogWarning("[CrosshairRenderPatch] " + msg);
		}
		catch
		{
		}
	}
}
