using System;
using System.IO;

namespace PCL.Core.App;

public static class Paths
{
	private static string _data;

	private static string _sharedData;

	private static string _sharedLocalData;

	private static string _temp;

	public static string DefaultDirectory => Basics.ExecutableDirectory;

	public static string Data
	{
		get
		{
			return _data;
		}
		set
		{
			_data = value;
		}
	}

	public static string SharedData
	{
		get
		{
			return _sharedData;
		}
		set
		{
			_sharedData = value;
		}
	}

	public static string OldSharedData { get; set; }

	public static string SharedLocalData
	{
		get
		{
			return _sharedLocalData;
		}
		set
		{
			_sharedLocalData = value;
		}
	}

	public static string Temp
	{
		get
		{
			return _temp;
		}
		set
		{
			_temp = value;
		}
	}

	public static string GetSpecialPath(Environment.SpecialFolder folder, string relative)
	{
		return Path.Combine(Environment.GetFolderPath(folder), relative);
	}

	static Paths()
	{
		_data = Path.Combine(DefaultDirectory, "PCL");
		_sharedData = GetSpecialPath(Environment.SpecialFolder.ApplicationData, "PCLCE");
		_sharedLocalData = GetSpecialPath(Environment.SpecialFolder.LocalApplicationData, "PCLCE");
		_temp = Path.Combine(Path.GetTempPath(), "PCLCE");
		OldSharedData = GetSpecialPath(Environment.SpecialFolder.ApplicationData, ".PCLCE");
		Directory.CreateDirectory(_data);
		Directory.CreateDirectory(_sharedData);
		Directory.CreateDirectory(_sharedLocalData);
		Directory.CreateDirectory(_temp);
	}
}
