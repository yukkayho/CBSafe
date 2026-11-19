using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib.Tools;

namespace CBSafe.Utils;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
internal static class CBSafeStartupLogFilter
{
	private sealed class FilteredLogListener : ILogListener, IDisposable
	{
		private readonly IReadOnlyList<ILogListener> _inner;

		private readonly HashSet<string> _muted;

		public LogLevel LogLevelFilter { get; set; }

		internal FilteredLogListener(IReadOnlyList<ILogListener> inner, HashSet<string> muted)
		{
			_inner = inner;
			_muted = muted;
			LogLevelFilter = LogLevel.All;
		}

		public void LogEvent(object sender, LogEventArgs logEventArgs)
		{
			ManualLogSource manualLogSource = (ManualLogSource)((sender is ManualLogSource) ? sender : null);
			if (manualLogSource != null && _muted.Contains(manualLogSource.SourceName))
			{
				return;
			}
			for (int i = 0; i < _inner.Count; i++)
			{
				try
				{
					_inner[i].LogEvent(sender, logEventArgs);
				}
				catch (IOException)
				{
				}
				catch (ObjectDisposedException)
				{
				}
			}
		}

		public void Dispose()
		{
			for (int i = 0; i < _inner.Count; i++)
			{
				_inner[i].Dispose();
			}
		}
	}

	private static readonly string[] MutedSources = new string[3] { "Il2CppInterop", "HarmonyX", "DobbyDetour" };

	private static bool _installed;

	private static List<ILogListener>? _savedListeners;

	private static FilteredLogListener? _wrapper;

	internal static void Install()
	{
		if (_installed)
		{
			return;
		}
		_installed = true;
		try
		{
			HarmonyLib.Tools.Logger.ChannelFilter = HarmonyLib.Tools.Logger.LogChannel.None;
		}
		catch
		{
		}
		try
		{
			HashSet<string> muted = new HashSet<string>(MutedSources, StringComparer.Ordinal);
			_savedListeners = new List<ILogListener>();
			foreach (ILogListener listener in BepInEx.Logging.Logger.Listeners)
			{
				_savedListeners!.Add(listener);
			}
			BepInEx.Logging.Logger.Listeners.Clear();
			_wrapper = new FilteredLogListener(_savedListeners, muted);
			BepInEx.Logging.Logger.Listeners.Add(_wrapper);
		}
		catch
		{
		}
		MuteLogSources();
	}

	internal static void MuteLogSources()
	{
		try
		{
			ICollection<ILogSource> sources = BepInEx.Logging.Logger.Sources;
			List<ILogSource> list = new List<ILogSource>();
			foreach (ILogSource item in sources)
			{
				list.Add(item);
			}
			for (int i = 0; i < list.Count; i++)
			{
				ILogSource logSource = list[i];
				ManualLogSource manualLogSource = (ManualLogSource)((logSource is ManualLogSource) ? logSource : null);
				if (manualLogSource != null && IsMutedSource(manualLogSource.SourceName))
				{
					sources.Remove(manualLogSource);
				}
			}
		}
		catch
		{
		}
	}

	private static bool IsMutedSource(string sourceName)
	{
		for (int i = 0; i < MutedSources.Length; i++)
		{
			if (sourceName == MutedSources[i])
			{
				return true;
			}
		}
		return false;
	}
}
