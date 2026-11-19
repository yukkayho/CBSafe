using HarmonyLib;

namespace CBSlotColor;

[HarmonyPatch(typeof(ItemIcon), "Awake")]
internal static class ItemIcon_Awake_Patch
{
	[HarmonyPostfix]
	public static void Postfix(ItemIcon __instance)
	{
		CBSlotColorPlugin.ApplyToIcon(__instance);
	}
}
