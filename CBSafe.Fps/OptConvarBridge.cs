using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace CBSafe.Fps;

internal static class OptConvarBridge
{
	private static readonly Dictionary<string, MemberInfo> Members = new Dictionary<string, MemberInfo>();

	private static MemberInfo FindMember(string typeName, string memberName)
	{
		string key = typeName + ":" + memberName;
		if (Members.TryGetValue(key, out var value))
		{
			return value;
		}
		Type type = AccessTools.TypeByName("ConVar." + typeName) ?? AccessTools.TypeByName(typeName);
		if (type == null)
		{
			Members[key] = null;
			return null;
		}
		value = (MemberInfo)(((object)type.GetField(memberName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) ?? ((object)type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)));
		Members[key] = value;
		return value;
	}

	private static object Read(string typeName, string memberName)
	{
		MemberInfo memberInfo = FindMember(typeName, memberName);
		FieldInfo fieldInfo = memberInfo as FieldInfo;
		if (fieldInfo != null)
		{
			return fieldInfo.GetValue(null);
		}
		PropertyInfo propertyInfo = memberInfo as PropertyInfo;
		if (propertyInfo != null && propertyInfo.CanRead)
		{
			return propertyInfo.GetValue(null, null);
		}
		return null;
	}

	private static bool Write(string typeName, string memberName, object value)
	{
		MemberInfo memberInfo = FindMember(typeName, memberName);
		FieldInfo fieldInfo = memberInfo as FieldInfo;
		if (fieldInfo != null && !fieldInfo.IsInitOnly)
		{
			fieldInfo.SetValue(null, value);
			return true;
		}
		PropertyInfo propertyInfo = memberInfo as PropertyInfo;
		if (propertyInfo != null && propertyInfo.CanWrite)
		{
			propertyInfo.SetValue(null, value, null);
			return true;
		}
		return false;
	}

	public static bool TryGetBool(string typeName, string memberName, out bool value)
	{
		try
		{
			object obj = Read(typeName, memberName);
			if (obj == null)
			{
				value = false;
				return false;
			}
			value = Convert.ToBoolean(obj);
			return true;
		}
		catch
		{
			value = false;
			return false;
		}
	}

	public static int GetInt(string typeName, string memberName, int fallback)
	{
		try
		{
			object obj = Read(typeName, memberName);
			return (obj == null) ? fallback : Convert.ToInt32(obj);
		}
		catch
		{
			return fallback;
		}
	}

	public static bool SetInt(string typeName, string memberName, int value)
	{
		try
		{
			MemberInfo memberInfo = FindMember(typeName, memberName);
			Type type = null;
			FieldInfo fieldInfo = memberInfo as FieldInfo;
			if (fieldInfo != null)
			{
				type = fieldInfo.FieldType;
			}
			PropertyInfo propertyInfo = memberInfo as PropertyInfo;
			if (propertyInfo != null)
			{
				type = propertyInfo.PropertyType;
			}
			object value2 = ((type == typeof(bool)) ? ((object)(value != 0)) : ((object)value));
			return Write(typeName, memberName, value2);
		}
		catch
		{
			return false;
		}
	}

	public static bool GetBool(string typeName, string memberName, bool fallback)
	{
		if (!TryGetBool(typeName, memberName, out var value))
		{
			return fallback;
		}
		return value;
	}

	public static bool SetBool(string typeName, string memberName, bool value)
	{
		try
		{
			return Write(typeName, memberName, value);
		}
		catch
		{
			return false;
		}
	}
}
