using PCL.Core.Utils.Exts;
using PCL.Core.Utils.OS;

namespace PCL.Core.App;

public static class Secrets
{
	public static string MSOAuthClientId { get; } = EnvironmentInterop.GetSecret("MS_CLIENT_ID", readEnv: true).ReplaceNullOrEmpty();

	public static string CurseForgeAPIKey { get; } = EnvironmentInterop.GetSecret("CURSEFORGE_API_KEY", readEnv: true).ReplaceNullOrEmpty();

	public static string TelemetryKey { get; } = EnvironmentInterop.GetSecret("TELEMETRY_KEY", readEnv: true).ReplaceNullOrEmpty();

	public static string NatayarkClientId { get; } = EnvironmentInterop.GetSecret("NAID_CLIENT_ID", readEnv: true).ReplaceNullOrEmpty();

	public static string NatayarkClientSecret { get; } = EnvironmentInterop.GetSecret("NAID_CLIENT_SECRET", readEnv: true).ReplaceNullOrEmpty();

	public static string[] LinkServers { get; } = EnvironmentInterop.GetSecret("LINK_SERVER_ROOT", readEnv: true).ReplaceNullOrEmpty().Split("|");

	public static string CommitHash { get; } = EnvironmentInterop.GetSecret("GITHUB_SHA", readEnv: true).ReplaceNullOrEmpty();
}
