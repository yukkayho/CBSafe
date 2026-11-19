using System;
using System.Reflection;
using CBSafe.Crosshair.Infrastructure;
using CBSafe.Crosshair.Render;
using UnityEngine;

namespace CBSafe.Crosshair.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public sealed class CrosshairContentPrefetcher : MonoBehaviour
{
	private static CrosshairContentPrefetcher _instance;

	private static float _pausedUntil;

	private int _framesUntilPrefetch = -1;

	private bool _building;

	public CrosshairContentPrefetcher(IntPtr ptr)
		: base(ptr)
	{
	}

	public static void Request(int delayFrames = 2)
	{
		EnsureInstance();
		if (!(_instance == null))
		{
			_instance._framesUntilPrefetch = ((delayFrames >= 0) ? delayFrames : 0);
			_instance._building = true;
		}
	}

	public static void Cancel()
	{
		if (_instance != null)
		{
			_instance._framesUntilPrefetch = -1;
			_instance._building = false;
		}
		try
		{
			CrosshairConfigStore.ForceReleaseUiInitGuard();
		}
		catch
		{
		}
	}

	public static void PauseForWorldLoad(float seconds = 25f)
	{
		try
		{
			_pausedUntil = Time.unscaledTime + Mathf.Max(1f, seconds);
		}
		catch
		{
			_pausedUntil = 0f;
		}
		Cancel();
	}

	public static void StopForGameplay()
	{
		PauseForWorldLoad();
	}

	public static void ResumeForSettings()
	{
		_pausedUntil = 0f;
		Request(0);
	}

	private static void EnsureInstance()
	{
		if (_instance != null)
		{
			return;
		}
		try
		{
			CBSafeCrosshairPlugin instance = CBSafeCrosshairPlugin.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				ScreenCrosshairRenderer screenRenderer = instance.ScreenRenderer;
				obj = ((screenRenderer != null) ? screenRenderer.gameObject : null);
			}
			GameObject gameObject = (GameObject)obj;
			if (!(gameObject == null))
			{
				_instance = gameObject.GetComponent<CrosshairContentPrefetcher>();
				if (_instance == null)
				{
					_instance = gameObject.AddComponent<CrosshairContentPrefetcher>();
				}
			}
		}
		catch
		{
		}
	}

	private void Update()
	{
		if (Time.unscaledTime < _pausedUntil || (!_building && _framesUntilPrefetch < 0))
		{
			return;
		}
		if (_framesUntilPrefetch > 0)
		{
			_framesUntilPrefetch--;
			return;
		}
		try
		{
			if (!CrosshairTabManager.TryBuildNextContentStep())
			{
				_building = false;
			}
			else
			{
				_framesUntilPrefetch = 1;
			}
		}
		catch
		{
			_building = false;
			_framesUntilPrefetch = -1;
		}
	}
}
