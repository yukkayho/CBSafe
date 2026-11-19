using System;
using System.Collections.Generic;
using System.Reflection;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
internal static class CraftSkinHelper
{
	private static readonly Dictionary<int, int> SelectedSkinByItemId = new Dictionary<int, int>();

	private static FieldInfo[] _pickerIntFields;

	private static bool _pickerFieldsScanned;

	internal static void RememberSelectedSkin(int itemId, int skinInventoryId)
	{
		if (itemId != 0 && skinInventoryId != 0)
		{
			SelectedSkinByItemId[itemId] = skinInventoryId;
		}
	}

	internal static int GetSelectedSkin(int itemId)
	{
		if (itemId == 0)
		{
			return 0;
		}
		try
		{
			int num = ReadSkinFromCraftUi(itemId);
			if (num != 0)
			{
				SelectedSkinByItemId[itemId] = num;
				return num;
			}
		}
		catch
		{
		}
		if (!SelectedSkinByItemId.TryGetValue(itemId, out var value))
		{
			return 0;
		}
		return value;
	}

	private static int ReadSkinFromCraftUi(int itemId)
	{
		if (!SelectedBlueprint.isOpen)
		{
			return 0;
		}
		SelectedBlueprint instance = SingletonComponent<SelectedBlueprint>.Instance;
		if (instance == null || instance.blueprint == null)
		{
			return 0;
		}
		ItemDefinition targetItem = instance.blueprint.targetItem;
		if (targetItem == null || targetItem.itemid != itemId)
		{
			return 0;
		}
		IconSkinPicker skinPicker = instance.skinPicker;
		if (skinPicker == null)
		{
			return 0;
		}
		EnsurePickerFieldCache(skinPicker);
		for (int i = 0; i < _pickerIntFields.Length; i++)
		{
			FieldInfo fieldInfo = _pickerIntFields[i];
			if (fieldInfo == null)
			{
				continue;
			}
			try
			{
				object value = fieldInfo.GetValue(skinPicker);
				if (value is int)
				{
					int num = (int)value;
					if (num != 0 && IsValidSkinForItem(num, itemId))
					{
						return num;
					}
				}
			}
			catch
			{
			}
		}
		return 0;
	}

	private static void EnsurePickerFieldCache(IconSkinPicker picker)
	{
		if (_pickerFieldsScanned)
		{
			return;
		}
		_pickerFieldsScanned = true;
		List<FieldInfo> list = new List<FieldInfo>();
		Type type = picker.GetType();
		while (type != null && type != typeof(object))
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.FieldType == typeof(int))
				{
					list.Add(fieldInfo);
				}
			}
			type = type.BaseType;
		}
		_pickerIntFields = list.ToArray();
	}

	private static bool IsValidSkinForItem(int skinInventoryId, int itemId)
	{
		if (skinInventoryId == 0 || itemId == 0)
		{
			return false;
		}
		if (!CraftStoreUnlockState.SkinInventoryIds.Contains(skinInventoryId))
		{
			return false;
		}
		ItemSkinDirectory.Skin skin = ItemSkinDirectory.FindByInventoryDefinitionId(skinInventoryId);
		if (skin.itemid != 0)
		{
			return skin.itemid == itemId;
		}
		ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemId);
		if (itemDefinition == null)
		{
			return false;
		}
		ItemSkinDirectory.Skin[] array = itemDefinition.skins;
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].id == skinInventoryId)
				{
					return true;
				}
			}
		}
		IPlayerItemDefinition[] array2 = itemDefinition.skins2;
		if (array2 != null)
		{
			IPlayerItemDefinition[] array3 = array2;
			foreach (IPlayerItemDefinition playerItemDefinition in array3)
			{
				if (playerItemDefinition != null && playerItemDefinition.DefinitionId == skinInventoryId)
				{
					return true;
				}
			}
		}
		return false;
	}
}
