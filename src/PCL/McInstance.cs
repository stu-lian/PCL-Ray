using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;

namespace PCL;

public class McInstance
{
	public string Desc = Lang.Text("Select.Instance.Description.NotLoaded");

	public McInstanceCardType displayType;

	public bool IsLoaded;

	private bool _jsonVersionInited;

	public bool IsStar;

	public string Logo;

	public DateTime releaseTime = new DateTime(1970, 1, 1, 15, 0, 0);

	public McInstanceState state;

	public string PathInstance { get; }

	public string PathIndie
	{
		get
		{
			if (Config.Instance.IndieV2Config.IsDefault((object)PathInstance))
			{
				if (!IsLoaded)
				{
					Load();
				}
				((ParameterizedProperty<object, bool>)(object)Config.Instance.IndieV2)[(object)PathInstance] = ShouldBeIndie();
			}
			if (!((ParameterizedProperty<object, bool>)(object)Config.Instance.IndieV2)[(object)PathInstance])
			{
				return ModFolder.mcFolderSelected;
			}
			return PathInstance;
			bool ShouldBeIndie()
			{
				if (!Config.Instance.IndieV1Config.IsDefault((object)PathInstance) && ((ParameterizedProperty<object, int>)(object)Config.Instance.IndieV1)[(object)PathInstance] > 0)
				{
					ModBase.Log("[Minecraft] 版本隔离初始化（" + Name + "）：从老的实例独立设置中迁移");
					return ((ParameterizedProperty<object, int>)(object)Config.Instance.IndieV1)[(object)PathInstance] == 1;
				}
				DirectoryInfo directoryInfo = new DirectoryInfo(PathInstance + "mods\\");
				DirectoryInfo directoryInfo2 = new DirectoryInfo(PathInstance + "saves\\");
				if ((directoryInfo.Exists && directoryInfo.EnumerateFiles().Any()) || (directoryInfo2.Exists && directoryInfo2.EnumerateDirectories().Any()))
				{
					ModBase.Log("[Minecraft] 版本隔离初始化（" + Name + "）：实例文件夹下存在 mods 或 saves 文件夹，自动开启");
					return true;
				}
				bool flag = state != McInstanceState.Fool && state != McInstanceState.Old && state != McInstanceState.Snapshot;
				ModBase.Log($"[Minecraft] 版本隔离初始化（{Name}）：从全局默认设置中（{Config.Launch.IndieSolutionV2}）判断，State {ModBase.GetStringFromEnum(state)}，IsRelease {flag}，Modable {Modable}");
				return Config.Launch.IndieSolutionV2 switch
				{
					0 => false, 
					1 => Info.HasLabyMod || Modable, 
					2 => !flag, 
					3 => Info.HasLabyMod || Modable || !flag, 
					_ => true, 
				};
			}
		}
	}

	public string Name
	{
		get
		{
			if (field == null && !string.IsNullOrEmpty(PathInstance))
			{
				field = ModBase.GetFolderNameFromPath(PathInstance);
			}
			return field;
		}
	}

	public bool Modable
	{
		get
		{
			if (!IsLoaded)
			{
				Load();
			}
			if (!Info.HasFabric && !Info.HasLegacyFabric && !Info.HasQuilt && !Info.HasForge && !Info.HasLiteLoader && !Info.HasNeoForge && !Info.HasCleanroom)
			{
				return displayType == McInstanceCardType.API;
			}
			return true;
		}
	}

