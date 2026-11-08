using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.IO;
using PCL.Core.IO.Net.Http;
using PCL.Core.Link;
using PCL.Core.Logging;

namespace PCL;

public static class ModTerracotta
{
	public enum TerracottaStatus
	{
		Idle, Starting, Downloading, Waiting, HostScanning, HostStarting, HostReady,
		GuestConnecting, GuestStarting, GuestReady, Error, Fatal
	}

	public sealed record TerracottaPlayer(string MachineId, string Name, string Vendor, string Kind);

	public sealed class TerracottaState
	{
		public TerracottaStatus Status { get; set; }
		public int? HttpPort { get; set; }
		public string? RoomCode { get; set; }
		public int? ServerPort { get; set; }
		public List<TerracottaPlayer> Players { get; set; } = new List<TerracottaPlayer>();
		public bool BinaryInstalled { get; set; }
		public string? InstalledVersion { get; set; }
		public string? ErrorMessage { get; set; }
	}

	public const string DefaultVersion = "0.4.2";

	private static readonly SemaphoreSlim OperationLock = new SemaphoreSlim(1, 1);
	private static readonly object StateLock = new object();
	private static readonly HttpClient LocalClient = new HttpClient(new SocketsHttpHandler { UseProxy = false, ConnectTimeout = TimeSpan.FromSeconds(3) }) { Timeout = TimeSpan.FromSeconds(10) };
	private static TerracottaState _state = new TerracottaState();
	private static Process? _process;
	private static CancellationTokenSource? _pollCts;
	private static BroadcastLocal? _lanBroadcast;
	private static int? _announcedPort;
	private static readonly StringBuilder _processOutput = new StringBuilder();

	public static event Action? StateChanged;

	static ModTerracotta()
	{
		Directory.CreateDirectory(TerracottaTemp);
		TryMigrateLegacyInstall();
	}

