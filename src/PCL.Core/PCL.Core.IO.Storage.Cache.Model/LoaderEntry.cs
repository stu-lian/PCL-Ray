namespace PCL.Core.IO.Storage.Cache.Model;

public record LoaderEntry
{
	public string Type { get; init; } = string.Empty;

	public string? Version { get; init; }
}
