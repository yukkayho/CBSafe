using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Core.UI;

internal static class CustomTabSelection
{
	internal static void Apply(Transform tabBar, string activeName)
	{
		if (tabBar == null)
		{
			return;
		}
		try
		{
			for (int i = 0; i < tabBar.childCount; i++)
			{
				Transform child = tabBar.GetChild(i);
				if (!(child == null) && IsCustomName(child.name))
				{
					Button button = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(includeInactive: true);
					if (button != null)
					{
						button.interactable = string.IsNullOrEmpty(activeName) || child.name != activeName;
					}
				}
			}
		}
		catch
		{
		}
	}

	private static bool IsCustomName(string name)
	{
		if (!(name == LocStrings.Name_Optimization) && !(name == LocStrings.Name_Prochee) && !(name == LocStrings.Name_Color_TabButton))
		{
			return name == LocStrings.Name_Crosshair_TabButton;
		}
		return true;
	}
}
