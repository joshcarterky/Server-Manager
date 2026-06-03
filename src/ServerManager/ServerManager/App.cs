using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ServerManager.Data;
using ServerManager.Services;
using ServerManager.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace ServerManager;

public class App : Application
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Action<HostBuilderContext, IConfigurationBuilder> _003C_003E9__4_1;

		public static Action<HostBuilderContext, IServiceCollection> _003C_003E9__4_2;

		public static DispatcherUnhandledExceptionEventHandler _003C_003E9__4_3;

		public static UnhandledExceptionEventHandler _003C_003E9__4_4;

		public static EventHandler<UnobservedTaskExceptionEventArgs> _003C_003E9__4_5;

		internal void _003COnStartup_003Eb__4_1(HostBuilderContext context, IConfigurationBuilder builder)
		{
			builder.SetBasePath(AppContext.BaseDirectory);
			builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
		}

		internal void _003COnStartup_003Eb__4_2(HostBuilderContext context, IServiceCollection services)
		{
			services.AddSingleton<ILoggingService, LoggingService>();
			services.AddSingleton<IActivityLogService, ActivityLogService>();
			services.AddSingleton<IServerConsoleService, ServerConsoleService>();
			services.AddSingleton<IAppDatabase, AppDatabase>();
			services.AddSingleton<IConfigService, ConfigService>();
			services.AddSingleton<ICurseForgeService, CurseForgeService>();
			services.AddSingleton<ISteamCmdService, SteamCmdService>();
			services.AddSingleton<IServerProcessManager, ServerProcessManager>();
			services.AddSingleton<IRconService, RconService>();
			services.AddSingleton<ISchedulerService, SchedulerService>();
			services.AddSingleton<IBackupService, BackupService>();
			services.AddSingleton<INotificationService, NotificationService>();
			services.AddSingleton<IPluginService, PluginService>();
			services.AddSingleton<DashboardViewModel>();
			services.AddSingleton<ServersViewModel>();
			services.AddSingleton<ModsViewModel>();
			services.AddSingleton<SchedulerViewModel>();
			services.AddSingleton<ConsoleViewModel>();
			services.AddSingleton<ConfigEditorViewModel>();
			services.AddSingleton<ValidationViewModel>();
			services.AddSingleton<WatchdogViewModel>();
			services.AddSingleton<PerformanceViewModel>();
			services.AddSingleton<TemplatesViewModel>();
			services.AddSingleton<DiscordViewModel>();
			services.AddSingleton<LogsViewModel>();
			services.AddSingleton<BackupsViewModel>();
			services.AddSingleton<ClustersViewModel>();
			services.AddSingleton<SettingsViewModel>();
			services.AddSingleton<MainViewModel>();
			services.AddSingleton<MainWindow>();
			services.AddHttpClient();
		}

		internal void _003COnStartup_003Eb__4_3(object _, DispatcherUnhandledExceptionEventArgs args)
		{
			Log.Error(args.Exception, "Unhandled UI exception");
			Host?.Services.GetService<IActivityLogService>()?.Error("Unhandled UI error: " + args.Exception.Message);
			MessageBox.Show(args.Exception.Message, "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Hand);
			args.Handled = true;
		}

		internal void _003COnStartup_003Eb__4_4(object _, UnhandledExceptionEventArgs args)
		{
			if (args.ExceptionObject is Exception ex)
			{
				Log.Fatal(ex, "Unhandled application exception");
				Host?.Services.GetService<IActivityLogService>()?.Error("Unhandled application error: " + ex.Message);
			}
		}

		internal void _003COnStartup_003Eb__4_5(object? _, UnobservedTaskExceptionEventArgs args)
		{
			Log.Error(args.Exception, "Unobserved task exception");
			Host?.Services.GetService<IActivityLogService>()?.Error("Background task error: " + args.Exception.GetBaseException().Message);
			args.SetObserved();
		}
	}

	private bool _contentLoaded;

	public static IHost? Host { get; private set; }

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		IHostBuilder hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder();
		string logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
		Directory.CreateDirectory(logDirectory);
		Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Async(delegate(LoggerSinkConfiguration a)
		{
			a.File(Path.Combine(logDirectory, "Server-Manager-.log"), LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, retainedFileCountLimit: 30, fileSizeLimitBytes: 1073741824L, levelSwitch: null, buffered: false, shared: false, flushToDiskInterval: null, rollingInterval: RollingInterval.Day);
		}).CreateLogger();
		Host = hostBuilder.ConfigureAppConfiguration(delegate(HostBuilderContext context, IConfigurationBuilder builder)
		{
			builder.SetBasePath(AppContext.BaseDirectory);
			builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
		}).ConfigureServices(delegate(HostBuilderContext context, IServiceCollection services)
		{
			services.AddSingleton<ILoggingService, LoggingService>();
			services.AddSingleton<IActivityLogService, ActivityLogService>();
			services.AddSingleton<IServerConsoleService, ServerConsoleService>();
			services.AddSingleton<IAppDatabase, AppDatabase>();
			services.AddSingleton<IConfigService, ConfigService>();
			services.AddSingleton<ICurseForgeService, CurseForgeService>();
			services.AddSingleton<ISteamCmdService, SteamCmdService>();
			services.AddSingleton<IServerProcessManager, ServerProcessManager>();
			services.AddSingleton<IRconService, RconService>();
			services.AddSingleton<ISchedulerService, SchedulerService>();
			services.AddSingleton<IBackupService, BackupService>();
			services.AddSingleton<INotificationService, NotificationService>();
			services.AddSingleton<IPluginService, PluginService>();
			services.AddSingleton<DashboardViewModel>();
			services.AddSingleton<ServersViewModel>();
			services.AddSingleton<ModsViewModel>();
			services.AddSingleton<SchedulerViewModel>();
			services.AddSingleton<ConsoleViewModel>();
			services.AddSingleton<ConfigEditorViewModel>();
			services.AddSingleton<ValidationViewModel>();
			services.AddSingleton<WatchdogViewModel>();
			services.AddSingleton<PerformanceViewModel>();
			services.AddSingleton<TemplatesViewModel>();
			services.AddSingleton<DiscordViewModel>();
			services.AddSingleton<LogsViewModel>();
			services.AddSingleton<BackupsViewModel>();
			services.AddSingleton<ClustersViewModel>();
			services.AddSingleton<SettingsViewModel>();
			services.AddSingleton<MainViewModel>();
			services.AddSingleton<MainWindow>();
			services.AddHttpClient();
		}).UseSerilog()
			.Build();
		App app = this;
		object obj = _003C_003Ec._003C_003E9__4_3;
		if (obj == null)
		{
			DispatcherUnhandledExceptionEventHandler val = delegate(object _, DispatcherUnhandledExceptionEventArgs args)
			{
				Log.Error(args.Exception, "Unhandled UI exception");
				Host?.Services.GetService<IActivityLogService>()?.Error("Unhandled UI error: " + args.Exception.Message);
				MessageBox.Show(args.Exception.Message, "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Hand);
				args.Handled = true;
			};
			_003C_003Ec._003C_003E9__4_3 = val;
			obj = (object)val;
		}
		app.DispatcherUnhandledException += (DispatcherUnhandledExceptionEventHandler)obj;
		AppDomain.CurrentDomain.UnhandledException += delegate(object _, UnhandledExceptionEventArgs args)
		{
			if (args.ExceptionObject is Exception ex2)
			{
				Log.Fatal(ex2, "Unhandled application exception");
				Host?.Services.GetService<IActivityLogService>()?.Error("Unhandled application error: " + ex2.Message);
			}
		};
		TaskScheduler.UnobservedTaskException += delegate(object? _, UnobservedTaskExceptionEventArgs args)
		{
			Log.Error(args.Exception, "Unobserved task exception");
			Host?.Services.GetService<IActivityLogService>()?.Error("Background task error: " + args.Exception.GetBaseException().Message);
			args.SetObserved();
		};
		try
		{
			Log.Information("Starting host");
			await Host.StartAsync();
			Log.Information("Host started");
			Host.Services.GetRequiredService<IActivityLogService>().Info("Application started.");
			IAppDatabase database = Host.Services.GetRequiredService<IAppDatabase>();
			await database.InitializeAsync();
			Log.Information("SQLite database initialized at {DatabasePath}", database.DatabasePath);
			Host.Services.GetRequiredService<IActivityLogService>().Info("Database initialized.");
			MainWindow requiredService = Host.Services.GetRequiredService<MainWindow>();
			Log.Information("Resolved MainWindow");
			requiredService.Show();
			requiredService.Activate();
			Log.Information("MainWindow shown");
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Unhandled startup exception");
			MessageBox.Show(ex.ToString(), "Startup error", MessageBoxButton.OK, MessageBoxImage.Hand);
			Shutdown();
		}
	}

	protected override void OnExit(ExitEventArgs e)
	{
		if (Host != null)
		{
			Host.StopAsync(TimeSpan.FromSeconds(3.0)).GetAwaiter().GetResult();
			Host.Dispose();
			Host = null;
		}
		Log.CloseAndFlush();
		base.OnExit(e);
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			// Startup resources are built in code; old recovered BAML is intentionally not loaded.
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.7.0")]
	public static void Main()
	{
		App app = new App();
		app.Run();
	}
}
