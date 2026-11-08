using System;

namespace PCL.Core.Utils;

[Serializable]
public struct PEStruct
{
	public MachineType Machine;

	public ushort NumberOfSections;

	public uint TimeDateStamp;

	public uint PointerToSymbolTable;

	public uint NumberOfSymbols;

	public ushort SizeOfOptionalHeader;

	public ushort Characteristics;

	public bool IsValid;

	public string ErrorMessage;
}
