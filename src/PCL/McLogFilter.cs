using System;
using System.Linq;

namespace PCL;

public static class McLogFilter
{
	public static string FilterAccessToken(string raw, char filterChar)
	{
		if (raw.Contains("accessToken "))
		{
			foreach (string item in raw.RegexSearch("(?<=accessToken ([^ ]{5}))[^ ]+(?=[^ ]{5})"))
			{
				raw = raw.Replace(item, new string(filterChar, item.Count()));
			}
		}
		string accessToken = ModLaunch.mcLoginLoader.output.AccessToken;
		if (accessToken != null && accessToken.Length >= 10 && raw.ContainsF(accessToken, ignoreCase: true) && (ModLaunch.mcLoginLoader.output.Uuid ?? "") != (ModLaunch.mcLoginLoader.output.AccessToken ?? ""))
		{
			raw = raw.Replace(accessToken, accessToken.Substring(0, 5) + new string(filterChar, accessToken.Length - 10) + accessToken.Substring(accessToken.Length - 5));
		}
		return raw;
	}

	public static string FilterUserName(string raw, char filterChar)
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		string text = folderPath.Split("\\").Last();
		string newValue = folderPath.Replace(text, new string(filterChar, text.Length));
		return raw.Replace(folderPath, newValue);
	}
}
