using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;

namespace PCL;

public static class ModInstanceList
{
	public const int mcInstanceCacheVersion = 30;

	private static object _McInstanceSelected_mcInstanceSelectedLast = 0;

	public static Dictionary<McInstanceCardType, List<McInstance>> mcInstanceList = new Dictionary<McInstanceCardType, List<McInstance>>();

	public static bool mcInstanceListForceRefresh;

	private static bool _isFirstMcInstanceListLoad = true;

	public static ModLoader.LoaderTask<string, int> mcInstanceListLoader = new ModLoader.LoaderTask<string, int>("Minecraft Instance List", InitMcInstanceList)
	{
		reloadTimeout = 1
	};

	public static McInstance McMcInstanceSelected
	{
		get
		{
			return field;
		}
		set
		{
			if (_McInstanceSelected_mcInstanceSelectedLast != value)
			{
				field = value;
				_McInstanceSelected_mcInstanceSelectedLast = value;
				if ((object)value != null)
				{
					PageDownloadCompDetail.cachedFolder.Clear();
				}
			}
		}
	}

	private static void InitMcInstanceList(ModLoader.LoaderTask<string, int> loader)
	{
		string input = loader.input;
		try
		{
			mcInstanceList = new Dictionary<McInstanceCardType, List<McInstance>>();
			string text = Path.Combine(input, "versions");
			List<string> list = new List<string>();
			if (Directory.Exists(text))
			{
				try
				{
					DirectoryInfo[] directories = new DirectoryInfo(text).GetDirectories();
					foreach (DirectoryInfo directoryInfo in directories)
					{
						list.Add(directoryInfo.Name);
					}
				}
				catch (Exception innerException)
				{
					throw new Exception(Lang.Text("Minecraft.Error.CannotReadInstanceFolder", new object[1] { text }), innerException);
				}
			}
			if (!list.Any())
			{
				ModBase.WriteIni(Path.Combine(input, "PCL.ini"), "InstanceCache", "");
				McMcInstanceSelected = null;
				States.Game.SelectedInstance = "";
				ModBase.Log("[Minecraft] 未找到可用 Minecraft 实例");
				return;
			}
			int num = (int)(ModBase.GetHash(30 + "#" + string.Join("#", list)) % 2147483646);
			bool flag = !mcInstanceListForceRefresh && ModBase.Val(ModBase.ReadIni(Path.Combine(input, "PCL.ini"), "InstanceCache")) == (double)num;
			if (flag)
			{
				Dictionary<McInstanceCardType, List<McInstance>> dictionary = InitMcInstanceListWithCache(input);
				if (dictionary != null)
				{
					mcInstanceList = dictionary;
				}
				else
				{
					flag = false;
				}
			}
			if (!flag)
			{
				mcInstanceListForceRefresh = false;
				ModBase.Log("[Minecraft] 文件夹列表变更或缓存无效，重载所有实例");
				ModBase.WriteIni(Path.Combine(input, "PCL.ini"), "InstanceCache", num.ToString());
				mcInstanceList = InitMcInstanceListWithoutCache(input);
			}
			_isFirstMcInstanceListLoad = false;
			if (loader.IsAborted)
			{
				return;
			}
			string text2 = ModBase.ReadIni(Path.Combine(input, "PCL.ini"), "Version");
			if (!string.IsNullOrEmpty(text2))
			{
				foreach (KeyValuePair<McInstanceCardType, List<McInstance>> mcInstance2 in mcInstanceList)
				{
					foreach (McInstance item in mcInstance2.Value)
					{
						if ((item.Name ?? "") == text2 && item.state != McInstanceState.Error)
						{
							McMcInstanceSelected = item;
							States.Game.SelectedInstance = McMcInstanceSelected.Name;
							ModBase.Log("[Minecraft] 选择该文件夹储存的 Minecraft 实例：" + McMcInstanceSelected.PathInstance);
							return;
						}
					}
				}
			}
			McInstance mcInstance = mcInstanceList.SelectMany((KeyValuePair<McInstanceCardType, List<McInstance>> kv) => kv.Value).FirstOrDefault((McInstance mcInstance2) => mcInstance2.state != McInstanceState.Error);
			if ((object)mcInstance != null)
			{
				McMcInstanceSelected = mcInstance;
				States.Game.SelectedInstance = McMcInstanceSelected.Name;
				ModBase.Log("[Launch] 自动选择 Minecraft 实例：" + McMcInstanceSelected.PathInstance);
			}
			else
			{
				McMcInstanceSelected = null;
				States.Game.SelectedInstance = "";
				ModBase.Log("[Minecraft] 未找到可用 Minecraft 实例");
			}
			bool addRandomDelay = Config.Debug.AddRandomDelay;
			if ((1u & (addRandomDelay ? 1u : 0u)) != 0)
			{
				Thread.Sleep(RandomUtils.NextInt(200, 3000));
			}
		}
		catch (ThreadInterruptedException)
		{
		}
		catch (Exception ex2)
		{
			ModBase.WriteIni(Path.Combine(input, "PCL.ini"), "InstanceCache", "");
			ModBase.Log(ex2, Lang.Text("Select.Instance.Error.ListLoad"), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Instance.Error.ListLoad"));
		}
	}

	private static Dictionary<McInstanceCardType, List<McInstance>> InitMcInstanceListWithCache(string path)
	{
		Dictionary<McInstanceCardType, List<McInstance>> dictionary = new Dictionary<McInstanceCardType, List<McInstance>>();
		try
		{
			int num = int.Parse(ModBase.ReadIni(path + "PCL.ini", "CardCount", (-1).ToString()));
			if (num == -1)
			{
				return null;
			}
			int i = 0;
			for (int num2 = num - 1; i <= num2; i++)
			{
				McInstanceCardType key = (McInstanceCardType)int.Parse(ModBase.ReadIni(path + "PCL.ini", "CardKey" + (i + 1), "0"));
				List<McInstance> list = new List<McInstance>();
				string[] array = ModBase.ReadIni(path + "PCL.ini", "CardValue" + (i + 1), ":").Split(":");
				foreach (string text in array)
				{
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					string text2 = path + "versions\\" + text + "\\";
					if (File.Exists(text2 + ".pclignore"))
					{
						if (!_isFirstMcInstanceListLoad)
						{
							ModBase.Log("[Minecraft] 跳过要求忽略的项目：" + text2);
							continue;
						}
						ModBase.Log("[Minecraft] 清理残留的忽略项目：" + text2);
						File.Delete(text2 + ".pclignore");
					}
					try
					{
						McInstance mcInstance = new McInstance(text2);
						list.Add(mcInstance);
						States.InstanceStatesGroup instance = States.Instance;
						mcInstance.Desc = ((ParameterizedProperty<object, string>)(object)instance.CustomInfo)[(object)mcInstance.PathInstance];
						if (string.IsNullOrEmpty(mcInstance.Desc))
						{
							mcInstance.Desc = ((ParameterizedProperty<object, string>)(object)instance.Info)[(object)mcInstance.PathInstance];
						}
						if (!instance.LogoPathConfig.IsDefault((object)mcInstance.PathInstance))
						{
							mcInstance.Logo = ((ParameterizedProperty<object, string>)(object)instance.LogoPath)[(object)mcInstance.PathInstance];
						}
						if (!instance.ReleaseTimeConfig.IsDefault((object)mcInstance.PathInstance))
						{
							mcInstance.releaseTime = DateTime.Parse(((ParameterizedProperty<object, string>)(object)instance.ReleaseTime)[(object)mcInstance.PathInstance]);
						}
						if (!instance.StateConfig.IsDefault((object)mcInstance.PathInstance))
						{
							mcInstance.state = (McInstanceState)((ParameterizedProperty<object, int>)(object)instance.State)[(object)mcInstance.PathInstance];
						}
						mcInstance.IsStar = ((ParameterizedProperty<object, bool>)(object)instance.Starred)[(object)mcInstance.PathInstance];
						mcInstance.displayType = (McInstanceCardType)((ParameterizedProperty<object, int>)(object)instance.CardType)[(object)mcInstance.PathInstance];
						if (mcInstance.state != McInstanceState.Error && !instance.VanillaVersionNameConfig.IsDefault((object)mcInstance.PathInstance) && !instance.VanillaVersionConfig.IsDefault((object)mcInstance.PathInstance))
						{
							McInstanceInfo mcInstanceInfo = new McInstanceInfo
							{
								Fabric = ((ParameterizedProperty<object, string>)(object)instance.FabricVersion)[(object)mcInstance.PathInstance],
								LegacyFabric = ((ParameterizedProperty<object, string>)(object)instance.LegacyFabricVersion)[(object)mcInstance.PathInstance],
								Quilt = ((ParameterizedProperty<object, string>)(object)instance.QuiltVersion)[(object)mcInstance.PathInstance],
								Forge = ((ParameterizedProperty<object, string>)(object)instance.ForgeVersion)[(object)mcInstance.PathInstance],
								LabyMod = ((ParameterizedProperty<object, string>)(object)instance.LabyModVersion)[(object)mcInstance.PathInstance],
								NeoForge = ((ParameterizedProperty<object, string>)(object)instance.NeoForgeVersion)[(object)mcInstance.PathInstance],
								Cleanroom = ((ParameterizedProperty<object, string>)(object)instance.CleanroomVersion)[(object)mcInstance.PathInstance],
								OptiFine = ((ParameterizedProperty<object, string>)(object)instance.OptiFineVersion)[(object)mcInstance.PathInstance],
								HasLiteLoader = ((ParameterizedProperty<object, bool>)(object)instance.HasLiteLoader)[(object)mcInstance.PathInstance],
								VanillaName = ((ParameterizedProperty<object, string>)(object)instance.VanillaVersionName)[(object)mcInstance.PathInstance],
								vanilla = new Version(((ParameterizedProperty<object, string>)(object)instance.VanillaVersion)[(object)mcInstance.PathInstance])
							};
							mcInstanceInfo.HasFabric = mcInstanceInfo.Fabric.Any();
							mcInstanceInfo.HasLegacyFabric = mcInstanceInfo.LegacyFabric.Any();
							mcInstanceInfo.HasQuilt = mcInstanceInfo.Quilt.Any();
							mcInstanceInfo.HasForge = mcInstanceInfo.Forge.Any();
							mcInstanceInfo.HasNeoForge = mcInstanceInfo.NeoForge.Any();
							mcInstanceInfo.HasCleanroom = mcInstanceInfo.Cleanroom.Any();
							mcInstanceInfo.HasOptiFine = mcInstanceInfo.OptiFine.Any();
							mcInstance.Info = mcInstanceInfo;
						}
						if (mcInstance.state == McInstanceState.Error)
						{
							string desc = mcInstance.Desc;
							mcInstance.state = McInstanceState.Original;
							mcInstance.Check();
							string value = ((ParameterizedProperty<object, string>)(object)States.Instance.CustomInfo)[(object)mcInstance.PathInstance];
							if (mcInstance.state == McInstanceState.Original || (string.IsNullOrEmpty(value) && !((desc ?? "") == (mcInstance.Desc ?? ""))))
							{
								ModBase.Log("[Minecraft] 实例 " + mcInstance.Name + " 的错误状态已变更，新的状态为：" + mcInstance.Desc);
								return null;
							}
						}
						if (string.IsNullOrEmpty(mcInstance.Logo))
						{
							ModBase.Log("[Minecraft] 实例 " + mcInstance.Name + " 未被加载");
							return null;
						}
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "读取实例加载缓存失败（" + text + "）");
						return null;
					}
				}
				if (list.Any())
				{
					dictionary.Add(key, list);
				}
			}
			return dictionary;
		}
		catch (Exception ex2)
		{
			ModBase.Log(ex2, "读取实例缓存失败");
			return null;
		}
	}

