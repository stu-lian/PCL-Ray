using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Core.Utils.Exts;
using PCL.Core.Utils.Hash;
using fNbt;

namespace PCL;

public static class ModLocalComp
{
	public class LocalCompFile
	{
		public enum LocalFileStatus
		{
			Fine,
			Disabled,
			Unavailable
		}

		public delegate void OnCompUpdateEventHandler(LocalCompFile sender);

		public readonly string path;

		private bool _nbtDataLoaded;

		private string _Name;

		private string _Description;

		private string _Version;

		private string _ModId;

		public List<string> possibleModId = new List<string>();

		private string _Authors;

		private long? _litematicTimeCreated;

		private long? _litematicTimeModified;

		private string _schemOriginalName;

		private string _litematicOriginalName;

		private int? _litematicVersion;

		private string _litematicEnclosingSize;

		private int? _litematicRegionCount;

		private int? _litematicTotalBlocks;

		private int? _litematicTotalVolume;

		private string _structureGameVersion;

		private int? _structureDataVersion;

		private string _structureAuthor;

		private int? _spongeVersion;

		private Dictionary<string, string> _Dependencies = new Dictionary<string, string>();

		private readonly Dictionary<string, string> _DependencyRaw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private readonly List<EmbeddedDependency> _DependencyDeclarations = new List<EmbeddedDependency>();

		private readonly HashSet<string> _ProvidedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, string> _ProvidedVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private readonly List<EmbeddedConflict> _ConflictDeclarations = new List<EmbeddedConflict>();

		private volatile bool isLoaded;

		private Exception _FileUnavailableReason;

		private bool isInfoWithoutClassAvailable;

		private bool isInfoWithClassLoaded;

		private bool isInfoWithClassAvailable;

		private readonly object _loadLock = new object();

		private List<LocalCompFile> _EmbeddedMods = new List<LocalCompFile>();

		private long? _jijExpectedLastModified;

		private long? _jijExpectedSize;

		private volatile bool _jijPending;

		public ModComp.CompFile compFile;

		public List<string> changelogUrls = new List<string>();

		public bool compLoaded;

		private uint? _CurseForgeHash;

		public bool IsFolder => path.EndsWithF("\\__FOLDER__", ignoreCase: true);

		public string ActualPath
		{
			get
			{
				if (IsFolder)
				{
					return path.Replace("\\__FOLDER__", "");
				}
				return path;
			}
		}

		public string RawPath => ModBase.GetPathFromFullPath(path) + RawFileName;

		public string FileName
		{
			get
			{
				if (IsFolder && !string.IsNullOrEmpty(Name))
				{
					return Name;
				}
				return ModBase.GetFileNameFromPath(path);
			}
		}

		public string RawFileName => FileName.Replace(".disabled", "").Replace(".old", "");

		public LocalFileStatus State
		{
			get
			{
				Load();
				if (!IsFileAvailable)
				{
					return LocalFileStatus.Unavailable;
				}
				if (path.EndsWithF(".disabled", ignoreCase: true) || path.EndsWithF(".old", ignoreCase: true))
				{
					return LocalFileStatus.Disabled;
				}
				return LocalFileStatus.Fine;
			}
		}

		public string Name
		{
			get
			{
				if (_Name == null)
				{
					Load();
				}
				if (_Name == null)
				{
					_Name = _ModId;
				}
				if (_Name == null)
				{
					if (IsFolder)
					{
						_Name = ModBase.GetFolderNameFromPath(ActualPath);
					}
					else
					{
						_Name = ModBase.GetFileNameWithoutExtentionFromPath(path);
					}
				}
				return _Name;
			}
			set
			{
				if (_Name == null && value != null && !value.Contains("modname") && value.ToLower() != "name" && value.Length > 1 && (ModBase.Val(value).ToString() ?? "") != (value ?? ""))
				{
					_Name = value;
				}
			}
		}

		public string Description
		{
			get
			{
				if (_Description == null)
				{
					Load();
				}
				if (_Description == null && FileUnavailableReason != null)
				{
					_Description = FileUnavailableReason.Message;
				}
				return _Description;
			}
			set
			{
				if (_Description == null && value != null && value.Length > 2)
				{
					_Description = value.Trim('\n');
					if (_Description.ToLower().LastIndexOfAny("qwertyuiopasdfghjklzxcvbnm0123456789".ToCharArray()) == _Description.Length - 1)
					{
						_Description += ".";
					}
				}
			}
		}

		public List<string> Tags
		{
			get
			{
				if (field == null)
				{
					field = new List<string>();
					if (IsFolder)
					{
						field.Add("文件夹");
					}
					else
					{
						switch (Path.GetExtension(RawPath).ToLower() ?? "")
						{
						case ".litematic":
							field.Add("原理图");
							break;
						case ".schem":
						case ".schematic":
							field.Add("Schematic结构");
							break;
						case ".nbt":
							field.Add("原版结构");
							break;
						}
					}
				}
				return field;
			}
		}

		public string Version
		{
			get
			{
				if (_Version == null)
				{
					Load();
				}
				return _Version;
			}
			set
			{
				if (_Version == null || !_Version.RegexCheck("[0-9.\\-]+"))
				{
					if (value != null && value.ContainsF("version", ignoreCase: true))
					{
						value = "version";
					}
					_Version = value;
				}
			}
		}

		public string ModId
		{
			get
			{
				if (_ModId == null)
				{
					Load();
				}
				return _ModId;
			}
			set
			{
				if (value == null)
				{
					return;
				}
				value = value.RegexSeek(RegexPatterns.ModIdMatch);
				if (value != null && value.Length > 1 && !((ModBase.Val(value).ToString() ?? "") == (value ?? "")) && !value.Equals("name", StringComparison.OrdinalIgnoreCase) && !value.Equals("modid", StringComparison.OrdinalIgnoreCase))
				{
					if (!possibleModId.Contains(value))
					{
						possibleModId.Add(value);
					}
					if (_ModId == null)
					{
						_ModId = value;
					}
				}
			}
		}

		public string Url
		{
			get
			{
				if (field == null)
				{
					Load();
				}
				return field;
			}
			set
			{
				if (field == null && value != null && value.StartsWithF("http"))
				{
					field = value;
				}
			}
		}

		public string Authors
		{
			get
			{
				if (_Authors == null)
				{
					Load();
				}
				return _Authors;
			}
			set
			{
				if (_Authors == null && !string.IsNullOrWhiteSpace(value))
				{
					_Authors = value;
				}
			}
		}

