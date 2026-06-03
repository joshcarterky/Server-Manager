using System.IO;

namespace ServerManager.Helpers;

public static class FileHelpers
{
	public static void EnsureDirectory(string path)
	{
		if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
	}

	public static string GetOrCreateDirectory(string path)
	{
		EnsureDirectory(path);
		return path;
	}
}
