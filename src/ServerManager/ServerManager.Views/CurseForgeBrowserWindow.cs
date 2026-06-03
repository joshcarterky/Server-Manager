using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;
using ServerManager.Models;
using ServerManager.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;

namespace ServerManager.Views;

public class CurseForgeBrowserWindow : Window, IComponentConnector
{
	private const string DefaultUrl = "https://www.curseforge.com/ark-survival-ascended/mods";

	private readonly DashboardViewModel? _viewModel;

	internal TextBox AddressBox;

	internal WebView2 Browser;

	internal Border LoadingOverlay;

	private readonly DispatcherTimer _loadingTimeout = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(10.0)
	};

	private bool _contentLoaded;
	private bool _retriedAfterFailedNavigation;

	public CurseForgeBrowserWindow(string? startUrl = null, DashboardViewModel? viewModel = null)
	{
		_viewModel = viewModel;
		InitializeComponent();
		_loadingTimeout.Tick += LoadingTimeout_Tick;
		Loaded += async delegate
		{
			try
			{
				string userDataFolder = Path.Combine(AppContext.BaseDirectory, "Data", "WebView2");
				Directory.CreateDirectory(userDataFolder);
				CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
				await Browser.EnsureCoreWebView2Async(environment);
				if (Browser.CoreWebView2 != null)
				{
					Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
					Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "The in-app browser could not initialize WebView2: " + ex.Message, "CurseForge browser", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			Navigate(string.IsNullOrWhiteSpace(startUrl) ? DefaultUrl : startUrl);
		};
	}

	private void Navigate(string address)
	{
		if (!Uri.TryCreate(address, UriKind.Absolute, out Uri result))
		{
			result = new Uri("https://www.curseforge.com/ark-survival-ascended/search?class=mods&page=1&pageSize=50&sortBy=total%20downloads&search=" + Uri.EscapeDataString(address));
		}
		AddressBox.Text = result.ToString();
		LoadingOverlay.Visibility = Visibility.Visible;
		_retriedAfterFailedNavigation = false;
		_loadingTimeout.Stop();
		_loadingTimeout.Start();
		if (Browser.CoreWebView2 != null)
		{
			Browser.CoreWebView2.Navigate(result.ToString());
		}
		else
		{
			Browser.Source = result;
		}
	}

	private void Back_Click(object sender, RoutedEventArgs e)
	{
		if (Browser.CanGoBack)
		{
			Browser.GoBack();
		}
	}

	private void Forward_Click(object sender, RoutedEventArgs e)
	{
		if (Browser.CanGoForward)
		{
			Browser.GoForward();
		}
	}

	private void Refresh_Click(object sender, RoutedEventArgs e)
	{
		LoadingOverlay.Visibility = Visibility.Visible;
		_loadingTimeout.Stop();
		_loadingTimeout.Start();
		Browser.Reload();
	}

	private void Go_Click(object sender, RoutedEventArgs e)
	{
		Navigate(AddressBox.Text.Trim());
	}

	private void OpenExternal_Click(object sender, RoutedEventArgs e)
	{
		string text = Browser.Source?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = text,
				UseShellExecute = true
			});
		}
	}

	private async void AddCurrentMod_Click(object sender, RoutedEventArgs e)
	{
		if (_viewModel == null)
		{
			return;
		}
		if (_viewModel.SelectedServer == null)
		{
			MessageBox.Show(this, "Select a server before adding a CurseForge mod.", "No server selected", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return;
		}
		try
		{
			string pageTextJson = await Browser.ExecuteScriptAsync("document.body ? document.body.innerText : ''");
			string value = await Browser.ExecuteScriptAsync("document.title || ''");
			string? pageText = JsonConvert.DeserializeObject<string>(pageTextJson) ?? string.Empty;
			string title = JsonConvert.DeserializeObject<string>(value) ?? "CurseForge Mod";
			string text = ExtractProjectId(pageText);
			if (string.IsNullOrWhiteSpace(text))
			{
				MessageBox.Show(this, "Open a specific CurseForge mod page first, then click Add Current Mod.", "Project ID not found", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				return;
			}
			CurseForgeModResult mod = new CurseForgeModResult
			{
				ProjectId = text,
				Name = CleanTitle(title),
				ProjectUrl = (Browser.Source?.ToString() ?? string.Empty),
				Summary = "Added from the CurseForge website browser."
			};
			await _viewModel.AddBrowserModAsync(mod);
			MessageBox.Show(this, $"Added {mod.Name} ({mod.ProjectId}) to {_viewModel.SelectedServer.Name}.", "Mod added", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, "Could not add this mod: " + ex.Message, "Add mod failed", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void AddressBox_KeyDown(object sender, KeyEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)e.Key == 6)
		{
			Navigate(AddressBox.Text.Trim());
			e.Handled = true;
		}
	}

	private void Browser_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
	{
		AddressBox.Text = Browser.Source?.ToString() ?? string.Empty;
	}

	private void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
	{
		_loadingTimeout.Stop();
		LoadingOverlay.Visibility = Visibility.Collapsed;
		if (!e.IsSuccess && !_retriedAfterFailedNavigation)
		{
			_retriedAfterFailedNavigation = true;
			AddressBox.Text = DefaultUrl;
			LoadingOverlay.Visibility = Visibility.Visible;
			_loadingTimeout.Start();
			if (Browser.CoreWebView2 != null)
			{
				Browser.CoreWebView2.Navigate(DefaultUrl);
			}
			else
			{
				Browser.Source = new Uri(DefaultUrl);
			}
			return;
		}
		if (Browser.CoreWebView2 != null)
		{
			base.Title = (string.IsNullOrWhiteSpace(Browser.CoreWebView2.DocumentTitle) ? "CurseForge Browser" : Browser.CoreWebView2.DocumentTitle);
		}
	}

	private void Browser_ContentLoading(object? sender, CoreWebView2ContentLoadingEventArgs e)
	{
		_loadingTimeout.Stop();
		LoadingOverlay.Visibility = Visibility.Collapsed;
	}

	private void LoadingTimeout_Tick(object? sender, EventArgs e)
	{
		_loadingTimeout.Stop();
		LoadingOverlay.Visibility = Visibility.Collapsed;
	}

	private static string ExtractProjectId(string pageText)
	{
		Match match = Regex.Match(pageText, "Project\\s*ID\\s*:?\\s*(\\d{4,})", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			return match.Groups[1].Value;
		}
		match = Regex.Match(pageText, "Project\\s+ID\\s*\\r?\\n\\s*(\\d{4,})", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return string.Empty;
		}
		return match.Groups[1].Value;
	}

	private static string CleanTitle(string title)
	{
		string input = Regex.Replace(title, "\\s*-\\s*ARK: Survival Ascended Mods\\s*-\\s*CurseForge\\s*$", string.Empty, RegexOptions.IgnoreCase);
		input = Regex.Replace(input, "\\s*-\\s*CurseForge\\s*$", string.Empty, RegexOptions.IgnoreCase);
		if (!string.IsNullOrWhiteSpace(input))
		{
			return input.Trim();
		}
		return "CurseForge Mod";
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/ServerManager;component/views/curseforgebrowserwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Button)target).Click += Back_Click;
			break;
		case 2:
			((Button)target).Click += Forward_Click;
			break;
		case 3:
			((Button)target).Click += Refresh_Click;
			break;
		case 4:
			AddressBox = (TextBox)target;
			AddressBox.KeyDown += AddressBox_KeyDown;
			break;
		case 5:
			((Button)target).Click += Go_Click;
			break;
		case 6:
			((Button)target).Click += AddCurrentMod_Click;
			break;
		case 7:
			((Button)target).Click += OpenExternal_Click;
			break;
		case 8:
			Browser = (WebView2)target;
			Browser.SourceChanged += Browser_SourceChanged;
			Browser.NavigationCompleted += Browser_NavigationCompleted;
			Browser.ContentLoading += Browser_ContentLoading;
			break;
		case 9:
			LoadingOverlay = (Border)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
