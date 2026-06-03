using System.Threading.Tasks;
using ServerManager.Models;

namespace ServerManager.Services;

public interface INotificationService
{
	Task SendDiscordWebhookAsync(string message);

	Task NotifyRestartAsync(ServerInstance server);

	Task NotifyCrashAsync(ServerInstance server, string details);

	Task NotifyUpdateAsync(ServerInstance server, string releaseNotes);
}
