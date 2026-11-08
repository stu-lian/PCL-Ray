using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App.Localization;
using PCL.Core.Link.McPing.Model;
using PCL.Core.Logging;
using PCL.Core.Utils;

namespace PCL.Core.Link.McPing;

public class McPingService : IMcPingService, IDisposable
{
	private readonly IPEndPoint _endpoint;

	private readonly string _host;

	private const int DefaultTimeout = 10000;

	private readonly int _timeout;

	private bool _disposed;

	private const string ModuleName = "McPing";

	private static readonly Dictionary<string, string> _ColorMap = new Dictionary<string, string>
	{
		["black"] = "0",
		["dark_blue"] = "1",
		["dark_green"] = "2",
		["dark_aqua"] = "3",
		["dark_red"] = "4",
		["dark_purple"] = "5",
		["gold"] = "6",
		["gray"] = "7",
		["dark_gray"] = "8",
		["blue"] = "9",
		["green"] = "a",
		["aqua"] = "b",
		["red"] = "c",
		["light_purple"] = "d",
		["yellow"] = "e",
		["white"] = "f"
	};

	public IPEndPoint Endpoint => _endpoint;

	public string Host => _host;

	public int Timeout => _timeout;

	public McPingService(IPEndPoint endpoint, int timeout = 10000)
	{
		_endpoint = endpoint;
		_host = _endpoint.Address.ToString();
		_timeout = timeout;
	}

	public McPingService(string ip, int port = 25565, int timeout = 10000)
	{
		_endpoint = (IPAddress.TryParse(ip, out IPAddress address) ? new IPEndPoint(address, port) : new IPEndPoint(Dns.GetHostAddresses(ip).First(), port));
		_host = ip;
		_timeout = timeout;
	}

	public McPingService(string host, IPEndPoint endpoint, int timeout = 10000)
	{
		_endpoint = endpoint;
		_host = host;
		_timeout = timeout;
	}

