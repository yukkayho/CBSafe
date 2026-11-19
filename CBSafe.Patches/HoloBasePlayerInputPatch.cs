using CBSafe.Holo;
using HarmonyLib;

namespace CBSafe.Patches;

[HarmonyPatch(typeof(BasePlayer), "ClientInput")]
internal static class HoloBasePlayerInputPatch
{
	private static void Postfix()
	{
		HolosightSwap.TickRetry();
	}
}