		public long? LitematicTimeCreated
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _litematicTimeCreated;
			}
		}

		public long? LitematicTimeModified
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _litematicTimeModified;
			}
		}

		public string SchemOriginalName
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _schemOriginalName;
			}
		}

		public string LitematicOriginalName
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _litematicOriginalName;
			}
		}

		public int? LitematicVersion
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _litematicVersion;
			}
		}

		public string LitematicEnclosingSize
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _litematicEnclosingSize;
			}
		}

		public int? LitematicRegionCount
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _litematicRegionCount;
			}
		}

		public int? LitematicTotalBlocks
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _litematicTotalBlocks;
			}
		}

		public int? LitematicTotalVolume
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _litematicTotalVolume;
			}
		}

		public string StructureGameVersion
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _structureGameVersion;
			}
		}

		public int? StructureDataVersion
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _structureDataVersion;
			}
		}

		public string StructureAuthor
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _structureAuthor;
			}
		}

		public int? SpongeVersion
		{
			get
			{
				LoadNbtDataIfNeeded();
				return _spongeVersion;
			}
		}

		public string Logo { get; set; }

		public Dictionary<string, string> Dependencies
		{
			get
			{
				Load();
				return _Dependencies;
			}
		}

		public Dictionary<string, string> DependencyRaw
		{
			get
			{
				Load();
				return _DependencyRaw;
			}
		}

		public IReadOnlyList<EmbeddedDependency> DependencyDeclarations
		{
			get
			{
				Load();
				return _DependencyDeclarations;
			}
		}

		public HashSet<string> ProvidedIds
		{
			get
			{
				Load();
				return _ProvidedIds;
			}
		}

		public IReadOnlyDictionary<string, string> ProvidedVersions
		{
			get
			{
				Load();
				return _ProvidedVersions;
			}
		}

		public IReadOnlyList<EmbeddedConflict> ConflictDeclarations
		{
			get
			{
				Load();
				return _ConflictDeclarations;
			}
		}

		public bool IsFileAvailable
		{
			get
			{
				Load();
				return FileUnavailableReason == null;
			}
		}

		public Exception FileUnavailableReason
		{
			get
			{
				Load();
				return _FileUnavailableReason;
			}
		}

		public List<LocalCompFile> EmbeddedMods
		{
			get
			{
				Load();
				return _EmbeddedMods;
			}
			internal set
			{
				_EmbeddedMods = value;
			}
		}

		internal bool JijPending => _jijPending;

		public string JijLoader { get; internal set; }

		public string JijTargetMcVersion { get; internal set; }

		public string JijIdentifier { get; internal set; }

		public string JijVersionRange { get; internal set; }

		public string JijArtifactVersion { get; internal set; }

		internal string? PreferredLoader { get; set; }

		public string DetectedLoader { get; internal set; }

		public ModComp.CompProject Comp
		{
			get
			{
				return field;
			}
			set
			{
				field = value;
				OnCompUpdate?.Invoke(this);
			}
		}

		public ModComp.CompFile UpdateFile
		{
			get
			{
				return field;
			}
			set
			{
				field = value;
				OnCompUpdate?.Invoke(this);
			}
		}

		public bool CanUpdate
		{
			get
			{
				if (!Config.Preference.Hide.FunctionModUpdate)
				{
					return changelogUrls.Any();
				}
				return false;
			}
		}

		public uint CurseForgeHash
		{
			get
			{
				uint? curseForgeHash = _CurseForgeHash;
				if (!curseForgeHash.HasValue)
				{
					byte[] array = StringExtension.HexToBytes(_hashCache.Value.GetMurmurHash2Async(path).GetAwaiter().GetResult());
					_CurseForgeHash = BitConverter.ToUInt32(array);
				}
				return _CurseForgeHash.Value;
			}
		}

		public string ModrinthHash
		{
			get
			{
				if (field == null)
				{
					field = _hashCache.Value.GetSHA1Async(path).GetAwaiter().GetResult();
				}
				return field;
			}
		}

		public event OnCompUpdateEventHandler? OnCompUpdate;

		public bool IsPresetMod()
		{
			if (!Dependencies.Any() && Name != null)
			{
				if (!Name.ToLower().Contains("core"))
				{
					return Name.ToLower().Contains("lib");
				}
				return true;
			}
			return false;
		}

		public static bool IsModFile(string path)
		{
			if (path == null || !path.Contains("."))
			{
				return false;
			}
			path = path.ToLower();
			if (path.EndsWithF(".jar", ignoreCase: true) || path.EndsWithF(".zip", ignoreCase: true) || path.EndsWithF(".litemod", ignoreCase: true) || path.EndsWithF(".jar.disabled", ignoreCase: true) || path.EndsWithF(".zip.disabled", ignoreCase: true) || path.EndsWithF(".litemod.disabled", ignoreCase: true) || path.EndsWithF(".jar.old", ignoreCase: true) || path.EndsWithF(".zip.old", ignoreCase: true) || path.EndsWithF(".litemod.old", ignoreCase: true))
			{
				return true;
			}
			return false;
		}

		public static bool IsCompFile(string path, ModComp.CompType compType)
		{
			if (path == null || !path.Contains("."))
			{
				return false;
			}
			path = path.ToLower();
			switch (compType)
			{
			case ModComp.CompType.Mod:
				return IsModFile(path);
			case ModComp.CompType.ResourcePack:
			case ModComp.CompType.Shader:
				return path.EndsWithF(".zip", ignoreCase: true);
			case ModComp.CompType.DataPack:
				if (!path.EndsWithF(".zip", ignoreCase: true))
				{
					return path.EndsWithF(".zip.disabled", ignoreCase: true);
				}
				return true;
			case ModComp.CompType.Schematic:
				if (!path.EndsWithF(".litematic", ignoreCase: true) && !path.EndsWithF(".nbt", ignoreCase: true) && !path.EndsWithF(".schematic", ignoreCase: true))
				{
					return path.EndsWithF(".schem", ignoreCase: true);
				}
				return true;
			default:
				return false;
			}
		}

		public string GetLogo()
		{
			if ((object)Comp != null && Comp.LogoUrl != null)
			{
				return Comp.LogoUrl;
			}
			if (Logo != null)
			{
				return Logo;
			}
			if (IsFolder)
			{
				return "pack://application:,,,/images/Icons/Folder.png";
			}
			return ModBase.pathImage + "Icons/NoIcon.png";
		}

		private void LoadLitematicNbtData()
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected Obj, but got Unknown
			try
			{
				ModBase.Log("开始读取 Litematic NBT 数据：" + path, ModBase.LogLevel.Debug);
				using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					NbtFile val = new NbtFile();
					val.LoadFromStream((Stream)fileStream, (NbtCompression)0);
					NbtInt val2 = (NbtInt)val.RootTag.Get("Version");
					if (val2 != null)
					{
						_litematicVersion = val2.Value;
					}
					NbtCompound val3 = val.RootTag.Get<NbtCompound>("Metadata");
					if (val3 != null)
					{
						ModBase.Log("找到 Litematic Metadata 节点", ModBase.LogLevel.Debug);
						NbtString val4 = val3.Get<NbtString>("Name");
						if (val4 != null && !string.IsNullOrWhiteSpace(val4.Value) && val4.Value != "Unnamed")
						{
							_litematicOriginalName = val4.Value;
						}
						NbtString val5 = val3.Get<NbtString>("Description");
						if (val5 != null && !string.IsNullOrWhiteSpace(val5.Value))
						{
							_Description = val5.Value;
						}
						NbtString val6 = val3.Get<NbtString>("Author");
						if (val6 != null && !string.IsNullOrWhiteSpace(val6.Value))
						{
							_Authors = val6.Value;
						}
						NbtLong val7 = val3.Get<NbtLong>("TimeCreated");
						if (val7 != null)
						{
							_litematicTimeCreated = val7.Value;
						}
						NbtLong val8 = val3.Get<NbtLong>("TimeModified");
						if (val8 != null)
						{
							_litematicTimeModified = val8.Value;
						}
						NbtCompound val9 = val3.Get<NbtCompound>("EnclosingSize");
						if (val9 != null)
						{
							NbtInt val10 = val9.Get<NbtInt>("x");
							NbtInt val11 = val9.Get<NbtInt>("y");
							NbtInt val12 = val9.Get<NbtInt>("z");
							if (val10 != null && val11 != null && val12 != null)
							{
								_litematicEnclosingSize = $"{val10.Value} × {val11.Value} × {val12.Value}";
							}
						}
						NbtInt val13 = val3.Get<NbtInt>("RegionCount");
						if (val13 != null)
						{
							_litematicRegionCount = val13.Value;
						}
						NbtInt val14 = val3.Get<NbtInt>("TotalBlocks");
						if (val14 != null)
						{
							_litematicTotalBlocks = val14.Value;
						}
						NbtInt val15 = val3.Get<NbtInt>("TotalVolume");
						if (val15 != null)
						{
							_litematicTotalVolume = val15.Value;
						}
					}
					else
					{
						ModBase.Log("未找到 Litematic Metadata 节点", ModBase.LogLevel.Debug);
					}
				}
				ModBase.Log("Litematic NBT 数据读取完成", ModBase.LogLevel.Debug);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "读取 Litematic NBT 数据时出错（" + path + "）");
			}
		}

		private void LoadSchemNbtData()
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				ModBase.Log("开始读取 Schem NBT 数据：" + path, ModBase.LogLevel.Debug);
				using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					NbtFile val = new NbtFile();
					val.LoadFromStream((Stream)fileStream, (NbtCompression)0);
					NbtInt val2 = val.RootTag.Get<NbtInt>("Version");
					if (val2 != null)
					{
						_spongeVersion = val2.Value;
					}
					NbtInt val3 = val.RootTag.Get<NbtInt>("DataVersion");
					if (val3 != null)
					{
						_structureDataVersion = val3.Value;
					}
					NbtShort val4 = val.RootTag.Get<NbtShort>("Width");
					NbtShort val5 = val.RootTag.Get<NbtShort>("Height");
					NbtShort val6 = val.RootTag.Get<NbtShort>("Length");
					if (val4 != null && val5 != null && val6 != null)
					{
						_litematicEnclosingSize = $"{val4.Value} × {val5.Value} × {val6.Value}";
						_litematicTotalVolume = (short)(val4.Value * val5.Value) * val6.Value;
						_litematicTotalBlocks = _litematicTotalVolume;
					}
					if (val.RootTag.Get<NbtCompound>("Palette") != null)
					{
						_litematicRegionCount = 1;
					}
					NbtCompound val7 = val.RootTag.Get<NbtCompound>("Metadata");
					if (val7 != null)
					{
						NbtString val8 = val7.Get<NbtString>("Name");
						if (val8 != null && !string.IsNullOrWhiteSpace(val8.Value))
						{
							_schemOriginalName = val8.Value;
						}
						NbtString val9 = val7.Get<NbtString>("Author");
						if (val9 != null && !string.IsNullOrWhiteSpace(val9.Value))
						{
							_structureAuthor = val9.Value;
							if (_Authors == null)
							{
								_Authors = _structureAuthor;
							}
						}
					}
				}
				ModBase.Log("Schem NBT 数据读取完成", ModBase.LogLevel.Debug);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "读取 Schem NBT 数据时出错（" + path + "）");
			}
		}

		private void LoadSchematicNbtData()
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				ModBase.Log("开始读取 Schematic NBT 数据：" + path, ModBase.LogLevel.Debug);
				using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				NbtFile val = new NbtFile();
				val.LoadFromStream((Stream)fileStream, (NbtCompression)0);
				NbtShort val2 = val.RootTag.Get<NbtShort>("Width");
				NbtShort val3 = val.RootTag.Get<NbtShort>("Height");
				NbtShort val4 = val.RootTag.Get<NbtShort>("Length");
				if (val2 != null && val3 != null && val4 != null)
				{
					_litematicEnclosingSize = $"{val2.Value} × {val3.Value} × {val4.Value}";
					_litematicTotalVolume = (short)(val2.Value * val3.Value) * val4.Value;
				}
				NbtString val5 = val.RootTag.Get<NbtString>("Materials");
				if (val5 != null)
				{
					ModBase.Log("Schematic 材料类型：" + val5.Value, ModBase.LogLevel.Debug);
				}
				ModBase.Log("Schematic NBT 数据读取完成", ModBase.LogLevel.Debug);
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "读取 Schematic NBT 数据时出错（" + path + "）");
			}
		}

		private void LoadStructureNbtData()
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				ModBase.Log("开始读取 NBT 结构文件数据：" + path, ModBase.LogLevel.Debug);
				using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				NbtFile val = new NbtFile();
				val.LoadFromStream((Stream)fileStream, (NbtCompression)0);
				NbtString val2 = val.RootTag.Get<NbtString>("author");
				if (val2 != null && !string.IsNullOrWhiteSpace(val2.Value))
				{
					_structureAuthor = val2.Value;
					if (_Authors == null)
					{
						_Authors = _structureAuthor;
					}
				}
				NbtList val3 = val.RootTag.Get<NbtList>("size");
				if (val3 != null)
				{
					NbtTag[] array = val3.ToArray();
					if (array.Length >= 3)
					{
						int[] array2 = (from e in array.Take(3)
							select e.IntValue).ToArray();
						_litematicEnclosingSize = $"{array2[0]} × {array2[1]} × {array2[2]}";
						_litematicTotalVolume = array2[0] * array2[1] * array2[2];
					}
				}
				NbtList val4 = val.RootTag.Get<NbtList>("blocks");
				if (val4 != null)
				{
					_litematicTotalBlocks = ((IEnumerable<NbtTag>)val4).Where((NbtTag x) =>
					{
						//IL_0001: Unknown result type (might be due to invalid IL or missing references)
						//IL_0008: Invalid comparison between Unknown and I4
						return (int)x.TagType == 10;
					}).Count();
				}
				if (val.RootTag.Get<NbtList>("palette") != null)
				{
					_litematicRegionCount = 1;
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "读取 NBT 结构文件数据时出错（" + path + "）");
			}
		}

		public LocalCompFile(string path)
		{
			this.path = path ?? "";
		}

		private void _AddConflict(string modID, string versionRange, bool hard, string declarerId = null)
		{
			if (modID == null || modID.Length < 2)
			{
				return;
			}
			modID = modID.ToLower();
			if (!ModDependencyIds.IsPlatform(modID) && (string.IsNullOrWhiteSpace(versionRange) || !versionRange.Contains("$")))
			{
				string raw = (string.IsNullOrWhiteSpace(versionRange) ? null : versionRange);
				if (!_ConflictDeclarations.Any((EmbeddedConflict c) => string.Equals(c.Target, modID, StringComparison.OrdinalIgnoreCase) && c.Raw == raw && c.Hard == hard && string.Equals(c.DeclarerId, declarerId, StringComparison.OrdinalIgnoreCase)))
				{
					_ConflictDeclarations.Add(new EmbeddedConflict
					{
						DeclarerId = declarerId,
						Target = modID,
						Raw = raw,
						Hard = hard
					});
				}
			}
		}

		private void AddDependency(string modID, string versionRequirement = null, bool optional = false)
		{
			if (modID == null || modID.Length < 2)
			{
				return;
			}
			modID = modID.ToLower();
			if (ModDependencyIds.IsLoaderId(modID) || modID == "name" || (ModBase.Val(modID).ToString() ?? "") == (modID ?? ""))
			{
				return;
			}
			string raw = ((string.IsNullOrWhiteSpace(versionRequirement) || versionRequirement.Contains("$")) ? null : versionRequirement);
			bool flag = _DependencyDeclarations.Any((EmbeddedDependency d) => string.Equals(d.Id, modID, StringComparison.OrdinalIgnoreCase) && !d.Optional);
			if (!optional)
			{
				_DependencyDeclarations.RemoveAll((EmbeddedDependency d) => string.Equals(d.Id, modID, StringComparison.OrdinalIgnoreCase) && d.Optional);
			}
			if ((!optional || !flag) && !_DependencyDeclarations.Any((EmbeddedDependency d) => string.Equals(d.Id, modID, StringComparison.OrdinalIgnoreCase) && d.Raw == raw && d.Optional == optional))
			{
				_DependencyDeclarations.Add(new EmbeddedDependency
				{
					Id = modID,
					Raw = raw,
					Optional = optional
				});
			}
			if (!_DependencyRaw.TryGetValue(modID, out string value) || value == null || (!optional && !flag))
			{
				_DependencyRaw[modID] = raw;
			}
			if (versionRequirement == null || (!versionRequirement.Contains(".") && !versionRequirement.Contains("-")) || versionRequirement.Contains("$"))
			{
				versionRequirement = null;
			}
			else if (!versionRequirement.StartsWithF("[") && !versionRequirement.StartsWithF("(") && !versionRequirement.EndsWithF("]") && !versionRequirement.EndsWithF(")"))
			{
				versionRequirement = "[" + versionRequirement + ",)";
			}
			if (_Dependencies.ContainsKey(modID))
			{
				if (_Dependencies[modID] == null)
				{
					_Dependencies[modID] = versionRequirement;
				}
			}
			else
			{
				_Dependencies.Add(modID, versionRequirement);
			}
		}

		private static List<(string DeclarerId, string Body)> _ExtractInlineDepArrays(string text)
		{
			List<(string, string)> list = new List<(string, string)>();
			foreach (Match item in Regex.Matches(text, "dependencies[.\\w\"-]*\\s*=\\s*\\[", RegexOptions.IgnoreCase))
			{
				string text2 = item.Value.Substring(0, item.Value.IndexOf('=')).Trim().Substring("dependencies".Length)
					.Trim()
					.TrimStart('.')
					.Trim('"');
				if (text2.Length == 0)
				{
					text2 = null;
				}
				int num = item.Index + item.Length;
				int num2 = 1;
				bool flag = false;
				for (int i = num; i < text.Length; i++)
				{
					char c = text[i];
					if (flag)
					{
						if (c == '"')
						{
							flag = false;
						}
						continue;
					}
					switch (c)
					{
					case '"':
						flag = true;
						continue;
					case '[':
						num2++;
						continue;
					case ']':
						break;
					default:
						continue;
					}
					num2--;
					if (num2 != 0)
					{
						continue;
					}
					list.Add((text2, text.Substring(num, i - num)));
					break;
				}
			}
			return list;
		}

		internal void MarkLoaded()
		{
			isLoaded = true;
		}

		internal void SetJijMetadata(string name, string modId, string version)
		{
			_Name = name;
			_ModId = modId;
			_Version = version;
			isLoaded = true;
		}

		internal void SetJijDependencies(List<EmbeddedDependency> declarations)
		{
			_Dependencies = new Dictionary<string, string>();
			_DependencyRaw.Clear();
			_DependencyDeclarations.Clear();
			if (declarations == null)
			{
				return;
			}
			foreach (EmbeddedDependency declaration in declarations)
			{
				if (declaration?.Id != null)
				{
					AddDependency(declaration.Id, declaration.Raw, declaration.Optional);
				}
			}
		}

		internal void SetJijConflicts(List<EmbeddedConflict> conflicts, List<string> provided, Dictionary<string, string> providedVersions)
		{
			_ConflictDeclarations.Clear();
			if (conflicts != null)
			{
				foreach (EmbeddedConflict conflict in conflicts)
				{
					if (!string.IsNullOrEmpty(conflict?.Target))
					{
						_ConflictDeclarations.Add(new EmbeddedConflict
						{
							DeclarerId = conflict.DeclarerId,
							Target = conflict.Target,
							Raw = conflict.Raw,
							Hard = conflict.Hard
						});
					}
				}
			}
			_ProvidedIds.Clear();
			if (provided != null)
			{
				_ProvidedIds.UnionWith(provided);
			}
			_ProvidedVersions.Clear();
			if (providedVersions == null)
			{
				return;
			}
			foreach (KeyValuePair<string, string> providedVersion in providedVersions)
			{
				_ProvidedVersions[providedVersion.Key] = providedVersion.Value;
			}
		}

		internal void CopyLoadedStateFrom(LocalCompFile other)
		{
			if (other == null || !other.isLoaded)
			{
				return;
			}
			lock (_loadLock)
			{
				_Name = other._Name;
				_Description = other._Description;
				_Version = other._Version;
				_ModId = other._ModId;
				possibleModId = new List<string>(other.possibleModId);
				_Dependencies = new Dictionary<string, string>(other._Dependencies);
				_DependencyRaw.Clear();
				foreach (KeyValuePair<string, string> item in other._DependencyRaw)
				{
					_DependencyRaw[item.Key] = item.Value;
				}
				_DependencyDeclarations.Clear();
				foreach (EmbeddedDependency dependencyDeclaration in other._DependencyDeclarations)
				{
					_DependencyDeclarations.Add(new EmbeddedDependency
					{
						Id = dependencyDeclaration.Id,
						Raw = dependencyDeclaration.Raw,
						Optional = dependencyDeclaration.Optional
					});
				}
				_ProvidedIds.Clear();
				_ProvidedIds.UnionWith(other._ProvidedIds);
				_ProvidedVersions.Clear();
				foreach (KeyValuePair<string, string> providedVersion in other._ProvidedVersions)
				{
					_ProvidedVersions[providedVersion.Key] = providedVersion.Value;
				}
				_ConflictDeclarations.Clear();
				foreach (EmbeddedConflict conflictDeclaration in other._ConflictDeclarations)
				{
					_ConflictDeclarations.Add(new EmbeddedConflict
					{
						DeclarerId = conflictDeclaration.DeclarerId,
						Target = conflictDeclaration.Target,
						Raw = conflictDeclaration.Raw,
						Hard = conflictDeclaration.Hard
					});
				}
				_EmbeddedMods = other._EmbeddedMods;
				PreferredLoader = other.PreferredLoader;
				JijLoader = other.JijLoader;
				JijTargetMcVersion = other.JijTargetMcVersion;
				JijIdentifier = other.JijIdentifier;
				JijVersionRange = other.JijVersionRange;
				JijArtifactVersion = other.JijArtifactVersion;
				DetectedLoader = other.DetectedLoader;
				Logo = other.Logo;
				Url = other.Url;
				Authors = other.Authors;
				_FileUnavailableReason = other._FileUnavailableReason;
				isLoaded = true;
			}
		}

		private void Init()
		{
			_Name = null;
			_Description = null;
			_Version = null;
			_ModId = null;
			possibleModId = new List<string>();
			_Dependencies = new Dictionary<string, string>();
			_DependencyRaw.Clear();
			_DependencyDeclarations.Clear();
			_ProvidedIds.Clear();
			_ProvidedVersions.Clear();
			_ConflictDeclarations.Clear();
			_EmbeddedMods = new List<LocalCompFile>();
			_jijExpectedLastModified = null;
			_jijExpectedSize = null;
			JijLoader = null;
			JijTargetMcVersion = null;
			JijIdentifier = null;
			JijVersionRange = null;
			JijArtifactVersion = null;
			DetectedLoader = null;
			isLoaded = false;
			_FileUnavailableReason = null;
			isInfoWithClassLoaded = false;
			isInfoWithClassAvailable = false;
		}

		public void LoadBasicInfo()
		{
			try
			{
				if (IsFolder)
				{
					isLoaded = true;
				}
				else if (!File.Exists(path))
				{
					_FileUnavailableReason = new FileNotFoundException("未找到资源文件（" + path + ")");
					isLoaded = true;
				}
				else if (path.EndsWithF(".litematic", ignoreCase: true) || path.EndsWithF(".nbt", ignoreCase: true) || path.EndsWithF(".schem", ignoreCase: true) || path.EndsWithF(".schematic", ignoreCase: true))
				{
					_Name = ModBase.GetFileNameWithoutExtentionFromPath(path);
					isLoaded = true;
				}
				else
				{
					Load();
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "加载基本信息失败：" + path);
			}
		}

		public void LoadNbtDataIfNeeded()
		{
			try
			{
				if (!_nbtDataLoaded)
				{
					if (path.EndsWithF(".litematic", ignoreCase: true))
					{
						LoadLitematicNbtData();
					}
					else if (path.EndsWithF(".nbt", ignoreCase: true))
					{
						LoadStructureNbtData();
					}
					else if (path.EndsWithF(".schem", ignoreCase: true))
					{
						LoadSchemNbtData();
					}
					else if (path.EndsWithF(".schematic", ignoreCase: true))
					{
						LoadSchematicNbtData();
					}
					_nbtDataLoaded = true;
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "延迟加载NBT数据失败：" + path);
			}
		}

		public void Load(bool forceReload = false)
		{
			if (isLoaded && !forceReload)
			{
				return;
			}
			lock (_loadLock)
			{
				if (!isLoaded || forceReload)
				{
					LoadCore();
				}
			}
		}

		private void LoadCore()
		{
			Init();
			if (path.Length < 2)
			{
				_FileUnavailableReason = new FileNotFoundException("错误的资源文件路径（" + (path ?? "null") + "）");
				isLoaded = true;
				return;
			}
			if (IsFolder)
			{
				if (!Directory.Exists(ActualPath))
				{
					_FileUnavailableReason = new DirectoryNotFoundException("未找到文件夹（" + ActualPath + "）");
					isLoaded = true;
				}
				else
				{
					isLoaded = true;
				}
				return;
			}
			if (!File.Exists(path))
			{
				_FileUnavailableReason = new FileNotFoundException("未找到资源文件（" + path + "）");
				isLoaded = true;
				return;
			}
			if (path.EndsWithF(".litematic", ignoreCase: true) || path.EndsWithF(".nbt", ignoreCase: true) || path.EndsWithF(".schem", ignoreCase: true) || path.EndsWithF(".schematic", ignoreCase: true))
			{
				try
				{
					_Name = ModBase.GetFileNameWithoutExtentionFromPath(path);
					if (path.EndsWithF(".litematic", ignoreCase: true))
					{
						LoadLitematicNbtData();
					}
					else if (path.EndsWithF(".schem", ignoreCase: true) || path.EndsWithF(".schematic", ignoreCase: true))
					{
						if (path.EndsWithF(".schem", ignoreCase: true))
						{
							LoadSchemNbtData();
						}
						else
						{
							LoadSchematicNbtData();
						}
					}
					else if (path.EndsWithF(".nbt", ignoreCase: true))
					{
						LoadStructureNbtData();
					}
					_nbtDataLoaded = true;
				}
				catch (Exception ex)
				{
					ModBase.Log(ex, "投影文件信息获取失败（" + path + "）", ModBase.LogLevel.Developer);
					_FileUnavailableReason = ex;
				}
				isLoaded = true;
				return;
			}
			ZipArchive zipArchive = null;
			try
			{
				FileInfo fileInfo = new FileInfo(path);
				long ticks = fileInfo.LastWriteTimeUtc.Ticks;
				long length = fileInfo.Length;
				_jijExpectedLastModified = ticks;
				_jijExpectedSize = length;
				zipArchive = new ZipArchive(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
				DetectedLoader = ModJarInJar.DetectLoader(zipArchive, PreferredLoader);
				LookupMetadata(zipArchive, DetectedLoader);
				List<LocalCompFile> list = ModJarInJar.ResolveCached(path, zipArchive, out var complete, DeferJijOnMiss, ticks, length, JijScanContext, PreferredLoader);
				if (list != null)
				{
					_EmbeddedMods = list;
				}
				_jijPending = list == null || !complete;
			}
			catch (UnauthorizedAccessException ex2)
			{
				ModBase.Log(ex2, "资源文件由于无权限无法打开（" + path + "）", ModBase.LogLevel.Developer);
				_FileUnavailableReason = new UnauthorizedAccessException("没有读取此文件的权限，请尝试右键以管理员身份运行 PCL", ex2);
			}
			catch (Exception ex3)
			{
				ModBase.Log(ex3, "资源文件无法打开（" + path + "）", ModBase.LogLevel.Developer);
				_FileUnavailableReason = ex3;
			}
			finally
			{
				zipArchive?.Dispose();
			}
			isLoaded = true;
		}

		internal bool ResolveJijNow()
		{
			lock (_loadLock)
			{
				if (!_jijPending)
				{
					return true;
				}
				ZipArchive zipArchive = null;
				try
				{
					FileInfo fileInfo = new FileInfo(path);
					long value = _jijExpectedLastModified ?? fileInfo.LastWriteTimeUtc.Ticks;
					long value2 = _jijExpectedSize ?? fileInfo.Length;
					zipArchive = new ZipArchive(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
					List<LocalCompFile> list = ModJarInJar.ResolveCached(path, zipArchive, out var complete, deferOnMiss: false, value, value2, JijScanContext, PreferredLoader);
					if (list != null)
					{
						_EmbeddedMods = list;
					}
					if (list == null || !complete)
					{
						return false;
					}
					_jijPending = false;
					return true;
				}
				catch (OperationCanceledException)
				{
					return false;
				}
				catch (Exception ex2)
				{
					ModBase.Log(ex2, "后台补解析内嵌模组失败（" + path + "）", ModBase.LogLevel.Developer);
					return false;
				}
				finally
				{
					zipArchive?.Dispose();
				}
			}
		}

		internal void LookupMetadata(ZipArchive jar, string? loaderType = null)
		{
			try
			{
				ZipArchiveEntry zipArchiveEntry = ((jar.GetEntry("fabric.mod.json") != null || jar.GetEntry("quilt.mod.json") != null || jar.GetEntry("META-INF/mods.toml") != null || jar.GetEntry("META-INF/neoforge.mods.toml") != null) ? null : jar.GetEntry("mcmod.info"));
				string text = null;
				if (zipArchiveEntry != null)
				{
					text = ModBase.ReadFile(zipArchiveEntry.Open());
					if (text.Length < 15)
					{
						text = null;
					}
				}
				if (text != null)
				{
					JsonNode json = ModBase.GetJson(text);
					JsonObject jsonObject = ((json.GetValueKind() != JsonValueKind.Array) ? ((JsonObject)json["modList"][0]) : ((JsonObject)json[0]));
					Name = (string?)jsonObject["name"];
					Description = (string?)jsonObject["description"];
					Version = (string?)jsonObject["version"];
					Url = (string?)jsonObject["url"];
					ModId = (string?)jsonObject["modid"];
					JsonArray jsonArray = (JsonArray)jsonObject["authorList"];
					if (jsonArray != null)
					{
						List<string> list = new List<string>();
						foreach (JsonNode item in jsonArray)
						{
							list.Add(item.ToString());
						}
						if (list.Any())
						{
							Authors = list.Join(", ");
						}
					}
					string text2 = (string?)jsonObject["logoFile"];
					if (text2 != null)
					{
						ZipArchiveEntry entry = jar.GetEntry(text2);
						if (entry != null)
						{
							string stringMD = ModBase.GetStringMD5(entry.Length + entry.CompressedLength + path);
							Logo = Path.Combine(ModBase.pathTemp, "Cache", "Images", stringMD + ".png");
							using Stream stream = entry.Open();
							ModBase.WriteFile(Logo, stream);
						}
					}
					JsonArray jsonArray2 = (JsonArray)jsonObject["requiredMods"];
					if (jsonArray2 != null)
					{
						foreach (JsonNode item2 in jsonArray2)
						{
							string text3 = (string?)item2;
							if (!string.IsNullOrEmpty(text3))
							{
								string text4 = text3;
								text4 = text4.Substring(text4.IndexOfF(":") + 1);
								if (text4.Contains("@"))
								{
									string[] array = text4.Split("@");
									AddDependency(array[0], array[1]);
								}
								else
								{
									AddDependency(text4);
								}
							}
						}
					}
					jsonArray2 = (JsonArray)jsonObject["dependencies"];
					if (jsonArray2 != null)
					{
						foreach (JsonNode item3 in jsonArray2)
						{
							string text5 = (string?)item3;
							if (!string.IsNullOrEmpty(text5))
							{
								string text6 = text5.Substring(text5.IndexOfF(":") + 1);
								if (text6.Contains("@"))
								{
									string[] array2 = text6.Split("@");
									AddDependency(array2[0], array2[1]);
								}
								else
								{
									AddDependency(text6);
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "读取 mcmod.info 时出现未知错误（" + path + "）", ModBase.LogLevel.Developer);
			}
			try
			{
				ZipArchiveEntry zipArchiveEntry2 = (string.Equals(loaderType, "Fabric", StringComparison.OrdinalIgnoreCase) ? jar.GetEntry("fabric.mod.json") : null);
				string text7 = null;
				if (zipArchiveEntry2 != null)
				{
					text7 = ModBase.ReadFile(zipArchiveEntry2.Open(), Encoding.UTF8);
					if (!text7.Contains("schemaVersion"))
					{
						text7 = null;
					}
				}
				if (text7 != null)
				{
					JsonObject jsonObject2 = (JsonObject)ModBase.GetJson(text7);
					if (jsonObject2.ContainsKey("name"))
					{
						Name = jsonObject2["name"].ToString();
					}
					if (jsonObject2.ContainsKey("version"))
					{
						Version = jsonObject2["version"].ToString();
					}
					if (jsonObject2.ContainsKey("description"))
					{
						Description = jsonObject2["description"].ToString();
					}
					if (jsonObject2.ContainsKey("id"))
					{
						ModId = jsonObject2["id"].ToString();
					}
					if (jsonObject2.ContainsKey("contact") && jsonObject2["contact"]["homepage"] != null)
					{
						Url = jsonObject2["contact"]["homepage"].ToString();
					}
					JsonArray jsonArray3 = (JsonArray)jsonObject2["authors"];
					if (jsonArray3 != null)
					{
						List<string> list2 = jsonArray3.Select((JsonNode t) => t.ToString()).ToList();
						if (list2.Any())
						{
							Authors = string.Join(", ", list2);
						}
					}
					if (jsonObject2.ContainsKey("icon"))
					{
						string entryName = jsonObject2["icon"].ToString();
						ZipArchiveEntry entry2 = jar.GetEntry(entryName);
						if (entry2 != null)
						{
							string stringMD2 = ModBase.GetStringMD5(entry2.Length + entry2.CompressedLength + path);
							Logo = Path.Combine(ModBase.pathTemp, "Cache", "Images", stringMD2 + ".png");
							using Stream stream2 = entry2.Open();
							ModBase.WriteFile(Logo, stream2);
						}
					}
					if (jsonObject2.ContainsKey("depends"))
					{
						foreach (KeyValuePair<string, JsonNode> item4 in (JsonObject)jsonObject2["depends"])
						{
							string text8 = ((item4.Value is JsonArray source) ? string.Join(" || ", from x in source
								select x?.ToString() into x
								where !string.IsNullOrEmpty(x)
								select x) : item4.Value?.ToString());
							AddDependency(item4.Key, string.IsNullOrEmpty(text8) ? null : text8);
						}
					}
					string[] array3 = new string[2] { "recommends", "suggests" };
					foreach (string propertyName in array3)
					{
						if (!(jsonObject2[propertyName] is JsonObject jsonObject3))
						{
							continue;
						}
						foreach (KeyValuePair<string, JsonNode> item5 in jsonObject3)
						{
							string text9 = ((item5.Value is JsonArray source2) ? string.Join(" || ", from x in source2
								select x?.ToString() into x
								where !string.IsNullOrEmpty(x)
								select x) : item5.Value?.ToString());
							AddDependency(item5.Key, string.IsNullOrEmpty(text9) ? null : text9, optional: true);
						}
					}
					(string, bool)[] array4 = new (string, bool)[2]
					{
						("breaks", true),
						("conflicts", false)
					};
					for (int num = 0; num < array4.Length; num++)
					{
						var (propertyName2, hard) = array4[num];
						if (!(jsonObject2[propertyName2] is JsonObject jsonObject4))
						{
							continue;
						}
						foreach (KeyValuePair<string, JsonNode> item6 in jsonObject4)
						{
							string text10 = ((item6.Value is JsonArray source3) ? string.Join(" || ", from x in source3
								select x?.ToString() into x
								where !string.IsNullOrEmpty(x)
								select x) : item6.Value?.ToString());
							_AddConflict(item6.Key, string.IsNullOrEmpty(text10) ? null : text10, hard);
						}
					}
					if (jsonObject2["provides"] is JsonArray jsonArray4)
					{
						foreach (JsonNode item7 in jsonArray4)
						{
							string text11 = item7?.ToString();
							if (!string.IsNullOrEmpty(text11))
							{
								_ProvidedIds.Add(text11.ToLower());
							}
						}
					}
				}
			}
			catch (Exception ex2)
			{
				ModBase.Log(ex2, "读取 fabric.mod.json 时出错（" + path + "）", ModBase.LogLevel.Developer);
			}
			try
			{
				ZipArchiveEntry zipArchiveEntry3 = (string.Equals(loaderType, "Quilt", StringComparison.OrdinalIgnoreCase) ? jar.GetEntry("quilt.mod.json") : null);
				string text12 = null;
				if (zipArchiveEntry3 != null)
				{
					text12 = ModBase.ReadFile(zipArchiveEntry3.Open(), Encoding.UTF8);
					if (!text12.Contains("schema_version"))
					{
						text12 = null;
					}
				}
				if (text12 != null)
				{
					JsonObject jsonObject5 = (JsonObject)((JsonObject)ModBase.GetJson(text12))["quilt_loader"];
					if (jsonObject5.ContainsKey("id"))
					{
						ModId = (string?)jsonObject5["id"];
					}
					if (jsonObject5.ContainsKey("version"))
					{
						Version = (string?)jsonObject5["version"];
					}
					if (jsonObject5.ContainsKey("metadata"))
					{
						JsonObject jsonObject6 = (JsonObject)jsonObject5["metadata"];
						if (jsonObject6.ContainsKey("name"))
						{
							Name = (string?)jsonObject6["name"];
						}
						if (jsonObject6.ContainsKey("description"))
						{
							Description = (string?)jsonObject6["description"];
						}
						if (jsonObject6.ContainsKey("contact"))
						{
							Url = (string?)(jsonObject6["contact"]["homepage"] ?? ((JsonNode?)""));
						}
					}
					if (jsonObject5.ContainsKey("icon"))
					{
						string text13 = (string?)jsonObject5["icon"];
						if (text13 != null)
						{
							ZipArchiveEntry entry3 = jar.GetEntry(text13);
							if (entry3 != null)
							{
								string stringMD3 = ModBase.GetStringMD5(entry3.Length + entry3.CompressedLength + path);
								Logo = Path.Combine(ModBase.pathTemp, "Cache", "Images", stringMD3 + ".png");
								using Stream stream3 = entry3.Open();
								ModBase.WriteFile(Logo, stream3);
							}
						}
					}
					goto IL_1ba8;
				}
			}
			catch (Exception ex3)
			{
				ModBase.Log(ex3, "读取 quilt.mod.json 时出现未知错误（" + path + "）", ModBase.LogLevel.Developer);
			}
			try
			{
				ZipArchiveEntry zipArchiveEntry4 = null;
				if (string.Equals(loaderType, "NeoForge", StringComparison.OrdinalIgnoreCase))
				{
					zipArchiveEntry4 = jar.GetEntry("META-INF/neoforge.mods.toml") ?? jar.GetEntry("META-INF/mods.toml");
				}
				else if (string.Equals(loaderType, "Forge", StringComparison.OrdinalIgnoreCase))
				{
					zipArchiveEntry4 = jar.GetEntry("META-INF/mods.toml") ?? jar.GetEntry("META-INF/neoforge.mods.toml");
				}
				string text14 = null;
				if (zipArchiveEntry4 != null)
				{
					using (StreamReader streamReader = new StreamReader(zipArchiveEntry4.Open()))
					{
						text14 = streamReader.ReadToEnd();
					}
					if (text14.Length < 15)
					{
						text14 = null;
					}
				}
				if (text14 != null)
				{
					List<string> list3 = new List<string>();
					string[] array3 = text14.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
					for (int num = 0; num < array3.Length; num++)
					{
						string text15 = array3[num];
						if (!text15.StartsWithF("#"))
						{
							if (text15.Contains("#"))
							{
								text15 = text15.Substring(0, text15.IndexOfF("#"));
							}
							text15 = text15.Trim(new char[3] { ' ', '\t', '\u3000' });
							if (!string.IsNullOrEmpty(text15))
							{
								list3.Add(text15);
							}
						}
					}
					List<KeyValuePair<string, Dictionary<string, object>>> list4 = new List<KeyValuePair<string, Dictionary<string, object>>>
					{
						new KeyValuePair<string, Dictionary<string, object>>("", new Dictionary<string, object>())
					};
					for (int num2 = 0; num2 < list3.Count; num2++)
					{
						string text16 = list3[num2];
						if (text16.StartsWithF("[") && text16.EndsWithF("]"))
						{
							string key = text16.Trim(new char[2] { '[', ']' });
							list4.Add(new KeyValuePair<string, Dictionary<string, object>>(key, new Dictionary<string, object>()));
						}
						else
						{
							if (!text16.Contains("="))
							{
								continue;
							}
							string key2 = text16.Substring(0, text16.IndexOfF("=")).TrimEnd(new char[3] { ' ', '\t', '\u3000' });
							string text17 = text16.Substring(text16.IndexOfF("=") + 1).TrimStart(new char[3] { ' ', '\t', '\u3000' });
							object value;
							if (text17.StartsWithF("\"") && text17.EndsWithF("\""))
							{
								value = text17.Trim('"');
							}
							else if (!text17.StartsWithF("'''"))
							{
								if (text17.ToLower() == "true" || text17.ToLower() == "false")
								{
									value = text17.ToLower() == "true";
								}
								else
								{
									value = ((!double.TryParse(text17, out var result)) ? text17 : ((object)result));
								}
							}
							else
							{
								List<string> list5 = new List<string> { text17.Replace("'''", "") };
								if (!text17.EndsWithF("'''") || text17.Length == 3)
								{
									while (num2 < list3.Count - 1)
									{
										num2++;
										string text18 = list3[num2];
										if (text18.EndsWithF("'''"))
										{
											list5.Add(text18.Replace("'''", ""));
											break;
										}
										list5.Add(text18);
									}
								}
								value = string.Join("\n", list5).Trim('\n').Replace("\n", "\r\n");
							}
							list4[list4.Count - 1].Value[key2] = value;
						}
					}
					Dictionary<string, object> dictionary = null;
					foreach (KeyValuePair<string, Dictionary<string, object>> item8 in list4)
					{
						if (item8.Key == "mods")
						{
							dictionary = item8.Value;
							break;
						}
					}
					if (dictionary != null && dictionary.ContainsKey("modId"))
					{
						ModId = dictionary["modId"].ToString();
						if (_ModId != null)
						{
							if (dictionary.ContainsKey("displayName"))
							{
								Name = dictionary["displayName"].ToString();
							}
							if (dictionary.ContainsKey("description"))
							{
								Description = dictionary["description"].ToString();
							}
							if (dictionary.ContainsKey("version"))
							{
								Version = dictionary["version"].ToString();
							}
							if (list4[0].Value.ContainsKey("displayURL"))
							{
								Url = list4[0].Value["displayURL"].ToString();
							}
							if (list4[0].Value.ContainsKey("authors"))
							{
								Authors = list4[0].Value["authors"].ToString();
							}
							string text19 = ModId.ToLower();
							Dictionary<string, string> dictionary2 = (from s in list4
								where s.Key == "mods" && s.Value.ContainsKey("modId")
								select (Id: s.Value["modId"].ToString().ToLower(), Version: s.Value.ContainsKey("version") ? s.Value["version"].ToString() : null)).GroupBy(((string Id, string Version) m) => m.Id, StringComparer.OrdinalIgnoreCase).ToDictionary((IGrouping<string, (string Id, string Version)> g) => g.Key, (IGrouping<string, (string Id, string Version)> g) => g.First().Version, StringComparer.OrdinalIgnoreCase);
							HashSet<string> hashSet = dictionary2.Keys.ToHashSet();
							foreach (string item9 in hashSet)
							{
								if (item9 != text19)
								{
									_ProvidedIds.Add(item9);
									if (!string.IsNullOrWhiteSpace(dictionary2[item9]))
									{
										_ProvidedVersions[item9] = dictionary2[item9];
									}
								}
							}
							foreach (KeyValuePair<string, Dictionary<string, object>> item10 in list4)
							{
								string text20 = item10.Key.ToLower();
								if (text20 != "dependencies" && !text20.StartsWithF("dependencies."))
								{
									continue;
								}
								Dictionary<string, object> value2 = item10.Value;
								if (!value2.ContainsKey("modId"))
								{
									continue;
								}
								string text21 = value2["modId"].ToString();
								string text22 = (text20.StartsWithF("dependencies.") ? text20.Substring("dependencies.".Length).Trim('"') : text19);
								if (!hashSet.Contains(text22))
								{
									continue;
								}
								string text23 = (value2.ContainsKey("type") ? value2["type"].ToString().ToLower() : null);
								bool flag = value2.ContainsKey("side") && value2["side"].ToString().ToLower() == "server";
								string text24 = (value2.ContainsKey("versionRange") ? value2["versionRange"].ToString() : null);
								bool flag2 = hashSet.Contains(text21.ToLower());
								if (flag2)
								{
									bool flag3 = ((text23 == "incompatible" || text23 == "discouraged") ? true : false);
									flag2 = !flag3;
								}
								if (flag2)
								{
									continue;
								}
								if ((text23 == "incompatible" || text23 == "discouraged") ? true : false)
								{
									if (!flag)
									{
										_AddConflict(text21, text24, text23 == "incompatible", text22);
									}
								}
								else if (!flag)
								{
									bool optional = text23 == "optional" || (value2.ContainsKey("mandatory") && value2["mandatory"].ToString().ToLower() == "false");
									AddDependency(text21, text24, optional);
								}
							}
							foreach (var item11 in _ExtractInlineDepArrays(string.Join("\n", list3)))
							{
								foreach (Match item12 in Regex.Matches(item11.Body, "\\{([^}]*)\\}"))
								{
									string text25 = item11.DeclarerId ?? text19;
									if (!hashSet.Contains(text25))
									{
										continue;
									}
									string value3 = item12.Groups[1].Value;
									Match match2 = Regex.Match(value3, "modId\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
									if (!match2.Success || Regex.IsMatch(value3, "side\\s*=\\s*\"server\"", RegexOptions.IgnoreCase))
									{
										continue;
									}
									Match match3 = Regex.Match(value3, "type\\s*=\\s*\"(incompatible|discouraged|optional|required)\"", RegexOptions.IgnoreCase);
									string text26 = (match3.Success ? match3.Groups[1].Value.ToLower() : null);
									bool optional2 = Regex.IsMatch(value3, "mandatory\\s*=\\s*\"?false", RegexOptions.IgnoreCase) || text26 == "optional";
									Match match4 = Regex.Match(value3, "versionRange\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
									string text27 = (match4.Success ? match4.Groups[1].Value : null);
									string value4 = match2.Groups[1].Value;
									bool flag2 = hashSet.Contains(value4.ToLower());
									if (flag2)
									{
										bool flag3 = ((text26 == "incompatible" || text26 == "discouraged") ? true : false);
										flag2 = !flag3;
									}
									if (!flag2)
									{
										if ((text26 == "incompatible" || text26 == "discouraged") ? true : false)
										{
											_AddConflict(value4, text27, text26 == "incompatible", text25);
										}
										else
										{
											AddDependency(value4, text27, optional2);
										}
									}
								}
							}
							goto IL_1ba8;
						}
					}
				}
			}
			catch (Exception ex4)
			{
				ModBase.Log(ex4, "读取 mods.toml 时出现未知错误（" + path + "）", ModBase.LogLevel.Developer);
			}
			try
			{
				ZipArchiveEntry entry4 = jar.GetEntry("META-INF/fml_cache_annotation.json");
				string text28 = null;
				if (entry4 != null)
				{
					text28 = ModBase.ReadFile(entry4.Open(), Encoding.UTF8);
					if (!text28.Contains("Lnet/minecraftforge/fml/common/Mod;"))
					{
						text28 = null;
					}
				}
				JsonObject jsonObject8;
				if (text28 != null)
				{
					JsonObject jsonObject7 = (JsonObject)ModBase.GetJson(text28);
					jsonObject8 = null;
					foreach (KeyValuePair<string, JsonNode> item13 in jsonObject7)
					{
						JsonArray jsonArray5 = (JsonArray)item13.Value["annotations"];
						if (jsonArray5 == null)
						{
							continue;
						}
						foreach (JsonNode item14 in jsonArray5)
						{
							if (!((string?)(item14["name"] ?? ((JsonNode?)"")) == "Lnet/minecraftforge/fml/common/Mod;"))
							{
								continue;
							}
							jsonObject8 = (JsonObject)item14["values"];
							goto IL_17b5;
						}
					}
				}
				goto end_IL_16aa;
				IL_17b5:
				if (jsonObject8.ContainsKey("useMetadata") && (jsonObject8["useMetadata"]["value"] ?? ((JsonNode?)"")).ToString().ToLower() == "true")
				{
					string text29 = (string?)jsonObject8["modid"]["value"];
					if (text29 != null)
					{
						text29 = text29.ToLower().RegexSeek(RegexPatterns.ModIdMatch);
						if (text29 != null && text29.ToLower() != "name" && text29.Length > 1 && (ModBase.Val(text29).ToString() ?? "") != (text29 ?? "") && !possibleModId.Contains(text29))
						{
							possibleModId.Add(text29);
						}
					}
				}
				else
				{
					if (jsonObject8.ContainsKey("name"))
					{
						Name = (string?)jsonObject8["name"]["value"];
					}
					if (jsonObject8.ContainsKey("version"))
					{
						Version = (string?)jsonObject8["version"]["value"];
					}
					if (jsonObject8.ContainsKey("modid"))
					{
						ModId = (string?)jsonObject8["modid"]["value"];
					}
					if (!jsonObject8.ContainsKey("serverSideOnly") || !JsonCompat.ToObject<bool>(jsonObject8["serverSideOnly"]["value"]))
					{
						string text30 = (string?)(((jsonObject8["acceptedMinecraftVersions"] != null) ? jsonObject8["acceptedMinecraftVersions"]["value"] : ((JsonNode?)"")) ?? ((JsonNode?)""));
						if (!string.IsNullOrEmpty(text30))
						{
							AddDependency("minecraft", text30);
						}
						string text31 = (string?)(((jsonObject8["dependencies"] != null) ? jsonObject8["dependencies"]["value"] : ((JsonNode?)"")) ?? ((JsonNode?)""));
						if (!string.IsNullOrEmpty(text31))
						{
							string[] array3 = text31.Split(";");
							foreach (string text32 in array3)
							{
								if (!string.IsNullOrEmpty(text32) && text32.StartsWithF("required-"))
								{
									string text33 = text32.Substring(text32.IndexOfF(":") + 1);
									if (text33.Contains("@"))
									{
										string[] array5 = text33.Split("@");
										AddDependency(array5[0], array5[1]);
									}
									else
									{
										AddDependency(text33);
									}
								}
							}
						}
					}
				}
				end_IL_16aa:;
			}
			catch (Exception ex5)
			{
				ModBase.Log(ex5, "读取 fml_cache_annotation.json 时出现未知错误（" + path + "）");
			}
			try
			{
				ZipArchiveEntry entry5 = jar.GetEntry("pack.png");
				if (entry5 != null)
				{
					try
					{
						string stringMD4 = ModBase.GetStringMD5(entry5.Length + entry5.CompressedLength + path);
						Logo = Path.Combine(ModBase.pathTemp, "Cache", "Images", stringMD4 + ".png");
						using (Stream stream4 = entry5.Open())
						{
							ModBase.WriteFile(Logo, stream4);
						}
						ModBase.Log("成功提取资源包图标：" + path, ModBase.LogLevel.Developer);
					}
					catch (Exception ex6)
					{
						ModBase.Log(ex6, "提取 pack.png 图标失败（" + path + "）", ModBase.LogLevel.Developer);
					}
				}
			}
			catch (Exception ex7)
			{
				ModBase.Log(ex7, "识别资源包图标时出现未知错误（" + path + "）", ModBase.LogLevel.Developer);
			}
			goto IL_1ba8;
			IL_1ba8:
			if (_Version == "version")
			{
				try
				{
					ZipArchiveEntry entry6 = jar.GetEntry("META-INF/MANIFEST.MF");
					if (entry6 != null)
					{
						string text34 = ModBase.ReadFile(entry6.Open()).Replace(" :", ":").Replace(": ", ":");
						if (text34.Contains("Implementation-Version:"))
						{
							text34 = text34.Substring(text34.IndexOfF("Implementation-Version:") + "Implementation-Version:".Count());
							text34 = text34.Substring(0, text34.IndexOfAny("\r\n".ToCharArray())).Trim();
							Version = text34;
						}
					}
				}
				catch (Exception)
				{
					ModBase.Log("获取 META-INF 中的版本信息失败（" + path + "）", ModBase.LogLevel.Developer);
					Version = null;
				}
			}
			if (_Version != null && !_Version.Contains(".") && !_Version.Contains("-"))
			{
				Version = null;
			}
		}

		public JsonObject ToJson()
		{
			JsonObject jsonObject = new JsonObject();
			if ((object)Comp != null)
			{
				jsonObject.Add("Comp", Comp.ToJson());
			}
			jsonObject.Add("ChangelogUrls", new JsonArray(((IEnumerable<string>)changelogUrls).Select((Func<string, JsonNode>)((string s) => s)).ToArray()));
			jsonObject.Add("CompLoaded", (JsonNode)compLoaded);
			if (compFile != null)
			{
				jsonObject.Add("CompFile", compFile.ToJson());
			}
			if (UpdateFile != null)
			{
				jsonObject.Add("UpdateFile", UpdateFile.ToJson());
			}
			return jsonObject;
		}

		public void FromJson(JsonObject json)
		{
			compLoaded = (bool)json["CompLoaded"];
			if (json.ContainsKey("Comp"))
			{
				Comp = new ModComp.CompProject((JsonObject)json["Comp"]);
			}
			if (json.ContainsKey("ChangelogUrls"))
			{
				changelogUrls = JsonCompat.ToObject<List<string>>(json["ChangelogUrls"]);
			}
			if (json.ContainsKey("CompFile"))
			{
				compFile = new ModComp.CompFile((JsonObject)json["CompFile"], ModComp.CompType.Mod);
			}
			if (json.ContainsKey("UpdateFile"))
			{
				UpdateFile = new ModComp.CompFile((JsonObject)json["UpdateFile"], ModComp.CompType.Mod);
			}
		}

		public override string ToString()
		{
			return $"{State} - {path}";
		}

		public override bool Equals(object obj)
		{
			if (obj is LocalCompFile localCompFile)
			{
				return (path ?? "") == (localCompFile.path ?? "");
			}
			return false;
		}
	}

	public class CompLocalLoaderData
	{
		public string compPath;

		public ModComp.CompType compType;

		public KeyValuePair<List<LocalCompFile>, JsonObject> detailInfo;

		public PageInstanceCompResource frm;

		public McInstance gameVersion;

		public List<ModComp.CompLoaderType> loaders;
	}

	private const int localModCacheVersion = 7;

	private static readonly Lazy<HashCache> _hashCache = new Lazy<HashCache>(() =>
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected Obj, but got Unknown
		return new HashCache(ModBase.pathTemp + "Cache\\HashCache.db");
	});

	public static ModLoader.LoaderTask<CompLocalLoaderData, List<LocalCompFile>> compResourceListLoader = new ModLoader.LoaderTask<CompLocalLoaderData, List<LocalCompFile>>("Comp Resource List Loader", CompResourceListLoad);

	[ThreadStatic]
	internal static bool DeferJijOnMiss;

	[ThreadStatic]
	internal static ModJarInJar.ScanContext JijScanContext;

	internal static volatile bool CompJijResolved = true;

	private static int _jijGeneration;

	public static ModLoader.LoaderTask<CompLocalLoaderData, int> compUpdateDetailLoader = new ModLoader.LoaderTask<CompLocalLoaderData, int>("Comp List Detail Loader", CompUpdateDetailLoad);

	private static readonly Regex regexIsJarFile = new Regex("\\.jar(\\.disabled)?$");

	private static string GetFolderDescription(string folderPath)
	{
		try
		{
			if (!Directory.Exists(folderPath))
			{
				return "空文件夹";
			}
			return "文件夹";
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "获取文件夹描述失败：" + folderPath);
			return "文件夹";
		}
	}

	internal static string? GetPreferredLoader(McInstance? instance)
	{
		if (instance?.Info == null)
		{
			return null;
		}
		if (instance.Info.HasNeoForge)
		{
			return "NeoForge";
		}
		if (instance.Info.HasForge || instance.Info.HasCleanroom)
		{
			return "Forge";
		}
		if (instance.Info.HasFabric)
		{
			return "Fabric";
		}
		if (instance.Info.HasQuilt)
		{
			return "Quilt";
		}
		return null;
	}

	private static void _ResolveJijPendingBackground(List<LocalCompFile> pending, List<string> allModPaths, string instancePath, int loaderTaskId, int generation)
	{
		ModJarInJarCache.UseInstance(instancePath);
		JijScanContext = new ModJarInJar.ScanContext(() => compResourceListLoader.IsAbortedWithThread(loaderTaskId));
		bool flag = false;
		try
		{
			foreach (LocalCompFile item in pending)
			{
				if (compResourceListLoader.IsAbortedWithThread(loaderTaskId))
				{
					return;
				}
				item.ResolveJijNow();
			}
			if (!compResourceListLoader.IsAbortedWithThread(loaderTaskId))
			{
				flag = pending.All((LocalCompFile m) => !m.JijPending);
				if (flag)
				{
					ModJarInJarCache.Prune(allModPaths);
				}
			}
		}
		catch (Exception ex)
		{
			ModBase.Log(ex, "Jar-in-Jar 后台补解析失败", ModBase.LogLevel.Developer);
		}
		finally
		{
			ModJarInJarCache.Flush();
			JijScanContext = null;
			ModJarInJarCache.UseInstance(null);
			if (generation == Volatile.Read(in _jijGeneration))
			{
				CompJijResolved = flag;
				ModBase.RunInUi(() =>
				{
					ModMain.frmInstanceModJarInJar?.OnJijResolved();
				});
			}
		}
	}

	private static void CompResourceListLoad(ModLoader.LoaderTask<CompLocalLoaderData, List<LocalCompFile>> loader)
	{
		try
		{
			ModBase.RunInUiWait(() =>
			{
				if (loader.input.frm != null)
				{
					loader.input.frm.Load.ShowProgress = false;
				}
			});
			if (PageInstanceCompResource.updatingVersions.Contains(loader.input.compPath))
			{
				ModBase.Log("[Mod] 等待资源更新完成后才能继续加载资源列表：" + loader.input.compPath);
				try
				{
					ModBase.RunInUiWait(() =>
					{
						if (loader.input.frm != null)
						{
							loader.input.frm.Load.Text = Lang.Text("Instance.Resource.Update.WaitingForUpdate");
						}
					});
					while (PageInstanceCompResource.updatingVersions.Contains(loader.input.compPath))
					{
						if (loader.IsAborted)
						{
							return;
						}
						Thread.Sleep(100);
					}
				}
				finally
				{
					ModBase.RunInUiWait(() =>
					{
						if (loader.input.frm != null)
						{
							loader.input.frm.Load.Text = Lang.Text("Instance.Resource.List.Loading");
						}
					});
				}
				loader.input.frm.LoaderRun(ModLoader.LoaderFolderRunType.UpdateOnly);
			}
			List<LocalCompFile> list = new List<LocalCompFile>();
			if (Directory.Exists(loader.input.compPath))
			{
				string text = loader.input.compPath.ToLower();
				if (loader.input.compType == ModComp.CompType.Schematic)
				{
					string text2 = "";
					if (loader.input.frm != null)
					{
						text2 = loader.input.frm.CurrentFolderPath;
					}
					string text3 = (string.IsNullOrEmpty(text2) ? loader.input.compPath : text2);
					try
					{
						DirectoryInfo directoryInfo = new DirectoryInfo(text3);
						foreach (DirectoryInfo item in directoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
						{
							list.Add(new LocalCompFile(Path.Combine(item.FullName, "__FOLDER__")));
						}
						foreach (FileInfo item2 in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
						{
							try
							{
								if (LocalCompFile.IsCompFile(item2.FullName, loader.input.compType))
								{
									list.Add(new LocalCompFile(item2.FullName));
								}
							}
							catch (Exception ex)
							{
								ModBase.Log(ex, "处理文件失败：" + item2.FullName);
							}
						}
					}
					catch (Exception ex2)
					{
						ModBase.Log(ex2, "枚举文件失败：" + text3);
					}
				}
				else
				{
					try
					{
						foreach (FileInfo item3 in ModBase.EnumerateFiles(loader.input.compPath))
						{
							try
							{
								if ((!((item3.DirectoryName.ToLower() ?? "") != (text.TrimEnd('\\') ?? "")) || ((object)PageInstanceLeft.McInstance != null && PageInstanceLeft.McInstance.Info.HasForge && PageInstanceLeft.McInstance.Info.Drop < 130 && (item3.Directory.Name ?? "") == (PageInstanceLeft.McInstance.Info.VanillaName ?? ""))) && LocalCompFile.IsCompFile(item3.FullName, loader.input.compType))
								{
									list.Add(new LocalCompFile(item3.FullName));
								}
							}
							catch (Exception ex3)
							{
								ModBase.Log(ex3, "处理文件失败：" + item3.FullName);
							}
						}
					}
					catch (Exception ex4)
					{
						ModBase.Log(ex4, "枚举文件夹失败：" + loader.input.compPath);
					}
				}
			}
			loader.Progress = 0.05;
			if (list.Count > 50)
			{
				ModBase.RunInUi(() =>
				{
					if (loader.input.frm != null)
					{
						loader.input.frm.Load.ShowProgress = true;
					}
				});
			}
			string filePath = ModBase.pathTemp + "Cache\\LocalComp.json";
			JsonObject jsonObject = new JsonObject();
			try
			{
				string text4 = ModBase.ReadFile(filePath);
				if (!string.IsNullOrWhiteSpace(text4))
				{
					jsonObject = (JsonObject)ModBase.GetJson(text4);
					if (!jsonObject.ContainsKey("version") || JsonCompat.ToObject<int>(jsonObject["version"]) != 7)
					{
						ModBase.Log("[Mod] 本地 Mod 信息缓存版本已过期，将弃用这些缓存信息", ModBase.LogLevel.Debug);
						jsonObject = new JsonObject();
					}
				}
			}
			catch (Exception ex5)
			{
				ModBase.Log(ex5, "读取本地 Mod 信息缓存失败，已重置");
				jsonObject = new JsonObject();
			}
			jsonObject["version"] = 7;
			List<LocalCompFile> list2 = new List<LocalCompFile>();
			bool flag = loader.input.compType == ModComp.CompType.Mod;
			string jijInstancePath = (flag ? loader.input.gameVersion.PathInstance : null);
			string preferredLoader = (flag ? GetPreferredLoader(loader.input.gameVersion) : null);
			int jijGeneration = 0;
			Action action = null;
			ModJarInJarCache.UseInstance(jijInstancePath);
			if (flag)
			{
				jijGeneration = Interlocked.Increment(ref _jijGeneration);
				CompJijResolved = false;
				DeferJijOnMiss = true;
			}
			foreach (LocalCompFile item4 in list)
			{
				loader.Progress += 0.94 / (double)list.Count;
				if (loader.IsAborted)
				{
					return;
				}
				if (item4.IsFolder)
				{
					continue;
				}
				if (flag)
				{
					item4.PreferredLoader = preferredLoader;
				}
				if (loader.input.compType == ModComp.CompType.Schematic)
				{
					item4.LoadBasicInfo();
				}
				else
				{
					item4.Load();
				}
				if (item4.State == LocalCompFile.LocalFileStatus.Unavailable)
				{
					continue;
				}
				string propertyName = item4.ModrinthHash + loader.input.gameVersion.Info.VanillaName + loader.input.loaders.Join("");
				if (jsonObject.ContainsKey(propertyName))
				{
					item4.FromJson((JsonObject)jsonObject[propertyName]);
					if (item4.compLoaded && DateTime.Now - JsonCompat.ToObject<DateTime>(jsonObject[propertyName]["Comp"]["CacheTime"]) < new TimeSpan(6, 0, 0))
					{
						continue;
					}
				}
				list2.Add(item4);
			}
			if (flag)
			{
				DeferJijOnMiss = false;
				List<string> allModPaths = (from m in list
					where !m.IsFolder
					select m.path).ToList();
				List<LocalCompFile> pending = list.Where((LocalCompFile m) => !m.IsFolder && m.JijPending).ToList();
				if (pending.Count == 0)
				{
					ModJarInJarCache.Prune(allModPaths);
					ModJarInJarCache.Flush();
					if (jijGeneration == Volatile.Read(in _jijGeneration))
					{
						CompJijResolved = true;
					}
				}
				else
				{
					int loaderTaskId = Task.CurrentId ?? (-1);
					action = () =>
					{
						_ResolveJijPendingBackground(pending, allModPaths, jijInstancePath, loaderTaskId, jijGeneration);
					};
				}
			}
			loader.Progress = 0.99;
			ModBase.Log($"[Mod] 共有 {list.Count} 个 Mod，其中 {list2.Where((LocalCompFile m) => (object)m.Comp == null).Count()} 个需要联网获取信息，{list2.Where((LocalCompFile m) => (object)m.Comp != null).Count()} 个需要更新信息");
			list.Sort((LocalCompFile left, LocalCompFile right) => (left.State == LocalCompFile.LocalFileStatus.Unavailable != (right.State == LocalCompFile.LocalFileStatus.Unavailable)) ? ((left.State == LocalCompFile.LocalFileStatus.Unavailable) ? 1 : (-1)) : right.FileName.CompareTo(left.FileName));
			if (!loader.IsAborted)
			{
				loader.output = list;
				if (action != null)
				{
					ModBase.RunInNewThread(action, "Jar-in-Jar 后台解析");
				}
				if (list2.Any())
				{
					loader.input.detailInfo = new KeyValuePair<List<LocalCompFile>, JsonObject>(list2, jsonObject);
					compUpdateDetailLoader.Start(loader.input, isForceRestart: true);
				}
			}
		}
		catch (Exception ex6)
		{
			ModBase.Log(ex6, "Mod 列表加载失败");
			throw;
		}
		finally
		{
			ModJarInJarCache.UseInstance(null);
			DeferJijOnMiss = false;
		}
	}

	private static void CompUpdateDetailLoad(ModLoader.LoaderTask<CompLocalLoaderData, int> loader)
	{
		List<LocalCompFile> mods = loader.input.detailInfo.Key;
		JsonObject value = loader.input.detailInfo.Value;
		List<ModComp.CompLoaderType> modLoaders = loader.input.loaders;
		ModComp.CompType compType = loader.input.compType;
		string mcInstance = loader.input.gameVersion.Info.VanillaName;
		ModBase.Log("[Mod] 目标加载器：" + string.Join("/", modLoaders) + "，版本：" + mcInstance);
		int endedThreadCount = 0;
		bool isFailed = false;
		int currentTaskId = Task.CurrentId ?? (-1);
		ModBase.RunInNewThread(() =>
		{
			try
			{
				List<string> values = mods.Select((LocalCompFile m) => m.ModrinthHash).ToList();
				JsonObject jsonObject = (JsonObject)ModBase.GetJson(ModDownload.DlModRequest("https://api.modrinth.com/v2/version_files", "POST", "{\"hashes\": [\"" + string.Join("\",\"", values) + "\"], \"algorithm\": \"sha1\"}", "application/json"));
				ModBase.Log($"[Mod] 从 Modrinth 获取到 {jsonObject.Count} 个本地 Mod 的对应信息");
				if (jsonObject.Count != 0)
				{
					Dictionary<string, List<LocalCompFile>> dictionary = new Dictionary<string, List<LocalCompFile>>();
					foreach (LocalCompFile item2 in mods)
					{
						if (jsonObject[item2.ModrinthHash] != null && !(jsonObject[item2.ModrinthHash]["files"][0]["hashes"]["sha1"].ToString() != item2.ModrinthHash))
						{
							string key = jsonObject[item2.ModrinthHash]["project_id"].ToString();
							if (ModComp.compProjectCache.ContainsKey(key) && (object)item2.Comp == null)
							{
								item2.Comp = ModComp.compProjectCache[key];
							}
							if (!dictionary.ContainsKey(key))
							{
								dictionary[key] = new List<LocalCompFile>();
							}
							dictionary[key].Add(item2);
							ModComp.CompFile compFile = new ModComp.CompFile((JsonObject)jsonObject[item2.ModrinthHash], ModComp.CompType.Mod);
							if (item2.compFile == null || item2.compFile.ReleaseDate < compFile.ReleaseDate)
							{
								item2.compFile = compFile;
							}
						}
					}
					if (!loader.IsAbortedWithThread(currentTaskId))
					{
						ModBase.Log($"[Mod] 需要从 Modrinth 获取 {dictionary.Count} 个本地 Mod 的工程信息");
						if (dictionary.Any())
						{
							foreach (JsonObject item3 in (JsonArray)ModBase.GetJson(ModDownload.DlModRequest("https://api.modrinth.com/v2/projects?ids=[\"" + string.Join("\",\"", dictionary.Keys) + "\"]", "GET", "", "application/json")))
							{
								ModComp.CompProject compProject = new ModComp.CompProject(item3);
								foreach (LocalCompFile item4 in dictionary[compProject.Id])
								{
									item4.Comp = compProject;
								}
							}
							ModBase.Log("[Mod] 已从 Modrinth 获取本地 Mod 信息，继续获取更新信息");
							string value2 = ((compType == ModComp.CompType.DataPack) ? "datapack" : string.Join("\",\"", modLoaders).ToLower());
							JsonObject jsonObject2 = (JsonObject)ModBase.GetJson(ModDownload.DlModRequest("https://api.modrinth.com/v2/version_files/update", "POST", $"{{\"hashes\": [\"{string.Join("\",\"", dictionary.SelectMany((KeyValuePair<string, List<LocalCompFile>> l) => l.Value.Select((LocalCompFile m) => m.ModrinthHash)))}\"], \"algorithm\": \"sha1\", \"loaders\": [\"{value2}\"],\"game_versions\": [\"{mcInstance}\"]}}", "application/json"));
							foreach (LocalCompFile item5 in mods)
							{
								if (jsonObject2[item5.ModrinthHash] != null && item5.compFile != null)
								{
									ModComp.CompFile compFile2 = new ModComp.CompFile((JsonObject)jsonObject2[item5.ModrinthHash], ModComp.CompType.Mod);
									if (compFile2.Available)
									{
										if (ModBase.modeDebug)
										{
											ModBase.Log("[Mod] 本地文件 " + item5.compFile.FileName + " 在 Modrinth 上的最新版为 " + compFile2.FileName);
										}
										if (!(item5.compFile.ReleaseDate >= compFile2.ReleaseDate) && !(item5.compFile.Hash == compFile2.Hash))
										{
											if (item5.UpdateFile != null && compFile2.Hash == item5.UpdateFile.Hash)
											{
												item5.changelogUrls.Add($"https://modrinth.com/mod/{jsonObject2[item5.ModrinthHash]["project_id"]}/changelog?g={mcInstance}");
												item5.UpdateFile.DownloadUrls.AddRange(compFile2.DownloadUrls);
												item5.UpdateFile = compFile2;
											}
											else if (item5.UpdateFile == null || compFile2.ReleaseDate >= item5.UpdateFile.ReleaseDate)
											{
												item5.changelogUrls = new List<string> { $"https://modrinth.com/mod/{jsonObject2[item5.ModrinthHash]["project_id"]}/changelog?g={mcInstance}" };
												item5.UpdateFile = compFile2;
											}
										}
									}
								}
							}
							ModBase.Log("[Mod] 从 Modrinth 获取本地 Mod 信息结束");
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "从 Modrinth 获取本地 Mod 信息失败");
				isFailed = true;
			}
			finally
			{
				Interlocked.Increment(ref endedThreadCount);
			}
		}, "Mod List Detail Loader Modrinth");
		ModBase.RunInNewThread(() =>
		{
			try
			{
				List<uint> list2 = new List<uint>();
				foreach (LocalCompFile item6 in mods)
				{
					list2.Add(item6.CurseForgeHash);
					if (loader.IsAbortedWithThread(currentTaskId))
					{
						return;
					}
				}
				JsonArray jsonArray = (JsonArray)((JsonObject)ModBase.GetJson(ModDownload.DlModRequest("https://api.curseforge.com/v1/fingerprints/432", "POST", "{\"fingerprints\": [" + string.Join(",", list2) + "]}", "application/json")))["data"]["exactMatches"];
				ModBase.Log($"[Mod] 从 CurseForge 获取到 {jsonArray.Count} 个本地 Mod 的对应信息");
				if (jsonArray.Any())
				{
					Dictionary<string, List<LocalCompFile>> dictionary = new Dictionary<string, List<LocalCompFile>>();
					foreach (JsonNode item7 in jsonArray)
					{
						string key = item7["id"].ToString();
						uint num = JsonCompat.ToObject<uint>(item7["file"]["fileFingerprint"]);
						foreach (LocalCompFile item8 in mods)
						{
							if (item8.CurseForgeHash == num)
							{
								if (ModComp.compProjectCache.ContainsKey(key) && (object)item8.Comp == null)
								{
									item8.Comp = ModComp.compProjectCache[key];
								}
								if (!dictionary.ContainsKey(key))
								{
									dictionary[key] = new List<LocalCompFile>();
								}
								dictionary[key].Add(item8);
								ModComp.CompFile compFile = new ModComp.CompFile((JsonObject)item7["file"], ModComp.CompType.Mod);
								if (item8.compFile == null || item8.compFile.ReleaseDate < compFile.ReleaseDate)
								{
									item8.compFile = compFile;
								}
							}
						}
					}
					if (!loader.IsAbortedWithThread(currentTaskId))
					{
						ModBase.Log($"[Mod] 需要从 CurseForge 获取 {dictionary.Count} 个本地 Mod 的工程信息");
						if (dictionary.Any())
						{
							JsonArray jsonArray2 = (JsonArray)((JsonObject)ModBase.GetJson(ModDownload.DlModRequest("https://api.curseforge.com/v1/mods", "POST", "{\"modIds\": [" + string.Join(",", dictionary.Keys) + "]}", "application/json")))["data"];
							Dictionary<int, List<LocalCompFile>> dictionary2 = new Dictionary<int, List<LocalCompFile>>();
							Dictionary<int, string> dictionary3 = new Dictionary<int, string>();
							foreach (JsonNode item9 in jsonArray2)
							{
								if (item9["isAvailable"] == null || JsonCompat.ToObject<bool>(item9["isAvailable"]))
								{
									ModComp.CompProject compProject = new ModComp.CompProject((JsonObject)item9);
									if (dictionary.ContainsKey(compProject.Id))
									{
										foreach (LocalCompFile item10 in dictionary[compProject.Id])
										{
											if ((object)item10.Comp != null && !item10.Comp.FromCurseForge)
											{
												ModComp.CompProject comp = item10.Comp;
												item10.Comp = comp;
											}
											else
											{
												item10.Comp = compProject;
											}
										}
										if (modLoaders.Count == 1)
										{
											string text = null;
											List<int> list3 = new List<int>();
											foreach (JsonNode item11 in (JsonArray)item9["latestFilesIndexes"])
											{
												if (compType == ModComp.CompType.DataPack || (item11["modLoader"] != null && modLoaders.Single() == (ModComp.CompLoaderType)JsonCompat.ToObject<int>(item11["modLoader"])))
												{
													string text2 = item11["gameVersion"].ToString();
													if (!(text2 != mcInstance) && (text == null || McVersionComparer.CompareVersion(text, text2) <= -1))
													{
														if (text != text2)
														{
															text = text2;
															list3.Clear();
														}
														list3.Add(JsonCompat.ToObject<int>(item11["fileId"]));
													}
												}
											}
											foreach (int item12 in list3)
											{
												if (!dictionary2.ContainsKey(item12))
												{
													dictionary2[item12] = new List<LocalCompFile>();
												}
												dictionary2[item12].AddRange(dictionary[compProject.Id]);
												dictionary3[item12] = compProject.Slug;
											}
										}
									}
								}
							}
							if (!loader.IsAbortedWithThread(currentTaskId))
							{
								ModBase.Log($"[Mod] 已从 CurseForge 获取本地 Mod 信息，需要获取 {dictionary2.Count} 个用于检查更新的文件信息");
								if (dictionary2.Any())
								{
									JsonArray jsonArray3 = (JsonArray)((JsonObject)ModBase.GetJson(ModDownload.DlModRequest("https://api.curseforge.com/v1/mods/files", "POST", "{\"fileIds\": [" + string.Join(",", dictionary2.Keys) + "]}", "application/json")))["data"];
									Dictionary<LocalCompFile, ModComp.CompFile> dictionary4 = new Dictionary<LocalCompFile, ModComp.CompFile>();
									foreach (JsonObject item13 in jsonArray3)
									{
										ModComp.CompFile compFile2 = new ModComp.CompFile(item13, ModComp.CompType.Mod);
										if (compFile2.Available && int.TryParse(compFile2.Id, out var result) && dictionary2.ContainsKey(result))
										{
											foreach (LocalCompFile item14 in dictionary2[result])
											{
												if (!dictionary4.ContainsKey(item14) || !(dictionary4[item14].ReleaseDate >= compFile2.ReleaseDate))
												{
													dictionary4[item14] = compFile2;
												}
											}
										}
									}
									foreach (KeyValuePair<LocalCompFile, ModComp.CompFile> item15 in dictionary4)
									{
										LocalCompFile key2 = item15.Key;
										ModComp.CompFile value2 = item15.Value;
										if (key2.compFile != null)
										{
											if (ModBase.modeDebug)
											{
												ModBase.Log("[Mod] 本地文件 " + key2.compFile.FileName + " 在 CurseForge 上的最新版为 " + value2.FileName);
											}
											if (!(key2.compFile.ReleaseDate >= value2.ReleaseDate) && !(key2.compFile.Hash == value2.Hash))
											{
												string item = "https://www.curseforge.com/minecraft/mc-mods/" + dictionary3[int.Parse(value2.Id)] + "/files/" + value2.Id;
												if (key2.UpdateFile != null && value2.Hash == key2.UpdateFile.Hash)
												{
													key2.changelogUrls.Add(item);
													key2.UpdateFile.DownloadUrls.AddRange(value2.DownloadUrls);
												}
												else if (key2.UpdateFile == null || value2.ReleaseDate > key2.UpdateFile.ReleaseDate)
												{
													key2.changelogUrls = new List<string> { item };
													key2.UpdateFile = value2;
												}
											}
										}
									}
									ModBase.Log("[Mod] 从 CurseForge 获取 Mod 更新信息结束");
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModBase.Log(ex, "从 CurseForge 获取本地 Mod 信息失败");
				isFailed = true;
			}
			finally
			{
				Interlocked.Increment(ref endedThreadCount);
			}
		}, "Mod List Detail Loader CurseForge");
		while (endedThreadCount < 2)
		{
			if (loader.IsAborted)
			{
				return;
			}
			Thread.Sleep(10);
		}
		List<LocalCompFile> list = mods.Where((LocalCompFile m) => (object)m.Comp != null).ToList();
		ModBase.Log($"[Mod] 联网获取本地 Mod 信息完成，为 {list.Count} 个 Mod 更新缓存");
		if (!list.Any())
		{
			return;
		}
		foreach (LocalCompFile item16 in list)
		{
			item16.compLoaded = !isFailed;
			value[item16.ModrinthHash + mcInstance + string.Join("", modLoaders)] = item16.ToJson();
		}
		ModBase.WriteFile(Path.Combine(ModBase.pathTemp, "Cache", "LocalComp.json"), value.ToJsonString(ModBase.modeDebug ? new JsonSerializerOptions(JsonCompat.SerializerOptions)
		{
			WriteIndented = true
		} : null));
		ModBase.RunInUi(() =>
		{
			PageInstanceCompResource? frmInstanceMod = ModMain.frmInstanceMod;
			if (frmInstanceMod != null && frmInstanceMod.Filter == PageInstanceCompResource.FilterType.CanUpdate)
			{
				ModMain.frmInstanceMod?.RefreshUI();
			}
			else
			{
				ModMain.frmInstanceMod?.RefreshBars();
			}
		});
	}

	public static List<ModComp.CompLoaderType> GetCurrentVersionModLoader()
	{
		List<ModComp.CompLoaderType> list = new List<ModComp.CompLoaderType>();
		if (PageInstanceLeft.McInstance.Info.HasForge)
		{
			list.Add(ModComp.CompLoaderType.Forge);
		}
		if (PageInstanceLeft.McInstance.Info.HasNeoForge)
		{
			list.Add(ModComp.CompLoaderType.NeoForge);
		}
		if (PageInstanceLeft.McInstance.Info.HasFabric)
		{
			list.Add(ModComp.CompLoaderType.Fabric);
		}
		if (PageInstanceLeft.McInstance.Info.HasQuilt)
		{
			list.AddRange(new ModComp.CompLoaderType[2]
			{
				ModComp.CompLoaderType.Fabric,
				ModComp.CompLoaderType.Quilt
			});
		}
		if (PageInstanceLeft.McInstance.Info.HasLiteLoader)
		{
			list.Add(ModComp.CompLoaderType.LiteLoader);
		}
		if (!list.Any())
		{
			list.AddRange(new ModComp.CompLoaderType[5]
			{
				ModComp.CompLoaderType.Forge,
				ModComp.CompLoaderType.NeoForge,
				ModComp.CompLoaderType.Fabric,
				ModComp.CompLoaderType.LiteLoader,
				ModComp.CompLoaderType.Quilt
			});
		}
		return list;
	}

	public static string GetPathNameByCompType(ModComp.CompType theType)
	{
		return theType switch
		{
			ModComp.CompType.Mod => "mods", 
			ModComp.CompType.ResourcePack => "resourcepacks", 
			ModComp.CompType.Shader => "shaderpacks", 
			ModComp.CompType.Schematic => "schematics", 
			ModComp.CompType.World => "saves", 
			_ => "Nothing", 
		};
	}

	public static LocalCompFile GetModLocalCompByKeywords(McInstance instance, string modId, string mainKeyword, params string[] keywords)
	{
		if (modId == null)
		{
			return null;
		}
		return GetModLocalCompByKeywords(instance, new string[1] { modId }, mainKeyword, keywords);
	}

	public static LocalCompFile GetModLocalCompByKeywords(McInstance instance, string[] modIds, string mainKeyword, params string[] keywords)
	{
		if (!instance.Modable)
		{
			return null;
		}
		string path = instance.PathInstance + "mods";
		if (!Directory.Exists(path))
		{
			return null;
		}
		foreach (string item in Directory.EnumerateFiles(path, "*" + mainKeyword + "*"))
		{
			string lowerFilePath = item.ToLower();
			if (regexIsJarFile.IsMatch(lowerFilePath) && (keywords.Length == 0 || keywords.Any((string keyword) => lowerFilePath.Contains(keyword))))
			{
				LocalCompFile localComp = new LocalCompFile(item);
				localComp.Load();
				if (modIds.Any((string modId) => (localComp.ModId ?? "") == (modId ?? "")))
				{
					return localComp;
				}
			}
		}
		return null;
	}
}
