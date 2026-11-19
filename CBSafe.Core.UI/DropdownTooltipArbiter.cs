using System;
using System.Collections.Generic;
using System.Reflection;

namespace CBSafe.Core.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public static class DropdownTooltipArbiter
{
	private struct Entry
	{
		public object Key;

		public Action ShowCallback;

		public Action HideCallback;
	}

	private static readonly List<Entry> _entries = new List<Entry>(16);

	private static object _currentOwner;

	public static object CurrentOwner => _currentOwner;

	public static void Register(object key, Action show, Action hide)
	{
		if (key == null)
		{
			return;
		}
		for (int i = 0; i < _entries.Count; i++)
		{
			if (_entries[i].Key == key)
			{
				_entries[i] = new Entry
				{
					Key = key,
					ShowCallback = show,
					HideCallback = hide
				};
				return;
			}
		}
		_entries.Add(new Entry
		{
			Key = key,
			ShowCallback = show,
			HideCallback = hide
		});
	}

	public static void Unregister(object key)
	{
		if (key == null)
		{
			return;
		}
		for (int num = _entries.Count - 1; num >= 0; num--)
		{
			if (_entries[num].Key == key)
			{
				_entries.RemoveAt(num);
			}
		}
		if (_currentOwner == key)
		{
			_currentOwner = null;
		}
	}

	public static void Clear()
	{
		_entries.Clear();
		_currentOwner = null;
	}

	public static void RequestShow(object key)
	{
		if (key == null)
		{
			return;
		}
		if (_currentOwner != key)
		{
			for (int i = 0; i < _entries.Count; i++)
			{
				Entry entry = _entries[i];
				if (entry.Key != key)
				{
					try
					{
						entry.HideCallback?.Invoke();
					}
					catch
					{
					}
				}
			}
		}
		_currentOwner = key;
		for (int j = 0; j < _entries.Count; j++)
		{
			Entry entry2 = _entries[j];
			if (entry2.Key == key)
			{
				try
				{
					entry2.ShowCallback?.Invoke();
					break;
				}
				catch
				{
					break;
				}
			}
		}
	}

	public static void NotifyHide(object key)
	{
		if (key == null)
		{
			return;
		}
		for (int i = 0; i < _entries.Count; i++)
		{
			Entry entry = _entries[i];
			if (entry.Key == key)
			{
				try
				{
					entry.HideCallback?.Invoke();
				}
				catch
				{
				}
				break;
			}
		}
		if (_currentOwner == key)
		{
			_currentOwner = null;
		}
	}

	public static void HideAll()
	{
		for (int i = 0; i < _entries.Count; i++)
		{
			try
			{
				_entries[i].HideCallback?.Invoke();
			}
			catch
			{
			}
		}
		_currentOwner = null;
	}
}
