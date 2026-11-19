using System.Reflection;
using CBSafe.Core;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
public static class CraftStoreUnlock
{
	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class ItemManagerInitPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(ItemManager), LocStrings.Meth_Initialize);
		}

		private static bool Prepare()
		{
			return false;
		}

		[HarmonyPostfix]
		private static void Postfix()
		{
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch]
	internal static class LoadingScreenPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(LoadingScreen), LocStrings.Meth_UpdateFromServer);
		}

		private static bool Prepare()
		{
			return false;
		}

		[HarmonyPostfix]
		private static void Postfix()
		{
		}
	}

	private static bool _dlcUnlocked;

	private static bool _skinIdsBuilt;

	private static bool _fullyApplied;

	private static MethodInfo _hasUnlockedMethod;

	private static MethodInfo _isUnlockedMethod;

	private static bool TryVanillaBlueprintUnlocked(PlayerBlueprints blueprints, ItemDefinition itemDef)
	{
		if (blueprints == null || itemDef == null)
		{
			return false;
		}
		if ((object)_hasUnlockedMethod == null)
		{
			_hasUnlockedMethod = AccessTools.Method(typeof(PlayerBlueprints), LocStrings.Meth_HasUnlocked);
		}
		if (_hasUnlockedMethod != null)
		{
			try
			{
				return (bool)_hasUnlockedMethod.Invoke(blueprints, new object[1] { itemDef });
			}
			catch
			{
			}
		}
		if ((object)_isUnlockedMethod == null)
		{
			_isUnlockedMethod = AccessTools.Method(typeof(PlayerBlueprints), LocStrings.Meth_IsUnlocked);
		}
		if (_isUnlockedMethod != null)
		{
			try
			{
				return (bool)_isUnlockedMethod.Invoke(blueprints, new object[1] { itemDef });
			}
			catch
			{
			}
		}
		return false;
	}

	internal static bool IsWeaponSkinRedirect(ItemDefinition def)
	{
		if (def == null || def.isRedirectOf == null)
		{
			return false;
		}
		return def.isRedirectOf.category == ItemCategory.Weapon;
	}

	internal static bool ShouldUnlockEventCraft(ItemDefinition def)
	{
		if (def == null || IsWeaponSkinRedirect(def))
		{
			return false;
		}
		ItemBlueprint blueprint = def.Blueprint;
		if (blueprint == null || !blueprint.userCraftable)
		{
			return false;
		}
		if (blueprint.NeedsSteamItem)
		{
			return true;
		}
		if (def.steamItem != null)
		{
			return true;
		}
		return CraftStoreUnlockState.UnlockItemIds.Contains(def.itemid);
	}

	internal static bool ShouldBypassSteamInventory(int itemId)
	{
		if (itemId == 0)
		{
			return false;
		}
		if (IsWeaponRedirectSkinInventoryId(itemId))
		{
			return false;
		}
		if (!CraftStoreUnlockState.SteamBypassIds.Contains(itemId))
		{
			return CraftStoreUnlockState.SkinInventoryIds.Contains(itemId);
		}
		return true;
	}

	internal static bool IsWeaponRedirectSkin(ItemSkin skin)
	{
		if (((skin != null) ? skin.Redirect : null) == null)
		{
			return false;
		}
		return IsWeaponSkinRedirect(skin.Redirect);
	}

	internal static bool IsBaseWeaponBlueprintUnlocked(ItemDefinition baseWeaponDef)
	{
		if (baseWeaponDef == null)
		{
			return false;
		}
		BasePlayer entity = LocalPlayer.Entity;
		PlayerBlueprints playerBlueprints = ((entity != null) ? entity.blueprints : null);
		if (playerBlueprints == null)
		{
			return false;
		}
		return TryVanillaBlueprintUnlocked(playerBlueprints, baseWeaponDef);
	}

	internal static bool IsWeaponRedirectSkinInventoryId(int skinInventoryId)
	{
		if (skinInventoryId == 0)
		{
			return false;
		}
		try
		{
			ItemSkinDirectory.Skin skin = ItemSkinDirectory.FindByInventoryDefinitionId(skinInventoryId);
			SteamInventoryItem invItem = skin.invItem;
			ItemSkin itemSkin = (ItemSkin)((invItem is ItemSkin) ? invItem : null);
			if (itemSkin != null && IsWeaponRedirectSkin(itemSkin))
			{
				return true;
			}
			if (skin.itemid != 0 && IsWeaponSkinRedirect(ItemManager.FindItemDefinition(skin.itemid)))
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public static void Apply()
	{
	}

	private static void UnlockDlcItems()
	{
	}

	private static void UnlockStoreItems()
	{
	}

	private static void BuildSkinInventoryIds()
	{
		CraftStoreUnlockState.SkinInventoryIds.Clear();
		ItemSkinDirectory instance = ItemSkinDirectory.Instance;
		if (((instance != null) ? instance.skins : null) != null)
		{
			ItemSkinDirectory.Skin[] array = instance.skins;
			for (int i = 0; i < array.Length; i++)
			{
				int id = array[i].id;
				if (id != 0)
				{
					CraftStoreUnlockState.SkinInventoryIds.Add(id);
				}
			}
		}
		List<ItemDefinition> itemList = ItemManager.itemList;
		if (itemList != null && itemList.Count > 0)
		{
			for (int j = 0; j < itemList.Count; j++)
			{
				ItemDefinition itemDefinition = itemList[j];
				if (itemDefinition == null)
				{
					continue;
				}
				ItemSkinDirectory.Skin[] array2 = itemDefinition.skins;
				if (array2 != null)
				{
					for (int k = 0; k < array2.Length; k++)
					{
						int id2 = array2[k].id;
						if (id2 != 0)
						{
							CraftStoreUnlockState.SkinInventoryIds.Add(id2);
						}
					}
				}
				IPlayerItemDefinition[] array3 = itemDefinition.skins2;
				if (array3 == null)
				{
					continue;
				}
				IPlayerItemDefinition[] array4 = array3;
				foreach (IPlayerItemDefinition playerItemDefinition in array4)
				{
					if (playerItemDefinition != null && playerItemDefinition.DefinitionId != 0)
					{
						CraftStoreUnlockState.SkinInventoryIds.Add(playerItemDefinition.DefinitionId);
					}
				}
			}
		}
		UnderwearManifest underwearManifest = UnderwearManifest.Get();
		if (!(underwearManifest != null) || underwearManifest.underwears == null)
		{
			return;
		}
		for (int m = 0; m < underwearManifest.underwears.Count; m++)
		{
			Underwear underwear = underwearManifest.underwears[m];
			if (!(underwear == null))
			{
				if (underwear.steamItem != null && underwear.steamItem.id != 0)
				{
					CraftStoreUnlockState.SkinInventoryIds.Add(underwear.steamItem.id);
				}
				if (underwear.steamDLC != null)
				{
					underwear.steamDLC.bypassLicenseCheck = true;
				}
			}
		}
	}

	private static void UnlockCraftSkins()
	{
		FieldInfo field = typeof(ItemSkin).GetField("UnlockedByDefault", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (field == null || field.FieldType != typeof(bool))
		{
			return;
		}
		ItemSkin[] array = Resources.FindObjectsOfTypeAll<ItemSkin>();
		if (array == null)
		{
			return;
		}
		ItemSkin[] array2 = array;
		foreach (ItemSkin itemSkin in array2)
		{
			if (!(itemSkin == null) && !IsWeaponRedirectSkin(itemSkin))
			{
				try
				{
					field.SetValue(itemSkin, true);
				}
				catch
				{
				}
			}
		}
		ItemSkinDirectory instance = ItemSkinDirectory.Instance;
		if (((instance != null) ? instance.skins : null) == null)
		{
			return;
		}
		ItemSkinDirectory.Skin[] array3 = instance.skins;
		for (int j = 0; j < array3.Length; j++)
		{
			SteamInventoryItem invItem = array3[j].invItem;
			ItemSkin itemSkin2 = (ItemSkin)((invItem is ItemSkin) ? invItem : null);
			if (itemSkin2 != null && !IsWeaponRedirectSkin(itemSkin2))
			{
				try
				{
					field.SetValue(itemSkin2, true);
				}
				catch
				{
				}
			}
		}
	}
}
