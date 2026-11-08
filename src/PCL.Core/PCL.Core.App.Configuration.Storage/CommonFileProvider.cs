using System.IO;
using PCL.Core.Utils;

namespace PCL.Core.App.Configuration.Storage;

public abstract class CommonFileProvider(string path) : IKeyValueFileProvider
{
	public string FilePath { get; set; } = path;

	public abstract T Get<T>(string key);

	public abstract void Set<T>(string key, T value);

	public abstract bool Exists(string key);

	public abstract void Remove(string key);

	protected abstract void WriteToStream(Stream stream);

	public void Sync()
	{
		if (!File.Exists(FilePath))
		{
			Directory.CreateDirectory(Basics.GetParentPath(FilePath));
		}
		string text = $"{FilePath}.tmp{RandomUtils.NextInt(1, 99999):00000}";
		string destinationBackupFileName = FilePath + ".bak";
		using (FileStream fileStream = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.Read))
		{
			WriteToStream(fileStream);
			fileStream.Flush(flushToDisk: true);
		}
		if (File.Exists(FilePath))
		{
			File.Replace(text, FilePath, destinationBackupFileName);
		}
		else
		{
			File.Move(text, FilePath);
		}
	}
}
