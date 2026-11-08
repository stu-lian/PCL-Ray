using System;

namespace PCL.Core.App.Configuration;

[Flags]
public enum ConfigEvent
{
	Init = 1,
	Get = 2,
	Set = 4,
	Reset = 8,
	CheckDefault = 0x10,
	None = 0,
	Read = Get | CheckDefault,
	Update = Set | Reset,
	Changed = Update | Init,
	All = Changed | Read
}
