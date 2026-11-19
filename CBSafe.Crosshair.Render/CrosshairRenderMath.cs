using System.Collections.Generic;
using System.Reflection;
using CBSafe.Crosshair.Infrastructure;
using UnityEngine;

namespace CBSafe.Crosshair.Render;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
internal static class CrosshairRenderMath
{
	public const float GameScale = 2.5f;

	public const float PreviewScale = 3f;

	public static int ToPx(float value, float scale)
	{
		return Mathf.Max(Mathf.RoundToInt(value * scale), 1);
	}

	public static int EvenSize(int px)
	{
		int num = Mathf.Max(px, 2);
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			num++;
		}
		return num;
	}

	public static void GetScreenCenter(CrosshairRect rect, out float centerX, out float centerY)
	{
		centerX = rect.X + rect.Width * 0.5f;
		centerY = 0f - (rect.Y + rect.Height * 0.5f);
	}

	public static int GapPx(CrosshairConfig cfg, float extraGap, float scale)
	{
		if (cfg == null)
		{
			return 0;
		}
		return Mathf.Max(Mathf.RoundToInt((cfg.Gap + extraGap) * scale), 0);
	}

	public static void BuildPixelRects(CrosshairConfig cfg, float extraGap, float scale, List<CrosshairRect> outRects, out bool hasDot, out CrosshairRect dotRect)
	{
		hasDot = false;
		dotRect = default(CrosshairRect);
		outRects.Clear();
		if (cfg == null || scale <= 0f)
		{
			return;
		}
		int num = GapPx(cfg, extraGap, scale);
		int num2 = ToPx(cfg.Length, scale);
		int num3 = EvenSize(ToPx(cfg.Thickness, scale));
		bool flag = cfg.StyleIndex != 1;
		bool flag2 = flag && cfg.StyleIndex != 2;
		bool flag3 = flag;
		bool flag4 = flag;
		bool flag5 = flag;
		if (num2 > 0 && num3 > 0 && flag)
		{
			float num4 = (float)num3 * 0.5f;
			if (flag4)
			{
				outRects.Add(new CrosshairRect
				{
					X = -(num + num2),
					Y = 0f - num4,
					Width = num2,
					Height = num3
				});
			}
			if (flag5)
			{
				outRects.Add(new CrosshairRect
				{
					X = num,
					Y = 0f - num4,
					Width = num2,
					Height = num3
				});
			}
			if (flag2)
			{
				outRects.Add(new CrosshairRect
				{
					X = 0f - num4,
					Y = -(num + num2),
					Width = num3,
					Height = num2
				});
			}
			if (flag3)
			{
				outRects.Add(new CrosshairRect
				{
					X = 0f - num4,
					Y = num,
					Width = num3,
					Height = num2
				});
			}
		}
		float num5 = Mathf.Max(0f, cfg.DotSize);
		bool num6;
		if (cfg.StyleIndex != 1)
		{
			if (!(num5 > 0.0001f))
			{
				return;
			}
			num6 = cfg.StyleIndex != 2;
		}
		else
		{
			num6 = num5 > 0.0001f;
		}
		if (num6)
		{
			int num7 = EvenSize(ToPx(num5, scale));
			hasDot = true;
			dotRect = new CrosshairRect
			{
				Width = num7,
				Height = num7
			};
		}
	}

	public static int OutlinePx(float scale)
	{
		return Mathf.Max(Mathf.RoundToInt(0.5f * scale), 1);
	}

	public static CrosshairRect ExpandOutlineOutward(CrosshairRect rect, float outlinePx)
	{
		float num = rect.X + rect.Width * 0.5f;
		float num2 = rect.Y + rect.Height * 0.5f;
		float num3 = outlinePx;
		float num4 = outlinePx;
		float num5 = outlinePx;
		float num6 = outlinePx;
		if (rect.Width > rect.Height + 0.01f)
		{
			if (num < -0.01f)
			{
				num4 = 0f;
			}
			else if (num > 0.01f)
			{
				num3 = 0f;
			}
		}
		else if (rect.Height > rect.Width + 0.01f)
		{
			if (num2 < -0.01f)
			{
				num6 = 0f;
			}
			else if (num2 > 0.01f)
			{
				num5 = 0f;
			}
		}
		CrosshairRect result = default(CrosshairRect);
		result.X = rect.X - num3;
		result.Y = rect.Y - num5;
		result.Width = rect.Width + num3 + num4;
		result.Height = rect.Height + num5 + num6;
		return result;
	}

	public static void SnapSymmetricRect(float x, float y, float w, float h, out float cx, out float cy, out int wi, out int hi)
	{
		wi = Mathf.Max(Mathf.RoundToInt(w), 1);
		hi = Mathf.Max(Mathf.RoundToInt(h), 1);
		cx = Mathf.Round((x + w * 0.5f) * 2f) * 0.5f;
		cy = Mathf.Round((0f - (y + h * 0.5f)) * 2f) * 0.5f;
	}
}
