using System.Reflection;
using UnityEngine;

namespace CBSafe.Crosshair.Render;

[Obfuscation(Exclude = true, ApplyToMembers = true, Feature = "renaming")]
internal static class CrosshairTextures
{
	private const int CircleSize = 64;

	private static Texture2D _circle;

	private static Sprite _circleSprite;

	public static Texture2D Circle
	{
		get
		{
			if (_circle != null)
			{
				return _circle;
			}
			_circle = new Texture2D(64, 64, TextureFormat.RGBA32, mipChain: false);
			float num = 31.25f;
			float num2 = 32f;
			float num3 = 32f;
			for (int i = 0; i < 64; i++)
			{
				for (int j = 0; j < 64; j++)
				{
					float num4 = (float)j + 0.5f - num2;
					float num5 = (float)i + 0.5f - num3;
					float num6 = Mathf.Sqrt(num4 * num4 + num5 * num5);
					float a = ((num6 <= num) ? 1f : ((num6 <= num + 1f) ? (num + 1f - num6) : 0f));
					_circle.SetPixel(j, i, new Color(1f, 1f, 1f, a));
				}
			}
			_circle.Apply(updateMipmaps: false, makeNoLongerReadable: false);
			_circle.filterMode = FilterMode.Bilinear;
			_circle.wrapMode = TextureWrapMode.Clamp;
			_circle.hideFlags = HideFlags.HideAndDontSave;
			return _circle;
		}
	}

	public static Sprite CircleSprite
	{
		get
		{
			if (_circleSprite != null)
			{
				return _circleSprite;
			}
			Texture2D circle = Circle;
			_circleSprite = Sprite.Create(circle, new Rect(0f, 0f, circle.width, circle.height), new Vector2(0.5f, 0.5f), 100f);
			_circleSprite.hideFlags = HideFlags.HideAndDontSave;
			return _circleSprite;
		}
	}
}
