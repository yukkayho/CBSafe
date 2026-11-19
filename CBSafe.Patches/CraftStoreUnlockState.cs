using System.Collections.Generic;
using System.Reflection;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
internal static class CraftStoreUnlockState
{
	internal static readonly Dictionary<ItemDefinition, SteamInventoryItem> BackupSteamItems = new Dictionary<ItemDefinition, SteamInventoryItem>();

	internal static readonly HashSet<int> UnlockItemIds = new HashSet<int>();

	internal static readonly HashSet<int> SteamBypassIds = new HashSet<int>();

	internal static readonly HashSet<int> SkinInventoryIds = new HashSet<int>();

	internal static bool Applied;

	internal static bool SkinsApplied;
}
