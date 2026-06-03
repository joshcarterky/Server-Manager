using Serilog;

namespace ServerManager.Services;

public class LoggingService : ILoggingService
{
	public ILogger Logger { get; }

	public LoggingService()
	{
		Logger = Log.Logger;
	}
}
