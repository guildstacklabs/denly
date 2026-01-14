using Microsoft.Extensions.Logging;
using Denly.Services;

namespace Denly;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		// Core services
		builder.Services.AddSingleton<IAuthService, SupabaseAuthService>();
		builder.Services.AddSingleton<IDenService, SupabaseDenService>();
		builder.Services.AddSingleton<IToastService, ToastService>();

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

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
