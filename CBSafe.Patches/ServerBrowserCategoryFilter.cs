using System;
using System.Collections.Generic;
using CBSafe.Core;
using Rust.UI;
using TMPro;
using UnityEngine.UI;

namespace CBSafe.Patches;

internal static class ServerBrowserCategoryFilter
{
	internal static void Apply(ServerBrowser browser)
	{
		if (browser == null)
		{
			return;
		}
		ServerBrowserList[] array = browser.GetComponentsInChildren<ServerBrowserList>(includeInactive: true);
		if (array == null || array.Length == 0)
		{
			return;
		}
		List<ServerBrowserList> list = new List<ServerBrowserList>();
		List<ServerBrowserList> list2 = new List<ServerBrowserList>();
		ServerBrowserList modifiedList = null;
		ServerBrowserList serverBrowserList = null;
		ServerBrowserList[] array2 = array;
		foreach (ServerBrowserList serverBrowserList2 in array2)
		{
			if (serverBrowserList2 == null)
			{
				continue;
			}
			try
			{
				serverBrowserList2.showEmpty = true;
			}
			catch
			{
			}
			if (serverBrowserList2.queryType == ServerBrowserList.QueryType.History)
			{
				serverBrowserList = serverBrowserList2;
				continue;
			}
			if (ShouldHideCategory(serverBrowserList2))
			{
				list.Add(serverBrowserList2);
				continue;
			}
			if (IsModifiedCategory(serverBrowserList2))
			{
				modifiedList = serverBrowserList2;
			}
			if (IsAmbiguousInternetList(serverBrowserList2))
			{
				list2.Add(serverBrowserList2);
			}
		}
		ResolveAmbiguousInternetLists(list2, ref modifiedList, list);
		foreach (ServerBrowserList item in list)
		{
			ManifestPatch.HideCategory(item);
		}
		TryActivateCategory(modifiedList ?? serverBrowserList);
	}

	private static void ResolveAmbiguousInternetLists(List<ServerBrowserList> internetCandidates, ref ServerBrowserList modifiedList, List<ServerBrowserList> toHide)
	{
		if (internetCandidates.Count == 0)
		{
			return;
		}
		ServerBrowserList serverBrowserList = modifiedList;
		if (serverBrowserList == null || !internetCandidates.Contains(serverBrowserList))
		{
			serverBrowserList = null;
			foreach (ServerBrowserList internetCandidate in internetCandidates)
			{
				if (HasModdedRules(internetCandidate))
				{
					serverBrowserList = internetCandidate;
					break;
				}
			}
			if (serverBrowserList == null)
			{
				serverBrowserList = internetCandidates[internetCandidates.Count - 1];
			}
		}
		modifiedList = serverBrowserList;
		foreach (ServerBrowserList internetCandidate2 in internetCandidates)
		{
			if (!(internetCandidate2 == serverBrowserList))
			{
				toHide.Add(internetCandidate2);
			}
		}
	}

	private static bool ShouldHideCategory(ServerBrowserList list)
	{
		if (list == null)
		{
			return true;
		}
		switch (list.queryType)
		{
		case ServerBrowserList.QueryType.History:
			return false;
		case ServerBrowserList.QueryType.Friends:
		case ServerBrowserList.QueryType.LAN:
		case ServerBrowserList.QueryType.Favourites:
			return true;
		default:
		{
			try
			{
				if (list.UseOfficialServers)
				{
					return true;
				}
			}
			catch
			{
			}
			string categoryLabel = GetCategoryLabel(list);
			if (LabelMatches(categoryLabel, LocStrings.Cat_Official, LocStrings.Cat_Official_En))
			{
				return true;
			}
			if (LabelMatches(categoryLabel, LocStrings.Cat_Community, LocStrings.Cat_Community_En))
			{
				return true;
			}
			if (LabelMatches(categoryLabel, LocStrings.Cat_Favorites, LocStrings.Cat_Favorites_En, LocStrings.Cat_Favorites_En2))
			{
				return true;
			}
			if (LabelMatches(categoryLabel, LocStrings.Cat_Modified, LocStrings.Cat_Modified_En, LocStrings.Cat_Modded_En))
			{
				return false;
			}
			if (LabelMatches(categoryLabel, LocStrings.Cat_History, LocStrings.Cat_History_En))
			{
				return false;
			}
			if (HasVanillaOnlyRules(list))
			{
				return true;
			}
			if (HasModdedRules(list))
			{
				return false;
			}
			if (IsAmbiguousInternetList(list))
			{
				return false;
			}
			return true;
		}
		}
	}

