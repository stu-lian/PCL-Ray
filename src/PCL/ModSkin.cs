using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using PCL.Core.App.Localization;
using PCL.Core.Minecraft.Profile;
using PCL.Core.Minecraft.Profile.Models;
using PCL.Core.UI;
using PCL.Network;

namespace PCL;

public static class ModSkin
{
	public struct McSkinInfo
	{
		public bool IsSlim;

		public string LocalFile;

		public bool IsVaild;
	}

	private static readonly object mcSkinDownloadLock = new object();

	public static McSkinInfo McSkinSelect()
	{
		string text = SystemDialogs.SelectFile(Lang.Text("Launch.Skin.FileDialog.Filter"), Lang.Text("Launch.Skin.FileDialog.Title"), (string)null);
		if (string.IsNullOrEmpty(text))
		{
			return new McSkinInfo
			{
				IsVaild = false
			};
		}
		try
		{
			MyBitmap myBitmap = new MyBitmap(text);
			if (myBitmap.pic.Width != 64 || (myBitmap.pic.Height != 32 && myBitmap.pic.Height != 64))
			{
				HintService.Hint(Lang.Text("Launch.Skin.InvalidSize"), HintType.Error);
				return new McSkinInfo
				{
					IsVaild = false
				};
			}
			FileInfo fileInfo = new FileInfo(text);
			if (fileInfo.Length > 24576)
			{
				HintService.Hint(Lang.Text("Launch.Skin.FileTooLarge", new object[1] { Lang.Number<double>((double)fileInfo.Length / 1024.0, "N2") }), HintType.Error);
				return new McSkinInfo
				{
					IsVaild = false
				};
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, Lang.Text("Launch.Skin.File.Error"), ModBase.LogLevel.Hint, null, Lang.Text("Launch.Skin.File.Error"));
			return new McSkinInfo
			{
				IsVaild = false
			};
		}
		int num = ModMain.MyMsgBox(Lang.Text("Launch.Skin.Model.SelectMessage"), Lang.Text("Launch.Skin.Model.SelectTitle"), Lang.Text("Launch.Skin.Model.Steve"), Lang.Text("Launch.Skin.Model.Alex"), Lang.Text("Common.Option.IDontKnow"), isWarn: false, highLight: false);
		if (num == 3)
		{
			HintService.Hint(Lang.Text("Launch.Skin.Model.UnknownHint"));
			return new McSkinInfo
			{
				IsVaild = false
			};
		}
		return new McSkinInfo
		{
			IsVaild = true,
			IsSlim = (num == 2),
			LocalFile = text
		};
	}

	public static string McSkinGetAddress(string uuid, string type)
	{
		if (string.IsNullOrEmpty(uuid))
		{
			throw new ArgumentException(Lang.Text("Minecraft.Skin.Error.UuidEmpty"), "uuid");
		}
		if (uuid.StartsWith("00000"))
		{
			throw new InvalidOperationException(Lang.Text("Minecraft.Skin.Error.OfflineNoSkin"));
		}
		string fileName = Path.Combine(ModBase.pathTemp, "Cache\\Skin\\Index" + type + ".ini");
		string text = ModBase.ReadIni(fileName, uuid);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		string text2;
		switch (type)
		{
		case "Mojang":
			text2 = "https://sessionserver.mojang.com/session/minecraft/profile/";
			break;
		case "Ms":
			text2 = "https://sessionserver.mojang.com/session/minecraft/profile/";
			break;
		case "Auth":
		{
			McProfile current = ProfileService.Current;
			text2 = (((current != null) ? current.Server : null) ?? string.Empty).Replace("/authserver", "") + "/sessionserver/session/minecraft/profile/";
			break;
		}
		default:
			throw new ArgumentException(Lang.Text("Minecraft.Skin.Error.InvalidSkinType", new object[1] { type ?? "null" }));
		}
		object obj = ModNet.NetGetCodeByRequestRetry(text2 + uuid);
		if (string.IsNullOrEmpty((string)obj))
		{
			throw new InvalidDataException(Lang.Text("Minecraft.Skin.Error.SkinReturnEmpty"));
		}
		string text3 = null;
		try
		{
			foreach (JsonNode item in ((JsonObject)ModBase.GetJson((string)obj))["properties"].AsArray())
			{
				if (item["name"]?.ToString() == "textures")
				{
					text3 = item["value"]?.ToString();
					break;
				}
			}
			if (text3 == null)
			{
				throw new InvalidDataException(Lang.Text("Minecraft.Skin.Error.PropertyNotFound"));
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, $"无法完成解析的皮肤返回值，可能是未设置自定义皮肤的用户：{obj}", ModBase.LogLevel.Developer);
			throw new InvalidDataException(Lang.Text("Minecraft.Skin.Error.NoSkinData"), ex);
		}
		JsonObject jsonObject = (JsonObject)ModBase.GetJson(Encoding.UTF8.GetString(Convert.FromBase64String(text3)).ToLowerInvariant());
		if (jsonObject["textures"]?["skin"]?["url"] == null)
		{
			throw new InvalidDataException(Lang.Text("Minecraft.Skin.Error.NoCustomSkin"));
		}
		string text4 = jsonObject["textures"]["skin"]["url"].ToString();
		text4 = (text4.Contains("minecraft.net/") ? text4.Replace("http://", "https://") : text4);
		ModBase.WriteIni(fileName, uuid, text4);
		ModBase.Log("[Skin] UUID " + uuid + " 对应的皮肤文件为 " + text4);
		return text4;
	}

	public static string McSkinDownload(string address)
	{
		ModBase.GetFileNameFromPath(address);
		string text = ModBase.pathTemp + "Cache\\Skin\\" + ModBase.GetHash(address) + ".png";
		lock (mcSkinDownloadLock)
		{
			if (!File.Exists(text))
			{
				FileDownloader.DownloadAsync(address, text).GetAwaiter().GetResult();
				ModBase.Log("[Minecraft] 皮肤下载成功：" + text);
			}
			return text;
		}
	}

	public static string McSkinSex(string uuid)
	{
		if (uuid.Length != 32)
		{
			return "Steve";
		}
		int num = int.Parse(uuid[7].ToString(), NumberStyles.AllowHexSpecifier);
		int num2 = int.Parse(uuid[15].ToString(), NumberStyles.AllowHexSpecifier);
		int num3 = int.Parse(uuid[23].ToString(), NumberStyles.AllowHexSpecifier);
		int num4 = int.Parse(uuid[31].ToString(), NumberStyles.AllowHexSpecifier);
		if ((num ^ num2 ^ num3 ^ num4) % 2 == 0)
		{
			return "Steve";
		}
		return "Alex";
	}
}
