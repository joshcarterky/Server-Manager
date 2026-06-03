using System.Collections.Generic;
using System.Linq;

namespace ServerManager.Models;

public static class GameProfileIds
{
	public const string ArkSurvivalAscended = "ark-survival-ascended";
}

public class GameProfile
{
	public string Id { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string ShortName { get; set; } = string.Empty;

	public int SteamAppId { get; set; }

	public string DefaultExecutableName { get; set; } = string.Empty;

	public string ConfigPath { get; set; } = string.Empty;

	public override string ToString()
	{
		return string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName;
	}
}

public static class GameProfileCatalog
{
	private static readonly IReadOnlyList<GameProfile> Profiles = new List<GameProfile>
	{
		new GameProfile
		{
			Id = GameProfileIds.ArkSurvivalAscended,
			DisplayName = "ARK: Survival Ascended",
			ShortName = "ASA",
			SteamAppId = 2430930,
			DefaultExecutableName = "ArkAscendedServer.exe",
			ConfigPath = "ShooterGame\\Saved\\Config\\WindowsServer"
		}
	};

	public static IReadOnlyList<GameProfile> All => Profiles;

	public static GameProfile Default => Profiles[0];

	public static GameProfile Get(string? id)
	{
		return Profiles.FirstOrDefault(profile => profile.Id == id) ?? Default;
	}
}
