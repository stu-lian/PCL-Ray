using System;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.IO.Net.Http;

namespace PCL;

/// <summary>
/// PCL Ray 内置的 AI 服务客户端（日志分析 / 模组翻译共用）。
/// 接口地址与密钥通过环境变量配置，仓库中不包含任何明文凭据。
/// </summary>
internal static class AiSupport
{
	public const string Model = "glm-4-flash";

	public static string Endpoint =>
		(Environment.GetEnvironmentVariable("PCL_AI_ENDPOINT") ?? "").Trim();

	private static string ApiKey =>
		(Environment.GetEnvironmentVariable("PCL_AI_API_KEY") ?? "").Trim();

	/// <summary>发起一次 chat/completions 请求，返回首个候选的文本内容。</summary>
	public static async Task<string> ChatAsync(string systemPrompt, string userContent, double temperature = 0.2,
		int timeoutSeconds = 20)
	{
		string endpoint = Endpoint;
		if (string.IsNullOrEmpty(endpoint))
		{
			throw new InvalidOperationException("未配置 AI 接口地址，请设置环境变量 PCL_AI_ENDPOINT");
		}
		string apiKey = ApiKey;
		if (string.IsNullOrEmpty(apiKey))
		{
			throw new InvalidOperationException("未配置 AI API Key，请设置环境变量 PCL_AI_API_KEY");
		}

		JsonObject request = new JsonObject
		{
			["model"] = Model,
			["temperature"] = temperature,
			["messages"] = new JsonArray
			{
				new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
				new JsonObject { ["role"] = "user", ["content"] = userContent }
			}
		};

		HttpRequestMessage http = HttpRequest.CreatePost(endpoint)
			.WithJsonContent(request)
			.WithHeader("Authorization", "Bearer " + apiKey);

		using HttpResponseMessage response = await HttpSenderExtension.SendAsync(http, null, true, true,
			HttpCompletionOption.ResponseContentRead, timeoutSeconds, CancellationToken.None);
		string body = await response.AsStringAsync(CancellationToken.None);
		if (!response.IsSuccess)
		{
			throw new HttpRequestException("AI 服务返回 " + (int)response.StatusCode);
		}
		JsonNode? root = ModBase.GetJson(body);
		string? text = root?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("AI 服务未返回内容");
		}
		return text;
	}
}