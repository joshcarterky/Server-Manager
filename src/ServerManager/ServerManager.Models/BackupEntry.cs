using System;

namespace ServerManager.Models;

public class BackupEntry
{
	public Guid Id { get; set; } = Guid.NewGuid();


	public Guid ServerId { get; set; }

	public string Name { get; set; } = string.Empty;


	public string FilePath { get; set; } = string.Empty;


	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


	public long SizeBytes { get; set; }
}