	public McInstanceInfo Info
	{
		get
		{
			if (field != null)
			{
				return field;
			}
			field = new McInstanceInfo();
			try
			{
				try
				{
					if (JsonObject["releaseTime"] == null)
					{
						releaseTime = new DateTime(1970, 1, 1, 15, 0, 0);
					}
					else
					{
						releaseTime = JsonCompat.ToObject<DateTime>(JsonObject["releaseTime"]);
					}
					if (releaseTime.Year <= 2000 || releaseTime.Year >= 2013)
					{
						goto end_IL_001a;
					}
					field.VanillaName = "Old";
					goto end_IL_001a_2;
					end_IL_001a:;
				}
				catch
				{
					releaseTime = new DateTime(1970, 1, 1, 15, 0, 0);
				}
				string text2;
				if ((string?)(JsonObject["type"] ?? ((JsonNode?)"")) == "pending")
				{
					field.VanillaName = "pending";
				}
				else if (JsonObject["clientVersion"] != null)
				{
					field.VanillaName = (string?)JsonObject["clientVersion"];
				}
				else
				{
					if (JsonObject["patches"] != null)
					{
						foreach (JsonNode item in JsonObject["patches"].AsArray())
						{
							JsonObject jsonObject = item.AsObject();
							if (!((jsonObject["id"] ?? ((JsonNode?)"")).ToString() == "game") || jsonObject["version"] == null)
							{
								continue;
							}
							field.VanillaName = jsonObject["version"].ToString();
							goto end_IL_001a_2;
						}
					}
					if (JsonObject["arguments"] != null)
					{
						if (JsonObject["arguments"]["game"] != null)
						{
							bool flag = false;
							foreach (JsonNode item2 in JsonObject["arguments"]["game"].AsArray())
							{
								if (!flag)
								{
									if (item2.ToString() == "--fml.mcVersion")
									{
										flag = true;
									}
									continue;
								}
								field.VanillaName = item2.ToString();
								goto end_IL_001a_2;
							}
						}
						if (JsonObject["arguments"]["jvm"] != null)
						{
							foreach (JsonNode item3 in JsonObject["arguments"]["jvm"].AsArray())
							{
								string text = item3.ToString().RegexSeek(RegexPatterns.LabyModVersion);
								if (text == null)
								{
									continue;
								}
								field.VanillaName = text;
								goto end_IL_001a_2;
							}
						}
					}
					if (!string.IsNullOrEmpty(InheritInstanceName))
					{
						field.VanillaName = (JsonObject["jar"] ?? ((JsonNode?)"")).ToString();
						if (string.IsNullOrEmpty(field.VanillaName))
						{
							field.VanillaName = InheritInstanceName;
						}
					}
					else
					{
						text2 = (JsonObject["downloads"] ?? ((JsonNode?)"")).ToString().RegexSeek(RegexPatterns.MinecraftDownloadUrlVersion);
						if (text2 != null)
						{
							field.VanillaName = text2;
						}
						else
						{
							string str = JsonObject["libraries"].ToString();
							text2 = str.RegexSeek(RegexPatterns.ForgeLibVersion);
							if (text2 != null)
							{
								field.VanillaName = text2;
							}
							else
							{
								text2 = str.RegexSeek(RegexPatterns.OptiFineLibVersion);
								if (text2 != null)
								{
									field.VanillaName = text2;
								}
								else
								{
									text2 = str.RegexSeek(RegexPatterns.FabricLikeLibVersion);
									if (text2 != null)
									{
										field.VanillaName = text2;
									}
									else if (JsonObject["jar"] != null)
									{
										field.VanillaName = JsonObject["jar"].ToString();
									}
									else
									{
										if (JsonVersion?["name"] == null)
										{
											goto IL_04bb;
										}
										string text3 = JsonVersion["name"].ToString();
										if (text3.Length >= 32)
										{
											goto IL_04bb;
										}
										field.VanillaName = text3;
										ModBase.Log("[Minecraft] 从版本 jar 中的 version.json 获取到版本号：" + text3);
									}
								}
							}
						}
					}
				}
				goto end_IL_001a_2;
				IL_04bb:
				text2 = ((string?)JsonObject["id"]).RegexSeek(RegexPatterns.MinecraftJsonVersion, RegexOptions.IgnoreCase);
				if (text2 != null)
				{
					field.VanillaName = text2;
				}
				else
				{
					ModBase.Log("[Minecraft] 无法完全确认 MC 版本号的版本：" + Name);
					field.Reliable = false;
					text2 = Name.RegexSeek(RegexPatterns.MinecraftJsonVersion, RegexOptions.IgnoreCase);
					if (text2 != null)
					{
						field.VanillaName = text2;
					}
					else
					{
						JsonObject jsonObject2 = (JsonObject)JsonObject.DeepClone();
						jsonObject2.Remove("libraries");
						text2 = jsonObject2.ToString().RegexSeek(RegexPatterns.MinecraftJsonVersion, RegexOptions.IgnoreCase);
						if (text2 != null)
						{
							field.VanillaName = text2;
						}
						else
						{
							field.VanillaName = "Unknown";
							Desc = Lang.Text("Select.Instance.Description.UnknownMcVersion");
						}
					}
				}
				end_IL_001a_2:;
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "识别 Minecraft 版本时出错");
				field.VanillaName = "Unknown";
				Desc = Lang.Text("Minecraft.Error.Unrecognizable", new object[1] { ex.Message });
			}
			if (field.VanillaName.StartsWithF("20.") || field.VanillaName.StartsWithF("21."))
			{
				field.VanillaName = "1." + field.VanillaName;
			}
			field.VanillaName = field.VanillaName.Replace("_unobfuscated", "").Replace(" Unobfuscated", "");
			if (field.VanillaName.StartsWithF("1."))
			{
				string[] array = field.VanillaName.Split(" _-.".ToCharArray());
				field.vanilla = new Version((int)Math.Round(ModBase.Val((array.Count() >= 2) ? array[1] : "0")), 0, (int)Math.Round(ModBase.Val((array.Count() >= 3) ? array[2] : "0")));
			}
			else if (field.VanillaName.RegexCheck("^[2-9][0-9]\\."))
			{
				string[] array2 = field.VanillaName.Split(" _-.".ToCharArray());
				field.vanilla = new Version((int)Math.Round(ModBase.Val(array2[0])), (int)Math.Round(ModBase.Val((array2.Count() >= 2) ? array2[1] : "0")), (int)Math.Round(ModBase.Val((array2.Count() >= 3) ? array2[2] : "0")));
			}
			else
			{
				field.vanilla = new Version(9999, 0, 0);
			}
			return field;
		}
		set
		{
			field = value;
		}
	}

	public string JsonText
	{
		get
		{
			if (field == null)
			{
				string text = PathInstance + Name + ".json";
				if (!File.Exists(text))
				{
					string[] files = Directory.GetFiles(PathInstance, "*.json");
					if (files.Count() != 1)
					{
						throw new Exception(Lang.Text("Minecraft.Error.InstanceJsonNotFound", new object[1] { PathInstance + Name + ".json" }));
					}
					text = files[0];
					ModBase.Log("[Minecraft] 未找到同名实例 JSON，自动换用 " + text, ModBase.LogLevel.Debug);
				}
				field = ModBase.ReadFile(text);
				if (!FastJsonCheck(field))
				{
					if (ModBase.RunInUi())
					{
						ModBase.Log("[Minecraft] 实例 JSON 文件为空或有误，将进行短暂重试（" + text + "）", ModBase.LogLevel.Debug);
						Thread.Sleep(200);
						field = ModBase.ReadFile(text);
					}
					else
					{
						ModBase.Log("[Minecraft] 实例 JSON 文件为空或有误，将在 2s 后重试读取（" + text + "）", ModBase.LogLevel.Debug);
						Thread.Sleep(2000);
						field = ModBase.ReadFile(text);
					}
					if (!FastJsonCheck(field))
					{
						ModBase.GetJson(field);
					}
				}
			}
			return field;
			static bool FastJsonCheck(string json)
			{
				string str = json.Trim();
				if (str.StartsWithF("{"))
				{
					return str.EndsWithF("}");
				}
				return false;
			}
		}
		set
		{
			field = value;
		}
	}

	public JsonObject JsonObject
	{
		get
		{
			if (field == null)
			{
				string jsonText = JsonText;
				try
				{
					field = (JsonObject)ModBase.GetJson(jsonText);
					if (field.ContainsKey("patches") && !field.ContainsKey("time"))
					{
						IsHmclFormatJson = true;
						JsonObject jsonObject = null;
						List<JsonObject> list = new List<JsonObject>();
						foreach (JsonNode item2 in field["patches"].AsArray())
						{
							JsonObject item = item2.AsObject();
							list.Add(item);
						}
						list.Sort((JsonObject left, JsonObject right) =>
						{
							double num = ModBase.Val((left["priority"] ?? ((JsonNode?)"0")).ToString());
							double value = ModBase.Val((right["priority"] ?? ((JsonNode?)"0")).ToString());
							return num.CompareTo(value);
						});
						foreach (JsonObject item3 in list)
						{
							string text = (string?)item3["id"];
							if (text != null)
							{
								ModBase.Log("[Minecraft] 合并 HMCL 分支项：" + text);
								if (jsonObject != null)
								{
									JsonCompat.Merge(jsonObject, (JsonNode)item3);
								}
								else
								{
									jsonObject = item3;
								}
							}
							else
							{
								ModBase.Log("[Minecraft] 存在为空的 HMCL 分支项");
							}
						}
						field = jsonObject;
						field["id"] = Name;
						if (field.ContainsKey("inheritsFrom"))
						{
							field.Remove("inheritsFrom");
						}
					}
					object obj = null;
					try
					{
						obj = ((field["inheritsFrom"] == null) ? "" : field["inheritsFrom"].ToString());
						if (object.Equals(obj, Name))
						{
							ModBase.Log("[Minecraft] 自引用的继承实例：" + Name, ModBase.LogLevel.Debug);
							obj = "";
						}
						else
						{
							while (!object.Equals(obj, ""))
							{
								McInstance mcInstance = new McInstance(obj?.ToString() ?? "");
								if (object.Equals(mcInstance.InheritInstanceName, obj))
								{
									throw new Exception(Lang.Text("Minecraft.Error.DependencyRecursion", new object[1] { obj }));
								}
								obj = mcInstance.InheritInstanceName;
								JsonCompat.Merge(mcInstance.JsonObject, (JsonNode)field);
								field = mcInstance.JsonObject;
							}
						}
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "合并实例依赖项 JSON 失败（" + (obj ?? "null")?.ToString() + "）");
					}
				}
				catch (Exception innerException)
				{
					throw new Exception(Lang.Text("Minecraft.Error.InitInstanceJsonFailed", new object[1] { Name ?? "null" }), innerException);
				}
			}
			return field;
		}
		set
		{
			field = value;
		}
	}

	public bool IsOldJson
	{
		get
		{
			if (JsonObject["minecraftArguments"] != null)
			{
				return (string?)JsonObject["minecraftArguments"] != "";
			}
			return false;
		}
	}

	public bool IsHmclFormatJson { get; set; }

	public JsonObject JsonVersion
	{
		get
		{
			if (!_jsonVersionInited)
			{
				_jsonVersionInited = true;
				try
				{
					if (File.Exists(PathInstance + Name + ".jar"))
					{
						using ZipArchive zipArchive = new ZipArchive(new FileStream(PathInstance + Name + ".jar", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
						ZipArchiveEntry entry = zipArchive.GetEntry("version.json");
						if (entry != null)
						{
							using StreamReader streamReader = new StreamReader(entry.Open());
							field = (JsonObject)ModBase.GetJson(streamReader.ReadToEnd());
						}
					}
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "从实例 JAR 中读取 version.json 失败 (" + PathInstance + Name + ".jar)");
				}
			}
			return field;
		}
	}

	public string InheritInstanceName
	{
		get
		{
			if (field == null)
			{
				field = (JsonObject["inheritsFrom"] ?? ((JsonNode?)"")).ToString();
				if (JsonText.Contains("liteloader") && (Info.VanillaName ?? "") != (Name ?? "") && !JsonText.Contains("logging") && ((JsonObject["jar"] ?? ((JsonNode?)Info.VanillaName)).ToString() ?? "") == (Info.VanillaName ?? ""))
				{
					field = Info.VanillaName;
				}
				if (IsHmclFormatJson)
				{
					field = "";
				}
			}
			return field;
		}
	}

	public McInstance(string name)
	{
		PathInstance = (name.Contains(":") ? name : Path.Combine(ModFolder.mcFolderSelected, "versions", name)) + (name.EndsWithF("\\") ? "" : "\\");
	}

	public bool Check()
	{
		if (!Directory.Exists(PathInstance))
		{
			state = McInstanceState.Error;
			Desc = Lang.Text("Select.Instance.Description.NotFound", new object[1] { Name });
			return false;
		}
		try
		{
			Directory.CreateDirectory(PathInstance + "PCL\\");
			ModBase.CheckPermissionWithException(PathInstance + "PCL\\");
		}
		catch (Exception ex)
		{
			state = McInstanceState.Error;
			Desc = Lang.Text("Select.Instance.Description.NoPermission");
			ModBase.Log(ex, "没有访问实例文件夹的权限");
			return false;
		}
		try
		{
			_ = JsonObject;
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "实例 JSON 可用性检查失败（" + PathInstance + "）");
			JsonText = "";
			JsonObject = null;
			Desc = ex2.Message;
			state = McInstanceState.Error;
			return false;
		}
		try
		{
			if (string.IsNullOrEmpty(Info.VanillaName))
			{
				throw new Exception(Lang.Text("Minecraft.Error.VersionNumberEmpty"));
			}
		}
		catch (Exception ex3)
		{
			ModBase.Log(ex3, "版本号获取失败（" + Name + "）");
			state = McInstanceState.Error;
			Desc = Lang.Text("Minecraft.Error.VersionNumberFetchFailed", new object[1] { ex3 });
			return false;
		}
		try
		{
			if (!string.IsNullOrEmpty(InheritInstanceName) && !File.Exists(Path.Combine(ModBase.GetPathFromFullPath(PathInstance), InheritInstanceName, InheritInstanceName + ".json")))
			{
				state = McInstanceState.Error;
				Desc = Lang.Text("Select.Instance.Description.NeedInherit", new object[1] { InheritInstanceName });
				return false;
			}
		}
		catch (Exception ex4)
		{
			ModBase.Log(ex4, "依赖实例检查出错（" + Name + "）");
			state = McInstanceState.Error;
			Desc = Lang.Text("Select.Instance.Description.UnknownError") + ": " + ex4;
			return false;
		}
		state = McInstanceState.Original;
		return true;
	}

	public McInstance Load()
	{
		try
		{
			if (Check())
			{
				string text = Info.VanillaName ?? "";
				if (!(text == "Unknown"))
				{
					if (text == "Old")
					{
						state = McInstanceState.Old;
					}
					else
					{
						string text2 = ((JsonObject != null) ? JsonObject.ToString() : JsonText);
						if ((JsonObject["type"] ?? ((JsonNode?)"")).ToString() == "fool" || !string.IsNullOrEmpty(McVersionClassifier.GetMcFoolName(Info.VanillaName)))
						{
							state = McInstanceState.Fool;
						}
						else if (IsSnapshot())
						{
							state = McInstanceState.Snapshot;
						}
						if (text2.Contains("optifine"))
						{
							state = McInstanceState.OptiFine;
							Info.HasOptiFine = true;
							Info.OptiFine = text2.RegexSeek(RegexPatterns.OptiFineVersion) ?? Lang.Text("Minecraft.Version.Unknown");
						}
						if (text2.Contains("liteloader"))
						{
							state = McInstanceState.LiteLoader;
							Info.HasLiteLoader = true;
						}
						if (text2.Contains("labymod_data"))
						{
							state = McInstanceState.LabyMod;
							Info.HasLabyMod = true;
							Info.LabyMod = (string?)JsonObject["labymod_data"]["version"];
						}
						else if (text2.Contains("net.legacyfabric:intermediary"))
						{
							state = McInstanceState.LegacyFabric;
							Info.HasLegacyFabric = true;
							Info.LegacyFabric = (text2.RegexSeek(RegexPatterns.LegacyFabricVersion) ?? Lang.Text("Minecraft.Version.Unknown")).Replace("+build", "");
						}
						else if (text2.Contains("net.fabricmc:fabric-loader"))
						{
							state = McInstanceState.Fabric;
							Info.HasFabric = true;
							Info.Fabric = (text2.RegexSeek(RegexPatterns.FabricVersion) ?? Lang.Text("Minecraft.Version.Unknown")).Replace("+build", "");
						}
						else if (text2.Contains("org.quiltmc:quilt-loader"))
						{
							state = McInstanceState.Quilt;
							Info.HasQuilt = true;
							Info.Quilt = (text2.RegexSeek(RegexPatterns.QuiltVersion) ?? Lang.Text("Minecraft.Version.Unknown")).Replace("+build", "");
						}
						else if (text2.Contains("com.cleanroommc:cleanroom:"))
						{
							state = McInstanceState.Cleanroom;
							Info.HasCleanroom = true;
							Info.Cleanroom = (text2.RegexSeek(RegexPatterns.CleanroomVersion) ?? Lang.Text("Minecraft.Version.Unknown")).Replace("+build", "");
						}
						else if (text2.Contains("minecraftforge") && !text2.Contains("net.neoforge"))
						{
							state = McInstanceState.Forge;
							Info.HasForge = true;
							Info.Forge = text2.RegexSeek(RegexPatterns.ForgeMainVersion) ?? text2.RegexSeek(RegexPatterns.ForgeLibVersion) ?? Lang.Text("Minecraft.Version.Unknown");
						}
						else if (text2.Contains("net.neoforge"))
						{
							state = McInstanceState.NeoForge;
							Info.HasNeoForge = true;
							Info.NeoForge = text2.RegexSeek(RegexPatterns.NeoForgeVersion) ?? Lang.Text("Minecraft.Version.Unknown");
						}
					}
				}
				else
				{
					state = McInstanceState.Error;
				}
			}
			Logo = ((ParameterizedProperty<object, string>)(object)States.Instance.LogoPath)[(object)PathInstance];
			if (File.Exists(Path.Combine(PathInstance, "PCL", "Logo.png")))
			{
				Logo = "PCL\\Logo.png";
				if (!((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)PathInstance])
				{
					((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)PathInstance] = true;
				}
			}
			else if (((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)PathInstance])
			{
				string logo = Logo;
				if (logo != null && logo.EndsWith("PCL\\Logo.png", StringComparison.OrdinalIgnoreCase))
				{
					((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)PathInstance] = false;
					Logo = null;
				}
			}
			if (string.IsNullOrEmpty(Logo) || !((ParameterizedProperty<object, bool>)(object)States.Instance.IsLogoCustom)[(object)PathInstance])
			{
				switch (state)
				{
				case McInstanceState.Original:
					Logo = ModBase.pathImage + "Blocks/Grass.png";
					break;
				case McInstanceState.Snapshot:
					Logo = ModBase.pathImage + "Blocks/CommandBlock.png";
					break;
				case McInstanceState.Old:
					Logo = ModBase.pathImage + "Blocks/CobbleStone.png";
					break;
				case McInstanceState.Forge:
					Logo = ModBase.pathImage + "Blocks/Anvil.png";
					break;
				case McInstanceState.NeoForge:
					Logo = ModBase.pathImage + "Blocks/NeoForge.png";
					break;
				case McInstanceState.Cleanroom:
					Logo = ModBase.pathImage + "Blocks/Cleanroom.png";
					break;
				case McInstanceState.Fabric:
					Logo = ModBase.pathImage + "Blocks/Fabric.png";
					break;
				case McInstanceState.LegacyFabric:
					Logo = ModBase.pathImage + "Blocks/Fabric.png";
					break;
				case McInstanceState.Quilt:
					Logo = ModBase.pathImage + "Blocks/Quilt.png";
					break;
				case McInstanceState.OptiFine:
					Logo = ModBase.pathImage + "Blocks/GrassPath.png";
					break;
				case McInstanceState.LiteLoader:
					Logo = ModBase.pathImage + "Blocks/Egg.png";
					break;
				case McInstanceState.Fool:
					Logo = ModBase.pathImage + "Blocks/GoldBlock.png";
					break;
				case McInstanceState.LabyMod:
					Logo = ModBase.pathImage + "Blocks/LabyMod.png";
					break;
				default:
					Logo = ModBase.pathImage + "Blocks/RedstoneBlock.png";
					break;
				}
			}
			if (state == McInstanceState.Error)
			{
				Desc = Desc;
			}
			else
			{
				Desc = ((ParameterizedProperty<object, string>)(object)States.Instance.CustomInfo)[(object)PathInstance];
				if ((Desc ?? "") == (GetDefaultDescription() ?? ""))
				{
					Desc = "";
				}
			}
			IsStar = ((ParameterizedProperty<object, bool>)(object)States.Instance.Starred)[(object)PathInstance];
			displayType = (McInstanceCardType)((ParameterizedProperty<object, int>)(object)States.Instance.CardType)[(object)PathInstance];
			if (Directory.Exists(PathInstance))
			{
				((ParameterizedProperty<object, int>)(object)States.Instance.State)[(object)PathInstance] = (int)state;
				((ParameterizedProperty<object, string>)(object)States.Instance.Info)[(object)PathInstance] = Desc;
				((ParameterizedProperty<object, string>)(object)States.Instance.LogoPath)[(object)PathInstance] = Logo;
			}
			if (state != McInstanceState.Error)
			{
				((ParameterizedProperty<object, string>)(object)States.Instance.ReleaseTime)[(object)PathInstance] = releaseTime.ToString("yyyy'-'MM'-'dd HH':'mm", CultureInfo.InvariantCulture);
				((ParameterizedProperty<object, string>)(object)States.Instance.FabricVersion)[(object)PathInstance] = Info.Fabric;
				((ParameterizedProperty<object, string>)(object)States.Instance.LegacyFabricVersion)[(object)PathInstance] = Info.LegacyFabric;
				((ParameterizedProperty<object, string>)(object)States.Instance.QuiltVersion)[(object)PathInstance] = Info.Quilt;
				((ParameterizedProperty<object, string>)(object)States.Instance.LabyModVersion)[(object)PathInstance] = Info.LabyMod;
				((ParameterizedProperty<object, string>)(object)States.Instance.OptiFineVersion)[(object)PathInstance] = Info.OptiFine;
				((ParameterizedProperty<object, bool>)(object)States.Instance.HasLiteLoader)[(object)PathInstance] = Info.HasLiteLoader;
				((ParameterizedProperty<object, string>)(object)States.Instance.ForgeVersion)[(object)PathInstance] = Info.Forge;
				((ParameterizedProperty<object, string>)(object)States.Instance.NeoForgeVersion)[(object)PathInstance] = Info.NeoForge;
				((ParameterizedProperty<object, string>)(object)States.Instance.CleanroomVersion)[(object)PathInstance] = Info.Cleanroom;
				((ParameterizedProperty<object, string>)(object)States.Instance.VanillaVersionName)[(object)PathInstance] = Info.VanillaName;
				((ParameterizedProperty<object, string>)(object)States.Instance.VanillaVersion)[(object)PathInstance] = Info.vanilla.ToString();
			}
		}
		catch (Exception ex)
		{
			Desc = Lang.Text("Select.Instance.Description.UnknownError") + ": " + ex;
			Logo = ModBase.pathImage + "Blocks/RedstoneBlock.png";
			state = McInstanceState.Error;
			ModBase.Log(ex, Lang.Text("Select.Instance.Error.Load", new object[1] { Name }), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Instance.Error.Load", new object[1] { Name }));
		}
		finally
		{
			IsLoaded = true;
		}
		return this;
	}

	private bool IsSnapshot()
	{
		if (!new string[6] { "w", "snapshot", "rc", "pre", "experimental", "-" }.Any((string s) => Info.VanillaName.ContainsF(s, ignoreCase: true)) && !Name.ContainsF("combat", ignoreCase: true) && !((JsonObject["type"] ?? ((JsonNode?)"")).ToString() == "snapshot"))
		{
			return (JsonObject["type"] ?? ((JsonNode?)"")).ToString() == "pending";
		}
		return true;
	}

	public string GetDefaultDescription()
	{
		string text = "";
		if (Info.HasForge)
		{
			text = text + ", Forge" + ((Info.Forge == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Info.Forge));
		}
		if (Info.HasNeoForge)
		{
			text = text + ", NeoForge" + ((Info.NeoForge == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Info.NeoForge));
		}
		if (Info.HasCleanroom)
		{
			text = text + ", Cleanroom" + ((Info.Cleanroom == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Info.Cleanroom));
		}
		if (Info.HasLabyMod)
		{
			text = text + ", LabyMod" + ((Info.LabyMod == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Info.LabyMod));
		}
		if (Info.HasFabric)
		{
			text = text + ", Fabric" + ((Info.Fabric == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Info.Fabric));
		}
		if (Info.HasQuilt)
		{
			text = text + ", Quilt" + ((Info.Quilt == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Info.Quilt));
		}
		if (Info.HasLegacyFabric)
		{
			text = text + ", Legacy Fabric" + ((Info.LegacyFabric == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Info.LegacyFabric));
		}
		if (Info.HasOptiFine)
		{
			text = text + ", OptiFine" + ((Info.OptiFine == Lang.Text("Minecraft.Version.Unknown")) ? "" : (" " + Info.OptiFine.Replace("-", " ").Replace("_", " ")));
		}
		if (Info.HasLiteLoader)
		{
			text += ", LiteLoader";
		}
		string text2;
		switch (state)
		{
		case McInstanceState.Original:
		case McInstanceState.Snapshot:
		case McInstanceState.OptiFine:
		case McInstanceState.Forge:
		case McInstanceState.NeoForge:
		case McInstanceState.LiteLoader:
		case McInstanceState.Fabric:
			if (Info.VanillaName.ContainsF("pre", ignoreCase: true))
			{
				text2 = Lang.Text("Select.Instance.Description.PreRelease", new object[1] { Info.VanillaName });
			}
			else if (Info.VanillaName.ContainsF("rc", ignoreCase: true))
			{
				text2 = Lang.Text("Select.Instance.Description.ReleaseCandidate", new object[1] { Info.VanillaName });
			}
			else if (Info.VanillaName.Contains("experimental"))
			{
				text2 = Lang.Text("Select.Instance.Description.ExperimentalSnapshot", new object[1] { Info.VanillaName });
			}
			else if (Info.VanillaName == "pending")
			{
				text2 = Lang.Text("Select.Instance.Description.ExperimentalSnapshot.Pending");
			}
			else if (IsSnapshot())
			{
				text2 = (Info.Reliable ? Lang.Text("Select.Instance.Description.Snapshot", new object[1] { Info.VanillaName.Replace("-snapshot", "") }) : Lang.Text("Select.Instance.Description.Snapshot.Unknown"));
			}
			else
			{
				text2 = (Info.Reliable ? Lang.Text("Select.Instance.Description.Release", new object[1] { Info.VanillaName }) : Lang.Text("Select.Instance.Description.Release.Unknown"));
			}
			break;
		case McInstanceState.Old:
			text2 = Lang.Text("Select.Instance.Description.Old");
			break;
		case McInstanceState.Fool:
			text2 = Lang.Text("Select.Instance.Description.AprilFools", new object[1] { Info.VanillaName });
			break;
		case McInstanceState.Error:
			return Desc;
		default:
			return Lang.Text("Select.Instance.Description.ReportUnknownError");
		}
		return (text2 + text).Replace("_", "-");
	}

	public override bool Equals(object obj)
	{
		if (obj is McInstance mcInstance)
		{
			return (PathInstance ?? "") == (mcInstance.PathInstance ?? "");
		}
		return false;
	}

	public static bool operator ==(McInstance? a, McInstance? b)
	{
		if ((object)a == null && (object)b == null)
		{
			return true;
		}
		if ((object)a == null || (object)b == null)
		{
			return false;
		}
		return (a.PathInstance ?? "") == (b.PathInstance ?? "");
	}

	public static bool operator !=(McInstance a, McInstance b)
	{
		return !(a == b);
	}
}
