using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CBSafe.Core;

public static class WarmUp
{
	private const BindingFlags AllDeclared = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

	public static void PreJit(params Type[] types)
	{
		if (types == null)
		{
			return;
		}
		foreach (Type type in types)
		{
			if (type == null)
			{
				continue;
			}
			MethodInfo[] methods;
			try
			{
				methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			}
			catch
			{
				continue;
			}
			MethodInfo[] array = methods;
			foreach (MethodInfo methodInfo in array)
			{
				if (!(methodInfo == null) && !methodInfo.IsAbstract && !methodInfo.ContainsGenericParameters)
				{
					try
					{
						RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);
					}
					catch
					{
					}
				}
			}
		}
	}
}
