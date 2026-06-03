using Serilog;

namespace ServerManager.Services;

public interface ILoggingService
{
	ILogger Logger { get; }
}
