using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PCL.Core.Logging;
using PCL.Core.Utils;

namespace PCL.Core.Minecraft.Java.Parser;

public class PeHeaderParser : IJavaParser
{
	private static readonly Dictionary<string, JavaBrandType> _BrandMap = new Dictionary<string, JavaBrandType>
	{
		["Eclipse"] = JavaBrandType.EclipseTemurin,
		["Temurin"] = JavaBrandType.EclipseTemurin,
		["Bellsoft"] = JavaBrandType.Liberica,
		["Microsoft"] = JavaBrandType.Microsoft,
		["Amazon"] = JavaBrandType.Corretto,
		["Azul"] = JavaBrandType.Zulu,
		["IBM"] = JavaBrandType.IBMSemeru,
		["Oracle"] = JavaBrandType.Oracle,
		["Tencent"] = JavaBrandType.TencentKona,
		["OpenJDK"] = JavaBrandType.OpenJDK,
		["Alibaba"] = JavaBrandType.Dragonwell,
		["GraalVM"] = JavaBrandType.GraalVmCommunity,
		["JetBrains"] = JavaBrandType.JetBrains
	};

	public JavaInstallation? Parse(string javaExePath)
	{
		try
		{
			if (!File.Exists(javaExePath))
			{
				return null;
			}
			LogWrapper.Info("Java", "解析 " + javaExePath + " 的 Java 程序信息");
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(javaExePath);
			Version version = Version.Parse(versionInfo.FileVersion ?? "0.0.0.0");
			JavaBrandType brand = _DetermineBrand(_NormalizeCompanyName(versionInfo));
			string? directoryName = Path.GetDirectoryName(javaExePath);
			bool flag = !File.Exists(Path.Combine(directoryName, "javac.exe"));
			MachineType machine = PEHeaderReader.ReadPEHeader(javaExePath).Machine;
			bool is64Bit = PEHeaderReader.IsMachine64Bit(machine);
			string path = Path.Combine(Directory.GetParent(directoryName).FullName, "lib");
			if (!flag && File.Exists(Path.Combine(path, "jvm.lib")))
			{
				_ = 1;
			}
			else if (flag)
			{
				File.Exists(Path.Combine(path, "rt.jar"));
			}
			else
				_ = 0;
			return new JavaInstallation(directoryName, version, brand, machine, is64Bit, flag);
		}
		catch (Exception ex)
		{
			LogWrapper.Error(ex, "[Java] 解析 " + javaExePath + " 时出错");
			return null;
		}
	}

	private static string _NormalizeCompanyName(FileVersionInfo info)
	{
		string text = info.CompanyName ?? info.FileDescription ?? info.ProductName ?? string.Empty;
		if (text.Contains("Oracle", StringComparison.OrdinalIgnoreCase) || text == "N/A")
		{
			string? fileDescription = info.FileDescription;
			if (fileDescription == null || !fileDescription.Contains("Java(TM)", StringComparison.OrdinalIgnoreCase))
			{
				string? productName = info.ProductName;
				if (productName == null || !productName.Contains("Java(TM)", StringComparison.OrdinalIgnoreCase))
				{
					return "OpenJDK";
				}
			}
			return "Oracle";
		}
		return text;
	}

	private static JavaBrandType _DetermineBrand(string output)
	{
		string text = _BrandMap.Keys.FirstOrDefault((string k) => output.Contains(k, StringComparison.OrdinalIgnoreCase));
		if (text == null)
		{
			return JavaBrandType.Unknown;
		}
		return _BrandMap[text];
	}
}
