using System;
using System.Reflection;
using CBSafe.Colors.UI;
using CBSafe.Crosshair;
using CBSafe.Crosshair.Render;
using CBSafe.Crosshair.UI;
using UnityEngine;

namespace CBSafe.Core.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public sealed class SettingsTabPrefetcher : MonoBehaviour
{
	private static SettingsTabPrefetcher _instance;

	private static bool _scheduledThisSession;

	private static float _pausedUntil;

	private static bool _loadBurstMode;

	private static bool _settingsBurstMode;

	private int _stage = -1;

	private int _waitFrames;

	private const int LoadBurstStepsPerTick = 16;

	private const int SettingsBurstStepsPerFrame = 8;

	private const int IdleStepsPerFrame = 4;

	public SettingsTabPrefetcher(IntPtr ptr)
		: base(ptr)
	{
	}

	public static bool IsFullyBuilt()
	{
		try
		{
			return TabSystemManager.IsContentBuilt && CrosshairTabManager.IsContentBuilt;
		}
		catch
		{
			return false;
		}
	}

	public static void Schedule(int initialDelayFrames = 12)
	{
		EnsureInstance();
		if (!(_instance == null) && (!_scheduledThisSession || _instance._stage >= 0 || !IsFullyBuilt()))
		{
			_scheduledThisSession = true;
			if (_instance._stage < 0)
			{
				_instance._stage = (TabSystemManager.IsContentBuilt ? 1 : 0);
			}
			_instance._waitFrames = ((initialDelayFrames >= 0) ? initialDelayFrames : 0);
		}
	}

	public static void EnsureRunning(int maxDelayFrames = 0)
	{
		EnsureInstance();
		if (!(_instance == null))
		{
			_scheduledThisSession = true;
			if (_instance._stage < 0)
			{
				_instance._stage = (TabSystemManager.IsContentBuilt ? 1 : 0);
			}
			if (_instance._waitFrames > maxDelayFrames)
			{
				_instance._waitFrames = ((maxDelayFrames >= 0) ? maxDelayFrames : 0);
			}
		}
	}

	public static void ArmLoadScreenBurst()
	{
		_pausedUntil = 0f;
		_loadBurstMode = true;
		_settingsBurstMode = false;
		EnsureRunning();
	}

	public static void ArmSettingsBurst()
	{
		_pausedUntil = 0f;
		_settingsBurstMode = true;
		EnsureRunning();
	}

	public static void TickLoadScreenBurst(int maxSteps = 16)
	{
		_pausedUntil = 0f;
		RunSteps(maxSteps);
		if (IsFullyBuilt())
		{
			_loadBurstMode = false;
			_settingsBurstMode = false;
			Cancel();
		}
	}

	public static void CompleteAllPending()
	{
		_pausedUntil = 0f;
		int num = 0;
		while (!IsFullyBuilt() && num++ < 64)
		{
			RunSteps(8);
		}
		if (IsFullyBuilt())
		{
			Cancel();
		}
	}

	public static void ResetForSettingsSession()
	{
		_scheduledThisSession = false;
		_loadBurstMode = false;
		_settingsBurstMode = false;
		Cancel();
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
		_loadBurstMode = false;
		_settingsBurstMode = false;
		Cancel();
	}

	public static void StopForGameplay()
	{
		PauseForWorldLoad();
	}

	public static void ResumeForSettings()
	{
		_pausedUntil = 0f;
		ArmSettingsBurst();
	}

	public static void Cancel()
	{
		if (_instance != null)
		{
			_instance._stage = -1;
		}
		_loadBurstMode = false;
		_settingsBurstMode = false;
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
				_instance = gameObject.GetComponent<SettingsTabPrefetcher>();
				if (_instance == null)
				{
					_instance = gameObject.AddComponent<SettingsTabPrefetcher>();
				}
			}
		}
		catch
		{
		}
	}

	private void Update()
	{
		if (Time.unscaledTime < _pausedUntil || _loadBurstMode || (_stage < 0 && !_settingsBurstMode))
		{
			return;
		}
		if (_waitFrames > 0)
		{
			_waitFrames--;
			return;
		}
		try
		{
			RunSteps(_settingsBurstMode ? 8 : 4);
			if (IsFullyBuilt())
			{
				_settingsBurstMode = false;
				Cancel();
			}
			else if (_stage >= 0)
			{
				_waitFrames = ((!_settingsBurstMode) ? 1 : 0);
			}
		}
		catch
		{
			_stage = -1;
			_settingsBurstMode = false;
		}
	}

	private static void RunSteps(int maxSteps)
	{
		if (maxSteps <= 0)
		{
			return;
		}
		if (_instance != null && _instance._stage < 0)
		{
			EnsureRunning();
		}
		for (int i = 0; i < maxSteps; i++)
		{
			if (IsFullyBuilt())
			{
				break;
			}
			if (_instance == null || _instance._stage < 0)
			{
				EnsureRunning();
				if (_instance == null || _instance._stage < 0)
				{
					break;
				}
			}
			bool flag = false;
			if (_instance._stage == 0)
			{
				if (TryAdvanceColorTabBuild())
				{
					flag = true;
				}
				else
				{
					_instance._stage = 1;
					flag = true;
				}
			}
			else if (_instance._stage == 1)
			{
				if (!CrosshairTabManager.TryBuildNextContentStep())
				{
					_instance._stage = -1;
					break;
				}
				flag = true;
			}
			if (!flag)
			{
				break;
			}
		}
	}

	private static bool TryAdvanceColorTabBuild()
	{
		try
		{
			if (TabSystemManager.IsContentBuilt)
			{
				return false;
			}
			return TabSystemManager.TryBuildNextContentStep();
		}
		catch
		{
			return false;
		}
	}
}
