using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Dapper;
using Humanizer;
using Microsoft.Data.Sqlite;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Logging;
using PCL.Core.UI;
using PCL.Core.Utils;
using PCL.Core.Utils.Hash;
using PCL.Network;
using PCL.Network.Loaders;
using ProtoBuf;

namespace PCL;

public static class ModComp
{
	public enum CompLoaderType
	{
		Any = 0,
		Forge = 1,
		LiteLoader = 3,
		Fabric = 4,
		Quilt = 5,
		NeoForge = 6,
		Minecraft = 7,
		Canvas = 8,
		Iris = 9,
		OptiFine = 10,
		Vanilla = 11,
		LabyMod = 12
	}

	public enum CompSortType
	{
		Default = 1,
		Relevance,
		Downloads,
		Follows,
		Newest,
		Updated
	}

	[Flags]
	public enum CompSourceType
	{
		CurseForge = 1,
		Modrinth = 2,
		Any = CurseForge | Modrinth
	}

	public enum CompType
	{
		Any = -1,
		Mod,
		ModPack,
		ResourcePack,
		Shader,
		DataPack,
		Plugin,
		Schematic,
		World
	}

	public enum CompDepsInstallTypes
	{
		Unresolved,
		WithDeps,
		WithoutDeps,
		Cancel
	}

	public enum DownloadReason
	{
		Standalone,
		Dependency,
		ModPack,
		Update
	}

	public class CompFavorites
	{
		public class FavData
		{
			[JsonPropertyName("Name")]
			public string Name { get; set; }

			[JsonPropertyName("Id")]
			public string Id { get; set; }

			[JsonPropertyName("Favs")]
			public HashSet<string> Favs { get; set; } = new HashSet<string>();

			[JsonPropertyName("Notes")]
			public Dictionary<string, string> Notes { get; set; } = new Dictionary<string, string>();
		}

		private static List<FavData> _FavoritesList;

		public static List<FavData> FavoritesList
		{
			get
			{
				if (_FavoritesList == null)
				{
					string compFavorites = States.Game.CompFavorites;
					List<FavData> list = null;
					try
					{
						list = JsonSerializer.Deserialize<List<FavData>>(compFavorites, JsonCompat.SerializerOptions);
					}
					catch (Exception)
					{
						try
						{
							HashSet<string> hashSet = JsonSerializer.Deserialize<HashSet<string>>(compFavorites, JsonCompat.SerializerOptions);
							if (hashSet != null)
							{
								list = new List<FavData> { GetNewFav(Lang.Text("Download.Comp.Detail.Favorites.DefaultName"), hashSet) };
							}
						}
						catch (Exception)
						{
						}
					}
					if (list == null || list.Count == 0)
					{
						list = new List<FavData> { GetNewFav(Lang.Text("Download.Comp.Detail.Favorites.DefaultName"), null) };
					}
					_FavoritesList = list;
					Save();
				}
				return _FavoritesList;
			}
			set
			{
				_FavoritesList = value;
				foreach (FavData favorites in _FavoritesList)
				{
					favorites.Notes = favorites.Notes.Where((KeyValuePair<string, string> n) => !string.IsNullOrWhiteSpace(n.Value)).ToDictionary();
				}
				JsonSerializer.Serialize(_FavoritesList, JsonCompat.SerializerOptions);
				States.Game.CompFavorites = JsonSerializer.Serialize(_FavoritesList, JsonCompat.SerializerOptions);
			}
		}

		public static string GetShareCode(HashSet<string> data)
		{
			try
			{
				return JsonSerializer.Serialize(data, JsonCompat.SerializerOptions);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[CompFavorites] 生成分享出错");
			}
			return "";
		}

		public static HashSet<string> GetIdsByShareCode(string code)
		{
			try
			{
				return JsonSerializer.Deserialize<HashSet<string>>(code, JsonCompat.SerializerOptions);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[CompFavorites] 通过分享获取 ID 出错");
			}
			return new HashSet<string>();
		}

		public static void ShowMenu(CompProject project, UIElement pos, Action closedCallBack = null)
		{
			ContextMenu contextMenu = new ContextMenu();
			foreach (FavData i in FavoritesList)
			{
				MyMenuItem myMenuItem = new MyMenuItem();
				myMenuItem.MaxWidth = 240.0;
				bool hasFavs = i.Favs.Contains(project.Id);
				if (hasFavs)
				{
					myMenuItem.Header = Lang.Text("Download.Comp.Detail.Favorites.UnfavoriteContextMenu", new object[1] { i.Name });
					myMenuItem.SvgIcon = "lucide/heart-filled";
				}
				else
				{
					myMenuItem.Header = Lang.Text("Download.Comp.Detail.Favorites.FavoriteContextMenu", new object[1] { i.Name });
					myMenuItem.SvgIcon = "lucide/heart";
				}
				myMenuItem.Click += (object _, RoutedEventArgs _) =>
				{
					try
					{
						if (hasFavs)
						{
							i.Favs.Remove(project.Id);
							HintService.Hint(Lang.Text("Download.Comp.Detail.Favorites.Remove", new object[2] { project.TranslatedName, i.Name }), HintType.Success);
						}
						else
						{
							i.Favs.Add(project.Id);
							HintService.Hint(Lang.Text("Download.Comp.Detail.Favorites.Add", new object[2] { project.TranslatedName, i.Name }), HintType.Success);
						}
						Save();
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "[CompFavorites] 改变收藏项出错");
					}
				};
				contextMenu.Items.Add(myMenuItem);
			}
			contextMenu.Closed += (object _, RoutedEventArgs _) =>
			{
				closedCallBack?.Invoke();
			};
			contextMenu.Placement = PlacementMode.Bottom;
			contextMenu.PlacementTarget = pos;
			contextMenu.IsOpen = true;
		}

		public static void ShowMenu(List<CompProject> project, UIElement pos, Action closedCallBack = null)
		{
			ContextMenu contextMenu = new ContextMenu();
			foreach (FavData i in FavoritesList)
			{
				MyMenuItem myMenuItem = new MyMenuItem();
				myMenuItem.MaxWidth = 240.0;
				myMenuItem.Header = Lang.Text("Download.Comp.Detail.Favorites.FavoriteContextMenu", new object[1] { i.Name });
				myMenuItem.SvgIcon = "lucide/heart";
				MyMenuItem myMenuItem2 = myMenuItem;
				myMenuItem2.Click += (object _, RoutedEventArgs _) =>
				{
					try
					{
						int count = i.Favs.Count;
						project.Select((CompProject p) => p.Id).ToList().ForEach((string x) =>
						{
							i.Favs.Add(x);
						});
						Save();
						int num = i.Favs.Count - count;
						int num2 = project.Count - num;
						HintService.Hint(Lang.Text((num2 > 0) ? "Download.Comp.Detail.Favorites.BulkAddWithFailures" : "Download.Comp.Detail.Favorites.BulkAdd", new object[3] { num, i.Name, num2 }), HintType.Success);
					}
					catch (Exception ex)
					{
						ModBase.Log(ex, "[CompFavorites] 改变收藏项出错");
					}
				};
				contextMenu.Items.Add(myMenuItem2);
			}
			contextMenu.Closed += (object _, RoutedEventArgs _) =>
			{
				closedCallBack?.Invoke();
			};
			contextMenu.Placement = PlacementMode.Bottom;
			contextMenu.PlacementTarget = pos;
			contextMenu.IsOpen = true;
		}

		public static void Save()
		{
			FavoritesList = _FavoritesList;
		}

		public static FavData GetNewFav(string name, HashSet<string> favList)
		{
			FavData favData = new FavData
			{
				Name = name,
				Id = Guid.NewGuid().ToString()
			};
			if (favList == null)
			{
				favData.Favs = new HashSet<string>();
			}
			else
			{
				favData.Favs = favList;
			}
			return favData;
		}

