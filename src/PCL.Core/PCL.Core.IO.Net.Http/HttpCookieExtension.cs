using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace PCL.Core.IO.Net.Http;

public static class HttpCookieExtension
{
	extension(HttpRequestMessage requestMessage)
	{
		public HttpRequestMessage WithCookie(string name, string value)
		{
			ArgumentNullException.ThrowIfNull(requestMessage, "requestMessage");
			ArgumentException.ThrowIfNullOrEmpty(value, "value");
			ArgumentNullException.ThrowIfNull(value, "value");
			string text = name + "=" + _GetSafeCookieValue(value);
			if (requestMessage.Headers.TryGetValues("Cookie", out IEnumerable<string> values))
			{
				string text2 = string.Join("; ", values);
				requestMessage.Headers.Remove("Cookie");
				requestMessage.Headers.Add("Cookie", text2 + "; " + text);
			}
			else
			{
				requestMessage.Headers.Add("Cookie", text);
			}
			return requestMessage;
		}
	}

	private static readonly char[] _ForbiddenCookieValueChar = new char[13]
	{
		';', ',', ' ', '\r', '\n', '\t', '\0', '=', '"', '\'',
		'\\', '<', '>'
	};

	private static string _GetSafeCookieValue(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		if (!value.Any((char c) => _ForbiddenCookieValueChar.Contains(c) || char.IsControl(c)))
		{
			return value;
		}
		return Uri.EscapeDataString(value);
	}
}
