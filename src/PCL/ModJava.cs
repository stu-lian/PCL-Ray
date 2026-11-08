using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.IO;
using PCL.Core.Minecraft;
using PCL.Core.Minecraft.Java.UserPreference;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public static class ModJava
{
	public static int javaListCacheVersion = 7;

	public static object javaLock = new object();

	private static string lastJavaBaseDir;

	private static readonly HashSet<string> ignoreHash = new string[3] { "12976a6c2b227cbac58969c1455444596c894656", "c80e4bab46e34d02826eab226a4441d0970f2aba", "84d2102ad171863db04e7ee22a259d1f6c5de4a5" }.ToHashSet();

	public static JavaManager Javas => JavaService.JavaManager;

	public static JavaEntry JavaSelect(string cancelException, Version minVersion = null, Version maxVersion = null, McInstance relatedInstance = null, bool enforceVersionRange = false, IReadOnlySet<int> compatibleMajors = null)
	{
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected Obj, but got Unknown
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Expected Obj, but got Unknown
		ModBase.Log($"[Java] 要求选择合适 Java，要求最低版本 {(((object)minVersion != null) ? minVersion.ToString() : "未指定")}，要求选择的最高版本 {(((object)maxVersion != null) ? maxVersion.ToString() : "未指定")}，关联实例 {(((object)relatedInstance != null) ? relatedInstance.Name : "未指定")}");
		if ((object)relatedInstance != null && relatedInstance.PathInstance != null)
		{
			if (!string.IsNullOrWhiteSpace(((ParameterizedProperty<object, string>)(object)Config.Instance.SelectedJava)[(object)relatedInstance.PathInstance]))
			{
				JavaPreference instanceJavaPreference = GetInstanceJavaPreference(relatedInstance);
				if (instanceJavaPreference != null)
				{
					if (instanceJavaPreference is ExistingJava)
					{
						ExistingJava val = (ExistingJava)instanceJavaPreference;
						JavaEntry val2 = Javas.AddOrGet(val.JavaExePath);
						if (val2 != null && val2.IsEnabled)
						{
							if (!IsVersionSuitable(val2.Installation.Version) || !IsAllowedMajor(val2))
							{
								HintService.Hint(_GetJavaRangeWarning("Minecraft.Launch.Java.Compatibility.InstanceSelectedOutOfRange", val2.Installation.Version, minVersion, maxVersion));
							}
							ModBase.Log($"[Java] 返回实例 '{relatedInstance.Name}' 指定的 Java: {val2}");
							return val2;
						}
						ModBase.Log("[Java] 警告：实例指定的 Java 路径无效或不可用: " + val.JavaExePath);
					}
					else if (instanceJavaPreference is UseRelativePath)
					{
						UseRelativePath val3 = (UseRelativePath)instanceJavaPreference;
						string fullPath = Path.GetFullPath(Path.Combine(Basics.ExecutableDirectory, val3.RelativePath));
						if (Files.IsPathWithinDirectory(fullPath, Basics.ExecutableDirectory))
						{
							JavaEntry val4 = Javas.Get(fullPath);
							if (val4 != null && val4.IsEnabled)
							{
								if (!IsVersionSuitable(val4.Installation.Version) || !IsAllowedMajor(val4))
								{
									HintService.Hint(_GetJavaRangeWarning("Minecraft.Launch.Java.Compatibility.RelativePathSelectedOutOfRange", val4.Installation.Version, minVersion, maxVersion), HintType.Error);
								}
								ModBase.Log($"[Java] 返回实例 '{relatedInstance.Name}' 相对路径指定的 Java ({val3.RelativePath}): {val4}");
								return val4;
							}
						}
						else
						{
							ModBase.Log("[Java] 警告：实例相对路径指定的 Java 无效: " + fullPath);
						}
					}
					else if (instanceJavaPreference is UseGlobalPreference)
					{
						ModBase.Log("[Java] 实例 '" + relatedInstance.Name + "' 配置为使用全局 Java 设置，继续检查全局配置");
					}
					else
					{
						ModBase.Log($"[Java] 警告：未知的 Java 偏好类型 '{instanceJavaPreference}'，跳过处理");
					}
				}
				else
				{
					ModBase.Log("[Java] 实例 '" + relatedInstance.Name + "' 未指定 Java 偏好（空值），使用自动选择策略");
				}
			}
			else
			{
				ModBase.Log("[Java] 实例 '" + relatedInstance.Name + "' 无 Java 偏好配置，使用自动选择策略");
			}
		}
		string selectedJava = Config.Launch.SelectedJava;
		if (!string.IsNullOrWhiteSpace(selectedJava))
		{
			selectedJava = selectedJava.Trim();
			JavaEntry val5 = Javas.AddOrGet(selectedJava);
			if (val5 != null && val5.IsEnabled)
			{
				bool flag = IsVersionSuitable(val5.Installation.Version) && IsAllowedMajor(val5);
				if (!enforceVersionRange || flag)
				{
					if (!flag)
					{
						HintService.Hint(_GetJavaRangeWarning("Minecraft.Launch.Java.Compatibility.GlobalSelectedOutOfRange", val5.Installation.Version, minVersion, maxVersion));
					}
					ModBase.Log($"[Java] 返回全局指定的 Java: {val5}");
					return val5;
				}
				ModBase.Log($"[Java] 全局指定的 Java 版本不满足强制范围要求，忽略并继续自动搜索: {val5}");
			}
			else
			{
				ModBase.Log("[Java] 警告：全局指定的 Java 路径无效或不可用: " + selectedJava);
			}
		}
		else
		{
			ModBase.Log("[Java] 无全局 Java 配置，使用自动选择策略");
		}
		ModBase.Log("[Java] 开始自动搜索符合版本要求的 Java 运行时");
		Javas.CheckAllAvailability();
		Version version = minVersion ?? new Version(1, 0, 0);
		Version version2 = maxVersion ?? new Version(999, 999, 999);
		JavaEntry[] array = FilterByCompatibleMajors(Javas.SelectSuitableJavaAsync(version, version2).GetAwaiter().GetResult());
		JavaEntry val6 = array.FirstOrDefault();
		if (val6 == null && array.Length == 0)
		{
			ModBase.Log("[Java] 未找到符合版本要求的 Java，触发全盘重新扫描");
			Javas.ScanJavaAsync(false).GetAwaiter().GetResult();
			array = FilterByCompatibleMajors(Javas.SelectSuitableJavaAsync(version, version2).GetAwaiter().GetResult());
			val6 = array.FirstOrDefault();
		}
		if (val6 != null)
		{
			ModBase.Log($"[Java] 返回自动选择的 Java: {val6}");
		}
		else
		{
			ModBase.Log("[Java] 最终未能确定可用的 Java 运行时");
		}
		return val6;
		JavaEntry[] FilterByCompatibleMajors(JavaEntry[] entries)
		{
			if (compatibleMajors != null)
			{
				return entries.Where((JavaEntry entry) => compatibleMajors.Contains(JavaManager.NormalizeVersion(entry.Installation.Version).Major)).ToArray();
			}
			return entries;
		}
		bool IsAllowedMajor(JavaEntry entry)
		{
			if (compatibleMajors != null)
			{
				return compatibleMajors.Contains(JavaManager.NormalizeVersion(entry.Installation.Version).Major);
			}
			return true;
		}
		bool IsVersionSuitable(Version ver)
		{
			if ((object)minVersion == null || ver >= minVersion)
			{
				if ((object)maxVersion != null)
				{
					return ver <= maxVersion;
				}
				return true;
			}
			return false;
		}
	}

	private static string _GetJavaRangeWarning(string key, Version selectedVersion, Version? minVersion, Version? maxVersion)
	{
		return Lang.Text(key, new object[3]
		{
			selectedVersion,
			minVersion?.ToString() ?? Lang.Text("Minecraft.Launch.Java.Compatibility.NoMinimum"),
			maxVersion?.ToString() ?? Lang.Text("Minecraft.Launch.Java.Compatibility.NoMaximum")
		});
	}

	public static JavaPreference GetInstanceJavaPreference(McInstance instance)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected Obj, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected Obj, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected Obj, but got Unknown
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected Obj, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected Obj, but got Unknown
		string text = ((ParameterizedProperty<object, string>)(object)Config.Instance.SelectedJava)[(object)instance.PathInstance];
		JavaPreference val = null;
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				val = JsonSerializer.Deserialize<JavaPreference>(text, JsonCompat.SerializerOptions);
			}
			catch (JsonException)
			{
			}
		}
		if (val == null)
		{
			string text2 = text?.Trim();
			if (string.IsNullOrEmpty(text2))
			{
				val = (JavaPreference)new AutoSelect();
			}
			else
			{
				val = ((!(text2 == "使用全局设置")) ? ((JavaPreference)new ExistingJava(text2)) : ((JavaPreference)new UseGlobalPreference()));
			}
		}
		if (val is ExistingJava)
		{
			if (!Path.IsPathRooted(((ExistingJava)val).JavaExePath))
			{
				val = (JavaPreference)new UseGlobalPreference();
			}
		}
		else if (val is UseRelativePath && !Files.IsPathWithinDirectory(((UseRelativePath)val).RelativePath, Basics.ExecutableDirectory))
		{
			val = (JavaPreference)new UseGlobalPreference();
		}
		return val;
	}

	public static bool IsGameSet64BitJava(McInstance relatedVersion = null)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected Obj, but got Unknown
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = Config.Launch.SelectedJava;
			if (text.StartsWith("{"))
			{
				JsonNode json = ModBase.GetJson(text);
				text = $"{json["Path"]}java.exe";
				Config.Launch.SelectedJava = text;
			}
			if ((object)relatedVersion != null)
			{
				JavaPreference instanceJavaPreference = GetInstanceJavaPreference(relatedVersion);
				if (instanceJavaPreference is AutoSelect)
				{
					return Javas.Existing64BitJava();
				}
				if (instanceJavaPreference is ExistingJava)
				{
					ExistingJava val = (ExistingJava)instanceJavaPreference;
					JavaEntry val2 = Javas.AddOrGet(val.JavaExePath);
					return val2 != null && val2.Installation.Is64Bit;
				}
				if (instanceJavaPreference is UseRelativePath)
				{
					string fullPath = Path.GetFullPath(((UseRelativePath)instanceJavaPreference).RelativePath);
					if (Files.IsPathWithinDirectory(fullPath, Basics.ExecutableDirectory))
					{
						JavaEntry val3 = Javas.Get(fullPath);
						return val3 != null && val3.Installation.Is64Bit;
					}
				}
			}
			if (!string.IsNullOrEmpty(text) && !File.Exists(text))
			{
				Config.Launch.SelectedJava = "";
				text = string.Empty;
			}
			if (string.IsNullOrEmpty(text))
			{
				return Javas.Existing64BitJava();
			}
			JavaEntry val4 = Javas.AddOrGet(text);
			return val4 != null && val4.Installation.Is64Bit;
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "检查 Java 类别时出错", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Launch.Java.Compatibility.CheckFailed"));
			if ((object)relatedVersion != null)
			{
				((ParameterizedProperty<object, string>)(object)Config.Instance.SelectedJava)[(object)relatedVersion.PathInstance] = "使用全局设置";
			}
			Config.Launch.SelectedJava = "";
		}
		return true;
	}

	public static bool JavaDownloadConfirm(string versionDescription, bool forcedManualDownload = false)
	{
		if (!forcedManualDownload)
		{
			return ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Java.AutoDownload.Message", new object[1] { versionDescription }), Lang.Text("Minecraft.Launch.Java.AutoDownload.Title"), Lang.Text("Minecraft.Launch.Java.AutoDownload.Action"), Lang.Text("Common.Action.Cancel")) == 1;
		}
		ModMain.MyMsgBox(Lang.Text("Minecraft.Launch.Java.NotFound.Manual.Message", new object[1] { versionDescription }), Lang.Text("Minecraft.Launch.Java.NotFound.Title"));
		return false;
	}

	public static ModLoader.LoaderCombo<string> GetJavaDownloadLoader()
	{
		LoaderDownload loaderDownload = new LoaderDownload(Lang.Text("Minecraft.Launch.Java.Task.DownloadFiles"), new List<DownloadFile>())
		{
			ProgressWeight = 10.0
		};
		ModLoader.LoaderCombo<string> result = new ModLoader.LoaderCombo<string>(Lang.Text("Minecraft.Launch.Java.Task.Download"), new _003C_003Ez__ReadOnlyArray<ModLoader.LoaderBase>(new ModLoader.LoaderBase[2]
		{
			new ModLoader.LoaderTask<string, List<DownloadFile>>(Lang.Text("Minecraft.Launch.Java.Task.GetDownloadInfo"), JavaFileList)
			{
				ProgressWeight = 2.0
			},
			loaderDownload
		}));
		loaderDownload.OnStateChangedThread += (ModLoader.LoaderBase _, ModBase.LoadState newState, ModBase.LoadState _) =>
		{
			switch (newState)
			{
			case ModBase.LoadState.Failed:
			case ModBase.LoadState.Aborted:
				if (lastJavaBaseDir != null)
				{
					ModBase.Log("[Java] 由于下载未完成，清理未下载完成的 Java 文件：" + lastJavaBaseDir, ModBase.LogLevel.Debug);
					ModBase.DeleteDirectory(lastJavaBaseDir);
				}
				break;
			case ModBase.LoadState.Finished:
				Javas.ScanJavaAsync(false).GetAwaiter().GetResult();
				lastJavaBaseDir = null;
				break;
			}
		};
		loaderDownload.hasOnStateChangedThread = true;
		return result;
	}

	private static void JavaFileList(ModLoader.LoaderTask<string, List<DownloadFile>> loader)
	{
		ModBase.Log("[Java] 开始获取 Java 下载信息");
		string data = ModNet.NetGetCodeByLoader(ModDownload.DlVersionListOrder(new string[1] { "https://piston-meta.mojang.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json" }, new string[1] { "https://bmclapi2.bangbang93.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json" }), 45000, isJson: true);
		string text = null;
		JsonNode jsonNode = null;
		JsonObject jsonObject = (JsonObject)((JsonObject)ModBase.GetJson(data))["windows-x" + (SystemInfo.Is32BitSystem ? "86" : "64")];
		if (jsonObject.ContainsKey(loader.input))
		{
			text = loader.input;
			jsonNode = jsonObject[loader.input];
		}
		else
		{
			KeyValuePair<string, JsonNode> keyValuePair = jsonObject.FirstOrDefault((KeyValuePair<string, JsonNode> c) =>
			{
				JsonNode value = c.Value;
				return ((value == null) ? ((bool?)null) : value.AsArray().FirstOrDefault()?["version"]?["name"]?.ToString().StartsWithF(loader.input)) == true;
			});
			text = keyValuePair.Key;
			jsonNode = keyValuePair.Value;
			if (text == null)
			{
				throw new Exception("未能找到所需的 Java " + loader.input);
			}
		}
		JsonNode jsonNode2 = ((jsonNode != null) ? jsonNode.AsArray().FirstOrDefault() : null);
		if (jsonNode2 == null)
		{
			throw new Exception("Mojang 未提供所需的 Java " + loader.input);
		}
		string text2 = (string?)jsonNode2["manifest"]["url"];
		ModLaunch.McLaunchLog($"准备下载 Java {jsonNode2["version"]["name"]}（{text}）：{text2}");
		JsonObject jsonObject2 = (JsonObject)Requester.FetchJson(ModDownload.DlSourceOrder(new string[1] { text2 }, new string[1] { text2.Replace("piston-meta.mojang.com", "bmclapi2.bangbang93.com") }).First(), RequestParam.WithRetry);
		lastJavaBaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "runtime", text);
		List<DownloadFile> list = new List<DownloadFile>(jsonObject2["files"].AsObject().Count);
		foreach (KeyValuePair<string, JsonNode> item in jsonObject2["files"].AsObject())
		{
			if (item.Value?.AsObject()?["downloads"]?["raw"] == null)
			{
				continue;
			}
			JsonObject jsonObject3 = item.Value["downloads"]["raw"].AsObject();
			JsonNode jsonNode3 = jsonObject3["sha1"];
			if (!ignoreHash.Contains((string?)jsonNode3))
			{
				ModBase.FileChecker fileChecker = new ModBase.FileChecker(-1L, (long)jsonObject3["size"], (string?)jsonObject3["sha1"]);
				string fullPath = Path.GetFullPath(Path.Combine(lastJavaBaseDir, item.Key));
				if (!Files.IsPathWithinDirectory(fullPath, lastJavaBaseDir))
				{
					throw new Exception(fullPath + " 不在 " + lastJavaBaseDir + " 中");
				}
				if (fileChecker.Check(fullPath) != null)
				{
					string text3 = (string?)jsonObject3["url"];
					list.Add(new DownloadFile(ModDownload.DlSourceOrder(new string[1] { text3 }, new string[1] { text3.Replace("piston-data.mojang.com", "bmclapi2.bangbang93.com") }), fullPath, fileChecker));
				}
			}
		}
		loader.output = list;
		ModBase.Log($"[Java] 需要下载 {list.Count} 个文件，目标文件夹：{lastJavaBaseDir}");
	}
}
