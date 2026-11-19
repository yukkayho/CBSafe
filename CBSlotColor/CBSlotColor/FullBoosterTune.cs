using System;
using System.Reflection;
using HarmonyLib;

namespace CBSlotColor;

internal static class FullBoosterTune
{
	private static readonly string[] HitchyFlags = new string[3] { "UnloadUnusedSkins", "BlockWorkshopSkins", "DisableSky" };

	public static void Apply(string reason)
	{
		try
		{
			Type type = AccessTools.TypeByName("CBSafe.Fps.OptConfig");
			if (type == null)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < HitchyFlags.Length; i++)
			{
				object obj = AccessTools.Field(type, HitchyFlags[i])?.GetValue(null);
				if (obj == null)
				{
					continue;
				}
				PropertyInfo property = obj.GetType().GetProperty("Value");
				if (!(property == null) && !(property.PropertyType != typeof(bool)))
				{
					object value = property.GetValue(obj);
					if (value is bool && (bool)value)
					{
						property.SetValue(obj, false);
						num++;
					}
				}
			}
			AccessTools.Field(AccessTools.TypeByName("CBSafe.Fps.OptRuntime"), "ForceApply")?.SetValue(null, true);
			if (num > 0)
			{
				CBSlotColorPlugin.PluginLog?.LogInfo("[CBSlotColor] Full booster tuned (" + reason + ")");
			}
		}
		catch
		{
		}
	}
}
