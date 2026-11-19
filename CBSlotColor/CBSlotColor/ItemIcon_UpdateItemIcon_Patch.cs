using HarmonyLib;

namespace CBSlotColor;

[HarmonyPatch(typeof(ItemIcon), "UpdateItemIcon")]
internal static class ItemIcon_UpdateItemIcon_Patch
{
	[HarmonyPrefix]
	public static void Prefix(ItemIcon __instance)
	{
		CBSlotColorPlugin.ApplyToIcon(__instance);
	}
}