	private static bool IsAmbiguousInternetList(ServerBrowserList list)
	{
		if (list == null)
		{
			return false;
		}
		if (list.queryType != 0)
		{
			return false;
		}
		try
		{
			return list.hideOfficialServers;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsModifiedCategory(ServerBrowserList list)
	{
		if (list == null)
		{
			return false;
		}
		if (list.queryType == ServerBrowserList.QueryType.History)
		{
			return false;
		}
		if (LabelMatches(GetCategoryLabel(list), LocStrings.Cat_Modified, LocStrings.Cat_Modified_En, LocStrings.Cat_Modded_En))
		{
			return true;
		}
		return HasModdedRules(list);
	}

	private static bool LabelMatches(string label, params string[] needles)
	{
		if (string.IsNullOrEmpty(label) || needles == null)
		{
			return false;
		}
		foreach (string value in needles)
		{
			if (!string.IsNullOrEmpty(value) && label.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string GetCategoryLabel(ServerBrowserList list)
	{
		try
		{
			ServerBrowserCategory categoryButton = list.categoryButton;
			if (categoryButton == null)
			{
				return string.Empty;
			}
			foreach (RustText componentsInChild in categoryButton.GetComponentsInChildren<RustText>(includeInactive: true))
			{
				if (!(componentsInChild == null))
				{
					string text = componentsInChild.text;
					if (!string.IsNullOrWhiteSpace(text))
					{
						return text.Trim();
					}
				}
			}
			foreach (TMP_Text componentsInChild2 in categoryButton.GetComponentsInChildren<TMP_Text>(includeInactive: true))
			{
				if (!(componentsInChild2 == null))
				{
					string text2 = componentsInChild2.text;
					if (!string.IsNullOrWhiteSpace(text2))
					{
						return text2.Trim();
					}
				}
			}
			foreach (Text componentsInChild3 in categoryButton.GetComponentsInChildren<Text>(includeInactive: true))
			{
				if (!(componentsInChild3 == null) && !string.IsNullOrWhiteSpace(componentsInChild3.text))
				{
					return componentsInChild3.text.Trim();
				}
			}
		}
		catch
		{
		}
		return string.Empty;
	}

	private static bool HasModdedRules(ServerBrowserList list)
	{
		return HasRuleTag(list, LocStrings.Cat_ModdedTag);
	}

	private static bool HasVanillaOnlyRules(ServerBrowserList list)
	{
		return HasRuleTag(list, LocStrings.Cat_VanillaTag);
	}

	private static bool HasRuleTag(ServerBrowserList list, string tagNeedle)
	{
		if (list == null || string.IsNullOrEmpty(tagNeedle))
		{
			return false;
		}
		try
		{
			ServerBrowserList.Rules[] array = list.rules;
			if (array == null)
			{
				return false;
			}
			for (int i = 0; i < array.Length; i++)
			{
				string tag = array[i].tag;
				if (!string.IsNullOrEmpty(tag) && tag.IndexOf(tagNeedle, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static void TryActivateCategory(ServerBrowserList list)
	{
		if (list == null)
		{
			return;
		}
		try
		{
			ServerBrowserCategory categoryButton = list.categoryButton;
			if (!(categoryButton == null))
			{
				RustButton rustButton = categoryButton.GetComponent<RustButton>();
				if (rustButton == null)
				{
					rustButton = categoryButton.GetComponentInChildren<RustButton>(includeInactive: true);
				}
				if (!(rustButton == null))
				{
					rustButton.SetPressed(value: true);
					rustButton.OnPressed.Invoke();
				}
			}
		}
		catch
		{
		}
	}
}
