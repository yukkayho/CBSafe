using System.Collections.Generic;
using CBSafe.Crosshair.Infrastructure;
using UnityEngine;

namespace CBSafe.Crosshair.Render;

public static class CrosshairGeometry
{
	public static void Build(CrosshairConfig cfg, float extraGap, List<CrosshairRect> outRects, out bool hasDot, out CrosshairRect dotRect)
	{
		hasDot = false;
		dotRect = default(CrosshairRect);
		outRects.Clear();
		if (cfg == null)
		{
			return;
		}
		float num = Mathf.Max(0f, cfg.Gap + extraGap);
		float num2 = Mathf.Max(0f, cfg.Length);
		float num3 = Mathf.Max(0f, cfg.Thickness);
		bool flag = cfg.StyleIndex != 1;
		bool flag2 = flag && cfg.StyleIndex != 2;
		bool flag3 = flag;
		bool flag4 = flag;
		bool flag5 = flag;
		if (num2 > 0f && num3 > 0f && flag)
		{
			float y = (0f - num3) * 0.5f;
			if (flag4)
			{
				outRects.Add(new CrosshairRect
				{
					X = 0f - (num + num2),
					Y = y,
					Width = num2,
					Height = num3
				});
			}
			if (flag5)
			{
				outRects.Add(new CrosshairRect
				{
					X = num,
					Y = y,
					Width = num2,
					Height = num3
				});
			}
			float x = (0f - num3) * 0.5f;
			if (flag2)
			{
				outRects.Add(new CrosshairRect
				{
					X = x,
					Y = 0f - (num + num2),
					Width = num3,
					Height = num2
				});
			}
			if (flag3)
			{
				outRects.Add(new CrosshairRect
				{
					X = x,
					Y = num,
					Width = num3,
					Height = num2
				});
			}
		}
		float num4 = Mathf.Max(0f, cfg.DotSize);
		bool flag6;
		if (cfg.StyleIndex != 1)
		{
			if (num4 <= 0.0001f)
			{
				return;
			}
			flag6 = cfg.StyleIndex != 2;
		}
		else
		{
			flag6 = num4 > 0.0001f;
		}
		if (flag6)
		{
			hasDot = true;
			dotRect = new CrosshairRect
			{
				X = (0f - num4) * 0.5f,
				Y = (0f - num4) * 0.5f,
				Width = num4,
				Height = num4
			};
		}
	}
}
