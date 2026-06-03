using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ServerManager.Models;
using Newtonsoft.Json.Linq;

namespace ServerManager.Services;

public class CurseForgeService : ICurseForgeService
{
	private const int ArkSurvivalAscendedGameId = 83374;

	private readonly IHttpClientFactory _httpClientFactory;

	private static readonly IReadOnlyList<CurseForgeModResult> BuiltInAsaMods = new List<CurseForgeModResult>
	{
		new CurseForgeModResult
		{
			ProjectId = "940975",
			Name = "Cybers Structures QoL+ (Crossplay)",
			Summary = "Quality-of-life structures and essentials for ARK: Survival Ascended.",
			Author = "CyberAngel",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/985/888/64/64/638495745806778606.png",
			Category = "Structures",
			DownloadCount = 23846032L,
			DownloadBarWidth = 180.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/cybers-structures"
		},
		new CurseForgeModResult
		{
			ProjectId = "928793",
			Name = "Pelayori's Cryo Storage (Crossplay!)",
			Summary = "Cryopods, cryogun, cryo terminal, neuter gun, and dino storage tools.",
			Author = "pelayori",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/972/618/64/64/638474922382511261.png",
			Category = "Structures",
			DownloadCount = 13329497L,
			DownloadBarWidth = 101.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/cryopods"
		},
		new CurseForgeModResult
		{
			ProjectId = "928597",
			Name = "Automated Ark",
			Summary = "Automation, pulling, storage, egg management, and QoL systems.",
			Author = "blitzfire911",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/895/880/64/64/638340875221384467.png",
			Category = "QoL",
			DownloadCount = 9065056L,
			DownloadBarWidth = 69.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/automated-ark"
		},
		new CurseForgeModResult
		{
			ProjectId = "947033",
			Name = "Awesome Spyglass!",
			Summary = "Enhanced spyglass overlay with target details for creatures, players, structures, eggs, and loot.",
			Author = "ChrisMods",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/915/889/64/64/638374185412464094.octet-stream",
			Category = "General",
			DownloadCount = 8691415L,
			DownloadBarWidth = 66.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/awesomespyglass"
		},
		new CurseForgeModResult
		{
			ProjectId = "928621",
			Name = "Utilities Plus",
			Summary = "Reusable tools and quality-of-life upgrades for everyday survival.",
			Author = "blitzfire911",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/925/338/64/64/638393863821735942.png",
			Category = "Weapons",
			DownloadCount = 6653023L,
			DownloadBarWidth = 50.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/utilities-plus"
		},
		new CurseForgeModResult
		{
			ProjectId = "928708",
			Name = "Custom Dino Levels",
			Summary = "Configurable wild dino level distribution for higher and more balanced spawns.",
			Author = "kitzykatty",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/895/998/64/64/638341049698788425.png",
			Category = "Creatures",
			DownloadCount = 6583792L,
			DownloadBarWidth = 50.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/custom-dino-levels"
		},
		new CurseForgeModResult
		{
			ProjectId = "942024",
			Name = "Dino Depot",
			Summary = "Crossplay dino storage with highly configurable capture, release, and terminal systems.",
			Author = "DelilahEve",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/910/158/64/64/638359541506021854.png",
			Category = "Structures",
			DownloadCount = 5304370L,
			DownloadBarWidth = 40.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/dino-depot"
		},
		new CurseForgeModResult
		{
			ProjectId = "930494",
			Name = "Upgrade Station",
			Summary = "Upgrade and salvage weapons, armor, tools, and saddles.",
			Author = "Ghazlawl",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/919/398/64/64/638381602612302436.png",
			Category = "Structures",
			DownloadCount = 5507857L,
			DownloadBarWidth = 42.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/upgrade-station"
		},
		new CurseForgeModResult
		{
			ProjectId = "950914",
			Name = "Awesome Teleporters!",
			Summary = "Teleporters, remote teleporting, and dino tracking utilities.",
			Author = "ChrisMods",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/920/315/64/64/638383589731796125.png",
			Category = "Structures",
			DownloadCount = 4003751L,
			DownloadBarWidth = 30.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/awesometeleporters"
		},
		new CurseForgeModResult
		{
			ProjectId = "940003",
			Name = "Super Structures Ascended",
			Summary = "Building and quality-of-life overhaul inspired by Super Structures.",
			Author = "Legendarsreign",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/936/770/64/64/638414479511924593.png",
			Category = "Structures",
			DownloadCount = 673204L,
			DownloadBarWidth = 12.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/super-structures-ascended"
		},
		new CurseForgeModResult
		{
			ProjectId = "929543",
			Name = "Imbue and Upgrade Station",
			Summary = "Workbench for item quality upgrades and random stat imbues.",
			Author = "HexenLord",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/901/282/64/64/638348462810670476.png",
			Category = "Structures",
			DownloadCount = 2228743L,
			DownloadBarWidth = 17.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/imbue-station"
		},
		new CurseForgeModResult
		{
			ProjectId = "955333",
			Name = "ASA Api Utils",
			Summary = "Client-side and utility features for ASA servers.",
			Author = "pelayori",
			ThumbnailUrl = "https://83374.media.forgecdn.net/avatars/thumbnails/895/909/64/64/638340903071594984.png",
			Category = "General",
			DownloadCount = 1272749L,
			DownloadBarWidth = 12.0,
			ProjectUrl = "https://www.curseforge.com/ark-survival-ascended/mods/asa-api-utils"
		}
	};