	private static void TryMigrateLegacyInstall()
	{
		try
		{
			if (File.Exists(BinaryPath) || !File.Exists(Path.Combine(LegacyInstallDirectory, "terracotta.exe")))
			{
				return;
			}
			Directory.CreateDirectory(InstallDirectory);
			foreach (string file in Directory.EnumerateFiles(LegacyInstallDirectory))
			{
				string name = Path.GetFileName(file);
				if (name.StartsWith("terracotta", StringComparison.OrdinalIgnoreCase) || name.Equals("VCRUNTIME140.DLL", StringComparison.OrdinalIgnoreCase))
				{
					File.Copy(file, Path.Combine(InstallDirectory, name), true);
				}
			}
			LogWrapper.Info("[Terracotta] Migrated legacy install to " + InstallDirectory);
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "[Terracotta] Failed to migrate legacy install");
		}
	}

	public static bool IsHost => GetState().Status is TerracottaStatus.HostReady or TerracottaStatus.HostStarting or TerracottaStatus.HostScanning;
	public static bool IsConnected => GetState().Status is TerracottaStatus.HostReady or TerracottaStatus.GuestReady;

	public static string InstallDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCLRay", "Terracotta");
	public static string TerracottaTemp => Path.Combine(Path.GetTempPath(), "PCLRay");
	private static string LegacyInstallDirectory => Path.Combine(Paths.SharedLocalData, "Terracotta");
	public static string BinaryPath => Path.Combine(InstallDirectory, "terracotta.exe");
	private static string VersionFile => Path.Combine(InstallDirectory, "terracotta-version.json");

	private static string PlatformKey => System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "windows-arm64" : "windows-x86_64";

	public static TerracottaState GetState()
	{
		lock (StateLock)
		{
			return new TerracottaState
			{
				Status = _state.Status,
				HttpPort = _state.HttpPort,
				RoomCode = _state.RoomCode,
				ServerPort = _state.ServerPort,
				Players = _state.Players.ToList(),
				BinaryInstalled = File.Exists(BinaryPath) && IsExecutable(BinaryPath),
				InstalledVersion = ReadInstalledVersion(),
				ErrorMessage = _state.ErrorMessage
			};
		}
	}

	private static void UpdateState(Action<TerracottaState> action)
	{
		lock (StateLock)
		{
			action(_state);
		}
		StateChanged?.Invoke();
	}

	public static string? ReadInstalledVersion()
	{
		try
		{
			if (!File.Exists(VersionFile))
			{
				return null;
			}
			return JsonNode.Parse(File.ReadAllText(VersionFile))?["version"]?.ToString();
		}
		catch
		{
			return null;
		}
	}

	public static bool IsExecutable(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return false;
			}
			using FileStream stream = File.OpenRead(path);
			return stream.ReadByte() == 'M' && stream.ReadByte() == 'Z';
		}
		catch
		{
			return false;
		}
	}

	public static async Task<string> GetLatestVersionAsync()
	{
		string[] endpoints =
		{
			"https://gitee.com/api/v5/repos/burningtnt/Terracotta/releases/latest",
			"https://api.github.com/repos/burningtnt/Terracotta/releases/latest"
		};
		List<string> errors = new List<string>();
		foreach (string endpoint in endpoints)
		{
			try
			{
				using HttpResponseMessage response = await HttpRequest.Create(endpoint).WithHeader("Accept", "application/json").SendAsync();
				if (!response.IsSuccess)
				{
					errors.Add(endpoint + ": HTTP " + (int)response.StatusCode);
					continue;
				}
				string? tag = JsonNode.Parse(await response.AsStringAsync())?["tag_name"]?.ToString();
				if (!string.IsNullOrWhiteSpace(tag))
				{
					return tag.TrimStart('v', 'V');
				}
				errors.Add(endpoint + ": missing tag_name");
			}
			catch (Exception ex)
			{
				errors.Add(endpoint + ": " + ex.Message);
			}
		}
		throw new InvalidOperationException(string.Join("; ", errors));
	}

	private static bool IsSafeVersion(string version)
	{
		return !string.IsNullOrWhiteSpace(version) && version.Length <= 64 && version.All(c => char.IsAsciiLetterOrDigit(c) || c == '.' || c == '-' || c == '_');
	}

	public static async Task DownloadAsync(string? version = null, Action<double>? progress = null)
	{
		await OperationLock.WaitAsync();
		try
		{
			UpdateState(delegate(TerracottaState s)
			{
				s.Status = TerracottaStatus.Downloading;
				s.ErrorMessage = null;
			});
			string targetVersion;
			if (!string.IsNullOrWhiteSpace(version))
			{
				targetVersion = version!.TrimStart('v', 'V');
			}
			else
			{
				try
				{
					targetVersion = await GetLatestVersionAsync();
				}
				catch (Exception ex)
				{
					LogWrapper.Warn(ex, "[Terracotta] Failed to query latest version, falling back to " + DefaultVersion);
					targetVersion = DefaultVersion;
				}
			}
			if (!IsSafeVersion(targetVersion))
			{
				throw new InvalidOperationException("Invalid terracotta version: " + targetVersion);
			}
			Directory.CreateDirectory(InstallDirectory);
			Directory.CreateDirectory(TerracottaTemp);
			string archive = Path.Combine(TerracottaTemp, "terracotta-" + targetVersion + "-" + PlatformKey + "-pkg.tar.gz");
			string artifact = $"terracotta-{targetVersion}-{PlatformKey}-pkg.tar.gz";
			string githubUrl = $"https://github.com/burningtnt/Terracotta/releases/download/v{targetVersion}/{artifact}";
			string[] urls =
			{
				$"https://gitee.com/burningtnt/Terracotta/releases/download/v{targetVersion}/{artifact}",
				githubUrl,
				"https://ghfast.top/" + githubUrl,
				"https://gh-proxy.com/" + githubUrl,
				"https://ghproxy.net/" + githubUrl,
				"https://mirror.ghproxy.com/" + githubUrl
			};
			Exception? lastError = null;
			foreach (string url in urls)
			{
				try
				{
					using HttpResponseMessage response = await HttpRequest.Create(url).SendAsync(httpCompletionOption: HttpCompletionOption.ResponseHeadersRead, retryTimes: 2);
					if (!response.IsSuccess)
					{
						lastError = new HttpRequestException($"HTTP {(int)response.StatusCode} from {url}");
						continue;
					}
					await using Stream input = await response.AsStreamAsync();
					await using FileStream output = new FileStream(archive, FileMode.Create, FileAccess.Write, FileShare.None);
					byte[] buffer = new byte[81920];
					long total = response.Content.Headers.ContentLength ?? -1;
					long written = 0;
					int read;
					while ((read = await input.ReadAsync(buffer)) > 0)
					{
						await output.WriteAsync(buffer.AsMemory(0, read));
						written += read;
						if (total > 0)
						{
							progress?.Invoke((double)written / total);
						}
					}
					lastError = null;
					break;
				}
				catch (Exception ex)
				{
					lastError = ex;
				}
			}
			if (lastError != null)
			{
				throw new InvalidOperationException("All terracotta download sources failed: " + lastError.Message, lastError);
			}
			string staging = Path.Combine(TerracottaTemp, "terracotta-staging-" + Guid.NewGuid().ToString("N"));
			List<string> extractedFiles = new List<string>();
			try
			{
				Directory.CreateDirectory(staging);
				await ExtractTarGzAsync(archive, staging);
				extractedFiles.AddRange(Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories));
				string? candidate = extractedFiles.FirstOrDefault(IsExecutable);
				if (candidate == null)
				{
					string names = string.Join(", ", extractedFiles.Select(Path.GetFileName));
					throw new InvalidOperationException("No valid terracotta executable found in downloaded archive. Files: " + names);
				}
				File.Copy(candidate, BinaryPath, true);
				foreach (string file in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories))
				{
					string name = Path.GetFileName(file);
					if (string.IsNullOrWhiteSpace(name) || name.Equals("terracotta-version.json", StringComparison.OrdinalIgnoreCase) || string.Equals(Path.GetFullPath(file), Path.GetFullPath(candidate), StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					File.Copy(file, Path.Combine(InstallDirectory, name), true);
				}
				File.WriteAllText(VersionFile, new JsonObject { ["version"] = targetVersion }.ToJsonString());
				CleanupInstallDirectory();
			}
			finally
			{
				TryDeleteFile(archive);
				try
				{
					if (Directory.Exists(staging))
					{
						Directory.Delete(staging, true);
					}
				}
				catch
				{
				}
			}
			progress?.Invoke(1.0);
			UpdateState(delegate(TerracottaState s)
			{
				s.Status = TerracottaStatus.Idle;
				s.InstalledVersion = targetVersion;
				s.BinaryInstalled = true;
			});
		}
		catch (Exception ex)
		{
			UpdateState(delegate(TerracottaState s)
			{
				s.Status = TerracottaStatus.Error;
				s.ErrorMessage = ex.Message;
			});
			throw;
		}
		finally
		{
			OperationLock.Release();
		}
	}

	private static async Task ExtractTarGzAsync(string archive, string destination)
	{
		await using FileStream file = new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.Read);
		await using System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionMode.Decompress);
		await TarFile.ExtractToDirectoryAsync(gzip, destination, overwriteFiles: true);
	}

	private static void TryDeleteFile(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private static void CleanupInstallDirectory()
	{
		foreach (string file in Directory.EnumerateFiles(InstallDirectory))
		{
			string name = Path.GetFileName(file);
			if (name.StartsWith("terracotta-", StringComparison.OrdinalIgnoreCase) && !name.Equals("terracotta-version.json", StringComparison.OrdinalIgnoreCase) && !name.Equals("terracotta.exe", StringComparison.OrdinalIgnoreCase))
			{
				TryDeleteFile(file);
			}
		}
	}

	public static async Task EnsureRunningAsync()
	{
		TerracottaState state = GetState();
		if (state.HttpPort.HasValue && state.Status != TerracottaStatus.Error && state.Status != TerracottaStatus.Fatal)
		{
			return;
		}
		if (!File.Exists(BinaryPath) || !IsExecutable(BinaryPath))
		{
			await DownloadAsync();
		}
		await StartProcessAsync();
	}

	public static async Task StartProcessAsync()
	{
		await OperationLock.WaitAsync();
		try
		{
			if (_process != null && !_process.HasExited && GetState().HttpPort.HasValue)
			{
				return;
			}
			StopProcessInternal();
			UpdateState(delegate(TerracottaState s)
			{
				s.Status = TerracottaStatus.Starting;
				s.ErrorMessage = null;
				s.RoomCode = null;
				s.ServerPort = null;
				s.Players = new List<TerracottaPlayer>();
			});
			string portFile = Path.Combine(TerracottaTemp, "terracotta_port_" + Environment.ProcessId + ".json");
			TryDeleteFile(portFile);
			lock (_processOutput)
			{
				_processOutput.Clear();
			}
			ProcessStartInfo info = new ProcessStartInfo
			{
				FileName = BinaryPath,
				Arguments = "--hmcl \"" + portFile + "\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			Process process = new Process { StartInfo = info, EnableRaisingEvents = true };
			process.OutputDataReceived += (_, e) => CaptureOutput(e.Data);
			process.ErrorDataReceived += (_, e) => CaptureOutput(e.Data);
			if (!process.Start())
			{
				throw new InvalidOperationException("Failed to start terracotta.");
			}
			_process = process;
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			int port = 0;
			for (int attempt = 0; attempt < 30; attempt++)
			{
				await Task.Delay(500);
				if (File.Exists(portFile))
				{
					try
					{
						string json = await File.ReadAllTextAsync(portFile);
						port = JsonNode.Parse(json)?["port"]?.GetValue<int>() ?? 0;
						if (port > 0)
						{
							break;
						}
					}
					catch
					{
					}
				}
				if (process.HasExited)
				{
					await Task.Delay(250);
					try
					{
						if (File.Exists(portFile))
						{
							string json = await File.ReadAllTextAsync(portFile);
							port = JsonNode.Parse(json)?["port"]?.GetValue<int>() ?? 0;
						}
					}
					catch
					{
					}
					if (port > 0)
					{
						break;
					}
					throw new InvalidOperationException("terracotta exited before startup completed.\n" + GetCapturedOutput());
				}
			}
			TryDeleteFile(portFile);
			if (port <= 0)
			{
				throw new InvalidOperationException("Timed out waiting for terracotta to start.\n" + GetCapturedOutput());
			}
			UpdateState(delegate(TerracottaState s)
			{
				s.HttpPort = port;
				s.Status = TerracottaStatus.Waiting;
			});
			StartPolling(port);
		}
		catch (Exception ex)
		{
			UpdateState(delegate(TerracottaState s)
			{
				s.Status = TerracottaStatus.Error;
				s.ErrorMessage = ex.Message;
			});
			StopProcessInternal();
			throw;
		}
		finally
		{
			OperationLock.Release();
		}
	}

	private static string GetCapturedOutput()
	{
		lock (_processOutput)
		{
			return _processOutput.ToString();
		}
	}

	private static void CaptureOutput(string? line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return;
		}
		lock (_processOutput)
		{
			_processOutput.AppendLine(line);
			if (_processOutput.Length > 16000)
			{
				_processOutput.Remove(0, _processOutput.Length - 12000);
			}
		}
		LogWrapper.Info("[Terracotta] " + line);
	}

	private static void StartPolling(int port)
	{
		_pollCts?.Cancel();
		_pollCts?.Dispose();
		_pollCts = new CancellationTokenSource();
		CancellationToken token = _pollCts.Token;
		_ = Task.Run(() => PollLoopAsync(port, token));
	}

	private static async Task PollLoopAsync(int port, CancellationToken token)
	{
		int failures = 0;
		while (!token.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(500, token);
				using HttpResponseMessage response = await LocalClient.GetAsync($"http://127.0.0.1:{port}/state", token);
				if (!response.IsSuccessStatusCode)
				{
					if (++failures >= 3)
					{
						SetPollingFailure("Terracotta state request failed: HTTP " + (int)response.StatusCode);
						return;
					}
					continue;
				}
				JsonNode? node = JsonNode.Parse(await response.Content.ReadAsStringAsync(token));
				if (node == null)
				{
					continue;
				}
				failures = 0;
				TerracottaStatus status = ParseStatus(node["state"]?.ToString() ?? "idle");
				int? serverPort = status == TerracottaStatus.GuestReady ? ParsePort(node["url"]?.ToString()) : null;
				List<TerracottaPlayer> players = new List<TerracottaPlayer>();
				if (node["profiles"] is JsonArray profiles)
				{
					foreach (JsonNode? profile in profiles)
					{
						if (profile == null)
						{
							continue;
						}
						players.Add(new TerracottaPlayer(
							profile["machine_id"]?.ToString() ?? "",
							profile["name"]?.ToString() ?? "",
							profile["vendor"]?.ToString() ?? "",
							profile["kind"]?.ToString() ?? "UNKNOWN"));
					}
				}
				string? error = status is TerracottaStatus.Fatal or TerracottaStatus.Error ? node["url"]?.ToString() : null;
				UpdateState(delegate(TerracottaState s)
				{
					s.Status = status;
					s.HttpPort = port;
					s.RoomCode = node["room"]?.ToString();
					s.ServerPort = serverPort;
					s.Players = players;
					s.ErrorMessage = error;
				});
				SyncLanBroadcast(serverPort);
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception ex)
			{
				LogWrapper.Warn(ex, "[Terracotta] Failed to poll state");
				if (++failures >= 3)
				{
					SetPollingFailure(ex.Message);
					return;
				}
			}
		}
	}

	private static void SetPollingFailure(string message)
	{
		UpdateState(delegate(TerracottaState s)
		{
			if (s.Status != TerracottaStatus.Idle)
			{
				s.Status = TerracottaStatus.Error;
				s.ErrorMessage = message;
			}
			s.HttpPort = null;
			s.ServerPort = null;
		});
		SyncLanBroadcast(null);
		StopProcessInternal();
	}

	private static TerracottaStatus ParseStatus(string value)
	{
		return value switch
		{
			"idle" => TerracottaStatus.Idle,
			"starting" => TerracottaStatus.Starting,
			"waiting" => TerracottaStatus.Waiting,
			"host-scanning" => TerracottaStatus.HostScanning,
			"host-starting" => TerracottaStatus.HostStarting,
			"host-ok" => TerracottaStatus.HostReady,
			"guest-connecting" => TerracottaStatus.GuestConnecting,
			"guest-starting" => TerracottaStatus.GuestStarting,
			"guest-ok" => TerracottaStatus.GuestReady,
			"exception" => TerracottaStatus.Error,
			"fatal" => TerracottaStatus.Fatal,
			_ => TerracottaStatus.Error
		};
	}

	private static int? ParsePort(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		string text = value.Trim();
		int colon = text.LastIndexOf(':');
		return colon >= 0 && int.TryParse(text.Substring(colon + 1), out int parsed) ? parsed : null;
	}

	private static void SyncLanBroadcast(int? port)
	{
		if (_announcedPort == port)
		{
			return;
		}
		_announcedPort = port;
		try
		{
			_lanBroadcast?.Stop();
			_lanBroadcast = null;
			if (port.HasValue)
			{
				_lanBroadcast = new BroadcastLocal("Terracotta | PCL Ray Multiplayer", port.Value);
				_lanBroadcast.Start();
			}
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "[Terracotta] Failed to sync LAN announcement");
		}
	}

	public static async Task HostAsync(string playerName)
	{
		await EnsureRunningAsync();
		string port = GetState().HttpPort?.ToString() ?? throw new InvalidOperationException("Terracotta is not running.");
		await SendRoomRequestAsync(port, "/state/scanning", "", playerName);
	}

	public static async Task JoinAsync(string roomCode, string playerName)
	{
		string code = NormalizeRoomCode(roomCode);
		await EnsureRunningAsync();
		string port = GetState().HttpPort?.ToString() ?? throw new InvalidOperationException("Terracotta is not running.");
		await SendRoomRequestAsync(port, "/state/guesting", code, playerName);
	}

	private static async Task SendRoomRequestAsync(string port, string path, string room, string playerName)
	{
		string url = $"http://127.0.0.1:{port}{path}?room={Uri.EscapeDataString(room)}&player={Uri.EscapeDataString(playerName.Trim())}";
		using HttpResponseMessage response = await LocalClient.GetAsync(url);
		string body = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"Terracotta returned HTTP {(int)response.StatusCode}: {body}");
		}
	}

	public static string NormalizeRoomCode(string code)
	{
		string trimmed = (code ?? "").Trim();
		return trimmed.StartsWith("u/", StringComparison.OrdinalIgnoreCase) ? "U/" + trimmed.Substring(2) : trimmed;
	}

	public static bool IsValidRoomCode(string code)
	{
		string value = NormalizeRoomCode(code);
		if (!value.StartsWith("U/", StringComparison.Ordinal) || value.Length != 21)
		{
			return false;
		}
		string[] parts = value.Substring(2).Split('-');
		return parts.Length == 4 && parts.All(part => part.Length == 4 && part.All(char.IsAsciiLetterOrDigit));
	}

	public static async Task ResetAsync()
	{
		await OperationLock.WaitAsync();
		try
		{
			TerracottaState state = GetState();
			if (state.HttpPort.HasValue)
			{
				try
				{
					using HttpResponseMessage response = await LocalClient.GetAsync($"http://127.0.0.1:{state.HttpPort}/state/ide");
					_ = response.StatusCode;
				}
				catch
				{
				}
			}
			UpdateState(delegate(TerracottaState s)
			{
				s.Status = TerracottaStatus.Idle;
				s.RoomCode = null;
				s.ServerPort = null;
				s.Players = new List<TerracottaPlayer>();
				s.ErrorMessage = null;
			});
			SyncLanBroadcast(null);
		}
		finally
		{
			OperationLock.Release();
		}
	}

	public static async Task StopAsync()
	{
		await OperationLock.WaitAsync();
		try
		{
			TerracottaState state = GetState();
			if (state.HttpPort.HasValue)
			{
				try
				{
					using HttpResponseMessage response = await LocalClient.GetAsync($"http://127.0.0.1:{state.HttpPort}/panic?peaceful=true");
					_ = response.StatusCode;
				}
				catch
				{
				}
			}
			StopProcessInternal();
			UpdateState(delegate(TerracottaState s)
			{
				s.Status = TerracottaStatus.Idle;
				s.HttpPort = null;
				s.RoomCode = null;
				s.ServerPort = null;
				s.Players = new List<TerracottaPlayer>();
			});
			SyncLanBroadcast(null);
		}
		finally
		{
			OperationLock.Release();
		}
	}

	private static void StopProcessInternal()
	{
		try
		{
			_pollCts?.Cancel();
			_pollCts?.Dispose();
			_pollCts = null;
			if (_process != null)
			{
				if (!_process.HasExited)
				{
					_process.Kill(true);
				}
				_process.Dispose();
			}
			_process = null;
			SyncLanBroadcast(null);
		}
		catch (Exception ex)
		{
			LogWrapper.Warn(ex, "[Terracotta] Failed to stop process");
		}
	}

	public static async Task<string> GetLogAsync()
	{
		TerracottaState state = GetState();
		if (state.HttpPort.HasValue)
		{
			try
			{
				using HttpResponseMessage response = await LocalClient.GetAsync($"http://127.0.0.1:{state.HttpPort}/log?fetch=");
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadAsStringAsync();
				}
			}
			catch
			{
			}
		}
		return GetCapturedOutput();
	}
}
