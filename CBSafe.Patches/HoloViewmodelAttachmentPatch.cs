using CBSafe.Holo;
using HarmonyLib;

namespace CBSafe.Patches;

[HarmonyPatch(typeof(ViewmodelAttachment), "OnEnable")]
internal static class HoloViewmodelAttachmentPatch
{
	private static void Postfix(ViewmodelAttachment __instance)
	{
		HolosightSwap.OnAttachment(__instance);
	}
}
