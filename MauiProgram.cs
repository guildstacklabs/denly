using Denly.Models;
using Denly.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Denly;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		// Load appsettings.json from embedded resource (works on all platforms)
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("Denly.appsettings.json");
		if (stream != null)
		{
			builder.Configuration.AddJsonStream(stream);
			System.Diagnostics.Debug.WriteLine(">>> MauiProgram: Loaded appsettings.json from embedded resource");
		}
		else
		{
			System.Diagnostics.Debug.WriteLine(">>> MauiProgram: WARNING - appsettings.json not found as embedded resource");
		}

		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		builder.Services.Configure<DenlyOptions>(builder.Configuration.GetSection(DenlyOptions.SectionName));

		// Core services
		builder.Services.AddSingleton<IClock, SystemClock>();
		builder.Services.AddSingleton<IAuthService, SupabaseAuthService>();
		builder.Services.AddSingleton<IDenService, SupabaseDenService>();
		builder.Services.AddSingleton<IToastService, ToastService>();
		builder.Services.AddSingleton<IStorageService, SupabaseStorageService>();

		// Data services (Supabase)
		builder.Services.AddSingleton<IScheduleService, SupabaseScheduleService>();
		builder.Services.AddSingleton<IExpenseService, SupabaseExpenseService>();
		builder.Services.AddSingleton<IDocumentService, SupabaseDocumentService>();

		// Platform services
#if ANDROID
		builder.Services.AddSingleton<ISafeAreaService, AndroidSafeAreaService>();
#else
		builder.Services.AddSingleton<ISafeAreaService, DefaultSafeAreaService>();
#endif
		builder.Services.AddSingleton<Microsoft.Maui.Networking.IConnectivity>(
			Microsoft.Maui.Networking.Connectivity.Current);

		AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
		{
			System.Diagnostics.Debug.WriteLine($"********** FirstChanceException **********");
			System.Diagnostics.Debug.WriteLine(e.Exception.ToString());
		};

		builder.Logging.AddConsole();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
		builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

		var app = builder.Build();

		var denService = app.Services.GetService<IDenService>();
		if (denService == null)
		{
			Console.WriteLine("********** DI ERROR: IDenService not registered **********");
			System.Diagnostics.Debug.WriteLine("********** DI ERROR: IDenService not registered **********");
		}

		return app;
	}
}
