using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServerManager.Models;
using ServerManager.Services;

namespace ServerManager.ViewModels;

public class DiscordViewModel : ObservableObject
{
	private readonly IConfigService _configService;
	private readonly INotificationService _notificationService;
	private AppConfig? _appConfig;
	private string _webhookUrl = string.Empty;
	private bool _sendRestartNotifications = true;
	private bool _sendCrashAlerts = true;
	private bool _sendUpdateAlerts = true;
	private string _statusText = "Discord settings loaded.";

	public string WebhookUrl
	{
		get => _webhookUrl;
		set => SetProperty(ref _webhookUrl, value);
	}

	public bool SendRestartNotifications
	{
		get => _sendRestartNotifications;
		set => SetProperty(ref _sendRestartNotifications, value);
	}

	public bool SendCrashAlerts
	{
		get => _sendCrashAlerts;
		set => SetProperty(ref _sendCrashAlerts, value);
	}

	public bool SendUpdateAlerts
	{
		get => _sendUpdateAlerts;
		set => SetProperty(ref _sendUpdateAlerts, value);
	}

	public string StatusText
	{
		get => _statusText;
		set => SetProperty(ref _statusText, value);
	}

	public IAsyncRelayCommand SaveCommand { get; }

	public IAsyncRelayCommand TestWebhookCommand { get; }

	public DiscordViewModel(IConfigService configService, INotificationService notificationService)
	{
		_configService = configService;
		_notificationService = notificationService;
		SaveCommand = new AsyncRelayCommand(SaveAsync);
		TestWebhookCommand = new AsyncRelayCommand(TestWebhookAsync);
		Task.Run(LoadAsync).GetAwaiter().GetResult();
	}

	private async Task LoadAsync()
	{
		_appConfig = await _configService.LoadAsync().ConfigureAwait(false);
		NotificationConfig settings = _appConfig.NotificationSettings ?? new NotificationConfig();
		_appConfig.NotificationSettings = settings;
		WebhookUrl = settings.DiscordWebhookUrl;
		SendRestartNotifications = settings.SendRestartNotifications;
		SendCrashAlerts = settings.SendCrashAlerts;
		SendUpdateAlerts = settings.SendUpdateAlerts;
	}

	private async Task SaveAsync()
	{
		if (_appConfig == null)
		{
			await LoadAsync().ConfigureAwait(false);
		}
		if (_appConfig == null)
		{
			StatusText = "Unable to load application settings.";
			return;
		}
		_appConfig.NotificationSettings ??= new NotificationConfig();
		_appConfig.NotificationSettings.DiscordWebhookUrl = WebhookUrl.Trim();
		_appConfig.NotificationSettings.SendRestartNotifications = SendRestartNotifications;
		_appConfig.NotificationSettings.SendCrashAlerts = SendCrashAlerts;
		_appConfig.NotificationSettings.SendUpdateAlerts = SendUpdateAlerts;
		await _configService.SaveAsync(_appConfig).ConfigureAwait(false);
		StatusText = "Discord settings saved.";
	}

	private async Task TestWebhookAsync()
	{
		if (string.IsNullOrWhiteSpace(WebhookUrl))
		{
			StatusText = "Enter a Discord webhook URL before sending a test.";
			return;
		}
		try
		{
			await SaveAsync().ConfigureAwait(false);
			await _notificationService.SendDiscordWebhookAsync("Dedicated Server Manager Discord webhook test sent at " + DateTime.Now.ToString("g") + ".").ConfigureAwait(false);
			StatusText = "Test message sent to Discord.";
		}
		catch (Exception ex)
		{
			StatusText = "Discord test failed: " + ex.Message;
		}
	}
}