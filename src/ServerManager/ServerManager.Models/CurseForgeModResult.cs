using System;
using System.Linq;

namespace ServerManager.Models;

public class CurseForgeModResult
{
	public string ProjectId { get; set; } = string.Empty;


	public string Name { get; set; } = string.Empty;


	public string Summary { get; set; } = string.Empty;


	public string Author { get; set; } = string.Empty;


	public string ThumbnailUrl { get; set; } = string.Empty;


	public string ProjectUrl { get; set; } = string.Empty;


	public string LatestFileName { get; set; } = string.Empty;


	public string LatestFileId { get; set; } = string.Empty;


	public string DownloadUrl { get; set; } = string.Empty;


	public string Category { get; set; } = string.Empty;


	public long FileSizeBytes { get; set; }

	public long DownloadCount { get; set; }

	public double DownloadBarWidth { get; set; }

	public DateTime? LastUpdated { get; set; }

	public string FileSizeText
	{
		get
		{
			if (FileSizeBytes > 0)
			{
				return $"{(double)FileSizeBytes / 1024.0 / 1024.0:0.##} MB";
			}
			return "Unknown size";
		}
	}

	public string DownloadCountText
	{
		get
		{
			if (DownloadCount > 0)
			{
				return DownloadCount.ToString("N0");
			}
			return "Unknown";
		}
	}

	public string LastUpdatedText => LastUpdated?.ToLocalTime().ToString("MMM d, yyyy") ?? "Unknown";

	public string Initials => string.Join("", from x in Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2)
		select x[0]).ToUpperInvariant();
}
