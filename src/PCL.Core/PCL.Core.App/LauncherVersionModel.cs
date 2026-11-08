using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using PCL.Core.Utils.OS;

namespace PCL.Core.App;

public sealed record LauncherVersionModel
{
	[JsonPropertyName("base")]
	public string BaseVersion { get; init; }

	[JsonPropertyName("suffix")]
	public string Suffix { get; init; }

	[JsonPropertyName("code")]
	public int Code { get; init; }

	[JsonPropertyName("upstream")]
	public string UpstreamVersion { get; init; }

	public string BaseName { get; }

	public string BranchName { get; }

	public int BranchCode { get; }

	public string StandardVersion { get; }

	public string Commit { get; }

	public string CommitDigest { get; }

	private static readonly (string Name, int Code) _CompilationBranchInfo = (Name: "Publish", Code: 0);

	private static readonly string? _SecretCommitInfo = EnvironmentInterop.GetSecret("GITHUB_SHA", readEnv: false);

	public LauncherVersionModel(string BaseVersion, string Suffix, int Code, string UpstreamVersion)
	{
		this.BaseVersion = BaseVersion;
		this.Suffix = Suffix;
		this.Code = Code;
		this.UpstreamVersion = UpstreamVersion;
		BaseName = BaseVersion + (string.IsNullOrWhiteSpace(Suffix) ? "" : ("-" + Suffix));
		BranchName = _CompilationBranchInfo.Name;
		BranchCode = _CompilationBranchInfo.Code;
		StandardVersion = BaseVersion + "." + _CompilationBranchInfo.Code;
		Commit = _SecretCommitInfo ?? "native";
		CommitDigest = _SecretCommitInfo?.Substring(0, 7) ?? "native";
	}

	public override string ToString()
	{
		return $"{BranchName} {BaseName} ({Code}, {CommitDigest})";
	}

	[CompilerGenerated]
	public void Deconstruct(out string BaseVersion, out string Suffix, out int Code, out string UpstreamVersion)
	{
		BaseVersion = this.BaseVersion;
		Suffix = this.Suffix;
		Code = this.Code;
		UpstreamVersion = this.UpstreamVersion;
	}
}
