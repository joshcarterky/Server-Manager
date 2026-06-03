namespace ServerManager.Services;

public interface IPlugin
{
	string Name { get; }

	string Version { get; }

	void Initialize();
}
