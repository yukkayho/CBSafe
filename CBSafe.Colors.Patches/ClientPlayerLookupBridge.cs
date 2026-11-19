using System.Reflection;

namespace CBSafe.Colors.Patches;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
internal static class ClientPlayerLookupBridge
{
	public static BasePlayer FindByID(ulong userId)
	{
		if (userId == 0L)
		{
			return null;
		}
		try
		{
			return BasePlayer.FindByID_Clientside(userId);
		}
		catch
		{
			return null;
		}
	}
}
