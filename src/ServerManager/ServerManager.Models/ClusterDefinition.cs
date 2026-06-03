namespace ServerManager.Models;

public class ClusterDefinition
{
	public string ClusterId { get; set; } = string.Empty;


	public string DisplayName { get; set; } = string.Empty;


	public string SharedStoragePath { get; set; } = string.Empty;


	public bool Enabled { get; set; } = true;

}
