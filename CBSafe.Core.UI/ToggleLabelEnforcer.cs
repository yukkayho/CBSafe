using System;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace CBSafe.Core.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class ToggleLabelEnforcer : MonoBehaviour
{
	private TMP_Text _tmp;

	private string _desired;

	private int _ticksLeft;

	public ToggleLabelEnforcer(IntPtr ptr)
		: base(ptr)
	{
	}

	public void Bind(TMP_Text tmp, string desired)
	{
		_tmp = tmp;
		_desired = desired ?? string.Empty;
		_ticksLeft = 120;
	}

	private void Apply()
	{
		if (_tmp == null)
		{
			return;
		}
		try
		{
			if (_tmp.text != _desired)
			{
				_tmp.text = _desired;
				try
				{
					_tmp.ForceMeshUpdate();
					return;
				}
				catch
				{
					return;
				}
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
		Apply();
	}
}
