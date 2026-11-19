using System;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace CBSlotColor;

[HarmonyPatch]
internal static class Patch_TabSystemManager_Build
{
	private static MethodBase _targetMethod;

	private static bool _buttonsInjected;

	private static MethodInfo _createButtonMethod;

	private static FieldInfo _pendingOverlayField;

	private static FieldInfo _contentBuiltField;

	public static MethodBase TargetMethod()
	{
		try
		{
			Type type = AccessTools.TypeByName("CBSafe.Colors.UI.TabSystemManager");
			if (type != null)
			{
				_pendingOverlayField = AccessTools.Field(type, "_pendingOverlay");
				_contentBuiltField = AccessTools.Field(type, "_contentBuilt");
				_targetMethod = AccessTools.Method(type, "TryBuildNextContentStep");
			}
			Type type2 = AccessTools.TypeByName("CBSafe.Colors.UI.ColorPickerButtonFactory");
			if (type2 != null)
			{
				_createButtonMethod = AccessTools.Method(type2, "Create", new Type[5]
				{
					typeof(Transform),
					typeof(string),
					typeof(ConfigEntry<string>),
					typeof(ConfigEntry<bool>),
					typeof(Action)
				});
			}
		}
		catch
		{
		}
		return _targetMethod;
	}

	public static void Postfix()
	{
		try
		{
			if (_createButtonMethod == null || _pendingOverlayField == null || _contentBuiltField == null || !(bool)_contentBuiltField.GetValue(null) || _buttonsInjected)
			{
				return;
			}
			Transform transform = _pendingOverlayField.GetValue(null) as Transform;
			if (!(transform == null))
			{
				_buttonsInjected = true;
				Action action = delegate
				{
					CBSlotColorPlugin.PreserveAlphaOnPickerWrite();
					CBSlotColorPlugin.UpdateColors();
					CBSlotColorPlugin.PersistNow();
					CBSlotColorPlugin.ThrottleRefreshOpenIcons();
				};
				_createButtonMethod.Invoke(null, new object[5]
				{
					transform,
					"ЦВЕТ СЛОТОВ (ПРЕДМЕТЫ)",
					CBSlotColorPlugin.SlotColor,
					CBSlotColorPlugin.SlotColorEnabled,
					action
				});
				_createButtonMethod.Invoke(null, new object[5]
				{
					transform,
					"ЦВЕТ СЛОТОВ (ПУСТЫЕ)",
					CBSlotColorPlugin.EmptySlotColor,
					CBSlotColorPlugin.SlotColorEnabled,
					action
				});
				_createButtonMethod.Invoke(null, new object[5]
				{
					transform,
					"ЦВЕТ ВЫБРАННОГО СЛОТА",
					CBSlotColorPlugin.SelectedSlotColor,
					CBSlotColorPlugin.SlotColorEnabled,
					action
				});
				CBSlotColorPlugin.PluginLog?.LogInfo("[CBSlotColor] Injected slot color controls into Colors tab");
			}
		}
		catch (Exception ex)
		{
			CBSlotColorPlugin.PluginLog?.LogWarning("[CBSlotColor] Settings injection error: " + ex.Message);
		}
	}
}
