using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ServerManager.Models;
using Newtonsoft.Json;

namespace ServerManager.Services;

public class NotificationService : INotificationService
{
	private readonly IConfigService _configService;

	private readonly IHttpClientFactory _httpClientFactory;

	public NotificationService(IConfigService configService, IHttpClientFactory httpClientFactory)
	{
		_configService = configService;
		_httpClientFactory = httpClientFactory;
	}

	public async Task SendDiscordWebhookAsync(string message)
	{
		AppConfig appConfig = await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (string.IsNullOrWhiteSpace(appConfig.NotificationSettings.DiscordWebhookUrl))
		{
			return;
		}
		string content = JsonConvert.SerializeObject(new
		{
			content = message
		});
		using HttpClient client = _httpClientFactory.CreateClient();
		using StringContent body = new StringContent(content, Encoding.UTF8, "application/json");
		await client.PostAsync(appConfig.NotificationSettings.DiscordWebhookUrl, body).ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task NotifyRestartAsync(ServerInstance server)
	{
		return SendDiscordWebhookAsync("Server restart scheduled: " + server.Name);
	}

	public Task NotifyCrashAsync(ServerInstance server, string details)
	{
		return SendDiscordWebhookAsync("Crash detected for " + server.Name + ": " + details);
	}

	public Task NotifyUpdateAsync(ServerInstance server, string releaseNotes)
	{
		return SendDiscordWebhookAsync("Update available for " + server.Name + ": " + releaseNotes);
	}
}
