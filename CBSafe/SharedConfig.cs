using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace CBSafe;

internal static class SharedConfig
{
	public const string FileName = "CBSafe.cfg";

	private static ConfigFile _file;

	private static string _resolvedConfigPath;

	private static readonly object _gate = new object();

	private static bool _cfgRepaired;

	internal static Action OnBeforeConfigFlush;

	private static bool _dirty;

	private static float _dirtyAt;

	private const float SaveDebounceSec = 0.75f;

	public static string ConfigDirectory
	{
		get
		{
			if (!string.IsNullOrEmpty(_resolvedConfigPath))
			{
				string directoryName = Path.GetDirectoryName(_resolvedConfigPath);
				if (!string.IsNullOrEmpty(directoryName))
				{
					return directoryName;
				}
			}
			return Paths.ConfigPath;
		}
	}

	public static ConfigFile File
	{
		get
		{
			if (_file != null)
			{
				return _file;
			}
			lock (_gate)
			{
				if (_file != null)
				{
					return _file;
				}
				_file = CreateConfigFile();
				try
				{
					_file.SaveOnConfigSet = false;
				}
				catch
				{
				}
				return _file;
			}
		}
	}

	public static bool IsDirty => _dirty;

	private static ConfigFile CreateConfigFile()
	{
		if (!_cfgRepaired)
		{
			_cfgRepaired = true;
		}
		string[] array = new string[3]
		{
			Paths.ConfigPath,
			string.IsNullOrWhiteSpace(Paths.BepInExRootPath) ? string.Empty : Path.Combine(Paths.BepInExRootPath, "config"),
			Path.Combine(Environment.CurrentDirectory, "BepInEx", "config")
		};
		foreach (string text in array)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			try
			{
				Directory.CreateDirectory(text);
				string text2 = Path.Combine(text, "CBSafe.cfg");
				try
				{
					RepairCorruptedKeys(text2);
				}
				catch
				{
				}
				_resolvedConfigPath = text2;
				return new ConfigFile(text2, saveOnInit: false);
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (IOException)
			{
			}
		}
		string text3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CBSafe", "config");
		Directory.CreateDirectory(text3);
		string text4 = Path.Combine(text3, "CBSafe.cfg");
		try
		{
			RepairCorruptedKeys(text4);
		}
		catch
		{
		}
		_resolvedConfigPath = text4;
		return new ConfigFile(text4, saveOnInit: false);
	}

	public static void EnsureDeferredSave()
	{
		try
		{
			File.SaveOnConfigSet = false;
		}
		catch
		{
		}
	}

	public static void MarkDirty()
	{
		_dirty = true;
		try
		{
			_dirtyAt = Time.unscaledTime;
		}
		catch
		{
			_dirtyAt = 0f;
		}
	}

	public static void DrainPendingSave()
	{
		if (!_dirty)
		{
			return;
		}
		float num = 0f;
		try
		{
			num = Time.unscaledTime;
		}
		catch
		{
			return;
		}
		if (num - _dirtyAt < 0.75f)
		{
			return;
		}
		_dirty = false;
		try
		{
			OnBeforeConfigFlush?.Invoke();
		}
		catch
		{
		}
		try
		{
			File.Save();
		}
		catch
		{
		}
	}

	public static void FlushNow()
	{
		_dirty = false;
		try
		{
			OnBeforeConfigFlush?.Invoke();
		}
		catch
		{
		}
		try
		{
			File.Save();
		}
		catch
		{
		}
	}

	private static void RepairCorruptedKeys(string path)
	{
		if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
		{
			return;
		}
		string[] array = System.IO.File.ReadAllLines(path);
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			if (text == null)
			{
				continue;
			}
			string text2 = text.TrimStart();
			if (text2.StartsWith("=", StringComparison.Ordinal) && i > 0)
			{
				string text3 = array[i - 1] ?? string.Empty;
				string text4 = null;
				if (text3.IndexOf("КРЕСТИК", StringComparison.OrdinalIgnoreCase) >= 0 || text3.IndexOf("CrosshairColor", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					text4 = "CrosshairColor" + text2;
				}
				else if ((text3.IndexOf("ПРИЦЕЛ", StringComparison.OrdinalIgnoreCase) >= 0 && text3.IndexOf("КРЕСТИК", StringComparison.OrdinalIgnoreCase) < 0) || text3.IndexOf("ReticleColor", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					text4 = "ReticleColor" + text2;
				}
				else if (text3.IndexOf("ReticleCustomized", StringComparison.OrdinalIgnoreCase) >= 0 || (text3.IndexOf("голо", StringComparison.OrdinalIgnoreCase) >= 0 && text3.IndexOf("прицел", StringComparison.OrdinalIgnoreCase) >= 0 && text3.IndexOf("idx=0", StringComparison.OrdinalIgnoreCase) >= 0))
				{
					text4 = "ReticleCustomized" + text2;
				}
				else if ((text3.IndexOf("ИГРОК", StringComparison.OrdinalIgnoreCase) >= 0 && text3.IndexOf("СОЮЗН", StringComparison.OrdinalIgnoreCase) < 0 && text3.IndexOf("ПРОТИВН", StringComparison.OrdinalIgnoreCase) < 0) || text3.IndexOf("PlayerName", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					text4 = "PlayerNameColor" + text2;
				}
				else if (text3.IndexOf("КОМАНД", StringComparison.OrdinalIgnoreCase) >= 0 || text3.IndexOf("TeamName", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					text4 = "TeamNameColor" + text2;
				}
				else if (text3.IndexOf("СОЮЗН", StringComparison.OrdinalIgnoreCase) >= 0 || text3.IndexOf("AllyName", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					text4 = "AllyNameColor" + text2;
				}
				else if (text3.IndexOf("ПРОТИВН", StringComparison.OrdinalIgnoreCase) >= 0 || text3.IndexOf("EnemyName", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					text4 = "EnemyNameColor" + text2;
				}
				if (text4 != null && text4 != text)
				{
					array[i] = text4;
					flag = true;
				}
			}
		}
		if (flag)
		{
			System.IO.File.WriteAllLines(path, array);
		}
	}
}
