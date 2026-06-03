namespace ServerManager.ViewModels;

public class PerformanceViewModel : PlaceholderModuleViewModel
{
	public PerformanceViewModel()
		: base("Performance Tools", "Server tuning recommendations and optimization helpers for ASA hosting.", "Recommended settings for save intervals, networking, and mod load", "CPU affinity and thread optimization planning", "High memory and slow tickrate detection", "One-click Optimize Server workflow")
	{
	}
}
