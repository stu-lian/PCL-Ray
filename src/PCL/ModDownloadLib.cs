using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Configuration.Storage;
using PCL.Core.App.Localization;
using PCL.Core.IO.Net.Http;
using PCL.Core.Minecraft;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public static class ModDownloadLib
{
	public class McInstallRequest
	{
		public ModDownload.DlCleanroomListEntry cleanroomEntry;

		public string cleanroomVersion;

		public ModComp.CompFile fabricApi;

		public string fabricVersion;

		public ModDownload.DlForgeVersionEntry forgeEntry;

		public string forgeVersion;

		public string labyModChannel;

		public string labyModCommitRef;

		public ModComp.CompFile legacyFabricApi;

		public string legacyFabricVersion;

		public ModDownload.DlLiteLoaderListEntry liteLoaderEntry;

		public string minecraftJson;

		public string minecraftName;

		public ModModpack.MMCPackInfo mmcPackInfo;

		public ModDownload.DlNeoForgeListEntry neoForgeEntry;

		public string neoForgeVersion;

		public ModComp.CompFile optiFabric;

		public ModDownload.DlOptiFineListEntry optiFineEntry;

		public string optiFineVersion;

		public string targetInstanceFolder;

		public string targetInstanceName;
	}

	private static readonly object installSyncLock = new object();

	private static readonly object vanillaSyncLock = new object();

	private static readonly string mcDownloadClientLibName = Lang.Text("Minecraft.Download.Stage.VanillaLibrariesDownload");

	private static readonly string mcDownloadClientJsonName = Lang.Text("Minecraft.Download.Stage.VanillaJsonDownload");

	private static bool isForgeRecommendedRefreshed;

	private const string mcInstallDefaultType = "安装";

	private static string CombineCacheSubfolder(string parentFolder, string childFolderName)
	{
		bool flag = string.IsNullOrWhiteSpace(childFolderName);
		if (!flag)
		{
			bool flag2 = ((childFolderName == "." || childFolderName == "..") ? true : false);
			flag = flag2;
		}
		if (flag || childFolderName.IndexOfAny(new char[10] { '/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0' }) >= 0 || Path.IsPathRooted(childFolderName))
		{
			CancelUnsafeCacheSubfolder(childFolderName, "包含非法路径字符");
		}
		string text = Path.GetFullPath(parentFolder).TrimEnd(new char[2]
		{
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar
		}) + Path.DirectorySeparatorChar;
		string fullPath = Path.GetFullPath(Path.Combine(text, childFolderName));
		if (!fullPath.StartsWith(text, StringComparison.OrdinalIgnoreCase))
		{
			CancelUnsafeCacheSubfolder(childFolderName, "导致缓存路径越界");
		}
		return fullPath;
	}

	private static void CancelUnsafeCacheSubfolder(string childFolderName, string reason)
	{
		string text = "远程版本名" + reason + "：" + childFolderName;
		ModBase.Log("[Download] " + text);
		HintService.Hint(text, HintType.Error);
		throw new ModBase.CancelledException();
	}

	public static ModLoader.LoaderCombo<string> McDownloadClient(NetPreDownloadBehaviour behaviour, string id, string jsonUrl = null)
	{
		if (!RestrictedFeaturePolicy.Check("minecraft-client-download"))
		{
			return null;
		}
		try
		{
			string text = Path.Combine(ModFolder.mcFolderSelected, "versions", id);
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				if (!(item.name != Lang.Text("Minecraft.Download.Stage.MinecraftDownload", new object[1] { id })))
				{
					if (behaviour == NetPreDownloadBehaviour.ExitWhileExistsOrDownloading)
					{
						return (ModLoader.LoaderCombo<string>)item;
					}
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return (ModLoader.LoaderCombo<string>)item;
				}
			}
			if (behaviour != NetPreDownloadBehaviour.IgnoreCheck && File.Exists(Path.Combine(text, id + ".json")) && File.Exists(Path.Combine(text, id + ".jar")))
			{
				if (behaviour == NetPreDownloadBehaviour.ExitWhileExistsOrDownloading)
				{
					return null;
				}
				if (ModMain.MyMsgBox(Lang.Text("Minecraft.Download.Error.InstanceAlreadyExists", new object[2] { id, "\r\n" }), Lang.Text("Minecraft.Download.Error.InstanceAlreadyExists.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")) != 1)
				{
					return null;
				}
				File.Delete(Path.Combine(text, id + ".jar"));
				File.Delete(Path.Combine(text, id + ".json"));
			}
			ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.MinecraftDownload", new object[1] { id }), McDownloadClientLoader(id, jsonUrl));
			loaderCombo.OnStateChanged = McInstallState;
			loaderCombo.Start(text);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
			return loaderCombo;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 Minecraft 下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
			return null;
		}
	}

	public static void McDownloadClientCore(string id, string jsonUrl, NetPreDownloadBehaviour behaviour)
	{
		if (!RestrictedFeaturePolicy.Check("minecraft-client-save"))
		{
			return;
		}
		try
		{
			string versionFolder = SystemDialogs.SelectFolder((string)null, (string)null);
			if (!versionFolder.Contains("\\"))
			{
				return;
			}
			versionFolder = Path.Combine(versionFolder, id);
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar)
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.MinecraftDownload", new object[1] { id }) ?? "")))
				{
					if (behaviour != NetPreDownloadBehaviour.ExitWhileExistsOrDownloading)
					{
						HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					}
					return;
				}
			}
			List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadInstanceJson"), new List<DownloadFile>
			{
				new DownloadFile(ModDownload.DlSourceLauncherOrMetaGet(jsonUrl), Path.Combine(versionFolder, id + ".json"), new ModBase.FileChecker(-1L, -1L, null, canUseExistsFile: false, isJson: true))
			})
			{
				ProgressWeight = 2.0
			});
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeCoreJarUrl"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				task.output = ModLibrary.McLibNetFilesFromInstance(new McInstance(versionFolder));
			})
			{
				ProgressWeight = 0.5,
				show = false
			});
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadCoreJar"), new List<DownloadFile>())
			{
				ProgressWeight = 5.0
			});
			ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.MinecraftDownload", new object[1] { id }), list);
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start(id);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 Minecraft 下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	private static List<ModLoader.LoaderBase> McDownloadClientLoader(string id, string jsonUrl = null, string instanceName = null)
	{
		instanceName = instanceName ?? id;
		string instanceFolder = Path.Combine(ModFolder.mcFolderSelected, "versions", instanceName);
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		if (jsonUrl == null)
		{
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.ObtainVanillaJsonUrl"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				string original = ModDownload.DlClientListGet(id)?.ToString();
				task.output = new List<DownloadFile>
				{
					new DownloadFile(ModDownload.DlSourceLauncherOrMetaGet(original), Path.Combine(instanceFolder, instanceName + ".json"))
				};
			})
			{
				ProgressWeight = 2.0,
				show = false
			});
		}
		list.Add(new LoaderDownload(mcDownloadClientJsonName, new List<DownloadFile>
		{
			new DownloadFile(ModDownload.DlSourceLauncherOrMetaGet(jsonUrl ?? ""), Path.Combine(instanceFolder, instanceName + ".json"), new ModBase.FileChecker(-1L, -1L, null, canUseExistsFile: false, isJson: true))
		})
		{
			ProgressWeight = 3.0
		});
		List<ModLoader.LoaderBase> list2 = new List<ModLoader.LoaderBase>();
		list2.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeVanillaLibraries.Side"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
		{
			string filePath = Path.Combine(instanceFolder, instanceName + ".json");
			ModBase.WaitForFileReady(filePath);
			ModBase.Log("[Download] 开始分析原版支持库文件：" + instanceFolder);
			if (id == "1.16.5" && Config.Download.FixAuthLib)
			{
				try
				{
					string text = ModBase.ReadFile(filePath);
					text = text.Replace("2.1.28/authlib-2.1.28.jar", "2.3.31/authlib-2.3.31.jar").Replace("com.mojang:authlib:2.1.28", "com.mojang:authlib:2.3.31").Replace("ad54da276bf59983d02d5ed16fc14541354c71fd", "bbd00ca33b052f73a6312254780fc580d2da3535")
						.Replace("76328", "87662");
					ModBase.WriteFile(filePath, text);
				}
				catch (Exception ex)
				{
					ModBase.Log("[Download] 替换 Authlib 版本失败: " + ex.Message);
				}
			}
			task.output = ModLibrary.McLibNetFilesFromInstance(new McInstance(instanceFolder));
		})
		{
			ProgressWeight = 1.0,
			show = false
		});
		list2.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadVanillaLibraries.Side"), new List<DownloadFile>())
		{
			ProgressWeight = 13.0,
			show = false
		});
		list.Add(new ModLoader.LoaderCombo<string>(mcDownloadClientLibName, list2)
		{
			block = false,
			ProgressWeight = 14.0
		});
		List<ModLoader.LoaderBase> list3 = new List<ModLoader.LoaderBase>();
		list3.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeAssetsIndex.Side"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
		{
			ModBase.WaitForFileReady(Path.Combine(instanceFolder, instanceName + ".json"));
			try
			{
				McInstance version = new McInstance(instanceFolder);
				task.output = new List<DownloadFile> { ModDownload.DlClientAssetIndexGet(version) };
			}
			catch (Exception innerException)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.AssetIndexAnalysisFailed"), innerException);
			}
			try
			{
				JsonObject jsonObject = (JsonObject)ModBase.GetJson(ModBase.ReadFile(Path.Combine(instanceFolder, instanceName + ".json")));
				jsonObject.Add("clientVersion", id);
				ModBase.WriteFile(Path.Combine(instanceFolder, instanceName + ".json"), jsonObject.ToString());
			}
			catch (Exception innerException2)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.AddClientVersionFailed"), innerException2);
			}
		})
		{
			ProgressWeight = 1.0,
			show = false
		});
		list3.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadAssetsIndex.Side"), new List<DownloadFile>())
		{
			ProgressWeight = 3.0,
			show = false
		});
		list3.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeRequiredAssets.Side"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
		{
			ModLoader.LoaderBase progressFeed = task;
			task.output = ModAssets.McAssetsFixList(new McInstance(instanceFolder), checkHash: true, ref progressFeed);
			task = (ModLoader.LoaderTask<string, List<DownloadFile>>)progressFeed;
		})
		{
			ProgressWeight = 0.01,
			show = false
		});
		list3.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadAssets.Side"), new List<DownloadFile>())
		{
			ProgressWeight = 14.0,
			show = false
		});
		list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.DownloadVanillaAssets"), list3)
		{
			block = false,
			ProgressWeight = 18.0
		});
		return list;
	}

	public static MyListItem McDownloadListItem(JsonObject entry, MyListItem.ClickEventHandler onClick, bool isSaveOnly)
	{
		string logo = entry["type"].ToString() switch
		{
			"release" => ModBase.pathImage + "Blocks/Grass.png", 
			"snapshot" => ModBase.pathImage + "Blocks/CommandBlock.png", 
			"pending" => ModBase.pathImage + "Blocks/CommandBlock.png", 
			"special" => ModBase.pathImage + "Blocks/GoldBlock.png", 
			_ => ModBase.pathImage + "Blocks/CobbleStone.png", 
		};
		string text = McFormatter.FormatVersion(entry["id"].ToString()).Replace("_", " ");
		MyListItem myListItem = new MyListItem
		{
			Logo = logo,
			SnapsToDevicePixels = true,
			Title = text,
			Height = 42.0,
			Type = MyListItem.CheckType.Clickable,
			Tag = entry
		};
		if (entry["lore"] == null)
		{
			if (text != (string?)entry["id"])
			{
				myListItem.Info = Lang.Date(JsonCompat.ToObject<DateTime>(entry["releaseTime"]), "g") + " | " + entry["id"];
			}
			else
			{
				myListItem.Info = Lang.Date(JsonCompat.ToObject<DateTime>(entry["releaseTime"]), "g");
			}
		}
		else if (text != (string?)entry["id"])
		{
			myListItem.Info = entry["lore"]?.ToString() + " | " + entry["id"];
		}
		else
		{
			myListItem.Info = entry["lore"].ToString();
		}
		if (entry["url"].ToString().Contains("unlisted-versions-of-minecraft"))
		{
			myListItem.Tags = Lang.Text("Download.Source.Tag.Uvmc");
		}
		myListItem.Click += onClick;
		if (isSaveOnly)
		{
			myListItem.ContentHandler = McDownloadSaveMenuBuild;
		}
		else
		{
			myListItem.ContentHandler = McDownloadMenuBuild;
		}
		return myListItem;
	}

	private static void McDownloadSaveMenuBuild(object sender, EventArgs _)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object ss, EventArgs ee) =>
		{
			ModDownloadLib.McDownloadMenuLog(ss, (dynamic)ee);
		};
		MyIconButton myIconButton2 = new MyIconButton
		{
			LogoScale = 1.0,
			SvgIcon = "lucide/server",
			ToolTip = Lang.Text("Download.Version.DownloadServer")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object ss, EventArgs ee) =>
		{
			ModDownloadLib.McDownloadMenuSaveServer(ss, (dynamic)ee);
		};
		((dynamic)sender).Buttons = new MyIconButton[2] { myIconButton2, myIconButton };
	}

	private static void McDownloadMenuBuild(object sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			SvgIcon = "lucide/save",
			ToolTip = Lang.Text("Download.Version.SaveAs")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object a, EventArgs b) =>
		{
			ModDownloadLib.McDownloadMenuSave(a, (dynamic)b);
		};
		MyIconButton myIconButton2 = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object a, EventArgs b) =>
		{
			ModDownloadLib.McDownloadMenuLog(a, (dynamic)b);
		};
		MyIconButton myIconButton3 = new MyIconButton
		{
			LogoScale = 1.0,
			SvgIcon = "lucide/server",
			ToolTip = Lang.Text("Download.Version.DownloadServer")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton3, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton3, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton3, 2.0);
		myIconButton3.Click += (object a, EventArgs b) =>
		{
			ModDownloadLib.McDownloadMenuSaveServer(a, (dynamic)b);
		};
		((dynamic)sender).Buttons = new MyIconButton[3] { myIconButton, myIconButton2, myIconButton3 };
	}

	private static void McDownloadMenuLog(object sender, RoutedEventArgs e)
	{
		JsonNode versionJson;
		if ((object)((dynamic)sender).Tag != null)
		{
			versionJson = (JsonNode)((dynamic)sender).Tag;
		}
		else
		{
			versionJson = (((object)((dynamic)sender).Parent.Tag == null) ? ((JsonNode)((dynamic)sender).Parent.Parent.Tag) : ((JsonNode)((dynamic)sender).Parent.Tag));
		}
		McUpdateLogShow(versionJson);
	}

	private static void McDownloadMenuSaveServer(object sender, RoutedEventArgs e)
	{
		MyListItem myListItem;
		if (sender is MyListItem)
		{
			myListItem = (MyListItem)sender;
		}
		else
		{
			myListItem = ((!(((dynamic)sender).Parent is MyListItem)) ? ((MyListItem)((dynamic)sender).Parent.Parent) : ((MyListItem)((dynamic)sender).Parent));
		}
		try
		{
			string id = myListItem.Title;
			string original = ((dynamic)myListItem.Tag)["url"].ToString();
			string versionFolder = SystemDialogs.SelectFolder((string)null, (string)null);
			if (!versionFolder.Contains("\\"))
			{
				return;
			}
			versionFolder = Path.Combine(versionFolder, id);
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.MinecraftServerDownload", new object[1] { id }) ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.ServerDownloading"), HintType.Error);
					return;
				}
			}
			List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadInstanceJson"), new List<DownloadFile>
			{
				new DownloadFile(ModDownload.DlSourceLauncherOrMetaGet(original), Path.Combine(versionFolder, id + ".json"), new ModBase.FileChecker(-1L, -1L, null, canUseExistsFile: false, isJson: true))
			})
			{
				ProgressWeight = 2.0
			});
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.BuildServer"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				McInstance mcInstance = new McInstance(versionFolder);
				if (mcInstance.JsonObject["downloads"] == null || mcInstance.JsonObject["downloads"]["server"] == null || mcInstance.JsonObject["downloads"]["server"]["url"] == null)
				{
					File.Delete(Path.Combine(versionFolder, id + ".json"));
					if (!new DirectoryInfo(versionFolder).GetFileSystemInfos().Any())
					{
						Directory.Delete(versionFolder);
					}
					task.output = new List<DownloadFile>();
					HintService.Hint(Lang.Text("Minecraft.Download.Error.NoOfficialServerDownload", new object[1] { id }), HintType.Error);
					Thread.Sleep(2000);
					task.Abort();
				}
				else
				{
					string original2 = (string?)mcInstance.JsonObject["downloads"]["server"]["url"];
					ModBase.FileChecker checker = new ModBase.FileChecker(1024L, (long)(mcInstance.JsonObject["downloads"]["server"]["size"] ?? ((JsonNode)(-1))), (string?)mcInstance.JsonObject["downloads"]["server"]["sha1"]);
					task.output = new List<DownloadFile>
					{
						new DownloadFile(ModDownload.DlSourceLauncherOrMetaGet(original2), Path.Combine(versionFolder, id + "-server.jar"), checker)
					};
					string text = $"@echo off\ntitle {Lang.Text("Minecraft.Download.ServerBatch.Title", new object[1] { id })}\necho {Lang.Text("Minecraft.Download.ServerBatch.InstructionJavaPath")}\necho {Lang.Text("Minecraft.Download.ServerBatch.InstructionPclSettings")}\necho ------------------------------\necho {Lang.Text("Minecraft.Download.ServerBatch.InstructionEula")}\necho ------------------------------\n\"java\" -server -XX:+UseG1GC -Xmx4096M -Xms1024M -XX:+UseCompressedOops -jar {id}-server.jar nogui\necho ----------------------\necho {Lang.Text("Minecraft.Download.ServerBatch.ServerStopped")}\npause";
					ModBase.WriteFile(Path.Combine(versionFolder, "Launch Server.bat"), text.Replace("\n", "\r\n"), append: false, Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF8 : Encoding.GetEncoding("GB18030"));
					File.Delete(Path.Combine(versionFolder, id + ".json"));
				}
			})
			{
				ProgressWeight = 0.5,
				show = false
			});
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadServerFile"), new List<DownloadFile>())
			{
				ProgressWeight = 5.0
			});
			ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.MinecraftServerDownload", new object[1] { id }), list);
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start(id);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 Minecraft 服务端下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	public static void McDownloadMenuSave(object sender, RoutedEventArgs e)
	{
		if (!RestrictedFeaturePolicy.Check("minecraft-client-save"))
		{
			return;
		}
		FrameworkElement frameworkElement = (FrameworkElement)sender;
		MyListItem myListItem2;
		if (frameworkElement is MyListItem myListItem)
		{
			myListItem2 = myListItem;
		}
		else
		{
			myListItem2 = ((!(frameworkElement.Parent is MyListItem myListItem3)) ? ((MyListItem)(object)((FrameworkElement)(object)frameworkElement.Parent).Parent) : myListItem3);
		}
		try
		{
			string title = myListItem2.Title;
			string original = ((JsonObject)myListItem2.Tag)["url"].ToString();
			string versionFolder = SystemDialogs.SelectFolder((string)null, (string)null);
			if (!versionFolder.Contains("\\"))
			{
				return;
			}
			versionFolder = Path.Combine(versionFolder, title);
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.MinecraftDownload", new object[1] { title }) ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadInstanceJson"), new List<DownloadFile>
			{
				new DownloadFile(ModDownload.DlSourceLauncherOrMetaGet(original), Path.Combine(versionFolder, title + ".json"), new ModBase.FileChecker(-1L, -1L, null, canUseExistsFile: false, isJson: true))
			})
			{
				ProgressWeight = 2.0
			});
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeCoreJarUrl"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				task.output = new List<DownloadFile> { ModDownload.DlClientJarGet(new McInstance(versionFolder), returnNothingOnFileUseable: false) };
			})
			{
				ProgressWeight = 0.5,
				show = false
			});
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadCoreJar"), new List<DownloadFile>())
			{
				ProgressWeight = 5.0
			});
			ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.MinecraftDownload", new object[1] { title }), list);
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start(title);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 Minecraft 下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	public static void McUpdateLogShow(JsonNode versionJson)
	{
		string wikiUrlSuffix = McFormatter.GetWikiUrlSuffix(versionJson["id"].ToString());
		ModBase.OpenWebsite(McFormatter.GetWikiBaseUrl() + "/" + wikiUrlSuffix.TrimStart('/'));
	}

	public static void McDownloadOptiFine(ModDownload.DlOptiFineListEntry downloadInfo)
	{
		try
		{
			string nameVersion = downloadInfo.NameVersion;
			string text = Path.Combine(ModFolder.mcFolderSelected, "versions", nameVersion);
			if (!McVersionComparer.CompareVersionGe(downloadInfo.Inherit, "1.14"))
			{
				InlineArray6<string> buffer = default;
				buffer[0] = ModFolder.mcFolderSelected;
				buffer[1] = "libraries";
				buffer[2] = "optifine";
				buffer[3] = "OptiFine";
				buffer[4] = downloadInfo.NameFile.Replace("OptiFine_", "").Replace(".jar", "").Replace("preview_", "");
				buffer[5] = downloadInfo.NameFile.Replace("OptiFine_", "OptiFine-").Replace("preview_", "");
				Path.Combine(buffer);
			}
			else
			{
				Path.Combine(ModBase.pathTemp, "Cache", "Code", downloadInfo.NameVersion + "_" + ModBase.GetUuid());
			}
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar)
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.OptiFineDownload", new object[1] { downloadInfo.DisplayName }) ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			if (File.Exists(Path.Combine(text, nameVersion + ".json")))
			{
				if (ModMain.MyMsgBox(Lang.Text("Minecraft.Download.Error.InstanceAlreadyExists", new object[2] { nameVersion, "\r\n" }), Lang.Text("Minecraft.Download.Error.InstanceAlreadyExists.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")) != 1)
				{
					return;
				}
				File.Delete(Path.Combine(text, nameVersion + ".jar"));
				File.Delete(Path.Combine(text, nameVersion + ".json"));
			}
			ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.OptiFineDownload", new object[1] { downloadInfo.DisplayName }), McDownloadOptiFineLoader(downloadInfo));
			loaderCombo.OnStateChanged = McInstallState;
			loaderCombo.Start(text);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 OptiFine 下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	private static void McDownloadOptiFineSave(ModDownload.DlOptiFineListEntry downloadInfo)
	{
		try
		{
			_ = downloadInfo.NameVersion;
			string text = SystemDialogs.SelectSaveFile(Lang.Text("Download.Version.SelectSaveLocation"), downloadInfo.NameFile, "OptiFine Jar (*.jar)|*.jar", (string)null);
			if (!text.Contains("\\"))
			{
				return;
			}
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.OptiFineDownload", new object[1] { downloadInfo.DisplayName }) ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			ModLoader.LoaderCombo<ModDownload.DlOptiFineListEntry> loaderCombo = new ModLoader.LoaderCombo<ModDownload.DlOptiFineListEntry>(Lang.Text("Minecraft.Download.Stage.OptiFineDownload", new object[1] { downloadInfo.DisplayName }), McDownloadOptiFineSaveLoader(downloadInfo, text));
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start(downloadInfo);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 OptiFine 下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	private static void McDownloadOptiFineInstall(string baseMcFolderHome, string target, ModLoader.LoaderTask<List<DownloadFile>, bool> task, bool useJavaWrapper, Version javaVersion)
	{
		JavaEntry val;
		lock (ModJava.javaLock)
		{
			val = ModJava.JavaSelect(Lang.Text("Minecraft.Download.Error.InstallationCanceled"), javaVersion, null, null, enforceVersionRange: true);
			if (val == null)
			{
				if (!ModJava.JavaDownloadConfirm(Lang.Text("Minecraft.Download.Error.JavaVersionRequired")))
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.JavaNotFoundInstallCanceled"));
				}
				int num = ((javaVersion.Major >= 9) ? javaVersion.Major : 8);
				ModLoader.LoaderCombo<string> javaDownloadLoader = ModJava.GetJavaDownloadLoader();
				try
				{
					javaDownloadLoader.Start(num, isForceRestart: true);
					while (javaDownloadLoader.State == ModBase.LoadState.Loading && !task.IsAborted)
					{
						Thread.Sleep(10);
					}
				}
				finally
				{
					javaDownloadLoader.Abort();
				}
				val = ModJava.JavaSelect(Lang.Text("Minecraft.Download.Error.InstallationCanceled"), javaVersion, null, null, enforceVersionRange: true);
				if (task.IsAborted)
				{
					return;
				}
				if (val == null)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.JavaNotFoundInstallCanceled"));
				}
			}
		}
		string text = ((!((useJavaWrapper && !(dynamic)Config.Launch.DisableJlw) ? true : false)) ? $"-Duser.home=\"{baseMcFolderHome.TrimEnd('\\')}\" -cp \"{target}\" optifine.Installer" : $"-Doolloo.jlw.tmpdir=\"{ModBase.pathPure.TrimEnd('\\')}\" -Duser.home=\"{baseMcFolderHome.TrimEnd('\\')}\" -cp \"{target}\" -jar \"{ModLaunch.ExtractJavaWrapper()}\" optifine.Installer");
		if (val.Installation.MajorVersion >= 9)
		{
			text = "--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED " + text;
		}
		lock (installSyncLock)
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				FileName = val.Installation.JavaExePath,
				Arguments = text,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WorkingDirectory = ModBase.ShortenPath(baseMcFolderHome)
			};
			if (processStartInfo.EnvironmentVariables.ContainsKey("appdata"))
			{
				processStartInfo.EnvironmentVariables["appdata"] = baseMcFolderHome;
			}
			else
			{
				processStartInfo.EnvironmentVariables.Add("appdata", baseMcFolderHome);
			}
			ModBase.Log("[Download] 开始安装 OptiFine：" + target);
			int totalLength = 0;
			Process process = new Process
			{
				StartInfo = processStartInfo
			};
			string lastResult = "";
			AutoResetEvent outputWaitHandle = new AutoResetEvent(initialState: false);
			try
			{
				AutoResetEvent errorWaitHandle = new AutoResetEvent(initialState: false);
				try
				{
					process.OutputDataReceived += (object _, DataReceivedEventArgs e) =>
					{
						try
						{
							if (e.Data == null)
							{
								outputWaitHandle.Set();
							}
							else
							{
								lastResult = e.Data;
								if (ModBase.modeDebug)
								{
									ModBase.Log("[Installer] " + lastResult);
								}
								totalLength++;
								task.Progress += 0.00012857142857142858;
							}
						}
						catch (ObjectDisposedException)
						{
						}
						catch (Exception ex2)
						{
							ModBase.Log(ex2, "读取 OptiFine 安装器信息失败");
						}
						try
						{
							if (task.State == ModBase.LoadState.Aborted && !process.HasExited)
							{
								ModBase.Log("[Installer] 由于任务取消，已中止 OptiFine 安装");
								process.Kill();
							}
						}
						catch
						{
						}
					};
					process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
					{
						try
						{
							if (e.Data == null)
							{
								errorWaitHandle.Set();
							}
							else
							{
								lastResult = e.Data;
								if (ModBase.modeDebug)
								{
									ModBase.Log("[Installer] " + lastResult);
								}
								totalLength++;
								task.Progress += 0.00012857142857142858;
							}
						}
						catch (ObjectDisposedException)
						{
						}
						catch (Exception ex2)
						{
							ModBase.Log(ex2, "读取 OptiFine 安装器错误信息失败");
						}
						try
						{
							if (task.State == ModBase.LoadState.Aborted && !process.HasExited)
							{
								ModBase.Log("[Installer] 由于任务取消，已中止 OptiFine 安装");
								process.Kill();
							}
						}
						catch
						{
						}
					};
					process.Start();
					process.BeginOutputReadLine();
					process.BeginErrorReadLine();
					while (!process.HasExited)
					{
						Thread.Sleep(10);
					}
					outputWaitHandle.WaitOne(10000);
					errorWaitHandle.WaitOne(10000);
					process.Dispose();
					if (totalLength < 1000 || lastResult.Contains("at "))
					{
						throw new Exception(Lang.Text("Minecraft.Download.Error.InstallerFailedLastLine", new object[1] { lastResult }));
					}
				}
				finally
				{
					if (errorWaitHandle != null)
					{
						((IDisposable)errorWaitHandle).Dispose();
					}
				}
			}
			finally
			{
				if (outputWaitHandle != null)
				{
					((IDisposable)outputWaitHandle).Dispose();
				}
			}
		}
	}

	private static List<ModLoader.LoaderBase> McDownloadOptiFineLoader(ModDownload.DlOptiFineListEntry downloadInfo, string mcFolder = null, ModLoader.LoaderCombo<string> clientDownloadLoader = null, string clientFolder = null, bool fixLibrary = true)
	{
		mcFolder = mcFolder ?? ModFolder.mcFolderSelected;
		bool isCustomFolder = (mcFolder ?? "") != (ModFolder.mcFolderSelected ?? "");
		string id = downloadInfo.NameVersion;
		string versionFolder = Path.Combine(mcFolder, "versions", id);
		bool flag = downloadInfo.Inherit.Contains("w") || McVersionComparer.CompareVersionGe(downloadInfo.Inherit, "1.14");
		string target = (flag ? (ModMain.RequestTaskTempFolder() + "OptiFine.jar") : $"{mcFolder}libraries\\optifine\\OptiFine\\{downloadInfo.NameFile.Replace("OptiFine_", "").Replace(".jar", "").Replace("preview_", "")}\\{downloadInfo.NameFile.Replace("OptiFine_", "OptiFine-").Replace("preview_", "")}");
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		Version javaVersion = (McVersionComparer.CompareVersionGe(downloadInfo.Inherit, "26.1.2") ? new Version(21, 0, 0, 0) : new Version(1, 8, 0, 0));
		list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.ObtainOptiFineUrl"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
		{
			if (clientDownloadLoader == null)
			{
				if (isCustomFolder)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.NoVanillaLoaderSpecified"));
				}
				clientDownloadLoader = McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, downloadInfo.Inherit);
			}
			task.Progress = 0.1;
			List<string> list2 = new List<string>();
			string text = downloadInfo.Inherit;
			if (text == "1.8" || text == "1.9")
			{
				text += ".0";
			}
			if (downloadInfo.IsPreview)
			{
				list2.Add("https://bmclapi2.bangbang93.com/optifine/" + text + "/HD_U_" + downloadInfo.DisplayName.Replace(downloadInfo.Inherit + " ", "").Replace(" ", "/"));
			}
			else
			{
				list2.Add("https://bmclapi2.bangbang93.com/optifine/" + text + "/HD_U/" + downloadInfo.DisplayName.Replace(downloadInfo.Inherit + " ", ""));
			}
			try
			{
				string str;
				using (HttpResponseMessage httpResponseMessage = HttpSenderExtension.SendAsync(HttpHeaderHandler.WithHeader(HttpHeaderHandler.WithHeader(HttpHeaderHandler.WithHeader(HttpRequest.Create("https://optifine.net/adloadx?f=" + downloadInfo.NameFile), "Accept", "text/html"), "Accept-Language", "en-US,en;q=0.5"), "X-Requested-With", "XMLHttpRequest"), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).GetAwaiter().GetResult())
				{
					httpResponseMessage.EnsureSuccessStatusCode();
					str = HttpResponseExtension.AsString(httpResponseMessage);
				}
				task.Progress = 0.8;
				list2.Add("https://optifine.net/" + str.RegexSearch("downloadx\\?f=[^\"']+")[0]);
				ModBase.Log("[Download] OptiFine " + downloadInfo.DisplayName + " 官方下载地址：" + list2.Last());
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "获取 OptiFine " + downloadInfo.DisplayName + " 官方下载地址失败");
			}
			task.output = new List<DownloadFile>
			{
				new DownloadFile(list2.ToArray(), target, new ModBase.FileChecker(307200L, -1L))
			};
		})
		{
			ProgressWeight = 8.0
		});
		list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadOptiFineMainFile"), new List<DownloadFile>())
		{
			ProgressWeight = 8.0
		});
		list.Add(new ModLoader.LoaderTask<List<DownloadFile>, bool>(Lang.Text("Minecraft.Download.Stage.WaitVanillaDownload"), (ModLoader.LoaderTask<List<DownloadFile>, bool> task) =>
		{
			if (clientDownloadLoader != null)
			{
				List<ModLoader.LoaderBase> source = (from l in clientDownloadLoader.GetLoaderList()
					where (l.name ?? "") == mcDownloadClientLibName || (l.name ?? "") == mcDownloadClientJsonName
					where l.State != ModBase.LoadState.Finished
					select l).ToList();
				if (source.Any())
				{
					ModBase.Log("[Download] OptiFine 安装正在等待原版文件下载完成");
				}
				while (source.Any() && !task.IsAborted)
				{
					source = source.Where((ModLoader.LoaderBase l) => l.State != ModBase.LoadState.Finished).ToList();
					Thread.Sleep(50);
				}
				if (!task.IsAborted && isCustomFolder)
				{
					lock (vanillaSyncLock)
					{
						string folderNameFromPath = ModBase.GetFolderNameFromPath(clientFolder);
						Directory.CreateDirectory(Path.Combine(mcFolder, "versions", downloadInfo.Inherit));
						if (!File.Exists(Path.Combine(mcFolder, "versions", downloadInfo.Inherit, downloadInfo.Inherit + ".json")))
						{
							ModBase.CopyFile(clientFolder + folderNameFromPath + ".json", $"{mcFolder}versions\\{downloadInfo.Inherit}\\{downloadInfo.Inherit}.json");
						}
						if (!File.Exists(Path.Combine(mcFolder, "versions", downloadInfo.Inherit, downloadInfo.Inherit + ".jar")))
						{
							ModBase.CopyFile(clientFolder + folderNameFromPath + ".jar", $"{mcFolder}versions\\{downloadInfo.Inherit}\\{downloadInfo.Inherit}.jar");
						}
					}
				}
			}
		})
		{
			ProgressWeight = 0.1,
			show = false
		});
		if (flag)
		{
			ModBase.Log("[Download] 检测为新版 OptiFine：" + downloadInfo.Inherit);
			list.Add(new ModLoader.LoaderTask<List<DownloadFile>, bool>(Lang.Text("Minecraft.Download.Stage.InstallOptiFine.MethodA"), (ModLoader.LoaderTask<List<DownloadFile>, bool> task) =>
			{
				string text = ModMain.RequestTaskTempFolder();
				string text2 = Path.Combine(text, ".minecraft");
				try
				{
					if (Directory.Exists(Path.Combine(text2, "versions", downloadInfo.Inherit)))
					{
						ModBase.DeleteDirectory(Path.Combine(text2, "versions", downloadInfo.Inherit));
					}
					Directory.CreateDirectory(Path.Combine(text2, "versions", downloadInfo.Inherit));
					ModFolder.McFolderLauncherProfilesJsonCreate(text2);
					ModBase.CopyFile(Path.Combine(mcFolder, "versions", downloadInfo.Inherit, downloadInfo.Inherit + ".json"), Path.Combine(text2, "versions", downloadInfo.Inherit, downloadInfo.Inherit + ".json"));
					ModBase.CopyFile(Path.Combine(mcFolder, "versions", downloadInfo.Inherit, downloadInfo.Inherit + ".jar"), Path.Combine(text2, "versions", downloadInfo.Inherit, downloadInfo.Inherit + ".jar"));
					task.Progress = 0.06;
					bool flag2 = ModBase.IsUtf8CodePage();
					while (true)
					{
						try
						{
							McDownloadOptiFineInstall(text, target, task, flag2, javaVersion);
						}
						catch (Exception ex)
						{
							if (flag2)
							{
								throw new Exception(Lang.Text("Minecraft.Download.Error.OptiFineInstallerRunFailed"), ex);
							}
							ModBase.Log(ex, "不使用 JavaWrapper 安装 OptiFine 失败，将使用 JavaWrapper 并重试");
							flag2 = true;
							continue;
						}
						break;
					}
					task.Progress = 0.96;
					File.Delete(Path.Combine(text2, "launcher_profiles.json"));
					ModBase.CopyDirectory(text2, mcFolder);
					task.Progress = 0.98;
					File.Delete(target);
					ModBase.DeleteDirectory(text);
				}
				catch (Exception innerException)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.OptiFineInstallFailed.MethodA"), innerException);
				}
			})
			{
				ProgressWeight = 8.0
			});
		}
		else
		{
			ModBase.Log("[Download] 检测为旧版 OptiFine：" + downloadInfo.Inherit);
			list.Add(new ModLoader.LoaderTask<List<DownloadFile>, bool>(Lang.Text("Minecraft.Download.Stage.InstallOptiFine.MethodB"), (ModLoader.LoaderTask<List<DownloadFile>, bool> task) =>
			{
				try
				{
					Directory.CreateDirectory(versionFolder);
					task.Progress = 0.1;
					if (File.Exists(Path.Combine(versionFolder, id + ".jar")))
					{
						File.Delete(Path.Combine(versionFolder, id + ".jar"));
					}
					ModBase.CopyFile(Path.Combine(mcFolder, "versions", downloadInfo.Inherit, downloadInfo.Inherit + ".jar"), Path.Combine(versionFolder, id + ".jar"));
					task.Progress = 0.7;
					McInstance mcInstance = new McInstance(Path.Combine(mcFolder, "versions", downloadInfo.Inherit));
					string text = "{\n    \"id\": \"" + id + "\",\n    \"inheritsFrom\": \"" + downloadInfo.Inherit + "\",\n    \"time\": \"" + (string.IsNullOrEmpty(downloadInfo.ReleaseTime) ? mcInstance.releaseTime.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture) : downloadInfo.ReleaseTime.Replace("/", "-")) + "T23:33:33+08:00\",\n    \"releaseTime\": \"" + (string.IsNullOrEmpty(downloadInfo.ReleaseTime) ? mcInstance.releaseTime.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture) : downloadInfo.ReleaseTime.Replace("/", "-")) + "T23:33:33+08:00\",\n    \"type\": \"release\",\n    \"libraries\": [\n        {\"name\": \"optifine:OptiFine:" + downloadInfo.NameFile.Replace("OptiFine_", "").Replace(".jar", "").Replace("preview_", "") + "\"},\n        {\"name\": \"net.minecraft:launchwrapper:1.12\"}\n    ],\n    \"mainClass\": \"net.minecraft.launchwrapper.Launch\",";
					task.Progress = 0.8;
					ModBase.WriteFile(text: (!mcInstance.IsOldJson) ? (text + "\n    \"minimumLauncherVersion\": \"21\",\n    \"arguments\": {\n        \"game\": [\n            \"--tweakClass\",\n            \"optifine.OptiFineTweaker\"\n        ]\n    }\n}") : (text + "\n    \"minimumLauncherVersion\": 18,\n    \"minecraftArguments\": \"" + mcInstance.JsonObject["minecraftArguments"]?.ToString() + "  --tweakClass optifine.OptiFineTweaker\"\n}"), filePath: Path.Combine(versionFolder, id + ".json"));
				}
				catch (Exception innerException)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.OptiFineInstallFailed.MethodB"), innerException);
				}
			})
			{
				ProgressWeight = 1.0
			});
		}
		if (fixLibrary)
		{
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeOptiFineLibraries"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				task.output = ModLibrary.McLibNetFilesFromInstance(new McInstance(versionFolder));
			})
			{
				ProgressWeight = 1.0,
				show = false
			});
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadOptiFineLibraries"), new List<DownloadFile>())
			{
				ProgressWeight = 4.0
			});
		}
		return list;
	}

	private static List<ModLoader.LoaderBase> McDownloadOptiFineSaveLoader(ModDownload.DlOptiFineListEntry downloadInfo, string targetFolder)
	{
		return new List<ModLoader.LoaderBase>
		{
			new ModLoader.LoaderTask<ModDownload.DlOptiFineListEntry, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.ObtainOptiFineDownloadUrl"), (ModLoader.LoaderTask<ModDownload.DlOptiFineListEntry, List<DownloadFile>> task) =>
			{
				List<string> list = new List<string>();
				string text = downloadInfo.Inherit;
				if (text == "1.8" || text == "1.9")
				{
					text += ".0";
				}
				if (downloadInfo.IsPreview)
				{
					list.Add("https://bmclapi2.bangbang93.com/optifine/" + text + "/HD_U_" + downloadInfo.DisplayName.Replace(downloadInfo.Inherit + " ", "").Replace(" ", "/"));
				}
				else
				{
					list.Add("https://bmclapi2.bangbang93.com/optifine/" + text + "/HD_U/" + downloadInfo.DisplayName.Replace(downloadInfo.Inherit + " ", ""));
				}
				try
				{
					string str;
					using (HttpResponseMessage httpResponseMessage = HttpSenderExtension.SendAsync(HttpHeaderHandler.WithHeader(HttpHeaderHandler.WithHeader(HttpHeaderHandler.WithHeader(HttpRequest.Create("https://optifine.net/adloadx?f=" + downloadInfo.NameFile), "Accept", "text/html"), "Accept-Language", "en-US,en;q=0.5"), "X-Requested-With", "XMLHttpRequest"), (HttpClient)null, true, true, HttpCompletionOption.ResponseContentRead, 3, default(CancellationToken)).GetAwaiter().GetResult())
					{
						httpResponseMessage.EnsureSuccessStatusCode();
						str = HttpResponseExtension.AsString(httpResponseMessage);
					}
					task.Progress = 0.8;
					list.Add("https://optifine.net/" + str.RegexSearch("downloadx\\?f=[^\"']+")[0]);
					ModBase.Log("[Download] OptiFine " + downloadInfo.DisplayName + " 官方下载地址：" + list.Last());
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "获取 OptiFine " + downloadInfo.DisplayName + " 官方下载地址失败");
				}
				task.Progress = 0.9;
				task.output = new List<DownloadFile>
				{
					new DownloadFile(list.ToArray(), targetFolder, new ModBase.FileChecker(65536L, -1L))
				};
			})
			{
				ProgressWeight = 6.0
			},
			new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadOptiFineMainFile"), new List<DownloadFile>())
			{
				ProgressWeight = 10.0,
				block = true
			}
		};
	}

	public static MyListItem OptiFineDownloadListItem(ModDownload.DlOptiFineListEntry entry, MyListItem.ClickEventHandler onClick, bool isSaveOnly)
	{
		List<string> list = new List<string> { entry.IsPreview ? Lang.Text("Download.Version.Type.Preview") : Lang.Text("Download.Version.Type.Release") };
		if (!string.IsNullOrEmpty(entry.ReleaseTime))
		{
			list.Add(Lang.Text("Download.Version.ReleaseDate", new object[1] { entry.ReleaseTime }));
		}
		if (entry.RequiredForgeVersion == null)
		{
			list.Add(Lang.Text("Download.Version.Optifine.IncompatibleForge"));
		}
		else if (!string.IsNullOrEmpty(entry.RequiredForgeVersion))
		{
			list.Add(Lang.Text("Download.Version.Optifine.CompatibleForge", new object[1] { entry.RequiredForgeVersion }));
		}
		MyListItem myListItem = new MyListItem();
		myListItem.Title = entry.DisplayName;
		myListItem.SnapsToDevicePixels = true;
		myListItem.Height = 42.0;
		myListItem.Type = MyListItem.CheckType.Clickable;
		myListItem.Tag = entry;
		myListItem.Info = string.Join("  |  ", list);
		myListItem.Logo = ModBase.pathImage + "Blocks/GrassPath.png";
		myListItem.Click += onClick;
		myListItem.ContentHandler = (isSaveOnly ? new Action<MyListItem, EventArgs>(OptiFineSaveContMenuBuild) : new Action<MyListItem, EventArgs>(OptiFineContMenuBuild));
		return myListItem;
	}

	private static void OptiFineSaveContMenuBuild(object sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object sender2, EventArgs e2) =>
		{
			OptiFineLog_Click(sender2, (RoutedEventArgs)e2);
		};
		((dynamic)sender).Buttons = new MyIconButton[1] { myIconButton };
	}

	private static void OptiFineContMenuBuild(object sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			SvgIcon = "lucide/save",
			ToolTip = Lang.Text("Download.Version.SaveAs")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		MyIconButton myIconButton2 = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object sender2, EventArgs e2) =>
		{
			OptiFineLog_Click(sender2, (RoutedEventArgs)e2);
		};
		((dynamic)sender).Buttons = new MyIconButton[2] { myIconButton, myIconButton2 };
	}

	private static void OptiFineLog_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlOptiFineListEntry dlOptiFineListEntry;
		if ((object)((dynamic)sender).Tag != null)
		{
			dlOptiFineListEntry = (ModDownload.DlOptiFineListEntry)((dynamic)sender).Tag;
		}
		else
		{
			dlOptiFineListEntry = (((object)((dynamic)sender).Parent.Tag == null) ? ((ModDownload.DlOptiFineListEntry)((dynamic)sender).Parent.Parent.Tag) : ((ModDownload.DlOptiFineListEntry)((dynamic)sender).Parent.Tag));
		}
		ModBase.OpenWebsite("https://optifine.net/changelog?f=" + dlOptiFineListEntry.NameFile);
	}

	public static void OptiFineSave_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlOptiFineListEntry downloadInfo;
		if ((object)((dynamic)sender).Tag != null)
		{
			downloadInfo = (ModDownload.DlOptiFineListEntry)((dynamic)sender).Tag;
		}
		else
		{
			downloadInfo = (((object)((dynamic)sender).Parent.Tag == null) ? ((ModDownload.DlOptiFineListEntry)((dynamic)sender).Parent.Parent.Tag) : ((ModDownload.DlOptiFineListEntry)((dynamic)sender).Parent.Tag));
		}
		McDownloadOptiFineSave(downloadInfo);
	}

	public static void McDownloadLiteLoader(ModDownload.DlLiteLoaderListEntry downloadInfo)
	{
		try
		{
			string inherit = downloadInfo.Inherit;
			Path.Combine(ModBase.pathTemp, "Download", inherit + "-Liteloader.jar");
			string text = downloadInfo.Inherit + "-LiteLoader";
			string text2 = Path.Combine(ModFolder.mcFolderSelected, "versions", text);
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.LiteLoaderDownload", new object[1] { inherit }) ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			if (File.Exists(Path.Combine(text2, text + ".json")))
			{
				if (ModMain.MyMsgBox(Lang.Text("Minecraft.Download.Error.InstanceAlreadyExists", new object[2] { text, "\r\n" }), Lang.Text("Minecraft.Download.Error.InstanceAlreadyExists.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")) != 1)
				{
					return;
				}
				File.Delete(Path.Combine(text2, text + ".jar"));
				File.Delete(Path.Combine(text2, text + ".json"));
			}
			ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LiteLoaderDownload", new object[1] { inherit }), McDownloadLiteLoaderLoader(downloadInfo));
			loaderCombo.OnStateChanged = McInstallState;
			loaderCombo.Start(text2);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 LiteLoader 下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	private static void McDownloadLiteLoaderSave(ModDownload.DlLiteLoaderListEntry downloadInfo)
	{
		try
		{
			string inherit = downloadInfo.Inherit;
			string text = SystemDialogs.SelectSaveFile(Lang.Text("Download.Version.SelectSaveLocation"), downloadInfo.FileName.Replace("-SNAPSHOT", ""), Lang.Text("Minecraft.Download.Stage.ForgelikeInstallerFilter", new object[2] { "LiteLoader", "jar" }), (string)null);
			if (!text.Contains("\\"))
			{
				return;
			}
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.LiteLoaderDownload", new object[1] { inherit }) ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
			List<string> list2 = new List<string>();
			if (downloadInfo.IsLegacy)
			{
				switch (downloadInfo.Inherit ?? "")
				{
				case "1.7.10":
					list2.Add("https://dl.liteloader.com/redist/1.7.10/liteloader-installer-1.7.10-04.jar");
					break;
				case "1.7.2":
					list2.Add("https://dl.liteloader.com/redist/1.7.2/liteloader-installer-1.7.2-04.jar");
					break;
				case "1.6.4":
					list2.Add("https://dl.liteloader.com/redist/1.6.4/liteloader-installer-1.6.4-01.jar");
					break;
				case "1.6.2":
					list2.Add("https://dl.liteloader.com/redist/1.6.2/liteloader-installer-1.6.2-04.jar");
					break;
				case "1.5.2":
					list2.Add("https://dl.liteloader.com/redist/1.5.2/liteloader-installer-1.5.2-01.jar");
					break;
				default:
					throw new NotSupportedException(Lang.Text("Minecraft.Download.Error.UnknownMinecraftVersion", new object[1] { downloadInfo.Inherit }));
				}
			}
			else
			{
				list2.Add("http://jenkins.liteloader.com/job/LiteLoaderInstaller%20" + downloadInfo.Inherit + "/lastSuccessfulBuild/artifact/" + ((downloadInfo.Inherit == "1.8") ? "ant/dist/" : "build/libs/") + downloadInfo.FileName);
			}
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadMainFile"), new List<DownloadFile>
			{
				new DownloadFile(list2.ToArray(), text, new ModBase.FileChecker(1048576L, -1L))
			})
			{
				ProgressWeight = 15.0
			});
			ModLoader.LoaderCombo<ModDownload.DlLiteLoaderListEntry> loaderCombo = new ModLoader.LoaderCombo<ModDownload.DlLiteLoaderListEntry>(Lang.Text("Minecraft.Download.Stage.LiteLoaderInstallerDownload", new object[1] { inherit }), list);
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start(downloadInfo);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 LiteLoader 安装器下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	private static List<ModLoader.LoaderBase> McDownloadLiteLoaderLoader(ModDownload.DlLiteLoaderListEntry downloadInfo, string mcFolder = null, ModLoader.LoaderCombo<string> clientDownloadLoader = null, bool fixLibrary = true)
	{
		mcFolder = mcFolder ?? ModFolder.mcFolderSelected;
		bool isCustomFolder = (mcFolder ?? "") != (ModFolder.mcFolderSelected ?? "");
		string inherit = downloadInfo.Inherit;
		Path.Combine(ModBase.pathTemp, "Download", inherit + "-Liteloader.jar");
		string versionName = downloadInfo.Inherit + "-LiteLoader";
		string versionFolder = Path.Combine(mcFolder, "versions", versionName);
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		if (clientDownloadLoader == null)
		{
			list.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Minecraft.Download.Stage.StartLiteLoaderDependencyDownload"), (ModLoader.LoaderTask<string, string> _) =>
			{
				if (isCustomFolder)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.NoVanillaLoaderSpecified"));
				}
				clientDownloadLoader = McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, downloadInfo.Inherit);
			})
			{
				ProgressWeight = 0.2,
				show = false,
				block = false
			});
		}
		list.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Minecraft.Download.Stage.InstallLiteLoader"), (ModLoader.LoaderTask<string, string> _) =>
		{
			try
			{
				Directory.CreateDirectory(versionFolder);
				JsonObject jsonObject = new JsonObject();
				jsonObject.Add("id", versionName);
				DateTime dateTime = DateTime.Parse(downloadInfo.ReleaseTime, Lang.Culture);
				jsonObject.Add("time", (JsonNode)dateTime);
				jsonObject.Add("releaseTime", (JsonNode)dateTime);
				jsonObject.Add("type", "release");
				jsonObject.Add("arguments", ModBase.GetJson("{\"game\":[\"--tweakClass\",\"" + downloadInfo.jsonToken["tweakClass"]?.ToString() + "\"]}"));
				jsonObject.Add("libraries", downloadInfo.jsonToken["libraries"]?.DeepClone());
				jsonObject["libraries"].AsArray().Add(ModBase.GetJson("{\"name\": \"com.mumfrey:liteloader:" + downloadInfo.jsonToken["version"]?.ToString() + "\",\"url\": \"https://dl.liteloader.com/versions/\"}"));
				jsonObject.Add("mainClass", "net.minecraft.launchwrapper.Launch");
				jsonObject.Add("minimumLauncherVersion", (JsonNode)18);
				jsonObject.Add("inheritsFrom", downloadInfo.Inherit);
				jsonObject.Add("jar", downloadInfo.Inherit);
				ModBase.WriteFile(Path.Combine(versionFolder, versionName + ".json"), jsonObject.ToString());
			}
			catch (Exception innerException)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.LiteLoaderInstallFailed"), innerException);
			}
		})
		{
			ProgressWeight = 1.0
		});
		if (fixLibrary)
		{
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeLiteLoaderLibraries"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				task.output = ModLibrary.McLibNetFilesFromInstance(new McInstance(versionFolder));
			})
			{
				ProgressWeight = 1.0,
				show = false
			});
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLiteLoaderLibraries"), new List<DownloadFile>())
			{
				ProgressWeight = 6.0
			});
		}
		return list;
	}

	public static MyListItem LiteLoaderDownloadListItem(ModDownload.DlLiteLoaderListEntry entry, MyListItem.ClickEventHandler onClick, bool isSaveOnly)
	{
		List<string> list = new List<string> { entry.IsPreview ? Lang.Text("Download.Version.Type.Preview") : Lang.Text("Download.Version.Type.Stable") };
		if (!string.IsNullOrEmpty(entry.ReleaseTime))
		{
			list.Add(Lang.Text("Download.Version.ReleaseDate", new object[1] { entry.ReleaseTime }));
		}
		MyListItem myListItem = new MyListItem();
		myListItem.Title = entry.Inherit;
		myListItem.SnapsToDevicePixels = true;
		myListItem.Height = 42.0;
		myListItem.Type = MyListItem.CheckType.Clickable;
		myListItem.Tag = entry;
		myListItem.Info = string.Join("  |  ", list);
		myListItem.Logo = ModBase.pathImage + "Blocks/Egg.png";
		myListItem.Click += onClick;
		myListItem.ContentHandler = (isSaveOnly ? new Action<MyListItem, EventArgs>(LiteLoaderSaveContMenuBuild) : new Action<MyListItem, EventArgs>(LiteLoaderContMenuBuild));
		return myListItem;
	}

	private static void LiteLoaderSaveContMenuBuild(MyListItem sender, EventArgs e)
	{
		if ((bool)((dynamic)sender.Tag).IsLegacy)
		{
			sender.Buttons = Array.Empty<MyIconButton>();
			return;
		}
		MyIconButton myIconButton = new MyIconButton
		{
			SvgIcon = "lucide/list",
			ToolTip = Lang.Text("Download.Version.ViewAllVersions"),
			Tag = sender
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object sender2, EventArgs e2) =>
		{
			LiteLoaderAll_Click(sender2, (RoutedEventArgs)e2);
		};
		sender.Buttons = new MyIconButton[1] { myIconButton };
	}

	private static void LiteLoaderContMenuBuild(MyListItem sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			SvgIcon = "lucide/save",
			ToolTip = Lang.Text("Download.Version.SaveInstaller"),
			Tag = sender
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object sender2, EventArgs e2) =>
		{
			LiteLoaderSave_Click(sender2, (RoutedEventArgs)e2);
		};
		if ((bool)((dynamic)sender.Tag).IsLegacy)
		{
			sender.Buttons = new _003C_003Ez__ReadOnlySingleElementList<MyIconButton>(myIconButton);
			return;
		}
		MyIconButton myIconButton2 = new MyIconButton
		{
			SvgIcon = "lucide/list",
			ToolTip = Lang.Text("Download.Version.ViewAllVersions"),
			Tag = sender
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object sender2, EventArgs e2) =>
		{
			LiteLoaderAll_Click(sender2, (RoutedEventArgs)e2);
		};
		sender.Buttons = new _003C_003Ez__ReadOnlyArray<MyIconButton>(new MyIconButton[2] { myIconButton, myIconButton2 });
	}

	private static void LiteLoaderAll_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlLiteLoaderListEntry dlLiteLoaderListEntry = ((!(((dynamic)sender).Tag is ModDownload.DlLiteLoaderListEntry)) ? ((ModDownload.DlLiteLoaderListEntry)((dynamic)sender).Tag.Tag) : ((ModDownload.DlLiteLoaderListEntry)((dynamic)sender).Tag));
		ModBase.OpenWebsite("https://jenkins.liteloader.com/view/" + dlLiteLoaderListEntry.Inherit);
	}

	public static void LiteLoaderSave_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlLiteLoaderListEntry downloadInfo = ((!(((dynamic)sender).Tag is ModDownload.DlLiteLoaderListEntry)) ? ((ModDownload.DlLiteLoaderListEntry)((dynamic)sender).Tag.Tag) : ((ModDownload.DlLiteLoaderListEntry)((dynamic)sender).Tag));
		McDownloadLiteLoaderSave(downloadInfo);
	}

	public static void McDownloadForgelikeSave(ModDownload.DlForgelikeEntry info)
	{
		try
		{
			string text = SystemDialogs.SelectSaveFile(Lang.Text("Download.Version.SelectSaveLocation"), $"{info.LoaderName}-{info.Inherit}-{info.VersionName}.{info.FileExtension}", Lang.Text("Minecraft.Download.Stage.ForgelikeInstallerFilter", new object[2] { info.LoaderName, info.FileExtension }), (string)null);
			string text2 = $"{info.LoaderName} {info.Inherit} - {info.VersionName}";
			if (!text.Contains("\\"))
			{
				return;
			}
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.ForgelikeDownload", new object[1] { text2 }) ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			List<DownloadFile> list = new List<DownloadFile>();
			if (info.forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.NeoForge)
			{
				string text3 = ((ModDownload.DlNeoForgeListEntry)info).UrlBase + "-installer.jar";
				list.Add(new DownloadFile(new string[2]
				{
					text3.Replace("maven.neoforged.net/releases", "bmclapi2.bangbang93.com/maven"),
					text3
				}, text, new ModBase.FileChecker(65536L, -1L)));
			}
			else if (info.forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.Cleanroom)
			{
				string text4 = ((ModDownload.DlCleanroomListEntry)info).UrlBase + "-installer.jar";
				list.Add(new DownloadFile(new string[1] { text4 }, text, new ModBase.FileChecker(65536L, -1L)));
			}
			else
			{
				ModDownload.DlForgeVersionEntry dlForgeVersionEntry = (ModDownload.DlForgeVersionEntry)info;
				list.Add(new DownloadFile(new string[2]
				{
					$"https://bmclapi2.bangbang93.com/maven/net/minecraftforge/forge/{dlForgeVersionEntry.Inherit}-{dlForgeVersionEntry.FileVersion}/forge-{dlForgeVersionEntry.Inherit}-{dlForgeVersionEntry.FileVersion}-{dlForgeVersionEntry.Category}.{dlForgeVersionEntry.FileExtension}",
					$"https://files.minecraftforge.net/maven/net/minecraftforge/forge/{dlForgeVersionEntry.Inherit}-{dlForgeVersionEntry.FileVersion}/forge-{dlForgeVersionEntry.Inherit}-{dlForgeVersionEntry.FileVersion}-{dlForgeVersionEntry.Category}.{dlForgeVersionEntry.FileExtension}"
				}, text, new ModBase.FileChecker(65536L, -1L, dlForgeVersionEntry.Hash)));
			}
			List<ModLoader.LoaderBase> list2 = new List<ModLoader.LoaderBase>();
			list2.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadMainFile"), list)
			{
				ProgressWeight = 6.0
			});
			ModLoader.LoaderCombo<ModDownload.DlForgelikeEntry> loaderCombo = new ModLoader.LoaderCombo<ModDownload.DlForgelikeEntry>(Lang.Text("Minecraft.Download.Stage.ForgelikeDownload", new object[1] { text2 }), list2);
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start(info);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 " + info.LoaderName + " 安装器下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	private static void ForgelikeInjector(string target, ModLoader.LoaderTask<bool, bool> task, string mcFolder, bool useJavaWrapper, ModDownload.DlForgelikeEntry.ForgelikeType forgeType)
	{
		JavaEntry val;
		lock (ModJava.javaLock)
		{
			val = ModJava.JavaSelect(Lang.Text("Minecraft.Download.Error.InstallationCanceled"), new Version(1, 8, 0, 60));
			if (val == null)
			{
				if (!ModJava.JavaDownloadConfirm(Lang.Text("Minecraft.Download.Error.JavaVersionRequired")))
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.JavaNotFoundInstallCanceled"));
				}
				ModLoader.LoaderCombo<string> javaDownloadLoader = ModJava.GetJavaDownloadLoader();
				try
				{
					javaDownloadLoader.Start(17, isForceRestart: true);
					while (javaDownloadLoader.State == ModBase.LoadState.Loading && !task.IsAborted)
					{
						Thread.Sleep(10);
					}
				}
				finally
				{
					javaDownloadLoader.Abort();
				}
				val = ModJava.JavaSelect(Lang.Text("Minecraft.Download.Error.InstallationCanceled"), new Version(1, 8, 0, 60));
				if (task.IsAborted)
				{
					return;
				}
				if (val == null)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.JavaNotFoundInstallCanceled"));
				}
			}
		}
		string text = ((!useJavaWrapper || Config.Launch.DisableJlw) ? $"-cp \"{ModBase.pathTemp}Cache\\forge_installer.jar;{target}\" com.bangbang93.ForgeInstaller \"{mcFolder}" : $"-Doolloo.jlw.tmpdir=\"{ModBase.pathPure.TrimEnd('\\')}\" -cp \"{ModBase.pathTemp}Cache\\forge_installer.jar;{target}\" -jar \"{ModLaunch.ExtractJavaWrapper()}\" com.bangbang93.ForgeInstaller \"{mcFolder}");
		if (val.Installation.MajorVersion >= 9)
		{
			text = "--add-exports cpw.mods.bootstraplauncher/cpw.mods.bootstraplauncher=ALL-UNNAMED " + text;
		}
		lock (installSyncLock)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = val.Installation.JavaExePath,
				Arguments = text,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true
			};
			string loaderName = ModBase.GetStringFromEnum(forgeType);
			ModBase.Log("[Download] 开始安装 " + loaderName + "：" + text);
			Process process = new Process
			{
				StartInfo = startInfo
			};
			Queue<string> lastResults = new Queue<string>();
			AutoResetEvent outputWaitHandle = new AutoResetEvent(initialState: false);
			try
			{
				AutoResetEvent errorWaitHandle = new AutoResetEvent(initialState: false);
				try
				{
					process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
					{
						try
						{
							if (e.Data == null)
							{
								outputWaitHandle.Set();
							}
							else
							{
								lastResults.Enqueue(e.Data);
								if (lastResults.Count > 100)
								{
									lastResults.Dequeue();
								}
								ForgelikeInjectorLine(e.Data, task);
							}
						}
						catch (ObjectDisposedException)
						{
						}
						catch (Exception ex2)
						{
							ModBase.Log(ex2, "读取 " + loaderName + " 安装器信息失败");
						}
						try
						{
							if (task.State == ModBase.LoadState.Aborted && !process.HasExited)
							{
								ModBase.Log("[Installer] 由于任务取消，已中止 " + loaderName + " 安装");
								process.Kill();
							}
						}
						catch
						{
						}
					};
					process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
					{
						try
						{
							if (e.Data == null)
							{
								errorWaitHandle.Set();
							}
							else
							{
								lastResults.Enqueue(e.Data);
								if (lastResults.Count > 100)
								{
									lastResults.Dequeue();
								}
								ForgelikeInjectorLine(e.Data, task);
							}
						}
						catch (ObjectDisposedException)
						{
						}
						catch (Exception ex2)
						{
							ModBase.Log(ex2, "读取 " + loaderName + " 安装器错误信息失败");
						}
						try
						{
							if (task.State == ModBase.LoadState.Aborted && !process.HasExited)
							{
								ModBase.Log("[Installer] 由于任务取消，已中止 " + loaderName + " 安装");
								process.Kill();
							}
						}
						catch
						{
						}
					};
					process.Start();
					process.BeginOutputReadLine();
					process.BeginErrorReadLine();
					while (!process.HasExited)
					{
						Thread.Sleep(10);
					}
					outputWaitHandle.WaitOne(10000);
					errorWaitHandle.WaitOne(10000);
					process.Dispose();
					if (lastResults.Reverse().Take(5).Any((string l) => l == "true"))
					{
						return;
					}
					ModBase.Log(lastResults.Join("\r\n"));
					string text2 = "";
					int num = Math.Max(0, lastResults.Count - 5);
					for (int num2 = lastResults.Count - 1; num <= num2; num++)
					{
						text2 = text2 + "\r\n" + lastResults.ElementAtOrDefault(num);
					}
					throw new Exception(Lang.Text("Minecraft.Download.Error.LoaderInstallerFailedLastLine", new object[2] { loaderName, text2 }));
				}
				finally
				{
					if (errorWaitHandle != null)
					{
						((IDisposable)errorWaitHandle).Dispose();
					}
				}
			}
			finally
			{
				if (outputWaitHandle != null)
				{
					((IDisposable)outputWaitHandle).Dispose();
				}
			}
		}
	}

	private static void ForgelikeInjectorLine(string content, ModLoader.LoaderTask<bool, bool> task)
	{
		switch (content ?? "")
		{
		case "Extracting json":
			ModBase.Log("[Installer] " + content);
			task.Progress = 0.07;
			break;
		case "Downloading libraries":
			ModBase.Log("[Installer] " + content);
			task.Progress = 0.08;
			break;
		case "  File exists: Checksum validated.":
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Installer] " + content);
			}
			task.Progress += 0.003;
			break;
		case "Building Processors":
			task.Progress = 0.18;
			break;
		case "Task: DOWNLOAD_MOJMAPS":
			task.Progress = 0.2;
			break;
		case "Task: MERGE_MAPPING":
			task.Progress = 0.3;
			break;
		case "Splitting: ":
			task.Progress = 0.35;
			break;
		case "Parameter Annotations":
			task.Progress = 0.4;
			break;
		case "Processing Complete":
			task.Progress = 0.5;
			break;
		case "log: null":
			task.Progress = 0.5;
			break;
		case "Sorting":
			task.Progress = 0.65;
			break;
		case "Remapping final jar":
			task.Progress = 0.72;
			break;
		case "Remapping jar... 50%":
			task.Progress = 0.76;
			break;
		case "Remapping jar... 100%":
			task.Progress = 0.81;
			break;
		case "Injecting profile":
			task.Progress = 0.91;
			break;
		default:
			if (ModBase.modeDebug)
			{
				ModBase.Log("[Installer] " + content);
			}
			return;
		}
		ModBase.Log("[Installer] " + content);
	}

	private static List<ModLoader.LoaderBase> McDownloadForgelikeLoader(ModDownload.DlForgelikeEntry.ForgelikeType forgeType, string loaderVersion, string targetVersion, string inherit, ModDownload.DlForgelikeEntry info = null, string mcFolder = null, ModLoader.LoaderCombo<string> clientDownloadLoader = null, string clientFolder = null)
	{
		mcFolder = mcFolder ?? ModFolder.mcFolderSelected;
		if (forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.NeoForge && info == null)
		{
			if (inherit == "1.20.1" && !loaderVersion.StartsWithF("1.20.1-"))
			{
				info = new ModDownload.DlNeoForgeListEntry("1.20.1-" + loaderVersion);
			}
			else
			{
				info = new ModDownload.DlNeoForgeListEntry(loaderVersion);
			}
		}
		if (forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.Cleanroom && info == null)
		{
			info = new ModDownload.DlCleanroomListEntry(loaderVersion);
		}
		if (forgeType != ModDownload.DlForgelikeEntry.ForgelikeType.NeoForge && loaderVersion.StartsWithF("1.") && loaderVersion.Contains("-"))
		{
			inherit = loaderVersion.BeforeFirst("-");
			loaderVersion = loaderVersion.AfterLast("-");
		}
		string loaderName = ModBase.GetStringFromEnum(forgeType);
		bool isCustomFolder = (mcFolder ?? "") != (ModFolder.mcFolderSelected ?? "");
		string installerAddress = ModMain.RequestTaskTempFolder() + "forge_installer.jar";
		string versionFolder = mcFolder + "versions\\" + targetVersion + "\\";
		_ = $"{loaderName} {inherit} - {loaderVersion}";
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		_ = ModFolder.mcFolderSelected + "versions\\" + targetVersion + "\\";
		if (info == null)
		{
			list.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Minecraft.Download.Stage.ObtainLoaderDetails", new object[1] { loaderName }), (ModLoader.LoaderTask<string, string> task) =>
			{
				ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>> loaderTask = new ModLoader.LoaderTask<string, List<ModDownload.DlForgeVersionEntry>>("McDownloadForgeLoader " + inherit, ModDownload.DlForgeVersionMain);
				loaderTask.WaitForExit(inherit);
				task.Progress = 0.8;
				foreach (ModDownload.DlForgeVersionEntry item in loaderTask.output)
				{
					if (McVersionComparer.CompareVersion(item.version.ToString(), loaderVersion) == 0)
					{
						info = item;
						return;
					}
				}
				throw new Exception(Lang.Text("Minecraft.Download.Error.LoaderDetailsNotFound", new object[3] { loaderName, inherit, loaderVersion }));
			})
			{
				ProgressWeight = 3.0
			});
		}
		list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.PrepareLoaderDownload", new object[1] { loaderName }), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
		{
			if (clientDownloadLoader == null)
			{
				if (isCustomFolder)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.NoVanillaLoaderSpecified"));
				}
				clientDownloadLoader = McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, inherit);
			}
			List<DownloadFile> list2 = new List<DownloadFile>();
			if (info.forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.NeoForge)
			{
				string text = ((ModDownload.DlNeoForgeListEntry)info).UrlBase + "-installer.jar";
				list2.Add(new DownloadFile(new string[2]
				{
					text.Replace("maven.neoforged.net/releases", "bmclapi2.bangbang93.com/maven"),
					text
				}, installerAddress, new ModBase.FileChecker(65536L, -1L)));
			}
			else if (info.forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.Cleanroom)
			{
				string text2 = ((ModDownload.DlCleanroomListEntry)info).UrlBase + "-installer.jar";
				list2.Add(new DownloadFile(new string[1] { text2 }, installerAddress, new ModBase.FileChecker(65536L, -1L)));
			}
			else
			{
				ModDownload.DlForgeVersionEntry dlForgeVersionEntry = (ModDownload.DlForgeVersionEntry)info;
				string text3 = $"{dlForgeVersionEntry.Inherit.Replace("-", "_")}-{dlForgeVersionEntry.FileVersion}/forge-{dlForgeVersionEntry.Inherit.Replace("-", "_")}-{dlForgeVersionEntry.FileVersion}-{dlForgeVersionEntry.Category}.{dlForgeVersionEntry.FileExtension}";
				list2.Add(new DownloadFile(new string[2]
				{
					"https://bmclapi2.bangbang93.com/maven/net/minecraftforge/forge/" + text3,
					"https://files.minecraftforge.net/maven/net/minecraftforge/forge/" + text3
				}, installerAddress, new ModBase.FileChecker(65536L, -1L, dlForgeVersionEntry.Hash)));
			}
			task.output = list2;
		})
		{
			ProgressWeight = 0.5,
			show = false
		});
		list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLoaderMainFile", new object[1] { loaderName }), new List<DownloadFile>())
		{
			ProgressWeight = 9.0
		});
		if (forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.NeoForge || Convert.ToDouble(loaderVersion.BeforeFirst(".")) >= 20.0)
		{
			ModBase.Log("[Download] 检测为" + ((forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.Forge) ? "新版 Forge" : (" " + forgeType)) + "：" + loaderVersion);
			List<ModLibrary.McLibToken> libs = null;
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeLoaderLibraries", new object[1] { loaderName }), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				task.output = new List<DownloadFile>();
				ZipArchive zipArchive = null;
				try
				{
					ModBase.WaitForFileReady(installerAddress);
					zipArchive = new ZipArchive(new FileStream(installerAddress, FileMode.Open));
					task.Progress = 0.2;
					JsonObject jsonObject = (JsonObject)ModBase.GetJson(ModBase.ReadFile(zipArchive.GetEntry("install_profile.json").Open()));
					JsonObject jsonObject2 = (JsonObject)ModBase.GetJson(ModBase.ReadFile(zipArchive.GetEntry("version.json").Open()));
					JsonCompat.Merge(jsonObject, (JsonNode)jsonObject2);
					if (inherit == "1.16.5" && Config.Download.FixAuthLib)
					{
						jsonObject = (JsonObject)ModBase.GetJson(jsonObject.ToString().Replace("2.1.28/authlib-2.1.28.jar", "2.3.31/authlib-2.3.31.jar").Replace("com.mojang:authlib:2.1.28", "com.mojang:authlib:2.3.31")
							.Replace("ad54da276bf59983d02d5ed16fc14541354c71fd", "bbd00ca33b052f73a6312254780fc580d2da3535")
							.Replace("76328", "87662"));
					}
					libs = ModLibrary.McLibListGetWithJson(jsonObject, keepSameNameDifferentVersionResult: true);
					if (jsonObject["data"] != null && jsonObject["data"]["MOJMAPS"] != null)
					{
						task.Progress = 0.4;
						JsonObject jsonObject3 = (JsonObject)ModBase.GetJson(ModNet.NetGetCodeByLoader(ModDownload.DlSourceLauncherOrMetaGet(ModDownload.DlClientListGet(inherit)?.ToString()), 45000, isJson: true));
						string text = jsonObject["data"]["MOJMAPS"]["client"].ToString().Trim("[]".ToCharArray()).BeforeFirst("@");
						string localPath = ModLibrary.McLibGet(text).Replace(".jar", "-mappings." + jsonObject["data"]["MOJMAPS"]["client"].ToString().Trim("[]".ToCharArray()).Split("@")[1]);
						JsonNode jsonNode = jsonObject3["downloads"]["client_mappings"];
						libs.Add(new ModLibrary.McLibToken
						{
							IsNatives = false,
							LocalPath = localPath,
							OriginalName = text,
							Url = (string?)jsonNode["url"],
							size = (long)jsonNode["size"],
							Sha1 = (string?)jsonNode["sha1"]
						});
						ModBase.Log($"[Download] 需要下载 Mappings：{jsonNode["url"]} (SHA1: {jsonNode["sha1"]})");
					}
					task.Progress = 0.8;
					int i = 0;
					for (int num = libs.Count - 1; i <= num; i++)
					{
						if (libs[i].LocalPath.EndsWithF($"{loaderName.ToLower()}-{inherit}-{loaderVersion}.jar") || libs[i].LocalPath.EndsWithF($"{loaderName.ToLower()}-{inherit}-{loaderVersion}-client.jar"))
						{
							ModBase.Log("[Download] 已从待下载 " + loaderName + " 支持库中移除：" + libs[i].LocalPath, ModBase.LogLevel.Debug);
							libs.RemoveAt(i);
							break;
						}
					}
					task.output = ModLibrary.McLibNetFilesFromTokens(libs);
				}
				catch (Exception innerException)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.LoaderLibraryListFailed", new object[1] { (forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.Forge) ? Lang.Text("Minecraft.Download.Loader.NewForge") : (" " + forgeType) }), innerException);
				}
				finally
				{
					zipArchive?.Dispose();
				}
			})
			{
				ProgressWeight = 2.0
			});
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLoaderLibraries", new object[1] { loaderName }), new List<DownloadFile>())
			{
				ProgressWeight = 12.0
			});
			list.Add(new ModLoader.LoaderTask<List<DownloadFile>, bool>(Lang.Text("Minecraft.Download.Stage.GetLoaderLibraries", new object[1] { loaderName }), (ModLoader.LoaderTask<List<DownloadFile>, bool> task) =>
			{
				if (isCustomFolder)
				{
					foreach (ModLibrary.McLibToken item2 in libs)
					{
						string text = item2.LocalPath.Replace(ModFolder.mcFolderSelected, mcFolder);
						if (!File.Exists(text))
						{
							Directory.CreateDirectory(Path.GetDirectoryName(text));
							ModBase.CopyFile(item2.LocalPath, text);
						}
						if (ModBase.modeDebug)
						{
							ModBase.Log("[Download] 复制的 " + loaderName + " 支持库文件：" + item2.LocalPath);
						}
					}
				}
				if (clientDownloadLoader != null)
				{
					List<ModLoader.LoaderBase> source = (from l in clientDownloadLoader.GetLoaderList()
						where (l.name ?? "") == mcDownloadClientLibName || (l.name ?? "") == mcDownloadClientJsonName
						where l.State != ModBase.LoadState.Finished
						select l).ToList();
					if (source.Any())
					{
						ModBase.Log("[Download] " + loaderName + " 安装正在等待原版文件下载完成");
					}
					while (source.Any() && !task.IsAborted)
					{
						source = source.Where((ModLoader.LoaderBase l) => l.State != ModBase.LoadState.Finished).ToList();
						Thread.Sleep(50);
					}
					if (!task.IsAborted && isCustomFolder)
					{
						lock (vanillaSyncLock)
						{
							string folderNameFromPath = ModBase.GetFolderNameFromPath(clientFolder);
							Directory.CreateDirectory(Path.Combine(mcFolder, "versions", inherit));
							if (!File.Exists(Path.Combine(mcFolder, "versions", inherit, inherit + ".json")))
							{
								ModBase.CopyFile(Path.Combine(clientFolder, folderNameFromPath + ".json"), Path.Combine(mcFolder, "versions", inherit, inherit + ".json"));
							}
							if (!File.Exists(Path.Combine(mcFolder, "versions", inherit, inherit + ".jar")))
							{
								ModBase.CopyFile(Path.Combine(clientFolder, folderNameFromPath + ".jar"), Path.Combine(mcFolder, "versions", inherit, inherit + ".jar"));
							}
						}
					}
				}
			})
			{
				ProgressWeight = 0.1,
				show = false
			});
			list.Add(new ModLoader.LoaderTask<bool, bool>((forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.Forge) ? Lang.Text("Minecraft.Download.Stage.InstallForge.MethodA") : Lang.Text("Minecraft.Download.Stage.InstallForgeType", new object[1] { forgeType }), (ModLoader.LoaderTask<bool, bool> task) =>
			{
				ModBase.WaitForFileReady(installerAddress);
				ZipArchive zipArchive = new ZipArchive(new FileStream(installerAddress, FileMode.Open));
				try
				{
					ModBase.Log("[Download] 开始进行 Forgelike 安装：" + installerAddress);
					List<string> oldList = (from i in new DirectoryInfo(mcFolder + "versions/").EnumerateDirectories()
						select i.FullName).ToList();
					ModBase.GetJson(ModBase.ReadFile(zipArchive.GetEntry("install_profile.json").Open()));
					Directory.CreateDirectory(versionFolder);
					task.Progress = 0.04;
					ModFolder.McFolderLauncherProfilesJsonCreate(mcFolder);
					task.Progress = 0.05;
					bool flag = ModBase.IsUtf8CodePage();
					while (true)
					{
						try
						{
							ModBase.WriteFile(Path.Combine(ModBase.pathTemp, "Cache", "forge_installer.jar"), ModBase.GetResourceStream("Resources/forge-installer.jar"));
							task.Progress = 0.06;
							ForgelikeInjector(installerAddress, task, mcFolder, flag, forgeType);
							task.Progress = 0.97;
						}
						catch (Exception ex)
						{
							if (flag)
							{
								throw new Exception(Lang.Text("Minecraft.Download.Error.LoaderInstallerRunFailed", new object[1] { loaderName }), ex);
							}
							ModBase.Log(ex, "不使用 JavaWrapper 安装 " + loaderName + " 失败，将使用 JavaWrapper 并重试");
							flag = true;
							continue;
						}
						break;
					}
					List<DirectoryInfo> list2 = new DirectoryInfo(mcFolder + "versions/").EnumerateDirectories().SkipWhile((DirectoryInfo i) => oldList.Contains(i.FullName)).ToList();
					if (list2.Count > 1)
					{
						list2 = list2.Where((DirectoryInfo l) => l.Name.ContainsF("forge", ignoreCase: true) && l.EnumerateFiles().Any()).ToList();
					}
					if (list2.Count == 1)
					{
						FileInfo fileInfo = list2[0].EnumerateFiles().First();
						ModBase.WriteFile(Path.Combine(versionFolder, targetVersion + ".json"), ModBase.ReadFile(fileInfo.FullName));
						ModBase.Log($"[Download] 已拷贝新增的实例 Json 文件：{fileInfo.FullName} -> {versionFolder}{targetVersion}.json");
					}
					else if (list2.Count > 1)
					{
						ModBase.Log("[Download] 有多个疑似的新增实例，无法确定：" + string.Join(";", list2.Select((DirectoryInfo d) => d.Name)));
					}
					else
					{
						ModBase.Log("[Download] 未找到新增的实例文件夹");
					}
				}
				catch (Exception innerException)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.LoaderInstallFailed", new object[1] { loaderName }), innerException);
				}
				finally
				{
					try
					{
						zipArchive?.Dispose();
						if (File.Exists(installerAddress))
						{
							File.Delete(installerAddress);
						}
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, "安装 " + loaderName + " 清理文件时出错");
					}
				}
			})
			{
				ProgressWeight = 10.0
			});
		}
		else
		{
			ModBase.Log("[Download] 检测为非新版 Forge：" + loaderVersion);
			list.Add(new ModLoader.LoaderTask<List<DownloadFile>, bool>(((forgeType == ModDownload.DlForgelikeEntry.ForgelikeType.Forge) ? Lang.Text("Minecraft.Download.Stage.InstallForge.MethodB") : Lang.Text("Minecraft.Download.Stage.InstallForgeType", new object[1] { forgeType })) ?? "", (ModLoader.LoaderTask<List<DownloadFile>, bool> task) =>
			{
				ZipArchive zipArchive = null;
				try
				{
					ModBase.WaitForFileReady(installerAddress);
					zipArchive = new ZipArchive(new FileStream(installerAddress, FileMode.Open));
					task.Progress = 0.2;
					JsonObject jsonObject = (JsonObject)ModBase.GetJson(ModBase.ReadFile(zipArchive.GetEntry("install_profile.json").Open()));
					task.Progress = 0.4;
					Directory.CreateDirectory(versionFolder);
					task.Progress = 0.5;
					if (jsonObject["install"] == null)
					{
						ModBase.Log("[Download] 开始进行 Forge 安装，Legacy 方式 1：" + installerAddress);
						JsonObject jsonObject2 = (JsonObject)ModBase.GetJson(ModBase.ReadFile(zipArchive.GetEntry(jsonObject["json"].ToString().TrimStart('/')).Open()));
						jsonObject2["id"] = targetVersion;
						ModBase.WriteFile(Path.Combine(versionFolder, targetVersion + ".json"), jsonObject2.ToString());
						task.Progress = 0.6;
						zipArchive.Dispose();
						string text = Path.Combine(Path.GetDirectoryName(installerAddress), "_unrar");
						ModBase.ExtractFile(installerAddress, text);
						ModBase.CopyDirectory(Path.Combine(text, "maven"), Path.Combine(mcFolder, "libraries"));
						ModBase.DeleteDirectory(text);
					}
					else
					{
						ModBase.Log("[Download] 开始进行 Forge 安装，Legacy 方式 2：" + installerAddress);
						string text2 = ModLibrary.McLibGet((string?)jsonObject["install"]["path"], withHead: true, ignoreLiteLoader: false, mcFolder);
						if (File.Exists(text2))
						{
							File.Delete(text2);
						}
						ModBase.WriteFile(text2, zipArchive.GetEntry((string?)jsonObject["install"]["filePath"]).Open());
						task.Progress = 0.9;
						jsonObject["versionInfo"]["id"] = targetVersion;
						if (jsonObject["versionInfo"]["inheritsFrom"] == null)
						{
							((JsonObject)jsonObject["versionInfo"]).Add("inheritsFrom", inherit);
						}
						ModBase.WriteFile(Path.Combine(versionFolder, targetVersion + ".json"), jsonObject["versionInfo"].ToString());
					}
				}
				catch (Exception innerException)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.ForgeOldInstallFailed"), innerException);
				}
				finally
				{
					try
					{
						zipArchive?.Dispose();
						if (File.Exists(installerAddress))
						{
							File.Delete(installerAddress);
						}
						string path = Path.Combine(Path.GetDirectoryName(installerAddress), "_unrar");
						if (Directory.Exists(path))
						{
							ModBase.DeleteDirectory(path);
						}
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "非新版方式安装 Forge 清理文件时出错");
					}
				}
			})
			{
				ProgressWeight = 1.0
			});
		}
		return list;
	}

	public static void ForgeDownloadListItemPreload(StackPanel stack, List<ModDownload.DlForgeVersionEntry> entries, MyListItem.ClickEventHandler onClick, bool isSaveOnly)
	{
		if (entries.Count == 1)
		{
			return;
		}
		ModDownload.DlForgeVersionEntry dlForgeVersionEntry = null;
		if (entries.Any())
		{
			dlForgeVersionEntry = entries[0];
		}
		else
		{
			ModBase.Log("[System] 未找到可用的 Forge 版本", ModBase.LogLevel.Debug);
		}
		ModDownload.DlForgeVersionEntry dlForgeVersionEntry2 = null;
		foreach (ModDownload.DlForgeVersionEntry entry in entries)
		{
			if (entry.IsRecommended)
			{
				dlForgeVersionEntry2 = entry;
			}
		}
		if (dlForgeVersionEntry != null && dlForgeVersionEntry == dlForgeVersionEntry2)
		{
			dlForgeVersionEntry = null;
		}
		if (dlForgeVersionEntry2 != null)
		{
			MyListItem myListItem = ForgeDownloadListItem(dlForgeVersionEntry2, onClick, isSaveOnly);
			myListItem.Info = Lang.Text("Download.Version.Type.Recommended") + (string.IsNullOrEmpty(myListItem.Info) ? "" : ("  |  " + myListItem.Info));
			stack.Children.Add(myListItem);
		}
		if (dlForgeVersionEntry != null)
		{
			MyListItem myListItem2 = ForgeDownloadListItem(dlForgeVersionEntry, onClick, isSaveOnly);
			myListItem2.Info = Lang.Text("Download.Version.Latest.Title") + (string.IsNullOrEmpty(myListItem2.Info) ? "" : ("  |  " + myListItem2.Info));
			stack.Children.Add(myListItem2);
		}
		stack.Children.Add(new TextBlock
		{
			Text = Lang.Text("Download.Version.AllVersions", new object[1] { entries.Count }),
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(6.0, 13.0, 0.0, 4.0)
		});
	}

	public static MyListItem ForgeDownloadListItem(ModDownload.DlForgeVersionEntry entry, MyListItem.ClickEventHandler onClick, bool isSaveOnly)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(entry.ReleaseTime))
		{
			list.Add(Lang.Text("Download.Version.ReleaseDate", new object[1] { entry.ReleaseTime }));
		}
		if (ModBase.modeDebug)
		{
			list.Add(Lang.Text("Download.Version.Forge.Type", new object[1] { entry.Category }));
		}
		MyListItem myListItem = new MyListItem
		{
			Title = entry.VersionName,
			SnapsToDevicePixels = true,
			Height = 42.0,
			Type = MyListItem.CheckType.Clickable,
			Tag = entry,
			Info = string.Join("  |  ", list),
			Logo = ModBase.pathImage + "Blocks/Anvil.png"
		};
		myListItem.Click += onClick;
		if (isSaveOnly)
		{
			myListItem.ContentHandler = ForgeSaveContMenuBuild;
		}
		else
		{
			myListItem.ContentHandler = ForgeContMenuBuild;
		}
		return myListItem;
	}

	private static void ForgeContMenuBuild(MyListItem sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			SvgIcon = "lucide/save",
			ToolTip = Lang.Text("Download.Version.SaveAs")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object ss, EventArgs ee) =>
		{
			ModDownloadLib.ForgeSave_Click(ss, (dynamic)ee);
		};
		MyIconButton myIconButton2 = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object ss, EventArgs ee) =>
		{
			ModDownloadLib.ForgeLog_Click(ss, (dynamic)ee);
		};
		sender.Buttons = new MyIconButton[2] { myIconButton, myIconButton2 };
	}

	private static void ForgeSaveContMenuBuild(MyListItem sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object ss, EventArgs ee) =>
		{
			ModDownloadLib.ForgeLog_Click(ss, (dynamic)e);
		};
		sender.Buttons = new MyIconButton[1] { myIconButton };
	}

	private static void ForgeLog_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlForgeVersionEntry dlForgeVersionEntry;
		if ((object)((dynamic)sender).Tag != null)
		{
			dlForgeVersionEntry = (ModDownload.DlForgeVersionEntry)((dynamic)sender).Tag;
		}
		else
		{
			dlForgeVersionEntry = (((object)((dynamic)sender).Parent.Tag == null) ? ((ModDownload.DlForgeVersionEntry)((dynamic)sender).Parent.Parent.Tag) : ((ModDownload.DlForgeVersionEntry)((dynamic)sender).Parent.Tag));
		}
		ModBase.OpenWebsite($"https://files.minecraftforge.net/maven/net/minecraftforge/forge/{dlForgeVersionEntry.Inherit}-{dlForgeVersionEntry.VersionName}/forge-{dlForgeVersionEntry.Inherit}-{dlForgeVersionEntry.VersionName}-changelog.txt");
	}

	public static void ForgeSave_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlForgeVersionEntry info;
		if ((object)((dynamic)sender).Tag != null)
		{
			info = (ModDownload.DlForgeVersionEntry)((dynamic)sender).Tag;
		}
		else
		{
			info = (((object)((dynamic)sender).Parent.Tag == null) ? ((ModDownload.DlForgeVersionEntry)((dynamic)sender).Parent.Parent.Tag) : ((ModDownload.DlForgeVersionEntry)((dynamic)sender).Parent.Tag));
		}
		McDownloadForgelikeSave(info);
	}

	public static void McDownloadForgeRecommendedRefresh()
	{
		if (isForgeRecommendedRefreshed)
		{
			return;
		}
		isForgeRecommendedRefreshed = true;
		ModBase.RunInNewThread(() =>
		{
			try
			{
				ModBase.Log("[Download] 刷新 Forge 推荐版本缓存开始");
				string text = ModNet.NetGetCodeByLoader("https://bmclapi2.bangbang93.com/forge/promos");
				if (text.Length < 1000)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.ForgePromosResultTooShort", new object[1] { text }));
				}
				JsonNode json = ModBase.GetJson(text);
				List<string> list = new List<string>();
				foreach (JsonObject item in json.AsArray())
				{
					if (item["name"] != null && item["build"] != null)
					{
						string text2 = (string?)item["name"];
						if (text2.EndsWithF("-recommended"))
						{
							list.Add("\"" + text2.Replace("-recommended", "\":\"" + item["build"]["version"]?.ToString() + "\""));
						}
					}
				}
				if (list.Count < 5)
				{
					throw new Exception(Lang.Text("Minecraft.Download.Error.ForgeRecommendedTooFew", new object[1] { text }));
				}
				string text3 = "{" + list.Join(",") + "}";
				ModBase.WriteFile(Path.Combine(ModBase.pathTemp, "Cache", "ForgeRecommendedList.json"), text3);
				ModBase.Log("[Download] 刷新 Forge 推荐版本缓存成功");
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "刷新 Forge 推荐版本缓存失败");
			}
		}, "ForgeRecommendedRefresh");
	}

	public static string McDownloadForgeRecommendedGet(string mcInstance)
	{
		try
		{
			if (mcInstance == null)
			{
				return null;
			}
			string text = ModBase.ReadFile(Path.Combine(ModBase.pathTemp, "Cache", "ForgeRecommendedList.json"));
			if (text == null || string.IsNullOrEmpty(text))
			{
				ModBase.Log("[Download] 没有 Forge 推荐版本缓存文件");
				return null;
			}
			JsonObject jsonObject = (JsonObject)ModBase.GetJson(text);
			if (jsonObject == null || !(mcInstance ?? "null").Contains(".") || !jsonObject.ContainsKey(mcInstance))
			{
				return null;
			}
			return (jsonObject[mcInstance] ?? ((JsonNode?)"")).ToString();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取 Forge 推荐版本失败（" + (mcInstance ?? "null") + "）", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
			return null;
		}
	}

	public static void NeoForgeDownloadListItemPreload(StackPanel stack, List<ModDownload.DlNeoForgeListEntry> entries, MyListItem.ClickEventHandler onClick, bool isSaveOnly)
	{
		if (entries.Count == 1)
		{
			return;
		}
		ModDownload.DlNeoForgeListEntry dlNeoForgeListEntry = null;
		ModDownload.DlNeoForgeListEntry dlNeoForgeListEntry2 = null;
		if (entries.Any())
		{
			foreach (ModDownload.DlNeoForgeListEntry item in entries.ToList())
			{
				if (item.IsBeta)
				{
					if (dlNeoForgeListEntry2 == null)
					{
						dlNeoForgeListEntry2 = item;
					}
					continue;
				}
				dlNeoForgeListEntry = item;
				break;
			}
		}
		else
		{
			ModBase.Log("[System] 未找到可用的 NeoForge 版本", ModBase.LogLevel.Debug);
		}
		if (dlNeoForgeListEntry != null)
		{
			MyListItem myListItem = NeoForgeDownloadListItem(dlNeoForgeListEntry, onClick, isSaveOnly);
			myListItem.Info = (string.IsNullOrEmpty(myListItem.Info) ? Lang.Text("Download.Version.Fresh.Stable") : (Lang.Text("Download.Version.Fresh.Latest") + "  |  " + myListItem.Info));
			stack.Children.Add(myListItem);
		}
		if (dlNeoForgeListEntry2 != null)
		{
			MyListItem myListItem2 = NeoForgeDownloadListItem(dlNeoForgeListEntry2, onClick, isSaveOnly);
			myListItem2.Info = (string.IsNullOrEmpty(myListItem2.Info) ? Lang.Text("Download.Version.Fresh.Development") : (Lang.Text("Download.Version.Fresh.Latest") + "  |  " + myListItem2.Info));
			stack.Children.Add(myListItem2);
		}
		stack.Children.Add(new TextBlock
		{
			Text = Lang.Text("Download.Version.AllVersions", new object[1] { entries.Count }),
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(6.0, 13.0, 0.0, 4.0)
		});
	}

	public static MyListItem NeoForgeDownloadListItem(ModDownload.DlNeoForgeListEntry info, MyListItem.ClickEventHandler onClick, bool isSaveOnly)
	{
		MyListItem myListItem = new MyListItem
		{
			Title = info.VersionName,
			SnapsToDevicePixels = true,
			Height = 42.0,
			Type = MyListItem.CheckType.Clickable,
			Tag = info,
			Info = (info.IsBeta ? Lang.Text("Download.Version.Type.Preview") : Lang.Text("Download.Version.Type.Stable")),
			Logo = ModBase.pathImage + "Blocks/NeoForge.png"
		};
		myListItem.Click += onClick;
		if (isSaveOnly)
		{
			myListItem.ContentHandler = NeoForgeSaveContMenuBuild;
		}
		else
		{
			myListItem.ContentHandler = NeoForgeContMenuBuild;
		}
		return myListItem;
	}

	private static void NeoForgeContMenuBuild(MyListItem sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			SvgIcon = "lucide/save",
			ToolTip = Lang.Text("Download.Version.SaveAs")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object sender2, EventArgs e2) =>
		{
			NeoForgeSave_Click(sender2, (RoutedEventArgs)e2);
		};
		MyIconButton myIconButton2 = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object sender2, EventArgs e2) =>
		{
			NeoForgeLog_Click(sender2, (RoutedEventArgs)e2);
		};
		sender.Buttons = new MyIconButton[2] { myIconButton, myIconButton2 };
	}

	private static void NeoForgeSaveContMenuBuild(MyListItem sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object sender2, EventArgs e2) =>
		{
			NeoForgeLog_Click(sender2, (RoutedEventArgs)e2);
		};
		sender.Buttons = new MyIconButton[1] { myIconButton };
	}

	private static void NeoForgeLog_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlNeoForgeListEntry dlNeoForgeListEntry;
		if ((object)((dynamic)sender).Tag != null)
		{
			dlNeoForgeListEntry = (ModDownload.DlNeoForgeListEntry)((dynamic)sender).Tag;
		}
		else
		{
			dlNeoForgeListEntry = (((object)((dynamic)sender).Parent.Tag == null) ? ((ModDownload.DlNeoForgeListEntry)((dynamic)sender).Parent.Parent.Tag) : ((ModDownload.DlNeoForgeListEntry)((dynamic)sender).Parent.Tag));
		}
		ModBase.OpenWebsite(dlNeoForgeListEntry.UrlBase + "-changelog.txt");
	}

	public static void NeoForgeSave_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlNeoForgeListEntry info;
		if ((object)((dynamic)sender).Tag != null)
		{
			info = (ModDownload.DlNeoForgeListEntry)((dynamic)sender).Tag;
		}
		else
		{
			info = (((object)((dynamic)sender).Parent.Tag == null) ? ((ModDownload.DlNeoForgeListEntry)((dynamic)sender).Parent.Parent.Tag) : ((ModDownload.DlNeoForgeListEntry)((dynamic)sender).Parent.Tag));
		}
		McDownloadForgelikeSave(info);
	}

	public static void CleanroomDownloadListItemPreload(StackPanel stack, List<ModDownload.DlCleanroomListEntry> entries, MyListItem.ClickEventHandler onClick, bool isSaveOnly)
	{
		ModDownload.DlCleanroomListEntry dlCleanroomListEntry = null;
		if (entries.Any())
		{
			dlCleanroomListEntry = entries[0];
		}
		else
		{
			ModBase.Log("[System] 未找到可用的 Cleanroom 版本", ModBase.LogLevel.Debug);
		}
		if (dlCleanroomListEntry != null)
		{
			MyListItem myListItem = CleanroomDownloadListItem(dlCleanroomListEntry, onClick, isSaveOnly);
			myListItem.Info = (string.IsNullOrEmpty(myListItem.Info) ? Lang.Text("Download.Version.Fresh.Development") : (Lang.Text("Download.Version.Fresh.Latest") + "  |  " + myListItem.Info));
			stack.Children.Add(myListItem);
		}
		stack.Children.Add(new TextBlock
		{
			Text = Lang.Text("Download.Version.AllVersions", new object[1] { entries.Count }),
			HorizontalAlignment = HorizontalAlignment.Left,
			Margin = new Thickness(6.0, 13.0, 0.0, 4.0)
		});
	}

	public static MyListItem CleanroomDownloadListItem(ModDownload.DlCleanroomListEntry info, MyListItem.ClickEventHandler onClick, bool isSaveOnly)
	{
		MyListItem myListItem = new MyListItem
		{
			Title = info.VersionName,
			SnapsToDevicePixels = true,
			Height = 42.0,
			Type = MyListItem.CheckType.Clickable,
			Tag = info,
			Info = (info.IsBeta ? Lang.Text("Download.Version.Type.Preview") : Lang.Text("Download.Version.Type.Stable")),
			Logo = ModBase.pathImage + "Blocks/Cleanroom.png"
		};
		myListItem.Click += onClick;
		if (isSaveOnly)
		{
			myListItem.ContentHandler = CleanroomSaveContMenuBuild;
		}
		else
		{
			myListItem.ContentHandler = CleanroomContMenuBuild;
		}
		return myListItem;
	}

	private static void CleanroomContMenuBuild(MyListItem sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			SvgIcon = "lucide/save",
			ToolTip = Lang.Text("Download.Version.SaveAs")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object sender2, EventArgs _e) =>
		{
			CleanroomSave_Click(sender2, (RoutedEventArgs)e);
		};
		MyIconButton myIconButton2 = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object sender2, EventArgs e2) =>
		{
			CleanroomLog_Click(sender2, (RoutedEventArgs)e2);
		};
		sender.Buttons = new MyIconButton[2] { myIconButton, myIconButton2 };
	}

	private static void CleanroomSaveContMenuBuild(MyListItem sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object a, EventArgs b) =>
		{
			ModDownloadLib.CleanroomLog_Click(a, (dynamic)b);
		};
		sender.Buttons = new MyIconButton[1] { myIconButton };
	}

	private static void CleanroomLog_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlCleanroomListEntry dlCleanroomListEntry;
		if ((object)((dynamic)sender).Tag != null)
		{
			dlCleanroomListEntry = (ModDownload.DlCleanroomListEntry)((dynamic)sender).Tag;
		}
		else
		{
			dlCleanroomListEntry = (((object)((dynamic)sender).Parent.Tag == null) ? ((ModDownload.DlCleanroomListEntry)((dynamic)sender).Parent.Parent.Tag) : ((ModDownload.DlCleanroomListEntry)((dynamic)sender).Parent.Tag));
		}
		ModBase.OpenWebsite(dlCleanroomListEntry.UrlBase + "-changelog.txt");
	}

	public static void CleanroomSave_Click(object sender, RoutedEventArgs e)
	{
		ModDownload.DlCleanroomListEntry info;
		if ((object)((dynamic)sender).Tag != null)
		{
			info = (ModDownload.DlCleanroomListEntry)((dynamic)sender).Tag;
		}
		else
		{
			info = (((object)((dynamic)sender).Parent.Tag == null) ? ((ModDownload.DlCleanroomListEntry)((dynamic)sender).Parent.Parent.Tag) : ((ModDownload.DlCleanroomListEntry)((dynamic)sender).Parent.Tag));
		}
		McDownloadForgelikeSave(info);
	}

	public static void McDownloadFabricLoaderSave(JsonObject downloadInfo)
	{
		try
		{
			string text = downloadInfo["url"].ToString();
			string fileNameFromPath = ModBase.GetFileNameFromPath(text);
			string fileNameFromPath2 = ModBase.GetFileNameFromPath(downloadInfo["version"].ToString());
			string text2 = SystemDialogs.SelectSaveFile(Lang.Text("Download.Version.SelectSaveLocation"), fileNameFromPath, Lang.Text("Download.Version.Installer.Fabric.Filter"), (string)null);
			if (!text2.Contains("\\"))
			{
				return;
			}
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.FabricInstallerDownload", new object[1] { fileNameFromPath2 }) ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
			List<string> list2 = new List<string>();
			list2.Add(text);
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadMainFile"), new List<DownloadFile>
			{
				new DownloadFile(list2.ToArray(), text2, new ModBase.FileChecker(65536L, -1L))
			})
			{
				ProgressWeight = 15.0
			});
			ModLoader.LoaderCombo<JsonObject> loaderCombo = new ModLoader.LoaderCombo<JsonObject>(Lang.Text("Minecraft.Download.Stage.FabricInstallerDownload", new object[1] { fileNameFromPath2 }), list);
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start(downloadInfo);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 Fabric 安装器下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	private static List<ModLoader.LoaderBase> McDownloadFabricLoader(string fabricVersion, string minecraftName, string mcFolder = null, bool fixLibrary = true)
	{
		mcFolder = mcFolder ?? ModFolder.mcFolderSelected;
		_ = (mcFolder ?? "") != (ModFolder.mcFolderSelected ?? "");
		string id = "fabric-loader-" + fabricVersion + "-" + minecraftName;
		string versionFolder = Path.Combine(mcFolder, "versions", id);
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		minecraftName = minecraftName.Replace("∞", "infinite");
		list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.ObtainFabricMainFileUrl"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
		{
			if (fixLibrary)
			{
				McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, minecraftName);
			}
			task.Progress = 0.5;
			string value = minecraftName.Replace("∞", "infinite");
			string text = $"https://bmclapi2.bangbang93.com/fabric-meta/v2/versions/loader/{value}/{fabricVersion}/profile/json";
			string text2 = $"https://meta.fabricmc.net/v2/versions/loader/{value}/{fabricVersion}/profile/json";
			string text3 = null;
			string[] array = new string[2] { text, text2 };
			foreach (string text4 in array)
			{
				try
				{
					text3 = Requester.FetchString(text4, new RequestParam
					{
						UseBrowserUserAgent = true,
						Timeout = 5000,
						Retries = 2
					});
					if (text3 != null)
					{
						break;
					}
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "[Download] 从 " + text4 + " 下载 Fabric meta 失败");
				}
			}
			if (text3 == null)
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.FabricMetaDownloadFailed", new object[1] { text + " and " + text2 }));
			}
			Directory.CreateDirectory(versionFolder);
			File.WriteAllText(Path.Combine(versionFolder, id + ".json"), text3, Encoding.UTF8);
			task.output = new List<DownloadFile>();
		})
		{
			ProgressWeight = 0.5
		});
		list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLoaderMainFile", new object[1] { "Fabric" }), new List<DownloadFile>())
		{
			ProgressWeight = 2.5
		});
		if (fixLibrary)
		{
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeFabricLibraries"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				task.output = ModLibrary.McLibNetFilesFromInstance(new McInstance(versionFolder));
			})
			{
				ProgressWeight = 1.0,
				show = false
			});
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLabyModClientJson"), new List<DownloadFile>())
			{
				ProgressWeight = 8.0
			});
		}
		return list;
	}

	public static void McDownloadLegacyFabricLoaderSave(JsonObject downloadInfo)
	{
		try
		{
			string text = downloadInfo["url"].ToString();
			string fileNameFromPath = ModBase.GetFileNameFromPath(text);
			string fileNameFromPath2 = ModBase.GetFileNameFromPath(downloadInfo["version"].ToString());
			string text2 = SystemDialogs.SelectSaveFile(Lang.Text("Download.Version.SelectSaveLocation"), fileNameFromPath, Lang.Text("Download.Version.Installer.LegacyFabric.Filter"), (string)null);
			if (!text2.Contains("\\"))
			{
				return;
			}
			foreach (ModLoader.LoaderBase item in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item.name ?? "") != (Lang.Text("Minecraft.Download.Stage.LegacyFabricInstallerDownload", new object[1] { fileNameFromPath2 }) ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
			List<string> list2 = new List<string>();
			list2.Add(text);
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadMainFile"), new List<DownloadFile>
			{
				new DownloadFile(list2.ToArray(), text2, new ModBase.FileChecker(65536L, -1L))
			})
			{
				ProgressWeight = 15.0
			});
			ModLoader.LoaderCombo<JsonObject> loaderCombo = new ModLoader.LoaderCombo<JsonObject>(Lang.Text("Minecraft.Download.Stage.LegacyFabricInstallerDownload", new object[1] { fileNameFromPath2 }), list);
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start(downloadInfo);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 Legacy Fabric 安装器下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	private static List<ModLoader.LoaderBase> McDownloadLegacyFabricLoader(string legacyFabricVersion, string minecraftName, string mcFolder = null, bool fixLibrary = true)
	{
		mcFolder = mcFolder ?? ModFolder.mcFolderSelected;
		_ = (mcFolder ?? "") != (ModFolder.mcFolderSelected ?? "");
		string id = "legacy-fabric-loader-" + legacyFabricVersion + "-" + minecraftName;
		string versionFolder = Path.Combine(mcFolder, "versions", id);
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.ObtainLegacyFabricMainFileUrl"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
		{
			if (fixLibrary)
			{
				McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, minecraftName);
			}
			task.Progress = 0.5;
			task.output = new List<DownloadFile>
			{
				new DownloadFile(new string[1] { "https://meta.legacyfabric.net/v2/versions/loader/" + minecraftName + "/" + legacyFabricVersion + "/profile/json" }, Path.Combine(versionFolder, id + ".json"), new ModBase.FileChecker(-1L, -1L, null, canUseExistsFile: true, isJson: true))
			};
		})
		{
			ProgressWeight = 0.5
		});
		list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLoaderMainFile", new object[1] { "Legacy Fabric" }), new List<DownloadFile>())
		{
			ProgressWeight = 2.5
		});
		if (fixLibrary)
		{
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeLegacyFabricLibraries"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				task.output = ModLibrary.McLibNetFilesFromInstance(new McInstance(versionFolder));
			})
			{
				ProgressWeight = 1.0,
				show = false
			});
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLoaderLibraries", new object[1] { "Legacy Fabric" }), new List<DownloadFile>())
			{
				ProgressWeight = 8.0
			});
		}
		return list;
	}

	public static MyListItem FabricDownloadListItem(JsonObject entry, MyListItem.ClickEventHandler onClick)
	{
		MyListItem myListItem = new MyListItem();
		myListItem.Title = entry["version"].ToString().Replace("+build", "");
		myListItem.SnapsToDevicePixels = true;
		myListItem.Height = 42.0;
		myListItem.Type = MyListItem.CheckType.Clickable;
		myListItem.Tag = entry;
		myListItem.Info = (JsonCompat.ToObject<bool>(entry["stable"]) ? Lang.Text("Download.Version.Type.Stable") : Lang.Text("Download.Version.Type.Preview"));
		myListItem.Logo = ModBase.pathImage + "Blocks/Fabric.png";
		myListItem.Click += onClick;
		myListItem.ContentHandler = FabricContMenuBuild;
		return myListItem;
	}

	private static void FabricContMenuBuild(object sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object a, EventArgs b) =>
		{
			ModDownloadLib.FabricLog_Click(a, (dynamic)b);
		};
		((dynamic)sender).Buttons = new MyIconButton[1] { myIconButton };
	}

	private static void FabricLog_Click(object sender, RoutedEventArgs e)
	{
		ModBase.OpenWebsite("https://fabricmc.net/blog");
	}

	public static MyListItem FabricApiDownloadListItem(ModComp.CompFile entry, MyListItem.ClickEventHandler onClick)
	{
		MyListItem myListItem = new MyListItem();
		myListItem.Title = entry.DisplayName.Split("]")[1].Replace("Fabric API ", "").Replace(" build ", ".").Trim();
		myListItem.SnapsToDevicePixels = true;
		myListItem.Height = 42.0;
		myListItem.Type = MyListItem.CheckType.Clickable;
		myListItem.Tag = entry;
		myListItem.Info = entry.StatusDescription + Lang.Text("Download.Version.ReleaseDate", new object[1] { Lang.Date(entry.ReleaseDate, "g") });
		myListItem.Logo = ModBase.pathImage + "Blocks/Fabric.png";
		myListItem.Click += onClick;
		return myListItem;
	}

	public static MyListItem OptiFabricDownloadListItem(ModComp.CompFile entry, MyListItem.ClickEventHandler onClick)
	{
		MyListItem myListItem = new MyListItem();
		myListItem.Title = entry.DisplayName.ToLower().Replace("optifabric-", "").Replace(".jar", "")
			.Trim()
			.TrimStart('v');
		myListItem.SnapsToDevicePixels = true;
		myListItem.Height = 42.0;
		myListItem.Type = MyListItem.CheckType.Clickable;
		myListItem.Tag = entry;
		myListItem.Info = entry.StatusDescription + Lang.Text("Download.Version.ReleaseDate", new object[1] { Lang.Date(entry.ReleaseDate, "g") });
		myListItem.Logo = ModBase.pathImage + "Blocks/OptiFabric.png";
		myListItem.Click += onClick;
		return myListItem;
	}

	public static MyListItem LegacyFabricDownloadListItem(JsonObject entry, MyListItem.ClickEventHandler onClick)
	{
		MyListItem myListItem = new MyListItem();
		myListItem.Title = entry["version"].ToString();
		myListItem.SnapsToDevicePixels = true;
		myListItem.Height = 42.0;
		myListItem.Type = MyListItem.CheckType.Clickable;
		myListItem.Tag = entry;
		myListItem.Info = (JsonCompat.ToObject<bool>(entry["stable"]) ? Lang.Text("Download.Version.Type.Stable") : Lang.Text("Download.Version.Type.Preview"));
		myListItem.Logo = ModBase.pathImage + "Blocks/Fabric.png";
		myListItem.Click += onClick;
		return myListItem;
	}

	public static MyListItem LegacyFabricApiDownloadListItem(ModComp.CompFile entry, MyListItem.ClickEventHandler onClick)
	{
		MyListItem myListItem = new MyListItem();
		myListItem.Title = entry.DisplayName.Replace("Legacy Fabric API ", "");
		myListItem.SnapsToDevicePixels = true;
		myListItem.Height = 42.0;
		myListItem.Type = MyListItem.CheckType.Clickable;
		myListItem.Tag = entry;
		myListItem.Info = entry.StatusDescription + Lang.Text("Download.Version.ReleaseDate", new object[1] { Lang.Date(entry.ReleaseDate, "g") });
		myListItem.Logo = ModBase.pathImage + "Blocks/Fabric.png";
		myListItem.Click += onClick;
		return myListItem;
	}

	public static void McDownloadLabyModProductionLoaderSave()
	{
		try
		{
			string item = "https://releases.labymod.net/api/v1/installer/production/java";
			string text = "LabyMod4ProductionInstaller.jar";
			string text2 = SystemDialogs.SelectSaveFile(Lang.Text("Download.Version.SelectSaveLocation"), text, Lang.Text("Download.Version.Installer.LabyMod.Filter"), (string)null);
			if (!text2.Contains("\\"))
			{
				return;
			}
			foreach (ModLoader.LoaderBase item2 in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item2.name ?? "") != (Lang.Text("Minecraft.Download.Stage.LabyModInstallerDownload") ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
			List<string> list2 = new List<string>();
			list2.Add(item);
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadMainFile"), new List<DownloadFile>
			{
				new DownloadFile(list2.ToArray(), text2, new ModBase.FileChecker(65536L, -1L))
			})
			{
				ProgressWeight = 15.0
			});
			ModLoader.LoaderCombo<JsonObject> loaderCombo = new ModLoader.LoaderCombo<JsonObject>(Lang.Text("Minecraft.Download.Stage.LabyModInstallerDownload"), list);
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start();
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 LabyMod 安装器下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	public static void McDownloadLabyModSnapshotLoaderSave()
	{
		try
		{
			string item = "https://releases.labymod.net/api/v1/installer/snapshot/java";
			string text = "LabyMod4SnapshotInstaller.jar";
			string text2 = SystemDialogs.SelectSaveFile(Lang.Text("Download.Version.SelectSaveLocation"), text, Lang.Text("Download.Version.Installer.LabyMod.Filter"), (string)null);
			if (!text2.Contains("\\"))
			{
				return;
			}
			foreach (ModLoader.LoaderBase item2 in ModLoader.loaderTaskbar.ToList())
			{
				if (!((item2.name ?? "") != (Lang.Text("Minecraft.Download.Stage.LabyModInstallerDownload") ?? "")))
				{
					HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceDownloading"), HintType.Error);
					return;
				}
			}
			List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
			List<string> list2 = new List<string>();
			list2.Add(item);
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadMainFile"), new List<DownloadFile>
			{
				new DownloadFile(list2.ToArray(), text2, new ModBase.FileChecker(65536L, -1L))
			})
			{
				ProgressWeight = 15.0
			});
			ModLoader.LoaderCombo<JsonObject> loaderCombo = new ModLoader.LoaderCombo<JsonObject>(Lang.Text("Minecraft.Download.Stage.LabyModInstallerDownload"), list);
			loaderCombo.OnStateChanged = LoaderStateChangedHintOnly;
			loaderCombo.Start();
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "开始 LabyMod 安装器下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
		}
	}

	private static List<ModLoader.LoaderBase> McDownloadLabyModLoader(string labyModCommitRef, string labyModChannel, string minecraftName, string mcFolder = null, bool fixLibrary = true)
	{
		mcFolder = mcFolder ?? ModFolder.mcFolderSelected;
		_ = (mcFolder ?? "") != (ModFolder.mcFolderSelected ?? "");
		string id = "labymod-" + labyModCommitRef + "-" + minecraftName;
		string versionFolder = Path.Combine(mcFolder, "versions", id);
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		minecraftName = minecraftName.Replace("∞", "infinite");
		list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.ObtainLabyModClientUrl"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
		{
			if (fixLibrary)
			{
				McDownloadClient(NetPreDownloadBehaviour.ExitWhileExistsOrDownloading, minecraftName, $"https://releases.r2.labymod.net/api/v1/download/manifest/labymod4/{labyModChannel}/{minecraftName}/{labyModCommitRef}.json");
			}
			task.Progress = 0.5;
			task.output = new List<DownloadFile>
			{
				new DownloadFile(new string[1] { $"https://releases.r2.labymod.net/api/v1/download/manifest/labymod4/{labyModChannel}/{minecraftName}/{labyModCommitRef}.json" }, Path.Combine(versionFolder, id + ".json"), new ModBase.FileChecker(-1L, -1L, null, canUseExistsFile: true, isJson: true))
			};
			task.Progress = 1.0;
		})
		{
			ProgressWeight = 2.0
		});
		list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLoaderLibraries", new object[1] { "Fabric" }), new List<DownloadFile>())
		{
			ProgressWeight = 10.0
		});
		if (fixLibrary)
		{
			list.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeLabyModLibraries"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
			{
				task.output = ModLibrary.McLibNetFilesFromInstance(new McInstance(versionFolder));
			})
			{
				ProgressWeight = 1.0,
				show = false
			});
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLoaderLibraries", new object[1] { "LabyMod" }), new List<DownloadFile>())
			{
				ProgressWeight = 8.0
			});
		}
		return list;
	}

	private static List<ModLoader.LoaderBase> McDownloadLabyModClientLoader(string id, string labyChannel, string labyCommitRef, string versionName = null)
	{
		versionName = versionName ?? id;
		string versionFolder = Path.Combine(ModFolder.mcFolderSelected, "versions", versionName) + "\\";
		return new List<ModLoader.LoaderBase>
		{
			new ModLoader.LoaderCombo<string>(loaders: new List<ModLoader.LoaderBase>
			{
				new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeVanillaAndLabyModLibrariesSide"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
				{
					ModBase.WaitForFileReady(Path.Combine(versionFolder, versionName + ".json"));
					ModBase.Log("[Download] 开始分析原版与 LabyMod 支持库文件：" + versionFolder);
					task.output = ModLibrary.McLibNetFilesFromInstance(new McInstance(versionFolder));
				})
				{
					ProgressWeight = 1.0,
					show = false
				},
				new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadVanillaAndLabyModLibrariesSide"), new List<DownloadFile>())
				{
					ProgressWeight = 13.0,
					show = false
				}
			}, name: mcDownloadClientLibName)
			{
				block = false,
				ProgressWeight = 14.0
			},
			new ModLoader.LoaderCombo<string>(loaders: new List<ModLoader.LoaderBase>
			{
				new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeAssetsIndex.Side"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
				{
					try
					{
						McInstance version = new McInstance(versionFolder);
						task.output = new List<DownloadFile> { ModDownload.DlClientAssetIndexGet(version) };
					}
					catch (Exception innerException)
					{
						throw new Exception(Lang.Text("Minecraft.Download.Error.AssetIndexAnalysisFailed"), innerException);
					}
					try
					{
						JsonObject jsonObject = (JsonObject)ModBase.GetJson(ModBase.ReadFile(Path.Combine(versionFolder, versionName + ".json")));
						jsonObject.Add("clientVersion", id);
						ModBase.WriteFile(Path.Combine(versionFolder, versionName + ".json"), jsonObject.ToString());
					}
					catch (Exception innerException2)
					{
						throw new Exception(Lang.Text("Minecraft.Download.Error.AddClientVersionFailed"), innerException2);
					}
				})
				{
					ProgressWeight = 1.0,
					show = false
				},
				new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadAssetsIndex.Side"), new List<DownloadFile>())
				{
					ProgressWeight = 3.0,
					show = false
				},
				new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeRequiredAssets.Side"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
				{
					ModLoader.LoaderBase progressFeed = task;
					task.output = ModAssets.McAssetsFixList(new McInstance(versionFolder), checkHash: true, ref progressFeed);
					task = (ModLoader.LoaderTask<string, List<DownloadFile>>)progressFeed;
				})
				{
					ProgressWeight = 3.0,
					show = false
				},
				new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadAssets.Side"), new List<DownloadFile>())
				{
					ProgressWeight = 14.0,
					show = false
				}
			}, name: Lang.Text("Minecraft.Download.Stage.DownloadVanillaAssets"))
			{
				block = false,
				ProgressWeight = 21.0
			}
		};
	}

	public static MyListItem LabyModDownloadListItem(JsonObject entry, MyListItem.ClickEventHandler onClick)
	{
		MyListItem myListItem = new MyListItem();
		myListItem.Title = entry["version"]?.ToString() + " " + (entry["channel"].ToString().Contains("snapshot") ? Lang.Text("Download.Version.Type.Snapshot") : Lang.Text("Download.Version.Type.Stable"));
		myListItem.SnapsToDevicePixels = true;
		myListItem.Height = 42.0;
		myListItem.Type = MyListItem.CheckType.Clickable;
		myListItem.Tag = entry;
		myListItem.Info = (entry["channel"].ToString().Contains("snapshot") ? Lang.Text("Download.Version.Type.Snapshot") : Lang.Text("Download.Version.Type.Stable"));
		myListItem.Logo = ModBase.pathImage + "Blocks/LabyMod.png";
		myListItem.Click += onClick;
		myListItem.ContentHandler = LabyModContMenuBuild;
		return myListItem;
	}

	private static void LabyModContMenuBuild(object sender, EventArgs e)
	{
		MyIconButton myIconButton = new MyIconButton
		{
			SvgIcon = "lucide/save",
			ToolTip = Lang.Text("Download.Version.SaveAs")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
		myIconButton.Click += (object a, EventArgs b) =>
		{
			ModDownloadLib.LabyModSave_Click(a, (dynamic)b);
		};
		MyIconButton myIconButton2 = new MyIconButton
		{
			LogoScale = 1.05,
			SvgIcon = "lucide/info",
			ToolTip = Lang.Text("Download.Version.Changelog")
		};
		ToolTipService.SetPlacement((DependencyObject)(object)myIconButton2, PlacementMode.Center);
		ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton2, 30.0);
		ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton2, 2.0);
		myIconButton2.Click += (object a, EventArgs b) =>
		{
			ModDownloadLib.LabyModLog_Click(a, (dynamic)b);
		};
		((dynamic)sender).Buttons = new MyIconButton[2] { myIconButton, myIconButton2 };
	}

	private static void LabyModLog_Click(object sender, RoutedEventArgs e)
	{
		ModBase.OpenWebsite("https://www.labymod.net/zh_Hans/download");
	}

	private static void LabyModSave_Click(object sender, RoutedEventArgs e)
	{
		JsonObject jsonObject;
		if ((object)((dynamic)sender).Tag != null)
		{
			jsonObject = (JsonObject)((dynamic)sender).Tag;
		}
		else
		{
			jsonObject = (((object)((dynamic)sender).Parent.Tag == null) ? ((JsonObject)((dynamic)sender).Parent.Parent.Tag) : ((JsonObject)((dynamic)sender).Parent.Tag));
		}
		if ((string?)jsonObject["channel"] == "snapshot")
		{
			McDownloadLabyModSnapshotLoaderSave();
		}
		else
		{
			McDownloadLabyModProductionLoaderSave();
		}
	}

	public static void LoaderStateChangedHintOnly(object loaderObj)
	{
		ModLoader.LoaderBase loaderBase = (ModLoader.LoaderBase)loaderObj;
		switch (loaderBase.State)
		{
		case ModBase.LoadState.Finished:
			HintService.Hint(loaderBase.name + Lang.Text("Common.Status.Success"), HintType.Success);
			break;
		case ModBase.LoadState.Failed:
			HintService.Hint(loaderBase.name + Lang.Text("Common.Status.Failure") + loaderBase.Error.Message, HintType.Error);
			break;
		case ModBase.LoadState.Aborted:
			HintService.Hint(loaderBase.name + Lang.Text("Common.Status.Cancelled"));
			break;
		}
	}

	public static void McInstallState(object loaderObj)
	{
		ModLoader.LoaderBase loaderBase = (ModLoader.LoaderBase)loaderObj;
		ModLoader.LoaderCombo loaderCombo = (ModLoader.LoaderCombo)loaderBase;
		switch (loaderBase.State)
		{
		case ModBase.LoadState.Finished:
			if (Config.Download.AutoSelectInstance)
			{
				string name = loaderBase.name;
				ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "Version", name.Remove(name.Length - 3, 3));
			}
			ModBase.WriteIni(ModFolder.mcFolderSelected + "PCL.ini", "InstanceCache", "");
			ModBase.DeleteDirectory($"{loaderCombo.input}PCLInstallBackups\\");
			HintService.Hint(loaderBase.name + Lang.Text("Common.Status.Success"), HintType.Success);
			break;
		case ModBase.LoadState.Failed:
			HintService.Hint(loaderBase.name + Lang.Text("Common.Status.Failure") + loaderBase.Error.Message, HintType.Error);
			break;
		case ModBase.LoadState.Aborted:
			HintService.Hint(loaderBase.name + Lang.Text("Common.Status.Cancelled"));
			break;
		case ModBase.LoadState.Loading:
			return;
		}
		if (loaderBase.State != ModBase.LoadState.Finished && Directory.Exists($"{loaderCombo.input}PCLInstallBackups\\"))
		{
			ModBase.CopyDirectory($"{loaderCombo.input}PCLInstallBackups\\", (string)loaderCombo.input);
			File.Delete($"{loaderCombo.input}.pclignore");
			ModBase.DeleteDirectory($"{loaderCombo.input}PCLInstallBackups\\");
		}
		else
		{
			McInstallFailedClearFolder(loaderBase);
		}
		ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\");
	}

	public static void McInstallFailedClearFolder(object loader)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Thread.Sleep(1000);
			if (((ModLoader.LoaderBase)loader).State == ModBase.LoadState.Failed || ((ModLoader.LoaderBase)loader).State == ModBase.LoadState.Aborted)
			{
				ModBase.Log($"[Download] 由于下载失败或取消，清理实例文件夹：{((ModLoader.LoaderCombo)loader).input}", ModBase.LogLevel.Developer);
				string text = (string)((ModLoader.LoaderCombo)loader).input;
				((DynamicCacheConfigStorage)ConfigService.GetProvider((ConfigSource)3)).InvalidateCache((object)text);
				ModBase.DeleteDirectory(text);
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "下载失败或取消后清理实例文件夹失败");
		}
	}

	public static bool McInstall(McInstallRequest request, string type = "安装")
	{
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		if (!RestrictedFeaturePolicy.Check("minecraft-install"))
		{
			return false;
		}
		try
		{
			List<ModLoader.LoaderBase> list = McInstallLoader(request, dontFixLibraries: false, type != "安装");
			if (list == null)
			{
				return false;
			}
			ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(request.targetInstanceName + " " + type, list);
			loaderCombo.OnStateChanged = McInstallState;
			loaderCombo.Start(request.targetInstanceFolder);
			ModLoader.LoaderTaskbarAdd(loaderCombo);
			ModMain.frmMain.BtnExtraDownload.ShowRefresh();
			ModMain.frmMain.BtnExtraDownload.Ribble();
			return true;
		}
		catch (ModBase.CancelledException)
		{
			return false;
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "开始合并安装失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Download.Error.OperationFailed"));
			try
			{
				if (Directory.Exists(request.targetInstanceFolder))
				{
					string[] files = Directory.GetFiles(request.targetInstanceFolder);
					string[] directories = Directory.GetDirectories(request.targetInstanceFolder);
					if (files.Length <= 1 && directories.Length == 0)
					{
						((DynamicCacheConfigStorage)ConfigService.GetProvider((ConfigSource)3)).InvalidateCache((object)request.targetInstanceFolder);
						ModBase.DeleteDirectory(request.targetInstanceFolder);
					}
				}
			}
			catch (Exception ex3)
			{
				ModBase.Log(ex3, "清理未完成的实例文件夹失败");
			}
			return false;
		}
	}

	public static List<ModLoader.LoaderBase> McInstallLoader(McInstallRequest request, bool dontFixLibraries = false, bool ignoreDump = false)
	{
		RestrictedFeaturePolicy.ThrowIfRestricted("minecraft-install");
		string tempMcFolder = ModMain.RequestTaskTempFolder(request.optiFineEntry != null || request.forgeEntry != null || request.neoForgeEntry != null);
		string instanceFolder = Path.Combine(ModFolder.mcFolderSelected, "versions", request.targetInstanceName);
		if (Directory.Exists(tempMcFolder))
		{
			ModBase.DeleteDirectory(tempMcFolder);
		}
		string optiFineFolder = null;
		if (request.optiFineVersion != null)
		{
			if (request.optiFineVersion.Contains("_HD_U_"))
			{
				request.optiFineVersion = "HD_U_" + request.optiFineVersion.AfterLast("_HD_U_");
			}
			request.optiFineEntry = new ModDownload.DlOptiFineListEntry
			{
				DisplayName = request.minecraftName + " " + request.optiFineVersion.Replace("HD_U_", "").Replace("_", "").Replace("pre", " pre"),
				Inherit = request.minecraftName,
				IsPreview = request.optiFineVersion.ContainsF("pre", ignoreCase: true),
				NameVersion = request.minecraftName + "-OptiFine_" + request.optiFineVersion,
				NameFile = (request.optiFineVersion.ContainsF("pre", ignoreCase: true) ? "preview_" : "") + "OptiFine_" + request.minecraftName + "_" + request.optiFineVersion + ".jar"
			};
		}
		if (request.optiFineEntry != null)
		{
			optiFineFolder = Path.Combine(tempMcFolder, "versions", request.optiFineEntry.NameVersion);
		}
		string forgeFolder = null;
		if (request.forgeEntry != null)
		{
			request.forgeVersion = request.forgeVersion ?? request.forgeEntry.VersionName;
		}
		if (request.forgeVersion != null)
		{
			forgeFolder = Path.Combine(tempMcFolder, "versions", "forge-" + request.forgeVersion);
		}
		string neoForgeFolder = null;
		if (request.neoForgeEntry != null)
		{
			request.neoForgeVersion = request.neoForgeVersion ?? request.neoForgeEntry.VersionName;
		}
		if (request.neoForgeVersion != null)
		{
			neoForgeFolder = Path.Combine(tempMcFolder, "versions", "neoforge-" + request.neoForgeVersion);
		}
		string cleanroomFolder = null;
		if (request.cleanroomEntry != null)
		{
			request.cleanroomVersion = request.cleanroomVersion ?? request.cleanroomEntry.VersionName;
		}
		if (request.cleanroomVersion != null)
		{
			cleanroomFolder = Path.Combine(tempMcFolder, "versions", "cleanroom-" + request.cleanroomVersion);
		}
		string fabricFolder = null;
		if (request.fabricVersion != null)
		{
			fabricFolder = Path.Combine(tempMcFolder, "versions", "fabric-loader-" + request.fabricVersion + "-" + request.minecraftName);
		}
		string legacyFabricFolder = null;
		if (request.legacyFabricVersion != null)
		{
			legacyFabricFolder = Path.Combine(tempMcFolder, "versions", "legacy-fabric-loader-" + request.legacyFabricVersion + "-" + request.minecraftName);
		}
		string labyModFolder = null;
		if (request.labyModCommitRef != null)
		{
			labyModFolder = Path.Combine(tempMcFolder, "versions", "labymod-" + request.labyModCommitRef + "-" + request.minecraftName);
		}
		string liteLoaderFolder = null;
		if (request.liteLoaderEntry != null)
		{
			liteLoaderFolder = Path.Combine(tempMcFolder, "versions", request.minecraftName + "-LiteLoader");
		}
		bool modable = request.fabricVersion != null || request.legacyFabricVersion != null || request.forgeEntry != null || request.neoForgeEntry != null || request.liteLoaderEntry != null;
		string modsTempFolder = Path.Combine(tempMcFolder, "mods") + "\\";
		bool optiFineAsMod = (request.optiFineEntry != null) & modable;
		if (optiFineAsMod)
		{
			ModBase.Log("[Download] OptiFine 将作为 Mod 进行下载");
			if (request.liteLoaderEntry != null)
			{
				optiFineFolder = CombineCacheSubfolder(modsTempFolder, request.minecraftName);
			}
			else
			{
				optiFineFolder = modsTempFolder;
			}
		}
		if (optiFineFolder != null)
		{
			ModBase.Log("[Download] OptiFine 缓存：" + optiFineFolder);
		}
		if (forgeFolder != null)
		{
			ModBase.Log("[Download] Forge 缓存：" + forgeFolder);
		}
		if (neoForgeFolder != null)
		{
			ModBase.Log("[Download] NeoForge 缓存：" + neoForgeFolder);
		}
		if (cleanroomFolder != null)
		{
			ModBase.Log("[Download] Cleanroom 缓存：" + cleanroomFolder);
		}
		if (fabricFolder != null)
		{
			ModBase.Log("[Download] Fabric 缓存：" + fabricFolder);
		}
		if (legacyFabricFolder != null)
		{
			ModBase.Log("[Download] LegacyFabric 缓存：" + legacyFabricFolder);
		}
		if (labyModFolder != null)
		{
			ModBase.Log("[Download] LabyMod 缓存：" + labyModFolder);
		}
		if (liteLoaderFolder != null)
		{
			ModBase.Log("[Download] LiteLoader 缓存：" + liteLoaderFolder);
		}
		ModBase.Log("[Download] 对应的原版版本：" + request.minecraftName);
		if (File.Exists(Path.Combine(instanceFolder, request.targetInstanceName + ".json")) && !ignoreDump)
		{
			HintService.Hint(Lang.Text("Minecraft.Download.Error.InstanceAlreadyExists", new object[2] { request.targetInstanceName, "" }), HintType.Error);
			throw new ModBase.CancelledException();
		}
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>();
		list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Minecraft.Download.Stage.AddIgnoreFlag"), (ModLoader.LoaderTask<int, int> _) =>
		{
			ModBase.WriteFile(Path.Combine(instanceFolder, ".pclignore"), "用于临时地在 PCL 的实例列表中屏蔽此实例。");
		})
		{
			show = false,
			block = false
		});
		if (request.fabricApi != null)
		{
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadFabricApi"), new List<DownloadFile> { request.fabricApi.ToNetFile(modsTempFolder, ModComp.DownloadReason.Dependency, null, ModComp.CompLoaderType.Fabric) })
			{
				ProgressWeight = 3.0,
				block = false
			});
		}
		if (request.legacyFabricApi != null)
		{
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadLegacyFabricApi"), new List<DownloadFile> { request.legacyFabricApi.ToNetFile(modsTempFolder, ModComp.DownloadReason.Dependency, null, ModComp.CompLoaderType.Fabric) })
			{
				ProgressWeight = 3.0,
				block = false
			});
		}
		if (request.optiFabric != null)
		{
			list.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadOptiFabric"), new List<DownloadFile> { request.optiFabric.ToNetFile(modsTempFolder, ModComp.DownloadReason.Dependency, null, ModComp.CompLoaderType.Fabric) })
			{
				ProgressWeight = 3.0,
				block = false
			});
		}
		if (request.labyModCommitRef != null)
		{
			list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2] { "LabyMod", request.labyModCommitRef }), McDownloadLabyModLoader(request.labyModCommitRef, request.labyModChannel, request.minecraftName, tempMcFolder, fixLibrary: false))
			{
				show = false,
				ProgressWeight = 10.0,
				block = true
			});
		}
		else
		{
			ModLoader.LoaderCombo<string> loaderCombo = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2]
			{
				Lang.Text("Minecraft.Version.Vanilla"),
				request.minecraftName
			}), McDownloadClientLoader(request.minecraftName, request.minecraftJson, request.targetInstanceName))
			{
				show = false,
				ProgressWeight = 39.0,
				block = (request.forgeVersion == null && request.neoForgeVersion == null && request.optiFineEntry == null && request.fabricVersion == null && request.liteLoaderEntry == null && request.cleanroomEntry == null && request.legacyFabricVersion == null)
			};
			list.Add(loaderCombo);
			if (request.optiFineEntry != null)
			{
				if (optiFineAsMod)
				{
					list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2]
					{
						"OptiFine",
						request.optiFineEntry.DisplayName
					}), McDownloadOptiFineSaveLoader(request.optiFineEntry, Path.Combine(optiFineFolder, request.optiFineEntry.NameFile)))
					{
						show = false,
						ProgressWeight = 16.0,
						block = (request.forgeVersion == null && request.neoForgeVersion == null && request.fabricVersion == null && request.liteLoaderEntry == null)
					});
				}
				else
				{
					list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2]
					{
						"OptiFine",
						request.optiFineEntry.DisplayName
					}), McDownloadOptiFineLoader(request.optiFineEntry, tempMcFolder, loaderCombo, request.targetInstanceFolder, fixLibrary: false))
					{
						show = false,
						ProgressWeight = 24.0,
						block = (request.forgeVersion == null && request.neoForgeVersion == null && request.fabricVersion == null && request.liteLoaderEntry == null)
					});
				}
			}
			if (request.forgeVersion != null)
			{
				list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2] { "Forge", request.forgeVersion }), McDownloadForgelikeLoader(ModDownload.DlForgelikeEntry.ForgelikeType.Forge, request.forgeVersion, "forge-" + request.forgeVersion, request.minecraftName, request.forgeEntry, tempMcFolder, loaderCombo, request.targetInstanceFolder))
				{
					show = false,
					ProgressWeight = 25.0,
					block = (request.fabricVersion == null && request.liteLoaderEntry == null && request.neoForgeEntry == null)
				});
			}
			if (request.neoForgeVersion != null)
			{
				list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2] { "NeoForge", request.neoForgeVersion }), McDownloadForgelikeLoader(ModDownload.DlForgelikeEntry.ForgelikeType.NeoForge, request.neoForgeVersion, "neoforge-" + request.neoForgeVersion, request.minecraftName, request.neoForgeEntry, tempMcFolder, loaderCombo, request.targetInstanceFolder))
				{
					show = false,
					ProgressWeight = 25.0,
					block = (request.forgeEntry == null && request.fabricVersion == null && request.liteLoaderEntry == null)
				});
			}
			if (request.cleanroomVersion != null)
			{
				list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2] { "Cleanroom", request.cleanroomVersion }), McDownloadForgelikeLoader(ModDownload.DlForgelikeEntry.ForgelikeType.Cleanroom, request.cleanroomVersion, "cleanroom-" + request.cleanroomVersion, request.minecraftName, request.cleanroomEntry, tempMcFolder, loaderCombo, request.targetInstanceFolder))
				{
					show = false,
					ProgressWeight = 25.0,
					block = (request.forgeEntry == null && request.fabricVersion == null && request.liteLoaderEntry == null)
				});
			}
			if (request.liteLoaderEntry != null)
			{
				list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2] { "LiteLoader", request.minecraftName }), McDownloadLiteLoaderLoader(request.liteLoaderEntry, tempMcFolder, loaderCombo, fixLibrary: false))
				{
					show = false,
					ProgressWeight = 1.0,
					block = (request.fabricVersion == null)
				});
			}
			if (request.fabricVersion != null)
			{
				list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2] { "Fabric", request.fabricVersion }), McDownloadFabricLoader(request.fabricVersion, request.minecraftName, tempMcFolder, fixLibrary: false))
				{
					show = false,
					ProgressWeight = 2.0,
					block = true
				});
			}
			if (request.legacyFabricVersion != null)
			{
				list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2] { "Legacy Fabric", request.legacyFabricVersion }), McDownloadLegacyFabricLoader(request.legacyFabricVersion, request.minecraftName, tempMcFolder, fixLibrary: false))
				{
					show = false,
					ProgressWeight = 2.0,
					block = true
				});
			}
		}
		list.Add(new ModLoader.LoaderTask<string, string>(Lang.Text("Minecraft.Download.Stage.InstallGame"), (ModLoader.LoaderTask<string, string> task) =>
		{
			MergeJson(instanceFolder, instanceFolder, optiFineFolder, optiFineAsMod, forgeFolder, request.forgeVersion, neoForgeFolder, request.neoForgeVersion, cleanroomFolder, request.cleanroomVersion, fabricFolder, labyModFolder, request.labyModChannel, liteLoaderFolder, request.mmcPackInfo, legacyFabricFolder);
			task.Progress = 0.2;
			if (Directory.Exists(Path.Combine(tempMcFolder, "libraries")))
			{
				ModBase.CopyDirectory(Path.Combine(tempMcFolder, "libraries"), Path.Combine(ModFolder.mcFolderSelected, "libraries"));
			}
			task.Progress = 0.8;
			string text = Path.Combine(new McInstance(instanceFolder).PathIndie, "mods");
			if (Directory.Exists(modsTempFolder))
			{
				ModBase.CopyDirectory(modsTempFolder, text);
			}
			else if (modable)
			{
				Directory.CreateDirectory(text);
				ModBase.Log("[Download] 自动创建 Mod 文件夹：" + text);
			}
			string text2 = Path.Combine(new McInstance(instanceFolder).PathIndie, "resourcepacks");
			Directory.CreateDirectory(text2);
			ModBase.Log("[Download] 自动创建资源包文件夹：" + text2);
		})
		{
			ProgressWeight = 2.0,
			block = true
		});
		if (!dontFixLibraries && (request.mmcPackInfo != null || request.optiFineEntry != null || (request.forgeVersion != null && Convert.ToDouble(request.forgeVersion.BeforeFirst(".")) >= 20.0) || request.neoForgeVersion != null || request.fabricVersion != null || request.cleanroomVersion != null || request.liteLoaderEntry != null || request.labyModCommitRef != null))
		{
			List<ModLoader.LoaderBase> list2 = new List<ModLoader.LoaderBase>();
			if (request.labyModCommitRef != null)
			{
				ModLoader.LoaderCombo<string> item = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.LoaderDownloadCombo", new object[2]
				{
					Lang.Text("Minecraft.Version.Vanilla"),
					request.minecraftName
				}), McDownloadLabyModClientLoader(request.minecraftName, request.labyModChannel, request.labyModCommitRef, request.targetInstanceName))
				{
					show = false,
					ProgressWeight = 39.0,
					block = false
				};
				list.Add(item);
			}
			else
			{
				list2.Add(new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Download.Stage.AnalyzeGameLibrariesSide"), (ModLoader.LoaderTask<string, List<DownloadFile>> task) =>
				{
					task.output = ModLibrary.McLibNetFilesFromInstance(new McInstance(instanceFolder));
				})
				{
					ProgressWeight = 1.0,
					show = false
				});
				list2.Add(new LoaderDownload(Lang.Text("Minecraft.Download.Stage.DownloadGameLibrariesSide"), new List<DownloadFile>())
				{
					ProgressWeight = 7.0,
					show = false
				});
				list.Add(new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Download.Stage.DownloadGameLibraries"), list2)
				{
					ProgressWeight = 8.0
				});
			}
		}
		list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Minecraft.Download.Stage.DeleteIgnoreFlag"), (ModLoader.LoaderTask<int, int> _) =>
		{
			File.Delete(Path.Combine(instanceFolder, ".pclignore"));
		})
		{
			show = false
		});
		return list;
	}

	private static void MergeJson(string outputFolder, string minecraftFolder, string optiFineFolder = null, bool optiFineAsMod = false, string forgeFolder = null, string forgeVersion = null, string neoForgeFolder = null, string neoForgeVersion = null, string cleanroomFolder = null, string cleanroomVersion = null, string fabricFolder = null, string labyModFolder = null, string labyModChannel = null, string liteLoaderFolder = null, ModModpack.MMCPackInfo mMCPackInfo = null, string legacyFabricFolder = null)
	{
		ModBase.Log("[Download] 开始进行实例合并，输出：" + outputFolder + "，Minecraft：" + minecraftFolder + ((optiFineFolder != null) ? ("，OptiFine：" + optiFineFolder) : "") + ((forgeFolder != null) ? ("，Forge：" + forgeFolder) : "") + ((neoForgeFolder != null) ? ("，NeoForge：" + neoForgeFolder) : "") + ((cleanroomFolder != null) ? ("，Cleanroom：" + cleanroomFolder) : "") + ((liteLoaderFolder != null) ? ("，LiteLoader：" + liteLoaderFolder) : "") + ((fabricFolder != null) ? ("，Fabric：" + fabricFolder) : "") + ((legacyFabricFolder != null) ? ("，LegacyFabric：" + legacyFabricFolder) : "") + ((labyModFolder != null) ? ("，LabyMod：" + labyModFolder) : ""));
		Directory.CreateDirectory(outputFolder);
		bool flag = optiFineFolder != null && !optiFineAsMod;
		bool flag2 = forgeFolder != null;
		bool flag3 = legacyFabricFolder != null;
		bool flag4 = neoForgeFolder != null;
		bool flag5 = cleanroomFolder != null;
		bool flag6 = liteLoaderFolder != null;
		bool flag7 = fabricFolder != null;
		bool flag8 = labyModFolder != null;
		string text = null;
		string text2 = null;
		string text3 = null;
		string text4 = null;
		string text5 = null;
		string text6 = null;
		string text7 = null;
		string filePath = null;
		if (!outputFolder.EndsWithF("\\"))
		{
			outputFolder += "\\";
		}
		string folderNameFromPath = ModBase.GetFolderNameFromPath(outputFolder);
		string filePath2 = Path.Combine(outputFolder, folderNameFromPath + ".json");
		string text8 = Path.Combine(outputFolder, folderNameFromPath + ".jar");
		if (!minecraftFolder.EndsWithF("\\"))
		{
			minecraftFolder += "\\";
		}
		string folderNameFromPath2 = ModBase.GetFolderNameFromPath(minecraftFolder);
		string text9 = Path.Combine(minecraftFolder, folderNameFromPath2 + ".json");
		string text10 = Path.Combine(minecraftFolder, folderNameFromPath2 + ".jar");
		if (flag)
		{
			if (!optiFineFolder.EndsWithF("\\"))
			{
				optiFineFolder += "\\";
			}
			string folderNameFromPath3 = ModBase.GetFolderNameFromPath(optiFineFolder);
			text = Path.Combine(optiFineFolder, folderNameFromPath3 + ".json");
		}
		if (flag2)
		{
			if (!forgeFolder.EndsWithF("\\"))
			{
				forgeFolder += "\\";
			}
			string folderNameFromPath4 = ModBase.GetFolderNameFromPath(forgeFolder);
			text2 = Path.Combine(forgeFolder, folderNameFromPath4 + ".json");
		}
		if (flag4)
		{
			if (!neoForgeFolder.EndsWithF("\\"))
			{
				neoForgeFolder += "\\";
			}
			string folderNameFromPath5 = ModBase.GetFolderNameFromPath(neoForgeFolder);
			text3 = Path.Combine(neoForgeFolder, folderNameFromPath5 + ".json");
		}
		if (flag5)
		{
			if (!cleanroomFolder.EndsWithF("\\"))
			{
				cleanroomFolder += "\\";
			}
			string folderNameFromPath6 = ModBase.GetFolderNameFromPath(cleanroomFolder);
			text4 = Path.Combine(cleanroomFolder, folderNameFromPath6 + ".json");
		}
		if (flag6)
		{
			if (!liteLoaderFolder.EndsWithF("\\"))
			{
				liteLoaderFolder += "\\";
			}
			string folderNameFromPath7 = ModBase.GetFolderNameFromPath(liteLoaderFolder);
			text5 = Path.Combine(liteLoaderFolder, folderNameFromPath7 + ".json");
		}
		if (flag7)
		{
			if (!fabricFolder.EndsWithF("\\"))
			{
				fabricFolder += "\\";
			}
			string folderNameFromPath8 = ModBase.GetFolderNameFromPath(fabricFolder);
			text6 = Path.Combine(fabricFolder, folderNameFromPath8 + ".json");
		}
		if (flag3)
		{
			if (!legacyFabricFolder.EndsWithF("\\"))
			{
				legacyFabricFolder += "\\";
			}
			string folderNameFromPath9 = ModBase.GetFolderNameFromPath(legacyFabricFolder);
			filePath = Path.Combine(legacyFabricFolder, folderNameFromPath9 + ".json");
		}
		if (flag8)
		{
			if (!labyModFolder.EndsWithF("\\"))
			{
				labyModFolder += "\\";
			}
			string folderNameFromPath10 = ModBase.GetFolderNameFromPath(labyModFolder);
			text7 = Path.Combine(labyModFolder, folderNameFromPath10 + ".json");
		}
		JsonObject jsonObject = null;
		JsonObject jsonObject2 = null;
		JsonObject jsonObject3 = null;
		JsonObject jsonObject4 = null;
		JsonObject jsonObject5 = null;
		JsonObject jsonObject6 = null;
		JsonObject jsonObject7 = null;
		JsonObject jsonObject8 = null;
		JsonObject jsonObject9 = null;
		string text11 = ModBase.ReadFile(text9);
		if (!flag8)
		{
			if (!text11.StartsWithF("{"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.JsonInvalid", new object[3]
				{
					"Minecraft",
					text9,
					text11.Substring(0, Math.Min(text11.Length, 1000))
				}));
			}
			jsonObject = (JsonObject)ModBase.GetJson(text11);
		}
		if (flag)
		{
			string text12 = ModBase.ReadFile(text);
			if (!text12.StartsWithF("{"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.JsonInvalid", new object[3]
				{
					"OptiFine",
					text,
					text12.Substring(0, Math.Min(text12.Length, 1000))
				}));
			}
			jsonObject2 = (JsonObject)ModBase.GetJson(text12);
		}
		if (flag2)
		{
			string text13 = ModBase.ReadFile(text2);
			if (!text13.StartsWithF("{"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.JsonInvalid", new object[3]
				{
					"Forge",
					text2,
					text13.Substring(0, Math.Min(text13.Length, 1000))
				}));
			}
			jsonObject3 = (JsonObject)ModBase.GetJson(text13);
		}
		if (flag4)
		{
			string text14 = ModBase.ReadFile(text3);
			if (!text14.StartsWithF("{"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.JsonInvalid", new object[3]
				{
					"NeoForge",
					text3,
					text14.Substring(0, Math.Min(text14.Length, 1000))
				}));
			}
			jsonObject4 = (JsonObject)ModBase.GetJson(text14);
		}
		if (flag5)
		{
			string text15 = ModBase.ReadFile(text4);
			if (!text15.StartsWithF("{"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.JsonInvalid", new object[3]
				{
					"Cleanroom",
					text4,
					text15.Substring(0, Math.Min(text15.Length, 1000))
				}));
			}
			jsonObject6 = (JsonObject)ModBase.GetJson(text15);
		}
		if (flag6)
		{
			string text16 = ModBase.ReadFile(text5);
			if (!text16.StartsWithF("{"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.JsonInvalid", new object[3]
				{
					"LiteLoader",
					text5,
					text16.Substring(0, Math.Min(text16.Length, 1000))
				}));
			}
			jsonObject7 = (JsonObject)ModBase.GetJson(text16);
		}
		if (flag7)
		{
			string text17 = ModBase.ReadFile(text6);
			if (!text17.StartsWithF("{"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.JsonInvalid", new object[3]
				{
					"Fabric",
					text6,
					text17.Substring(0, Math.Min(text17.Length, 1000))
				}));
			}
			jsonObject8 = (JsonObject)ModBase.GetJson(text17);
		}
		if (flag3)
		{
			string text18 = ModBase.ReadFile(filePath);
			if (!text18.StartsWithF("{"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.JsonInvalid", new object[3]
				{
					"Legacy Fabric",
					text6,
					text18.Substring(0, Math.Min(text18.Length, 1000))
				}));
			}
			jsonObject5 = (JsonObject)ModBase.GetJson(text18);
		}
		if (flag8)
		{
			string text19 = ModBase.ReadFile(text7);
			if (!text19.StartsWithF("{"))
			{
				throw new Exception(Lang.Text("Minecraft.Download.Error.JsonInvalid", new object[3]
				{
					"LabyMod",
					text7,
					text19.Substring(0, Math.Min(text19.Length, 1000))
				}));
			}
			jsonObject9 = (JsonObject)ModBase.GetJson(text19);
		}
		List<string> list = (from l in (((jsonObject != null) ? (jsonObject["minecraftArguments"] ?? ((JsonNode?)" ")).ToString() : " ") + " " + ((jsonObject9 != null) ? (jsonObject9["minecraftArguments"] ?? ((JsonNode?)" ")).ToString() : " ") + " " + ((jsonObject2 != null) ? (jsonObject2["minecraftArguments"] ?? ((JsonNode?)" ")).ToString() : " ") + " " + ((jsonObject3 != null) ? (jsonObject3["minecraftArguments"] ?? ((JsonNode?)" ")).ToString() : " ") + " " + ((jsonObject4 != null) ? (jsonObject4["minecraftArguments"] ?? ((JsonNode?)" ")).ToString() : " ") + " " + ((jsonObject7 != null) ? (jsonObject7["minecraftArguments"] ?? ((JsonNode?)" ")).ToString() : " ") + " " + ((jsonObject6 != null) ? (jsonObject6["minecraftArguments"] ?? ((JsonNode?)" ")).ToString() : " ")).Split(" ")
			where !string.IsNullOrEmpty(l)
			select l.Trim()).ToList();
		List<string> list2 = new List<string>();
		int num = 0;
		for (int num2 = list.Count - 1; num <= num2; num++)
		{
			if (list[num].StartsWithF("-"))
			{
				list2.Add(list[num]);
			}
			else if (list2.Any() && list2.Last().StartsWithF("-") && !list2.Last().Contains(" "))
			{
				list2[list2.Count - 1] = list2.Last() + " " + list[num];
			}
			else
			{
				list2.Add(list[num]);
			}
		}
		string text20 = list2.Distinct().ToList().Join(" ");
		JsonObject jsonObject10;
		if (mMCPackInfo != null)
		{
			if (mMCPackInfo.isMinecraftOverrided)
			{
				ModBase.Log("[Download] 当前实例的 MC 核心已被修改，使用对应的 MMC 整合包参数");
				jsonObject10 = ((jsonObject != null) ? ((JsonObject)jsonObject.DeepClone()) : new JsonObject());
				foreach (KeyValuePair<string, JsonNode> item2 in mMCPackInfo.overridedJson)
				{
					jsonObject10[item2.Key] = item2.Value?.DeepClone();
				}
			}
			else
			{
				ModBase.Log("[Download] 存在无修改 MC 核心文件的 MMC 整合包信息，应用相关参数");
				jsonObject10 = jsonObject;
				JsonCompat.Merge(jsonObject10, (JsonNode)mMCPackInfo.overridedJson);
			}
		}
		else
		{
			jsonObject10 = jsonObject;
		}
		if (flag)
		{
			jsonObject2.Remove("releaseTime");
			jsonObject2.Remove("time");
			JsonCompat.Merge(jsonObject10, (JsonNode)jsonObject2);
		}
		if (flag2 && (mMCPackInfo == null || !mMCPackInfo.isForgeOverrided))
		{
			jsonObject3.Remove("releaseTime");
			jsonObject3.Remove("time");
			JsonCompat.Merge(jsonObject10, (JsonNode)jsonObject3);
		}
		if (flag4 && (mMCPackInfo == null || !mMCPackInfo.isNeoForgeOverrided))
		{
			jsonObject4.Remove("releaseTime");
			jsonObject4.Remove("time");
			JsonCompat.Merge(jsonObject10, (JsonNode)jsonObject4);
		}
		if (flag5 && (mMCPackInfo == null || !mMCPackInfo.isCleanroomOverrided))
		{
			jsonObject6.Remove("releaseTime");
			jsonObject6.Remove("time");
			JsonCompat.Merge(jsonObject10, (JsonNode)jsonObject6);
		}
		if (flag6)
		{
			jsonObject7.Remove("releaseTime");
			jsonObject7.Remove("time");
			JsonCompat.Merge(jsonObject10, (JsonNode)jsonObject7);
		}
		if (flag7 && (mMCPackInfo == null || !mMCPackInfo.isFabricOverrided))
		{
			jsonObject8.Remove("releaseTime");
			jsonObject8.Remove("time");
			JsonCompat.Merge(jsonObject10, (JsonNode)jsonObject8);
		}
		if (flag3 && (mMCPackInfo == null || !mMCPackInfo.isFabricOverrided))
		{
			jsonObject5.Remove("releaseTime");
			jsonObject5.Remove("time");
			JsonCompat.Merge(jsonObject10, (JsonNode)jsonObject5);
		}
		if (flag8)
		{
			jsonObject9.Remove("releaseTime");
			jsonObject9.Remove("time");
			if (jsonObject10 == null)
			{
				jsonObject10 = new JsonObject();
			}
			JsonCompat.Merge(jsonObject10, (JsonNode)jsonObject9);
			JsonObject jsonObject11 = (JsonObject)Requester.FetchJson("https://releases.r2.labymod.net/api/v1/libraries/" + labyModChannel + ".json", RequestParam.WithRetry);
			JsonObject jsonObject12 = (JsonObject)Requester.FetchJson("https://releases.r2.labymod.net/api/v1/manifest/" + labyModChannel + "/latest.json", RequestParam.WithRetry);
			JsonArray jsonArray = new JsonArray();
			Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
			JsonNode item = jsonObject9["_minecraftVersion"];
			foreach (JsonNode item3 in jsonObject11["isolated_libraries"].AsArray())
			{
				if (((JsonArray)item3["versions"]).Contains(item))
				{
					dictionary.Add(item3["name"].ToString(), value: true);
				}
			}
			foreach (JsonNode item4 in jsonObject9["libraries"].AsArray())
			{
				string text21 = item4["name"].ToString().RegexSeek(RegexPatterns.CatchLwjglInLib);
				if (text21 == null || !dictionary.Contains(new KeyValuePair<string, bool>(text21, value: true)))
				{
					jsonArray.Add(item4.DeepClone());
				}
			}
			foreach (JsonNode item5 in jsonObject11["libraries"].AsArray())
			{
				string text22 = item5?["url"]?.ToString() ?? "";
				jsonArray.Add(new JsonObject
				{
					["name"] = item5?["name"]?.ToString(),
					["downloads"] = new JsonObject { ["artifact"] = new JsonObject
					{
						["path"] = text22.Substring(text22.LastIndexOfF("https://releases.r2.labymod.net/libraries/") + 42),
						["sha1"] = item5?["sha1"]?.ToString(),
						["size"] = item5?["size"]?.DeepClone(),
						["url"] = text22
					} }
				});
			}
			string text23 = jsonObject12["commitReference"]?.ToString() ?? "";
			jsonArray.Add(new JsonObject
			{
				["name"] = "net.labymod:LabyMod:4",
				["downloads"] = new JsonObject { ["artifact"] = new JsonObject
				{
					["path"] = "net/labymod/LabyMod/4/LabyMod-4.jar",
					["sha1"] = jsonObject12["sha1"]?.ToString(),
					["size"] = jsonObject12["size"]?.DeepClone(),
					["url"] = $"https://releases.r2.labymod.net/api/v1/download/labymod4/{labyModChannel}/{text23}.jar"
				} }
			});
			jsonObject10["libraries"] = jsonArray;
			jsonObject10.Add("labymod_data", new JsonObject
			{
				["channelType"] = labyModChannel,
				["commitReference"] = text23,
				["version"] = jsonObject12["labyModVersion"]?.ToString(),
				["versionType"] = "release"
			});
		}
		if (jsonObject10["libraries"] is JsonArray jsonArray2 && jsonArray2.Any(IsLwjgl3Library))
		{
			for (int num3 = jsonArray2.Count - 1; num3 >= 0; num3--)
			{
				if (IsLwjgl2Library(jsonArray2[num3]))
				{
					ModBase.Log("[Download] 已移除与 LWJGL3 冲突的 LWJGL2 支持库：" + jsonArray2[num3]?["name"]);
					jsonArray2.RemoveAt(num3);
				}
			}
		}
		if (text20 != null && !string.IsNullOrEmpty(text20.Replace(" ", "")))
		{
			jsonObject10["minecraftArguments"] = text20;
		}
		if (mMCPackInfo != null && mMCPackInfo.isMcArgsEdited)
		{
			jsonObject10.Remove("minecraftArguments");
		}
		jsonObject10.Remove("_comment_");
		jsonObject10.Remove("inheritsFrom");
		jsonObject10.Remove("jar");
		jsonObject10["id"] = folderNameFromPath;
		ModBase.WriteFile(filePath2, jsonObject10.ToString());
		if ((text10 ?? "") != (text8 ?? ""))
		{
			if (File.Exists(text8))
			{
				File.Delete(text8);
			}
			ModBase.CopyFile(text10, text8);
		}
		ModBase.Log("[Download] 实例合并 " + folderNameFromPath + " 完成");
	}

	private static bool IsLwjgl3Library(JsonNode library)
	{
		if (!(library is JsonObject jsonObject))
		{
			return false;
		}
		string[] array = (jsonObject["name"]?.ToString() ?? "").Split(':');
		if (array.Length >= 3 && array[0] == "org.lwjgl")
		{
			return array[2].StartsWith("3.", StringComparison.Ordinal);
		}
		return false;
	}

	private static bool IsLwjgl2Library(JsonNode library)
	{
		if (!(library is JsonObject jsonObject))
		{
			return false;
		}
		string[] array = (jsonObject["name"]?.ToString() ?? "").Split(':');
		if (array.Length >= 3)
		{
			if (!(array[0] == "org.lwjgl.lwjgl"))
			{
				if (array[0] == "org.lwjgl")
				{
					return array[2].StartsWith("2.", StringComparison.Ordinal);
				}
				return false;
			}
			return true;
		}
		return false;
	}
}
