using HarmonyLib;

namespace CBSlotColor;

[HarmonyPatch(typeof(ItemIcon), "UpdateBackgroundColor")]
internal static class ItemIcon_UpdateBackgroundColor_Patch
{
	[HarmonyPrefix]
	public static void Prefix(ItemIcon __instance)
	{
		CBSlotColorPlugin.ApplyToIcon(__instance);
	}
}
