using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Rust.Platform.Steam;
using Rust.Workshop;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
public static class Skins
{
	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(LoadingScreen), "UpdateFromServer")]
	internal static class UpdateFromServerPatch
	{
		[HarmonyPostfix]
		internal static void Postfix()
		{
			try
			{
				LoadLocalWorkshopSkinsOnce();
			}
			catch
			{
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(ItemManager), "Initialize")]
	internal static class ItemManagerWorkshopPatch
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			try
			{
				LoadLocalWorkshopSkinsOnce();
			}
			catch
			{
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(ItemBlueprint), "get_NeedsSteamDLC")]
	internal static class SteamDlcPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref bool __result)
		{
			try
			{
				if (!DlcUnlock.Active)
				{
					return true;
				}
				__result = false;
				return false;
			}
			catch
			{
				return true;
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(SteamDLCItem), "IsInstalled")]
	internal static class InstalledPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(ref bool __result)
		{
			try
			{
				if (!DlcUnlock.Active)
				{
					return true;
				}
				__result = true;
				return false;
			}
			catch
			{
				return true;
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(ISteamUGC), "GetItemInstallInfo")]
	internal static class WorkshopGetItemPatch
	{
		[HarmonyPostfix]
		public static void Postfix(PublishedFileId nPublishedFileID, ref ulong punSizeOnDisk, ref string pchFolder, ref uint punTimeStamp, ref bool __result)
		{
			try
			{
				ulong value = nPublishedFileID.Value;
				if (HasLocalWorkshopItem(value))
				{
					pchFolder = GetWorkshopItemPath(value);
					__result = true;
				}
			}
			catch
			{
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(ISteamUGC), "DownloadItem")]
	internal static class WorkshopDownloadItemPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(PublishedFileId nPublishedFileID, bool bHighPriority, ref bool __result)
		{
			try
			{
				if (!HasLocalWorkshopItem(nPublishedFileID.Value))
				{
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
	[HarmonyPatch(typeof(ISteamUGC), "GetItemState")]
	internal static class ItemStatePatch
	{
		[HarmonyPrefix]
		public static bool Prefix(PublishedFileId nPublishedFileID, ref uint __result)
		{
			try
			{
				if (!HasLocalWorkshopItem(nPublishedFileID.Value))
				{
					return true;
				}
				__result = 4u;
				return false;
			}
			catch
			{
			}
			return true;
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(SteamWorkshopContent), "Download")]
	internal static class WorkshopDownloadExItemPatch
	{
		[HarmonyPrefix]
		public static bool Prefix(SteamWorkshopContent __instance, ref bool __result)
		{
			try
			{
				if (__instance == null)
				{
					return true;
				}
				if (!HasLocalWorkshopItem(__instance.WorkshopId))
				{
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
	[HarmonyPatch(typeof(Skin), "LoadIcon")]
	internal static class WorkshopLoadIconPatch
	{
		[HarmonyPrefix]
		public static void Prefix(Skin __instance, ulong workshopId, ref string directory, AssetBundle bundle = null)
		{
			try
			{
				if (HasLocalWorkshopItem(workshopId))
				{
					directory = GetWorkshopItemPath(workshopId);
					ItemIconOverride.EnsureWorkshopIconFile(directory);
				}
			}
			catch
			{
			}
		}
	}

	[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
	[HarmonyPatch(typeof(Skin), "LoadAssets")]
	internal static class WorkshopLoadAssetsPatch
	{
		[HarmonyPrefix]
		public static void Prefix(Skin __instance, ulong workshopId, ref string directory, AssetBundle bundle = null)
		{
			try
			{
				if (HasLocalWorkshopItem(workshopId))
				{
					directory = GetWorkshopItemPath(workshopId);
				}
			}
			catch
			{
			}
		}
	}

	private static readonly HashSet<ulong> LoadedWorkshopItems = new HashSet<ulong>();

	private static string GameRootPath
	{
		get
		{
			string text = Application.dataPath;
			if (text.Length > 16)
			{
				text = text.Remove(text.Length - 16);
			}
			return text;
		}
	}

	private static string WorkshopRootPath => Path.Combine(GameRootPath, "workshop");

	internal static string GetWorkshopItemPath(ulong workshopId)
	{
		return Path.Combine(WorkshopRootPath, workshopId.ToString());
	}

	internal static bool HasLocalWorkshopItem(ulong workshopId)
	{
		return Directory.Exists(GetWorkshopItemPath(workshopId));
	}

	private static void EnsureWorkshopDirectory()
	{
		if (!Directory.Exists(WorkshopRootPath))
		{
			Directory.CreateDirectory(WorkshopRootPath);
		}
	}

	internal static void LoadLocalWorkshopSkinsOnce()
	{
		try
		{
			EnsureWorkshopDirectory();
			string[] directories = Directory.GetDirectories(WorkshopRootPath);
			for (int i = 0; i < directories.Length; i++)
			{
				if (ulong.TryParse(Path.GetFileName(directories[i]), out var result) && !LoadedWorkshopItems.Contains(result))
				{
					try
					{
						Rust.Workshop.WorkshopSkin.LoadFromWorkshop(result);
						LoadedWorkshopItems.Add(result);
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}
}
