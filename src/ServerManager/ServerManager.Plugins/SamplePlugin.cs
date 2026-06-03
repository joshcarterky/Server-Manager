using ServerManager.Services;

namespace ServerManager.Plugins;

public class SamplePlugin : IPlugin
{
	public string Name => "ASA Plugin Sample";

	public string Version => "1.0.0";

	public void Initialize()
	{
	}
}