	public async Task<McPingResult?> PingAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		using Socket so = new Socket(SocketType.Stream, ProtocolType.Tcp);
		using CancellationTokenSource timeoutCts = new CancellationTokenSource(_timeout);
		using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
		try
		{
			LogWrapper.Debug("McPing", $"Connecting to {_endpoint}");
			await so.ConnectAsync(_endpoint.Address, _endpoint.Port, linkedCts.Token);
		}
		catch (OperationCanceledException)
		{
			LogWrapper.Error(new TimeoutException(Lang.Text("Tools.ServerQuery.Error.Timeout.Connect")), "McPing", $"Failed to connect to the {_endpoint}");
			return null;
		}
		catch (Exception ex2)
		{
			LogWrapper.Error(ex2, "McPing", $"Failed to connect to the {_endpoint}");
			return null;
		}
		LogWrapper.Debug("McPing", $"Connection established: {_endpoint}");
		McPingResult result;
		await using (NetworkStream stream = new NetworkStream(so, ownsSocket: false))
		{
			byte[] handshakePacket = _BuildHandshakePacket(_host, _endpoint.Port);
			byte[] statusPacket = _BuildStatusRequestPacket();
			long latency = 0L;
			byte[] array;
			try
			{
				await stream.WriteAsync(handshakePacket, linkedCts.Token);
				LogWrapper.Debug("McPing", $"Handshake sent, packet length: {handshakePacket.Length}");
				await stream.WriteAsync(statusPacket, linkedCts.Token);
				LogWrapper.Debug("McPing", $"Status sent, packet length: {statusPacket.Length}");
				long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				byte[] pingPacket = _BuildPingRequestPacket(timestamp);
				await stream.WriteAsync(pingPacket, linkedCts.Token);
				LogWrapper.Debug("McPing", $"Ping sent, packet length: {pingPacket.Length}");
				(array, latency) = await _ReadStatusPayloadAsync(stream, linkedCts.Token);
			}
			catch (OperationCanceledException)
			{
				LogWrapper.Error(new TimeoutException(Lang.Text("Tools.ServerQuery.Error.Timeout.ReadWrite")), "McPing", $"Operation timed out on {_endpoint}");
				result = null;
				goto end_IL_021e;
			}
			catch (Exception ex4)
			{
				LogWrapper.Error(ex4, "McPing", $"Failed to communicate with {_endpoint}: {ex4.Message}");
				result = null;
				goto end_IL_021e;
			}
			finally
			{
				if (so.Connected)
				{
					so.Shutdown(SocketShutdown.Both);
				}
			}
			so.Close();
			if (array == null || array.Length == 0)
			{
				throw new InvalidDataException(Lang.Text("Tools.ServerQuery.State.NoInfo"));
			}
			JsonNode jsonNode = JsonCompat.ParseNode(Encoding.UTF8.GetString(array)) ?? throw new NullReferenceException(Lang.Text("Tools.ServerQuery.Error.InvalidResponse"));
			if (jsonNode["description"] is JsonObject jsonNode2)
			{
				jsonNode["description"] = _ConvertJNodeToMcString(jsonNode2);
			}
			McPingResult mcPingResult = jsonNode.Deserialize<McPingResult>(JsonCompat.SerializerOptions);
			if ((object)mcPingResult?.Version == null)
			{
				throw new NullReferenceException(Lang.Text("Tools.ServerQuery.Error.InvalidResponse"));
			}
			mcPingResult = mcPingResult with
			{
				Latency = latency
			};
			result = mcPingResult;
			end_IL_021e:;
		}
		return result;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}

	private byte[] _BuildHandshakePacket(string serverIp, int serverPort)
	{
		List<byte> list = new List<byte>();
		list.AddRange(VarIntHelper.Encode(0u));
		list.AddRange(VarIntHelper.Encode(772u));
		byte[] bytes = Encoding.UTF8.GetBytes(serverIp);
		if (bytes.Length > 255)
		{
			throw new Exception(Lang.Text("Tools.ServerQuery.Error.AddressTooLong"));
		}
		list.AddRange(VarIntHelper.Encode((uint)bytes.Length));
		list.AddRange(bytes);
		list.AddRange(BitConverter.GetBytes((ushort)serverPort).AsEnumerable().Reverse());
		list.AddRange(VarIntHelper.Encode(1u));
		list.InsertRange(0, VarIntHelper.Encode((uint)list.Count));
		return list.ToArray();
	}

	private byte[] _BuildStatusRequestPacket()
	{
		List<byte> list = new List<byte>();
		list.AddRange(VarIntHelper.Encode(1u));
		list.AddRange(VarIntHelper.Encode(0u));
		return list.ToArray();
	}

	private byte[] _BuildPingRequestPacket(long timestamp)
	{
		List<byte> list = new List<byte>();
		list.AddRange(VarIntHelper.Encode(9u));
		list.AddRange(VarIntHelper.Encode(1u));
		list.AddRange(BitConverter.GetBytes(timestamp).AsEnumerable().Reverse());
		return list.ToArray();
	}

	private async Task<(byte[] StatusPayload, long Latency)> _ReadStatusPayloadAsync(Stream stream, CancellationToken cancellationToken)
	{
		byte[] statusPayload = null;
		long? latency = null;
		try
		{
			while (statusPayload == null || !latency.HasValue)
			{
				int num = checked((int)(await VarIntHelper.ReadFromStreamAsync(stream, cancellationToken)));
				LogWrapper.Debug("McPing", $"Packet length: {num}");
				if (num <= 0)
				{
					throw new InvalidDataException(Lang.Text("Tools.ServerQuery.Error.EmptyPacket"));
				}
				using MemoryStream packetStream = new MemoryStream(await _ReadExactAsync(stream, num, cancellationToken), writable: false);
				int num2 = checked((int)(await VarIntHelper.ReadFromStreamAsync(packetStream, cancellationToken)));
				LogWrapper.Debug("McPing", $"Packet id: {num2}");
				switch (num2)
				{
				case 0:
				{
					int length = checked((int)(await VarIntHelper.ReadFromStreamAsync(packetStream, cancellationToken)));
					statusPayload = await _ReadExactAsync(packetStream, length, cancellationToken);
					if (packetStream.Position != packetStream.Length)
					{
						LogWrapper.Warn("McPing", $"Status packet contains {packetStream.Length - packetStream.Position} trailing bytes.");
					}
					break;
				}
				case 1:
				{
					byte[] data = await _ReadExactAsync(packetStream, 8, cancellationToken);
					if (packetStream.Position != packetStream.Length)
					{
						LogWrapper.Warn("McPing", $"Pong packet contains {packetStream.Length - packetStream.Position} trailing bytes.");
					}
					latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _ReadInt64BigEndian(data);
					break;
				}
				default:
					LogWrapper.Warn("McPing", $"Ignore unexpected packet type: {num2}");
					break;
				}
			}
		}
		catch (EndOfStreamException innerException)
		{
			if (statusPayload != null && !latency.HasValue)
			{
				throw new EndOfStreamException(Lang.Text("Tools.ServerQuery.Error.StaleConnection"), innerException);
			}
			if (statusPayload == null)
			{
				throw new EndOfStreamException(Lang.Text("Tools.ServerQuery.Error.IncompleteConnection"), innerException);
			}
			throw;
		}
		return (StatusPayload: statusPayload, Latency: latency.Value);
	}

	private static long _ReadInt64BigEndian(byte[] data)
	{
		if (data.Length == 8)
		{
			return BinaryPrimitives.ReadInt64BigEndian(data);
		}
		throw new ArgumentException(Lang.Text("Tools.ServerQuery.Error.PongDataLength"), "data");
	}

	private static async Task<byte[]> _ReadExactAsync(Stream stream, int length, CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[length];
		await stream.ReadExactlyAsync(buffer, cancellationToken);
		return buffer;
	}

	private static string _ConvertJNodeToMcString(JsonNode? jsonNode)
	{
		if (jsonNode == null)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		Stack<JsonNode> stack = new Stack<JsonNode>();
		stack.Push(jsonNode);
		while (stack.Count > 0)
		{
			JsonNode jsonNode2 = stack.Pop();
			switch (jsonNode2.GetValueKind())
			{
			case JsonValueKind.Object:
			{
				JsonObject jsonObject = jsonNode2.AsObject();
				if (jsonObject.TryGetPropertyValue("extra", out JsonNode jsonNode3) && jsonNode3 is JsonArray jsonArray2)
				{
					for (int num2 = jsonArray2.Count - 1; num2 >= 0; num2--)
					{
						if (jsonArray2[num2] != null)
						{
							stack.Push(jsonArray2[num2]);
						}
					}
				}
				if (jsonObject.TryGetPropertyValue("text", out JsonNode _))
				{
					string value = _GetTextStyleString(jsonObject["color"]?.ToString() ?? string.Empty, Convert.ToBoolean(jsonObject["bold"]?.ToString() ?? "false"), Convert.ToBoolean(jsonObject["obfuscated"]?.ToString() ?? "false"), Convert.ToBoolean(jsonObject["strikethrough"]?.ToString() ?? "false"), Convert.ToBoolean(jsonObject["underline"]?.ToString() ?? "false"), Convert.ToBoolean(jsonObject["italic"]?.ToString() ?? "false"));
					StringBuilder stringBuilder2 = stringBuilder;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 2, stringBuilder2);
					handler.AppendFormatted(value);
					handler.AppendFormatted(jsonObject["text"] ?? ((JsonNode?)string.Empty));
					stringBuilder2.Append(ref handler);
				}
				break;
			}
			case JsonValueKind.String:
				stringBuilder.Append(jsonNode2);
				break;
			case JsonValueKind.Array:
			{
				JsonArray jsonArray = jsonNode2.AsArray();
				for (int num = jsonArray.Count - 1; num >= 0; num--)
				{
					if (jsonArray[num] != null)
					{
						stack.Push(jsonArray[num]);
					}
				}
				break;
			}
			default:
				LogWrapper.Warn("McPing", $"解析到无法处理的 Motd 内容({jsonNode2.GetValueKind()})：{jsonNode2}");
				break;
			}
		}
		LogWrapper.Debug("McPing", $"处理 Motd 内容完成，结果：{stringBuilder}");
		return stringBuilder.ToString();
	}

	private static string _GetTextStyleString(string color, bool bold = false, bool obfuscated = false, bool strikethrough = false, bool underline = false, bool italic = false)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (_ColorMap.TryGetValue(color, out string value))
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, stringBuilder2);
			handler.AppendLiteral("§");
			handler.AppendFormatted(value);
			stringBuilder2.Append(ref handler);
		}
		if (bold)
		{
			stringBuilder.Append("§l");
		}
		if (italic)
		{
			stringBuilder.Append("§o");
		}
		if (underline)
		{
			stringBuilder.Append("§n");
		}
		if (strikethrough)
		{
			stringBuilder.Append("§m");
		}
		if (color.StartsWith('#'))
		{
			stringBuilder.Append(color);
		}
		return stringBuilder.ToString();
	}
}
