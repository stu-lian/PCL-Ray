using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace PCL.Core.Utils;

public static class RegexPatterns
{
	public static readonly Regex TerracottaId = _TerracottaId();

	public static readonly Regex NewLine = _NewLine();

	public static readonly Regex Iso8601EndOfDay = _Iso8601EndOfDay();

	public static readonly Regex SemVer = _SemVer();

	private const string PatternSemVer = "^v?(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-(?<prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+(?<build>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$";

	public static readonly Regex HttpUri = _HttpUri();

	private const string PatternHttpUri = "^https?://(?:\\[[^\\]\\s]+\\]|[^/\\s?#:]+)(?::\\d{1,5})?(?:/[^\\s?#]*)?(?:\\?[^\\s#]*)?(?:#\\S*)?$";

	public static readonly Regex FullHttpUri = _FullHttpUri();

	private const string PatternFullHttpUri = "^(?<scheme>https?)://(?<host>localhost|(?:(?:25[0-5]|2[0-4]\\d|1?\\d?\\d)(?:\\.(?:25[0-5]|2[0-4]\\d|1?\\d?\\d)){3})|\\[(?<ipv6>[0-9A-Fa-f:.]+)\\]|(?:(?:[A-Za-z0-9](?:[A-Za-z0-9\\-]{0,61}[A-Za-z0-9])?\\.)+(?:[A-Za-z]{2,63}|xn--[A-Za-z0-9\\-]{2,59})))(?::(?<port>6553[0-5]|655[0-2]\\d|65[0-4]\\d{2}|6[0-4]\\d{3}|[1-5]\\d{4}|[1-9]\\d{0,3}))?(?<path>/[^\\s?#]*)?(?:\\?(?<query>[^\\s#]*))?(?:#(?<fragment>[^\\s]*))?$";

	public static readonly Regex LastPendingLogPath = _LastPendingLogPath();

	public static readonly Regex IncompatibleModLoaderErrorHint = _IncompatibleModLoaderErrorHint();

	public static readonly Regex HexColor = _HexColor();

	public static readonly Regex MotdCode = _MotdCode();

	public static readonly Regex BroadcastMotd = _BroadcastMotd();

	public static readonly Regex BroadcastAd = _BroadcastAd();

	public static readonly Regex McNormalVersion = _McNormalVersion();

	public static readonly Regex McSnapshotVersion = _McSnapshotVersion();

	public static readonly Regex McIndevVersion = _McIndevVersion();

	public static readonly Regex McInfdevVersion = _McInfdevVersion();

	public static readonly Regex AccessToken = _AccessToken();

	public static readonly Regex HasChineseChar = _HasChineseChar();

	public static readonly Regex EnglishSpacedKeywords = _EnglishSpacedKeywords();

	public static readonly Regex Ntfs83FileName = _Ntfs83FileName();

	public static readonly Regex UncPath = _UncPath();

	public static readonly Regex OptiFineVersion = _OptiFineVersion();

	public static readonly Regex OptiFineLibVersion = _OptiFineLibVersion();

	public static readonly Regex LegacyFabricVersion = _LegacyFabricVersion();

	public static readonly Regex FabricVersion = _FabricVersion();

	public static readonly Regex QuiltVersion = _QuiltVersion();

	public static readonly Regex CleanroomVersion = _CleanroomVersion();

	public static readonly Regex ForgeMainVersion = _ForgeMainVersion();

	public static readonly Regex ForgeLibVersion = _ForgeLibVersion();

	public static readonly Regex NeoForgeVersion = _NeoForgeVersion();

	public static readonly Regex FabricLikeLibVersion = _FabricLikeLibVersion();

	public static readonly Regex LabyModVersion = _LabyModVersion();

	public static readonly Regex MinecraftJsonVersion = _MinecraftJsonVersion();

	public static readonly Regex MinecraftDownloadUrlVersion = _MinecraftDownloadUrlVersion();

	public static readonly Regex CatchLwjglInLib = _CatchLwjglInLib();

	public static readonly Regex DlNeoForgeVersion = _DlNeoForgeVersion();

	public static readonly Regex ModIdMatch = _ModIdMatch();

	private static Regex _TerracottaId()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___TerracottaId_4.Instance;
	}

	private static Regex _NewLine()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NewLine_5.Instance;
	}

	private static Regex _Iso8601EndOfDay()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___Iso8601EndOfDay_6.Instance;
	}

	private static Regex _SemVer()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___SemVer_7.Instance;
	}

	private static Regex _HttpUri()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___HttpUri_8.Instance;
	}

	private static Regex _FullHttpUri()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FullHttpUri_9.Instance;
	}

	private static Regex _LastPendingLogPath()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___LastPendingLogPath_10.Instance;
	}

	private static Regex _IncompatibleModLoaderErrorHint()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___IncompatibleModLoaderErrorHint_11.Instance;
	}

	private static Regex _HexColor()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___HexColor_12.Instance;
	}

	private static Regex _MotdCode()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MotdCode_13.Instance;
	}

	private static Regex _BroadcastMotd()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___BroadcastMotd_14.Instance;
	}

	private static Regex _BroadcastAd()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___BroadcastAd_15.Instance;
	}

	private static Regex _McNormalVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___McNormalVersion_16.Instance;
	}

	private static Regex _McSnapshotVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___McSnapshotVersion_17.Instance;
	}

	private static Regex _McIndevVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___McIndevVersion_18.Instance;
	}

	private static Regex _McInfdevVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___McInfdevVersion_19.Instance;
	}

	private static Regex _AccessToken()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___AccessToken_20.Instance;
	}

	private static Regex _HasChineseChar()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___HasChineseChar_21.Instance;
	}

	private static Regex _EnglishSpacedKeywords()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___EnglishSpacedKeywords_22.Instance;
	}

	private static Regex _Ntfs83FileName()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___Ntfs83FileName_23.Instance;
	}

	private static Regex _UncPath()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___UncPath_24.Instance;
	}

	private static Regex _OptiFineVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___OptiFineVersion_25.Instance;
	}

	private static Regex _OptiFineLibVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___OptiFineVersion_25.Instance;
	}

	private static Regex _LegacyFabricVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___LegacyFabricVersion_26.Instance;
	}

	private static Regex _FabricVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___LegacyFabricVersion_26.Instance;
	}

	private static Regex _QuiltVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___QuiltVersion_27.Instance;
	}

	private static Regex _CleanroomVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CleanroomVersion_28.Instance;
	}

	private static Regex _ForgeMainVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeMainVersion_29.Instance;
	}

	private static Regex _ForgeLibVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ForgeLibVersion_30.Instance;
	}

	private static Regex _NeoForgeVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___NeoForgeVersion_31.Instance;
	}

	private static Regex _FabricLikeLibVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___FabricLikeLibVersion_32.Instance;
	}

	private static Regex _LabyModVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___LabyModVersion_33.Instance;
	}

	private static Regex _MinecraftJsonVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftJsonVersion_34.Instance;
	}

	private static Regex _MinecraftDownloadUrlVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___MinecraftDownloadUrlVersion_35.Instance;
	}

	private static Regex _CatchLwjglInLib()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___CatchLwjglInLib_36.Instance;
	}

	private static Regex _DlNeoForgeVersion()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___DlNeoForgeVersion_37.Instance;
	}

	private static Regex _ModIdMatch()
	{
		return _003CRegexGenerator_g_003EF4C1BC19F7A3FA6C690C40F3C5F1F2C64ABEDB43FC3F54080F036AB3ED7DF0B37___ModIdMatch_38.Instance;
	}
}
