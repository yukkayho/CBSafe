using System;
using System.Reflection;
using Rust.UI;
using TMPro;
using UnityEngine;

namespace CBSafe.Core.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
public class DropdownLabelEnforcer : MonoBehaviour
{
	private TMP_Text _tmp;

	private string _desired;

	public DropdownLabelEnforcer(IntPtr ptr)
		: base(ptr)
	{
	}

	public void Bind(TMP_Text tmp, string desired)
	{
		_tmp = tmp;
		_desired = desired ?? string.Empty;
		Apply();
	}

	private void Apply()
	{
		if (_tmp == null)
		{
			return;
		}
		try
		{
			RustText rustText = _tmp.TryCast<RustText>();
			if (rustText != null)
			{
				try
				{
					rustText.Token = string.Empty;
				}
				catch
				{
				}
				try
				{
					rustText.IsLocalized = false;
				}
				catch
				{
				}
			}
			if (_tmp.text != _desired)
			{
				_tmp.text = _desired;
				try
				{
					_tmp.ForceMeshUpdate();
				}
				catch
				{
				}
			}
			TextMeshProUGUI textMeshProUGUI = _tmp.TryCast<TextMeshProUGUI>();
			if (!(textMeshProUGUI != null))
			{
				return;
			}
			try
			{
				if (textMeshProUGUI.m_text != _desired)
				{
					textMeshProUGUI.m_text = _desired;
				}
			}
			catch
			{
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
	}
}
