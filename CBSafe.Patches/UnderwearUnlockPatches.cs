using System.Reflection;
using HarmonyLib;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
public static class UnderwearUnlockPatches
{
	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class UnderwearValidatePatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(Underwear), "Validate");
		}

		private static bool Prepare()
		{
			return TargetMethod() != null;
		}

		[HarmonyPrefix]
		private static bool Prefix(Underwear __instance, BasePlayer player, ref bool __result)
		{
			try
			{
				if (__instance == null)
				{
					return true;
				}
				if (!__instance.adminOnly)
				{
					__result = true;
					return false;
				}
				if (__instance.steamItem != null && CraftStoreUnlockState.SkinInventoryIds.Contains(__instance.steamItem.id))
				{
					__result = true;
					return false;
				}
				if (__instance.steamDLC != null && __instance.steamDLC.bypassLicenseCheck)
				{
					__result = true;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}
	}
}
