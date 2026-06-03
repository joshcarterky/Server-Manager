using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ServerManager.Models;
using ServerManager.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServerManager.ViewModels;

public class ModsViewModel : ObservableObject
{
	private readonly IConfigService _configService;

	private ModEntry? _selectedMod;

	public ObservableCollection<ModEntry> Mods { get; } = new ObservableCollection<ModEntry>();


	public ModEntry? SelectedMod
	{
		get
		{
			return _selectedMod;
		}
		set
		{
			SetProperty(ref _selectedMod, value, "SelectedMod");
			RemoveModCommand.NotifyCanExecuteChanged();
			MoveUpCommand.NotifyCanExecuteChanged();
			MoveDownCommand.NotifyCanExecuteChanged();
		}
	}

	public IAsyncRelayCommand AddModCommand { get; }

	public IAsyncRelayCommand RemoveModCommand { get; }

	public IRelayCommand MoveUpCommand { get; }

	public IRelayCommand MoveDownCommand { get; }

	public ModsViewModel(IConfigService configService)
	{
		_configService = configService;
		Task.Run(() => LoadModsAsync()).GetAwaiter().GetResult();
		AddModCommand = new AsyncRelayCommand(AddModAsync);
		RemoveModCommand = new AsyncRelayCommand(RemoveModAsync, () => SelectedMod != null);
		MoveUpCommand = new RelayCommand(MoveUp, CanMoveUp);
		MoveDownCommand = new RelayCommand(MoveDown, CanMoveDown);
	}

	private async Task LoadModsAsync()
	{
		foreach (ServerInstance server in (await _configService.LoadAsync().ConfigureAwait(continueOnCapturedContext: false)).Servers)
		{
			foreach (ModEntry mod in server.Mods)
			{
				Mods.Add(mod);
			}
		}
	}

	private Task AddModAsync()
	{
		Mods.Add(new ModEntry
		{
			Title = "New Mod",
			WorkshopId = "0"
		});
		return Task.CompletedTask;
	}

	private Task RemoveModAsync()
	{
		if (SelectedMod != null)
		{
			Mods.Remove(SelectedMod);
		}
		return Task.CompletedTask;
	}

	private void MoveUp()
	{
		if (SelectedMod != null)
		{
			int num = Mods.IndexOf(SelectedMod);
			if (num > 0)
			{
				Mods.Move(num, num - 1);
				UpdateLoadOrders();
			}
		}
	}

	private void MoveDown()
	{
		if (SelectedMod != null)
		{
			int num = Mods.IndexOf(SelectedMod);
			if (num >= 0 && num < Mods.Count - 1)
			{
				Mods.Move(num, num + 1);
				UpdateLoadOrders();
			}
		}
	}

	private bool CanMoveUp()
	{
		if (SelectedMod != null)
		{
			return Mods.IndexOf(SelectedMod) > 0;
		}
		return false;
	}

	private bool CanMoveDown()
	{
		if (SelectedMod != null)
		{
			return Mods.IndexOf(SelectedMod) < Mods.Count - 1;
		}
		return false;
	}

	private void UpdateLoadOrders()
	{
		for (int i = 0; i < Mods.Count; i++)
		{
			Mods[i].LoadOrder = i + 1;
		}
	}
}
