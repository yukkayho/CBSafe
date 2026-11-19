using System;
using System.Reflection;
using UnityEngine;

namespace CBSafe.Core.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class ToggleVisualEnforcer : MonoBehaviour
{
	private GameObject _offGo;

	private GameObject _onGo;

	private bool _value;

	private int _ticksLeft;

	public ToggleVisualEnforcer(IntPtr ptr)
		: base(ptr)
	{
	}

	public void Bind(GameObject offGo, GameObject onGo, bool value)
	{
		_offGo = offGo;
		_onGo = onGo;
		_value = value;
		_ticksLeft = 180;
		Apply();
	}

	public void SetValue(bool value)
	{
		_value = value;
		Apply();
	}

	private void Apply()
	{
		try
		{
			if (_offGo != null)
			{
				_offGo.SetActive(!_value);
			}
		}
		catch
		{
		}
		try
		{
			if (_onGo != null)
			{
				_onGo.SetActive(_value);
			}
		}
		catch
		{
		}
	}

	private void OnEnable()
	{
		Apply();
	}

	private void Start()
	{
		Apply();
	}

	private void LateUpdate()
	{
		Apply();
		if (_ticksLeft <= 0)
		{
			try
			{
				UnityEngine.Object.Destroy(this);
				return;
			}
			catch
			{
				return;
			}
		}
		_ticksLeft--;
	}
}
