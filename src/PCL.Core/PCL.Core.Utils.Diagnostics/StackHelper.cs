using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace PCL.Core.Utils.Diagnostics;

public static class StackHelper
{
	private const string Unknown = "<unknown>";

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static string GetDirectCallerName(bool includeNamespace = true, bool includeParameters = false, int skipAppFrames = 0)
	{
		MethodBase methodBase = new StackTrace(1, fNeedFileInfo: false).GetFrame(skipAppFrames)?.GetMethod();
		if ((object)methodBase == null)
		{
			return "<unknown>";
		}
		methodBase = _TryMapAsyncOrIterator(methodBase);
		return _FormatMethod(methodBase, includeNamespace, includeParameters);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static IReadOnlyList<string> GetStack(int maxFrames = 10, bool includeNamespace = true, bool includeParameters = false, bool needFileInfo = false)
	{
		if (maxFrames <= 0)
		{
			return Array.Empty<string>();
		}
		StackTrace stackTrace = new StackTrace(1, needFileInfo);
		List<string> list = new List<string>(Math.Min(maxFrames, stackTrace.FrameCount));
		int i = 0;
		int num = 0;
		for (; i < stackTrace.FrameCount; i++)
		{
			if (num >= maxFrames)
			{
				break;
			}
			MethodBase methodBase = stackTrace.GetFrame(i)?.GetMethod();
			if ((object)methodBase == null)
			{
				continue;
			}
			methodBase = _TryMapAsyncOrIterator(methodBase);
			string text = _FormatMethod(methodBase, includeNamespace, includeParameters);
			if (needFileInfo)
			{
				StackFrame? frame = stackTrace.GetFrame(i);
				string text2 = frame?.GetFileName();
				int num2 = frame?.GetFileLineNumber() ?? 0;
				if (!string.IsNullOrEmpty(text2) && num2 > 0)
				{
					text = $"{text} ({Path.GetFileName(text2)}:{num2})";
				}
			}
			list.Add(text);
			num++;
		}
		return list;
	}

	private static MethodBase _TryMapAsyncOrIterator(MethodBase method)
	{
		if (method.Name != "MoveNext")
		{
			return method;
		}
		Type declaringType = method.DeclaringType;
		if ((object)declaringType == null)
		{
			return method;
		}
		string name = declaringType.Name;
		int num = name.IndexOf('<');
		int num2 = name.IndexOf('>');
		if (num >= 0 && num2 > num + 1)
		{
			string b = name.Substring(num + 1, num2 - num - 1);
			MethodInfo[] methods = (declaringType.DeclaringType ?? declaringType).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (string.Equals(methodInfo.Name, b, StringComparison.Ordinal))
				{
					return methodInfo;
				}
			}
		}
		return method;
	}

	private static string _FormatMethod(MethodBase method, bool includeNamespace, bool includeParameters, bool includeParameterNamespace = false)
	{
		StringBuilder stringBuilder = new StringBuilder();
		Type declaringType = method.DeclaringType;
		if ((object)declaringType != null)
		{
			stringBuilder.Append(_FormatTypeName(declaringType, includeNamespace));
			stringBuilder.Append('.');
		}
		stringBuilder.Append(method.Name);
		if (method is MethodInfo methodInfo && method.IsGenericMethod)
		{
			stringBuilder.Append('[');
			Type[] genericArguments = methodInfo.GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(_FormatTypeName(genericArguments[i], includeNamespace: false));
			}
			stringBuilder.Append(']');
		}
		if (includeParameters)
		{
			ParameterInfo[] parameters = method.GetParameters();
			stringBuilder.Append('(');
			for (int j = 0; j < parameters.Length; j++)
			{
				if (j > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(_FormatTypeName(parameters[j].ParameterType, includeParameterNamespace));
				if (!string.IsNullOrEmpty(parameters[j].Name))
				{
					stringBuilder.Append(' ').Append(parameters[j].Name);
				}
			}
			stringBuilder.Append(')');
		}
		return stringBuilder.ToString();
	}

	private static string _FormatTypeName(Type t, bool includeNamespace)
	{
		if (t.IsGenericType)
		{
			Type genericTypeDefinition = t.GetGenericTypeDefinition();
			string text = genericTypeDefinition.Name;
			int num = text.IndexOf('`');
			if (num >= 0)
			{
				text = text.Substring(0, num);
			}
			string? value;
			if (includeNamespace)
			{
				value = ((genericTypeDefinition.Namespace == null) ? "" : (genericTypeDefinition.Namespace + "."));
			}
			else
			{
				value = "";
			}
			StringBuilder stringBuilder = new StringBuilder(value).Append(text).Append('[');
			Type[] genericArguments = t.GetGenericArguments();
			for (int i = 0; i < genericArguments.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(_FormatTypeName(genericArguments[i], includeNamespace: false));
			}
			stringBuilder.Append(']');
			return stringBuilder.ToString();
		}
		if (!includeNamespace || t.Namespace == null)
		{
			return t.Name;
		}
		return t.Namespace + "." + t.Name;
	}
}
