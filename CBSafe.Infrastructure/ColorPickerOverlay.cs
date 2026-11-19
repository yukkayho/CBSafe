using System;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using UnityEngine;

namespace CBSafe.Infrastructure;

internal sealed class ColorPickerOverlay : MonoBehaviour
{
	private static ColorPickerOverlay _instance;

	private readonly ColorPicker _picker = new ColorPicker();

	private ConfigEntry<string> _targetConfig;

	private Action _onChanged;

	private float _wx = 100f;

	private float _wy = 100f;

	private const float WIN_W = 320f;

	private const float TITLE_H = 32f;

	private const float PAD = 10f;

	private bool _drag;

	private Vector2 _dragOff;

	private static GUIStyle _titleStyle;

	private static GUIStyle _closeStyle;

	internal static bool IsOpen
	{
		get
		{
			if (_instance != null)
			{
				return _instance._targetConfig != null;
			}
			return false;
		}
	}

	internal static void Open(ConfigEntry<string> config, Color current, string label, Action onChanged = null)
	{
		if (_instance == null)
		{
			GameObject obj = new GameObject("CBSafe_ColorPickerOverlay");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			obj.AddComponent<ColorPickerOverlay>();
		}
		_instance._targetConfig = config;
		_instance._onChanged = onChanged;
		_instance._picker.Label = label;
		_instance._picker.SetColor(current);
		_instance._picker.OnColorChanged -= _instance.OnPickerChanged;
		_instance._picker.OnColorChanged += _instance.OnPickerChanged;
		_instance._wx = (float)Screen.width * 0.5f - 160f;
		_instance._wy = (float)Screen.height * 0.5f - 180f;
	}

	internal static void Close()
	{
		if (!(_instance == null))
		{
			_instance._targetConfig = null;
		}
	}

	private void Awake()
	{
		_instance = this;
	}

	private void OnDestroy()
	{
		_picker.Destroy();
		_instance = null;
	}

	private void OnPickerChanged(Color c)
	{
		if (_targetConfig != null)
		{
			byte value = (byte)Mathf.RoundToInt(c.r * 255f);
			byte value2 = (byte)Mathf.RoundToInt(c.g * 255f);
			byte value3 = (byte)Mathf.RoundToInt(c.b * 255f);
			ConfigEntry<string> targetConfig = _targetConfig;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 3);
			defaultInterpolatedStringHandler.AppendFormatted(value);
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(value2);
			defaultInterpolatedStringHandler.AppendLiteral(",");
			defaultInterpolatedStringHandler.AppendFormatted(value3);
			defaultInterpolatedStringHandler.AppendLiteral(",255");
			targetConfig.Value = defaultInterpolatedStringHandler.ToStringAndClear();
			SharedConfig.File.Save();
			_onChanged?.Invoke();
		}
	}

	private void OnGUI()
	{
		if (_targetConfig == null)
		{
			return;
		}
		InitStyles();
		Event current = Event.current;
		float num = 300f;
		float num2 = num * 0.65f + 6f + 20f + 6f + 18f;
		float height = 42f + num2 + 10f;
		if (current != null && current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
		{
			Close();
			current.Use();
			return;
		}
		Rect rect = default(Rect);
		rect = new Rect(_wx, _wy, 320f, height);
		if (current != null && current.type == EventType.MouseDown && !rect.Contains(current.mousePosition))
		{
			Close();
			current.Use();
			return;
		}
		GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.97f);
		GUI.DrawTexture(rect, Texture2D.whiteTexture);
		Rect rect2 = default(Rect);
		rect2 = new Rect(_wx, _wy, 320f, 32f);
		GUI.color = new Color(0.08f, 0.08f, 0.08f, 1f);
		GUI.DrawTexture(rect2, Texture2D.whiteTexture);
		GUI.color = Color.white;
		GUI.Label(new Rect(_wx + 10f, _wy + 8f, 260f, 24f), _picker.Label, _titleStyle);
		GUI.color = new Color(0.65f, 0.08f, 0.08f, 1f);
		GUI.DrawTexture(new Rect(_wx + 320f - 30f, _wy + 6f, 22f, 20f), Texture2D.whiteTexture);
		GUI.color = Color.white;
		if (GUI.Button(new Rect(_wx + 320f - 30f, _wy + 6f, 22f, 20f), "x", _closeStyle))
		{
			Close();
			return;
		}
		if (current != null)
		{
			if (current.type == EventType.MouseDown && rect2.Contains(current.mousePosition))
			{
				_drag = true;
				_dragOff = current.mousePosition - new Vector2(_wx, _wy);
				current.Use();
			}
			else if (current.type == EventType.MouseDrag && _drag)
			{
				_wx = current.mousePosition.x - _dragOff.x;
				_wy = current.mousePosition.y - _dragOff.y;
				current.Use();
			}
			else if (current.type == EventType.MouseUp)
			{
				_drag = false;
			}
		}
		GUI.color = Color.white;
		_picker.Draw(_wx + 10f, _wy + 32f + 10f, num);
	}

	private static void InitStyles()
	{
		if (_titleStyle == null)
		{
			_titleStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 13,
				fontStyle = FontStyle.Bold,
				normal = 
				{
					textColor = Color.white
				},
				alignment = TextAnchor.MiddleLeft
			};
			_closeStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 14,
				fontStyle = FontStyle.Bold,
				normal = 
				{
					textColor = Color.white
				},
				alignment = TextAnchor.MiddleCenter
			};
		}
	}
}
