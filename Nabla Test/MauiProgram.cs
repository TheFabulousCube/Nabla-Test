using Microsoft.Extensions.Logging;
using Nabla_Test.Services;
using Plugin.Maui.Audio;

namespace Nabla_Test;
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
        // Registering the services
        builder.Services.AddSingleton<IAudioManager>(AudioManager.Current);
        builder.Services.AddSingleton<AmendAudioRecorderService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
