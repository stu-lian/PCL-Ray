using System;

namespace PCL.Core.Minecraft.Java;

public class JavaConsts
{
	public static readonly string[] ExcludeFolderNames = new string[4] { "javapath", "java8path", "common files", "netease" };

	public static readonly string[] MostPossibleKeywords = new string[16]
	{
		"java", "jdk", "jre", "dragonwell", "azul", "zulu", "oracle", "open", "amazon", "corretto",
		"eclipse", "temurin", "hotspot", "semeru", "kona", "bellsoft"
	};

	public static readonly string[] PossibleKeywords = new string[11]
	{
		"environment", "env", "runtime", "x86_64", "amd64", "arm64", "x64", "pcl", "hmcl", "baka",
		"minecraft"
	};

	public static readonly string[] AllKeywords;

	static JavaConsts()
	{
		string[] possibleKeywords = PossibleKeywords;
		string[] mostPossibleKeywords = MostPossibleKeywords;
		int num = 0;
		string[] array = new string[possibleKeywords.Length + mostPossibleKeywords.Length];
		ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(possibleKeywords);
		readOnlySpan.CopyTo(new Span<string>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		ReadOnlySpan<string> readOnlySpan2 = new ReadOnlySpan<string>(mostPossibleKeywords);
		readOnlySpan2.CopyTo(new Span<string>(array).Slice(num, readOnlySpan2.Length));
		num += readOnlySpan2.Length;
		AllKeywords = array;
	}
}
