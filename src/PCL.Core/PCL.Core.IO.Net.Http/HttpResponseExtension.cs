using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.Utils;

namespace PCL.Core.IO.Net.Http;

public static class HttpResponseExtension
{
	extension(HttpResponseMessage responseMessage)
	{
		public bool IsSuccess => responseMessage.IsSuccessStatusCode;

		public string AsString()
		{
			return responseMessage.AsStringAsync().GetAwaiter().GetResult();
		}

		public async Task<string> AsStringAsync(CancellationToken ct = default(CancellationToken))
		{
			try
			{
				return await responseMessage.Content.ReadAsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (TaskCanceledException)
			{
				throw new TimeoutException("The request was canceled due to a timeout.");
			}
			catch (OperationCanceledException)
			{
				throw new TimeoutException("The operation was canceled.");
			}
		}

		public async Task<Stream> AsStreamAsync(CancellationToken ct = default(CancellationToken))
		{
			try
			{
				return await responseMessage.Content.ReadAsStreamAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (TaskCanceledException)
			{
				throw new TimeoutException("The request was canceled due to a timeout.");
			}
			catch (OperationCanceledException)
			{
				throw new TimeoutException("The operation was canceled.");
			}
		}

		public async Task<byte[]> AsByteArrayAsync(CancellationToken ct = default(CancellationToken))
		{
			try
			{
				return await responseMessage.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (TaskCanceledException)
			{
				throw new TimeoutException("The request was canceled due to a timeout.");
			}
			catch (OperationCanceledException)
			{
				throw new TimeoutException("The operation was canceled.");
			}
		}

		public async Task<T?> AsJsonAsync<T>(JsonSerializerOptions? options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			_ = 2;
			try
			{
				T result;
				await using (Stream stream = await responseMessage.AsStreamAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
				{
					result = await JsonSerializer.DeserializeAsync<T>(stream, options ?? JsonCompat.SerializerOptions, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				return result;
			}
			catch (JsonException innerException)
			{
				throw new InvalidDataException("Failed to deserialize JSON response.", innerException);
			}
		}

		public Dictionary<string, string[]> GetHeaders()
		{
			return responseMessage.Headers.Concat(responseMessage.Content.Headers).ToDictionary((KeyValuePair<string, IEnumerable<string>> k) => k.Key, (KeyValuePair<string, IEnumerable<string>> v) => v.Value.ToArray());
		}

		public Dictionary<string, string[]> GetContentHeaders()
		{
			return responseMessage.Content.Headers.ToDictionary((KeyValuePair<string, IEnumerable<string>> k) => k.Key, (KeyValuePair<string, IEnumerable<string>> v) => v.Value.ToArray());
		}

		public string[] GetHeader(string name)
		{
			if (responseMessage.Headers.TryGetValues(name, out IEnumerable<string> values) || responseMessage.Content.Headers.TryGetValues(name, out values))
			{
				return values.ToArray();
			}
			return Array.Empty<string>();
		}

		public bool TryGetHeader(string name, out string[] values)
		{
			if (responseMessage.Headers.TryGetValues(name, out IEnumerable<string> values2) || responseMessage.Content.Headers.TryGetValues(name, out values2))
			{
				values = values2.ToArray();
				return true;
			}
			values = Array.Empty<string>();
			return false;
		}

		public string? GetFirstHeaderValue(string name)
		{
			return responseMessage.GetHeader(name).FirstOrDefault();
		}

		public bool TryGetFirstHeaderValue(string name, out string value)
		{
			string[] header = responseMessage.GetHeader(name);
			if (header.Length == 0)
			{
				value = string.Empty;
				return false;
			}
			value = header.First();
			return true;
		}

		public void EnsureSuccessStatusCode()
		{
			if (!get_IsSuccess(responseMessage))
			{
				throw new HttpRequestException($"HTTP request failed with status code {responseMessage.StatusCode}: {responseMessage.ReasonPhrase}");
			}
		}

		public async Task EnsureSuccessStatusCodeWithContentAsync(CancellationToken ct = default(CancellationToken))
		{
			if (!get_IsSuccess(responseMessage))
			{
				string value = await responseMessage.AsStringAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				throw new HttpRequestException($"HTTP request failed with status code {responseMessage.StatusCode}: {responseMessage.ReasonPhrase}. Response content: {value}");
			}
		}
	}
}
