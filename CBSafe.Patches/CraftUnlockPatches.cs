using System.Reflection;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
public static class CraftUnlockPatches
{
	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class EventItemHasUnlockedPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(PlayerBlueprints), LocStrings.Meth_HasUnlocked);
		}

		private static bool Prepare()
		{
			return false;
		}

		[HarmonyPrefix]
		private static bool Prefix(ItemDefinition targetItem, ref bool __result)
		{
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class IsUnlockedPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(PlayerBlueprints), LocStrings.Meth_IsUnlocked);
		}

		private static bool Prepare()
		{
			return false;
		}

		[HarmonyPrefix]
		private static bool Prefix(ItemDefinition itemDef, ref bool __result)
		{
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class CanCraftPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(PlayerBlueprints), LocStrings.Meth_CanCraft);
		}

		private static bool Prepare()
		{
			return false;
		}

		[HarmonyPrefix]
		private static bool Prefix(int itemid, int skinItemId, ulong playerId, ref bool __result)
		{
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class CheckSkinOwnershipPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(PlayerBlueprints), LocStrings.Meth_CheckSkinOwnership);
		}

		private static bool Prepare()
		{
			return false;
		}

		[HarmonyPrefix]
		private static bool Prefix(int skinItemId, ulong playerId, ref bool __result)
		{
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class SteamInventoryHasItemPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(SteamInventory), LocStrings.Meth_HasItem);
		}

		private static bool Prepare()
		{
			return false;
		}

		[HarmonyPrefix]
		private static bool Prefix(int itemid, ref bool __result)
		{
			return true;
		}
	}

	private static bool IsUnlockedItem(ItemDefinition def)
	{
		if (def == null)
		{
			return false;
		}
		if (!CraftStoreUnlockState.UnlockItemIds.Contains(def.itemid))
		{
			return CraftStoreUnlock.ShouldUnlockEventCraft(def);
		}
		return true;
	}
}
