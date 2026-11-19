using System;
using System.Collections.Generic;
using System.Reflection;
using CBSafe.Crosshair.Infrastructure;
using CBSafe.Crosshair.Render;
using UnityEngine;
using UnityEngine.UI;

namespace CBSafe.Crosshair.UI;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
[HydraNoObfuscate]
public class PreviewRenderer : MonoBehaviour
{
	private RectTransform _holder;

	private readonly List<GameObject> _children = new List<GameObject>();

	private readonly List<CrosshairRect> _rects = new List<CrosshairRect>();

	private bool _needsRebuild = true;

	private bool _subscribed;

	public PreviewRenderer(IntPtr ptr)
		: base(ptr)
	{
	}

	public void Bind(RectTransform holder)
	{
		_holder = holder;
		_needsRebuild = true;
		if (!_subscribed)
		{
			_subscribed = true;
			CrosshairConfigStore.Subscribe(OnConfigChanged);
		}
	}

	public void MarkDirty()
	{
		_needsRebuild = true;
	}

	private void OnConfigChanged()
	{
		_needsRebuild = true;
	}

	private void OnEnable()
	{
		_needsRebuild = true;
	}

	private void OnDisable()
	{
		ClearChildren();
		_needsRebuild = true;
	}

	private void OnDestroy()
	{
		ClearChildren();
	}

	private void LateUpdate()
	{
		if (!(_holder == null) && _needsRebuild)
		{
			_needsRebuild = false;
			Rebuild();
		}
	}

	private void Rebuild()
	{
		ClearChildren();
		CrosshairConfig current = CrosshairConfigStore.Current;
		if (current == null)
		{
			return;
		}
		float scale = 3f;
		CrosshairRenderMath.BuildPixelRects(current, 0f, scale, _rects, out var hasDot, out var dotRect);
		Color mainColor = current.MainColor;
		Color outlineColor = current.OutlineColor;
		int num = CrosshairRenderMath.OutlinePx(scale);
		if (current.OutlineEnabled)
		{
			float outlinePx = num;
			foreach (CrosshairRect rect in _rects)
			{
				CrosshairRect crosshairRect = CrosshairRenderMath.ExpandOutlineOutward(rect, outlinePx);
				AddChild(crosshairRect.X, crosshairRect.Y, crosshairRect.Width, crosshairRect.Height, outlineColor);
			}
			if (hasDot)
			{
				int diameterPx = CrosshairRenderMath.EvenSize(Mathf.RoundToInt(dotRect.Width) + num * 2);
				AddDotChild(diameterPx, outlineColor);
			}
		}
		foreach (CrosshairRect rect2 in _rects)
		{
			AddChild(rect2.X, rect2.Y, rect2.Width, rect2.Height, mainColor);
		}
		if (hasDot)
		{
			AddDotChild(Mathf.RoundToInt(dotRect.Width), mainColor);
		}
	}

	private void ClearChildren()
	{
		for (int i = 0; i < _children.Count; i++)
		{
			GameObject gameObject = _children[i];
			if (gameObject != null)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
		_children.Clear();
	}

	private void AddChild(float x, float y, float w, float h, Color color)
	{
		CrosshairRenderMath.SnapSymmetricRect(x, y, w, h, out var cx, out var cy, out var wi, out var hi);
		GameObject gameObject = new GameObject("CrosshairPreviewPart");
		gameObject.transform.SetParent(_holder, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.anchoredPosition = new Vector2(cx, cy);
		rectTransform.sizeDelta = new Vector2(wi, hi);
		Image image = gameObject.AddComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		_children.Add(gameObject);
	}

	private void AddDotChild(int diameterPx, Color color)
	{
		int num = CrosshairRenderMath.EvenSize(diameterPx);
		GameObject gameObject = new GameObject("CrosshairPreviewDot");
		gameObject.transform.SetParent(_holder, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.anchoredPosition = Vector2.zero;
		rectTransform.sizeDelta = new Vector2(num, num);
		Image image = gameObject.AddComponent<Image>();
		image.sprite = CrosshairTextures.CircleSprite;
		image.type = Image.Type.Simple;
		image.preserveAspect = true;
		image.color = color;
		image.raycastTarget = false;
		_children.Add(gameObject);
	}
}
