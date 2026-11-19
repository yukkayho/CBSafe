using System.Reflection;

namespace CBSafe.Patches;

[Obfuscation(Exclude = true)]
public class ManifestPatch
{
	internal static void HideCategory(ServerBrowserList category)
	{
	}
}
