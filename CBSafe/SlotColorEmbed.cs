using System;
using System.IO;
using System.Reflection;
using BepInEx.Logging;

namespace CBSafe;

public static class SlotColorEmbed
{
	public static void Boot(ManualLogSource P_0)
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CBSafe.Embedded.CBSlotColor.dll");
			if (stream == null)
			{
				P_0?.LogWarning("[CBSlotColor] embedded payload missing");
				return;
			}
			using MemoryStream memoryStream = new MemoryStream();
			stream.CopyTo(memoryStream);
			(Assembly.Load(memoryStream.ToArray()).GetType("CBSlotColor.CBSlotColorPlugin")?.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public))?.Invoke(null, new object[1] { P_0 });
		}
		catch (Exception ex)
		{
			P_0?.LogError("[CBSlotColor] boot failed: " + ex);
		}
	}
}
