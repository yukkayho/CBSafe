using System.Reflection;
using Il2CppSystem;
using Rust.UI;
using TMPro;
using UnityEngine;

namespace CBSafe.Core.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public static class TabLabelHelper
{
	public static void ApplyFixedTabLabel(GameObject tabRoot, string desired)
	{
		if (tabRoot == null || string.IsNullOrEmpty(desired))
		{
			return;
		}
		StripLocalizationComponents(tabRoot.transform);
		foreach (TMP_Text componentsInChild in tabRoot.GetComponentsInChildren<TMP_Text>(includeInactive: true))
		{
			if (!(componentsInChild == null))
			{
				ApplyFixedTabLabel(componentsInChild, desired);
			}
		}
	}

	public static void ApplyFixedTabLabel(TMP_Text labelTmp, string desired)
	{
		if (labelTmp == null || string.IsNullOrEmpty(desired))
		{
			return;
		}
		StripLocalizationComponents(labelTmp.transform);
		try
		{
			RustText rustText = labelTmp.TryCast<RustText>();
			if (rustText != null)
			{
				try
				{
					rustText.Token = string.Empty;
				}
				catch
				{
				}
				try
				{
					rustText.IsLocalized = false;
				}
				catch
				{
				}
				try
				{
					rustText.English = desired;
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		try
		{
			labelTmp.text = desired;
		}
		catch
		{
		}
		try
		{
			TextMeshProUGUI textMeshProUGUI = labelTmp.TryCast<TextMeshProUGUI>();
			if (textMeshProUGUI != null)
			{
				textMeshProUGUI.m_text = desired;
			}
		}
		catch
		{
		}
		try
		{
			labelTmp.ForceMeshUpdate();
		}
		catch
		{
		}
		try
		{
			DropdownLabelEnforcer component = labelTmp.GetComponent<DropdownLabelEnforcer>();
			if (component == null)
			{
				labelTmp.gameObject.AddComponent<DropdownLabelEnforcer>().Bind(labelTmp, desired);
			}
			else
			{
				component.Bind(labelTmp, desired);
			}
		}
		catch
		{
		}
	}

	private static void StripLocalizationComponents(Transform root)
	{
		if (root == null)
		{
			return;
		}
		try
		{
			foreach (Component component in root.GetComponents<Component>())
			{
				if (component == null)
				{
					continue;
				}
				string text;
				try
				{
					Type il2CppType = component.GetIl2CppType();
					text = ((il2CppType != null) ? il2CppType.Name : null);
				}
				catch
				{
					continue;
				}
				if (!string.IsNullOrEmpty(text) && (text.Contains(LocStrings.Marker_Locali) || text.Contains(LocStrings.Marker_Phrase) || text.Contains(LocStrings.Marker_Token)))
				{
					try
					{
						UnityEngine.Object.DestroyImmediate(component);
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
