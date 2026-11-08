using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public static class ModFolder
{
	public class McFolder
	{
		public enum Types
		{
			Original,
			RenamedOriginal,
			Custom
		}

		public string Location;

		public string Name;

		public Types type;

		public override bool Equals(object obj)
		{
			if (!(obj is McFolder))
			{
				return false;
			}
			McFolder mcFolder = (McFolder)obj;
			if ((Name ?? "") == (mcFolder.Name ?? "") && (Location ?? "") == (mcFolder.Location ?? ""))
			{
				return type == mcFolder.type;
			}
			return false;
		}

		public override string ToString()
		{
			return Location;
		}
	}

	public static string mcFolderSelected;

	public static List<McFolder> mcFolderList = new List<McFolder>();

	public static ModLoader.LoaderTask<int, int> mcFolderListLoader = new ModLoader.LoaderTask<int, int>("Minecraft Folder List", (ModLoader.LoaderTask<int, int> _) =>
	{
		McFolderListLoadSub();
	}, null, ThreadPriority.AboveNormal);

	private static void McFolderListLoadSub()
	{
		try
		{
			List<McFolder> list = new List<McFolder>();
			foreach (string item2 in ((dynamic)States.Game.Folders).Split("|"))
			{
				if (string.IsNullOrEmpty(item2))
				{
					continue;
				}
				if (!item2.Contains(">") || !item2.EndsWithF("\\"))
				{
					HintService.Hint(Lang.Text("Select.Folder.Invalid", new object[1] { item2 }), HintType.Error);
					continue;
				}
				string name = item2.Split(">")[0];
				string text2 = item2.Split(">")[1];
				try
				{
					ModBase.CheckPermissionWithException(text2);
					list.Add(new McFolder
					{
						Name = name,
						Location = text2,
						type = McFolder.Types.Custom
					});
				}
				catch (Exception ex)
				{
					ModMain.MyMsgBox(Lang.Text("Select.Folder.Invalid.WithDetail", new object[2]
					{
						text2,
						ex.ToString()
					}), Lang.Text("Select.Folder.InvalidTitle"), null, "", "", isWarn: true);
					ModBase.Log(ex, "无法访问 Minecraft 文件夹 " + text2);
				}
			}
			List<McFolder> list2 = new List<McFolder>();
			List<McFolder> list3 = new List<McFolder>();
			try
			{
				if (Directory.Exists(ModBase.exePath + "versions\\"))
				{
					list3.Add(new McFolder
					{
						Name = Lang.Text("Select.Folder.CurrentFolder"),
						Location = ModBase.exePath,
						type = McFolder.Types.Original
					});
				}
				DirectoryInfo[] directories = new DirectoryInfo(ModBase.exePath).GetDirectories();
				foreach (DirectoryInfo directoryInfo in directories)
				{
					if (Directory.Exists(Path.Combine(directoryInfo.FullName, "versions")) || directoryInfo.Name == ".minecraft")
					{
						McFolder item = new McFolder
						{
							Name = directoryInfo.Name,
							Location = directoryInfo.FullName + "\\",
							type = McFolder.Types.Original
						};
						list3.Add(item);
						list2.Add(item);
					}
				}
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "扫描 PCL 所在文件夹中是否有 MC 文件夹失败");
			}
			string text3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft") + "\\";
			if ((!list2.Any() || (text3 ?? "") != (list2[0].Location ?? "")) && Directory.Exists(Path.Combine(text3, "versions")))
			{
				list3.Add(new McFolder
				{
					Name = Lang.Text("Select.Folder.OfficialLauncherFolder"),
					Location = text3,
					type = McFolder.Types.Original
				});
			}
			ModBase.Log(list.Count + " 个自定义文件夹，" + list3.Count + " 个原始文件夹");
			foreach (McFolder item3 in list3)
			{
				bool flag = true;
				foreach (McFolder item4 in list)
				{
					if ((item4.Location ?? "") == (item3.Location ?? ""))
					{
						if ((item4.Name ?? "") != (item3.Name ?? ""))
						{
							item4.type = McFolder.Types.RenamedOriginal;
						}
						else
						{
							item4.type = McFolder.Types.Original;
						}
						flag = false;
					}
				}
				if (flag)
				{
					list.Add(item3);
				}
			}
			List<string> list4 = new List<string>();
			foreach (McFolder item5 in list)
			{
				list4.Add(item5.Name + ">" + item5.Location);
			}
			if (!list4.Any())
			{
				list4.Add("");
			}
			States.Game.Folders = list4.Join("|");
			if (!list.Any())
			{
				Directory.CreateDirectory(ModBase.exePath + ".minecraft\\versions\\");
				list.Add(new McFolder
				{
					Name = Lang.Text("Select.Folder.CurrentFolder"),
					Location = ModBase.exePath + ".minecraft\\",
					type = McFolder.Types.Original
				});
			}
			foreach (McFolder item6 in list)
			{
				McFolderLauncherProfilesJsonCreate(item6.Location);
			}
			if (Config.Debug.AddRandomDelay)
			{
				Thread.Sleep(RandomUtils.NextInt(200, 2000));
			}
			mcFolderList = list;
		}
		catch (Exception ex3)
		{
			ModBase.Log(ex3, Lang.Text("Select.Folder.Error.Load"), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Folder.Error.Load"));
		}
	}

	public static void McFolderLauncherProfilesJsonCreate(string folder)
	{
		try
		{
			if (!File.Exists(Path.Combine(folder, "launcher_profiles.json")))
			{
				DateTime now = DateTime.Now;
				string text = "{\n    \"profiles\":  {\n        \"PCL\": {\n            \"icon\": \"Grass\",\n            \"name\": \"PCL\",\n            \"lastVersionId\": \"latest-release\",\n            \"type\": \"latest-release\",\n            \"lastUsed\": \"" + now.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture) + "T" + now.ToString("HH':'mm':'ss", CultureInfo.InvariantCulture) + ".0000Z\"\n        }\n    },\n    \"selectedProfile\": \"PCL\",\n    \"clientToken\": \"23323323323323323323323323323333\"\n}";
				ModBase.WriteFile(Path.Combine(folder, "launcher_profiles.json"), text, append: false, Encoding.GetEncoding("GB18030"));
				ModBase.Log("[Minecraft] 已创建 launcher_profiles.json：" + folder);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "创建 launcher_profiles.json 失败（" + folder + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Folder.Error.OperationFailed"));
		}
	}
}