	public CurseForgeService(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	public async Task<IReadOnlyList<CurseForgeModResult>> SearchAsaModsAsync(string apiKey, string searchText, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(searchText))
		{
			throw new InvalidOperationException("Enter a mod name or Project ID to search.");
		}
		IReadOnlyList<CurseForgeModResult> localResults = SearchLocalCatalog(searchText);
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			return localResults;
		}
		string url = $"https://api.curseforge.com/v1/mods/search?gameId={83374}&searchFilter={Uri.EscapeDataString(searchText.Trim())}&pageSize=25&sortField=2&sortOrder=desc";
		var (httpResponseMessage, json, _) = await SendCurseForgeRequestAsync(url, apiKey.Trim(), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (httpResponseMessage.StatusCode == HttpStatusCode.Forbidden)
		{
			if (localResults.Count > 0)
			{
				return localResults;
			}
			throw new InvalidOperationException("CurseForge returned 403 Forbidden for ASA mod search. Showing no API results because this credential is not allowed to query ARK: Survival Ascended mods. You can still add mods with Add ID by using their CurseForge Project ID.");
		}
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"CurseForge search failed: {httpResponseMessage.StatusCode} {httpResponseMessage.ReasonPhrase}");
		}
		List<CurseForgeModResult> list = (from x in ((JObject.Parse(json)["data"] as JArray) ?? new JArray()).Select(ParseMod)
			where !string.IsNullOrWhiteSpace(x.ProjectId)
			select x).ToList();
		IReadOnlyList<CurseForgeModResult> result;
		if (list.Count <= 0)
		{
			result = localResults;
		}
		else
		{
			IReadOnlyList<CurseForgeModResult> readOnlyList = list;
			result = readOnlyList;
		}
		return result;
	}

	public IReadOnlyList<CurseForgeModResult> GetTopDownloadedAsaMods()
	{
		return BuiltInAsaMods.OrderByDescending((CurseForgeModResult mod) => mod.DownloadCount).ToList();
	}

	public async Task<IReadOnlyList<CurseForgeModResult>> RefreshTopDownloadedAsaModsAsync(string apiKey, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			return GetTopDownloadedAsaMods();
		}
		string url = $"https://api.curseforge.com/v1/mods/search?gameId={83374}&index=0&pageSize=50&sortField=6&sortOrder=desc";
		var (httpResponseMessage, json, _) = await SendCurseForgeRequestAsync(url, apiKey.Trim(), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			return GetTopDownloadedAsaMods();
		}
		List<CurseForgeModResult> list = (from x in ((JObject.Parse(json)["data"] as JArray) ?? new JArray()).Select(ParseMod)
			where !string.IsNullOrWhiteSpace(x.ProjectId)
			select x).ToList();
		return (list.Count > 0) ? NormalizeDownloadBars(list) : GetTopDownloadedAsaMods();
	}

	private static IReadOnlyList<CurseForgeModResult> NormalizeDownloadBars(IReadOnlyList<CurseForgeModResult> mods)
	{
		long num = Math.Max(1L, mods.Max((CurseForgeModResult mod) => mod.DownloadCount));
		foreach (CurseForgeModResult mod in mods)
		{
			mod.DownloadBarWidth = Math.Max(12.0, Math.Round((double)mod.DownloadCount / (double)num * 180.0));
		}
		return mods;
	}

	private static IReadOnlyList<CurseForgeModResult> SearchLocalCatalog(string searchText)
	{
		string term = searchText.Trim();
		return (from mod in BuiltInAsaMods
			where mod.ProjectId.Equals(term, StringComparison.OrdinalIgnoreCase) || mod.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || mod.Author.Contains(term, StringComparison.OrdinalIgnoreCase) || mod.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)
			orderby mod.Name
			select mod).ToList();
	}

	private async Task<(HttpResponseMessage Response, string Body, string AuthMode)> SendCurseForgeRequestAsync(string url, string apiKey, CancellationToken cancellationToken)
	{
		HttpResponseMessage coreResponse = await SendAsync(url, "x-api-key", apiKey, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		string item = await coreResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (coreResponse.StatusCode != HttpStatusCode.Forbidden)
		{
			return (Response: coreResponse, Body: item, AuthMode: "Core API key");
		}
		coreResponse.Dispose();
		HttpResponseMessage tokenResponse = await SendAsync(url, "X-Api-Token", apiKey, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return (Response: tokenResponse, Body: await tokenResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false), AuthMode: "API token");
	}

	private async Task<string> CheckGamesAccessAsync(string apiKey, CancellationToken cancellationToken)
	{
		using HttpResponseMessage response = await SendAsync("https://api.curseforge.com/v1/games?pageSize=50", "x-api-key", apiKey, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (!response.IsSuccessStatusCode)
		{
			return $"The same credential also failed the CurseForge games endpoint with {response.StatusCode} {response.ReasonPhrase}.";
		}
		return ((JObject.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))["data"] as JArray)?.Any((JToken game) => ReadString(game["id"]) == 83374.ToString(CultureInfo.InvariantCulture)) ?? false) ? "The credential can reach CurseForge games, but the ASA mod search endpoint still denied the request." : "The credential can reach CurseForge games, but ARK: Survival Ascended was not listed as an accessible game for it.";
	}

	private Task<HttpResponseMessage> SendAsync(string url, string headerName, string apiKey, CancellationToken cancellationToken)
	{
		HttpClient httpClient = _httpClientFactory.CreateClient();
		httpClient.DefaultRequestHeaders.Accept.Clear();
		httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		httpClient.DefaultRequestHeaders.UserAgent.Clear();
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ServerManager/1.0");
		httpClient.DefaultRequestHeaders.Remove(headerName);
		httpClient.DefaultRequestHeaders.Add(headerName, apiKey);
		return httpClient.GetAsync(url, cancellationToken);
	}

	private static CurseForgeModResult ParseMod(JToken mod)
	{
		JToken jToken = mod["latestFiles"]?.FirstOrDefault() ?? mod["latestFilesIndexes"]?.FirstOrDefault();
		JArray jArray = mod["authors"] as JArray;
		JToken jToken2 = mod["logo"];
		CurseForgeModResult obj = new CurseForgeModResult
		{
			ProjectId = ReadString(mod["id"]),
			Name = ReadString(mod["name"]),
			Summary = ReadString(mod["summary"]),
			Author = ReadString(jArray?.FirstOrDefault()?["name"])
		};
		string text = ReadString(jToken2?["thumbnailUrl"]);
		obj.ThumbnailUrl = ((text != null && text.Length > 0) ? text : ReadString(jToken2?["url"]));
		obj.ProjectUrl = ReadString(mod["links"]?["websiteUrl"]);
		string text2 = ReadString(jToken?["fileName"]);
		obj.LatestFileName = ((text2 != null && text2.Length > 0) ? text2 : ReadString(jToken?["filename"]));
		string text3 = ReadString(jToken?["id"]);
		obj.LatestFileId = ((text3 != null && text3.Length > 0) ? text3 : ReadString(jToken?["fileId"]));
		obj.DownloadUrl = ReadString(jToken?["downloadUrl"]);
		obj.Category = ReadString((mod["categories"] as JArray)?.FirstOrDefault()?["name"]);
		obj.FileSizeBytes = ReadLong(jToken?["fileLength"]);
		obj.DownloadCount = ReadLong(mod["downloadCount"]);
		obj.LastUpdated = ReadDate(mod["dateModified"]) ?? ReadDate(jToken?["fileDate"]);
		return obj;
	}

	private static string ReadString(JToken? token)
	{
		object obj;
		if (token == null || token.Type != JTokenType.Null)
		{
			obj = token?.ToString();
			if (obj == null)
			{
				return string.Empty;
			}
		}
		else
		{
			obj = string.Empty;
		}
		return (string)obj;
	}

	private static long ReadLong(JToken? token)
	{
		if (token == null)
		{
			return 0L;
		}
		if (!long.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return 0L;
		}
		return result;
	}

	private static DateTime? ReadDate(JToken? token)
	{
		if (token == null)
		{
			return null;
		}
		if (!DateTime.TryParse(token.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
		{
			return null;
		}
		return result;
	}
}
