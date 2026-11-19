using HarmonyLib;

namespace CBSlotColor;

[HarmonyPatch(typeof(ItemIcon), "OnEnable")]
internal static class ItemIcon_OnEnable_Patch
{
	[HarmonyPrefix]
	public static void Prefix(ItemIcon __instance)
	{
		CBSlotColorPlugin.ApplyToIcon(__instance);
	}
}
