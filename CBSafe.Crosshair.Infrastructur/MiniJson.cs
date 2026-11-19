using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CBSafe.Crosshair.Infrastructure;

internal static class MiniJson
{
	public static string Serialize(CrosshairConfig c)
	{
		if (c == null)
		{
			return "{}";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{\n");
		WriteBool(stringBuilder, "Enabled", c.Enabled, isLast: false);
		WriteBool(stringBuilder, "DynamicVisibility", c.DynamicVisibility, isLast: false);
		WriteBool(stringBuilder, "DynamicGap", c.DynamicGap, isLast: false);
		WriteInt(stringBuilder, "StyleIndex", c.StyleIndex, isLast: false);
		WriteString(stringBuilder, "MainColor", c.MainColorString, isLast: false);
		WriteString(stringBuilder, "OutlineColor", c.OutlineColorString, isLast: false);
		WriteFloat(stringBuilder, "DotSize", c.DotSize, isLast: false);
		WriteFloat(stringBuilder, "Gap", c.Gap, isLast: false);
		WriteFloat(stringBuilder, "Length", c.Length, isLast: false);
		WriteFloat(stringBuilder, "Thickness", c.Thickness, isLast: false);
		WriteBool(stringBuilder, "OutlineEnabled", c.OutlineEnabled, isLast: true);
		stringBuilder.Append("}\n");
		return stringBuilder.ToString();
	}

	public static CrosshairConfig Deserialize(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return CrosshairConfig.CreateDefaults();
		}
		Dictionary<string, string> dictionary = ParseFlatObject(json);
		if (dictionary == null)
		{
			return CrosshairConfig.CreateDefaults();
		}
		CrosshairConfig crosshairConfig = CrosshairConfig.CreateDefaults();
		if (dictionary.TryGetValue("Enabled", out var value))
		{
			crosshairConfig.Enabled = AsBool(value, crosshairConfig.Enabled);
		}
		if (dictionary.TryGetValue("DynamicVisibility", out var value2))
		{
			crosshairConfig.DynamicVisibility = AsBool(value2, crosshairConfig.DynamicVisibility);
		}
		if (dictionary.TryGetValue("DynamicGap", out var value3))
		{
			crosshairConfig.DynamicGap = AsBool(value3, crosshairConfig.DynamicGap);
		}
		if (dictionary.TryGetValue("StyleIndex", out var value4))
		{
			crosshairConfig.StyleIndex = AsInt(value4, crosshairConfig.StyleIndex);
		}
		if (dictionary.TryGetValue("MainColor", out var value5))
		{
			crosshairConfig.MainColorString = AsString(value5, crosshairConfig.MainColorString);
		}
		if (dictionary.TryGetValue("OutlineColor", out var value6))
		{
			crosshairConfig.OutlineColorString = AsString(value6, crosshairConfig.OutlineColorString);
		}
		if (dictionary.TryGetValue("DotSize", out var value7))
		{
			crosshairConfig.DotSize = AsFloat(value7, crosshairConfig.DotSize);
		}
		if (dictionary.TryGetValue("Gap", out var value8))
		{
			crosshairConfig.Gap = AsFloat(value8, crosshairConfig.Gap);
		}
		if (dictionary.TryGetValue("Length", out var value9))
		{
			crosshairConfig.Length = AsFloat(value9, crosshairConfig.Length);
		}
		if (dictionary.TryGetValue("Thickness", out var value10))
		{
			crosshairConfig.Thickness = AsFloat(value10, crosshairConfig.Thickness);
		}
		if (dictionary.TryGetValue("OutlineEnabled", out var value11))
		{
			crosshairConfig.OutlineEnabled = AsBool(value11, crosshairConfig.OutlineEnabled);
		}
		return crosshairConfig;
	}

	private static void WriteBool(StringBuilder sb, string key, bool value, bool isLast)
	{
		sb.Append("  \"").Append(key).Append("\": ")
			.Append(value ? "true" : "false");
		sb.Append(isLast ? "\n" : ",\n");
	}

	private static void WriteInt(StringBuilder sb, string key, int value, bool isLast)
	{
		sb.Append("  \"").Append(key).Append("\": ")
			.Append(value.ToString(CultureInfo.InvariantCulture));
		sb.Append(isLast ? "\n" : ",\n");
	}

	private static void WriteString(StringBuilder sb, string key, string value, bool isLast)
	{
		sb.Append("  \"").Append(key).Append("\": \"")
			.Append(value)
			.Append("\"");
		sb.Append(isLast ? "\n" : ",\n");
	}

	private static void WriteFloat(StringBuilder sb, string key, float value, bool isLast)
	{
		sb.Append("  \"").Append(key).Append("\": ")
			.Append(value.ToString("0.######", CultureInfo.InvariantCulture));
		sb.Append(isLast ? "\n" : ",\n");
	}

	private static Dictionary<string, string> ParseFlatObject(string json)
	{
		int i = 0;
		SkipWs(json, ref i);
		if (i >= json.Length || json[i] != '{')
		{
			return null;
		}
		i++;
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		while (true)
		{
			SkipWs(json, ref i);
			if (i >= json.Length || json[i] == '}')
			{
				return dictionary;
			}
			string text = ReadString(json, ref i);
			if (text == null)
			{
				break;
			}
			SkipWs(json, ref i);
			if (i < json.Length && json[i] == ':')
			{
				i++;
			}
			SkipWs(json, ref i);
			string text2 = ReadScalar(json, ref i);
			if (text2 != null)
			{
				dictionary[text] = text2;
			}
			SkipWs(json, ref i);
			if (i < json.Length && json[i] == ',')
			{
				i++;
			}
			else if (i < json.Length && json[i] == '}')
			{
				return dictionary;
			}
		}
		return dictionary;
	}

	private static void SkipWs(string s, ref int i)
	{
		while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r'))
		{
			i++;
		}
	}

	private static string ReadString(string s, ref int i)
	{
		if (i >= s.Length || s[i] != '"')
		{
			return null;
		}
		i++;
		StringBuilder stringBuilder = new StringBuilder();
		while (i < s.Length && s[i] != '"')
		{
			if (s[i] == '\\' && i + 1 < s.Length)
			{
				i++;
				char c = s[i];
				if (c <= '\\')
				{
					if (c != '"')
					{
						if (c != '/')
						{
							if (c != '\\')
							{
								goto IL_00d7;
							}
							stringBuilder.Append('\\');
						}
						else
						{
							stringBuilder.Append('/');
						}
					}
					else
					{
						stringBuilder.Append('"');
					}
				}
				else if (c != 'n')
				{
					if (c != 'r')
					{
						if (c != 't')
						{
							goto IL_00d7;
						}
						stringBuilder.Append('\t');
					}
					else
					{
						stringBuilder.Append('\r');
					}
				}
				else
				{
					stringBuilder.Append('\n');
				}
				goto IL_00cf;
			}
			stringBuilder.Append(s[i]);
			i++;
			continue;
			IL_00cf:
			i++;
			continue;
			IL_00d7:
			stringBuilder.Append(s[i]);
			goto IL_00cf;
		}
		if (i < s.Length && s[i] == '"')
		{
			i++;
		}
		return stringBuilder.ToString();
	}

	private static string ReadScalar(string s, ref int i)
	{
		if (i >= s.Length)
		{
			return null;
		}
		if (s[i] == '"')
		{
			return ReadString(s, ref i);
		}
		int num = i;
		while (i < s.Length && s[i] != ',' && s[i] != '}' && s[i] != '\n' && s[i] != '\r')
		{
			i++;
		}
		return s.Substring(num, i - num).Trim();
	}

	private static bool AsBool(string v, bool fallback)
	{
		if (v == null)
		{
			return fallback;
		}
		v = v.Trim();
		if (!v.Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			return !v.Equals("false", StringComparison.OrdinalIgnoreCase) && fallback;
		}
		return true;
	}

	private static int AsInt(string v, int fallback)
	{
		if (v == null)
		{
			return fallback;
		}
		if (!int.TryParse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}

	private static string AsString(string v, string fallback)
	{
		if (v == null)
		{
			return fallback;
		}
		return v;
	}

	private static float AsFloat(string v, float fallback)
	{
		if (v == null)
		{
			return fallback;
		}
		if (!float.TryParse(v.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}
}