		public static bool IsFavourite(string id)
		{
			if (FavoritesList == null)
			{
				return false;
			}
			foreach (FavData favorites in FavoritesList)
			{
				if (favorites.Favs.Contains(id))
				{
					return true;
				}
			}
			return false;
		}
	}

	public class CompRequest
	{
		public static bool IsFromCurseForge(string id)
		{
			int result = 0;
			return int.TryParse(id, out result);
		}

		public static async Task<List<CompProject>> GetListByIdsFromModrinthAsync(List<string> ids)
		{
			List<CompProject> res = new List<CompProject>();
			try
			{
				await Task.Run(() =>
				{
					foreach (object item in (IEnumerable)ModDownload.DlModRequest<JsonArray>("https://api.modrinth.com/v2/projects?ids=[\"" + ids.Join("\",\"") + "\"]"))
					{
						res.Add(new CompProject((JsonObject)item));
					}
				});
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "从 Modrinth 获取数据失败");
			}
			return res;
		}

		public static async Task<List<CompProject>> GetListByIdsFromCurseforgeAsync(List<string> ids)
		{
			List<CompProject> res = new List<CompProject>();
			try
			{
				await Task.Run(() =>
				{
					string data = "{\"modIds\": [" + string.Join(",", ids) + "]}";
					JsonArray jsonArray = (JsonArray)ModDownload.DlModRequest<JsonObject>("https://api.curseforge.com/v1/mods", "POST", data, "application/json")["data"];
					if (jsonArray != null)
					{
						List<CompProject> collection = (from JsonObject data2 in jsonArray
							select new CompProject(data2)).ToList();
						res.AddRange(collection);
					}
				});
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "Failed to get project data from CurseForge");
			}
			return res;
		}

		public static List<CompProject> GetCompProjectsByIds(List<string> input)
		{
			return GetCompProjectsByIdsAsync(input).GetAwaiter().GetResult();
		}

		public static async Task<List<CompProject>> GetCompProjectsByIdsAsync(List<string> input)
		{
			if (input != null && !input.Any())
			{
				return new List<CompProject>();
			}
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			foreach (string item in input)
			{
				if (IsFromCurseForge(item))
				{
					list2.Add(item);
				}
				else
				{
					list.Add(item);
				}
			}
			List<Task<List<CompProject>>> tasks = new List<Task<List<CompProject>>>();
			if (list2.Any())
			{
				tasks.Add(GetListByIdsFromCurseforgeAsync(list2));
			}
			if (list.Any())
			{
				tasks.Add(GetListByIdsFromModrinthAsync(list));
			}
			await Task.WhenAll(tasks.ToArray());
			List<CompProject> list3 = new List<CompProject>();
			foreach (Task<List<CompProject>> item2 in tasks)
			{
				list3.AddRange(item2.Result);
			}
			return list3;
		}
	}

	public class CompClipboard
	{
		public static string? currentText;

		public static void GetClipboardResource()
		{
			string text = null;
			ModBase.RunInUiWait(() => text = Clipboard.GetText());
			if (string.IsNullOrEmpty(text) || text == currentText)
			{
				return;
			}
			currentText = text;
			ModBase.RunInNewThread(() =>
			{
				try
				{
					string projectId = ResolveLinkToProjectId(text);
					if (!string.IsNullOrEmpty(projectId))
					{
						ModBase.Log("[Clipboard] Found ProjectId: " + projectId);
						((DispatcherObject)System.Windows.Application.Current).Dispatcher.BeginInvoke((Delegate)(Func<Task>)(async () =>
						{
							if (ModMain.MyMsgBox(Lang.Text("Download.Comp.Detail.Clipboard.Detected.Message"), Lang.Text("Download.Comp.Detail.Clipboard.Detected.Title"), Lang.Text("Common.Action.Confirm"), Lang.Text("Common.Action.Cancel"), "", isWarn: false, highLight: true, forceWait: true) == 1)
							{
								HintService.Hint(Lang.Text("Download.Comp.Detail.Clipboard.Fetching"));
								List<CompProject> list = await CompRequest.GetCompProjectsByIdsAsync(new List<string> { projectId });
								if (list.Count == 0)
								{
									HintService.Hint(Lang.Text("Download.Comp.Detail.Clipboard.InvalidContent"), HintType.Error);
								}
								else
								{
									ModMain.frmMain.PageChange(new FormMain.PageStackData
									{
										page = FormMain.PageType.CompDetail,
										additional = (list.First(), new List<string>(), string.Empty, CompLoaderType.Any, CompType.Any, null)
									});
								}
							}
						}), Array.Empty<object>());
					}
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "Error processing clipboard resource");
				}
			}, "Clipboard Resource Processing");
		}
	}

	[ProtoContract]
	private class CompDatabaseEntry
	{
		[ProtoMember(1)]
		public int WikiId { get; set; }

		[ProtoMember(2)]
		public string ChineseName { get; set; } = "";

		[ProtoMember(3)]
		public string CurseForgeSlug { get; set; }

		[ProtoMember(4)]
		public string ModrinthSlug { get; set; }

		public override string ToString()
		{
			return CurseForgeSlug + "&" + ModrinthSlug + "|" + WikiId + "|" + ChineseName;
		}
	}

	public class CompProject
	{
		private sealed class CompProjectBuildResult
		{
			public bool FromCurseForge;

			public CompType Type;

			public string Slug;

			public string Id;

			public List<int> CurseForgeFileIds = new List<int>();

			public string RawName;

			public string Description;

			public string Website;

			public DateTime? LastUpdate;

			public int DownloadCount;

			public List<CompLoaderType> ModLoaders = new List<CompLoaderType>();

			public List<string> Tags = new List<string>();

			public string LogoUrl;

			public List<int> Drops = new List<int>();
		}

		public readonly List<int> CurseForgeFileIds;

		public readonly string Description;

		public readonly int DownloadCount;

		public readonly List<int> Drops;

		public readonly bool FromCurseForge;

		public readonly string Id;

		public readonly DateTime? LastUpdate;

		public readonly List<CompLoaderType> ModLoaders;

		public readonly string RawName;

		public readonly string Slug;

		public readonly List<string> Tags;

		public readonly CompType Type;

		public readonly string Website;

		private CompDatabaseEntry _DatabaseEntry;

		private bool loadedDatabase;

		public string LogoUrl;

		private static readonly Dictionary<string, CompLoaderType> modrinthLoaderTypes = new Dictionary<string, CompLoaderType>(StringComparer.OrdinalIgnoreCase)
		{
			["forge"] = CompLoaderType.Forge,
			["fabric"] = CompLoaderType.Fabric,
			["quilt"] = CompLoaderType.Quilt,
			["neoforge"] = CompLoaderType.NeoForge
		};

		private static readonly Dictionary<int, string> curseForgeCategoryLangKeys = new Dictionary<int, string>
		{
			[406] = "Download.Comp.Category.WorldGen",
			[407] = "Download.Comp.Category.Biomes",
			[410] = "Download.Comp.Category.Dimensions",
			[408] = "Download.Comp.Category.OresResources",
			[409] = "Download.Comp.Category.Structures",
			[412] = "Download.Comp.Category.Technology",
			[415] = "Download.Comp.Category.PipesLogistics",
			[4843] = "Download.Comp.Category.Automation",
			[417] = "Download.Comp.Category.Energy",
			[4558] = "Download.Comp.Category.Redstone",
			[436] = "Download.Comp.Category.FoodCooking",
			[416] = "Download.Comp.Category.Farming",
			[414] = "Download.Comp.Category.Transportation",
			[420] = "Download.Comp.Category.Storage",
			[419] = "Download.Comp.Category.Magic",
			[422] = "Download.Comp.Category.Adventure",
			[424] = "Download.Comp.Category.Decoration",
			[411] = "Download.Comp.Category.Mobs",
			[434] = "Download.Comp.Category.Equipment",
			[6814] = "Download.Comp.Category.Optimization",
			[9026] = "Download.Comp.Category.Creative",
			[423] = "Download.Comp.Category.Display",
			[435] = "Download.Comp.Category.Server",
			[5191] = "Download.Comp.Category.Tweaks",
			[421] = "Download.Comp.Category.Library",
			[4484] = "Download.Comp.Category.Multiplayer",
			[4479] = "Download.Comp.Category.Modpack.Hardcore",
			[4483] = "Download.Comp.Category.Combat",
			[4478] = "Download.Comp.Category.Modpack.Quests",
			[4472] = "Download.Comp.Category.Technology",
			[4473] = "Download.Comp.Category.Magic",
			[4475] = "Download.Comp.Category.Adventure",
			[4476] = "Download.Comp.Category.Modpack.Exploration",
			[4477] = "Download.Comp.Category.Modpack.MiniGame",
			[4471] = "Download.Comp.Category.Modpack.SciFi",
			[4736] = "Download.Comp.Category.Modpack.Skyblock",
			[5128] = "Download.Comp.Category.Modpack.VanillaPlus",
			[4487] = "Download.Comp.Category.Modpack.Ftb",
			[4480] = "Download.Comp.Category.Modpack.MapBased",
			[4481] = "Download.Comp.Category.Modpack.SmallLight",
			[4482] = "Download.Comp.Category.Modpack.ExtraLarge",
			[403] = "Download.Comp.Category.VanillaLike",
			[400] = "Download.Comp.Category.Realistic",
			[401] = "Download.Comp.Category.Modern",
			[402] = "Download.Comp.Category.Medieval",
			[399] = "Download.Comp.Category.Steampunk",
			[5244] = "Download.Comp.Category.Fonts",
			[404] = "Download.Comp.Category.Animated",
			[4465] = "Download.Comp.Category.ModSupport",
			[393] = "Download.Comp.Category.ResourcePack.Resolution16x",
			[394] = "Download.Comp.Category.ResourcePack.Resolution32x",
			[395] = "Download.Comp.Category.ResourcePack.Resolution64x",
			[396] = "Download.Comp.Category.ResourcePack.Resolution128x",
			[397] = "Download.Comp.Category.ResourcePack.Resolution256x",
			[398] = "Download.Comp.Category.ResourcePack.Resolution512xOrHigher",
			[5193] = "Download.Comp.Type.DataPack",
			[6553] = "Download.Comp.Category.Realistic",
			[6554] = "Download.Comp.Category.Fantasy",
			[6555] = "Download.Comp.Category.VanillaLike",
			[6948] = "Download.Comp.Category.Adventure",
			[6949] = "Download.Comp.Category.DataPack.Fantasy",
			[6950] = "Download.Comp.Category.Library",
			[6952] = "Download.Comp.Category.Magic",
			[6946] = "Download.Comp.Category.Mod.ModRelated",
			[6951] = "Download.Comp.Category.Technology",
			[6953] = "Download.Comp.Category.Utility",
			[248] = "Download.Comp.Category.Adventure",
			[249] = "Download.Comp.Category.World.Creative",
			[250] = "Download.Comp.Category.Modpack.MiniGame",
			[251] = "Download.Comp.Category.World.Parkour",
			[252] = "Download.Comp.Category.World.Puzzle",
			[253] = "Download.Comp.Category.World.Survival",
			[4464] = "Download.Comp.Category.World.ModWorld"
		};

		private static readonly Dictionary<string, string> resourcePackHiddenCategoryLangKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["decoration"] = "Download.Comp.Category.Decoration",
			["mobs"] = "Download.Comp.Category.Mobs",
			["equipment"] = "Download.Comp.Category.Equipment"
		};

		private static readonly Dictionary<string, string> modrinthCategoryLangKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["technology"] = "Download.Comp.Category.Technology",
			["magic"] = "Download.Comp.Category.Magic",
			["adventure"] = "Download.Comp.Category.Adventure",
			["utility"] = "Download.Comp.Category.Utility",
			["optimization"] = "Download.Comp.Category.Optimization",
			["vanilla-like"] = "Download.Comp.Category.VanillaLike",
			["realistic"] = "Download.Comp.Category.Realistic",
			["worldgen"] = "Download.Comp.Category.WorldGen",
			["food"] = "Download.Comp.Category.FoodCooking",
			["game-mechanics"] = "Download.Comp.Category.GameMechanics",
			["transportation"] = "Download.Comp.Category.Transportation",
			["storage"] = "Download.Comp.Category.Storage",
			["social"] = "Download.Comp.Category.Server",
			["library"] = "Download.Comp.Category.Library",
			["multiplayer"] = "Download.Comp.Category.Multiplayer",
			["challenging"] = "Download.Comp.Category.Modpack.Hardcore",
			["combat"] = "Download.Comp.Category.Combat",
			["quests"] = "Download.Comp.Category.Modpack.Quests",
			["kitchen-sink"] = "Download.Comp.Category.Modpack.KitchenSink",
			["lightweight"] = "Download.Comp.Category.Modpack.SmallLight",
			["simplistic"] = "Download.Comp.Category.Simplistic",
			["tweaks"] = "Download.Comp.Category.Tweaks",
			["8x-"] = "Download.Comp.Category.ResourcePack.Resolution8xOrLower",
			["16x"] = "Download.Comp.Category.ResourcePack.Resolution16x",
			["32x"] = "Download.Comp.Category.ResourcePack.Resolution32x",
			["48x"] = "Download.Comp.Category.ResourcePack.Resolution48x",
			["64x"] = "Download.Comp.Category.ResourcePack.Resolution64x",
			["128x"] = "Download.Comp.Category.ResourcePack.Resolution128x",
			["256x"] = "Download.Comp.Category.ResourcePack.Resolution256x",
			["512x+"] = "Download.Comp.Category.ResourcePack.Resolution512xOrHigher",
			["audio"] = "Download.Comp.Category.Audio",
			["fonts"] = "Download.Comp.Category.Fonts",
			["models"] = "Download.Comp.Category.Models",
			["gui"] = "Download.Comp.Category.Gui",
			["locale"] = "Download.Comp.Category.Locale",
			["core-shaders"] = "Download.Comp.Category.CoreShaders",
			["modded"] = "Download.Comp.Category.ModSupport",
			["fantasy"] = "Download.Comp.Category.Fantasy",
			["semi-realistic"] = "Download.Comp.Category.SemiRealistic",
			["cartoon"] = "Download.Comp.Category.Cartoon",
			["colored-lighting"] = "Download.Comp.Category.ColoredLighting",
			["path-tracing"] = "Download.Comp.Category.PathTracing",
			["pbr"] = "Download.Comp.Category.Pbr",
			["reflections"] = "Download.Comp.Category.Reflections",
			["iris"] = "Download.Comp.Category.Iris",
			["optifine"] = "Download.Comp.Category.Optifine",
			["vanilla"] = "Download.Comp.Filter.Loader.VanillaAvailable"
		};

		private CompDatabaseEntry DatabaseEntry
		{
			get
			{
				if (!loadedDatabase)
				{
					loadedDatabase = true;
					if (Type == CompType.Mod || Type == CompType.DataPack)
					{
						_DatabaseEntry = GetCompWikiEntryBySlug(Slug);
					}
				}
				return _DatabaseEntry;
			}
			set
			{
				loadedDatabase = true;
				_DatabaseEntry = value;
			}
		}

		public int WikiId
		{
			get
			{
				if (DatabaseEntry != null)
				{
					return DatabaseEntry.WikiId;
				}
				return 0;
			}
		}

		public string TranslatedName
		{
			get
			{
				if (Lang.IsChineseMainland)
				{
					string text = DatabaseEntry?.ChineseName;
					if (text != null && text.Length > 0)
					{
						return text;
					}
				}
				return RawName;
			}
		}

		public Task<string> ChineseDescription => GetChineseDescriptionAsync();

		public CompProject(JsonObject data)
		{
			CompProjectBuildResult compProjectBuildResult = (data.ContainsKey("Tags") ? _BuildFromCompJson(data) : (data.ContainsKey("summary") ? _BuildFromCurseForge(data) : _BuildFromModrinth(data)));
			if (!data.ContainsKey("Tags"))
			{
				if (compProjectBuildResult.Tags.Count == 0)
				{
					compProjectBuildResult.Tags.Add(Lang.Text("Download.Comp.Category.Other"));
				}
				compProjectBuildResult.Tags = compProjectBuildResult.Tags.Distinct().ToList();
				compProjectBuildResult.Tags.Sort();
				compProjectBuildResult.ModLoaders = (from t in compProjectBuildResult.ModLoaders.Distinct()
					orderby t
					select t).ToList();
			}
			FromCurseForge = compProjectBuildResult.FromCurseForge;
			Type = compProjectBuildResult.Type;
			Slug = compProjectBuildResult.Slug;
			Id = compProjectBuildResult.Id;
			CurseForgeFileIds = compProjectBuildResult.CurseForgeFileIds;
			RawName = compProjectBuildResult.RawName;
			Description = compProjectBuildResult.Description;
			Website = compProjectBuildResult.Website;
			LastUpdate = compProjectBuildResult.LastUpdate;
			DownloadCount = compProjectBuildResult.DownloadCount;
			ModLoaders = compProjectBuildResult.ModLoaders;
			Tags = compProjectBuildResult.Tags;
			LogoUrl = compProjectBuildResult.LogoUrl;
			Drops = compProjectBuildResult.Drops;
			compProjectCache[Id] = this;
		}

		private static CompProjectBuildResult _BuildFromCompJson(JsonObject data)
		{
			CompProjectBuildResult compProjectBuildResult = new CompProjectBuildResult
			{
				FromCurseForge = ((string?)data["DataSource"] == "CurseForge"),
				Type = (CompType)JsonCompat.ToObject<int>(data["Type"]),
				Slug = (string?)data["Slug"],
				Id = (string?)data["Id"],
				RawName = (string?)data["RawName"],
				Description = (string?)data["Description"],
				Website = (string?)data["Website"],
				DownloadCount = (int)data["DownloadCount"],
				Tags = ((JsonArray)data["Tags"]).Select((JsonNode t) => t.ToString()).ToList()
			};
			if (data.TryGetPropertyValue("CurseForgeFileIds", out JsonNode jsonNode))
			{
				compProjectBuildResult.CurseForgeFileIds = ((JsonArray)jsonNode).Select((JsonNode t) => JsonCompat.ToObject<int>(t)).ToList();
			}
			if (data.TryGetPropertyValue("LastUpdate", out JsonNode jsonNode2))
			{
				compProjectBuildResult.LastUpdate = ((jsonNode2 != null) ? new DateTime?(JsonCompat.ToObject<DateTime>(jsonNode2)) : ((DateTime?)null));
			}
			if (data.TryGetPropertyValue("ModLoaders", out JsonNode jsonNode3))
			{
				compProjectBuildResult.ModLoaders = ((JsonArray)jsonNode3).Select((JsonNode t) => (CompLoaderType)JsonCompat.ToObject<int>(t)).ToList();
			}
			if (data.TryGetPropertyValue("LogoUrl", out JsonNode jsonNode4))
			{
				compProjectBuildResult.LogoUrl = (string?)jsonNode4;
			}
			if (data.TryGetPropertyValue("Drops", out JsonNode jsonNode5))
			{
				compProjectBuildResult.Drops = ((JsonArray)jsonNode5).Select((JsonNode t) => JsonCompat.ToObject<int>(t)).ToList();
			}
			return compProjectBuildResult;
		}

		private static CompProjectBuildResult _BuildFromCurseForge(JsonObject data)
		{
			CompProjectBuildResult compProjectBuildResult = new CompProjectBuildResult
			{
				FromCurseForge = true,
				Id = data["id"].ToString(),
				Slug = (string?)data["slug"],
				RawName = (string?)data["name"],
				Description = (string?)data["summary"],
				Website = (data["links"]?["websiteUrl"]?.ToString() ?? "").TrimEnd('/')
			};
			JsonNode? jsonNode = data["dateReleased"];
			compProjectBuildResult.LastUpdate = ((jsonNode != null) ? new DateTime?(JsonCompat.ToObject<DateTime>(jsonNode)) : ((DateTime?)null));
			compProjectBuildResult.DownloadCount = (int)data["downloadCount"];
			CompProjectBuildResult compProjectBuildResult2 = compProjectBuildResult;
			if (data["logo"] is JsonObject { Count: >0 } jsonObject)
			{
				compProjectBuildResult2.LogoUrl = (string.IsNullOrEmpty((string?)jsonObject["thumbnailUrl"]) ? ((string?)jsonObject["url"]) : ((string?)jsonObject["thumbnailUrl"]));
			}
			if (string.IsNullOrEmpty(compProjectBuildResult2.LogoUrl))
			{
				compProjectBuildResult2.LogoUrl = null;
			}
			compProjectBuildResult2.Type = _GetCurseForgeTypeByWebsite(compProjectBuildResult2.Website);
			List<KeyValuePair<int, List<string>>> list = new List<KeyValuePair<int, List<string>>>();
			foreach (JsonNode item in (data["latestFiles"] as JsonArray) ?? new JsonArray())
			{
				CompFile compFile = new CompFile((JsonObject)item, compProjectBuildResult2.Type);
				if (compFile.Available)
				{
					compProjectBuildResult2.ModLoaders.AddRange(compFile.ModLoaders);
					JsonNode? jsonNode2 = item["gameVersions"];
					List<string> list2 = ((jsonNode2 != null) ? JsonCompat.ToObject<List<string>>(jsonNode2) : null) ?? new List<string>();
					if (list2.Any(McInstanceInfo.IsFormatFit))
					{
						list.Add(new KeyValuePair<int, List<string>>((int)item["id"], list2));
					}
				}
			}
			list.AddRange(from File in (data["latestFilesIndexes"] as JsonArray) ?? new JsonArray()
				let GameVersion = File["gameVersion"]?.ToString() ?? ""
				where McInstanceInfo.IsFormatFit(GameVersion)
				select new KeyValuePair<int, List<string>>((int)File["fileId"], new string[1] { GameVersion }.ToList()));
			compProjectBuildResult2.CurseForgeFileIds = list.Select((KeyValuePair<int, List<string>> f) => f.Key).Distinct().ToList();
			compProjectBuildResult2.Drops = (from v in (from v in list.SelectMany((KeyValuePair<int, List<string>> f) => f.Value)
					select McInstanceInfo.VersionToDrop(v) into v
					where v > 0
					select v).Distinct()
				orderby v descending
				select v).ToList();
			compProjectBuildResult2.ModLoaders = (from t in compProjectBuildResult2.ModLoaders.Distinct()
				orderby t
				select t).ToList();
			foreach (int item2 in from t in (from t in ((data["categories"] as JsonArray) ?? new JsonArray()).Select((JsonNode t) =>
					{
						JsonNode? jsonNode3 = t["id"];
						return (jsonNode3 == null) ? ((int?)null) : JsonCompat.ToObject<int?>(jsonNode3);
					})
					where t.HasValue
					select t.Value).Distinct()
				orderby t descending
				select t)
			{
				if (curseForgeCategoryLangKeys.TryGetValue(item2, out string value))
				{
					_AddTag(compProjectBuildResult2.Tags, value);
				}
			}
			return compProjectBuildResult2;
		}

		private static CompProjectBuildResult _BuildFromModrinth(JsonObject data)
		{
			string text = data["project_type"]?.ToString() ?? "";
			string text2 = (string?)data["slug"];
			CompProjectBuildResult compProjectBuildResult = new CompProjectBuildResult();
			compProjectBuildResult.FromCurseForge = false;
			compProjectBuildResult.Id = (string?)(data["project_id"] ?? data["id"]);
			compProjectBuildResult.Slug = text2;
			compProjectBuildResult.RawName = (string?)data["title"];
			compProjectBuildResult.Description = (string?)data["description"];
			JsonNode? jsonNode = data["date_modified"];
			compProjectBuildResult.LastUpdate = ((jsonNode != null) ? new DateTime?(JsonCompat.ToObject<DateTime>(jsonNode)) : ((DateTime?)null));
			compProjectBuildResult.DownloadCount = (int)data["downloads"];
			compProjectBuildResult.LogoUrl = (string?)data["icon_url"];
			compProjectBuildResult.Website = "https://modrinth.com/" + text + "/" + text2;
			CompProjectBuildResult compProjectBuildResult2 = compProjectBuildResult;
			compProjectBuildResult2.Type = text switch
			{
				"modpack" => CompType.ModPack, 
				"resourcepack" => CompType.ResourcePack, 
				"shader" => CompType.Shader, 
				_ => CompType.Mod, 
			};
			CompProjectBuildResult compProjectBuildResult3 = compProjectBuildResult;
			if (string.IsNullOrEmpty(compProjectBuildResult3.LogoUrl))
			{
				compProjectBuildResult3.LogoUrl = null;
			}
			compProjectBuildResult3.Drops = (from v in (from v in ((data["game_versions"] ?? data["versions"]) as JsonArray) ?? new JsonArray()
					select McInstanceInfo.VersionToDrop((string?)v) into v
					where v > 0
					select v).Distinct()
				orderby v descending
				select v).ToList();
			JsonArray obj = data["loaders"] as JsonArray;
			foreach (string item in ((obj != null) ? obj.Select((JsonNode t) => t.ToString()) : null) ?? Array.Empty<string>())
			{
				if (modrinthLoaderTypes.TryGetValue(item ?? "", out var value))
				{
					compProjectBuildResult3.ModLoaders.Add(value);
				}
			}
			JsonArray obj2 = data["categories"] as JsonArray;
			foreach (string item2 in ((obj2 != null) ? obj2.Select((JsonNode t) => t.ToString()) : null) ?? Array.Empty<string>())
			{
				if (string.IsNullOrEmpty(item2))
				{
					continue;
				}
				string value3;
				string value4;
				if (modrinthLoaderTypes.TryGetValue(item2, out var value2))
				{
					compProjectBuildResult3.ModLoaders.Add(value2);
				}
				else if (item2.Equals("datapack", StringComparison.OrdinalIgnoreCase))
				{
					compProjectBuildResult3.Type = CompType.DataPack;
				}
				else if (resourcePackHiddenCategoryLangKeys.TryGetValue(item2, out value3))
				{
					if (compProjectBuildResult3.Type != CompType.ResourcePack)
					{
						_AddTag(compProjectBuildResult3.Tags, value3);
					}
				}
				else if (modrinthCategoryLangKeys.TryGetValue(item2, out value4))
				{
					_AddTag(compProjectBuildResult3.Tags, value4);
				}
			}
			return compProjectBuildResult3;
		}

		private static CompType _GetCurseForgeTypeByWebsite(string website)
		{
			string text = (website ?? "").ToLowerInvariant();
			if (text.Contains("/mc-mods/") || text.Contains("/mod/"))
			{
				return CompType.Mod;
			}
			if (text.Contains("/modpacks/"))
			{
				return CompType.ModPack;
			}
			if (text.Contains("/resourcepacks/") || text.Contains("/texture-packs/"))
			{
				return CompType.ResourcePack;
			}
			if (text.Contains("/shaders/"))
			{
				return CompType.Shader;
			}
			if (text.Contains("/worlds/"))
			{
				return CompType.World;
			}
			return CompType.DataPack;
		}

		private static void _AddTag(List<string> tags, string langKey)
		{
			string item = Lang.Text(langKey);
			if (!tags.Contains(item))
			{
				tags.Add(item);
			}
		}

		private async Task<string> GetChineseDescriptionAsync()
		{
			string text = (FromCurseForge ? "curseforge" : "modrinth");
			_ = FromCurseForge;
			string result = null;
			string descHash = Id + ModBase.GetStringMD5(Description);
			string cacheFilePath = ModBase.pathTemp + "Cache\\CompTranslation.ini";
			string text2 = ModBase.ReadIni(cacheFilePath, descHash);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return ModBase.Base64Decode(text2);
			}
			try
			{
				JsonObject jsonObject = (JsonObject)(await Requester.FetchJsonAsync("https://mod.mcimirror.top/translate/" + text + "/" + Id));
				if (jsonObject.ContainsKey("translated"))
				{
					result = jsonObject["translated"].ToString();
					ModBase.WriteIni(cacheFilePath, descHash, ModBase.Base64Encode(result));
				}
			}
			catch (HttpRequestException ex)
			{
				if (ex.Message.Contains("404"))
				{
					ModMain.MyMsgBox(Lang.Text("Download.Comp.Detail.DescriptionNoTranslation"), Lang.Text("Download.Comp.Detail.DescriptionTranslationFailed"), Lang.Text("Download.Comp.Detail.KnownButton"));
					return null;
				}
				ModBase.Log(ex, "获取中文描述时出现错误", ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Comp.Error.OperationFailed"));
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "获取中文描述时出现错误", ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Comp.Error.OperationFailed"));
			}
			return result;
		}

		public JsonObject ToJson()
		{
			JsonObject jsonObject = new JsonObject();
			jsonObject["DataSource"] = (FromCurseForge ? "CurseForge" : "Modrinth");
			jsonObject["Type"] = (int)Type;
			jsonObject["Slug"] = Slug;
			jsonObject["Id"] = Id;
			if (CurseForgeFileIds != null)
			{
				jsonObject["CurseForgeFileIds"] = new JsonArray(((IEnumerable<int>)CurseForgeFileIds).Select((Func<int, JsonNode>)((int i) => i)).ToArray());
			}
			jsonObject["RawName"] = RawName;
			jsonObject["Description"] = Description;
			jsonObject["Website"] = Website;
			DateTime? lastUpdate = LastUpdate;
			if (lastUpdate.HasValue)
			{
				jsonObject["LastUpdate"] = LastUpdate;
			}
			jsonObject["DownloadCount"] = DownloadCount;
			if (ModLoaders != null && ModLoaders.Any())
			{
				jsonObject["ModLoaders"] = new JsonArray(((IEnumerable<CompLoaderType>)ModLoaders).Select((Func<CompLoaderType, JsonNode>)((CompLoaderType m) => (int)m)).ToArray());
			}
			jsonObject["Tags"] = new JsonArray(((IEnumerable<string>)Tags).Select((Func<string, JsonNode>)((string s) => s)).ToArray());
			if (LogoUrl != null)
			{
				jsonObject["LogoUrl"] = LogoUrl;
			}
			if (Drops.Any())
			{
				jsonObject["Drops"] = new JsonArray(((IEnumerable<int>)Drops).Select((Func<int, JsonNode>)((int i) => i)).ToArray());
			}
			jsonObject["CacheTime"] = DateTime.Now;
			return jsonObject;
		}

		public MyVirtualizingElement<MyCompItem> ToCompItem(bool showMcVersionDesc, bool showLoaderDesc, bool showQuickDownload = false)
		{
			string gameVersionDescription;
			if (Drops == null || !Drops.Any())
			{
				gameVersionDescription = Lang.Text("Download.Comp.Detail.CompItem.SnapshotOnly");
			}
			else
			{
				List<string> list = new List<string>();
				bool flag = false;
				for (int i = 0; i < Drops.Count; i++)
				{
					int num = Drops[i];
					int num2 = Drops[i];
					if (num < 100)
					{
						if (list.Any() && !flag)
						{
							break;
						}
						flag = true;
					}
					for (int j = i + 1; j < Drops.Count; j++)
					{
						if (ModDownload.AllDrops == null)
						{
							break;
						}
						if (ModDownload.AllDrops.IndexOf(Drops[j]) != ModDownload.AllDrops.IndexOf(num2) + 1)
						{
							break;
						}
						num2 = Drops[j];
						i = j;
					}
					string text = McInstanceInfo.DropToVersion(num);
					string text2 = McInstanceInfo.DropToVersion(num2);
					if (num == num2)
					{
						list.Add(text);
						continue;
					}
					List<int> allDrops = ModDownload.AllDrops;
					if (allDrops != null && allDrops.Any() && num >= ModDownload.AllDrops.First())
					{
						if (num2 < 100)
						{
							list.Clear();
							list.Add(Lang.Text("Download.Comp.Detail.CompItem.AllVersions"));
							break;
						}
						list.Add(text2 + "+");
						continue;
					}
					if (num2 < 100)
					{
						list.Add(text + "-");
						break;
					}
					if (ModDownload.AllDrops == null || ModDownload.AllDrops.IndexOf(num2) - ModDownload.AllDrops.IndexOf(num) == 1)
					{
						list.Add(text + ", " + text2);
					}
					else
					{
						list.Add(text + "~" + text2);
					}
				}
				gameVersionDescription = string.Join(", ", list);
			}
			List<CompLoaderType> modLoadersForDesc = ModLoaders.ToList();
			if (Config.Download.Comp.IgnoreQuilt)
			{
				modLoadersForDesc.Remove(CompLoaderType.Quilt);
			}
			var (fullDesc, partDesc) = modLoadersForDesc.Count switch
			{
				0 => (ModLoaders.Count == 1) ? (Lang.Text("Download.Comp.Type.Only", new object[1] { ModLoaders.Single().ToString() }), ModLoaders.Single().ToString()) : (Lang.Text("Download.Comp.Type.Unknown"), ""), 
				1 => (Lang.Text("Download.Comp.Type.Only", new object[1] { modLoadersForDesc.Single().ToString() }), modLoadersForDesc.Single().ToString()), 
				_ => GetMultiLoaderDesc(), 
			};
			return new MyVirtualizingElement<MyCompItem>(() =>
			{
				MyCompItem myCompItem = new MyCompItem
				{
					Tag = this
				};
				ApplyLogoToMyImage(myCompItem.PathLogo);
				KeyValuePair<string, string> controlTitle = GetControlTitle(hasModLoaderDescription: true);
				myCompItem.Title = controlTitle.Key;
				if (string.IsNullOrEmpty(controlTitle.Value))
				{
					((StackPanel)(object)myCompItem.LabTitleRaw.Parent).Children.Remove(myCompItem.LabTitleRaw);
				}
				else
				{
					myCompItem.SubTitle = controlTitle.Value;
				}
				myCompItem.Tags = Tags;
				myCompItem.Description = Description.Replace("\r", "").Replace("\n", "");
				myCompItem.ShowDownloadBtn = showQuickDownload;
				TextBlock labVersion = myCompItem.LabVersion;
				string text3 = (showMcVersionDesc ? ((!showLoaderDesc) ? gameVersionDescription : ((string.IsNullOrEmpty(partDesc) ? "" : (partDesc + " ")) + gameVersionDescription)) : ((!showLoaderDesc) ? "" : fullDesc));
				labVersion.Text = text3;
				if (!showMcVersionDesc && !showLoaderDesc)
				{
					((Grid)(object)((FrameworkElement)(object)myCompItem.SvgIconVersion).Parent).Children.Remove((UIElement)(object)myCompItem.SvgIconVersion);
					((Grid)(object)myCompItem.LabVersion.Parent).Children.Remove(myCompItem.LabVersion);
					myCompItem.ColumnVersion1.Width = new GridLength(0.0);
					myCompItem.ColumnVersion2.MaxWidth = 0.0;
					myCompItem.ColumnVersion3.Width = new GridLength(0.0);
				}
				myCompItem.LabSource.Text = (FromCurseForge ? "CurseForge" : "Modrinth");
				DateTime? lastUpdate = LastUpdate;
				if (lastUpdate.HasValue)
				{
					myCompItem.LabTime.Text = Lang.TimeSpan(LastUpdate.Value - DateTime.Now, 1, true, (TimeUnit)7, (TimeUnit)3);
				}
				else
				{
					myCompItem.LabTime.Visibility = Visibility.Collapsed;
					ColumnDefinition columnTime = myCompItem.ColumnTime1;
					ColumnDefinition columnTime2 = myCompItem.ColumnTime2;
					GridLength gridLength = (myCompItem.ColumnTime3.Width = new GridLength(0.0));
					GridLength width = (columnTime2.Width = gridLength);
					columnTime.Width = width;
				}
				myCompItem.LabDownload.Text = Lang.CompactNumber((long)DownloadCount);
				return myCompItem;
			})
			{
				Height = 64.0
			};
			(string, string) GetMultiLoaderDesc()
			{
				int num3 = Drops?.FirstOrDefault() ?? 9999;
				bool num4 = ModLoaders.Contains(CompLoaderType.Forge) && (num3 < 140 || ModLoaders.Contains(CompLoaderType.Fabric)) && (num3 < 200 || ModLoaders.Contains(CompLoaderType.NeoForge)) && (num3 < 140 || ModLoaders.Contains(CompLoaderType.Quilt) || Config.Download.Comp.IgnoreQuilt);
				string text3 = string.Join(" / ", modLoadersForDesc);
				if (!num4)
				{
					return (text3, text3);
				}
				return (Lang.Text("Download.Comp.Type.Any"), "");
			}
		}

		public MyListItem ToListItem()
		{
			return new MyListItem
			{
				Title = TranslatedName,
				Info = Description.Replace("\r", "").Replace("\n", ""),
				Logo = (string.IsNullOrEmpty(LogoUrl) ? (ModBase.pathImage + "Icons/NoIcon.png") : LogoUrl),
				Tags = Tags,
				Tag = this,
				LogoCornerRadius = new CornerRadius(6.0)
			};
		}

		public void ApplyLogoToMyImage(MyImage img)
		{
			if (string.IsNullOrEmpty(LogoUrl))
			{
				img.Source = ModBase.pathImage + "Icons/NoIcon.png";
				return;
			}
			img.Source = LogoUrl;
			img.FallbackSource = ModDownload.DlSourceModGet(LogoUrl);
		}

		public KeyValuePair<string, string> GetControlTitle(bool hasModLoaderDescription)
		{
			string rawName = RawName;
			List<string> list = new List<string>();
			if (TranslatedName == RawName)
			{
				List<string> list2 = (from text5 in TranslatedName.Split(new string[8] { " | ", " - ", "(", ")", "[", "]", "{", "}" }, StringSplitOptions.RemoveEmptyEntries)
					select text5.Trim(new char[4] { ' ', '/', '\\', '"' }) into w
					where !string.IsNullOrEmpty(w)
					select w).ToList();
				if (list2.Count <= 1)
				{
					return BuildResult(rawName, "");
				}
				List<string> list3 = new List<string>();
				foreach (string item in list2)
				{
					string input = item.ToLower();
					if (item.ToUpper() == item && item != "FPS" && item != "HUD")
					{
						list.Add(item);
					}
					else if (IsModLoaderMarker(input))
					{
						list.Add(item);
					}
					else
					{
						list3.Add(item);
					}
				}
				if (!list3.Any() || !list.Any())
				{
					return BuildResult(rawName, "");
				}
				rawName = string.Join(" - ", list3);
			}
			else
			{
				rawName = TranslatedName.BeforeFirst(" (").BeforeFirst(" - ");
				string text = "";
				if (TranslatedName.AfterLast(")").Contains(" - "))
				{
					text = TranslatedName.AfterLast(")").AfterLast(" - ");
				}
				string text2 = TranslatedName;
				if (!string.IsNullOrEmpty(text))
				{
					text2 = text2.Replace(" - " + text, "");
				}
				text2 = text2.Replace(rawName, "").Trim(new char[3] { '(', ')', ' ' });
				list = (from text5 in text2.Split(new string[8] { " | ", " - ", "(", ")", "[", "]", "{", "}" }, StringSplitOptions.RemoveEmptyEntries)
					select text5.Trim(new char[2] { ' ', '/' }) into w
					where !string.IsNullOrEmpty(w)
					select w).ToList();
				if (list.Count > 1 && !list.Any((string text5) => IsModLoaderMarker(text5.ToLower())) && (list.Count != 2 || !(list.Last().ToUpper() == list.Last())))
				{
					list = new List<string> { text2 };
				}
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(text);
				}
			}
			List<string> list4 = new List<string>();
			foreach (string item2 in list.Distinct())
			{
				string text3 = item2;
				string text4 = text3.ToLower();
				bool flag = text4.Contains("forge") || text4.Contains("fabric") || text4.Contains("quilt");
				if (!(!hasModLoaderDescription & flag) && (text3.Length >= 16 || !text4.Contains("fabric") || !text4.Contains("forge")))
				{
					if (flag && !text3.Contains("版") && text4.Replace("forge", "").Replace("fabric", "").Replace("quilt", "")
						.Length <= 3)
					{
						text3 = text3.Replace("Edition", "", StringComparison.OrdinalIgnoreCase).Replace("edition", "", StringComparison.OrdinalIgnoreCase).Trim()
							.Capitalize() + Lang.Text("Download.Comp.Detail.CompItem.EditionSuffix");
					}
					text3 = text3.Replace("forge", "Forge").Replace("neo", "Neo").Replace("fabric", "Fabric")
						.Replace("quilt", "Quilt");
					list4.Add(text3.Trim());
				}
			}
			string s = (list4.Any() ? ("  |  " + string.Join("  |  ", list4)) : "");
			return BuildResult(rawName, s);
			static KeyValuePair<string, string> BuildResult(string t, string value)
			{
				return new KeyValuePair<string, string>(t, value);
			}
			static bool IsModLoaderMarker(string text5)
			{
				if (text5.Contains("forge") || text5.Contains("fabric") || text5.Contains("quilt"))
				{
					return !text5.Replace("forge", "").Replace("fabric", "").Replace("quilt", "")
						.RegexCheck("[a-z]+");
				}
				return false;
			}
		}

		public bool IsLike(CompProject project)
		{
			if ((Id ?? "") == (project.Id ?? ""))
			{
				return true;
			}
			if (FromCurseForge == project.FromCurseForge)
			{
				return false;
			}
			if (ModLoaders.Count != project.ModLoaders.Count || ModLoaders.Except(project.ModLoaders).Any())
			{
				return false;
			}
			if (Type != CompType.Shader && (Drops.Count != project.Drops.Count || Drops.Except(project.Drops).Any()))
			{
				return false;
			}
			DateTime? lastUpdate = LastUpdate;
			if (lastUpdate.HasValue)
			{
				lastUpdate = project.LastUpdate;
				if (lastUpdate.HasValue && Math.Abs((LastUpdate - project.LastUpdate).Value.TotalDays) > 7.0)
				{
					return false;
				}
			}
			if ((TranslatedName ?? "") == (project.TranslatedName ?? "") || (RawName ?? "") == (project.RawName ?? "") || (Description ?? "") == (project.Description ?? "") || (GetRaw(Slug) ?? "") == (GetRaw(project.Slug) ?? ""))
			{
				ModBase.Log($"[Comp] 将 {RawName} ({Slug}) 与 {project.RawName} ({project.Slug}) 认定为相似工程");
				if (DatabaseEntry == null && project.DatabaseEntry != null)
				{
					DatabaseEntry = project.DatabaseEntry;
				}
				if (DatabaseEntry != null && project.DatabaseEntry == null)
				{
					project.DatabaseEntry = DatabaseEntry;
				}
				return true;
			}
			return false;
			static string GetRaw(string data)
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (char item in data.Where((char c) => char.IsLetterOrDigit(c)))
				{
					stringBuilder.Append(item);
				}
				return stringBuilder.ToString().ToLower();
			}
		}

		public override string ToString()
		{
			return $"{Id} ({Slug}): {RawName}";
		}

		public override bool Equals(object obj)
		{
			if (obj is CompProject compProject)
			{
				return (Id ?? "") == (compProject.Id ?? "");
			}
			return false;
		}

		public static bool operator ==(CompProject left, CompProject right)
		{
			return EqualityComparer<CompProject>.Default.Equals(left, right);
		}

		public static bool operator !=(CompProject left, CompProject right)
		{
			return !(left == right);
		}
	}

	public class CompProjectRequest
	{
		public string gameVersion;

		public CompLoaderType modLoader;

		public string searchText;

		public string curseForgeAltSearchText;

		public CompSortType sort = CompSortType.Default;

		public CompSourceType source = CompSourceType.Any;

		public CompProjectStorage storage;

		public string tag = "";

		public int targetResultCount;

		public CompType type;

		public bool CanContinue
		{
			get
			{
				if (tag.StartsWithF("/") || !source.HasFlag(CompSourceType.CurseForge))
				{
					storage.curseForgeTotal = 0;
				}
				if (tag.EndsWithF("/") || !source.HasFlag(CompSourceType.Modrinth))
				{
					storage.modrinthTotal = 0;
				}
				if (storage.curseForgeTotal == -1 || storage.modrinthTotal == -1)
				{
					return true;
				}
				if (storage.curseForgeOffset >= storage.curseForgeTotal)
				{
					return storage.modrinthOffset < storage.modrinthTotal;
				}
				return true;
			}
		}

		public CompProjectRequest(CompType type, CompProjectStorage storage, int targetResultCount)
		{
			this.type = type;
			this.storage = storage;
			this.targetResultCount = targetResultCount;
		}

		public string GetCurseForgeAddress()
		{
			if (!source.HasFlag(CompSourceType.CurseForge))
			{
				return null;
			}
			if (tag.StartsWithF("/"))
			{
				storage.curseForgeTotal = 0;
			}
			if (storage.curseForgeTotal > -1 && storage.curseForgeTotal <= storage.curseForgeOffset)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder($"https://api.curseforge.com/v1/mods/search?gameId=432&sortOrder=desc&pageSize={40}");
			switch (type)
			{
			case CompType.Mod:
				stringBuilder.Append("&classId=6");
				break;
			case CompType.ModPack:
				stringBuilder.Append("&classId=4471");
				break;
			case CompType.DataPack:
				stringBuilder.Append("&classId=6945");
				break;
			case CompType.Shader:
				stringBuilder.Append("&classId=6552");
				break;
			case CompType.ResourcePack:
				stringBuilder.Append("&classId=12");
				break;
			case CompType.World:
				stringBuilder.Append("&classId=17");
				break;
			}
			if (!string.IsNullOrEmpty(tag))
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
				handler.AppendLiteral("&categoryId=");
				handler.AppendFormatted(tag.BeforeFirst("/"));
				stringBuilder2.Append(ref handler);
			}
			if (modLoader != CompLoaderType.Any)
			{
				StringBuilder stringBuilder3 = stringBuilder.Append("&modLoaderType=");
				int num = (int)modLoader;
				stringBuilder3.Append(num.ToString());
			}
			if (!string.IsNullOrEmpty(gameVersion))
			{
				stringBuilder.Append("&gameVersion=").Append(gameVersion);
			}
			if (!string.IsNullOrEmpty(curseForgeAltSearchText ?? searchText))
			{
				stringBuilder.Append("&searchFilter=").Append(WebUtility.UrlEncode(curseForgeAltSearchText ?? searchText));
			}
			if (storage.curseForgeOffset > 0)
			{
				stringBuilder.Append("&index=").Append(storage.curseForgeOffset);
			}
			switch (sort)
			{
			case CompSortType.Relevance:
				stringBuilder.Append("&sortField=4");
				break;
			case CompSortType.Downloads:
				stringBuilder.Append("&sortField=6");
				break;
			case CompSortType.Follows:
				stringBuilder.Append("&sortField=2");
				break;
			case CompSortType.Newest:
				stringBuilder.Append("&sortField=11");
				break;
			case CompSortType.Updated:
				stringBuilder.Append("&sortField=3");
				break;
			default:
				stringBuilder.Append("&sortField=2");
				break;
			}
			return stringBuilder.ToString();
		}

		public string GetModrinthAddress()
		{
			if (!source.HasFlag(CompSourceType.Modrinth))
			{
				return null;
			}
			if (tag.EndsWithF("/"))
			{
				storage.modrinthTotal = 0;
			}
			if (storage.modrinthTotal > -1 && storage.modrinthTotal <= storage.modrinthOffset)
			{
				return null;
			}
			string text = $"https://api.modrinth.com/v2/search?limit={40}";
			text = sort switch
			{
				CompSortType.Relevance => text + "&index=relevance", 
				CompSortType.Downloads => text + "&index=downloads", 
				CompSortType.Follows => text + "&index=follows", 
				CompSortType.Newest => text + "&index=newest", 
				CompSortType.Updated => text + "&index=updated", 
				_ => text + "&index=relevance", 
			};
			if (!string.IsNullOrEmpty(searchText))
			{
				text = text + "&query=" + WebUtility.UrlEncode(searchText);
			}
			if (storage.modrinthOffset > 0)
			{
				text = text + "&offset=" + storage.modrinthOffset;
			}
			List<string> list = new List<string>();
			list.Add("[\"project_type:" + ModBase.GetStringFromEnum(type).ToLower() + "\"]");
			if (!string.IsNullOrEmpty(tag))
			{
				list.Add("[\"categories:'" + tag.AfterLast("/") + "'\"]");
			}
			if (modLoader != CompLoaderType.Any)
			{
				list.Add("[\"categories:'" + ModBase.GetStringFromEnum(modLoader).ToLower() + "'\"]");
			}
			if (!string.IsNullOrEmpty(gameVersion))
			{
				list.Add("[\"versions:'" + gameVersion + "'\"]");
			}
			return text + "&facets=[" + string.Join(",", list) + "]";
		}

		public override bool Equals(object obj)
		{
			if (obj is CompProjectRequest compProjectRequest && type == compProjectRequest.type && targetResultCount == compProjectRequest.targetResultCount && (tag ?? "") == (compProjectRequest.tag ?? "") && modLoader == compProjectRequest.modLoader && source == compProjectRequest.source && (gameVersion ?? "") == (compProjectRequest.gameVersion ?? "") && (searchText ?? "") == (compProjectRequest.searchText ?? ""))
			{
				return sort == compProjectRequest.sort;
			}
			return false;
		}

		public static bool operator ==(CompProjectRequest left, CompProjectRequest right)
		{
			return EqualityComparer<CompProjectRequest>.Default.Equals(left, right);
		}

		public static bool operator !=(CompProjectRequest left, CompProjectRequest right)
		{
			return !(left == right);
		}
	}

	public class CompProjectStorage
	{
		public int curseForgeOffset;

		public int curseForgeTotal = -1;

		public string errorMessage;

		public int modrinthOffset;

		public int modrinthTotal = -1;

		public List<CompProject> results = new List<CompProject>();
	}

	private enum ResourceSite
	{
		None,
		CurseForge,
		Modrinth
	}

	public enum CompFileStatus
	{
		Release = 1,
		Beta,
		Alpha
	}

	public class CompFile
	{
		public readonly List<string> Dependencies = new List<string>();

		public readonly int DownloadCount;

		public readonly string FileName;

		public readonly bool FromCurseForge;

		public readonly List<string> RawGameVersions = new List<string>();

		public readonly List<string> GameVersions = new List<string>();

		public readonly string Hash;

		public readonly string Id;

		public readonly List<CompLoaderType> ModLoaders = new List<CompLoaderType>();

		public readonly List<string> OptionalDependencies = new List<string>();

		public readonly string ProjectId;

		public readonly List<string> RawDependencies = new List<string>();

		public readonly List<string> RawOptionalDependencies = new List<string>();

		public readonly DateTime ReleaseDate;

		public readonly CompFileStatus Status;

		public readonly CompType Type;

		public string DisplayName;

		public List<string> DownloadUrls;

		public string Version;

		public string StatusDescription => Status switch
		{
			CompFileStatus.Release => Lang.Text("Download.Comp.Detail.FileList.ReleaseType.Release"), 
			CompFileStatus.Beta => Lang.Text("Download.Comp.Detail.FileList.ReleaseType.Beta"), 
			_ => Lang.Text("Download.Comp.Detail.FileList.ReleaseType.Alpha"), 
		};

		public bool Available
		{
			get
			{
				if (FileName != null)
				{
					return DownloadUrls != null;
				}
				return false;
			}
		}

		public CompFile(JsonObject data, CompType defaultType)
		{
			Type = defaultType;
			if (data.ContainsKey("FromCurseForge"))
			{
				FromCurseForge = JsonCompat.ToObject<bool>(data["FromCurseForge"]);
				Id = data["Id"].ToString();
				DisplayName = data["DisplayName"].ToString();
				if (data.ContainsKey("Version"))
				{
					Version = data["Version"].ToString();
				}
				ReleaseDate = JsonCompat.ToObject<DateTime>(data["ReleaseDate"]);
				DownloadCount = JsonCompat.ToObject<int>(data["DownloadCount"]);
				Status = (CompFileStatus)JsonCompat.ToObject<int>(data["Status"]);
				if (data.ContainsKey("FileName"))
				{
					FileName = data["FileName"].ToString();
				}
				if (data.ContainsKey("DownloadUrls"))
				{
					DownloadUrls = JsonCompat.ToObject<List<string>>(data["DownloadUrls"]);
				}
				if (data.ContainsKey("ModLoaders"))
				{
					ModLoaders = JsonCompat.ToObject<List<CompLoaderType>>(data["ModLoaders"]);
				}
				if (data.ContainsKey("Hash"))
				{
					Hash = data["Hash"].ToString();
				}
				if (data.ContainsKey("RawGameVersions"))
				{
					RawGameVersions = JsonCompat.ToObject<List<string>>(data["RawGameVersions"]);
				}
				if (data.ContainsKey("GameVersions"))
				{
					GameVersions = JsonCompat.ToObject<List<string>>(data["GameVersions"]);
				}
				if (data.ContainsKey("RawDependencies"))
				{
					RawDependencies = JsonCompat.ToObject<List<string>>(data["RawDependencies"]);
				}
				if (data.ContainsKey("Dependencies"))
				{
					Dependencies = JsonCompat.ToObject<List<string>>(data["Dependencies"]);
				}
				if (data.ContainsKey("RawOptionalDependencies"))
				{
					RawOptionalDependencies = JsonCompat.ToObject<List<string>>(data["RawOptionalDependencies"]);
				}
				if (data.ContainsKey("OptionalDependencies"))
				{
					OptionalDependencies = JsonCompat.ToObject<List<string>>(data["OptionalDependencies"]);
				}
				return;
			}
			FromCurseForge = data.ContainsKey("gameId");
			if (FromCurseForge)
			{
				Id = data["id"].ToString();
				ProjectId = data["modId"].ToString();
				DisplayName = data["displayName"].ToString().Replace("\t", "").Trim(' ');
				Version = null;
				ReleaseDate = JsonCompat.ToObject<DateTime>(data["fileDate"]);
				Status = (CompFileStatus)JsonCompat.ToObject<int>(data["releaseType"]);
				DownloadCount = (int)data["downloadCount"];
				FileName = (string?)data["fileName"];
				Hash = (string?)((JsonArray)data["hashes"]).ToList().FirstOrDefault((JsonNode s) => JsonCompat.ToObject<int>(s["algo"]) == 1)?["value"];
				if (Hash == null)
				{
					Hash = (string?)((JsonArray)data["hashes"]).ToList().FirstOrDefault((JsonNode s) => JsonCompat.ToObject<int>(s["algo"]) == 2)?["value"];
				}
				string text = data["downloadUrl"]?.ToString() ?? "";
				if (string.IsNullOrWhiteSpace(text))
				{
					text = $"https://edge.forgecdn.net/files/{int.Parse(Id.Substring(0, 4))}/{int.Parse(Id.Substring(4))}/{FileName}";
				}
				text = text.Replace(FileName, WebUtility.UrlEncode(FileName));
				text = text.Replace("+", "%20");
				DownloadUrls = ModDownload.DlSourceModDownloadGet(HandleCurseForgeDownloadUrls(text));
				if (data.ContainsKey("dependencies"))
				{
					RawDependencies = (from d in data["dependencies"].AsArray()
						where JsonCompat.ToObject<int>(d["relationType"]) == 3 && JsonCompat.ToObject<int>(d["modId"]) != 306612 && JsonCompat.ToObject<int>(d["modId"]) != 634179
						select d["modId"].ToString()).ToList();
					RawOptionalDependencies = (from d in data["dependencies"].AsArray()
						where JsonCompat.ToObject<int>(d["relationType"]) == 2 && JsonCompat.ToObject<int>(d["modId"]) != 306612 && JsonCompat.ToObject<int>(d["modId"]) != 634179
						select d["modId"].ToString()).ToList();
				}
				RawGameVersions = (from t in data["gameVersions"].AsArray()
					select t.ToString().Trim().ToLower()).ToList();
				GameVersions = (from v in RawGameVersions
					where McInstanceInfo.IsFormatFit(v)
					select v.Replace("-snapshot", Lang.Text("Download.Comp.Detail.CompItem.PreviewSuffix"))).Distinct().ToList();
				if (GameVersions.Count > 1)
				{
					GameVersions = ModBase.Sort(GameVersions, McVersionComparer.CompareVersionGe).ToList();
					if (Type == CompType.ModPack)
					{
						GameVersions = new List<string> { GameVersions[0] };
					}
				}
				else if (GameVersions.Count == 1)
				{
					GameVersions = GameVersions.ToList();
				}
				else
				{
					GameVersions = new List<string> { Lang.Text("Download.Comp.Detail.CompItem.UnknownVersion") };
				}
				ModLoaders = new List<CompLoaderType>();
				if (RawGameVersions.Contains("forge"))
				{
					ModLoaders.Add(CompLoaderType.Forge);
				}
				if (RawGameVersions.Contains("fabric"))
				{
					ModLoaders.Add(CompLoaderType.Fabric);
				}
				if (RawGameVersions.Contains("quilt"))
				{
					ModLoaders.Add(CompLoaderType.Quilt);
				}
				if (RawGameVersions.Contains("neoforge"))
				{
					ModLoaders.Add(CompLoaderType.NeoForge);
				}
				return;
			}
			Id = (string?)data["id"];
			ProjectId = (string?)data["project_id"];
			DisplayName = data["name"].ToString().Replace("\t", "").Trim(' ');
			Version = (string?)data["version_number"];
			ReleaseDate = JsonCompat.ToObject<DateTime>(data["date_published"]);
			Status = ((data["version_type"].ToString() == "release") ? CompFileStatus.Release : ((data["version_type"].ToString() == "beta") ? CompFileStatus.Beta : CompFileStatus.Alpha));
			DownloadCount = (int)data["downloads"];
			if (((JsonArray)data["files"]).Any())
			{
				JsonNode jsonNode = data["files"][0];
				FileName = (string?)jsonNode["filename"];
				DownloadUrls = ModDownload.DlSourceModDownloadGet(jsonNode["url"].ToString());
				Hash = (string?)jsonNode["hashes"]["sha1"];
			}
			List<string> list = (from v in data["loaders"].AsArray()
				select v.ToString()).ToList();
			ModLoaders = new List<CompLoaderType>();
			if (Type == CompType.Mod)
			{
				if (list.Intersect(new string[5] { "bukkit", "folia", "paper", "purpur", "spigot" }).Any())
				{
					Type = CompType.Plugin;
				}
				if (list.Contains("datapack"))
				{
					Type = CompType.DataPack;
				}
				if (list.Contains("forge"))
				{
					ModLoaders.Add(CompLoaderType.Forge);
					Type = CompType.Mod;
				}
				if (list.Contains("neoforge"))
				{
					ModLoaders.Add(CompLoaderType.NeoForge);
					Type = CompType.Mod;
				}
				if (list.Contains("fabric"))
				{
					ModLoaders.Add(CompLoaderType.Fabric);
					Type = CompType.Mod;
				}
				if (list.Contains("quilt"))
				{
					ModLoaders.Add(CompLoaderType.Quilt);
					Type = CompType.Mod;
				}
			}
			else if (Type == CompType.DataPack)
			{
				if (list.Intersect(new string[5] { "bukkit", "folia", "paper", "purpur", "spigot" }).Any())
				{
					Type = CompType.Plugin;
				}
				if (list.Contains("forge"))
				{
					ModLoaders.Add(CompLoaderType.Forge);
					Type = CompType.Mod;
				}
				if (list.Contains("neoforge"))
				{
					ModLoaders.Add(CompLoaderType.NeoForge);
					Type = CompType.Mod;
				}
				if (list.Contains("fabric"))
				{
					ModLoaders.Add(CompLoaderType.Fabric);
					Type = CompType.Mod;
				}
				if (list.Contains("quilt"))
				{
					ModLoaders.Add(CompLoaderType.Quilt);
					Type = CompType.Mod;
				}
				if (list.Contains("datapack"))
				{
					Type = CompType.DataPack;
				}
			}
			if (data.ContainsKey("dependencies"))
			{
				RawDependencies = (from d in data["dependencies"].AsArray()
					where (string?)d["dependency_type"] == "required" && d["project_id"] != null && (string?)d["project_id"] != "P7dR8mSH" && (string?)d["project_id"] != "qvIfYCYJ" && d["project_id"] != null
					select d["project_id"].ToString()).ToList();
				RawOptionalDependencies = (from d in data["dependencies"].AsArray()
					where (string?)d["dependency_type"] == "optional" && d["project_id"] != null && (string?)d["project_id"] != "P7dR8mSH" && (string?)d["project_id"] != "qvIfYCYJ" && d["project_id"] != null
					select d["project_id"].ToString()).ToList();
			}
			RawGameVersions = (from t in data["game_versions"].AsArray()
				select t.ToString().Trim().ToLower()).ToList();
			GameVersions = (from v in RawGameVersions
				where v.Contains(".")
				select (!v.Contains("-")) ? ((!v.StartsWithF("b1.")) ? v : Lang.Text("Download.Comp.Detail.CompItem.AncientVersion")) : (v.BeforeFirst("-") + Lang.Text("Download.Comp.Detail.CompItem.PreviewSuffix"))).Distinct().ToList();
			if (GameVersions.Count > 1)
			{
				GameVersions = ModBase.Sort(GameVersions, McVersionComparer.CompareVersionGe).ToList();
				if (Type == CompType.ModPack)
				{
					GameVersions = new List<string> { GameVersions[0] };
				}
			}
			else
			{
				if (GameVersions.Count == 1)
				{
					return;
				}
				if (RawGameVersions.Any((string v) => v.RegexCheck("[0-9]{2}w[0-9]{2}[a-z]")))
				{
					GameVersions = RawGameVersions.Where((string v) => v.RegexCheck("[0-9]{2}w[0-9]{2}[a-z]")).ToList();
				}
				else
				{
					GameVersions = new List<string> { Lang.Text("Download.Comp.Detail.CompItem.UnknownVersion") };
				}
			}
		}

		public DownloadFile ToNetFile(string localAddress, DownloadReason reason = DownloadReason.Standalone)
		{
			return ToNetFile(localAddress, reason, RawGameVersions.FirstOrDefault(), ModLoaders.FirstOrDefault());
		}

		public DownloadFile ToNetFile(string localAddress, DownloadReason reason, string? version, CompLoaderType modLoader = CompLoaderType.Any)
		{
			if (DownloadUrls == null)
			{
				throw new InvalidCastException("DownloadUrls 为空");
			}
			return new DownloadFile(HandleModrinthDownloadUrls(DownloadUrls, reason, version, modLoader), localAddress + (localAddress.EndsWithF("\\") ? CompFileNameSanitize(FileName) : ""), new ModBase.FileChecker(-1L, -1L, Hash), useBrowserUserAgent: true);
		}

		public static string HandleCurseForgeDownloadUrls(string url)
		{
			return url.Replace("-service.overwolf.wtf", ".forgecdn.net").Replace("://media.", "://edge.").Replace("://mediafilez.", "://edge.");
		}

		public static IEnumerable<string> HandleModrinthDownloadUrls(IEnumerable<string> urls, DownloadReason reason = DownloadReason.Standalone, string? version = null, CompLoaderType modLoader = CompLoaderType.Any)
		{
			foreach (string url in urls)
			{
				if (!url.Contains("modrinth", StringComparison.InvariantCultureIgnoreCase))
				{
					yield return url;
					continue;
				}
				StringBuilder stringBuilder = new StringBuilder(url);
				stringBuilder.Append(url.Contains('?') ? "&mr_download_reason=" : "?mr_download_reason=");
				stringBuilder.Append(reason.ToString().ToLowerInvariant());
				if (version != null)
				{
					stringBuilder.Append("&mr_game_version=");
					stringBuilder.Append(version);
				}
				if (modLoader != CompLoaderType.Any)
				{
					stringBuilder.Append("&mr_loader=");
					stringBuilder.Append(modLoader.ToString().ToLowerInvariant());
				}
				yield return stringBuilder.ToString();
			}
		}

		public JsonObject ToJson()
		{
			JsonObject jsonObject = new JsonObject();
			jsonObject.Add("FromCurseForge", (JsonNode)FromCurseForge);
			jsonObject.Add("Id", Id);
			if (Version != null)
			{
				jsonObject.Add("Version", Version);
			}
			jsonObject.Add("DisplayName", DisplayName);
			jsonObject.Add("ReleaseDate", (JsonNode)ReleaseDate);
			jsonObject.Add("DownloadCount", (JsonNode)DownloadCount);
			jsonObject.Add("ModLoaders", new JsonArray(((IEnumerable<CompLoaderType>)ModLoaders).Select((Func<CompLoaderType, JsonNode>)((CompLoaderType m) => (int)m)).ToArray()));
			jsonObject.Add("RawGameVersions", new JsonArray(((IEnumerable<string>)RawGameVersions).Select((Func<string, JsonNode>)((string s) => s)).ToArray()));
			jsonObject.Add("GameVersions", new JsonArray(((IEnumerable<string>)GameVersions).Select((Func<string, JsonNode>)((string s) => s)).ToArray()));
			jsonObject.Add("Status", (JsonNode)(int)Status);
			if (FileName != null)
			{
				jsonObject.Add("FileName", FileName);
			}
			if (DownloadUrls != null)
			{
				jsonObject.Add("DownloadUrls", new JsonArray(((IEnumerable<string>)DownloadUrls).Select((Func<string, JsonNode>)((string s) => s)).ToArray()));
			}
			if (Hash != null)
			{
				jsonObject.Add("Hash", Hash);
			}
			jsonObject.Add("RawDependencies", new JsonArray(((IEnumerable<string>)RawDependencies).Select((Func<string, JsonNode>)((string s) => s)).ToArray()));
			jsonObject.Add("RawOptionalDependencies", new JsonArray(((IEnumerable<string>)RawOptionalDependencies).Select((Func<string, JsonNode>)((string s) => s)).ToArray()));
			jsonObject.Add("Dependencies", new JsonArray(((IEnumerable<string>)Dependencies).Select((Func<string, JsonNode>)((string s) => s)).ToArray()));
			jsonObject.Add("OptionalDependencies", new JsonArray(((IEnumerable<string>)OptionalDependencies).Select((Func<string, JsonNode>)((string s) => s)).ToArray()));
			return jsonObject;
		}

		public MyVirtualizingElement<MyListItem> ToListItem(MyListItem.ClickEventHandler onClick, MyIconButton.ClickEventHandler? onSaveClick = null, bool badDisplayName = false)
		{
			return new MyVirtualizingElement<MyListItem>(() =>
			{
				string text = (badDisplayName ? FileName : DisplayName);
				List<string> list = new List<string>();
				if (text != FileName.BeforeLast("."))
				{
					list.Add(FileName.BeforeLast("."));
				}
				if (Dependencies.Any())
				{
					list.Add(Lang.Text("Download.Comp.Detail.FileList.DependencyCount", new object[1] { Enumerable.Count(Dependencies) }));
				}
				string[] snapshotKeywords = new string[6] { "w", "snapshot", "rc", "pre", "experimental", "-" };
				if (GameVersions.All((string ver) => !ver.Contains('.') || snapshotKeywords.Any((string s) => ver.ContainsF(s, ignoreCase: true))))
				{
					list.Add(Lang.Text("Download.Comp.Detail.FileList.GameVersion", new object[1] { string.Join("、", GameVersions) }));
				}
				if (DownloadCount > 0)
				{
					list.Add(Lang.Text("Common.Format.DownloadCount", new object[1] { Lang.CompactNumber((long)DownloadCount) }));
				}
				list.Add(Lang.Text("Download.Comp.Detail.FileList.Updated", new object[1] { Lang.TimeSpan(ReleaseDate - DateTime.Now, 2, true, (TimeUnit)7, (TimeUnit)3) }));
				if (Status != CompFileStatus.Release)
				{
					list.Add(StatusDescription);
				}
				MyListItem myListItem = new MyListItem
				{
					Title = text,
					SnapsToDevicePixels = true,
					Height = 42.0,
					Type = MyListItem.CheckType.Clickable,
					Tag = this,
					Info = string.Join("  |  ", list)
				};
				MyListItem myListItem2 = myListItem;
				myListItem2.Logo = Status switch
				{
					CompFileStatus.Release => ModBase.pathImage + "Icons/R.png", 
					CompFileStatus.Beta => ModBase.pathImage + "Icons/B.png", 
					_ => ModBase.pathImage + "Icons/A.png", 
				};
				MyListItem myListItem3 = myListItem;
				myListItem3.Click += onClick;
				if (onSaveClick != null)
				{
					MyIconButton myIconButton = new MyIconButton
					{
						SvgIcon = "lucide/save",
						ToolTip = Lang.Text("Download.Version.SaveAs")
					};
					ToolTipService.SetPlacement((DependencyObject)(object)myIconButton, PlacementMode.Center);
					ToolTipService.SetVerticalOffset((DependencyObject)(object)myIconButton, 30.0);
					ToolTipService.SetHorizontalOffset((DependencyObject)(object)myIconButton, 2.0);
					myIconButton.Click += onSaveClick;
					myListItem3.Buttons = new MyIconButton[1] { myIconButton };
				}
				return myListItem3;
			})
			{
				Height = 42.0
			};
		}

		public override string ToString()
		{
			return Id + ": " + FileName;
		}
	}

	private static readonly Lazy<string> _dbInitializer = new Lazy<string>(InitializeModDbAndGetConnectionString);

	private const int compPageSize = 40;

	public static ConcurrentDictionary<string, CompProject> compProjectCache = new ConcurrentDictionary<string, CompProject>();

	private static readonly Dictionary<string, string> curseForgeCategoryClassIds = new Dictionary<string, string>
	{
		{ "mc-mods", "6" },
		{ "modpacks", "4471" },
		{ "texture-packs", "12" },
		{ "shaders", "6552" }
	};

	public static ConcurrentDictionary<string, List<CompFile>> compFilesCache = new ConcurrentDictionary<string, List<CompFile>>();

	private static string CompDBConnectionString => _dbInitializer.Value;

	private static SqliteConnection CompDB
	{
		get
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected Obj, but got Unknown
			//IL_0011: Expected Obj, but got Unknown
			SqliteConnection val = new SqliteConnection(CompDBConnectionString);
			((DbConnection)val).Open();
			return val;
		}
	}

	public static string GetCompTypeName(CompType type)
	{
		return Lang.Text(type switch
		{
			CompType.Mod => "Download.Comp.Type.Mod", 
			CompType.ModPack => "Download.Comp.Type.Modpack", 
			CompType.ResourcePack => "Download.Comp.Type.ResourcePack", 
			CompType.Shader => "Download.Comp.Type.Shader", 
			CompType.DataPack => "Download.Comp.Type.DataPack", 
			CompType.Plugin => "Download.Comp.Type.Plugin", 
			CompType.World => "Download.Comp.Type.World", 
			CompType.Schematic => "Download.Comp.Type.Schematic", 
			_ => "Download.Comp.Type.Unknown", 
		});
	}

	public static string GetCompLoadingName(CompType type)
	{
		return Lang.Text(type switch
		{
			CompType.Mod => "Download.Comp.List.Loading.Mod", 
			CompType.ModPack => "Download.Comp.List.Loading.Modpack", 
			CompType.ResourcePack => "Download.Comp.List.Loading.ResourcePack", 
			CompType.Shader => "Download.Comp.List.Loading.Shader", 
			CompType.DataPack => "Download.Comp.List.Loading.DataPack", 
			CompType.Plugin => "Download.Comp.List.Loading.Plugin", 
			CompType.World => "Download.Comp.List.Loading.World", 
			CompType.Schematic => "Download.Comp.List.Loading.Schematic", 
			_ => "Download.Comp.List.Loading.Unknown", 
		});
	}

	public static string GetCompSearchName(CompType type)
	{
		return Lang.Text(type switch
		{
			CompType.Mod => "Download.Comp.List.Search.Mod", 
			CompType.ModPack => "Download.Comp.List.Search.Modpack", 
			CompType.ResourcePack => "Download.Comp.List.Search.ResourcePack", 
			CompType.Shader => "Download.Comp.List.Search.Shader", 
			CompType.DataPack => "Download.Comp.List.Search.DataPack", 
			CompType.Plugin => "Download.Comp.List.Search.Plugin", 
			CompType.World => "Download.Comp.List.Search.World", 
			CompType.Schematic => "Download.Comp.List.Search.Schematic", 
			_ => "Download.Comp.List.Search.Unknown", 
		});
	}

	private static string InitializeModDbAndGetConnectionString()
	{
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected Obj, but got Unknown
		ModBase.Log("[DB] 解压 ModData (SQLite) 中");
		using Stream stream = ModBase.GetResourceStream("Resources/mcmod.buf");
		using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
		using MemoryStream memoryStream = new MemoryStream();
		gZipStream.CopyTo(memoryStream);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		object hexString = ModBase.GetHexString(SHA1Provider.Instance.ComputeHash((Stream)memoryStream));
		string text = Path.Combine(ModBase.pathTemp, "Cache");
		string text2 = Path.Combine(text, $"ModData{hexString}.sqlite");
		if (File.Exists(text2) && !IsDatabaseValid(text2))
		{
			File.Delete(text2);
		}
		if (!File.Exists(text2))
		{
			memoryStream.Seek(0L, SeekOrigin.Begin);
			List<CompDatabaseEntry> list = Serializer.Deserialize<List<CompDatabaseEntry>>((Stream)memoryStream);
			Directory.CreateDirectory(text);
			string text3 = text2 + ".tmp";
			if (File.Exists(text3))
			{
				File.Delete(text3);
			}
			SqliteConnection val = new SqliteConnection("Data Source=\"" + text3 + "\";Pooling=False");
			try
			{
				((DbConnection)(object)val).Open();
				SqliteTransaction val2 = val.BeginTransaction();
				try
				{
					SqlMapper.Execute((IDbConnection)val, "\n                                    CREATE TABLE ModTranslation (\n                                        WikiId INTEGER,\n                                        ChineseName TEXT,\n                                        CurseForgeSlug TEXT,\n                                        ModrinthSlug TEXT\n                                    );\n                                    CREATE INDEX idx_curseforge ON ModTranslation (CurseForgeSlug);\n                                    CREATE INDEX idx_modrinth ON ModTranslation (ModrinthSlug);\n                                    CREATE INDEX idx_chinesename ON ModTranslation (ChineseName);\n                                ", (object)null, (IDbTransaction)null, (int?)null, (CommandType?)null);
					string text4 = "INSERT INTO ModTranslation (WikiId, ChineseName, CurseForgeSlug, ModrinthSlug) \n                                    VALUES (@WikiId, @ChineseName, @CurseForgeSlug, @ModrinthSlug)";
					foreach (CompDatabaseEntry item in list)
					{
						SqlMapper.Execute((IDbConnection)val, text4, (object)item, (IDbTransaction)val2, (int?)null, (CommandType?)null);
					}
					((DbTransaction)(object)val2).Commit();
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			File.Move(text3, text2, overwrite: true);
		}
		return "Data Source=\"" + text2 + "\"";
	}

	private static bool IsDatabaseValid(string dbPath)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected Obj, but got Unknown
		try
		{
			SqliteConnection val = new SqliteConnection("Data Source=\"" + dbPath + "\";Pooling=False;Mode=ReadOnly");
			try
			{
				((DbConnection)(object)val).Open();
				if (SqlMapper.ExecuteScalar<int>((IDbConnection)val, "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='ModTranslation'", (object)null, (IDbTransaction)null, (int?)null, (CommandType?)null) == 0)
				{
					return false;
				}
				return SqlMapper.ExecuteScalar<int>((IDbConnection)val, "SELECT COUNT(*) FROM ModTranslation", (object)null, (IDbTransaction)null, (int?)null, (CommandType?)null) > 0;
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "检查模组翻译数据库有效性失败");
			return false;
		}
	}

	private static CompDatabaseEntry GetCompWikiEntryBySlug(string slug)
	{
		try
		{
			SqliteConnection compDB = CompDB;
			try
			{
				return SqlMapper.QueryFirstOrDefault<CompDatabaseEntry>((IDbConnection)compDB, "SELECT * FROM ModTranslation WHERE CurseForgeSlug = @s OR ModrinthSlug = @s LIMIT 1", (object)new
				{
					s = slug
				}, (IDbTransaction)null, (int?)null, (CommandType?)null);
			}
			finally
			{
				((IDisposable)compDB)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取模组翻译信息失败", ModBase.LogLevel.Hint, null, Lang.Text("Minecraft.Comp.Error.OperationFailed"));
			return null;
		}
	}

	private static bool TryParseResourceLink(string token, out ResourceSite site, out string category, out string slug)
	{
		site = ResourceSite.None;
		category = string.Empty;
		slug = string.Empty;
		if (!Uri.TryCreate(token, UriKind.Absolute, out Uri result) && !Uri.TryCreate("https://" + token, UriKind.Absolute, out result))
		{
			return false;
		}
		if (result.Scheme != Uri.UriSchemeHttp && result.Scheme != Uri.UriSchemeHttps)
		{
			return false;
		}
		string[] array = result.AbsolutePath.Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		if (IsHostOf(result.Host, "curseforge.com"))
		{
			if (array.Length < 3 || !array[0].Equals("minecraft", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			site = ResourceSite.CurseForge;
			category = array[1];
			slug = array[2];
			return true;
		}
		if (IsHostOf(result.Host, "modrinth.com"))
		{
			if (array.Length < 2)
			{
				return false;
			}
			site = ResourceSite.Modrinth;
			category = array[0];
			slug = array[1];
			return true;
		}
		return false;
	}

	private static bool IsHostOf(string host, string domain)
	{
		if (!host.Equals(domain, StringComparison.OrdinalIgnoreCase))
		{
			return host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string? FindFirstResourceLinkToken(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		string[] array = text.Split(new char[4] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			if (TryParseResourceLink(text2, out ResourceSite _, out string _, out string _))
			{
				return text2;
			}
		}
		return null;
	}

	public static string? ResolveLinkToProjectId(string url)
	{
		if (!TryParseResourceLink(url, out ResourceSite site, out string category, out string slug))
		{
			string text = FindFirstResourceLinkToken(url);
			if (text == null || !TryParseResourceLink(text, out site, out category, out slug))
			{
				return null;
			}
		}
		if (site == ResourceSite.CurseForge)
		{
			string text2 = WebUtility.UrlEncode(slug);
			JsonArray jsonArray = (JsonArray)ModDownload.DlModRequest<JsonObject>("https://api.curseforge.com/v1/mods/search?gameId=432&slug=" + text2)["data"];
			if (!jsonArray.Any())
			{
				return null;
			}
			string text3 = ((JsonObject)jsonArray[0])["classId"]?.ToString();
			if (!curseForgeCategoryClassIds.TryGetValue(category, out string value) || text3 == value)
			{
				return jsonArray[0]["id"]?.ToString();
			}
			JsonArray jsonArray2 = (JsonArray)ModDownload.DlModRequest<JsonObject>("https://api.curseforge.com/v1/mods/search?gameId=432&slug=" + text2 + "&classId=" + value)["data"];
			if (!jsonArray2.Any())
			{
				return null;
			}
			return jsonArray2[0]["id"]?.ToString();
		}
		return ModDownload.DlModRequest<JsonObject>("https://api.modrinth.com/v2/project/" + Uri.EscapeDataString(slug))["id"]?.ToString();
	}

	public static string? TryExtractSingleResourceLink(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		string[] array = text.Split(new char[4] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		string text2 = null;
		string[] array2 = array;
		foreach (string text3 in array2)
		{
			if (TryParseResourceLink(text3, out ResourceSite _, out string _, out string _))
			{
				if (text2 != null)
				{
					return null;
				}
				text2 = text3;
			}
		}
		return text2;
	}

	public static void CompProjectsGet(ModLoader.LoaderTask<CompProjectRequest, int> task)
	{
		CompProjectRequest request = task.input;
		CompProjectStorage storage = request.storage;
		string text = TryExtractSingleResourceLink(request.searchText);
		if (text != null)
		{
			if (!storage.results.Any())
			{
				CompProject compProject;
				try
				{
					string text2 = ResolveLinkToProjectId(text);
					compProject = (string.IsNullOrEmpty(text2) ? null : CompRequest.GetCompProjectsByIds(new List<string> { text2 }).FirstOrDefault());
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "[Comp] 解析资源链接失败");
					throw new Exception(Lang.Text("Download.Comp.Link.ResolveFailed"));
				}
				if ((object)compProject == null)
				{
					throw new Exception(Lang.Text("Download.Comp.Link.ResolveFailed"));
				}
				if (request.type != CompType.Any && compProject.Type != request.type)
				{
					throw new Exception(Lang.Text("Download.Comp.List.NoMatchingResults"));
				}
				storage.results.Add(compProject);
				storage.curseForgeTotal = 0;
				storage.modrinthTotal = 0;
			}
			return;
		}
		if (storage.results.Count >= request.targetResultCount)
		{
			LogWrapper.Info($"[Comp] 已有 {storage.results.Count} 个结果，多于所需的 {request.targetResultCount} 个结果，结束处理");
			return;
		}
		if (!request.CanContinue)
		{
			if (!storage.results.Any())
			{
				throw new Exception(Lang.Text("Download.Comp.List.NoMatchingResults"));
			}
			LogWrapper.Info($"[Comp] 已有 {storage.results.Count} 个结果，少于所需的 {request.targetResultCount} 个结果，但无法继续获取，结束处理");
			return;
		}
		if (request.modLoader == CompLoaderType.Quilt && McVersionComparer.CompareVersion(request.gameVersion ?? "1.15", "1.14") == -1)
		{
			throw new Exception(Lang.Text("Minecraft.Error.QuiltUnsupported", new object[1] { request.gameVersion }));
		}
		string rawFilter = (request.searchText ?? "").Trim();
		request.searchText = rawFilter;
		rawFilter.ToLower();
		LogWrapper.Info("[Comp] 工程列表搜索原始文本：" + rawFilter);
		bool flag = Lang.IsChineseMainland && RegexPatterns.HasChineseChar.IsMatch(rawFilter) && !string.IsNullOrEmpty(rawFilter);
		bool flag2 = flag;
		if (flag2)
		{
			CompType type = request.type;
			bool flag3 = ((type == CompType.Mod || type == CompType.DataPack) ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			List<ModBase.SearchEntry<CompDatabaseEntry>> list = new List<ModBase.SearchEntry<CompDatabaseEntry>>();
			SqliteConnection compDB = CompDB;
			try
			{
				string text3 = rawFilter.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
				foreach (CompDatabaseEntry item in SqlMapper.Query<CompDatabaseEntry>((IDbConnection)compDB, "SELECT * FROM ModTranslation WHERE ChineseName LIKE @p ESCAPE '\\' OR CurseForgeSlug LIKE @p ESCAPE '\\' OR ModrinthSlug LIKE @p ESCAPE '\\'", (object)new
				{
					p = "%" + text3 + "%"
				}, (IDbTransaction)null, true, (int?)null, (CommandType?)null).ToList())
				{
					if (!item.ChineseName.Contains("动态的树"))
					{
						list.Add(new ModBase.SearchEntry<CompDatabaseEntry>
						{
							item = item,
							searchSource = new List<ModBase.SearchSource>
							{
								new ModBase.SearchSource(item.ChineseName.BeforeFirst(" (").Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries)),
								new ModBase.SearchSource(item.ChineseName.AfterFirst(" (") + item.CurseForgeSlug + item.ModrinthSlug, 0.5)
							}
						});
					}
				}
			}
			finally
			{
				((IDisposable)compDB)?.Dispose();
			}
			List<ModBase.SearchEntry<CompDatabaseEntry>> list2 = ModBase.Search(list, request.searchText, 40, 0.2);
			if (!list2.Any())
			{
				throw new Exception(Lang.Text("Download.Comp.List.NoResults"));
			}
			Dictionary<string, HashSet<int>> dictionary = new Dictionary<string, HashSet<int>>();
			foreach (ModBase.SearchEntry<CompDatabaseEntry> item2 in list2)
			{
				string[] array = ExtractWords(item2);
				foreach (string key in array)
				{
					if (!dictionary.TryGetValue(key, out var value))
					{
						value = (dictionary[key] = new HashSet<int>());
					}
					value.Add(item2.item.WikiId);
				}
			}
			if (dictionary.Count == 0)
			{
				throw new Exception(Lang.Text("Download.Comp.List.NoResults"));
			}
			string normalizedQuery = NormalizeName(rawFilter);
			List<ModBase.SearchEntry<CompDatabaseEntry>> source = list2.Where((ModBase.SearchEntry<CompDatabaseEntry> r) => CanonName(r.item.ChineseName) == normalizedQuery).ToList();
			if (source.Select((ModBase.SearchEntry<CompDatabaseEntry> r) => r.item.WikiId).Distinct().Count() == 1)
			{
				string[] array2 = ExtractWords((from r in source
					orderby r.absoluteRight descending, r.similarity descending, (r.item.CurseForgeSlug ?? r.item.ModrinthSlug ?? r.item.ChineseName).Length
					select r).First());
				request.searchText = (array2.Any() ? string.Join(" ", array2) : string.Join(" ", from w in dictionary.OrderByDescending((KeyValuePair<string, HashSet<int>> w) => w.Value.Count).Take(2)
					select w.Key));
				List<string> list3 = (from r in source
					select r.item.CurseForgeSlug into s
					where s != null
					select s).Distinct().ToList();
				if (list3.Count == 1)
				{
					request.curseForgeAltSearchText = list3[0];
				}
			}
			else
			{
				int maxCount = dictionary.Values.Max((HashSet<int> mods) => mods.Count);
				if (maxCount <= 1)
				{
					ModBase.SearchEntry<CompDatabaseEntry> result = (from r in list2
						orderby r.absoluteRight descending, r.similarity descending, (r.item.CurseForgeSlug ?? r.item.ModrinthSlug ?? r.item.ChineseName).Length
						select r).First();
					request.searchText = string.Join(" ", ExtractWords(result));
				}
				else
				{
					List<KeyValuePair<string, HashSet<int>>> list4 = (from w in dictionary
						where w.Value.Count == maxCount
						orderby w.Key.Length
						select w).ToList();
					HashSet<int> anchorMods = list4[0].Value;
					request.searchText = string.Join(" ", from w in list4.Where((KeyValuePair<string, HashSet<int>> w) => w.Value.Overlaps(anchorMods)).Take(3)
						select w.Key);
				}
			}
			LogWrapper.Debug("[Comp] 中文搜索基础关键词：" + request.searchText);
		}
		if (request.curseForgeAltSearchText != null)
		{
			processKeywords(ref request.curseForgeAltSearchText);
			LogWrapper.Debug("[Comp] 工程列表搜索最终文本（CurseForge）：" + request.curseForgeAltSearchText);
		}
		processKeywords(ref request.searchText);
		LogWrapper.Debug("[Comp] 工程列表搜索最终文本：" + request.searchText);
		task.Progress = 0.1;
		List<CompProject> realResults = new List<CompProject>();
		while (true)
		{
			List<CompProject> rawResults = new List<CompProject>();
			Exception lastError = null;
			object resultsLock = new object();
			bool flag4 = request.modLoader == CompLoaderType.Forge && McInstanceInfo.VersionToDrop(request.gameVersion, allowSnapshot: true) < 140;
			if (flag4)
			{
				request.modLoader = CompLoaderType.Any;
			}
			string curseForgeUrl = request.GetCurseForgeAddress();
			string modrinthUrl = request.GetModrinthAddress();
			if (flag4)
			{
				request.modLoader = CompLoaderType.Forge;
			}
			List<Task> list5 = new List<Task>();
			if (curseForgeUrl != null)
			{
				list5.Add(Task.Run(() =>
				{
					try
					{
						LogWrapper.Info("[Comp] 开始从 CurseForge 获取列表：" + curseForgeUrl);
						JsonObject jsonObject = ModDownload.DlModRequest<JsonObject>(curseForgeUrl);
						List<CompProject> list9 = (from j in jsonObject["data"].AsArray()
							select new CompProject((JsonObject)j) into p
							where request.type != CompType.ResourcePack || !p.Tags.Contains(Lang.Text("Download.Comp.Type.DataPack"))
							select p).ToList();
						lock (resultsLock)
						{
							rawResults.AddRange(list9);
						}
						storage.curseForgeOffset += list9.Count;
						storage.curseForgeTotal = JsonCompat.ToObject<int>(jsonObject["pagination"]["totalCount"]);
					}
					catch (Exception ex2)
					{
						LogWrapper.Error(lastError = ex2, "CurseForge 获取失败");
					}
				}));
			}
			if (modrinthUrl != null)
			{
				list5.Add(Task.Run(() =>
				{
					try
					{
						LogWrapper.Info("[Comp] 开始从 Modrinth 获取列表：" + modrinthUrl);
						JsonObject jsonObject = ModDownload.DlModRequest<JsonObject>(modrinthUrl);
						List<CompProject> list9 = (from j in jsonObject["hits"].AsArray()
							select new CompProject((JsonObject)j)).ToList();
						lock (resultsLock)
						{
							rawResults.AddRange(list9);
						}
						storage.modrinthOffset += list9.Count;
						storage.modrinthTotal = JsonCompat.ToObject<int>(jsonObject["total_hits"]);
					}
					catch (Exception ex2)
					{
						LogWrapper.Error(lastError = ex2, "Modrinth 获取失败");
					}
				}));
			}
			Task.WaitAll(list5.ToArray());
			task.Progress += 0.4;
			if (task.IsAborted)
			{
				return;
			}
			if (flag4)
			{
				rawResults = rawResults.Where((CompProject p) => !p.ModLoaders.Any() || p.ModLoaders.Contains(CompLoaderType.Forge)).ToList();
			}
			if (!rawResults.Any())
			{
				if (lastError != null)
				{
					throw lastError;
				}
				throw new Exception(Lang.Text("Download.Comp.List.NoResultsSimple"));
			}
			List<CompProject> list6 = (from r in rawResults
				orderby r.FromCurseForge
				where !realResults.Any((CompProject b) => r.IsLike(b)) && !storage.results.Any((CompProject b) => r.IsLike(b))
				select r).ToList();
			realResults.AddRange(list6);
			LogWrapper.Info($"[Comp] 去重、筛选后累计新增结果 {list6.Count} 个（目前已有结果 {storage.results.Count} 个）");
			if (realResults.Count + storage.results.Count >= request.targetResultCount || !request.CanContinue || lastError != null)
			{
				break;
			}
			LogWrapper.Info("[Comp] 数量不足，继续加载下一页");
		}
		if (request.sort != CompSortType.Default)
		{
			if (task.IsAborted)
			{
				throw new ThreadInterruptedException();
			}
			storage.results.AddRange(realResults);
			return;
		}
		Dictionary<CompProject, double> dictionary2 = new Dictionary<CompProject, double>();
		Func<CompProject, double> func = (CompProject p) =>
		{
			switch (request.type)
			{
			case CompType.Mod:
			case CompType.ModPack:
				return p.FromCurseForge ? 1 : 7;
			case CompType.DataPack:
				return (!p.FromCurseForge) ? 1 : 10;
			case CompType.ResourcePack:
			case CompType.Shader:
				return p.FromCurseForge ? 1 : 5;
			default:
				return 1.0;
			}
		};
		if (string.IsNullOrEmpty(rawFilter))
		{
			foreach (CompProject item3 in realResults)
			{
				dictionary2.Add(item3, (double)item3.DownloadCount * func(item3));
			}
		}
		else
		{
			List<ModBase.SearchEntry<CompProject>> list7 = new List<ModBase.SearchEntry<CompProject>>();
			foreach (CompProject item4 in realResults)
			{
				dictionary2.Add(item4, ((Lang.IsChineseMainland && item4.WikiId > 0) ? 0.2 : 0.0) + Math.Log10((double)Math.Max(item4.DownloadCount, 1) * func(item4)) / 9.0);
				list7.Add(new ModBase.SearchEntry<CompProject>
				{
					item = item4,
					searchSource = new List<ModBase.SearchSource>
					{
						new ModBase.SearchSource((flag ? item4.TranslatedName : item4.RawName).Split(new char[1] { '/' }, StringSplitOptions.RemoveEmptyEntries)),
						new ModBase.SearchSource(item4.Description, 0.05)
					}
				});
			}
			List<ModBase.SearchEntry<CompProject>> list8 = ModBase.Search(list7, rawFilter, 101, -1.0);
			foreach (ModBase.SearchEntry<CompProject> item5 in list8)
			{
				dictionary2[item5.item] += (item5.absoluteRight ? 10.0 : item5.similarity) / (list8.First().absoluteRight ? 10.0 : list8.First().similarity);
			}
		}
		if (task.IsAborted)
		{
			throw new ThreadInterruptedException();
		}
		storage.results.AddRange(from s in dictionary2
			orderby s.Value descending
			select s.Key);
		static string CanonName(string name)
		{
			return NormalizeName(name.BeforeFirst(" ("));
		}
		static string[] ExtractWords(ModBase.SearchEntry<CompDatabaseEntry> searchEntry)
		{
			string text4 = "";
			if (searchEntry.item.CurseForgeSlug != null)
			{
				text4 = text4 + searchEntry.item.CurseForgeSlug.Replace("-", " ").Replace("/", " ") + " ";
			}
			if (searchEntry.item.ModrinthSlug != null)
			{
				text4 = text4 + searchEntry.item.ModrinthSlug.Replace("-", " ").Replace("/", " ") + " ";
			}
			text4 += searchEntry.item.ChineseName.AfterLast(" (").TrimEnd(new char[2] { ')', ' ' }).BeforeFirst(" - ")
				.Replace(":", "")
				.Replace("(", "")
				.Replace(")", "")
				.ToLower()
				.Replace("/", " ")
				.Replace("-", " ");
			return (from w in text4.ToLower().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
				select w.TrimStart(new char[3] { '{', '[', '(' }).TrimEnd(new char[3] { '}', ']', ')' })).Where((string w) =>
			{
				if (w.Length <= 1)
				{
					return false;
				}
				if (!w.Any(char.IsLetterOrDigit))
				{
					return false;
				}
				if (new ReadOnlySpan<string>(new string[9] { "the", "of", "for", "mod", "and", "forge", "fabric", "quilt", "neoforge" }).Contains(w))
				{
					return false;
				}
				if (ModBase.Val(w) > 0.0)
				{
					return false;
				}
				return (w.Split(' ').Length <= 3 || !w.Contains("ftb")) ? true : false;
			}).Distinct().ToArray();
		}
		static string NormalizeName(string s)
		{
			return new string(s.Where((char c) => !char.IsWhiteSpace(c) && !char.IsSurrogate(c)).ToArray());
		}
		void processKeywords(ref string reference)
		{
			if (reference != null)
			{
				reference = reference.ToLowerInvariant();
				List<string> list9 = new List<string>();
				string[] array3 = reference.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				for (int j = 0; j < array3.Length; j++)
				{
					string text4 = array3[j].Trim(new char[2] { '[', ']' });
					if (!string.IsNullOrEmpty(text4))
					{
						if (new ReadOnlySpan<string>(new string[5] { "forge", "fabric", "for", "mod", "quilt" }).Contains(text4))
						{
							LogWrapper.Debug("[Comp] 已跳过搜索关键词：" + text4);
						}
						else
						{
							list9.Add(text4);
						}
					}
				}
				if (rawFilter.Length > 0 && !list9.Any())
				{
					reference = rawFilter;
				}
				else
				{
					reference = string.Join(" ", list9.Distinct());
				}
				if (rawFilter.Replace(" ", "").ContainsF("optiforge", ignoreCase: true))
				{
					reference = "optiforge";
				}
				if (rawFilter.Replace(" ", "").ContainsF("optifabric", ignoreCase: true))
				{
					reference = "optifabric";
				}
			}
		}
	}

	public static List<CompFile> CompFilesGet(string projectId, bool fromCurseForge)
	{
		CompProject targetProject = null;
		if (!compProjectCache.TryGetValue(projectId, out targetProject))
		{
			string url = (fromCurseForge ? ("https://api.curseforge.com/v1/mods/" + projectId) : ("https://api.modrinth.com/v2/project/" + projectId));
			if (fromCurseForge)
			{
				JsonObject jsonObject = ModDownload.DlModRequest<JsonObject>(url);
				targetProject = new CompProject((JsonObject)jsonObject["data"]);
			}
			else
			{
				targetProject = new CompProject(ModDownload.DlModRequest<JsonObject>(url));
			}
		}
		if (!compFilesCache.ContainsKey(projectId))
		{
			ModBase.Log("[Comp] 开始获取文件列表：" + projectId);
			JsonArray source = ((!fromCurseForge) ? ModDownload.DlModRequest<JsonArray>("https://api.modrinth.com/v2/project/" + projectId + "/version?include_changelog=false") : ((JsonArray)ModDownload.DlModRequest<JsonObject>("https://api.curseforge.com/v1/mods/" + projectId + "/files?pageSize=10000")["data"]));
			compFilesCache[projectId] = (from a in source
				select new CompFile((JsonObject)a, targetProject.Type) into a
				where a.Available
				group a by a.Id into g
				select g.First()).ToList();
		}
		List<CompFile> list = compFilesCache[projectId];
		List<string> source2 = list.SelectMany((CompFile f) => f.RawDependencies.Concat(f.RawOptionalDependencies)).Distinct().ToList();
		List<string> list2 = source2.Where((string id) => !compProjectCache.ContainsKey(id)).ToList();
		if (list2.Any())
		{
			ModBase.Log($"[Comp] {projectId} 需要补全信息的依赖项共 {list2.Count} 个");
			JsonArray jsonArray = ((!fromCurseForge) ? ModDownload.DlModRequest<JsonArray>("https://api.modrinth.com/v2/projects?ids=[\"" + list2.Join("\",\"") + "\"]") : ((JsonArray)ModDownload.DlModRequest<JsonObject>("https://api.curseforge.com/v1/mods", "POST", "{\"modIds\": [" + string.Join(",", list2) + "]}", "application/json")["data"]));
			foreach (JsonObject item in jsonArray)
			{
				new CompProject(item);
			}
		}
		List<CompProject> list3 = (from id in source2
			where compProjectCache.ContainsKey(id) && (id ?? "") != (projectId ?? "")
			select compProjectCache[id]).ToList();
		foreach (CompFile item2 in list)
		{
			foreach (CompProject item3 in list3)
			{
				if (item2.RawDependencies.Contains(item3.Id) && !item2.Dependencies.Contains(item3.Id))
				{
					item2.Dependencies.Add(item3.Id);
				}
				if (item2.RawOptionalDependencies.Contains(item3.Id) && !item2.OptionalDependencies.Contains(item3.Id))
				{
					item2.OptionalDependencies.Add(item3.Id);
				}
			}
		}
		return compFilesCache[projectId];
	}

	public static string CompFileNameGet(CompProject proj, CompFile file)
	{
		string text;
		if ((proj.TranslatedName ?? "") == (proj.RawName ?? ""))
		{
			text = file.FileName;
		}
		else
		{
			string text2 = proj.TranslatedName.BeforeFirst(" (").BeforeFirst(" - ").Replace("\\", "＼")
				.Replace("/", "／")
				.Replace("|", "｜")
				.Replace(":", "：")
				.Replace("<", "＜")
				.Replace(">", "＞")
				.Replace("*", "＊")
				.Replace("?", "？")
				.Replace("\"", "")
				.Replace("： ", "：");
			text = Config.Download.Comp.NameFormatV2 switch
			{
				0 => "【" + text2 + "】" + file.FileName, 
				1 => "[" + text2 + "] " + file.FileName, 
				2 => text2 + "-" + file.FileName, 
				3 => file.FileName + "-" + text2, 
				_ => file.FileName, 
			};
		}
		if (file.Type == CompType.Mod)
		{
			text = text.Replace("~", "-");
		}
		return CompFileNameSanitize(text);
	}

	public static string CompFileNameSanitize(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return "download";
		}
		StringBuilder stringBuilder = new StringBuilder(fileName.Length);
		foreach (char c in fileName)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			stringBuilder2.Append(c switch
			{
				'\\' => '＼', 
				'/' => '／', 
				':' => '：', 
				'*' => '＊', 
				'?' => '？', 
				'"' => '＂', 
				'<' => '＜', 
				'>' => '＞', 
				'|' => '｜', 
				_ => (!char.IsControl(c)) ? c : '_', 
			});
		}
		string text = stringBuilder.ToString().Trim();
		if (((text == null || text.Length != 0) && !(text == ".") && !(text == "..")) || 1 == 0)
		{
			return text;
		}
		return "download";
	}

	public static void QuickDownload(CompProject project)
	{
		ModBase.RunInNewThread(() =>
		{
			try
			{
				HintService.Hint(Lang.Text("Download.Comp.QuickDownload.Hint.Loading"));
				List<CompFile> list = FilterFilesByType((from f in CompFilesGet(project.Id, project.FromCurseForge)
					where f.Available
					select f).ToList(), project.Type);
				if (list.Count == 0)
				{
					HintService.Hint(Lang.Text("Download.Comp.QuickDownload.Hint.NoFile"));
				}
				else
				{
					int num = Config.Download.Comp.QuickDownloadBehavior;
					if (num == 0)
					{
						int? num2 = ModBase.RunInUiWait(() => ModMain.MyMsgBoxSelect(new List<IMyRadio>
						{
							new MyRadioBox
							{
								Text = Lang.Text("Download.Comp.QuickDownload.ChooseMethod.CurrentInstance")
							},
							new MyRadioBox
							{
								Text = Lang.Text("Download.Comp.QuickDownload.ChooseMethod.AskInstance")
							},
							new MyRadioBox
							{
								Text = Lang.Text("Download.Comp.QuickDownload.ChooseMethod.AskPath")
							}
						}, Lang.Text("Download.Comp.QuickDownload.ChooseMethod.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")));
						if (!num2.HasValue)
						{
							return;
						}
						num = num2.Value + 1;
					}
					switch (num)
					{
					case 1:
						_QuickDownloadToInstance(project, list, ModInstanceList.McMcInstanceSelected);
						break;
					case 2:
					{
						List<McInstance> list2 = _QuickDownloadPickInstances(project, list);
						if (list2 != null)
						{
							HashSet<string> startedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
							{
								foreach (McInstance item in list2)
								{
									_QuickDownloadToInstance(project, list, item, startedTargets);
								}
								break;
							}
						}
						break;
					}
					case 3:
						_QuickDownloadToFolder(project, list);
						break;
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "[Comp] 快速下载失败", ModBase.LogLevel.Feedback, null, Lang.Text("Minecraft.Comp.Error.OperationFailed"));
			}
		}, "Comp QuickDownload");
	}

	private static void _QuickDownloadToInstance(CompProject project, List<CompFile> files, McInstance? instance, ISet<string>? startedTargets = null)
	{
		if ((object)instance == null)
		{
			HintService.Hint(Lang.Text("Download.Comp.QuickDownload.Hint.NoInstance"));
			return;
		}
		if (!instance.IsLoaded)
		{
			instance.Load();
		}
		CompFile compFile = _PickLatestFile(files.Where((CompFile f) => IsInstanceSuitableForFile(instance, f, _ResolveLoaders(f, project))).ToList());
		if (compFile == null)
		{
			HintService.Hint(Lang.Text("Download.Comp.QuickDownload.Hint.NoCompatibleFile"));
			return;
		}
		string text = instance.PathIndie + _GetSubFolder(project.Type);
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, CompFileNameGet(project, compFile));
		if (startedTargets == null || startedTargets.Add(text2))
		{
			_StartQuickDownload(compFile, text2);
			HintService.Hint(Lang.Text("Download.Comp.QuickDownload.Hint.DownloadStarted", new object[1] { project.RawName }), HintType.Success);
		}
	}

	private static List<McInstance>? _QuickDownloadPickInstances(CompProject project, List<CompFile> files)
	{
		if (ModInstanceList.mcInstanceListLoader.State != ModBase.LoadState.Finished)
		{
			HintService.Hint(Lang.Text("Download.Comp.QuickDownload.Hint.Loading"));
			ModLoader.LoaderFolderRun(ModInstanceList.mcInstanceListLoader, ModFolder.mcFolderSelected, ModLoader.LoaderFolderRunType.ForceRun, 1, "versions\\", waitForExit: true);
		}
		List<McInstance> compatible = (from v in ModInstanceList.mcInstanceList.Values.SelectMany((List<McInstance> l) => l)
			where (object)v != null && files.Any((CompFile f) => IsInstanceSuitableForFile(v, f, _ResolveLoaders(f, project)))
			select v).ToList();
		if (compatible.Count == 0)
		{
			HintService.Hint(Lang.Text("Download.Comp.QuickDownload.Hint.NoCompatibleInstance"));
			return null;
		}
		McInstance current = ModInstanceList.McMcInstanceSelected;
		if ((object)current != null)
		{
			compatible = (from v in compatible
				orderby (!(v == current)) ? 1 : 0, v.Name
				select v).ToList();
		}
		return ModBase.RunInUiWait(() => ModMain.MyMsgBoxMultiSelect(compatible.Select((McInstance v) => new MyListItem
		{
			Title = v.Name
		}).ToList(), Lang.Text("Download.Comp.QuickDownload.ChooseInstance.Title"), Lang.Text("Common.Action.Continue"), Lang.Text("Common.Action.Cancel")))?.Select((int i) => compatible[i]).ToList();
	}

	private static void _QuickDownloadToFolder(CompProject project, List<CompFile> files)
	{
		CompFile compFile = _PickLatestFile(files);
		if (compFile == null)
		{
			HintService.Hint(Lang.Text("Download.Comp.QuickDownload.Hint.NoFile"));
			return;
		}
		string text = ModBase.RunInUiWait(() => SystemDialogs.SelectFolder(Lang.Text("Download.Comp.QuickDownload.Hint.SelectFolder"), (string)null));
		if (!string.IsNullOrWhiteSpace(text))
		{
			string target = Path.Combine(text, CompFileNameGet(project, compFile));
			_StartQuickDownload(compFile, target);
			HintService.Hint(Lang.Text("Download.Comp.QuickDownload.Hint.DownloadStarted", new object[1] { project.RawName }), HintType.Success);
		}
	}

	private static void _StartQuickDownload(CompFile file, string target)
	{
		string text = file.Type switch
		{
			CompType.Mod => Lang.Text("Download.Comp.Type.Mod"), 
			CompType.ResourcePack => Lang.Text("Download.Comp.Type.ResourcePack"), 
			CompType.Shader => Lang.Text("Download.Comp.Type.Shader"), 
			CompType.DataPack => Lang.Text("Download.Comp.Type.DataPack"), 
			CompType.World => Lang.Text("Download.Comp.Type.World"), 
			_ => Lang.Text("Download.Comp.Type.Mod"), 
		};
		string name = Lang.Text("Download.Comp.Detail.DownloadResource", new object[2]
		{
			text,
			ModBase.GetFileNameWithoutExtentionFromPath(target)
		});
		List<ModLoader.LoaderBase> list = new List<ModLoader.LoaderBase>
		{
			new LoaderDownload(Lang.Text("Download.Comp.Detail.DownloadFile"), new List<DownloadFile> { (file.Type == CompType.Mod) ? file.ToNetFile(target) : file.ToNetFile(target, DownloadReason.Standalone, null) })
			{
				ProgressWeight = 6.0,
				block = true
			}
		};
		if (file.Type == CompType.World)
		{
			string extractDir = Path.GetDirectoryName(target);
			list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Download.Comp.Detail.InstallWorld"), (ModLoader.LoaderTask<int, int> _) =>
			{
				ModBase.ExtractFile(target, extractDir, Encoding.UTF8);
			})
			{
				ProgressWeight = 0.1,
				block = true
			});
			list.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Download.Comp.Detail.CleanCache"), (ModLoader.LoaderTask<int, int> _) =>
			{
				File.Delete(target);
			}));
		}
		ModLoader.LoaderCombo<int> loaderCombo = new ModLoader.LoaderCombo<int>(name, list);
		loaderCombo.OnStateChanged = ModDownloadLib.LoaderStateChangedHintOnly;
		loaderCombo.Start(1);
		ModLoader.LoaderTaskbarAdd(loaderCombo);
		ModMain.frmMain.BtnExtraDownload.ShowRefresh();
		ModMain.frmMain.BtnExtraDownload.Ribble();
	}

	private static string _GetSubFolder(CompType type)
	{
		return type switch
		{
			CompType.Mod => "mods\\", 
			CompType.ResourcePack => "resourcepacks\\", 
			CompType.Shader => "shaderpacks\\", 
			CompType.World => "saves\\", 
			_ => "", 
		};
	}

	private static List<CompLoaderType> _ResolveLoaders(CompFile file, CompProject project)
	{
		if (file.ModLoaders.Count <= 0)
		{
			return project.ModLoaders;
		}
		return file.ModLoaders;
	}

	private static List<CompFile> FilterFilesByType(List<CompFile> files, CompType type)
	{
		if (type == CompType.Shader || type == CompType.ResourcePack)
		{
			return files;
		}
		return files.Where((CompFile f) => f.Type == type).ToList();
	}

	public static bool IsInstanceSuitableForFile(McInstance? version, CompFile file, List<CompLoaderType> allowedLoaders)
	{
		if ((object)version == null)
		{
			return false;
		}
		if (!version.IsLoaded)
		{
			version.Load();
		}
		if ((file.Type == CompType.Mod || file.Type == CompType.DataPack) && file.GameVersions.Any((string v) => v.Contains(".")) && !file.GameVersions.Any((string v) => v.Contains(".") && v == version.Info.VanillaName))
		{
			return false;
		}
		if (allowedLoaders.Count == 0)
		{
			return true;
		}
		if (allowedLoaders.Contains(CompLoaderType.Forge) && version.Info.HasForge)
		{
			return true;
		}
		if (allowedLoaders.Contains(CompLoaderType.Forge) && version.Info.HasCleanroom)
		{
			return true;
		}
		if (allowedLoaders.Contains(CompLoaderType.Fabric) && (version.Info.HasFabric || version.Info.HasLegacyFabric))
		{
			return true;
		}
		if (allowedLoaders.Contains(CompLoaderType.NeoForge) && version.Info.HasNeoForge)
		{
			return true;
		}
		if (allowedLoaders.Contains(CompLoaderType.Quilt) && version.Info.HasQuilt)
		{
			return true;
		}
		if (allowedLoaders.Contains(CompLoaderType.LiteLoader) && version.Info.HasLiteLoader)
		{
			return true;
		}
		return false;
	}

	private static CompFile? _PickLatestFile(List<CompFile> files, Func<CompFile, bool>? compatFilter = null)
	{
		object source;
		if (compatFilter != null)
		{
			source = files.Where(compatFilter);
		}
		else
		{
			source = files;
		}
		List<CompFile> list = ((IEnumerable<CompFile>)source).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		return (from f in list
			orderby f.Status == CompFileStatus.Release descending, f.ReleaseDate descending
			select f).First();
	}

	public static void CompFilesCardPreload(StackPanel stack, List<CompFile> files)
	{
		List<string> list = files.SelectMany((CompFile f) => f.Dependencies).Distinct().ToList();
		List<string> list2 = files.SelectMany((CompFile f) => f.OptionalDependencies).Distinct().ToList();
		if (list.Any() || list2.Any())
		{
			_AddDependencyBar(stack, list, Lang.Text("Download.Comp.Detail.FileList.RequiredDependencies"), collapsed: false);
			_AddDependencyBar(stack, list2, Lang.Text("Download.Comp.Detail.FileList.OptionalDependencies"), collapsed: true);
			stack.Children.Add(new TextBlock
			{
				Text = Lang.Text("Download.Comp.Detail.FileList.VersionList"),
				FontSize = 14.0,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(6.0, 12.0, 0.0, 5.0)
			});
		}
	}

	private static void _AddDependencyBar(StackPanel stack, List<string> depIds, string title, bool collapsed)
	{
		if (depIds == null || !depIds.Any())
		{
			return;
		}
		depIds.Sort();
		List<CompProject> list = new List<CompProject>();
		foreach (string depId in depIds)
		{
			if (compProjectCache.TryGetValue(depId, out CompProject value))
			{
				list.Add(value);
			}
			else
			{
				ModBase.Log("[Comp] 未找到 ID " + depId + " 的前置信息", ModBase.LogLevel.Debug);
			}
		}
		if (!list.Any())
		{
			return;
		}
		MyCollapseBar myCollapseBar = new MyCollapseBar
		{
			Title = $"{title} ({list.Count})",
			IsCollapsed = collapsed
		};
		foreach (CompProject item in list)
		{
			myCollapseBar.ContentPanel.Children.Add(item.ToCompItem(showMcVersionDesc: false, showLoaderDesc: false));
		}
		stack.Children.Add(myCollapseBar);
	}
}
