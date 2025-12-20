using Microsoft.Extensions.Logging;
using SecureJournalapp_Paruhang_Khapung.Services;

namespace SecureJournalapp_Paruhang_Khapung
{
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

            // ✅ Required for Blazor MAUI
            builder.Services.AddMauiBlazorWebView();

            // ✅ REGISTER ALL SERVICES (MANDATORY)
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton<AuthDbService>();
            builder.Services.AddSingleton<SecurityService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}