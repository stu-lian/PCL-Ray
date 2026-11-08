using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.Logging;
using PCL.Core.Utils.Exts;

namespace PCL.Core.IO.Net.Http;

public static class HttpSenderExtension
{
	extension(HttpRequestMessage requestMessage)
	{
		public async Task<HttpResponseMessage> SendAsync(HttpClient? httpClient = null, bool addMetedata = true, bool enableLogging = true, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead, int retryTimes = 3, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				if (httpClient == null)
				{
					httpClient = NetworkService.GetClient();
				}
				if (addMetedata)
				{
					requestMessage.WithHeader("User-Agent", "stu-lian/PCL-Ray/" + Basics.VersionName).WithHeader("Referer", $"https://{Basics.VersionCode}.ce.open.pcl2.server/");
				}
				string requestId = Guid.NewGuid().ToString();
				if (enableLogging)
				{
					LogWrapper.Info("Request", $"Send request to {requestMessage.RequestUri} (method = {requestMessage.Method}, id = {requestId})");
				}
				HttpResponseMessage httpResponseMessage = await NetworkService.GetRetryPolicy(retryTimes).ExecuteAsync<HttpResponseMessage>((Func<CancellationToken, Task<HttpResponseMessage>>)(async (CancellationToken token) =>
				{
					if (enableLogging)
					{
						LogWrapper.Debug("Request", "Try attempt (id = " + requestId + ")");
					}
					try
					{
						using HttpRequestMessage requestCopy = await requestMessage.CloneAsync().ConfigureAwait(continueOnCapturedContext: false);
						return await httpClient.SendAsync(requestCopy, httpCompletionOption, token).ConfigureAwait(continueOnCapturedContext: false);
					}
					catch (Exception ex)
					{
						LogWrapper.Debug(ex, "Request", "Try attempt failed (id = " + requestId + ")");
						throw;
					}
				}), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				if (enableLogging)
				{
					LogWrapper.Info("Request", $"End request, got http status code {httpResponseMessage.StatusCode} (id = {requestId})");
				}
				return httpResponseMessage;
			}
			finally
			{
				if (requestMessage != null)
				{
					((IDisposable)requestMessage).Dispose();
				}
			}
		}
	}
}
