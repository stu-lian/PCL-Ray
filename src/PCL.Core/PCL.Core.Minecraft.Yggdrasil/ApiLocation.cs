using System;
using System.Net.Http;
using System.Threading.Tasks;
using PCL.Core.IO.Net.Http;

namespace PCL.Core.Minecraft.Yggdrasil;

public static class ApiLocation
{
	public static async Task<string> TryRequestAsync(string address)
	{
		string originAddr = (address.StartsWith("http") ? address : ("https://" + address));
		Uri originUri = new Uri(originAddr);
		using HttpResponseMessage responseMessage = await HttpRequest.CreateHead(originAddr).SendAsync();
		if (!responseMessage.TryGetHeader("X-Authlib-Injector-Api-Location", out string[] values) || values.Length == 0)
		{
			return originAddr;
		}
		string text = values[0];
		if (string.IsNullOrEmpty(text))
		{
			return originAddr;
		}
		if (text.StartsWith(originUri.Scheme))
		{
			return text;
		}
		if (text.StartsWith("http:") && originUri.Scheme == "https")
		{
			return text.Replace("http", "https");
		}
		return new Uri(originUri, text).ToString();
	}
}
