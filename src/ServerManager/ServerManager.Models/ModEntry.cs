namespace ServerManager.Models;

public class ModEntry
{
	public string Title { get; set; } = string.Empty;


	public string WorkshopId { get; set; } = string.Empty;


	public string CurseForgeFileId { get; set; } = string.Empty;


	public string Author { get; set; } = string.Empty;


	public string ThumbnailUrl { get; set; } = string.Empty;


	public string ProjectUrl { get; set; } = string.Empty;


	public string LatestFileName { get; set; } = string.Empty;


	public string DownloadUrl { get; set; } = string.Empty;


	public string FileSizeText { get; set; } = string.Empty;


	public string LastUpdatedText { get; set; } = string.Empty;


	public string DownloadCountText { get; set; } = string.Empty;


	public int LoadOrder { get; set; }

	public bool AutoUpdate { get; set; } = true;

}
