using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using CBSafe.Core;
using HarmonyLib;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
public static class CraftSkinPatches
{
	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(ConsoleSystem), "SendToServer")]
	internal static class CraftAddSkinPatch
	{
		[HarmonyPrefix]
		private static void Prefix(ref string command)
		{
			try
			{
				if (string.IsNullOrEmpty(command) || !command.StartsWith("craft.add ", StringComparison.Ordinal))
				{
					return;
				}
				string[] array = command.Split(' ');
				if (array.Length < 3 || !int.TryParse(array[1], out var result) || result == 0 || !int.TryParse(array[2], out var result2) || result2 < 1)
				{
					return;
				}
				if (array.Length >= 4 && int.TryParse(array[3], out var result3) && result3 != 0)
				{
					CraftSkinHelper.RememberSelectedSkin(result, result3);
					return;
				}
				int selectedSkin = CraftSkinHelper.GetSelectedSkin(result);
				if (selectedSkin != 0)
				{
					if (array.Length == 3)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 3);
						defaultInterpolatedStringHandler.AppendLiteral("craft.add ");
						defaultInterpolatedStringHandler.AppendFormatted(result);
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted(result2);
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted(selectedSkin);
						command = defaultInterpolatedStringHandler.ToStringAndClear();
					}
					else if (array.Length == 4 && (array[3] == "true" || array[3] == "false"))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(13, 4);
						defaultInterpolatedStringHandler.AppendLiteral("craft.add ");
						defaultInterpolatedStringHandler.AppendFormatted(result);
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted(result2);
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted(selectedSkin);
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted(array[3]);
						command = defaultInterpolatedStringHandler.ToStringAndClear();
					}
				}
			}
			catch
			{
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class ItemSkinHasUnlockedPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(ItemSkin), LocStrings.Meth_HasUnlocked);
		}

		private static bool Prepare()
		{
			return TargetMethod() != null;
		}

		[HarmonyPrefix]
		private static bool Prefix(ItemSkin __instance, ref bool __result)
		{
			try
			{
				if (__instance == null)
				{
					return true;
				}
				if (CraftStoreUnlock.IsWeaponRedirectSkin(__instance))
				{
					ItemDefinition redirect = __instance.Redirect;
					if (((redirect != null) ? redirect.isRedirectOf : null) != null)
					{
						__result = CraftStoreUnlock.IsBaseWeaponBlueprintUnlocked(__instance.Redirect.isRedirectOf);
						return false;
					}
					return true;
				}
				__result = true;
				return false;
			}
			catch
			{
			}
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class SteamInventoryItemHasUnlockedPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(SteamInventoryItem), LocStrings.Meth_HasUnlocked);
		}

		private static bool Prepare()
		{
			return TargetMethod() != null;
		}

		[HarmonyPrefix]
		private static bool Prefix(SteamInventoryItem __instance, ref bool __result)
		{
			try
			{
				if (__instance == null)
				{
					return true;
				}
				if (DlcUnlock.Active)
				{
					__result = true;
					return false;
				}
				ItemSkin itemSkin = (ItemSkin)((__instance is ItemSkin) ? __instance : null);
				if (itemSkin != null && CraftStoreUnlock.IsWeaponRedirectSkin(itemSkin))
				{
					ItemDefinition redirect = itemSkin.Redirect;
					if (((redirect != null) ? redirect.isRedirectOf : null) != null)
					{
						__result = CraftStoreUnlock.IsBaseWeaponBlueprintUnlocked(itemSkin.Redirect.isRedirectOf);
						return false;
					}
					return true;
				}
				if (__instance.id != 0 && CraftStoreUnlock.ShouldBypassSteamInventory(__instance.id))
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
