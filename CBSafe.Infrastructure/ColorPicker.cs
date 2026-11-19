using System;
using UnityEngine;

namespace CBSafe.Infrastructure;

internal sealed class ColorPicker
{
	private float _lastEventTime;

	private float _h;

	private float _s = 1f;

	private float _v = 1f;

	private Texture2D _svTex;

	private Texture2D _hueTex;

	private int _svBuildH = -1;

	private bool _svDirty = true;

	private bool _dragSV;

	private bool _dragHue;

	internal string Label = string.Empty;

	internal Color CurrentColor => Color.HSVToRGB(_h, _s, _v);

	internal event Action<Color> OnColorChanged;

	private void FireColorChanged(bool force = false)
	{
		if (force || Time.unscaledTime - _lastEventTime > 0.05f)
		{
			_lastEventTime = Time.unscaledTime;
			this.OnColorChanged?.Invoke(CurrentColor);
		}
	}

	internal void SetColor(Color c)
	{
		Color.RGBToHSV(c, out _h, out _s, out _v);
		_svDirty = true;
	}

	internal float Draw(float x, float y, float w)
	{
		float num = w * 0.65f;
		float num2 = 20f;
		float num3 = 6f;
		Rect rect = default(Rect);
		rect = new Rect(x, y, w, num);
		Rect rect2 = default(Rect);
		rect2 = new Rect(x, y + num + num3, w, num2);
		EnsureSVTex((int)w, (int)num);
		if (_svTex != null)
		{
			GUI.DrawTexture(rect, _svTex);
		}
		float cx = x + _s * w;
		float cy = y + (1f - _v) * num;
		DrawRing(cx, cy, 6f);
		EnsureHueTex((int)w);
		if (_hueTex != null)
		{
			GUI.DrawTexture(rect2, _hueTex);
		}
		float cx2 = x + _h * w;
		float cy2 = y + num + num3 + num2 * 0.5f;
		DrawRing(cx2, cy2, 8f);
		GUI.color = Color.white;
		float result = num + num3 + num2 + num3;
		HandleInput(x, y, w, num, num3, num2);
		return result;
	}

	private void HandleInput(float x, float y, float w, float svH, float pad, float hueH)
	{
		Event current = Event.current;
		if (current == null)
		{
			return;
		}
		Vector2 mousePosition = current.mousePosition;
		bool flag = current.type == EventType.MouseDown;
		bool flag2 = current.type == EventType.MouseDrag;
		bool num = current.type == EventType.MouseUp;
		Rect rect = default(Rect);
		rect = new Rect(x, y, w, svH);
		Rect rect2 = default(Rect);
		rect2 = new Rect(x, y + svH + pad, w, hueH);
		if (num && (_dragSV || _dragHue))
		{
			_dragSV = false;
			_dragHue = false;
			FireColorChanged(force: true);
		}
		if ((flag || flag2) && (flag ? rect.Contains(mousePosition) : _dragSV))
		{
			_dragSV = true;
			_s = Mathf.Clamp01((mousePosition.x - x) / w);
			_v = Mathf.Clamp01(1f - (mousePosition.y - y) / svH);
			FireColorChanged(flag);
			if (flag)
			{
				current.Use();
			}
		}
		else if ((flag || flag2) && (flag ? rect2.Contains(mousePosition) : _dragHue))
		{
			_dragHue = true;
			_h = Mathf.Clamp01((mousePosition.x - x) / w);
			_svDirty = true;
			FireColorChanged(flag);
			if (flag)
			{
				current.Use();
			}
		}
	}

	private static void DrawRing(float cx, float cy, float r)
	{
		GUI.color = new Color(0f, 0f, 0f, 0.85f);
		DrawFilledCircle(cx, cy, r + 2f);
		GUI.color = Color.white;
		DrawFilledCircle(cx, cy, r);
		GUI.color = new Color(0f, 0f, 0f, 0f);
		DrawFilledCircle(cx, cy, r - 2.5f);
		GUI.color = Color.white;
	}

	private static void DrawFilledCircle(float cx, float cy, float r)
	{
		if (!(r <= 0f))
		{
			GUI.DrawTexture(new Rect(cx - r, cy - r, r * 2f, r * 2f), Texture2D.whiteTexture);
		}
	}

	private void EnsureSVTex(int w, int h)
	{
		if (w <= 0 || h <= 0)
		{
			return;
		}
		int num = Mathf.RoundToInt(_h * 1000f);
		if (!_svDirty && _svTex != null && _svTex.width == w && _svTex.height == h && _svBuildH == num)
		{
			return;
		}
		_svDirty = false;
		_svBuildH = num;
		if (_svTex == null || _svTex.width != w || _svTex.height != h)
		{
			if (_svTex != null)
			{
				UnityEngine.Object.Destroy(_svTex);
			}
			_svTex = new Texture2D(w, h, TextureFormat.ARGB32, mipChain: false);
			_svTex.filterMode = FilterMode.Bilinear;
			_svTex.wrapMode = TextureWrapMode.Clamp;
		}
		Color[] array = new Color[w * h];
		for (int i = 0; i < h; i++)
		{
			float v = (float)i / (float)(h - 1);
			for (int j = 0; j < w; j++)
			{
				float s = (float)j / (float)(w - 1);
				array[i * w + j] = Color.HSVToRGB(_h, s, v);
			}
		}
		_svTex.SetPixels(array);
		_svTex.Apply();
	}

	private void EnsureHueTex(int w)
	{
		if (w > 0 && (!(_hueTex != null) || _hueTex.width != w))
		{
			if (_hueTex != null)
			{
				UnityEngine.Object.Destroy(_hueTex);
			}
			_hueTex = new Texture2D(w, 1, TextureFormat.ARGB32, mipChain: false);
			_hueTex.filterMode = FilterMode.Bilinear;
			_hueTex.wrapMode = TextureWrapMode.Clamp;
			Color[] array = new Color[w];
			for (int i = 0; i < w; i++)
			{
				array[i] = Color.HSVToRGB((float)i / (float)(w - 1), 1f, 1f);
			}
			_hueTex.SetPixels(array);
			_hueTex.Apply();
		}
	}

	internal void Destroy()
	{
		if (_svTex != null)
		{
			UnityEngine.Object.Destroy(_svTex);
			_svTex = null;
		}
		if (_hueTex != null)
		{
			UnityEngine.Object.Destroy(_hueTex);
			_hueTex = null;
		}
	}
}
