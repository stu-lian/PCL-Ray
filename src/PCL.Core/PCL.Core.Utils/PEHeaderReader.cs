using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PCL.Core.Utils;

public static class PEHeaderReader
{
	private const int PE_POINTER_OFFSET = 60;

	private const uint PE_SIGNATURE = 17744u;

	public static PEStruct ReadPEHeader(string filePath)
	{
		PEStruct result = default;
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			result.ErrorMessage = "文件不存在或路径无效";
			return result;
		}
		try
		{
			using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			if (!_IsValidDosHeader(fileStream))
			{
				result.ErrorMessage = "无效的DOS头(MZ签名)";
				return result;
			}
			long num = _GetPEOffset(fileStream);
			if (num <= 0 || num >= fileStream.Length - 24)
			{
				result.ErrorMessage = "无效的PE头偏移量";
				return result;
			}
			fileStream.Seek(num, SeekOrigin.Begin);
			if (!_IsValidPESignature(fileStream))
			{
				result.ErrorMessage = "无效的PE签名";
				return result;
			}
			result = _ParseImageFileHeader(fileStream);
			result.IsValid = true;
		}
		catch (Exception ex)
		{
			result.IsValid = false;
			result.ErrorMessage = "读取失败: " + ex.Message;
		}
		return result;
	}

	private static bool _IsValidDosHeader(FileStream fs)
	{
		if (fs.Length < 2)
		{
			return false;
		}
		fs.Seek(0L, SeekOrigin.Begin);
		if (fs.ReadByte() == 77)
		{
			return fs.ReadByte() == 90;
		}
		return false;
	}

	private static long _GetPEOffset(FileStream fs)
	{
		fs.Seek(60L, SeekOrigin.Begin);
		using BinaryReader binaryReader = new BinaryReader(fs, Encoding.Default, leaveOpen: true);
		return binaryReader.ReadInt32();
	}

	private static bool _IsValidPESignature(FileStream fs)
	{
		using BinaryReader binaryReader = new BinaryReader(fs, Encoding.Default, leaveOpen: true);
		return binaryReader.ReadUInt32() == 17744;
	}

	private static PEStruct _ParseImageFileHeader(FileStream fs)
	{
		using BinaryReader binaryReader = new BinaryReader(fs, Encoding.Default, leaveOpen: true);
		return new PEStruct
		{
			Machine = (MachineType)binaryReader.ReadUInt16(),
			NumberOfSections = binaryReader.ReadUInt16(),
			TimeDateStamp = binaryReader.ReadUInt32(),
			PointerToSymbolTable = binaryReader.ReadUInt32(),
			NumberOfSymbols = binaryReader.ReadUInt32(),
			SizeOfOptionalHeader = binaryReader.ReadUInt16(),
			Characteristics = binaryReader.ReadUInt16()
		};
	}

	public static bool IsMachine64Bit(MachineType machine)
	{
		return new List<MachineType>
		{
			MachineType.IA64,
			MachineType.ARM64,
			MachineType.AMD64
		}.Contains(machine);
	}
}