	private static Dictionary<McInstanceCardType, List<McInstance>> InitMcInstanceListWithoutCache(string path)
	{
		List<McInstance> instanceList = new List<McInstance>();
		DirectoryInfo[] directories = new DirectoryInfo(path + "versions").GetDirectories();
		foreach (DirectoryInfo directoryInfo in directories)
		{
			if (!directoryInfo.Exists || !directoryInfo.EnumerateFiles().Any())
			{
				ModBase.Log("[Minecraft] 跳过空文件夹：" + directoryInfo.FullName);
				continue;
			}
			if ((directoryInfo.Name == "cache" || directoryInfo.Name == "BLClient" || directoryInfo.Name == "PCL") && !File.Exists(Path.Combine(directoryInfo.FullName, directoryInfo.Name + ".json")))
			{
				ModBase.Log("[Minecraft] 跳过可能不是实例文件夹的项目：" + directoryInfo.FullName);
				continue;
			}
			string text = directoryInfo.FullName + "\\";
			if (File.Exists(text + ".pclignore"))
			{
				if (!_isFirstMcInstanceListLoad)
				{
					ModBase.Log("[Minecraft] 跳过要求忽略的项目：" + text);
					continue;
				}
				ModBase.Log("[Minecraft] 清理残留的忽略项目：" + text);
				try
				{
					File.Delete(text + ".pclignore");
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, Lang.Text("Select.Folder.Error.Cleanup", new object[1] { text }), ModBase.LogLevel.Hint, null, Lang.Text("Select.Folder.Error.Cleanup", new object[1] { text }));
				}
			}
			McInstance mcInstance = new McInstance(text);
			instanceList.Add(mcInstance);
			mcInstance.Load();
		}
		Dictionary<McInstanceCardType, List<McInstance>> dictionary = new Dictionary<McInstanceCardType, List<McInstance>>();
		try
		{
			Dictionary<McInstanceCardType, List<McInstance>> target = new Dictionary<McInstanceCardType, List<McInstance>>();
			List<McInstance> list = new List<McInstance>();
			foreach (McInstance item in instanceList.ToList())
			{
				if (item.IsStar && item.displayType != McInstanceCardType.Hidden)
				{
					list.Add(item);
					instanceList.Remove(item);
				}
			}
			if (list.Any())
			{
				target.Add(McInstanceCardType.Star, list);
			}
			McInstanceFilter(ref instanceList, ref target, new McInstanceState[1], McInstanceCardType.Error);
			McInstanceFilter(ref instanceList, ref target, new McInstanceState[1] { McInstanceState.Fool }, McInstanceCardType.Fool);
			McInstanceFilter(ref instanceList, ref target, new McInstanceState[8]
			{
				McInstanceState.Forge,
				McInstanceState.NeoForge,
				McInstanceState.LiteLoader,
				McInstanceState.Fabric,
				McInstanceState.LegacyFabric,
				McInstanceState.Quilt,
				McInstanceState.Cleanroom,
				McInstanceState.LabyMod
			}, McInstanceCardType.API);
			List<McInstance> list2 = new List<McInstance>();
			List<McInstance> keepList = new List<McInstance>();
			McInstanceFilter(ref instanceList, new McInstanceState[1] { McInstanceState.Old }, ref keepList);
			McInstance mcInstance2 = ListUtils.MaxOrDefault<McInstance, DateTime>(instanceList.Where((McInstance v) => v.state == McInstanceState.Original || v.state == McInstanceState.Snapshot), (Func<McInstance, DateTime>)((McInstance v) => v.releaseTime));
			if ((object)mcInstance2 != null && mcInstance2.state == McInstanceState.Snapshot)
			{
				list2.Add(mcInstance2);
				instanceList.Remove(mcInstance2);
			}
			McInstanceFilter(ref instanceList, new McInstanceState[1] { McInstanceState.Snapshot }, ref keepList);
			Dictionary<string, McInstance> dictionary2 = new Dictionary<string, McInstance>();
			List<int> list3 = new List<int>();
			foreach (McInstance item2 in instanceList)
			{
				if (!item2.Info.Valid)
				{
					continue;
				}
				if (!list3.Contains(item2.Info.Drop))
				{
					list3.Add(item2.Info.Drop);
				}
				string text2 = item2.Info.Drop.ToString();
				int i = (int)item2.state;
				string key = text2 + "-" + i;
				if (!dictionary2.ContainsKey(key))
				{
					dictionary2.Add(key, item2);
				}
				else if (item2.Info.HasOptiFine)
				{
					if (item2.Info.OptiFineCode > dictionary2[key].Info.OptiFineCode)
					{
						dictionary2[key] = item2;
					}
				}
				else if (item2.releaseTime > dictionary2[key].releaseTime)
				{
					dictionary2[key] = item2;
				}
			}
			foreach (int item3 in list3)
			{
				if (dictionary2.ContainsKey(item3 + "-" + 4) && dictionary2.ContainsKey(item3 + "-" + 1))
				{
					McInstance mcInstance3 = dictionary2[item3 + "-" + 1];
					McInstance mcInstance4 = dictionary2[item3 + "-" + 4];
					if (mcInstance3.Info.Drop > mcInstance4.Info.Drop)
					{
						list2.Add(mcInstance3);
						instanceList.Remove(mcInstance3);
					}
					list2.Add(mcInstance4);
					instanceList.Remove(mcInstance4);
				}
				else if (dictionary2.ContainsKey(item3 + "-" + 4))
				{
					list2.Add(dictionary2[item3 + "-" + 4]);
					instanceList.Remove(dictionary2[item3 + "-" + 4]);
				}
				else if (dictionary2.ContainsKey(item3 + "-" + 1))
				{
					list2.Add(dictionary2[item3 + "-" + 1]);
					instanceList.Remove(dictionary2[item3 + "-" + 1]);
				}
			}
			keepList.AddRange(instanceList);
			if (list2.Any())
			{
				target.Add(McInstanceCardType.OriginalLike, list2);
			}
			if (keepList.Any())
			{
				target.Add(McInstanceCardType.Rubbish, keepList);
			}
			foreach (KeyValuePair<McInstanceCardType, List<McInstance>> item4 in target)
			{
				foreach (McInstance item5 in item4.Value)
				{
					McInstanceCardType key2 = ((item5.displayType == McInstanceCardType.Auto || item4.Key == McInstanceCardType.Star) ? item4.Key : item5.displayType);
					if (!dictionary.ContainsKey(key2))
					{
						dictionary.Add(key2, new List<McInstance>());
					}
					dictionary[key2].Add(item5);
				}
			}
		}
		catch (Exception ex2)
		{
			dictionary.Clear();
			ModBase.Log(ex2, Lang.Text("Select.Instance.Error.Classify"), ModBase.LogLevel.Feedback, null, Lang.Text("Select.Instance.Error.Classify"));
		}
		Dictionary<McInstanceCardType, List<McInstance>> dictionary3 = new Dictionary<McInstanceCardType, List<McInstance>>();
		McInstanceCardType[] array = new McInstanceCardType[7]
		{
			McInstanceCardType.Star,
			McInstanceCardType.API,
			McInstanceCardType.OriginalLike,
			McInstanceCardType.Rubbish,
			McInstanceCardType.Fool,
			McInstanceCardType.Error,
			McInstanceCardType.Hidden
		};
		foreach (McInstanceCardType key3 in array)
		{
			if (dictionary.ContainsKey(key3))
			{
				dictionary3.Add(key3, dictionary[key3]);
			}
		}
		dictionary = dictionary3;
		array = new McInstanceCardType[5]
		{
			McInstanceCardType.Star,
			McInstanceCardType.API,
			McInstanceCardType.OriginalLike,
			McInstanceCardType.Rubbish,
			McInstanceCardType.Fool
		};
		foreach (McInstanceCardType key4 in array)
		{
			if (!dictionary.ContainsKey(key4))
			{
				continue;
			}
			dictionary[key4] = SortUtils.Sort<McInstance>((IList<McInstance>)dictionary[key4], (Func<McInstance, McInstance, bool>)((McInstance left, McInstance right) =>
			{
				if ((left.releaseTime.Year >= 2000 || right.releaseTime.Year >= 2000) && left.releaseTime != right.releaseTime)
				{
					return left.releaseTime > right.releaseTime;
				}
				if (left.Info.HasFabric != right.Info.HasFabric)
				{
					return left.Info.HasFabric;
				}
				if (left.Info.HasQuilt != right.Info.HasQuilt)
				{
					return left.Info.HasQuilt;
				}
				if (left.Info.HasLegacyFabric != right.Info.HasLegacyFabric)
				{
					return left.Info.HasLegacyFabric;
				}
				if (left.Info.HasNeoForge != right.Info.HasNeoForge)
				{
					return left.Info.HasNeoForge;
				}
				if (left.Info.HasForge != right.Info.HasForge)
				{
					return left.Info.HasForge;
				}
				if (left.Info.HasCleanroom != right.Info.HasCleanroom)
				{
					return left.Info.HasCleanroom;
				}
				if (left.Info.HasLabyMod != right.Info.HasLabyMod)
				{
					return left.Info.HasLabyMod;
				}
				if (left.Info.HasOptiFine != right.Info.HasOptiFine)
				{
					return left.Info.HasOptiFine;
				}
				if (left.Info.HasLiteLoader != right.Info.HasLiteLoader)
				{
					return left.Info.HasLiteLoader;
				}
				return (getComponentCode(left) != getComponentCode(right)) ? (getComponentCode(left) > getComponentCode(right)) : (string.CompareOrdinal(left.Name, right.Name) > 0);
			}));
		}
		ModBase.WriteIni(path + "PCL.ini", "CardCount", dictionary.Count.ToString());
		int num = 0;
		for (int num2 = dictionary.Count - 1; num <= num2; num++)
		{
			ModBase.WriteIni(path + "PCL.ini", "CardKey" + (num + 1), ((int)dictionary.Keys.ElementAtOrDefault(num)).ToString());
			string text3 = "";
			foreach (McInstance item6 in dictionary.Values.ElementAtOrDefault(num))
			{
				text3 = text3 + item6.Name + ":";
			}
			ModBase.WriteIni(path + "PCL.ini", "CardValue" + (num + 1), text3);
		}
		return dictionary;
		static int getComponentCode(McInstance instance)
		{
			if (instance.Info.ForgelikeCode > 0)
			{
				return instance.Info.ForgelikeCode;
			}
			if (instance.Info.HasOptiFine)
			{
				return instance.Info.OptiFineCode;
			}
			return 0;
		}
	}

	private static void McInstanceFilter(ref List<McInstance> instanceList, ref Dictionary<McInstanceCardType, List<McInstance>> target, McInstanceState[] formula, McInstanceCardType cardType)
	{
		List<McInstance> list = instanceList.Where((McInstance v) => formula.Contains(v.state, null)).ToList();
		if (list.Any())
		{
			target.Add(cardType, list);
			instanceList = instanceList.Except(list).ToList();
		}
	}

	private static void McInstanceFilter(ref List<McInstance> instanceList, McInstanceState[] formula, ref List<McInstance> keepList)
	{
		keepList.AddRange(instanceList.Where((McInstance v) => formula.Contains(v.state, null)));
		if (keepList.Any())
		{
			instanceList = instanceList.Except(keepList).ToList();
		}
	}
}
